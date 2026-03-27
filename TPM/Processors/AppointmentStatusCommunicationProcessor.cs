using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web;
using FSM.Entity;
using FSM.SMSService;

namespace TPM.Processors
{
    /// <summary>
    /// Handles email and SMS communication when appointment status changes.
    /// Uses existing Automated Message Customization system and Field Agent Content Profiles.
    /// </summary>
    public class AppointmentStatusCommunicationProcessor
    {
        private string connStr;
        private string companyId;

        public AppointmentStatusCommunicationProcessor(string companyId)
        {
            this.companyId = companyId;
            this.connStr = ConfigurationManager.AppSettings["ConnString"]?.ToString() ?? "";
        }

        /// <summary>
        /// Process communication when appointment status changes
        /// Uses existing SendAppointmentSMS method with Field Agent Content Profiles for resource mobile
        /// </summary>
        public void ProcessStatusChange(int appointmentId, int oldStatusId, int newStatusId, string oldStatusName, string newStatusName, int? resourceId = null)
        {
            ConsoleLog($"ProcessStatusChange called: ApptID={appointmentId}, OldStatus={oldStatusName}({oldStatusId}), NewStatus={newStatusName}({newStatusId})");

            try
            {
                // Get appointment basic details
                var appointment = GetAppointmentBasicDetails(appointmentId);
                if (appointment == null)
                {
                    ConsoleLog($"Appointment {appointmentId} not found. Skipping communication.");
                    return;
                }

                // Map status to SMS code (1=Pending, 2=Scheduled/Confirmed, 3=Cancelled, 4=Closed, 5=Progress, 6=Completed)
                string statusCode = MapStatusToSmsCode(newStatusId, newStatusName);
                
                // Get customer details for template replacement
                var customerDetails = GetCustomerDetailsForTemplates(appointment.CustomerID);
                
                // Load email and SMS templates from Settings (tbl_FSMSMSSettings)
                var templates = GetMessageTemplates(newStatusName, statusCode);
                
                // Send Email if template exists and enabled
                if (templates.EmailEnabled && !string.IsNullOrEmpty(templates.EmailTemplate) && !string.IsNullOrEmpty(customerDetails.Email))
                {
                    string templateForDesign = templates.EmailTemplate.Replace("[Calendar]", "").Replace("{Calendar}", "");
                    string templateContent = ReplacePlaceholders(templateForDesign, customerDetails, appointment, prependCalendarIfMissing: false);
                    string emailBody = BuildStatusEmailHtml(newStatusName, customerDetails, appointment, templateContent);
                    string emailSubject = GetEmailSubject(newStatusName, customerDetails.ServiceName);
                    // CC: from Settings (comma-separated) + resource email for Confirmed, Dispatched, FA-ID, In-Route/Arrived
                    var ccList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(templates.EmailCC))
                    {
                        foreach (var e in templates.EmailCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = e.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains("@")) ccList.Add(trimmed);
                        }
                        ccList = ccList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    }
                    bool addResourceToCC = newStatusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.Equals("FA-ID", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.IndexOf("FA-ID", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        newStatusName.Equals("In-Route", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.Equals("Progress", StringComparison.OrdinalIgnoreCase) ||
                        newStatusName.Equals("Arrived", StringComparison.OrdinalIgnoreCase);
                    if (addResourceToCC && appointment.ResourceID > 0)
                    {
                        string resourceEmail = GetResourceEmailForAppointment(appointment.ResourceID);
                        if (!string.IsNullOrEmpty(resourceEmail) && !ccList.Any(x => x.Equals(resourceEmail, StringComparison.OrdinalIgnoreCase)))
                            ccList.Add(resourceEmail);
                    }
                    string cc = ccList.Count > 0 ? string.Join(", ", ccList) : "";
                    string bcc = "";
                    if (!string.IsNullOrWhiteSpace(templates.EmailBCC))
                    {
                        var bccList = new List<string>();
                        foreach (var e in templates.EmailBCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = e.Trim();
                            if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains("@")) bccList.Add(trimmed);
                        }
                        bcc = string.Join(", ", bccList.Distinct(StringComparer.OrdinalIgnoreCase));
                    }
                    bool emailSent = SendEmailToCustomer(customerDetails.Email, emailSubject, emailBody, appointment, cc, bcc);
                    if (emailSent)
                    {
                        ConsoleLog($"Email sent to customer {customerDetails.Email} for status {newStatusName}");
                    }
                    else
                    {
                        ConsoleLog($"Email FAILED to send to customer {customerDetails.Email} for status {newStatusName} - Check SMTP configuration");
                    }
                }
                else
                {
                    ConsoleLog($"Email not sent - Enabled: {templates.EmailEnabled}, Template exists: {!string.IsNullOrEmpty(templates.EmailTemplate)}, Email: {!string.IsNullOrEmpty(customerDetails.Email)}");
                }
                
                // Send SMS using existing SendAppointmentSMS method which uses tbl_FSMSMSSettings
                if (templates.SMSEnabled)
                {
                    var twilioService = new TwilioSMSService();
                    twilioService.SendAppointmentSMS(
                        appointmentId.ToString(),
                        appointment.CustomerID,
                        statusCode,
                        appointment.CompanyID,
                        appointment.CompanyName,
                        appointment.RequestDate,
                        appointment.TimeSlot,
                        resourceId.HasValue ? resourceId.Value : appointment.ResourceID
                    );
                    ConsoleLog($"SMS communication processed for ApptID={appointmentId}, Status={newStatusName}({statusCode})");
                }

                // For Scheduled/Confirmed status (status 2), also send notification to resource using Field Agent Content Profile
                if (newStatusId == 2 || newStatusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || 
                    newStatusName.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    int techResourceId = resourceId.HasValue ? resourceId.Value : appointment.ResourceID;
                    if (techResourceId > 0)
                    {
                        SendTechNotification(appointment, techResourceId);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR in ProcessStatusChange: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get basic appointment details needed for communication
        /// </summary>
        private AppointmentBasicDetails GetAppointmentBasicDetails(int appointmentId)
        {
            ConsoleLog($"Getting appointment basic details for ApptID={appointmentId}");

            try
            {
                string sql = @"
                    SELECT 
                        apt.ApptID, apt.CustomerID, apt.CompanyID, 
                        COALESCE(CONVERT(VARCHAR(10), apt.ApptDateTime, 120), CONVERT(VARCHAR(10), apt.StartDateTime, 120), '') AS RequestDate,
                        apt.TimeSlot, apt.Status, apt.ResourceID, apt.Note,
                        c.CompanyName,
                        apt.ApptDateTime AS ApptDateTimeRaw,
                        apt.StartDateTime AS StartDateTimeRaw
                    FROM tbl_Appointment apt WITH (NOLOCK)
                    LEFT JOIN tbl_Company c WITH (NOLOCK) ON apt.CompanyID = c.CompanyID
                    WHERE apt.ApptID = @ApptID AND apt.CompanyID = @CompanyID";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 10;
                    cmd.Parameters.AddWithValue("@ApptID", appointmentId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);

                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            DateTime? apptDt = null;
                            DateTime? startDt = null;
                            if (dr["ApptDateTimeRaw"] != DBNull.Value && dr["ApptDateTimeRaw"] != null)
                                try { apptDt = Convert.ToDateTime(dr["ApptDateTimeRaw"]); } catch { }
                            if (dr["StartDateTimeRaw"] != DBNull.Value && dr["StartDateTimeRaw"] != null)
                                try { startDt = Convert.ToDateTime(dr["StartDateTimeRaw"]); } catch { }
                            return new AppointmentBasicDetails
                            {
                                AppointmentID = appointmentId,
                                CustomerID = dr["CustomerID"] != DBNull.Value ? dr["CustomerID"].ToString() : "",
                                CompanyID = dr["CompanyID"] != DBNull.Value ? dr["CompanyID"].ToString() : "",
                                RequestDate = dr["RequestDate"] != DBNull.Value ? dr["RequestDate"].ToString() : "",
                                TimeSlot = dr["TimeSlot"] != DBNull.Value ? dr["TimeSlot"].ToString() : "",
                                Status = dr["Status"] != DBNull.Value ? dr["Status"].ToString() : "",
                                ResourceID = dr["ResourceID"] != DBNull.Value ? Convert.ToInt32(dr["ResourceID"]) : 0,
                                CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString() : "",
                                Note = dr["Note"] != DBNull.Value && dr["Note"] != null ? dr["Note"].ToString().Trim() : "",
                                ApptDateTimeUtc = (apptDt ?? startDt)?.ToUniversalTime(),
                                ApptDateTimeLocal = apptDt ?? startDt
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting appointment basic details: {ex.Message}\n{ex.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Send notification to resource/tech using Field Agent Content Profile mobile number
        /// Priority: Field Agent Content Profile (tbl_FaProfile.MobilePhone) -> Fallback to tbl_Resources.Mobile
        /// </summary>
        private void SendTechNotification(AppointmentBasicDetails appointment, int resourceId)
        {
            try
            {
                ConsoleLog($"Sending tech notification for ResourceID={resourceId}");

                string resourceMobile = "";
                string resourceEmail = "";
                string resourceName = "";
                string mobileSource = "";
                string emailSource = "";

                // FIRST: Get resource mobile and email from Field Agent Content Profile (tbl_FaProfile)
                // This is the primary source - matches Resource (e.g., "Saruf") with Field Agent Content Profile (e.g., "Saruf")
                string faProfileSql = @"
                    SELECT TOP 1 p.MobilePhone, p.FaName, r.Name AS ResourceName, r.Email AS ResourceEmail
                    FROM tbl_FaProfile p WITH (NOLOCK)
                    LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) 
                        ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                    WHERE p.ResourceID = @ResourceID 
                      AND p.CompanyID = @CompanyID 
                      AND p.IsActive = 1
                    ORDER BY p.ProfileID DESC";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(faProfileSql, conn))
                {
                    cmd.CommandTimeout = 5;
                    cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);

                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            resourceMobile = dr["MobilePhone"] != DBNull.Value ? dr["MobilePhone"].ToString().Trim() : "";
                            resourceEmail = dr["ResourceEmail"] != DBNull.Value ? dr["ResourceEmail"].ToString().Trim() : "";
                            resourceName = dr["FaName"] != DBNull.Value ? dr["FaName"].ToString().Trim() : 
                                         (dr["ResourceName"] != DBNull.Value ? dr["ResourceName"].ToString().Trim() : "");
                            
                            if (!string.IsNullOrEmpty(resourceMobile))
                            {
                                mobileSource = "Field Agent Content Profile";
                                ConsoleLog($"Found mobile from Field Agent Content Profile: {resourceMobile} (FA Name: {resourceName})");
                            }
                            if (!string.IsNullOrEmpty(resourceEmail))
                            {
                                emailSource = "Field Agent Content Profile (from tbl_Resources)";
                                ConsoleLog($"Found email from Field Agent Content Profile: {resourceEmail} (FA Name: {resourceName})");
                            }
                        }
                    }
                }

                // FALLBACK: If no Field Agent Content Profile found or mobile/email is empty, try tbl_Resources
                if (string.IsNullOrEmpty(resourceMobile) || string.IsNullOrEmpty(resourceEmail))
                {
                    ConsoleLog($"Field Agent Content Profile missing mobile or email for ResourceID={resourceId}, trying tbl_Resources as fallback...");
                    string resourceSql = @"
                        SELECT Name, Mobile, Email 
                        FROM [msSchedulerV3].[dbo].[tbl_Resources] WITH (NOLOCK) 
                        WHERE Id = @ResourceID AND CompanyID = @CompanyID";
                    
                    using (var conn = new SqlConnection(connStr))
                    using (var cmd = new SqlCommand(resourceSql, conn))
                    {
                        cmd.CommandTimeout = 5;
                        cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        conn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                if (string.IsNullOrEmpty(resourceMobile))
                                {
                                    resourceMobile = dr["Mobile"] != DBNull.Value ? dr["Mobile"].ToString().Trim() : "";
                                    if (!string.IsNullOrEmpty(resourceMobile))
                                    {
                                        mobileSource = "tbl_Resources (fallback)";
                                        ConsoleLog($"Found mobile from tbl_Resources: {resourceMobile} (Resource Name: {resourceName})");
                                    }
                                }
                                if (string.IsNullOrEmpty(resourceEmail))
                                {
                                    resourceEmail = dr["Email"] != DBNull.Value ? dr["Email"].ToString().Trim() : "";
                                    if (!string.IsNullOrEmpty(resourceEmail))
                                    {
                                        emailSource = "tbl_Resources (fallback)";
                                        ConsoleLog($"Found email from tbl_Resources: {resourceEmail} (Resource Name: {resourceName})");
                                    }
                                }
                                if (string.IsNullOrEmpty(resourceName))
                                {
                                    resourceName = dr["Name"] != DBNull.Value ? dr["Name"].ToString().Trim() : "";
                                }
                            }
                        }
                    }
                }

                // Get customer details for email content
                string customerName = "";
                string serviceName = "";
                GetCustomerDetails(appointment.CustomerID, out customerName, out serviceName);

                // Send Email if email found (same HTML format as customer confirmation)
                if (!string.IsNullOrEmpty(resourceEmail))
                {
                    string emailSubject = $"New Appointment Assignment - {serviceName}";
                    string emailBody = BuildResourceAssignmentEmailHtml(resourceName, customerName, serviceName, appointment, isFaId: false);

                    bool emailSent = SendEmailToResource(resourceEmail, emailSubject, emailBody, appointment, resourceId);
                    if (emailSent)
                    {
                        ConsoleLog($"Tech notification email sent to {resourceEmail} from {emailSource} (Resource: {resourceName})");
                    }
                    else
                    {
                        ConsoleLog($"Tech notification email FAILED to send to {resourceEmail} from {emailSource} (Resource: {resourceName}) - Check SMTP configuration");
                    }
                }
                else
                {
                    ConsoleLog($"WARNING: No email address found for ResourceID={resourceId}. Checked Field Agent Content Profile and tbl_Resources - both returned empty.");
                }

                // Send SMS if mobile number found
                if (!string.IsNullOrEmpty(resourceMobile))
                {
                    string resourceSMS = $"Hi {resourceName}, You have a new appointment at {appointment.RequestDate} {appointment.TimeSlot}. Open the app to see the details.";
                    var twilioService = new TwilioSMSService();
                    string smsId = twilioService.SendSMS(resourceMobile, resourceSMS, companyId);
                    
                    // Log SMS
                    LogSMS(companyId, appointment.CustomerID, resourceId.ToString(), appointment.AppointmentID.ToString(), 
                           resourceMobile, "FSM Assign Resource", resourceSMS, smsId);
                    
                    ConsoleLog($"Tech notification SMS sent to {resourceMobile} from {mobileSource} (SMS ID: {smsId}, Resource: {resourceName})");
                }
                else
                {
                    ConsoleLog($"WARNING: No mobile number found for ResourceID={resourceId}. Checked Field Agent Content Profile (tbl_FaProfile) and tbl_Resources - both returned empty.");
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR sending tech notification: {ex.Message}\n{ex.StackTrace}");
            }
        }
        public void SendMessageToCustomerFrmFaId(int appointmentId, List<int> profileIds)
        {
            if (profileIds == null || profileIds.Count == 0) return;
            var appointment = GetAppointmentBasicDetails(appointmentId);
            if (appointment == null)
            {
                ConsoleLog($"SendFaIdToFieldAgentProfiles: Appointment {appointmentId} not found.");
                return;
            }

            // Load FA-ID template from Settings (Triggered by Status: Field Agent ID)
            var templates = GetFAMessageTemplates();
            var customerDetails = GetCustomerDetailsForTemplates(appointment.CustomerID);
            string customerName = customerDetails.FullName ?? "";
            string serviceName = customerDetails.ServiceName ?? "";
            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);

            // FA-ID: collect field agent profiles, then send email/SMS only to the CUSTOMER (not to the field agents)
            var faProfilesForCustomer = new List<FaProfileForEmail>();

            foreach (int profileId in profileIds)
            {
                try
                {
                    string resourceMobile = "";
                    string resourceEmail = "";
                    string resourceName = "";
                    int resourceId = 0;
                    string pictureUrl = "";
                    string ImageUrl = "";
                    string customContent = "";

                    string faProfileSql = @"
                        SELECT TOP 1 p.ProfileID,p.ImageUrl, p.ResourceID, p.MobilePhone, p.FaName, ISNULL(p.PictureUrl,'') AS PictureUrl, ISNULL(p.CustomContent,'') AS CustomContent, r.Name AS ResourceName, r.Email AS ResourceEmail
                        FROM tbl_FaProfile p WITH (NOLOCK)
                        LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                        WHERE p.ProfileID = @ProfileID AND p.CompanyID = @CompanyID AND p.IsActive = 1";
                    string faProfileSqlMinimal = @"
                        SELECT TOP 1 p.ProfileID, p.ResourceID,p.ImageUrl, p.MobilePhone, p.FaName, r.Name AS ResourceName, r.Email AS ResourceEmail
                        FROM tbl_FaProfile p WITH (NOLOCK)
                        LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                        WHERE p.ProfileID = @ProfileID AND p.CompanyID = @CompanyID AND p.IsActive = 1";
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        foreach (string sql in new[] { faProfileSql, faProfileSqlMinimal })
                        {
                            try
                            {
                                using (var cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 50;
                                    cmd.Parameters.AddWithValue("@ProfileID", profileId);
                                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        if (dr.Read())
                                        {
                                            resourceId = dr["ResourceID"] != DBNull.Value ? Convert.ToInt32(dr["ResourceID"]) : 0;
                                            resourceMobile = dr["MobilePhone"] != DBNull.Value ? dr["MobilePhone"].ToString().Trim() : "";
                                            resourceName = dr["FaName"] != DBNull.Value ? dr["FaName"].ToString().Trim() : (dr["ResourceName"] != DBNull.Value ? dr["ResourceName"].ToString().Trim() : "");
                                            resourceEmail = dr["ResourceEmail"] != DBNull.Value ? dr["ResourceEmail"].ToString().Trim() : "";
                                            try { pictureUrl = dr["PictureUrl"] != DBNull.Value && dr["PictureUrl"] != null ? dr["PictureUrl"].ToString().Trim() : ""; } catch { }
                                            try { ImageUrl = dr["ImageUrl"] != DBNull.Value && dr["ImageUrl"] != null ? dr["ImageUrl"].ToString().Trim() : ""; } catch { }
                                            try { customContent = dr["CustomContent"] != DBNull.Value && dr["CustomContent"] != null ? dr["CustomContent"].ToString().Trim() : ""; } catch { }
                                            if (!string.IsNullOrEmpty(resourceName))
                                                faProfilesForCustomer.Add(new FaProfileForEmail
                                                {
                                                    ProfileID = profileId,
                                                    FaName = resourceName,
                                                    ImageUrl = ImageUrl,

                                                    PictureUrl = pictureUrl ?? "",
                                                    CustomContent = customContent ?? "",
                                                    MobilePhone = resourceMobile ?? "",
                                                    Email = resourceEmail ?? ""
                                                });
                                        }
                                    }
                                }

                                string customerSmsBody = templates.standardContent;
                                if (templates != null && !string.IsNullOrWhiteSpace(templates.standardContent))
                                    customerSmsBody = ReplaceFaIdSmsPlaceholders(templates.standardContent.Trim(), customerDetails, appointment, faProfilesForCustomer);

                                // Replace TEXT
                                string trackUrl = (ConfigurationManager.AppSettings["FAIDTrackingUrl"] ?? "").Trim();
                                string whatToExpectUrl = (ConfigurationManager.AppSettings["FAIDWhatToExpectUrl"] ??  "").Trim();
                                string body = customerSmsBody
                                   .Replace("[FA Name]", resourceName)
                                   .Replace("[Company Name]", companyDetails.CompanyName)
                                   .Replace("[Address]", customerDetails.Address)
                                   .Replace("[FA Custom Content]", customContent)
                                   .Replace("[FA Phone]", resourceMobile)
                                   .Replace("[FA Email]", resourceEmail)
                                   .Replace("[FA Picture URL]", ImageUrl)
                                   .Replace("[What to Expect URL]", whatToExpectUrl)
                                   .Replace("[Track URL]", trackUrl)
                                   .Replace("[Service Name]", serviceName);
                                body += customContent
                                   .Replace("[FA Name]", resourceName)
                                   .Replace("[Company Name]", companyDetails.CompanyName)
                                   .Replace("[Address]", customerDetails.Address)
                                   .Replace("[FA Custom Content]", customContent)
                                   .Replace("[FA Phone]", resourceMobile)
                                   .Replace("[FA Email]", resourceEmail)
                                   .Replace("[FA Picture URL]", ImageUrl)
                                   .Replace("[What to Expect URL]", whatToExpectUrl)
                                   .Replace("[Track URL]", trackUrl)
                                   .Replace("[Service Name]", serviceName);
                                if (templates.EnableSms)
                                {
                                    try
                                    {
                                        var twilioService = new TwilioSMSService();
                                        string smsId;

                                        if (templates.enableMms)
                                        {
                                            ImageUrl = ConfigurationManager.AppSettings["baseurl"].Trim() + ImageUrl;
                                            if (!String.IsNullOrEmpty(ImageUrl))
                                            {
                                                smsId = twilioService.SendMMS(customerDetails.Mobile, body, ImageUrl, companyId);
                                                LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerDetails.Mobile, "FSM FA-ID Customer MMS", customerSmsBody, smsId ?? "");
                                            }
                                        }
                                          else
                                        {

                                                smsId = twilioService.SendSMS(customerDetails.Mobile, body,  companyId);
                                                LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerDetails.Mobile, "FSM FA-ID Customer MMS", customerSmsBody, smsId ?? "");
                                          
                                        }
                                         
                                          
                                      
                                    }
                                    catch (Exception ex)
                                    {
                                        ConsoleLog($"FA-ID customer SMS/MMS FAILED: {ex.Message}");
                                    }
                                }

                                    break;
                            }
                            catch (SqlException)
                            {
                                pictureUrl = "";
                                customContent = "";
                                continue;
                            }
                        }
                    }

                    //if (resourceId > 0 && (string.IsNullOrEmpty(resourceEmail) || string.IsNullOrEmpty(resourceMobile)))
                    //{
                    //    string resourceSql = "SELECT Email, Mobile, Name FROM [msSchedulerV3].[dbo].[tbl_Resources] WITH (NOLOCK) WHERE Id = @ResourceID AND CompanyID = @CompanyID";
                    //    using (var conn = new SqlConnection(connStr))
                    //    using (var cmd = new SqlCommand(resourceSql, conn))
                    //    {
                    //        cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                    //        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    //        conn.Open();
                    //        using (var dr = cmd.ExecuteReader())
                    //        {
                    //            if (dr.Read())
                    //            {
                    //                if (string.IsNullOrEmpty(resourceEmail)) resourceEmail = dr["Email"] != DBNull.Value ? dr["Email"].ToString().Trim() : "";
                    //                if (string.IsNullOrEmpty(resourceMobile)) resourceMobile = dr["Mobile"] != DBNull.Value ? dr["Mobile"].ToString().Trim() : "";
                    //                if (string.IsNullOrEmpty(resourceName)) resourceName = dr["Name"] != DBNull.Value ? dr["Name"].ToString().Trim() : "";
                    //                if (!string.IsNullOrEmpty(resourceName) && !faProfilesForCustomer.Any(f => f.FaName.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
                    //                    faProfilesForCustomer.Add(new FaProfileForEmail
                    //                    {
                    //                        ProfileID = profileId,
                    //                        FaName = resourceName,
                    //                        PictureUrl = pictureUrl ?? "",
                    //                        CustomContent = customContent ?? "",
                    //                        MobilePhone = resourceMobile ?? "",
                    //                        Email = resourceEmail ?? ""
                    //                    });
                    //            }
                    //        }
                    //    }
                    //}

                    ConsoleLog($"FA-ID: collected profile for {resourceName} (ProfileID={profileId}) – will send to customer only.");
                }
                catch (Exception ex)
                {
                    ConsoleLog($"ERROR collecting FA profile ProfileID={profileId}: {ex.Message}");
                }
            }

            // Send email and SMS/MMS to CUSTOMER only (appointment owner) – explicit lookup so we never send to the field agent by mistake
            GetAppointmentCustomerContact(appointment.CustomerID, companyId, out string customerEmailForFaId, out string customerMobileForFaId);

            //if (faProfilesForCustomer.Count > 0 && !string.IsNullOrEmpty(customerEmailForFaId))
            //{
            //    string customerEmailBody = BuildFaIdCustomerEmailHtml(customerDetails, appointment, faProfilesForCustomer);
            //    string faNamesCsv = string.Join(", ", faProfilesForCustomer.Select(f => f.FaName).Where(n => !string.IsNullOrEmpty(n)));
            //    string customerSubject = $"Your Field Agent: {faNamesCsv} - {serviceName}";
            //    string cc = !string.IsNullOrWhiteSpace(templates.EmailCC) ? string.Join(", ", templates.EmailCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e) && e.Contains("@")).Distinct(StringComparer.OrdinalIgnoreCase)) : "";
            //    string bcc = !string.IsNullOrWhiteSpace(templates.EmailBCC) ? string.Join(", ", templates.EmailBCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e) && e.Contains("@")).Distinct(StringComparer.OrdinalIgnoreCase)) : "";
            //    bool customerEmailSent = SendEmailToCustomer(customerEmailForFaId, customerSubject, customerEmailBody, appointment, cc, bcc);
            //    if (customerEmailSent)
            //        ConsoleLog($"FA-ID customer email sent to {customerEmailForFaId} with field agent(s): {faNamesCsv}");
            //    else
            //        ConsoleLog($"FA-ID customer email FAILED to send to {customerEmailForFaId}");
            //}
            //else if (faProfilesForCustomer.Count > 0)
            //{
            //    ConsoleLog($"FA-ID customer email skipped: no customer email on file for appointment owner (CustomerID={appointment.CustomerID}).");
            //}

            //if(templates.EnableSms)
            //{
            //    string customerMobile = !string.IsNullOrWhiteSpace(customerMobileForFaId) ? customerMobileForFaId.Trim() : "";
            //    if (faProfilesForCustomer.Count > 0 && !string.IsNullOrEmpty(customerMobile))
            //    {
            //       // BuildFaIdCustomerSmsBody(customerDetails, appointment, faProfilesForCustomer, out string customerSmsBody, out string firstFaPictureUrlForMms);
            //        // If Settings has an FA-ID SMS template, use it (placeholders replaced); otherwise use the built body above.
            //      string   customerSmsBody = templates.standardContent;
            //        if (templates != null && !string.IsNullOrWhiteSpace(templates.standardContent))
            //            customerSmsBody = ReplaceFaIdSmsPlaceholders(templates.standardContent.Trim(), customerDetails, appointment, faProfilesForCustomer);

                  

            //        if (!string.IsNullOrEmpty(customerSmsBody))
            //        {
            //            try
            //            {
            //                var twilioService = new TwilioSMSService();
            //                string smsId;
            //                if (!string.IsNullOrEmpty(firstFaPictureUrlForMms))
            //                {
            //                    smsId = twilioService.SendMMS(customerMobile, customerSmsBody, firstFaPictureUrlForMms, companyId);
            //                    LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerMobile, "FSM FA-ID Customer MMS", customerSmsBody, smsId ?? "");
            //                    if (!string.IsNullOrEmpty(smsId))
            //                        ConsoleLog($"FA-ID customer MMS sent to {customerMobile} with field agent photo (SID={smsId})");
            //                }
            //                else
            //                {
            //                    smsId = twilioService.SendSMS(customerMobile, customerSmsBody, companyId);
            //                    LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerMobile, "FSM FA-ID Customer", customerSmsBody, smsId ?? "");
            //                    if (!string.IsNullOrEmpty(smsId))
            //                        ConsoleLog($"FA-ID customer SMS sent to {customerMobile} (SID={smsId})");
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                ConsoleLog($"FA-ID customer SMS/MMS FAILED: {ex.Message}");
            //            }
            //        }
            //    }
            //}
            // Send SMS/MMS to CUSTOMER only (appointment owner)
           
        }

        /// <summary>
        /// Send FA-ID (SMS + email) to selected Field Agent profiles using Settings → Field Agent ID templates.
        /// Used when user checks one or more Field Agents in the FA-ID Sent modal and clicks Send.
        /// </summary>
        public void SendFaIdToFieldAgentProfiles(int appointmentId, List<int> profileIds)
        {
            if (profileIds == null || profileIds.Count == 0) return;
            var appointment = GetAppointmentBasicDetails(appointmentId);
            if (appointment == null)
            {
                ConsoleLog($"SendFaIdToFieldAgentProfiles: Appointment {appointmentId} not found.");
                return;
            }

            // Load FA-ID template from Settings (Triggered by Status: Field Agent ID)
            var templates = GetMessageTemplates("FA-ID", "");
            var customerDetails = GetCustomerDetailsForTemplates(appointment.CustomerID);
            string customerName = customerDetails.FullName ?? "";
            string serviceName = customerDetails.ServiceName ?? "";

            // FA-ID: collect field agent profiles, then send email/SMS only to the CUSTOMER (not to the field agents)
            var faProfilesForCustomer = new List<FaProfileForEmail>();

            foreach (int profileId in profileIds)
            {
                try
                {
                    string resourceMobile = "";
                    string resourceEmail = "";
                    string resourceName = "";
                    int resourceId = 0;
                    string pictureUrl = "";
                    string customContent = "";

                    string faProfileSql = @"
                        SELECT TOP 1 p.ProfileID, p.ResourceID, p.MobilePhone, p.FaName, ISNULL(p.PictureUrl,'') AS PictureUrl, ISNULL(p.CustomContent,'') AS CustomContent, r.Name AS ResourceName, r.Email AS ResourceEmail
                        FROM tbl_FaProfile p WITH (NOLOCK)
                        LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                        WHERE p.ProfileID = @ProfileID AND p.CompanyID = @CompanyID AND p.IsActive = 1";
                    string faProfileSqlMinimal = @"
                        SELECT TOP 1 p.ProfileID, p.ResourceID, p.MobilePhone, p.FaName, r.Name AS ResourceName, r.Email AS ResourceEmail
                        FROM tbl_FaProfile p WITH (NOLOCK)
                        LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                        WHERE p.ProfileID = @ProfileID AND p.CompanyID = @CompanyID AND p.IsActive = 1";
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        foreach (string sql in new[] { faProfileSql, faProfileSqlMinimal })
                        {
                            try
                            {
                                using (var cmd = new SqlCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 5;
                                    cmd.Parameters.AddWithValue("@ProfileID", profileId);
                                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        if (dr.Read())
                                        {
                                            resourceId = dr["ResourceID"] != DBNull.Value ? Convert.ToInt32(dr["ResourceID"]) : 0;
                                            resourceMobile = dr["MobilePhone"] != DBNull.Value ? dr["MobilePhone"].ToString().Trim() : "";
                                            resourceName = dr["FaName"] != DBNull.Value ? dr["FaName"].ToString().Trim() : (dr["ResourceName"] != DBNull.Value ? dr["ResourceName"].ToString().Trim() : "");
                                            resourceEmail = dr["ResourceEmail"] != DBNull.Value ? dr["ResourceEmail"].ToString().Trim() : "";
                                            try { pictureUrl = dr["PictureUrl"] != DBNull.Value && dr["PictureUrl"] != null ? dr["PictureUrl"].ToString().Trim() : ""; } catch { }
                                            try { customContent = dr["CustomContent"] != DBNull.Value && dr["CustomContent"] != null ? dr["CustomContent"].ToString().Trim() : ""; } catch { }
                                            if (!string.IsNullOrEmpty(resourceName))
                                                faProfilesForCustomer.Add(new FaProfileForEmail
                                                {
                                                    ProfileID = profileId,
                                                    FaName = resourceName,
                                                    PictureUrl = pictureUrl ?? "",
                                                    CustomContent = customContent ?? "",
                                                    MobilePhone = resourceMobile ?? "",
                                                    Email = resourceEmail ?? ""
                                                });
                                        }
                                    }
                                }
                                break;
                            }
                            catch (SqlException)
                            {
                                pictureUrl = "";
                                customContent = "";
                                continue;
                            }
                        }
                    }

                    if (resourceId > 0 && (string.IsNullOrEmpty(resourceEmail) || string.IsNullOrEmpty(resourceMobile)))
                    {
                        string resourceSql = "SELECT Email, Mobile, Name FROM [msSchedulerV3].[dbo].[tbl_Resources] WITH (NOLOCK) WHERE Id = @ResourceID AND CompanyID = @CompanyID";
                        using (var conn = new SqlConnection(connStr))
                        using (var cmd = new SqlCommand(resourceSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                            cmd.Parameters.AddWithValue("@CompanyID", companyId);
                            conn.Open();
                            using (var dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    if (string.IsNullOrEmpty(resourceEmail)) resourceEmail = dr["Email"] != DBNull.Value ? dr["Email"].ToString().Trim() : "";
                                    if (string.IsNullOrEmpty(resourceMobile)) resourceMobile = dr["Mobile"] != DBNull.Value ? dr["Mobile"].ToString().Trim() : "";
                                    if (string.IsNullOrEmpty(resourceName)) resourceName = dr["Name"] != DBNull.Value ? dr["Name"].ToString().Trim() : "";
                                    if (!string.IsNullOrEmpty(resourceName) && !faProfilesForCustomer.Any(f => f.FaName.Equals(resourceName, StringComparison.OrdinalIgnoreCase)))
                                        faProfilesForCustomer.Add(new FaProfileForEmail
                                        {
                                            ProfileID = profileId,
                                            FaName = resourceName,
                                            PictureUrl = pictureUrl ?? "",
                                            CustomContent = customContent ?? "",
                                            MobilePhone = resourceMobile ?? "",
                                            Email = resourceEmail ?? ""
                                        });
                                }
                            }
                        }
                    }

                    ConsoleLog($"FA-ID: collected profile for {resourceName} (ProfileID={profileId}) – will send to customer only.");
                }
                catch (Exception ex)
                {
                    ConsoleLog($"ERROR collecting FA profile ProfileID={profileId}: {ex.Message}");
                }
            }

            // Send email and SMS/MMS to CUSTOMER only (appointment owner) – explicit lookup so we never send to the field agent by mistake
            GetAppointmentCustomerContact(appointment.CustomerID, companyId, out string customerEmailForFaId, out string customerMobileForFaId);

            if (faProfilesForCustomer.Count > 0 && !string.IsNullOrEmpty(customerEmailForFaId))
            {
                string customerEmailBody = BuildFaIdCustomerEmailHtml(customerDetails, appointment, faProfilesForCustomer);
                string faNamesCsv = string.Join(", ", faProfilesForCustomer.Select(f => f.FaName).Where(n => !string.IsNullOrEmpty(n)));
                string customerSubject = $"Your Field Agent: {faNamesCsv} - {serviceName}";
                string cc = !string.IsNullOrWhiteSpace(templates.EmailCC) ? string.Join(", ", templates.EmailCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e) && e.Contains("@")).Distinct(StringComparer.OrdinalIgnoreCase)) : "";
                string bcc = !string.IsNullOrWhiteSpace(templates.EmailBCC) ? string.Join(", ", templates.EmailBCC.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e) && e.Contains("@")).Distinct(StringComparer.OrdinalIgnoreCase)) : "";
                bool customerEmailSent = SendEmailToCustomer(customerEmailForFaId, customerSubject, customerEmailBody, appointment, cc, bcc);
                if (customerEmailSent)
                    ConsoleLog($"FA-ID customer email sent to {customerEmailForFaId} with field agent(s): {faNamesCsv}");
                else
                    ConsoleLog($"FA-ID customer email FAILED to send to {customerEmailForFaId}");
            }
            else if (faProfilesForCustomer.Count > 0)
            {
                ConsoleLog($"FA-ID customer email skipped: no customer email on file for appointment owner (CustomerID={appointment.CustomerID}).");
            }

            // Send SMS/MMS to CUSTOMER only (appointment owner)
            string customerMobile = !string.IsNullOrWhiteSpace(customerMobileForFaId) ? customerMobileForFaId.Trim() : "";
            if (faProfilesForCustomer.Count > 0 && !string.IsNullOrEmpty(customerMobile))
            {
                BuildFaIdCustomerSmsBody(customerDetails, appointment, faProfilesForCustomer, out string customerSmsBody, out string firstFaPictureUrlForMms);
                // If Settings has an FA-ID SMS template, use it (placeholders replaced); otherwise use the built body above.
                if (templates != null && !string.IsNullOrWhiteSpace(templates.SMSTemplate))
                    customerSmsBody = ReplaceFaIdSmsPlaceholders(templates.SMSTemplate.Trim(), customerDetails, appointment, faProfilesForCustomer);
                if (!string.IsNullOrEmpty(customerSmsBody))
                {
                    try
                    {
                        var twilioService = new TwilioSMSService();
                        string smsId;
                        if (!string.IsNullOrEmpty(firstFaPictureUrlForMms))
                        {
                            smsId = twilioService.SendMMS(customerMobile, customerSmsBody, firstFaPictureUrlForMms, companyId);
                            LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerMobile, "FSM FA-ID Customer MMS", customerSmsBody, smsId ?? "");
                            if (!string.IsNullOrEmpty(smsId))
                                ConsoleLog($"FA-ID customer MMS sent to {customerMobile} with field agent photo (SID={smsId})");
                        }
                        else
                        {
                            smsId = twilioService.SendSMS(customerMobile, customerSmsBody, companyId);
                            LogSMS(companyId, appointment.CustomerID, "", appointment.AppointmentID.ToString(), customerMobile, "FSM FA-ID Customer", customerSmsBody, smsId ?? "");
                            if (!string.IsNullOrEmpty(smsId))
                                ConsoleLog($"FA-ID customer SMS sent to {customerMobile} (SID={smsId})");
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleLog($"FA-ID customer SMS/MMS FAILED: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Send email to resource/tech
        /// Returns true if email was sent successfully, false otherwise
        /// </summary>
        private bool SendEmailToResource(string toEmail, string subject, string body, AppointmentBasicDetails appointment, int resourceId)
        {
            try
            {
                var emailProcessor = new EmailProcessor();
                string result = emailProcessor.SendHtmlFormattedEmail(
                    appointment.CompanyID,
                    "", // No customer ID for resource emails
                    $"Tech Assignment: {appointment.Status}",
                    subject,
                    body,
                    toEmail,
                    "",
                    "",
                    new List<EmailContent>()
                );
                
                // Check if email was sent successfully (returns "Sent" on success, error message on failure)
                if (result == "Sent")
                {
                    ConsoleLog($"Email sent successfully to resource {toEmail}");
                    return true;
                }
                else
                {
                    ConsoleLog($"ERROR sending email to resource {toEmail}: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR sending email to resource: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get customer details for email content
        /// </summary>
        private void GetCustomerDetails(string customerId, out string customerName, out string serviceName)
        {
            customerName = "";
            serviceName = "";

            try
            {
                string sql = @"
                    SELECT TOP 1 
                        ISNULL(c.FirstName, '') + ' ' + ISNULL(c.LastName, '') AS CustomerName,
                        ISNULL(srv.ServiceName, '') AS ServiceName
                    FROM tbl_Customer c WITH (NOLOCK)
                    LEFT JOIN tbl_Appointment apt WITH (NOLOCK) ON c.CustomerID = apt.CustomerID AND c.CompanyID = apt.CompanyID
                    LEFT JOIN tbl_ServiceType srv WITH (NOLOCK) ON apt.CompanyID = srv.CompanyID AND 
                        (TRY_CAST(apt.ServiceType AS INT) = srv.ServiceTypeID OR apt.ServiceType = srv.ServiceName)
                    WHERE c.CustomerID = @CustomerID AND c.CompanyID = @CompanyID";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 5;
                    cmd.Parameters.AddWithValue("@CustomerID", customerId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            customerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString().Trim() : "";
                            serviceName = dr["ServiceName"] != DBNull.Value ? dr["ServiceName"].ToString().Trim() : "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting customer details: {ex.Message}");
            }
        }
        private MasterTemplate GetFAMessageTemplates()
        {
            try
            {
              

             

                string sql = @"SELECT  [TemplateID]
                              ,[CompanyID]
                              ,[EnableSms]
                              ,[EnableMms]
                              ,[StandardContent]
                              ,[LastModifiedDate]
                              ,[DaysBeforeAppointment]
                              ,[EnableEmail]
                          FROM[msSchedulerV3].[dbo].[tbl_FaMessageMasterTemplate] where CompanyID=@CompanyID;";

                //sql += @"SELECT [ProfileID]
                //          ,[CompanyID]
                //          ,[ResourceID]
                //          ,[FaName]
                //          ,[MobilePhone]
                //          ,[CustomContent]
                //          ,[PictureUrl]
                //          ,[IsActive]
                //      FROM [msSchedulerV3].[dbo].[tbl_FaProfile] where where CompanyID=@CompanyID and ResourceID = @ResourceID";


                Database db = new Database(connStr);

                db.Command.Parameters.Clear();
              //  db.Command.Parameters.AddWithValue("@ResourceID", ResourceID);

                 DataSet dataSet =  db.Get_DataSet(sql, companyId);

                if (dataSet.Tables[0].Rows.Count > 0)
                {
                    DataRow row = dataSet.Tables[0].Rows[0];
                    return new MasterTemplate
                    {
                     
                            EnableSms = row["EnableSms"] != DBNull.Value && Convert.ToBoolean(row["EnableSms"]),
                            enableMms = row["EnableMms"] != DBNull.Value && Convert.ToBoolean(row["EnableMms"]),
                            enableEmail = row.Table.Columns.Contains("EnableEmail") && row["EnableEmail"] != DBNull.Value ? Convert.ToBoolean(row["EnableEmail"]) : false,
                            standardContent = row["StandardContent"]?.ToString() ?? "",
                            daysBeforeAppointment = row["DaysBeforeAppointment"] != DBNull.Value ? Convert.ToInt32(row["DaysBeforeAppointment"]) : 0
                       
                    };
                }


            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting message templates: {ex.Message}");
            }

            return new MasterTemplate { EnableSms = false, enableMms = false, enableEmail = false, standardContent = "" };
        }
        /// <summary>
        /// Get message templates (email and SMS) from database based on status
        /// </summary>
        private MessageTemplates GetMessageTemplates(string statusName, string statusCode)
        {
            try
            {
                // Map status name to column names
                string smsColumn = "";
                string emailColumn = "";
                string emailCCColumn = "";
                string emailBCCColumn = "";
                string ynColumn = "";
                
                if (statusName.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    smsColumn = "PendingText";
                    emailColumn = "PendingEmailTemplate";
                    emailCCColumn = "PendingEmailCC";
                    emailBCCColumn = "PendingEmailBCC";
                    ynColumn = "PendingYN";
                }
                else if (statusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || 
                         statusName.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) || statusCode == "2")
                {
                    smsColumn = "ScheduledText";
                    emailColumn = "ScheduledEmailTemplate";
                    emailCCColumn = "ScheduledEmailCC";
                    emailBCCColumn = "ScheduledEmailBCC";
                    ynColumn = "ScheduledYN";
                }
                else if (statusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) || statusCode == "4")
                {
                    smsColumn = "ClosedText";
                    emailColumn = "ClosedEmailTemplate";
                    emailCCColumn = "ClosedEmailCC";
                    emailBCCColumn = "ClosedEmailBCC";
                    ynColumn = "ClosedYN";
                }
                else if (statusName.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || statusCode == "3")
                {
                    smsColumn = "CancelledText";
                    emailColumn = "CancelledEmailTemplate";
                    emailCCColumn = "CancelledEmailCC";
                    emailBCCColumn = "CancelledEmailBCC";
                    ynColumn = "CancelledYN";
                }
                else if (statusName.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) && statusCode == "1")
                {
                    smsColumn = "DispatchedText";
                    emailColumn = "DispatchedEmailBody";
                    emailCCColumn = "DispatchedEmailCC";
                    emailBCCColumn = "DispatchedEmailBCC";
                    ynColumn = "DispatchedYN";
                }
                else if (statusName.Equals("FA-ID", StringComparison.OrdinalIgnoreCase) || 
                         statusName.Equals("FA-ID Sent", StringComparison.OrdinalIgnoreCase) ||
                         statusName.IndexOf("FA-ID", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    smsColumn = "FAIDSentText";
                    emailColumn = "FAIDSentEmailBody";
                    emailCCColumn = "FAIDSentEmailCC";
                    emailBCCColumn = "FAIDSentEmailBCC";
                    ynColumn = "FAIDSentYN";
                }
                else if (statusName.Equals("Arrived", StringComparison.OrdinalIgnoreCase) ||
                         statusName.IndexOf("Arrived", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Arrived: use ArrivedEmailBody / ArrivedYN when columns exist; else fall back to In-Route
                    smsColumn = "ArrivedText";
                    emailColumn = "ArrivedEmailBody";
                    emailCCColumn = "ArrivedEmailCC";
                    emailBCCColumn = "ArrivedEmailBCC";
                    ynColumn = "ArrivedYN";
                }
                else if (statusName.Equals("In-Route", StringComparison.OrdinalIgnoreCase) || 
                         statusName.Equals("Progress", StringComparison.OrdinalIgnoreCase) || statusCode == "5")
                {
                    smsColumn = "InRouteText";
                    emailColumn = "InRouteEmailBody";
                    emailCCColumn = "InRouteEmailCC";
                    emailBCCColumn = "InRouteEmailBCC";
                    ynColumn = "InRouteYN";
                }
                else if (statusName.Equals("Completed", StringComparison.OrdinalIgnoreCase) || statusCode == "6")
                {
                    smsColumn = "CompletedText";
                    emailColumn = "CompletedEmailBody";
                    emailCCColumn = "CompletedEmailCC";
                    emailBCCColumn = "CompletedEmailBCC";
                    ynColumn = "CompletedYN";
                }
                else
                {
                    return new MessageTemplates { EmailEnabled = false, SMSEnabled = false };
                }

                string sql = $"SELECT {smsColumn}, {ynColumn}";
                bool emailColumnExists = false;
                bool emailEnabledColExists = false;
                // Dispatched/FA-ID/In-Route/Completed use *EmailBody + *EmailYN; others use *EmailTemplate + *EmailEnabled
                string emailEnabledColumn = !string.IsNullOrEmpty(emailColumn) && emailColumn.Contains("Body") 
                    ? emailColumn.Replace("Body", "YN") 
                    : emailColumn.Replace("Template", "Enabled");

                // Check if email column and emailEnabled column exist
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string checkColumnSql = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'tbl_FSMSMSSettings' 
                          AND COLUMN_NAME = @EmailColumn";
                    using (var checkCmd = new SqlCommand(checkColumnSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@EmailColumn", emailColumn);
                        emailColumnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    using (var checkEnabledCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'tbl_FSMSMSSettings' AND COLUMN_NAME = @Col", conn))
                    {
                        checkEnabledCmd.Parameters.AddWithValue("@Col", emailEnabledColumn);
                        emailEnabledColExists = Convert.ToInt32(checkEnabledCmd.ExecuteScalar()) > 0;
                    }

                    bool ccColExists = false;
                    bool bccColExists = false;
                    if (!string.IsNullOrEmpty(emailCCColumn))
                    {
                        using (var ck = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tbl_FSMSMSSettings' AND COLUMN_NAME=@Col", conn))
                        {
                            ck.Parameters.AddWithValue("@Col", emailCCColumn);
                            ccColExists = Convert.ToInt32(ck.ExecuteScalar()) > 0;
                        }
                    }
                    if (!string.IsNullOrEmpty(emailBCCColumn))
                    {
                        using (var ck = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tbl_FSMSMSSettings' AND COLUMN_NAME=@Col", conn))
                        {
                            ck.Parameters.AddWithValue("@Col", emailBCCColumn);
                            bccColExists = Convert.ToInt32(ck.ExecuteScalar()) > 0;
                        }
                    }

                    if (emailColumnExists) sql += $", {emailColumn}";
                    if (emailEnabledColExists) sql += $", {emailEnabledColumn}";
                    if (ccColExists && !string.IsNullOrEmpty(emailCCColumn)) sql += $", {emailCCColumn}";
                    if (bccColExists && !string.IsNullOrEmpty(emailBCCColumn)) sql += $", {emailBCCColumn}";

                    sql += $" FROM tbl_FSMSMSSettings WHERE CompanyId = @CompanyId";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompanyId", companyId);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string smsTemplate = dr[smsColumn] != DBNull.Value ? dr[smsColumn].ToString() : "";
                                string emailTemplate = "";
                                if (emailColumnExists)
                                {
                                    try
                                    {
                                        emailTemplate = dr[emailColumn] != DBNull.Value ? dr[emailColumn].ToString() : "";
                                    }
                                    catch
                                    {
                                        // Column doesn't exist in result set
                                        emailTemplate = "";
                                    }
                                }
                                bool ynEnabled = dr[ynColumn] != DBNull.Value && Convert.ToBoolean(dr[ynColumn]);
                                bool emailEnabled = !string.IsNullOrEmpty(emailTemplate); // Default: enabled if template has content
                                if (emailEnabledColExists)
                                {
                                    try { emailEnabled = dr[emailEnabledColumn] != DBNull.Value && Convert.ToBoolean(dr[emailEnabledColumn]) && !string.IsNullOrEmpty(emailTemplate); }
                                    catch { }
                                }

                                string emailCC = "";
                                string emailBCC = "";
                                if (ccColExists && !string.IsNullOrEmpty(emailCCColumn))
                                {
                                    try { emailCC = dr[emailCCColumn] != DBNull.Value ? dr[emailCCColumn].ToString() : ""; }
                                    catch { }
                                }
                                if (bccColExists && !string.IsNullOrEmpty(emailBCCColumn))
                                {
                                    try { emailBCC = dr[emailBCCColumn] != DBNull.Value ? dr[emailBCCColumn].ToString() : ""; }
                                    catch { }
                                }

                                // Arrived: ensure customer always gets an email (use In-Route template or default)
                                if (statusName != null && (statusName.Equals("Arrived", StringComparison.OrdinalIgnoreCase) || statusName.IndexOf("Arrived", StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    if (string.IsNullOrWhiteSpace(emailTemplate))
                                    {
                                        emailTemplate = "Hello [First Name],\n\nYour technician has arrived at your location.\n\nAppointment: [Date] at [Time].\nService: [Service Name].\n\nThank you!";
                                        emailEnabled = true;
                                    }
                                    else if (!emailEnabled)
                                    {
                                        emailEnabled = true; // Send for Arrived even if In-Route email was disabled in Settings
                                    }
                                }

                                return new MessageTemplates
                                {
                                    EmailEnabled = emailEnabled,
                                    EmailTemplate = emailTemplate ?? "",
                                    EmailCC = emailCC ?? "",
                                    EmailBCC = emailBCC ?? "",
                                    SMSEnabled = !string.IsNullOrEmpty(smsTemplate),
                                    SMSTemplate = smsTemplate,
                                    YNEnabled = ynEnabled
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting message templates: {ex.Message}");
            }

            return new MessageTemplates { EmailEnabled = false, SMSEnabled = false, EmailCC = "", EmailBCC = "" };
        }

        /// <summary>
        /// Get resource (technician) email for an appointment - used for CC on status emails (Confirmed, Dispatched, FA-ID, In-Route).
        /// </summary>
        private string GetResourceEmailForAppointment(int resourceId)
        {
            if (resourceId <= 0) return "";
            try
            {
                string resourceEmail = "";
                string faProfileSql = @"
                    SELECT TOP 1 r.Email AS ResourceEmail
                    FROM tbl_FaProfile p WITH (NOLOCK)
                    LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r WITH (NOLOCK) 
                        ON p.ResourceID = r.Id AND p.CompanyID = r.CompanyID
                    WHERE p.ResourceID = @ResourceID AND p.CompanyID = @CompanyID AND p.IsActive = 1
                    ORDER BY p.ProfileID DESC";
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(faProfileSql, conn))
                {
                    cmd.CommandTimeout = 5;
                    cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read() && dr["ResourceEmail"] != DBNull.Value)
                            resourceEmail = dr["ResourceEmail"].ToString().Trim();
                    }
                }
                if (string.IsNullOrEmpty(resourceEmail))
                {
                    string resourceSql = "SELECT Email FROM [msSchedulerV3].[dbo].[tbl_Resources] WITH (NOLOCK) WHERE Id = @ResourceID AND CompanyID = @CompanyID";
                    using (var conn = new SqlConnection(connStr))
                    using (var cmd = new SqlCommand(resourceSql, conn))
                    {
                        cmd.CommandTimeout = 5;
                        cmd.Parameters.AddWithValue("@ResourceID", resourceId);
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        conn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read() && dr["Email"] != DBNull.Value)
                                resourceEmail = dr["Email"].ToString().Trim();
                        }
                    }
                }
                return resourceEmail ?? "";
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting resource email for ResourceID={resourceId}: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Get only the appointment customer's email and mobile from tbl_Customer.
        /// Use this for FA-ID customer email/SMS so we never accidentally send to the field agent's address.
        /// </summary>
        private void GetAppointmentCustomerContact(string customerId, string companyId, out string email, out string mobile)
        {
            email = "";
            mobile = "";
            if (string.IsNullOrEmpty(customerId)) return;
            try
            {
                string sql = "SELECT ISNULL(Email,'') AS Email, ISNULL(Mobile, ISNULL(Phone,'')) AS Mobile FROM tbl_Customer WITH (NOLOCK) WHERE CustomerID = @CustomerID AND CompanyID = @CompanyID";
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 5;
                    cmd.Parameters.AddWithValue("@CustomerID", customerId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            email = dr["Email"] != DBNull.Value ? dr["Email"].ToString().Trim() : "";
                            mobile = dr["Mobile"] != DBNull.Value ? dr["Mobile"].ToString().Trim() : "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting customer contact for CustomerID={customerId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get customer details for template placeholder replacement
        /// </summary>
        private CustomerDetailsForTemplates GetCustomerDetailsForTemplates(string customerId)
        {
            var details = new CustomerDetailsForTemplates();

            try
            {
                string sql = @"
                    SELECT TOP 1 
                        c.FirstName, c.LastName, c.Email, c.Mobile, c.Phone, c.Title, c.JobTitle,
                        c.Address1, c.City, c.State, c.ZipCode, ISNULL(c.Country, '') AS Country,
                        ISNULL(srv.ServiceName, '') AS ServiceName,
                        ISNULL(c.CompanyName, '') AS CompanyName
                    FROM tbl_Customer c WITH (NOLOCK)
                    LEFT JOIN tbl_Appointment apt WITH (NOLOCK) ON c.CustomerID = apt.CustomerID AND c.CompanyID = apt.CompanyID
                    LEFT JOIN tbl_ServiceType srv WITH (NOLOCK) ON apt.CompanyID = srv.CompanyID AND 
                        (TRY_CAST(apt.ServiceType AS INT) = srv.ServiceTypeID OR apt.ServiceType = srv.ServiceName)
                    WHERE c.CustomerID = @CustomerID AND c.CompanyID = @CompanyID";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 5;
                    cmd.Parameters.AddWithValue("@CustomerID", customerId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            details.FirstName = dr["FirstName"] != DBNull.Value ? dr["FirstName"].ToString().Trim() : "";
                            details.LastName = dr["LastName"] != DBNull.Value ? dr["LastName"].ToString().Trim() : "";
                            details.FullName = $"{details.FirstName} {details.LastName}".Trim();
                            details.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString().Trim() : "";
                            details.Mobile = dr["Mobile"] != DBNull.Value ? dr["Mobile"].ToString().Trim() : "";
                            details.Phone = dr["Phone"] != DBNull.Value ? dr["Phone"].ToString().Trim() : "";
                            details.Title = dr["Title"] != DBNull.Value ? dr["Title"].ToString().Trim() : "";
                            details.JobTitle = dr["JobTitle"] != DBNull.Value ? dr["JobTitle"].ToString().Trim() : "";
                            details.ServiceName = dr["ServiceName"] != DBNull.Value ? dr["ServiceName"].ToString().Trim() : "";
                            details.CompanyName = dr["CompanyName"] != DBNull.Value ? dr["CompanyName"].ToString().Trim() : "";
                            string addr1 = dr["Address1"] != DBNull.Value && dr["Address1"] != null ? dr["Address1"].ToString().Trim() : "";
                            string city = dr["City"] != DBNull.Value && dr["City"] != null ? dr["City"].ToString().Trim() : "";
                            string state = dr["State"] != DBNull.Value && dr["State"] != null ? dr["State"].ToString().Trim() : "";
                            string zip = dr["ZipCode"] != DBNull.Value && dr["ZipCode"] != null ? dr["ZipCode"].ToString().Trim() : "";
                            string country = dr["Country"] != DBNull.Value && dr["Country"] != null ? dr["Country"].ToString().Trim() : "";
                            details.Address = string.Join(", ", new[] { addr1, city, state, zip, country }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR getting customer details: {ex.Message}");
            }

            return details;
        }

        /// <summary>
        /// Build calendar-style HTML block for appointment date/time (table-based for email client compatibility)
        /// </summary>
        private string BuildCalendarBlock(string date, string time)
        {
            string displayDate = string.IsNullOrEmpty(date) ? "" : date.Trim();
            string displayTime = string.IsNullOrEmpty(time) ? "" : time.Trim();
            if (string.IsNullOrEmpty(displayDate) && string.IsNullOrEmpty(displayTime)) return "";

            return $@"<table cellpadding='0' cellspacing='0' border='0' width='100%' style='margin:0 0 24px 0;border-collapse:collapse;'>
<tr><td style='padding:20px;background:#f8fafc;border-radius:12px;border:1px solid #e2e8f0;'>
<table cellpadding='0' cellspacing='0' border='0' width='100%'>
<tr>
<td width='50%' style='padding-right:10px;vertical-align:top;'>
<table cellpadding='0' cellspacing='0' border='0' width='100%' style='background:#0f172a;border-radius:10px;'>
<tr><td style='padding:16px;color:#94a3b8;font-size:11px;text-transform:uppercase;letter-spacing:1px;'>Date</td></tr>
<tr><td style='padding:0 16px 16px;color:#fff;font-size:18px;font-weight:700;'>{System.Web.HttpUtility.HtmlEncode(displayDate)}</td></tr>
</table>
</td>
<td width='50%' style='padding-left:10px;vertical-align:top;'>
<table cellpadding='0' cellspacing='0' border='0' width='100%' style='background:#1e40af;border-radius:10px;'>
<tr><td style='padding:16px;color:#93c5fd;font-size:11px;text-transform:uppercase;letter-spacing:1px;'>Time</td></tr>
<tr><td style='padding:0 16px 16px;color:#fff;font-size:18px;font-weight:700;'>{System.Web.HttpUtility.HtmlEncode(displayTime)}</td></tr>
</table>
</td>
</tr>
</table>
</td></tr>
</table>";
        }

        /// <summary>
        /// Replace placeholders in email/SMS templates.
        /// When prependCalendarIfMissing is false (e.g. for status HTML design which has its own date/time tiles), do not prepend the calendar block.
        /// </summary>
        private string ReplacePlaceholders(string template, CustomerDetailsForTemplates customer, AppointmentBasicDetails appointment, bool prependCalendarIfMissing = true)
        {
            if (string.IsNullOrEmpty(template)) return "";

            string formattedDate = FormatDateForEmail(appointment);
            string formattedTime = FormatTimeForEmail(appointment);
            if (string.IsNullOrEmpty(formattedDate)) formattedDate = appointment.RequestDate ?? "";
            if (string.IsNullOrEmpty(formattedTime)) formattedTime = appointment.TimeSlot ?? "";
            string calendarBlock = BuildCalendarBlock(formattedDate, formattedTime);
            string companyName = string.IsNullOrEmpty(customer.CompanyName) ? appointment.CompanyName : customer.CompanyName;
            string result = template;

            // [Square bracket] format
            result = result.Replace("[Calendar]", calendarBlock);
            result = result.Replace("[First Name]", customer.FirstName);
            result = result.Replace("[Last Name]", customer.LastName);
            result = result.Replace("[Full Name]", customer.FullName);
            result = result.Replace("[Title]", customer.Title);
            result = result.Replace("[Job Title]", customer.JobTitle);
            result = result.Replace("[Date]", formattedDate);
            result = result.Replace("[Time]", formattedTime);
            result = result.Replace("[Service Name]", customer.ServiceName);
            result = result.Replace("[Company Name]", companyName);
            result = result.Replace("[Address]", customer.Address ?? "");
            result = result.Replace("[Phone]", customer.Phone ?? "");
            result = result.Replace("[Mobile]", customer.Mobile ?? "");
            result = result.Replace("[Email]", customer.Email ?? "");
            result = result.Replace("[Note]", appointment.Note ?? "");

            // {Curly brace} format (alternative names)
            result = result.Replace("{AppointmentDate}", formattedDate);
            result = result.Replace("{Date}", formattedDate);
            result = result.Replace("{TimeSlot}", formattedTime);
            result = result.Replace("{Time}", formattedTime);
            result = result.Replace("{ServiceType}", customer.ServiceName);
            result = result.Replace("{ServiceName}", customer.ServiceName);
            result = result.Replace("{FirstName}", customer.FirstName);
            result = result.Replace("{LastName}", customer.LastName);
            result = result.Replace("{FullName}", customer.FullName);
            result = result.Replace("{CompanyName}", companyName);
            result = result.Replace("{Calendar}", calendarBlock);
            result = result.Replace("{Address}", customer.Address ?? "");
            result = result.Replace("{Phone}", customer.Phone ?? "");
            result = result.Replace("{Mobile}", customer.Mobile ?? "");
            result = result.Replace("{Email}", customer.Email ?? "");
            result = result.Replace("{Note}", appointment.Note ?? "");

            // If template doesn't have calendar placeholder, optionally prepend calendar block (skip when using status HTML design)
            if (prependCalendarIfMissing)
            {
                bool hasCalendarPlaceholder = template.Contains("[Calendar]") || template.Contains("{Calendar}");
                if (!string.IsNullOrEmpty(calendarBlock) && !hasCalendarPlaceholder)
                    result = calendarBlock + result;
            }

            return result;
        }

        /// <summary>
        /// Format email body so appointment detail lines (Customer Name:, Appointment Date:, etc.) each appear on their own line.
        /// Uses &lt;br&gt; when body already contains HTML (e.g. calendar block) so line breaks render; otherwise uses \n for the layout to convert.
        /// </summary>
        private static string FormatEmailBodyWithLineBreaks(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;
            string normalized = body.Replace("\r\n", "\n").Replace("\r", "\n");
            // Insert line breaks before common appointment detail labels when they appear run-on
            string pattern = @"\s+(Customer Name:|Appointment Date:|Appointment Type:|Time Slot:|Address:|Phone:|Mobile:|Email:|Note:|Company:)\s*";
            string withBreaks = Regex.Replace(normalized, pattern, "\n\n$1 ");
            // When body contains HTML (e.g. calendar table), layout does not convert \n to <br>, so use HTML line breaks here
            if (withBreaks.IndexOf('<') >= 0)
            {
                withBreaks = withBreaks.Replace("\n\n", "<br><br>").Replace("\n", "<br>");
            }
            return withBreaks;
        }

        /// <summary>
        /// Get status-specific title and badge for the status email design.
        /// </summary>
        private static void GetStatusTitleAndBadge(string statusName, out string title, out string badgeText, out string badgeStyle)
        {
            badgeStyle = "background:#e2e8f0;color:#475569;border:1px solid #cbd5e1;";
            if (string.IsNullOrEmpty(statusName)) { title = "Appointment Update"; badgeText = "Update"; return; }
            string s = statusName.Trim();
            if (s.Equals("Pending", StringComparison.OrdinalIgnoreCase)) { title = "Appointment Received"; badgeText = "Pending"; badgeStyle = "background:#fef3c7;color:#b45309;border:1px solid #fde68a;"; return; }
            if (s.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || s.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)) { title = "Appointment Confirmed ✅"; badgeText = "Confirmed"; badgeStyle = "background:#dcfce7;color:#166534;border:1px solid #bbf7d0;"; return; }
            if (s.Equals("Dispatched", StringComparison.OrdinalIgnoreCase)) { title = "Technician Dispatched"; badgeText = "Dispatched"; badgeStyle = "background:#dbeafe;color:#1e40af;border:1px solid #bfdbfe;"; return; }
            if (s.Equals("FA-ID", StringComparison.OrdinalIgnoreCase) || (s.IndexOf("FA-ID", StringComparison.OrdinalIgnoreCase) >= 0)) { title = "Field Agent On the Way"; badgeText = "FA-ID"; badgeStyle = "background:#e0e7ff;color:#3730a3;border:1px solid #c7d2fe;"; return; }
            if (s.Equals("In-Route", StringComparison.OrdinalIgnoreCase) || s.Equals("Progress", StringComparison.OrdinalIgnoreCase)) { title = "Technician In Route"; badgeText = "In-Route"; badgeStyle = "background:#dbeafe;color:#1e40af;border:1px solid #bfdbfe;"; return; }
            if (s.Equals("Arrived", StringComparison.OrdinalIgnoreCase)) { title = "Technician Arrived"; badgeText = "Arrived"; badgeStyle = "background:#dcfce7;color:#166534;border:1px solid #bbf7d0;"; return; }
            if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) { title = "Appointment Completed"; badgeText = "Completed"; badgeStyle = "background:#dcfce7;color:#166534;border:1px solid #bbf7d0;"; return; }
            if (s.Equals("Closed", StringComparison.OrdinalIgnoreCase)) { title = "Appointment Closed"; badgeText = "Closed"; badgeStyle = "background:#f1f5f9;color:#64748b;border:1px solid #e2e8f0;"; return; }
            if (s.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) { title = "Appointment Cancelled"; badgeText = "Cancelled"; badgeStyle = "background:#fee2e2;color:#b91c1c;border:1px solid #fecaca;"; return; }
            title = "Appointment Update"; badgeText = s; return;
        }

        /// <summary>
        /// Build HTML email for resource/tech assignment in the same format as customer confirmation (logo, company, greeting, body, Appointment Details box, logo).
        /// </summary>
        private string BuildResourceAssignmentEmailHtml(string resourceName, string customerName, string serviceName, AppointmentBasicDetails appointment, bool isFaId = false)
        {
            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);
            string companyName = !string.IsNullOrEmpty(companyDetails.CompanyName) ? companyDetails.CompanyName : appointment.CompanyName ?? "Company";
            if (string.IsNullOrEmpty(companyName)) companyName = "Company";
            string logoUrl = GetCompanyLogoUrl(appointment.CompanyID);
            string baseUrl = ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "";
            if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
            string manageUrl = baseUrl + "Appointments.aspx";
            string resourceNameEnc = HttpUtility.HtmlEncode(resourceName ?? "");
            string companyNameEnc = HttpUtility.HtmlEncode(companyName);
            string customerNameEnc = HttpUtility.HtmlEncode(customerName ?? "");
            string serviceNameEnc = HttpUtility.HtmlEncode(serviceName ?? "");
            string formattedDate = FormatDateForEmail(appointment);
            string formattedTime = FormatTimeForEmail(appointment);
            if (string.IsNullOrEmpty(formattedDate)) formattedDate = appointment.RequestDate ?? "";
            if (string.IsNullOrEmpty(formattedTime)) formattedTime = appointment.TimeSlot ?? "";
            string dateStr = HttpUtility.HtmlEncode(formattedDate);
            string timeSlot = HttpUtility.HtmlEncode(formattedTime);
            string officePhone = !string.IsNullOrEmpty(companyDetails.Phone) ? HttpUtility.HtmlEncode(companyDetails.Phone) : "";
            string officePhoneDisplay = string.IsNullOrEmpty(officePhone) ? "—" : officePhone;
            int year = DateTime.UtcNow.Year;
            string title = isFaId ? "FA-ID: New Appointment" : "New Appointment Assignment";
            string bodyText = isFaId
                ? $"You are assigned to this appointment. <strong>Customer:</strong> {customerNameEnc}. <strong>Service:</strong> {serviceNameEnc}. Please review the appointment details below."
                : $"You have a new appointment. <strong>Customer:</strong> {customerNameEnc}. <strong>Service:</strong> {serviceNameEnc}. Please review the appointment details below.";

            string logoBlock = string.IsNullOrEmpty(logoUrl)
                ? ""
                : $"<img src=\"{HttpUtility.HtmlAttributeEncode(logoUrl)}\" width=\"120\" alt=\"Company Logo\" style=\"display:block;border:0;outline:none;text-decoration:none;\">";

            return $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>{HttpUtility.HtmlEncode(title)}</title>
</head>
<body style=""margin:0;padding:0;background:#e5e7eb;font-family:Arial,sans-serif;font-size:13px;"">
  <div style=""display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;"">{HttpUtility.HtmlEncode(title)} – {dateStr} {timeSlot}.</div>

  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#e5e7eb;"">
    <tr><td align=""center"" style=""padding:24px 16px;"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:800px;background:#ffffff;border:1px solid #d1d5db;"">
        <tr><td style=""padding:20px 20px 12px 20px;"">
          {logoBlock}
        </td></tr>
        <tr><td style=""padding:0 20px 8px 20px;"">
          <div style=""font-size:13px;color:#374151;"">Thank You for Choosing</div>
          <div style=""font-size:16px;font-weight:bold;color:#111827;margin-top:2px;"">{companyNameEnc}</div>
        </td></tr>
        <tr><td style=""padding:12px 20px 20px 20px;"">
          <div style=""font-size:13px;color:#374151;line-height:1.5;"">Hi <strong>{resourceNameEnc}</strong>,</div>
          <div style=""font-size:13px;color:#374151;line-height:1.5;margin-top:12px;"">{bodyText}</div>
        </td></tr>
        <tr><td style=""padding:0 20px 16px 20px;"">
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f3f4f6;border:1px solid #d1d5db;"">
            <tr><td style=""padding:12px 14px;font-size:13px;font-weight:bold;color:#111827;text-align:center;"">Appointment Details</td></tr>
            <tr><td style=""padding:0 14px 12px 14px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;width:80px;"">Date</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{dateStr}</td></tr>
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;"">Time</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{timeSlot}</td></tr>
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;"">Office</td><td style=""padding:6px 0 12px 0;font-size:13px;font-weight:bold;color:#111827;"">{officePhoneDisplay}</td></tr>
              </table>
            </td></tr>
          </table>
        </td></tr>
        <tr><td style=""padding:0 20px 20px 20px;"">
          <div style=""margin-top:12px;font-size:12px;""><a href=""{HttpUtility.HtmlAttributeEncode(manageUrl)}"" target=""_blank"" style=""color:#2563eb;text-decoration:none;"">View Appointment</a></div>
          <div style=""margin-top:16px;"">{logoBlock}</div>
        </td></tr>
      </table>
    </td></tr>
    <tr><td style=""padding:8px;font-size:11px;color:#6b7280;text-align:center;"">© {year} {companyNameEnc}. All rights reserved.</td></tr>
  </table>
</body>
</html>";
        }

        /// <summary>
        /// Build HTML email to customer when FA-ID is sent: same format as confirmation, with Field Agent Content Profile details (picture, name, custom content).
        /// </summary>
        private string BuildFaIdCustomerEmailHtml(CustomerDetailsForTemplates customer, AppointmentBasicDetails appointment, List<FaProfileForEmail> fieldAgentProfiles)
        {
            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);
            string companyName = !string.IsNullOrEmpty(companyDetails.CompanyName) ? companyDetails.CompanyName : (customer?.CompanyName ?? appointment.CompanyName ?? "Company");
            if (string.IsNullOrEmpty(companyName)) companyName = "Company";
            string logoUrl = GetCompanyLogoUrl(appointment.CompanyID);
            string baseUrl = ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "";
            if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
            string manageUrl = baseUrl + "Appointments.aspx";
            string fullName = customer != null ? HttpUtility.HtmlEncode(customer.FullName ?? "") : "";
            string companyNameEnc = HttpUtility.HtmlEncode(companyName);
            string formattedDate = FormatDateForEmail(appointment);
            string formattedTime = FormatTimeForEmail(appointment);
            if (string.IsNullOrEmpty(formattedDate)) formattedDate = appointment.RequestDate ?? "";
            if (string.IsNullOrEmpty(formattedTime)) formattedTime = appointment.TimeSlot ?? "";
            string dateStr = HttpUtility.HtmlEncode(formattedDate);
            string timeSlot = HttpUtility.HtmlEncode(formattedTime);
            string officePhone = !string.IsNullOrEmpty(companyDetails.Phone) ? HttpUtility.HtmlEncode(companyDetails.Phone) : "";
            string officePhoneDisplay = string.IsNullOrEmpty(officePhone) ? "—" : officePhone;
            int year = DateTime.UtcNow.Year;

            string faNamesList = fieldAgentProfiles != null && fieldAgentProfiles.Count > 0
                ? string.Join(", ", fieldAgentProfiles.Select(f => "<strong>" + HttpUtility.HtmlEncode(f.FaName) + "</strong>"))
                : "your assigned technician";
            string bodyText = $"The following field agent(s) will be handling your appointment: {faNamesList}. They will arrive <strong>between</strong> the times listed below. We look forward to seeing you soon!";

            // Build Field Agent profile cards (picture + name + custom content)
            string faCardsHtml = "";
            if (fieldAgentProfiles != null && fieldAgentProfiles.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<div style=\"margin-top:16px;font-size:13px;font-weight:bold;color:#111827;\">Your field agent(s)</div>");
                foreach (var fa in fieldAgentProfiles)
                {
                    string nameEnc = HttpUtility.HtmlEncode(fa.FaName ?? "");
                    string imgTag = "";
                    if (!string.IsNullOrEmpty(fa.PictureUrl))
                    {
                        string src = fa.PictureUrl.Trim();
                        string imgUrl = "";
                        if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            // data: URIs are blocked by email clients -- serve via handler
                            if (fa.ProfileID > 0)
                                imgUrl = baseUrl + "FaProfileImage.ashx?profileId=" + fa.ProfileID + "&companyId=" + Uri.EscapeDataString(appointment.CompanyID);
                        }
                        else if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            imgUrl = src;
                        else if (!src.Contains(" ") && src.Length < 200)
                            imgUrl = baseUrl + "CompanyLogo/" + src;

                        if (!string.IsNullOrEmpty(imgUrl))
                            imgTag = $"<img src=\"{HttpUtility.HtmlAttributeEncode(imgUrl)}\" alt=\"{HttpUtility.HtmlAttributeEncode(fa.FaName ?? "Agent")}\" width=\"80\" height=\"80\" style=\"width:80px;height:80px;object-fit:cover;border-radius:50%;border:2px solid #e5e7eb;display:block;\">";
                    }
                    string contentEnc = "";
                    if (!string.IsNullOrWhiteSpace(fa.CustomContent))
                    {
                        string content = fa.CustomContent.Trim();
                        contentEnc = "<div style=\"font-size:12px;color:#6b7280;line-height:1.4;margin-top:4px;font-weight:normal;\">" + HttpUtility.HtmlEncode(content).Replace("\n", "<br>") + "</div>";
                    }
                    sb.Append($@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin-top:12px;width:100%;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;"">
<tr><td style=""padding:12px;vertical-align:top;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
  <td style=""padding-right:12px;vertical-align:top;"">{imgTag}</td>
  <td style=""vertical-align:top;"">
    <div style=""font-size:14px;font-weight:bold;color:#111827;"">{nameEnc}</div>
    {contentEnc}
  </td>
  </tr></table>
</td></tr>
</table>");
                }
                faCardsHtml = sb.ToString();
            }

            string logoBlock = string.IsNullOrEmpty(logoUrl)
                ? ""
                : $"<img src=\"{HttpUtility.HtmlAttributeEncode(logoUrl)}\" width=\"120\" alt=\"Company Logo\" style=\"display:block;border:0;outline:none;text-decoration:none;\">";

            return $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>Field Agent Assigned - Your Appointment</title>
</head>
<body style=""margin:0;padding:0;background:#e5e7eb;font-family:Arial,sans-serif;font-size:13px;"">
  <div style=""display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;"">Field Agent Assigned – {dateStr} {timeSlot}.</div>

  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#e5e7eb;"">
    <tr><td align=""center"" style=""padding:24px 16px;"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:800px;background:#ffffff;border:1px solid #d1d5db;"">
        <tr><td style=""padding:20px 20px 12px 20px;"">{logoBlock}</td></tr>
        <tr><td style=""padding:0 20px 8px 20px;"">
          <div style=""font-size:13px;color:#374151;"">Thank You for Choosing</div>
          <div style=""font-size:16px;font-weight:bold;color:#111827;margin-top:2px;"">{companyNameEnc}</div>
        </td></tr>
        <tr><td style=""padding:12px 20px 12px 20px;"">
          <div style=""font-size:13px;color:#374151;line-height:1.5;"">Hi <strong>{fullName}</strong>,</div>
          <div style=""font-size:13px;color:#374151;line-height:1.5;margin-top:12px;"">{bodyText}</div>
          {faCardsHtml}
        </td></tr>
        <tr><td style=""padding:0 20px 16px 20px;"">
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f3f4f6;border:1px solid #d1d5db;"">
            <tr><td style=""padding:12px 14px;font-size:13px;font-weight:bold;color:#111827;text-align:center;"">Appointment Details</td></tr>
            <tr><td style=""padding:0 14px 12px 14px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;width:80px;"">Date</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{dateStr}</td></tr>
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;"">Time</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{timeSlot}</td></tr>
                <tr><td style=""padding:6px 0 12px 0;font-size:13px;color:#374151;"">Office</td><td style=""padding:6px 0 12px 0;font-size:13px;font-weight:bold;color:#111827;"">{officePhoneDisplay}</td></tr>
              </table>
            </td></tr>
          </table>
        </td></tr>
        <tr><td style=""padding:0 20px 20px 20px;"">
          <div style=""margin-top:12px;font-size:12px;""><a href=""{HttpUtility.HtmlAttributeEncode(manageUrl)}"" target=""_blank"" style=""color:#2563eb;text-decoration:none;"">Manage Appointment</a></div>
          <div style=""margin-top:16px;"">{logoBlock}</div>
        </td></tr>
      </table>
    </td></tr>
    <tr><td style=""padding:8px;font-size:11px;color:#6b7280;text-align:center;"">© {year} {companyNameEnc}. All rights reserved.</td></tr>
  </table>
</body>
</html>";
        }

        /// <summary>
        /// Replace FA-ID SMS template placeholders from Settings. Used when Settings → Field Agent ID → SMS Template is set.
        /// Placeholders: [FA Name], [Company Name], [Address], [FA Custom Content], [What to Expect URL], [Track URL], [Service Name], [FA Phone], [FA Email], [FA Picture URL].
        /// </summary>
        private string ReplaceFaIdSmsPlaceholders(string template, CustomerDetailsForTemplates customer, AppointmentBasicDetails appointment, List<FaProfileForEmail> fieldAgentProfiles)
        {
            if (string.IsNullOrEmpty(template)) return "";
            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);
            string companyName = !string.IsNullOrEmpty(companyDetails.CompanyName) ? companyDetails.CompanyName : (customer?.CompanyName ?? appointment.CompanyName ?? "Company");
            if (string.IsNullOrEmpty(companyName)) companyName = "Company";
            string address = (customer != null && !string.IsNullOrEmpty(customer.Address)) ? customer.Address.Trim() : "";
            string website = !string.IsNullOrEmpty(companyDetails.Website) ? companyDetails.Website.Trim() : "";
            string baseUrl = (ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "").Trim();
            if (string.IsNullOrEmpty(baseUrl) && HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                try { baseUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/"); }
                catch { }
            }
            if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
            string whatToExpectUrl = (ConfigurationManager.AppSettings["FAIDWhatToExpectUrl"] ?? website ?? baseUrl ?? "").Trim();
            string trackUrl = (ConfigurationManager.AppSettings["FAIDTrackingUrl"] ?? "").Trim();
            string serviceName = !string.IsNullOrEmpty(customer?.ServiceName) ? customer.ServiceName : "";
            string faName = "Your technician";
            string faCustomContent = "";
            string faPhone = "";
            string faEmail = "";
            string faPictureUrl = "";
            if (fieldAgentProfiles != null && fieldAgentProfiles.Count > 0)
            {
                var firstFa = fieldAgentProfiles[0];
                faName = !string.IsNullOrWhiteSpace(firstFa.FaName) ? firstFa.FaName.Trim() : faName;
                faCustomContent = !string.IsNullOrWhiteSpace(firstFa.CustomContent) ? firstFa.CustomContent.Trim().Replace("\r\n", "\n").Replace("\r", "\n") : "";
                faPhone = !string.IsNullOrWhiteSpace(firstFa.MobilePhone) ? firstFa.MobilePhone.Trim() : "";
                faEmail = !string.IsNullOrWhiteSpace(firstFa.Email) ? firstFa.Email.Trim() : "";

                // [FA Picture URL] is meant for humans clicking from SMS: show the profile page (picture+details).
                // Twilio MMS media still uses the raw image URL built in BuildFaIdCustomerSmsBody.
                if (firstFa.ProfileID > 0 && !string.IsNullOrEmpty(appointment.CompanyID) && !string.IsNullOrEmpty(baseUrl))
                {
                    faPictureUrl = baseUrl + "FaProfileImage.ashx?profileId=" + firstFa.ProfileID + "&companyId=" + Uri.EscapeDataString(appointment.CompanyID) + "&view=profile";
                }
            }
            string body = template
                .Replace("[FA Name]", faName)
                .Replace("[Company Name]", companyName)
                .Replace("[Address]", address)
                .Replace("[FA Custom Content]", faCustomContent)
                .Replace("[FA Phone]", faPhone)
                .Replace("[FA Email]", faEmail)
                .Replace("[FA Picture URL]", faPictureUrl)
                .Replace("[What to Expect URL]", whatToExpectUrl)
                .Replace("[Track URL]", trackUrl)
                .Replace("[Service Name]", serviceName);
            return body;
        }

        /// <summary>
        /// Build SMS/MMS body for customer when FA-ID is sent (screenshot format: agent picture + FA name, company, full address, bio, what to expect, track link).
        /// Returns (body, firstFaPictureUrlForMms): use firstFaPictureUrlForMms for MMS when it is a public http(s) URL (Twilio must be able to fetch it).
        /// </summary>
        private void BuildFaIdCustomerSmsBody(CustomerDetailsForTemplates customer, AppointmentBasicDetails appointment, List<FaProfileForEmail> fieldAgentProfiles, out string smsBody, out string firstFaPictureUrlForMms)
        {
            firstFaPictureUrlForMms = null;
            smsBody = "";
            if (fieldAgentProfiles == null || fieldAgentProfiles.Count == 0) return;

            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);
            string companyName = !string.IsNullOrEmpty(companyDetails.CompanyName) ? companyDetails.CompanyName : (customer?.CompanyName ?? appointment.CompanyName ?? "Company");
            if (string.IsNullOrEmpty(companyName)) companyName = "Company";
            // Full address for "on the way to [address]" (e.g. "1149 Lafayette Road, Wayne, PA 19087 USA")
            string address = (customer != null && !string.IsNullOrEmpty(customer.Address)) ? customer.Address.Trim() : "";
            string website = !string.IsNullOrEmpty(companyDetails.Website) ? companyDetails.Website.Trim() : "";
            // Base URL must be publicly reachable (e.g. https://yoursite.com/fsm/) so Twilio can fetch FaProfileImage.ashx for MMS.
            string baseUrl = (ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "").Trim();
            if (string.IsNullOrEmpty(baseUrl) && HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                try
                {
                    var req = HttpContext.Current.Request;
                    baseUrl = req.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute("~/");
                }
                catch { }
            }
            if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
            string whatToExpectUrl = (ConfigurationManager.AppSettings["FAIDWhatToExpectUrl"] ?? website ?? baseUrl ?? "").Trim();
            string trackUrl = (ConfigurationManager.AppSettings["FAIDTrackingUrl"] ?? "").Trim();

            var firstFa = fieldAgentProfiles[0];
            string faName = !string.IsNullOrWhiteSpace(firstFa.FaName) ? firstFa.FaName.Trim() : "Your technician";
            bool plural = fieldAgentProfiles.Count > 1;
            string onTheWay = plural ? "are on the way" : "is on the way";

            // Intro line like: "Hi, Antonio Lewis from Mattioni Plumbing, Heating & Cooling is on the way to 1149 Lafayette Road, Wayne, PA 19087 USA."
            string intro = string.IsNullOrEmpty(address)
                ? $"Hi, {faName} from {companyName} {onTheWay}."
                : $"Hi, {faName} from {companyName} {onTheWay} to {address}.";
            var parts = new List<string> { intro };

            // First FA's bio (CustomContent) – keep line breaks for readability; normalize to single newline for SMS
            if (!string.IsNullOrWhiteSpace(firstFa.CustomContent))
            {
                string bio = firstFa.CustomContent.Trim()
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
                // Collapse multiple newlines to one
                while (bio.Contains("\n\n\n")) bio = bio.Replace("\n\n\n", "\n\n");
                parts.Add(bio);
            }

            if (!string.IsNullOrWhiteSpace(whatToExpectUrl))
                parts.Add("What to expect: " + whatToExpectUrl);
            if (!string.IsNullOrWhiteSpace(trackUrl))
                parts.Add("Track your expert: " + trackUrl);

            smsBody = string.Join("\n\n", parts);

            // MMS: Twilio requires a public absolute URL for media. Use http(s) URL as-is; for base64 (data:) use FaProfileImage.ashx (baseurl must be publicly reachable).
            if (!string.IsNullOrEmpty(firstFa.PictureUrl))
            {
                string url = firstFa.PictureUrl.Trim();
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    firstFaPictureUrlForMms = url;
                else if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && firstFa.ProfileID > 0 && !string.IsNullOrEmpty(appointment.CompanyID) && !string.IsNullOrEmpty(baseUrl))
                    firstFaPictureUrlForMms = baseUrl + "FaProfileImage.ashx?profileId=" + firstFa.ProfileID + "&companyId=" + Uri.EscapeDataString(appointment.CompanyID);
            }
        }

        /// <summary>
        /// Build full HTML email for any status (modern card layout): Settings template content + date/time tiles + details table. Used for all statuses.
        /// </summary>
        private string BuildStatusEmailHtml(string statusName, CustomerDetailsForTemplates customer, AppointmentBasicDetails appointment, string templateContent)
        {
            GetStatusTitleAndBadge(statusName, out string title, out string badgeText, out string badgeStyle);
            CompanyDetailsForEmail companyDetails = GetCompanyDetailsForEmail(appointment.CompanyID);
            string companyName = !string.IsNullOrEmpty(companyDetails.CompanyName) ? companyDetails.CompanyName : (string.IsNullOrEmpty(customer.CompanyName) ? appointment.CompanyName : customer.CompanyName);
            if (string.IsNullOrEmpty(companyName)) companyName = "Company";
            string logoUrl = GetCompanyLogoUrl(appointment.CompanyID);
            string baseUrl = ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "";
            if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
            string manageUrl = baseUrl + "Appointments.aspx";
            string firstName = HttpUtility.HtmlEncode(customer.FirstName ?? "");
            string fullName = HttpUtility.HtmlEncode(customer.FullName ?? "");
            string serviceName = HttpUtility.HtmlEncode(customer.ServiceName ?? "");
            string formattedDate = FormatDateForEmail(appointment);
            string formattedTime = FormatTimeForEmail(appointment);
            if (string.IsNullOrEmpty(formattedDate)) formattedDate = appointment.RequestDate ?? "";
            if (string.IsNullOrEmpty(formattedTime)) formattedTime = appointment.TimeSlot ?? "";
            string timeSlot = HttpUtility.HtmlEncode(formattedTime);
            string dateStr = HttpUtility.HtmlEncode(formattedDate);
            string address = HttpUtility.HtmlEncode(customer.Address ?? "");
            string phone = HttpUtility.HtmlEncode(customer.Phone ?? "");
            string mobile = HttpUtility.HtmlEncode(customer.Mobile ?? "");
            string email = HttpUtility.HtmlEncode(customer.Email ?? "");
            string note = HttpUtility.HtmlEncode(appointment.Note ?? "");
            string companyNameEnc = HttpUtility.HtmlEncode(companyName);
            int year = DateTime.UtcNow.Year;
            string emailMailto = string.IsNullOrEmpty(customer.Email) ? "" : ("mailto:" + customer.Email);
            string messageHtml = string.IsNullOrEmpty(templateContent) ? "" : (templateContent.IndexOf('<') >= 0 ? templateContent.Replace("\n\n", "<br><br>").Replace("\n", "<br>") : HttpUtility.HtmlEncode(templateContent).Replace("\n", "<br>"));

            string logoBlock = string.IsNullOrEmpty(logoUrl)
                ? ""
                : $"<img src=\"{HttpUtility.HtmlAttributeEncode(logoUrl)}\" width=\"120\" alt=\"Company Logo\" style=\"display:block;border:0;outline:none;text-decoration:none;\">";
            string googleCalUrl = BuildGoogleCalendarUrl(appointment, customer, companyName);
            string outlookCalUrl = BuildOutlookCalendarUrl(appointment, customer, companyName);
            string googleCalHref = string.IsNullOrEmpty(googleCalUrl) ? "#" : googleCalUrl;
            string outlookCalHref = string.IsNullOrEmpty(outlookCalUrl) ? "#" : outlookCalUrl;
            string officePhone = !string.IsNullOrEmpty(companyDetails.Phone) ? HttpUtility.HtmlEncode(companyDetails.Phone) : "";
            string officePhoneDisplay = string.IsNullOrEmpty(officePhone) ? "—" : officePhone;
            string messageHtmlBlock = string.IsNullOrEmpty(messageHtml) ? "" : "<div style=\"font-size:13px;color:#374151;line-height:1.5;margin-top:12px;\">" + messageHtml + "</div>";

            return $@"<!doctype html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>{HttpUtility.HtmlEncode(title)}</title>
</head>
<body style=""margin:0;padding:0;background:#e5e7eb;font-family:Arial,sans-serif;font-size:13px;"">
  <div style=""display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;"">{HttpUtility.HtmlEncode(title)} – {dateStr} {timeSlot}.</div>

  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#e5e7eb;"">
    <tr><td align=""center"" style=""padding:24px 16px;"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:800px;background:#ffffff;border:1px solid #d1d5db;"">
        <tr><td style=""padding:20px 20px 12px 20px;"">
          {logoBlock}
        </td></tr>
        <tr><td style=""padding:0 20px 8px 20px;"">
          <div style=""font-size:13px;color:#374151;"">Thank You for Choosing</div>
          <div style=""font-size:16px;font-weight:bold;color:#111827;margin-top:2px;"">{companyNameEnc}</div>
        </td></tr>
        <tr><td style=""padding:12px 20px 20px 20px;"">
          <div style=""font-size:13px;color:#374151;line-height:1.5;"">Hi <strong>{fullName}</strong>,</div>
          <div style=""font-size:13px;color:#374151;line-height:1.5;margin-top:12px;"">You have made an appointment with <strong>{companyNameEnc}</strong> for the property at <strong>{address}</strong>. Our technician will arrive <strong>between</strong> the times listed below. We look forward to seeing you soon!</div>
          {messageHtmlBlock}
        </td></tr>
        <tr><td style=""padding:0 20px 16px 20px;"">
          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f3f4f6;border:1px solid #d1d5db;"">
            <tr><td style=""padding:12px 14px;font-size:13px;font-weight:bold;color:#111827;text-align:center;"">Appointment Details</td></tr>
            <tr><td style=""padding:0 14px 12px 14px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;width:80px;"">Date</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{dateStr}</td></tr>
                <tr><td style=""padding:6px 0;font-size:13px;color:#374151;"">Time</td><td style=""padding:6px 0;font-size:13px;font-weight:bold;color:#111827;"">{timeSlot}</td></tr>
                <tr><td style=""padding:6px 0 12px 0;font-size:13px;color:#374151;"">Office</td><td style=""padding:6px 0 12px 0;font-size:13px;font-weight:bold;color:#111827;"">{officePhoneDisplay}</td></tr>
              </table>
            </td></tr>
          </table>
        </td></tr>
        <tr><td style=""padding:0 20px 20px 20px;"">
          <div style=""margin-top:12px;font-size:12px;""><a href=""{HttpUtility.HtmlAttributeEncode(manageUrl)}"" target=""_blank"" style=""color:#2563eb;text-decoration:none;"">Manage Appointment</a></div>
          <div style=""margin-top:16px;"">{logoBlock}</div>
        </td></tr>
      </table>
    </td></tr>
    <tr><td style=""padding:8px;font-size:11px;color:#6b7280;text-align:center;"">© {year} {companyNameEnc}. All rights reserved.</td></tr>
  </table>
</body>
</html>";
        }

        private string GetCompanyLogoUrl(string companyId)
        {
            if (string.IsNullOrEmpty(companyId)) return "";
            try
            {
                string baseUrl = ConfigurationManager.AppSettings["baseurl"] ?? ConfigurationManager.AppSettings["BaseUrl"] ?? "";
                if (baseUrl.Length > 0 && !baseUrl.EndsWith("/")) baseUrl += "/";
                string sql = "SELECT LogoFile FROM tbl_Company WHERE CompanyID=@CompanyID";
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value && !string.IsNullOrWhiteSpace(o.ToString()))
                        return baseUrl + "CompanyLogo/" + (o.ToString().Contains(" ") ? Uri.EscapeDataString(o.ToString()) : o.ToString());
                }
            }
            catch { }
            return "";
        }

        /// <summary>Format appointment date for email display (e.g. "Tuesday March 3 2026" to match CEC style).</summary>
        private static string FormatDateForEmail(AppointmentBasicDetails appointment)
        {
            if (appointment == null) return "";
            if (appointment.ApptDateTimeLocal.HasValue)
                return appointment.ApptDateTimeLocal.Value.ToString("dddd MMMM d yyyy", CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(appointment.RequestDate))
            {
                if (DateTime.TryParse(appointment.RequestDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                    return d.ToString("dddd MMMM d yyyy", CultureInfo.InvariantCulture);
                return appointment.RequestDate.Trim();
            }
            return "";
        }

        /// <summary>Format appointment time for email (e.g. "2:00 PM" or "Morning" if no time).</summary>
        private static string FormatTimeForEmail(AppointmentBasicDetails appointment)
        {
            if (appointment == null) return "";
            if (appointment.ApptDateTimeLocal.HasValue)
                return appointment.ApptDateTimeLocal.Value.ToString("h:mm tt", CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(appointment.TimeSlot) ? "" : appointment.TimeSlot.Trim();
        }

        /// <summary>Company details for email footer (logo, address, phone, etc.).</summary>
        private class CompanyDetailsForEmail
        {
            public string CompanyName { get; set; }
            public string LogoFile { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Website { get; set; }
        }

        private CompanyDetailsForEmail GetCompanyDetailsForEmail(string companyId)
        {
            var details = new CompanyDetailsForEmail();
            if (string.IsNullOrEmpty(companyId)) return details;
            try
            {
                string sql = "SELECT CompanyName, LogoFile, ISNULL(Address,'') AS Address, ISNULL(Phone,'') AS Phone, ISNULL(Email,'') AS Email, ISNULL(Website,'') AS Website FROM tbl_Company WHERE CompanyID=@CompanyID";
                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            details.CompanyName = rdr["CompanyName"] != DBNull.Value && rdr["CompanyName"] != null ? rdr["CompanyName"].ToString().Trim() : "";
                            details.LogoFile = rdr["LogoFile"] != DBNull.Value && rdr["LogoFile"] != null ? rdr["LogoFile"].ToString().Trim() : "";
                            details.Address = rdr["Address"] != DBNull.Value && rdr["Address"] != null ? rdr["Address"].ToString().Trim() : "";
                            details.Phone = rdr["Phone"] != DBNull.Value && rdr["Phone"] != null ? rdr["Phone"].ToString().Trim() : "";
                            details.Email = rdr["Email"] != DBNull.Value && rdr["Email"] != null ? rdr["Email"].ToString().Trim() : "";
                            details.Website = rdr["Website"] != DBNull.Value && rdr["Website"] != null ? rdr["Website"].ToString().Trim() : "";
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    string sql = "SELECT CompanyName, LogoFile FROM tbl_Company WHERE CompanyID=@CompanyID";
                    using (var conn = new SqlConnection(connStr))
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        conn.Open();
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                details.CompanyName = rdr["CompanyName"] != DBNull.Value && rdr["CompanyName"] != null ? rdr["CompanyName"].ToString().Trim() : "";
                                details.LogoFile = rdr["LogoFile"] != DBNull.Value && rdr["LogoFile"] != null ? rdr["LogoFile"].ToString().Trim() : "";
                            }
                        }
                    }
                }
                catch { }
            }
            return details;
        }

        /// <summary>Build Google Calendar "Add to Calendar" URL (opens pre-filled event).</summary>
        private string BuildGoogleCalendarUrl(AppointmentBasicDetails appointment, CustomerDetailsForTemplates customer, string companyName)
        {
            if (!appointment.ApptDateTimeUtc.HasValue) return "";
            DateTime startUtc = appointment.ApptDateTimeUtc.Value;
            DateTime endUtc = startUtc.AddHours(1);
            string title = Uri.EscapeDataString(string.IsNullOrEmpty(customer.ServiceName) ? "Appointment" : customer.ServiceName);
            string dates = startUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "/" + endUtc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            string details = Uri.EscapeDataString("Appointment with " + (customer.FullName ?? "") + (string.IsNullOrEmpty(appointment.Note) ? "" : "\n\n" + appointment.Note));
            string location = Uri.EscapeDataString(customer.Address ?? "");
            return "https://calendar.google.com/calendar/render?action=TEMPLATE&text=" + title + "&dates=" + dates + "&details=" + details + "&location=" + location;
        }

        /// <summary>Build Outlook "Add to Calendar" URL (opens compose with pre-filled event).</summary>
        private string BuildOutlookCalendarUrl(AppointmentBasicDetails appointment, CustomerDetailsForTemplates customer, string companyName)
        {
            if (!appointment.ApptDateTimeUtc.HasValue) return "";
            DateTime startUtc = appointment.ApptDateTimeUtc.Value;
            DateTime endUtc = startUtc.AddHours(1);
            string subject = Uri.EscapeDataString(string.IsNullOrEmpty(customer.ServiceName) ? "Appointment" : customer.ServiceName);
            string startIso = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            string endIso = endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            string body = Uri.EscapeDataString("Appointment with " + (customer.FullName ?? "") + (string.IsNullOrEmpty(appointment.Note) ? "" : "\n\n" + appointment.Note));
            string location = Uri.EscapeDataString(customer.Address ?? "");
            return "https://outlook.office.com/calendar/0/action/compose?subject=" + subject + "&startdt=" + startIso + "&enddt=" + endIso + "&body=" + body + "&location=" + location;
        }

        /// <summary>
        /// Get email subject based on status
        /// </summary>
        private string GetEmailSubject(string statusName, string serviceName)
        {
            if (statusName.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return $"Appointment Confirmation - {serviceName}";
            else if (statusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || statusName.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                return $"Appointment Confirmed - {serviceName}";
            else if (statusName.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                return $"Appointment Closed - {serviceName}";
            else if (statusName.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                return $"Appointment Cancelled - {serviceName}";
            else if (statusName.Equals("Arrived", StringComparison.OrdinalIgnoreCase))
                return $"Technician Arrived - {serviceName}";
            else
                return $"Appointment Update - {serviceName}";
        }

        /// <summary>
        /// Send email to customer
        /// Returns true if email was sent successfully, false otherwise
        /// </summary>
        private bool SendEmailToCustomer(string toEmail, string subject, string body, AppointmentBasicDetails appointment, string cc = "", string bcc = "")
        {
            try
            {
                var emailProcessor = new EmailProcessor();
                string result = emailProcessor.SendHtmlFormattedEmail(
                    appointment.CompanyID,
                    appointment.CustomerID,
                    $"Appointment Status: {appointment.Status}",
                    subject,
                    body,
                    toEmail,
                    cc ?? "",
                    bcc ?? "",
                    new List<EmailContent>()
                );
                
                // Check if email was sent successfully (returns "Sent" on success, error message on failure)
                if (result == "Sent")
                {
                    ConsoleLog($"Email sent successfully to customer {toEmail}");
                    return true;
                }
                else
                {
                    ConsoleLog($"ERROR sending email to customer {toEmail}: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR sending email to customer: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Map status ID/name to SMS code used by SendAppointmentSMS
        /// </summary>
        private string MapStatusToSmsCode(int statusId, string statusName)
        {
            // Status codes: 1=Pending, 2=Scheduled/Confirmed, 3=Cancelled, 4=Closed, 5=Progress, 6=Completed
            if (statusId == 1 || statusName.Equals("Pending", StringComparison.OrdinalIgnoreCase)) return "1";
            if (statusId == 2 || statusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || 
                statusName.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)) return "2";
            if (statusId == 3 || statusId == 9 || statusName.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) return "3";
            if (statusId == 4 || statusId == 7 || statusName.Equals("Closed", StringComparison.OrdinalIgnoreCase)) return "4";
            if (statusId == 5 || statusName.Equals("In-Route", StringComparison.OrdinalIgnoreCase) || 
                statusName.Equals("Progress", StringComparison.OrdinalIgnoreCase) || 
                statusName.Equals("Arrived", StringComparison.OrdinalIgnoreCase) ||
                (statusName != null && statusName.IndexOf("Arrived", StringComparison.OrdinalIgnoreCase) >= 0)) return "5";
            if (statusId == 6 || statusName.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "6";
            return "1"; // Default to Pending
        }

        /// <summary>
        /// Log SMS to database
        /// </summary>
        private bool LogSMS(string companyId, string customerId, string resourceId, string apptId, string mobile, string smsType, string smsBody, string smsSid)
        {
            try
            {
                string sql = @"INSERT INTO [dbo].[tbl_TwilioSMSLog] 
                              ([CompanyId], [CustomerId], [ResourceId], [AppointmentId], [ToNumber], [SMSType], [SMSBody], [SMSSid], [SendDateTime]) 
                              VALUES 
                              (@CompanyId, @CustomerId, @ResourceId, @AppointmentId, @ToNumber, @SMSType, @SMSBody, @SMSSid, GETDATE())";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", companyId);
                    cmd.Parameters.AddWithValue("@CustomerId", string.IsNullOrEmpty(customerId) ? (object)DBNull.Value : customerId);
                    cmd.Parameters.AddWithValue("@ResourceId", string.IsNullOrEmpty(resourceId) ? (object)DBNull.Value : resourceId);
                    cmd.Parameters.AddWithValue("@AppointmentId", string.IsNullOrEmpty(apptId) ? (object)DBNull.Value : apptId);
                    cmd.Parameters.AddWithValue("@ToNumber", mobile);
                    cmd.Parameters.AddWithValue("@SMSType", smsType);
                    cmd.Parameters.AddWithValue("@SMSBody", smsBody);
                    cmd.Parameters.AddWithValue("@SMSSid", string.IsNullOrEmpty(smsSid) ? (object)DBNull.Value : smsSid);
                    
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    ConsoleLog($"SMS logged to database: {rowsAffected} row(s) affected");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                ConsoleLog($"ERROR logging SMS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Console logging for debugging
        /// </summary>
        private void ConsoleLog(string message)
        {
            string logMessage = $"[AppointmentStatusCommunicationProcessor] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            Debug.WriteLine(logMessage);
            System.Diagnostics.Debug.WriteLine(logMessage);
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Response != null)
                {
                    HttpContext.Current.Response.AppendHeader("X-Debug-Log", System.Web.HttpUtility.UrlEncode(logMessage));
                }
            }
            catch { }
        }

        /// <summary>
        /// Basic appointment details helper class
        /// </summary>
        private class AppointmentBasicDetails
        {
            public int AppointmentID { get; set; }
            public string CustomerID { get; set; }
            public string CompanyID { get; set; }
            public string RequestDate { get; set; }
            public string TimeSlot { get; set; }
            public string Status { get; set; }
            public int ResourceID { get; set; }
            public string CompanyName { get; set; }
            public string Note { get; set; }
            /// <summary>Appointment datetime in UTC for calendar links.</summary>
            public DateTime? ApptDateTimeUtc { get; set; }
            /// <summary>Appointment datetime in local time for display.</summary>
            public DateTime? ApptDateTimeLocal { get; set; }
        }

        /// <summary>
        /// Message templates helper class
        /// </summary>
        private class MessageTemplates
        {
            public bool EmailEnabled { get; set; }
            public string EmailTemplate { get; set; }
            public string EmailCC { get; set; }
            public string EmailBCC { get; set; }
            public bool SMSEnabled { get; set; }
            public string SMSTemplate { get; set; }
            public bool YNEnabled { get; set; }
        }
        private class MasterTemplate
        {
            public bool enableMms { get; set; }
            public bool EnableSms { get; set; }
          
            public bool enableEmail { get; set; }
            public string standardContent { get; set; }
            public int daysBeforeAppointment { get; set; }
         
        }
                               
    /// <summary>Field agent profile info for FA-ID customer email/SMS (name, picture, custom content, ProfileID for image handler URL).</summary>
    private class FaProfileForEmail
        {
            public int ProfileID { get; set; }
            public string FaName { get; set; }
            public string PictureUrl { get; set; }
            public string CustomContent { get; set; }
            public string MobilePhone { get; set; }
            public string Email { get; set; }
            public string ImageUrl { get; set; }
        }

        /// <summary>
        /// Customer details for template replacement
        /// </summary>
        private class CustomerDetailsForTemplates
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Mobile { get; set; }
            public string Phone { get; set; }
            public string Title { get; set; }
            public string JobTitle { get; set; }
            public string ServiceName { get; set; }
            public string CompanyName { get; set; }
            public string Address { get; set; }
        }
    }
}
