using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;

namespace FSM.Processors
{
    /// <summary>
    /// Status queue for AHS/ServiceBench API and RPA portal jobs.
    /// Items are created when status is pushed with SubmissionMethod API (on failure) or RPA (always).
    /// </summary>
    public class AhsStatusQueueProcessor
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public int EnqueueStatus(string companyId, int workOrderId, int thirdPartyId, string statusCode,
            string submissionMethod, string payloadJson, string portalStatusCode = null)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMStatusQueue]
                      (CompanyID, WorkOrderId, ThirdPartyId, StatusCode, PortalStatusCode, PayloadJson, SubmissionMethod, Status, CreatedDate)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @WorkOrderId, @ThirdPartyId, @StatusCode, @PortalStatusCode, @PayloadJson, @SubmissionMethod, 'Pending', GETDATE())", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
                cmd.Parameters.AddWithValue("@ThirdPartyId", thirdPartyId > 0 ? (object)thirdPartyId : DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusCode", statusCode ?? "Acknowledged");
                cmd.Parameters.AddWithValue("@PortalStatusCode", (object)portalStatusCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PayloadJson", (object)payloadJson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SubmissionMethod", submissionMethod ?? "API");
                return (int)cmd.ExecuteScalar();
            }
        }

        public int EnqueueTestStatus(string companyId, int thirdPartyId, string statusCode = "Acknowledged")
        {
            int workOrderId = GetLatestWorkOrderId(companyId, thirdPartyId);
            if (workOrderId <= 0)
                return 0;

            var hub = new PortalIntegrationHub();
            string portalCode = hub.GetPortalStatusCode(companyId, thirdPartyId > 0 ? thirdPartyId : (int?)null, statusCode);
            var config = thirdPartyId > 0 ? hub.LoadThirdPartyConfig(companyId, thirdPartyId) : null;
            string method = config?.SubmissionMethod ?? "RPA";
            if (string.Equals(method, "Manual", StringComparison.OrdinalIgnoreCase))
                method = "RPA";

            string payload = "{\"action\":\"TestQueue\",\"status\":\"" + statusCode + "\",\"portalCode\":\"" + portalCode + "\"}";
            if (config != null && !string.IsNullOrEmpty(config.PortalUrl))
                payload = "{\"portalUrl\":\"" + config.PortalUrl + "\",\"action\":\"TestQueue\",\"status\":\"" + statusCode + "\"}";

            return EnqueueStatus(companyId, workOrderId, thirdPartyId, statusCode, method, payload, portalCode);
        }

        public Dictionary<string, int> GetQueueSummary(string companyId)
        {
            var summary = new Dictionary<string, int>
            {
                { "Pending", 0 }, { "Processed", 0 }, { "Failed", 0 }, { "Total", 0 }
            };
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT Status, COUNT(1) AS Cnt FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue]
                      WHERE CompanyID = @CompanyID GROUP BY Status", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string status = r["Status"]?.ToString() ?? "";
                        int cnt = Convert.ToInt32(r["Cnt"]);
                        if (summary.ContainsKey(status)) summary[status] = cnt;
                        summary["Total"] += cnt;
                    }
                }
            }
            return summary;
        }

        public List<Dictionary<string, object>> GetRecentItems(string companyId, int maxItems = 20)
        {
            var list = new List<Dictionary<string, object>>();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP (@Max) q.Id, q.CompanyID, q.WorkOrderId, q.ThirdPartyId, q.StatusCode,
                             q.PortalStatusCode, q.SubmissionMethod, q.Status, q.RetryCount, q.ErrorMessage,
                             q.CreatedDate, q.ProcessedDate,
                             wo.WorkOrderNumber, tp.ThirdPartyName, ac.PortalUrl, ac.ApiEndpoint
                      FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue] q
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_WorkOrders] wo ON wo.Id = q.WorkOrderId AND wo.CompanyID = q.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.Id = q.ThirdPartyId
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = q.ThirdPartyId AND ac.CompanyID = q.CompanyID
                      WHERE q.CompanyID = @CompanyID
                      ORDER BY q.CreatedDate DESC", con);
                cmd.Parameters.AddWithValue("@Max", maxItems);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(ReadQueueRow(r));
                }
            }
            return list;
        }

        public List<Dictionary<string, object>> GetPendingItems(string companyId, int maxItems = 50)
        {
            var list = new List<Dictionary<string, object>>();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP (@Max) q.*, wo.WorkOrderNumber, tp.ThirdPartyName,
                             ac.PortalUrl, ac.ApiEndpoint, ac.AuthToken, ac.ApiKey,
                             wc.StatusServiceEndpoint, wc.StatusServiceUser, wc.StatusServicePassword
                      FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue] q
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_WorkOrders] wo ON wo.Id = q.WorkOrderId AND wo.CompanyID = q.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.Id = q.ThirdPartyId
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = q.ThirdPartyId AND ac.CompanyID = q.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc ON wc.CompanyName = tp.ThirdPartyName
                      WHERE q.CompanyID = @CompanyID AND q.Status = 'Pending' AND q.RetryCount < 5
                      ORDER BY q.CreatedDate", con);
                cmd.Parameters.AddWithValue("@Max", maxItems);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(ReadQueueRow(r));
                }
            }
            return list;
        }

        public QueueProcessResult ProcessQueueItem(int queueId, string companyId)
        {
            var result = new QueueProcessResult { QueueId = queueId };
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT q.*, wo.WorkOrderNumber, tp.ThirdPartyName,
                             ac.PortalUrl, ac.ApiEndpoint, ac.AuthToken, ac.ApiKey, ac.SubmissionMethod AS ConfigMethod,
                             wc.StatusServiceEndpoint
                      FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue] q
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_WorkOrders] wo ON wo.Id = q.WorkOrderId
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.Id = q.ThirdPartyId
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = q.ThirdPartyId AND ac.CompanyID = q.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc ON wc.CompanyName = tp.ThirdPartyName
                      WHERE q.Id = @Id AND q.CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Id", queueId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                    {
                        result.Success = false;
                        result.Message = "Queue item not found.";
                        return result;
                    }

                    result.WorkOrderNumber = r["WorkOrderNumber"]?.ToString();
                    result.ThirdPartyName = r["ThirdPartyName"]?.ToString();
                    result.SubmissionMethod = r["SubmissionMethod"]?.ToString() ?? "RPA";
                    string portalUrl = r["PortalUrl"]?.ToString();
                    string apiEndpoint = r["ApiEndpoint"]?.ToString();
                    if (string.IsNullOrEmpty(apiEndpoint))
                        apiEndpoint = r["StatusServiceEndpoint"]?.ToString();
                    string statusCode = r["StatusCode"]?.ToString();
                    string payload = r["PayloadJson"]?.ToString() ?? "";

                    if (string.Equals(result.SubmissionMethod, "RPA", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(portalUrl))
                        {
                            result.Success = false;
                            result.Message = "RPA: Portal URL not configured. Use Configure Portal.";
                            MarkProcessed(queueId, false, result.Message);
                            return result;
                        }
                        result.Success = true;
                        result.Message = "RPA job recorded. Portal: " + portalUrl + " | Status: " + statusCode;
                        result.ManualUrl = portalUrl;
                        MarkProcessed(queueId, true, "RPA queued for portal update");
                        LogCommunication(con, companyId, queueId, result.Message);
                        return result;
                    }

                    if (string.Equals(result.SubmissionMethod, "API", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(apiEndpoint))
                        {
                            result.Success = false;
                            result.Message = "API: No API Endpoint configured. Set it in Configure Portal.";
                            MarkProcessed(queueId, false, result.Message);
                            return result;
                        }
                        try
                        {
                            string apiResult = PostToApi(apiEndpoint, payload, r["AuthToken"]?.ToString(), r["ApiKey"]?.ToString());
                            result.Success = true;
                            result.Message = "API status sent to " + apiEndpoint + ". " + apiResult;
                            MarkProcessed(queueId, true, result.Message);
                            LogCommunication(con, companyId, queueId, result.Message);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            result.Success = false;
                            result.Message = "API failed: " + ex.Message;
                            MarkProcessed(queueId, false, result.Message);
                            return result;
                        }
                    }

                    result.Success = false;
                    result.Message = "Unknown submission method: " + result.SubmissionMethod;
                    MarkProcessed(queueId, false, result.Message);
                    return result;
                }
            }
        }

        public bool ProcessAhsServiceBenchItem(int queueId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand("SELECT CompanyID FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue] WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", queueId);
                var companyId = cmd.ExecuteScalar()?.ToString();
                if (string.IsNullOrEmpty(companyId)) return false;
                return ProcessQueueItem(queueId, companyId).Success;
            }
        }

        public List<QueueProcessResult> ProcessPendingBatch(string companyId, int maxItems = 10)
        {
            var results = new List<QueueProcessResult>();
            var pending = GetPendingItems(companyId, maxItems);
            foreach (var item in pending)
            {
                int id = Convert.ToInt32(item["Id"]);
                results.Add(ProcessQueueItem(id, companyId));
            }
            return results;
        }

        public bool MarkProcessed(int queueId, bool success, string errorMessage)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"UPDATE [msSchedulerV3].[dbo].[tbl_TPMStatusQueue]
                      SET Status = @Status, ProcessedDate = GETDATE(), ErrorMessage = @Error,
                          RetryCount = RetryCount + CASE WHEN @Success = 0 THEN 1 ELSE 0 END
                      WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", queueId);
                cmd.Parameters.AddWithValue("@Status", success ? "Processed" : "Failed");
                cmd.Parameters.AddWithValue("@Error", (object)errorMessage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Success", success ? 1 : 0);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private int GetLatestWorkOrderId(string companyId, int thirdPartyId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 Id FROM [msSchedulerV3].[dbo].[tbl_WorkOrders]
                      WHERE CompanyID = @CompanyID
                        AND (@ThirdPartyId <= 0 OR ThirdPartyId = @ThirdPartyId)
                      ORDER BY CreatedDate DESC", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@ThirdPartyId", thirdPartyId);
                var val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) return 0;
                return Convert.ToInt32(val);
            }
        }

        private static string PostToApi(string endpoint, string payload, string authToken, string apiKey)
        {
            string url = endpoint.TrimEnd('/');
            if (!url.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                url += "/status";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 15000;
            if (!string.IsNullOrEmpty(authToken))
                request.Headers.Add("Authorization", "Bearer " + authToken);
            else if (!string.IsNullOrEmpty(apiKey))
                request.Headers.Add("X-Api-Key", apiKey);

            byte[] data = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(payload) ? "{}" : payload);
            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
                return "HTTP " + (int)response.StatusCode + " " + reader.ReadToEnd();
        }

        private void LogCommunication(SqlConnection con, string companyId, int queueId, string message)
        {
            try
            {
                var cmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMCommunications]
                      (CompanyID, WorkOrderId, ThirdPartyId, MessageType, Direction, Subject, Body, SentDate, SentBy, Status)
                      SELECT q.CompanyID, q.WorkOrderId, q.ThirdPartyId, 'StatusQueue', 'Outbound', 'Status queue processed', @Body, GETDATE(), 'QueueProcessor', 'Sent'
                      FROM [msSchedulerV3].[dbo].[tbl_TPMStatusQueue] q WHERE q.Id = @QueueId", con);
                cmd.Parameters.AddWithValue("@Body", message ?? "");
                cmd.Parameters.AddWithValue("@QueueId", queueId);
                cmd.ExecuteNonQuery();
            }
            catch { /* optional logging */ }
        }

        private Dictionary<string, object> ReadQueueRow(IDataReader r)
        {
            var d = new Dictionary<string, object>();
            for (int i = 0; i < r.FieldCount; i++)
                d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            return d;
        }
    }

    public class QueueProcessResult
    {
        public int QueueId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public string WorkOrderNumber { get; set; }
        public string ThirdPartyName { get; set; }
        public string SubmissionMethod { get; set; }
        public string ManualUrl { get; set; }
    }
}
