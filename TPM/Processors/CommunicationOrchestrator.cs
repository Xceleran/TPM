using FSM.Models.TPM;
using FSM.SMSService;
using TPM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FSM.Processors
{
    public class CommunicationOrchestrator
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public void OnStatusTransition(WorkOrderContext ctx, StatusUpdate update, string messageType)
        {
            if (ctx == null || string.IsNullOrEmpty(messageType)) return;

            var settings = LoadSettings(ctx.CompanyId, messageType);
            if (settings == null) return;

            string siteEmail = "", siteMobile = "", tpEmail = "", tpPhone = "", resourceEmail = "", resourceMobile = "";

            if (settings.sendToCustomer || settings.emailEnabled || settings.smsEnabled)
                LoadCustomerSiteContacts(ctx, out siteEmail, out siteMobile);

            if (settings.sendToThirdParty)
                LoadThirdPartyContacts(ctx, out tpEmail, out tpPhone);

            if (settings.sendToResource)
                LoadResourceContacts(ctx, out resourceEmail, out resourceMobile);

            string emailBody = ApplyTokens(settings.emailContent, ctx, update);
            string smsBody = ApplyTokens(settings.smsContent, ctx, update);
            string subject = ApplyTokens(settings.emailSubject, ctx, update);

            if (settings.emailEnabled)
            {
                if (settings.sendToCustomer && !string.IsNullOrEmpty(siteEmail))
                    SendEmail(ctx, siteEmail, subject, emailBody, messageType, "PolicyHolder");
                if (settings.sendToThirdParty && !string.IsNullOrEmpty(tpEmail))
                    SendEmail(ctx, tpEmail, subject, emailBody, messageType, "ThirdParty");
                if (settings.sendToResource && !string.IsNullOrEmpty(resourceEmail))
                    SendEmail(ctx, resourceEmail, subject, emailBody, messageType, "Resource");
            }

            if (settings.smsEnabled)
            {
                if (settings.sendToCustomer && !string.IsNullOrEmpty(siteMobile))
                    SendSms(ctx, siteMobile, smsBody, messageType, "PolicyHolder");
                if (settings.sendToThirdParty && !string.IsNullOrEmpty(tpPhone))
                    SendSms(ctx, tpPhone, smsBody, messageType, "ThirdParty");
                if (settings.sendToResource && !string.IsNullOrEmpty(resourceMobile))
                    SendSms(ctx, resourceMobile, smsBody, messageType, "Resource");
            }
        }

        public void SendManualCommunication(WorkOrderContext ctx, string messageType, string customSubject, string customBody)
        {
            var settings = LoadSettings(ctx.CompanyId, messageType) ?? new CommunicationSettingsExtended
            {
                emailEnabled = true,
                smsEnabled = false,
                sendToCustomer = true,
                sendToThirdParty = true
            };

            string siteEmail = "", siteMobile = "";
            LoadCustomerSiteContacts(ctx, out siteEmail, out siteMobile);

            var update = new StatusUpdate { CanonicalStatus = ctx.WorkOrder?.Status, Notes = customBody };
            if (settings.emailEnabled && !string.IsNullOrEmpty(siteEmail))
                SendEmail(ctx, siteEmail, customSubject ?? settings.emailSubject, customBody, messageType, "Manual");
            if (settings.smsEnabled && !string.IsNullOrEmpty(siteMobile))
                SendSms(ctx, siteMobile, customBody, messageType, "Manual");
        }

        private CommunicationSettingsExtended LoadSettings(string companyId, string messageType)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 * FROM [msSchedulerV3].[dbo].[tbl_TPMCommunicationSettings]
                      WHERE CompanyID = @CompanyID AND messageType = @MessageType", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@MessageType", messageType);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new CommunicationSettingsExtended
                    {
                        messageType = messageType,
                        triggerStatus = r["StatusName"]?.ToString(),
                        emailEnabled = r["SendEmail"] != DBNull.Value && Convert.ToBoolean(r["SendEmail"]),
                        smsEnabled = r["SendSMS"] != DBNull.Value && Convert.ToBoolean(r["SendSMS"]),
                        emailContent = r["EmailTemplate"]?.ToString() ?? "",
                        emailSubject = r["EmailSubject"]?.ToString() ?? r["emailSubject"]?.ToString() ?? "",
                        smsContent = r["SMSTemplate"]?.ToString() ?? "",
                        autoSend = r["AutoSend"] != DBNull.Value && Convert.ToBoolean(r["AutoSend"]),
                        sendToCustomer = r["SendToCustomer"] == DBNull.Value || Convert.ToBoolean(r["SendToCustomer"]),
                        sendToResource = r["SendToResource"] != DBNull.Value && Convert.ToBoolean(r["SendToResource"]),
                        sendToThirdParty = messageType != "AcceptTPWorkOrder"
                    };
                }
            }
        }

        private void LoadCustomerSiteContacts(WorkOrderContext ctx, out string email, out string mobile)
        {
            email = ""; mobile = "";
            if (ctx.AppointmentId <= 0) return;
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 cs.Email, cs.MobileNumber, c.Email AS CustEmail, c.Mobile AS CustMobile
                      FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_CustomerSite] cs ON cs.CompanyID = a.CompanyID AND cs.CustomerID = a.CustomerID AND cs.Id = a.SiteID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
                      WHERE a.CompanyID = @CompanyID AND a.ApptID = @ApptID", con);
                cmd.Parameters.AddWithValue("@CompanyID", ctx.CompanyId);
                cmd.Parameters.AddWithValue("@ApptID", ctx.AppointmentId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        email = r["Email"]?.ToString();
                        if (string.IsNullOrEmpty(email)) email = r["CustEmail"]?.ToString();
                        mobile = r["MobileNumber"]?.ToString();
                        if (string.IsNullOrEmpty(mobile)) mobile = r["CustMobile"]?.ToString();
                    }
                }
            }
        }

        private void LoadThirdPartyContacts(WorkOrderContext ctx, out string email, out string phone)
        {
            email = ""; phone = "";
            if (ctx.ThirdPartyId <= 0) return;
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT ContactEmail, ContactPhone FROM [msSchedulerV3].[dbo].[tbl_ThirdParties]
                      WHERE Id = @Id AND CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Id", ctx.ThirdPartyId);
                cmd.Parameters.AddWithValue("@CompanyID", ctx.CompanyId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        email = r["ContactEmail"]?.ToString();
                        phone = r["ContactPhone"]?.ToString();
                    }
                }
            }
        }

        private void LoadResourceContacts(WorkOrderContext ctx, out string email, out string mobile)
        {
            email = ""; mobile = "";
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 r.Email, r.Mobile FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_Resources] r ON r.Id = a.ResourceID AND r.CompanyID = a.CompanyID
                      WHERE a.CompanyID = @CompanyID AND a.ApptID = @ApptID", con);
                cmd.Parameters.AddWithValue("@CompanyID", ctx.CompanyId);
                cmd.Parameters.AddWithValue("@ApptID", ctx.AppointmentId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        email = r["Email"]?.ToString();
                        mobile = r["Mobile"]?.ToString();
                    }
                }
            }
        }

        private void SendEmail(WorkOrderContext ctx, string toEmail, string subject, string body, string commType, string recipientType)
        {
            try
            {
                var ep = new EmailProcessor();
                ep.SendHtmlFormattedEmail(ctx.CompanyId, ctx.CustomerId ?? "", subject, body, "", toEmail, "", "", new List<EmailContent>());
                LogCommunication(ctx, commType, "Outbound", subject, body, toEmail, null, recipientType);
            }
            catch { }
        }

        private void SendSms(WorkOrderContext ctx, string toMobile, string body, string commType, string recipientType)
        {
            try
            {
                var sms = new TwilioSMSService();
                sms.SendSMS(toMobile, body, ctx.CompanyId);
                LogCommunication(ctx, commType, "Outbound", null, body, null, toMobile, recipientType);
            }
            catch { }
        }

        private void LogCommunication(WorkOrderContext ctx, string commType, string direction, string subject, string message, string email, string phone, string recipientType)
        {
            try
            {
                using (var con = new SqlConnection(_connStr))
                {
                    con.Open();
                    var cmd = new SqlCommand(
                        @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMCommunications]
                          (CompanyID, WorkOrderId, ThirdPartyId, CommunicationType, Direction, Subject, Message, SentDate, SentBy, RecipientEmail, RecipientPhone, Status)
                          VALUES (@CompanyID, @WorkOrderId, @ThirdPartyId, @CommType, @Direction, @Subject, @Message, GETDATE(), @SentBy, @Email, @Phone, @Status)", con);
                    cmd.Parameters.AddWithValue("@CompanyID", ctx.CompanyId);
                    cmd.Parameters.AddWithValue("@WorkOrderId", ctx.WorkOrder?.Id ?? 0);
                    cmd.Parameters.AddWithValue("@ThirdPartyId", ctx.ThirdPartyId > 0 ? (object)ctx.ThirdPartyId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommType", commType + ":" + recipientType);
                    cmd.Parameters.AddWithValue("@Direction", direction);
                    cmd.Parameters.AddWithValue("@Subject", (object)subject ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Message", message ?? "");
                    cmd.Parameters.AddWithValue("@SentBy", "TPM");
                    cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", "Sent");
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private string ApplyTokens(string template, WorkOrderContext ctx, StatusUpdate update)
        {
            if (string.IsNullOrEmpty(template)) return "";
            return template
                .Replace("{Status}", update?.CanonicalStatus ?? "")
                .Replace("{WorkOrderNumber}", ctx.WorkOrder?.WorkOrderNumber ?? "")
                .Replace("{AppointmentId}", ctx.AppointmentId.ToString())
                .Replace("{Notes}", update?.Notes ?? "");
        }
    }
}
