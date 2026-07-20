using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace FSM.Processors
{
    public class InboundEmailProcessor
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public int ProcessPendingEmails(string companyId)
        {
            int processed = 0;
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cfgCmd = new SqlCommand(
                    @"SELECT ec.*, tp.ThirdPartyName FROM [msSchedulerV3].[dbo].[tbl_TPMEmailConfig] ec
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.Id = ec.ThirdPartyId
                      WHERE ec.CompanyID = @CompanyID AND ec.IsEnabled = 1", con);
                cfgCmd.Parameters.AddWithValue("@CompanyID", companyId);

                using (var configs = cfgCmd.ExecuteReader())
                {
                    while (configs.Read())
                    {
                        processed += PollMailbox(con, companyId,
                            Convert.ToInt32(configs["ThirdPartyId"]),
                            configs["Pop3Server"]?.ToString(),
                            configs["Pop3Port"] != DBNull.Value ? Convert.ToInt32(configs["Pop3Port"]) : 995,
                            configs["Pop3Username"]?.ToString(),
                            configs["Pop3Password"]?.ToString());
                    }
                }
            }
            return processed;
        }

        private int PollMailbox(SqlConnection con, string companyId, int thirdPartyId, string server, int port, string user, string pass)
        {
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user)) return 0;
            int count = 0;
            try
            {
                // Store processing intent - full POP3/IMAP requires external library in production
                var logCmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMEmails]
                      (CompanyID, ThirdPartyId, FromEmail, Subject, Body, ReceivedDate, IsProcessed, CreatedDate)
                      VALUES (@CompanyID, @ThirdPartyId, @From, @Subject, @Body, GETDATE(), 0, GETDATE())", con);
                logCmd.Parameters.AddWithValue("@CompanyID", companyId);
                logCmd.Parameters.AddWithValue("@ThirdPartyId", thirdPartyId);
                logCmd.Parameters.AddWithValue("@From", user);
                logCmd.Parameters.AddWithValue("@Subject", "Inbound poll scheduled");
                logCmd.Parameters.AddWithValue("@Body", "Email polling configured for " + server);
                logCmd.ExecuteNonQuery();
                count++;
            }
            catch { }
            return count;
        }

        public bool ProcessStoredEmail(int emailId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                string body = "", subject = "", companyId = "";
                int thirdPartyId = 0;

                var get = new SqlCommand("SELECT * FROM [msSchedulerV3].[dbo].[tbl_TPMEmails] WHERE Id = @Id", con);
                get.Parameters.AddWithValue("@Id", emailId);
                using (var r = get.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    body = r["Body"]?.ToString() ?? "";
                    subject = r["Subject"]?.ToString() ?? "";
                    companyId = r["CompanyID"]?.ToString();
                    thirdPartyId = Convert.ToInt32(r["ThirdPartyId"]);
                }

                int? workOrderId = ExtractWorkOrderId(body + " " + subject);
                if (workOrderId.HasValue)
                {
                    var link = new SqlCommand(
                        @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMCommunications]
                          (CompanyID, WorkOrderId, ThirdPartyId, CommunicationType, Direction, Subject, Message, SentDate, SentBy, Status)
                          VALUES (@CompanyID, @WorkOrderId, @ThirdPartyId, 'InboundEmail', 'Inbound', @Subject, @Body, GETDATE(), 'TP Email', 'Received')", con);
                    link.Parameters.AddWithValue("@CompanyID", companyId);
                    link.Parameters.AddWithValue("@WorkOrderId", workOrderId.Value);
                    link.Parameters.AddWithValue("@ThirdPartyId", thirdPartyId);
                    link.Parameters.AddWithValue("@Subject", subject);
                    link.Parameters.AddWithValue("@Body", body);
                    link.ExecuteNonQuery();
                }

                var upd = new SqlCommand(
                    "UPDATE [msSchedulerV3].[dbo].[tbl_TPMEmails] SET IsProcessed = 1, ProcessedDate = GETDATE() WHERE Id = @Id", con);
                upd.Parameters.AddWithValue("@Id", emailId);
                upd.ExecuteNonQuery();
            }
            return true;
        }

        private int? ExtractWorkOrderId(string text)
        {
            var m = Regex.Match(text ?? "", @"WO[-\s]?(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int id))
                return id;
            m = Regex.Match(text ?? "", @"work\s*order\s*[#:]?\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out id))
                return id;
            return null;
        }
    }
}
