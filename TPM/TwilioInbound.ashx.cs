using FSM.Processors;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace TPM
{
    public class TwilioInbound : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string from = context.Request.Form["From"] ?? "";
            string body = context.Request.Form["Body"] ?? "";
            string to = context.Request.Form["To"] ?? "";

            context.Response.ContentType = "text/xml";

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(body))
            {
                context.Response.Write("<Response></Response>");
                return;
            }

            string companyId = ResolveCompanyByPhone(to);
            if (string.IsNullOrEmpty(companyId))
            {
                context.Response.Write("<Response><Message>Unable to route your message. Please contact support.</Message></Response>");
                return;
            }

            var chat = new InquiryChatService();
            int? appointmentId = ResolveAppointmentByPhone(companyId, from);
            var thread = chat.GetOrCreateOpenThread(companyId, "PolicyHolder", null, appointmentId, null, null);
            var result = chat.SendMessage(thread.AccessToken, body, "PolicyHolder");
            dynamic r = result;
            string reply = r.response ?? "Thank you for your message.";

            context.Response.Write("<Response><Message>" + System.Security.SecurityElement.Escape(reply) + "</Message></Response>");
        }

        private string ResolveCompanyByPhone(string toNumber)
        {
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"];
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                    var cmd = new SqlCommand(
                        "SELECT TOP 1 CompanyID FROM [msSchedulerV3].[dbo].[tbl_TwilioSetting] WHERE PhoneNumber LIKE @Phone", con);
                    cmd.Parameters.AddWithValue("@Phone", "%" + toNumber.TrimStart('+').Substring(Math.Max(0, toNumber.Length - 10)) + "%");
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
            catch { return null; }
        }

        private int? ResolveAppointmentByPhone(string companyId, string fromPhone)
        {
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"];
                string phone = fromPhone.TrimStart('+');
                if (phone.Length > 10) phone = phone.Substring(phone.Length - 10);
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                    var cmd = new SqlCommand(
                        @"SELECT TOP 1 a.ApptID FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                          INNER JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
                          WHERE a.CompanyID = @CompanyID AND (c.Mobile LIKE @Phone OR c.Phone LIKE @Phone)
                          ORDER BY a.ApptDateTime DESC", con);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    cmd.Parameters.AddWithValue("@Phone", "%" + phone + "%");
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) return Convert.ToInt32(val);
                }
            }
            catch { }
            return null;
        }

        public bool IsReusable => false;
    }
}
