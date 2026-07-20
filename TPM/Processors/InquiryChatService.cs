using FSM.Models.TPM;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FSM.Processors
{
    public class InquiryChatService
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private readonly CSLDataScopeProvider _dataScope = new CSLDataScopeProvider();

        public InquiryThreadEntity CreateThread(string companyId, string channelType, int? workOrderId, int? appointmentId, string customerId, int? thirdPartyId)
        {
            string token = Guid.NewGuid().ToString("N");
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMInquiryThreads]
                      (CompanyID, WorkOrderId, AppointmentId, CustomerId, ThirdPartyId, ChannelType, AccessToken, Status, CreatedDate)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @WorkOrderId, @AppointmentId, @CustomerId, @ThirdPartyId, @Channel, @Token, 'Open', GETDATE())", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WorkOrderId", (object)workOrderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AppointmentId", (object)appointmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerId", (object)customerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ThirdPartyId", (object)thirdPartyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Channel", channelType);
                cmd.Parameters.AddWithValue("@Token", token);
                int id = (int)cmd.ExecuteScalar();
                return new InquiryThreadEntity { Id = id, AccessToken = token, CompanyID = companyId, ChannelType = channelType, Status = "Open", AppointmentId = appointmentId, WorkOrderId = workOrderId };
            }
        }

        public InquiryThreadEntity GetOrCreateOpenThread(string companyId, string channelType, int? workOrderId, int? appointmentId, string customerId, int? thirdPartyId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 * FROM [msSchedulerV3].[dbo].[tbl_TPMInquiryThreads]
                      WHERE CompanyID = @CompanyID AND ChannelType = @Channel AND Status IN ('Open','Escalated')
                        AND (@AppointmentId IS NULL OR AppointmentId = @AppointmentId)
                        AND (@WorkOrderId IS NULL OR WorkOrderId = @WorkOrderId)
                      ORDER BY CreatedDate DESC", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@Channel", channelType);
                cmd.Parameters.AddWithValue("@AppointmentId", (object)appointmentId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WorkOrderId", (object)workOrderId ?? DBNull.Value);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new InquiryThreadEntity
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            CompanyID = r["CompanyID"].ToString(),
                            WorkOrderId = r["WorkOrderId"] != DBNull.Value ? (int?)Convert.ToInt32(r["WorkOrderId"]) : null,
                            AppointmentId = r["AppointmentId"] != DBNull.Value ? (int?)Convert.ToInt32(r["AppointmentId"]) : null,
                            ChannelType = r["ChannelType"].ToString(),
                            AccessToken = r["AccessToken"].ToString(),
                            Status = r["Status"].ToString()
                        };
                    }
                }
            }
            return CreateThread(companyId, channelType, workOrderId, appointmentId, customerId, thirdPartyId);
        }

        public InquiryThreadEntity GetThreadByToken(string token)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "SELECT * FROM [msSchedulerV3].[dbo].[tbl_TPMInquiryThreads] WHERE AccessToken = @Token", con);
                cmd.Parameters.AddWithValue("@Token", token);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new InquiryThreadEntity
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        CompanyID = r["CompanyID"].ToString(),
                        WorkOrderId = r["WorkOrderId"] != DBNull.Value ? (int?)Convert.ToInt32(r["WorkOrderId"]) : null,
                        AppointmentId = r["AppointmentId"] != DBNull.Value ? (int?)Convert.ToInt32(r["AppointmentId"]) : null,
                        CustomerId = r["CustomerId"]?.ToString(),
                        ThirdPartyId = r["ThirdPartyId"] != DBNull.Value ? (int?)Convert.ToInt32(r["ThirdPartyId"]) : null,
                        ChannelType = r["ChannelType"].ToString(),
                        AccessToken = r["AccessToken"].ToString(),
                        Status = r["Status"].ToString()
                    };
                }
            }
        }

        public object SendMessage(string token, string userMessage, string senderType)
        {
            var thread = GetThreadByToken(token);
            if (thread == null) return new { success = false, message = "Invalid thread" };

            var priorMessages = GetThreadMessages(thread.Id);
            SaveMessage(thread.Id, "Inbound", senderType, userMessage, null, null);

            DataScopeChannel channel = thread.ChannelType == "ThirdParty" ? DataScopeChannel.ThirdParty : DataScopeChannel.PolicyHolder;
            var scoped = _dataScope.GetScopedData(thread.CompanyID, thread.AppointmentId ?? 0, thread.WorkOrderId, channel);
            string aiContext = _dataScope.BuildAiContext(scoped, channel);
            bool hasServiceContext = HasServiceContext(scoped, thread);

            string aiResponse;
            decimal confidence = 0.85m;

            if (ShouldEscalateToStaff(userMessage, priorMessages))
            {
                EscalateThread(thread.Id);
                aiResponse = "Thank you. A service representative has been notified and will contact you shortly.";
                confidence = 0;
            }
            else
            {
                aiResponse = GenerateAiResponse(userMessage, aiContext, thread.CompanyID, priorMessages, channel, hasServiceContext);
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    aiResponse = GenerateRuleBasedResponse(userMessage, aiContext, priorMessages, channel, hasServiceContext);
                    confidence = 0.6m;
                }
            }

            string refs = aiContext.Length > 500 ? aiContext.Substring(0, 500) : aiContext;
            SaveMessage(thread.Id, "Outbound", "AI", aiResponse, confidence, refs);
            return new { success = true, response = aiResponse, confidence = confidence, escalated = confidence == 0 };
        }

        private static bool HasServiceContext(CslScopedData scoped, InquiryThreadEntity thread)
        {
            if (thread.AppointmentId.HasValue && thread.AppointmentId.Value > 0) return true;
            if (thread.WorkOrderId.HasValue && thread.WorkOrderId.Value > 0) return true;
            if (scoped == null) return false;
            return !string.IsNullOrEmpty(scoped.AppointmentDate)
                || (!string.IsNullOrEmpty(scoped.Status) && !scoped.Status.Equals("Unknown", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldEscalateToStaff(string userMessage, List<object> priorMessages)
        {
            string lower = (userMessage ?? "").Trim().ToLower();
            if (string.IsNullOrEmpty(lower)) return false;

            string[] directPhrases = { "speak to", "talk to", "representative", "real person", "human", "agent", "call me", "callback", "call back" };
            if (directPhrases.Any(p => lower.Contains(p)))
                return true;

            string[] affirmatives = { "yes", "yeah", "yep", "sure", "ok", "okay", "please", "connect me" };
            if (!affirmatives.Any(a => lower == a || lower.StartsWith(a + " ") || lower.StartsWith(a + ",")))
                return false;

            string lastOutbound = GetLastOutboundText(priorMessages);
            if (string.IsNullOrEmpty(lastOutbound)) return false;

            string lastLower = lastOutbound.ToLower();
            return lastLower.Contains("representative") || lastLower.Contains("speak with") || lastLower.Contains("contact you");
        }

        private static string GetLastOutboundText(List<object> priorMessages)
        {
            if (priorMessages == null || priorMessages.Count == 0) return "";
            for (int i = priorMessages.Count - 1; i >= 0; i--)
            {
                dynamic m = priorMessages[i];
                if (m.direction == "Outbound")
                    return m.message?.ToString() ?? "";
            }
            return "";
        }

        public List<object> GetThreadMessages(int threadId)
        {
            var list = new List<object>();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT Direction, SenderType, MessageText, AiConfidence, CreatedDate
                      FROM [msSchedulerV3].[dbo].[tbl_TPMInquiryMessages]
                      WHERE ThreadId = @ThreadId ORDER BY CreatedDate", con);
                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new
                        {
                            direction = r["Direction"].ToString(),
                            senderType = r["SenderType"].ToString(),
                            message = r["MessageText"].ToString(),
                            confidence = r["AiConfidence"] != DBNull.Value ? r["AiConfidence"] : null,
                            date = Convert.ToDateTime(r["CreatedDate"]).ToString("g"),
                            createdDate = Convert.ToDateTime(r["CreatedDate"]).ToString("o")
                        });
                    }
                }
            }
            return list;
        }

        public List<object> GetThreadMessagesByToken(string token)
        {
            var thread = GetThreadByToken(token);
            if (thread == null) return new List<object>();
            return GetThreadMessages(thread.Id);
        }

        private void SaveMessage(int threadId, string direction, string senderType, string text, decimal? confidence, string sourceRefs)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMInquiryMessages]
                      (ThreadId, Direction, SenderType, MessageText, AiConfidence, SourceDataRefs, CreatedDate)
                      VALUES (@ThreadId, @Direction, @SenderType, @Message, @Confidence, @Refs, GETDATE())", con);
                cmd.Parameters.AddWithValue("@ThreadId", threadId);
                cmd.Parameters.AddWithValue("@Direction", direction);
                cmd.Parameters.AddWithValue("@SenderType", senderType);
                cmd.Parameters.AddWithValue("@Message", text ?? "");
                cmd.Parameters.AddWithValue("@Confidence", (object)confidence ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Refs", (object)sourceRefs ?? DBNull.Value);
                cmd.ExecuteNonQuery();

                var upd = new SqlCommand(
                    "UPDATE [msSchedulerV3].[dbo].[tbl_TPMInquiryThreads] SET UpdatedDate = GETDATE() WHERE Id = @Id", con);
                upd.Parameters.AddWithValue("@Id", threadId);
                upd.ExecuteNonQuery();
            }
        }

        private void EscalateThread(int threadId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "UPDATE [msSchedulerV3].[dbo].[tbl_TPMInquiryThreads] SET EscalatedToStaff = 1, Status = 'Escalated' WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", threadId);
                cmd.ExecuteNonQuery();
            }
        }

        private string GenerateAiResponse(string userMessage, string context, string companyId, List<object> priorMessages, DataScopeChannel channel, bool hasServiceContext)
        {
            if (!IsAiEnabled(companyId))
                return null;

            string apiKey = ConfigurationManager.AppSettings["OpenAiApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                try
                {
                    return CallOpenAi(apiKey, userMessage, context, priorMessages, channel, hasServiceContext);
                }
                catch { }
            }
            return null;
        }

        private string CallOpenAi(string apiKey, string userMessage, string context, List<object> priorMessages, DataScopeChannel channel, bool hasServiceContext)
        {
            string channelLabel = channel == DataScopeChannel.ThirdParty ? "third-party warranty provider" : "policy holder";
            string systemPrompt = "You are a helpful assistant for a home warranty field service company speaking with a " + channelLabel + ". "
                + "Answer using only the service context provided. Be concise and friendly. "
                + "If asked about 'policy', explain this portal shares appointment status and service updates scoped to their role—not internal staff notes or full warranty contract terms. "
                + "If no service record is linked, explain they need an appointment-linked access link. "
                + "If they want a human, confirm a representative will follow up.";

            var messages = new JArray { new JObject { ["role"] = "system", ["content"] = systemPrompt } };

            if (priorMessages != null)
            {
                foreach (dynamic m in priorMessages.Take(6))
                {
                    string role = m.direction == "Inbound" ? "user" : "assistant";
                    messages.Add(new JObject { ["role"] = role, ["content"] = m.message?.ToString() ?? "" });
                }
            }

            string userContent = "Service context:\n" + (string.IsNullOrWhiteSpace(context) ? "(No appointment linked yet)" : context)
                + "\n\nHas linked service record: " + (hasServiceContext ? "yes" : "no")
                + "\n\nQuestion: " + userMessage;
            messages.Add(new JObject { ["role"] = "user", ["content"] = userContent });

            var payload = new JObject
            {
                ["model"] = "gpt-4o-mini",
                ["messages"] = messages,
                ["max_tokens"] = 300,
                ["temperature"] = 0.4
            };

            var request = (HttpWebRequest)WebRequest.Create("https://api.openai.com/v1/chat/completions");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Bearer " + apiKey);
            request.Timeout = 30000;
            byte[] data = Encoding.UTF8.GetBytes(payload.ToString());
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = JObject.Parse(reader.ReadToEnd());
                    return json["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        System.Diagnostics.Debug.WriteLine("OpenAI error: " + reader.ReadToEnd());
                    }
                }
                return null;
            }
        }

        private string GenerateRuleBasedResponse(string userMessage, string context, List<object> priorMessages, DataScopeChannel channel, bool hasServiceContext)
        {
            string lower = (userMessage ?? "").ToLower();

            if (lower.Contains("policy") || lower.Contains("what is this") || lower.Contains("what can you"))
            {
                if (channel == DataScopeChannel.PolicyHolder)
                {
                    return "This Policy Holder Portal lets you ask about your service appointment, technician, and work order status. "
                        + "I only share information approved for policy holders—not internal staff notes or full warranty contract details. "
                        + (hasServiceContext ? "I can see your linked service record below." : "Your chat is not linked to an appointment yet—use the link from your service notification or include ?apptId= in the URL.");
                }
                return "This TP Inquiry Portal provides authorization, coverage, and work order status scoped for third-party providers.";
            }

            if (!hasServiceContext)
            {
                return "Your chat is not linked to a service appointment yet, so I don't have status details. "
                    + "Open this portal using the link from your service company (with your appointment ID), or ask your coordinator to send you a personalized link.";
            }

            if (lower.Contains("when") || lower.Contains("appointment") || lower.Contains("schedule"))
            {
                if (context.Contains("Appointment:"))
                {
                    foreach (var l in context.Split('\n'))
                        if (l.StartsWith("Appointment:"))
                            return "Your appointment is scheduled for " + l.Replace("Appointment:", "").Trim() + ".";
                }
                return "Your appointment details are being confirmed. A representative can provide the exact date and time.";
            }
            if (lower.Contains("status") || lower.Contains("progress"))
            {
                foreach (var l in context.Split('\n'))
                    if (l.StartsWith("Status:"))
                        return "The current status of your service request is: " + l.Replace("Status:", "").Trim() + ".";
            }
            if (lower.Contains("technician") || lower.Contains("tech"))
            {
                foreach (var l in context.Split('\n'))
                    if (l.StartsWith("Technician:"))
                        return "Your assigned technician is " + l.Replace("Technician:", "").Trim() + ".";
            }
            if (lower.Contains("authoriz") || lower.Contains("coverage") || lower.Contains("covered"))
            {
                return "Authorization and coverage details are being reviewed. Please check back shortly or contact your service coordinator.";
            }

            if (!string.IsNullOrWhiteSpace(context) && context.Contains("Status:"))
            {
                foreach (var l in context.Split('\n'))
                    if (l.StartsWith("Status:"))
                        return "Here's what I can tell you: your service request status is " + l.Replace("Status:", "").Trim()
                            + ". Ask about your appointment date, technician, or say 'representative' to speak with staff.";
            }

            return "I can help with appointment status, schedule, and technician information for your service request. "
                + "Would you like to speak with a representative?";
        }

        public bool IsAiEnabled(string companyId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "SELECT ISNULL(AiChatEnabled, 0) FROM [msSchedulerV3].[dbo].[tbl_TPMSettings] WHERE CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                var val = cmd.ExecuteScalar();
                return val != null && val != DBNull.Value && Convert.ToBoolean(val);
            }
        }
    }
}
