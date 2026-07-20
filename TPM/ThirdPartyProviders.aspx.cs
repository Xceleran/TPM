using FSM.Entity.Customer;
using FSM.Helper;
using FSM.Processors;
using FSM.Models.TPM;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Script.Services;
using System.Web.Services;


namespace TPM
{
   
    public partial class ThirdPartyProviders : System.Web.UI.Page
    {
        static string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        static string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("Dashboard.aspx");
            }
            if (!IsPostBack)
            {
               
            }

            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            companyId.Attributes.Add("value", companyid);
        }

        
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object AssignWarrentyCompany(string WarrentyCompanyID)
        {
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
                return new { success = false, message = "Session expired. Please log in again." };

            if (string.IsNullOrWhiteSpace(WarrentyCompanyID))
                return new { success = false, message = "No warranty company selected." };

            long warrantyUid;
            if (!long.TryParse(WarrentyCompanyID.Trim(), out warrantyUid))
                return new { success = false, message = "Invalid warranty company ID." };

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            bool assignRowExists = false;
            try
            {
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();

                    int existingCustomerId;
                    if (TryGetLinkedCustomerId(con, companyid, warrantyUid, out existingCustomerId))
                        return new { success = true, message = "Provider is already assigned.", alreadyAssigned = true, customerId = existingCustomerId };

                    assignRowExists = AssignRowExists(con, companyid, warrantyUid);

                    int customerId;
                    if (!TryGetLinkedCustomerId(con, companyid, warrantyUid, out customerId))
                    {
                        var nextIdCmd = new SqlCommand(
                            @"SELECT ISNULL(MAX(CustomerID), 0) + 1 FROM [msSchedulerV3].[dbo].[tbl_Customer]
                              WHERE CompanyID = @CompanyID", con);
                        nextIdCmd.Parameters.AddWithValue("@CompanyID", companyid);
                        customerId = Convert.ToInt32(nextIdCmd.ExecuteScalar());

                        var customerCmd = new SqlCommand(
                            @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
                              ([CompanyID],[CustomerID],[FirstName],[LastName],[Title],[JobTitle],[Address1],[City],[State],[ZipCode],
                               [Phone],[Mobile],[Email],[CustomerGuid],[IsPrimaryContact],[IsBusinessContact],[Notes],
                               [CompanyName],[BusinessName],[BusinessID],[Country],[CSLTagId],[CSLTagString],[WarrentyCompanyID])
                              SELECT @CompanyID, @CustomerID, wc.CompanyName, '', '', '',
                                     ISNULL(wc.[Address],''), ISNULL(wc.[City],''), ISNULL(wc.[State],''), ISNULL(wc.[Zip],''),
                                     '', '', '', @CustomerGuid, 1, 1, '',
                                     wc.CompanyName, wc.CompanyName, 0, 'USA', 0, '', CAST(@WarrantyUID AS NVARCHAR(50))
                              FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc
                              WHERE wc.WarrantyCompanyUID = @WarrantyUID AND wc.IsActive = 1", con);
                        customerCmd.Parameters.AddWithValue("@CompanyID", companyid);
                        customerCmd.Parameters.AddWithValue("@CustomerID", customerId);
                        customerCmd.Parameters.AddWithValue("@CustomerGuid", Guid.NewGuid().ToString().ToUpper());
                        customerCmd.Parameters.AddWithValue("@WarrantyUID", warrantyUid);
                        if (customerCmd.ExecuteNonQuery() <= 0)
                            return new { success = false, message = "Warranty company not found or inactive in the catalog." };

                        var linkCmd = new SqlCommand(
                            @"INSERT INTO [msSchedulerV3].[dbo].[tbl_WarrentyCompanyCustomer]
                              ([WarrentyCompanyID],[CustomerID],[CompanyID])
                              VALUES (@WarrantyUID, @CustomerID, @CompanyID)", con);
                        linkCmd.Parameters.AddWithValue("@WarrantyUID", warrantyUid);
                        linkCmd.Parameters.AddWithValue("@CustomerID", customerId.ToString());
                        linkCmd.Parameters.AddWithValue("@CompanyID", companyid);
                        linkCmd.ExecuteNonQuery();
                    }

                    if (!assignRowExists)
                    {
                        var assignCmd = new SqlCommand(
                            @"INSERT INTO [msSchedulerV3].[dbo].[tbl_AssignWarrantyCompany]
                              ([CompanyID],[WarrantyCompanyUID])
                              VALUES (@CompanyID, @WarrantyUID)", con);
                        assignCmd.Parameters.AddWithValue("@CompanyID", companyid);
                        assignCmd.Parameters.AddWithValue("@WarrantyUID", warrantyUid);
                        assignCmd.ExecuteNonQuery();
                    }
                }

                string msg = assignRowExists
                    ? "Provider link repaired — customer record created for this warranty company."
                    : "Provider assigned successfully.";
                return new { success = true, message = msg, alreadyAssigned = assignRowExists, repaired = assignRowExists };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AssignWarrentyCompany error: " + ex.Message);
                return new { success = false, message = "Assign failed: " + ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<WarrantyCompany> GetBillableItems()
        {
            var items = new List<WarrantyCompany>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetBillableItems: CompanyID is missing from session");
                return items;
            }
            Database db = new Database();
            try
            {
               
                DataTable dt = new DataTable();
                // Check if QboType column exists, if not use 0 as default
                string sql = @"SELECT  [WarrantyCompanyUID],WarrantyCompanyGuID
                              ,[CompanyName]
                              ,[IsActive]
                              ,[ShortName]
                              ,[Address]
                              ,[City]
                              ,[State]
                              ,[Zip]
                          FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] where [IsActive] = 1 order by CompanyName;";

                sql += @"SELECT awc.WarrantyCompanyUID AS WarrentyCompanyID,
                                COALESCE(NULLIF(LTRIM(RTRIM(wcc.CustomerID)), ''),
                                         CAST(cByLink.CustomerID AS NVARCHAR(20)),
                                         CAST(cByWarranty.CustomerID AS NVARCHAR(20))) AS CustomerID,
                                COALESCE(cByLink.CustomerGuid, cByWarranty.CustomerGuid) AS CustomerGuid,
                                COALESCE(cByLink.Email, cByWarranty.Email) AS Email,
                                COALESCE(cByLink.Phone, cByWarranty.Phone) AS Phone,
                                COALESCE(cByLink.Mobile, cByWarranty.Mobile) AS Mobile,
                                ac.PortalUrl, ac.ApiEndpoint, ac.SubmissionMethod, ISNULL(ac.IsEnabled, 0) AS ApiEnabled, tp.Id AS ThirdPartyId
                         FROM [msSchedulerV3].[dbo].[tbl_AssignWarrantyCompany] awc
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_WarrentyCompanyCustomer] wcc
                           ON wcc.CompanyID = awc.CompanyID AND wcc.WarrentyCompanyID = awc.WarrantyCompanyUID
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] cByLink
                           ON cByLink.CompanyID = awc.CompanyID AND cByLink.CustomerID = TRY_CAST(wcc.CustomerID AS INT)
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] cByWarranty
                           ON cByWarranty.CompanyID = awc.CompanyID
                          AND TRY_CAST(cByWarranty.WarrentyCompanyID AS BIGINT) = awc.WarrantyCompanyUID
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc ON wc.WarrantyCompanyUID = awc.WarrantyCompanyUID
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.ThirdPartyName = wc.CompanyName AND tp.CompanyID = awc.CompanyID
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = tp.Id AND ac.CompanyID = awc.CompanyID
                         WHERE awc.CompanyID = @CompanyID;";

                sql += @"SELECT wc.WarrantyCompanyUID, tp.Id AS ThirdPartyId, ac.PortalUrl, ac.ApiEndpoint, ac.SubmissionMethod, ISNULL(ac.IsEnabled, 0) AS ApiEnabled
                         FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.ThirdPartyName = wc.CompanyName AND tp.CompanyID = @CompanyID
                         LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = tp.Id AND ac.CompanyID = @CompanyID
                         WHERE wc.IsActive = 1;";
                DataSet dataSet= db.Get_DataSet(sql, companyid);

                dt = dataSet.Tables[0];
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var item = new WarrantyCompany();
                        item.CompanyID = companyid;
                        item.Id = row["WarrantyCompanyUID"].ToString().Trim() ;
                        item.CompanyName = row.Field<string>("CompanyName") ?? "";
                        item.Address = row.Field<string>("Address") ?? "";
                        item.City = row.Field<string>("City") ?? "";
                        item.State = row.Field<string>("State") ?? "";
                        item.Zip = row.Field<string>("Zip") ?? "";
                        item.WarrantyCompanyGuID = row.Field<string>("WarrantyCompanyGuID") ?? "";
                        foreach (DataRow _row in dataSet.Tables[1].Rows)
                        {
                            if (string.Equals(row["WarrantyCompanyUID"].ToString().Trim(), _row["WarrentyCompanyID"].ToString().Trim()))
                            {
                                item.IsEnable = true;
                                item.CustomerID = _row["CustomerID"]?.ToString()?.Trim() ?? "";
                                item.CustomerGuid = _row["CustomerGuid"]?.ToString()?.Trim() ?? "";
                                item.Email = _row["Email"]?.ToString() ?? "";
                                item.Phone = _row["Phone"]?.ToString() ?? "";
                                item.Mobile = _row["Mobile"]?.ToString() ?? "";
                                item.PortalUrl = _row["PortalUrl"]?.ToString() ?? "";
                                if (_row["ThirdPartyId"] != DBNull.Value)
                                    item.ThirdPartyId = Convert.ToInt32(_row["ThirdPartyId"]);
                                break;
                            }
                        }

                        if (item.IsEnable && string.IsNullOrEmpty(item.CustomerID))
                            BackfillCustomerFromWarranty(companyid, item);

                        if (dataSet.Tables.Count > 2)
                        {
                            foreach (DataRow tpRow in dataSet.Tables[2].Rows)
                            {
                                if (string.Equals(row["WarrantyCompanyUID"].ToString().Trim(), tpRow["WarrantyCompanyUID"].ToString().Trim()))
                                {
                                    if (tpRow["ThirdPartyId"] != DBNull.Value && item.ThirdPartyId <= 0)
                                        item.ThirdPartyId = Convert.ToInt32(tpRow["ThirdPartyId"]);
                                    if (string.IsNullOrEmpty(item.PortalUrl))
                                        item.PortalUrl = tpRow["PortalUrl"]?.ToString() ?? "";
                                    break;
                                }
                            }
                        }

                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetBillableItems: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return items;
            }
            finally
            {
                db.Close();
            }
            return items;
        }

        private static bool AssignRowExists(SqlConnection con, string companyId, long warrantyUid)
        {
            var cmd = new SqlCommand(
                @"SELECT COUNT(1) FROM [msSchedulerV3].[dbo].[tbl_AssignWarrantyCompany]
                  WHERE CompanyID = @CompanyID AND WarrantyCompanyUID = @WarrantyUID", con);
            cmd.Parameters.AddWithValue("@CompanyID", companyId);
            cmd.Parameters.AddWithValue("@WarrantyUID", warrantyUid);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool TryGetLinkedCustomerId(SqlConnection con, string companyId, long warrantyUid, out int customerId)
        {
            customerId = 0;
            var cmd = new SqlCommand(
                @"SELECT TOP 1 CustomerID FROM (
                      SELECT TRY_CAST(wcc.CustomerID AS INT) AS CustomerID
                      FROM [msSchedulerV3].[dbo].[tbl_WarrentyCompanyCustomer] wcc
                      WHERE wcc.CompanyID = @CompanyID AND wcc.WarrentyCompanyID = @WarrantyUID
                      UNION
                      SELECT c.CustomerID
                      FROM [msSchedulerV3].[dbo].[tbl_Customer] c
                      WHERE c.CompanyID = @CompanyID
                        AND TRY_CAST(c.WarrentyCompanyID AS BIGINT) = @WarrantyUID
                  ) linked
                  WHERE CustomerID IS NOT NULL AND CustomerID > 0
                  ORDER BY CustomerID DESC", con);
            cmd.Parameters.AddWithValue("@CompanyID", companyId);
            cmd.Parameters.AddWithValue("@WarrantyUID", warrantyUid);
            var val = cmd.ExecuteScalar();
            if (val == null || val == DBNull.Value) return false;
            customerId = Convert.ToInt32(val);
            return customerId > 0;
        }

        private static void BackfillCustomerFromWarranty(string companyId, WarrantyCompany item)
        {
            if (string.IsNullOrEmpty(companyId) || item == null || string.IsNullOrEmpty(item.Id)) return;
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            using (var con = new SqlConnection(connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 CustomerID, CustomerGuid, Email, Phone, Mobile
                      FROM [msSchedulerV3].[dbo].[tbl_Customer]
                      WHERE CompanyID = @CompanyID
                        AND TRY_CAST(WarrentyCompanyID AS BIGINT) = TRY_CAST(@WarrantyUID AS BIGINT)
                      ORDER BY CustomerID DESC", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WarrantyUID", item.Id);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return;
                    item.CustomerID = r["CustomerID"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(item.CustomerGuid))
                        item.CustomerGuid = r["CustomerGuid"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(item.Email))
                        item.Email = r["Email"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(item.Phone))
                        item.Phone = r["Phone"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(item.Mobile))
                        item.Mobile = r["Mobile"]?.ToString() ?? "";
                }
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveApiConfig(int thirdPartyId, string portalUrl, string apiEndpoint, string submissionMethod, bool isEnabled, int warrantyCompanyId = 0)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
                return new { success = false, message = "Session expired." };

            if (thirdPartyId <= 0 && warrantyCompanyId > 0)
                thirdPartyId = EnsureThirdPartyRecord(companyId, warrantyCompanyId);

            if (thirdPartyId <= 0)
                return new { success = false, message = "Could not resolve third party. Assign the provider first or try again." };

            var hub = new PortalIntegrationHub();
            hub.SaveApiConfig(new ThirdPartyConfig
            {
                CompanyId = companyId,
                ThirdPartyId = thirdPartyId,
                PortalUrl = portalUrl,
                ApiEndpoint = apiEndpoint,
                SubmissionMethod = submissionMethod ?? "Manual",
                IsEnabled = isEnabled
            }, HttpContext.Current.Session["LoginUser"]?.ToString());
            return new { success = true, thirdPartyId = thirdPartyId };
        }

        private static int EnsureThirdPartyRecord(string companyId, int warrantyCompanyId)
        {
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            using (var con = new System.Data.SqlClient.SqlConnection(connStr))
            {
                con.Open();
                var nameCmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT CompanyName FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] WHERE WarrantyCompanyUID = @Id", con);
                nameCmd.Parameters.AddWithValue("@Id", warrantyCompanyId);
                string name = nameCmd.ExecuteScalar()?.ToString();
                if (string.IsNullOrEmpty(name)) return 0;

                var check = new System.Data.SqlClient.SqlCommand(
                    "SELECT Id FROM [msSchedulerV3].[dbo].[tbl_ThirdParties] WHERE CompanyID = @CompanyID AND ThirdPartyName = @Name", con);
                check.Parameters.AddWithValue("@CompanyID", companyId);
                check.Parameters.AddWithValue("@Name", name);
                var existing = check.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing);

                var insert = new System.Data.SqlClient.SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_ThirdParties]
                      (CompanyID, ThirdPartyName, ThirdPartyType, IsActive, CreatedDate, CreatedBy)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @Name, 'Warranty', 1, GETDATE(), 'System')", con);
                insert.Parameters.AddWithValue("@CompanyID", companyId);
                insert.Parameters.AddWithValue("@Name", name);
                return Convert.ToInt32(insert.ExecuteScalar());
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetApiConfig(int thirdPartyId)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId) || thirdPartyId <= 0)
                return new { success = false, message = "Invalid request." };

            var hub = new PortalIntegrationHub();
            var config = hub.LoadThirdPartyConfig(companyId, thirdPartyId);
            if (config == null)
                return new { success = true, portalUrl = "", apiEndpoint = "", submissionMethod = "Manual", isEnabled = false };

            return new
            {
                success = true,
                portalUrl = config.PortalUrl ?? "",
                apiEndpoint = config.ApiEndpoint ?? "",
                submissionMethod = config.SubmissionMethod ?? "Manual",
                isEnabled = config.IsEnabled
            };
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object PushStatusToPortal(int workOrderId, string status)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
                return new { success = false, message = "Session expired. Please log in again." };

            if (workOrderId <= 0)
                return new { success = false, message = "Enter a valid work order ID." };

            var woProc = new WorkOrderProcessor();
            var wo = woProc.GetById(companyId, workOrderId);
            if (wo == null || wo.Id <= 0)
                return new { success = false, message = "Work order " + workOrderId + " was not found." };

            var ctx = new WorkOrderContext
            {
                WorkOrder = wo,
                CompanyId = companyId,
                ThirdPartyId = wo.ThirdPartyId,
                AppointmentId = wo.AppointmentId ?? 0
            };
            var hub = new PortalIntegrationHub();
            var result = hub.PushStatusAsync(ctx, new StatusUpdate
            {
                CanonicalStatus = string.IsNullOrEmpty(status) ? "Acknowledged" : status,
                ChangedBy = HttpContext.Current.Session["LoginUser"]?.ToString()
            }).GetAwaiter().GetResult();

            return new
            {
                success = result.Success,
                message = result.Message,
                manualUrl = result.ManualUrl,
                requiresManual = result.RequiresManualAction
            };
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetEmailHistoryUrl(string customerId)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            string userId = HttpContext.Current.Session["LoginUser"]?.ToString();
            if (string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(customerId))
                return new { success = false, message = "Session expired or customer not assigned." };

            try
            {
                string newGuid = Guid.NewGuid().ToString();
                string sql = $"INSERT INTO XinatorCentral.dbo.tbl_Login (SessionGuid, SessionString) VALUES ('{newGuid}', '{userId}|{companyId}')";
                Database db = new Database();
                db.UpdateSql(sql);

                string cecBaseUrl = ConfigurationManager.AppSettings["cecBaseUrl"];
                if (string.IsNullOrEmpty(cecBaseUrl))
                    return new { success = false, message = "CEC URL is not configured in Web.config." };

                string redirectUrl = HttpUtility.UrlEncode($"EmailHistory_List.aspx?Id={customerId}");
                string url = $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
                return new { success = true, url = url };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetStatusQueue()
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
                return new { success = false, message = "Session expired." };

            var queue = new AhsStatusQueueProcessor();
            var summary = queue.GetQueueSummary(companyId);
            var items = queue.GetRecentItems(companyId, 15);
            var pending = queue.GetPendingItems(companyId, 50);

            var rows = new List<object>();
            foreach (var item in items)
            {
                rows.Add(new
                {
                    id = item.ContainsKey("Id") ? item["Id"] : null,
                    workOrderNumber = item.ContainsKey("WorkOrderNumber") ? item["WorkOrderNumber"] : "",
                    thirdPartyName = item.ContainsKey("ThirdPartyName") ? item["ThirdPartyName"] : "",
                    statusCode = item.ContainsKey("StatusCode") ? item["StatusCode"] : "",
                    submissionMethod = item.ContainsKey("SubmissionMethod") ? item["SubmissionMethod"] : "",
                    status = item.ContainsKey("Status") ? item["Status"] : "",
                    createdDate = item.ContainsKey("CreatedDate") ? item["CreatedDate"] : null,
                    processedDate = item.ContainsKey("ProcessedDate") ? item["ProcessedDate"] : null,
                    errorMessage = item.ContainsKey("ErrorMessage") ? item["ErrorMessage"] : ""
                });
            }

            return new
            {
                success = true,
                summary = summary,
                pendingCount = pending.Count,
                items = rows
            };
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object EnqueueTestPortalStatus(int thirdPartyId, string status)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
                return new { success = false, message = "Session expired." };

            if (thirdPartyId <= 0)
                return new { success = false, message = "Save portal config first (third party ID required)." };

            var hub = new PortalIntegrationHub();
            var config = hub.LoadThirdPartyConfig(companyId, thirdPartyId);
            if (config == null || !config.IsEnabled)
                return new { success = false, message = "Enable portal integration and save config first." };

            if (string.Equals(config.SubmissionMethod, "Manual", StringComparison.OrdinalIgnoreCase))
                return new { success = false, message = "Manual mode does not use the queue. Set Submission Method to API or RPA." };

            var queue = new AhsStatusQueueProcessor();
            int queueId = queue.EnqueueTestStatus(companyId, thirdPartyId, string.IsNullOrEmpty(status) ? "Acknowledged" : status);
            if (queueId <= 0)
                return new { success = false, message = "No work order found. Accept a job on New Work Orders first to create a work order." };

            return new
            {
                success = true,
                message = "Test status queued (ID " + queueId + "). Click Process Status Queue.",
                queueId = queueId,
                submissionMethod = config.SubmissionMethod
            };
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object ProcessStatusQueue()
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
                return new { success = false, message = "Session expired." };

            var queue = new AhsStatusQueueProcessor();
            int pendingBefore = queue.GetPendingItems(companyId, 50).Count;
            var results = queue.ProcessPendingBatch(companyId, 10);
            var summary = queue.GetQueueSummary(companyId);
            var items = queue.GetRecentItems(companyId, 10);

            int processed = 0, failed = 0;
            var resultRows = new List<object>();
            foreach (var r in results)
            {
                if (r.Success) processed++; else failed++;
                resultRows.Add(new
                {
                    queueId = r.QueueId,
                    success = r.Success,
                    message = r.Message,
                    workOrderNumber = r.WorkOrderNumber,
                    thirdPartyName = r.ThirdPartyName,
                    method = r.SubmissionMethod,
                    manualUrl = r.ManualUrl
                });
            }

            return new
            {
                success = true,
                processed = processed,
                failed = failed,
                pending = summary.ContainsKey("Pending") ? summary["Pending"] : 0,
                pendingBefore = pendingBefore,
                message = pendingBefore == 0
                    ? "No pending items. Configure portal as API/RPA, then use 'Queue test status' or push status from a work order."
                    : "Processed " + processed + " item(s), " + failed + " failed.",
                results = resultRows,
                recent = items.Count
            };
        }


       
    }

    public class WarrantyCompany
    {
        public string CompanyID { get; set; }
        public string Id { get; set; }
        public string Zip { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string WarrantyCompanyGuID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerGuid { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string PortalUrl { get; set; }
        public int ThirdPartyId { get; set; }
        public int WorkOrderId { get; set; }
        
        public Boolean IsEnable { get; set; }
    }
    
}