using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using FSM.Entity.Forms;
using FSM.Processors;
using FSM.Helper;

namespace FSM
{
    [ScriptService]
    public partial class Forms : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Check authentication and authorization
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("~/AuthVerify.aspx");
                return;
            }
        }

        #region Web Methods for Template Management

        [WebMethod]
        public static List<string> GetServiceTypes()
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new List<string>();

                // Get unique service types from appointments
                string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
                var db = new Database(connectionString);
                var serviceTypes = new List<string>();
                
                try
                {
                    db.Init("sp_Forms_GetServiceTypes");
                    db.AddParameter("@CompanyID", companyId, System.Data.SqlDbType.VarChar);
                    
                    if (db.Execute())
                    {
                        while (db.Reader.Read())
                        {
                            var serviceType = db.GetString("ServiceType");
                            if (!string.IsNullOrEmpty(serviceType))
                            {
                                serviceTypes.Add(serviceType);
                            }
                        }
                    }
                }
                finally
                {
                    db.Close();
                }
                
                return serviceTypes;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving service types: " + ex.Message);
            }
        }

        [WebMethod]
        public static List<FormTemplate> GetAllTemplates()
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new List<FormTemplate>();

                var processor = new FormProcessor();
                return processor.GetAllTemplates(companyId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving templates: " + ex.Message);
            }
        }

        [WebMethod]
        public static FormTemplate GetTemplate(int templateId)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return null;

                var processor = new FormProcessor();
                return processor.GetTemplate(templateId, companyId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving template: " + ex.Message);
            }
        }

        [WebMethod]
        public static int SaveTemplate(FormTemplate template)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(companyId))
                    return 0;

                template.CompanyID = companyId;
                if (template.Id == 0)
                {
                    template.CreatedBy = userId;
                }
                else
                {
                    template.UpdatedBy = userId;
                }

                var processor = new FormProcessor();
                return processor.SaveTemplate(template);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving template: " + ex.Message);
            }
        }

        [WebMethod]
        public static bool ToggleTemplateStatus(int templateId, bool isActive)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return false;

                var processor = new FormProcessor();
                var template = processor.GetTemplate(templateId, companyId);
                if (template != null)
                {
                    template.IsActive = isActive;
                    template.UpdatedBy = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                    return processor.SaveTemplate(template) > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating template status: " + ex.Message);
            }
        }

        [WebMethod]
        public static int DuplicateTemplate(int templateId)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(companyId))
                    return 0;

                var processor = new FormProcessor();
                var originalTemplate = processor.GetTemplate(templateId, companyId);
                
                if (originalTemplate != null)
                {
                    var duplicateTemplate = new FormTemplate
                    {
                        Id = 0, // New template
                        CompanyID = companyId,
                        TemplateName = originalTemplate.TemplateName + " (Copy)",
                        Description = originalTemplate.Description,
                        Category = originalTemplate.Category,
                        IsAutoAssignEnabled = false, // Don't auto-assign duplicates
                        AutoAssignServiceTypes = "",
                        FormStructure = originalTemplate.FormStructure,
                        RequireSignature = originalTemplate.RequireSignature,
                        RequireTip = originalTemplate.RequireTip,
                        IsActive = false, // Start as inactive
                        CreatedBy = userId
                    };

                    return processor.SaveTemplate(duplicateTemplate);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error duplicating template: " + ex.Message);
            }
        }

        #endregion

        #region Web Methods for Form Structure

        [WebMethod]
        public static string GetFormStructure(int templateId)
        {
            try
            {
                
                if (templateId <= 0)
                {

                    return "{}";
                }
                
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                {
                    System.Diagnostics.Debug.WriteLine("CompanyID is null or empty");
                    return "{}";
                }

                System.Diagnostics.Debug.WriteLine($"CompanyID: {companyId}");

                var processor = new FormProcessor();
                var template = processor.GetTemplate(templateId, companyId);
                
                if (template == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Template not found for ID: {templateId}");
                    return "{}";
                }
                
                System.Diagnostics.Debug.WriteLine($"Template found: {template.TemplateName}");
                return template?.FormStructure ?? "{}";
            }
            catch (Exception ex)
            {
                
                throw new Exception("Error retrieving form structure: " + ex.Message);
            }
        }
        [WebMethod]
        public static string GetFormStructure(int templateId,string companyId)
        {
            try
            {
                //string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return "{}";

                var processor = new FormProcessor();
                var template = processor.GetTemplate(templateId, companyId);
                return template?.FormStructure ?? "{}";
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form structure: " + ex.Message);
            }
        }

        [WebMethod]
        public static bool SaveFormStructure(int templateId, string formStructure)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(companyId))
                    return false;

                var processor = new FormProcessor();
                var template = processor.GetTemplate(templateId, companyId);
                
                if (template != null)
                {
                    template.FormStructure = formStructure;
                    template.UpdatedBy = userId;
                    return processor.SaveTemplate(template) > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving form structure: " + ex.Message);
            }
        }

        #endregion

        #region Web Methods for Auto-Assignment and Form Instances

        [WebMethod]
        public static List<FormTemplate> GetAutoAssignedForms(string serviceType)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new List<FormTemplate>();

                var processor = new FormProcessor();
                return processor.GetAutoAssignedForms(serviceType, companyId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving auto-assigned forms: " + ex.Message);
            }
        }

        [WebMethod]
        public static int CreateFormInstance(int templateId, string appointmentId, string customerId)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(companyId))
                    return 0;

                var processor = new FormProcessor();
                
                // Generate auto-filled form data
                var autoFilledData = processor.GenerateAutoFilledFormData(templateId, appointmentId, customerId, companyId);
                
                var instance = new FormInstance
                {
                    CompanyID = companyId,
                    TemplateId = templateId,
                    AppointmentId = appointmentId,
                    CustomerID = customerId,
                    FormData = autoFilledData,
                    Status = "Pending",
                    FilledBy = userId
                };

                return processor.CreateFormInstance(instance);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating form instance: " + ex.Message);
            }
        }

        [WebMethod]
        public static List<FormInstance> GetAppointmentForms(string appointmentId)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new List<FormInstance>();

                var processor = new FormProcessor();
                return processor.GetAppointmentForms(appointmentId, companyId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving appointment forms: " + ex.Message);
            }
        }

        #endregion

        #region Web Methods for Form Responses

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveFormResponse(string responses, int templateId, string companyId, int apptId, int cId)
        {
            try
            {
               
                string formStructure = responses ?? "[]";
            
                try
                {
                   
                    byte[] bytes = Convert.FromBase64String(formStructure);
                    string decoded = System.Text.Encoding.UTF8.GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(decoded) && (decoded.TrimStart().StartsWith("[") || decoded.TrimStart().StartsWith("{")))
                    {
                        formStructure = decoded;
                    }
                }
                catch { /* ignore if not base64 */ }

                string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"];
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
INSERT INTO [myServiceJobs].[dbo].[FormResponse]
(CompanyID, TemplateId, FormStructure, AppointmentID, CustomerID)
VALUES (@CompanyID, @TemplateId, @FormStructure, @AppointmentID, @CustomerID);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        cmd.Parameters.AddWithValue("@TemplateId", templateId);
                        cmd.Parameters.AddWithValue("@FormStructure", formStructure);
                        cmd.Parameters.AddWithValue("@AppointmentID", (object)apptId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerID", cId);

                        conn.Open();
                        var newId = (int)cmd.ExecuteScalar();
                        return new { success = true, id = newId };
                    }
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        #endregion

        #region Web Methods for Usage Log

        [WebMethod]
        public static List<FormUsageLog> GetUsageLog()
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new List<FormUsageLog>();

                var processor = new FormProcessor();
                return processor.GetUsageLog(companyId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving usage log: " + ex.Message);
            }
        }

        #endregion

        #region Web Methods for Form Instance Updates

        [WebMethod]
        public static bool UpdateFormInstance(FormInstance formInstance)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return false;

                var processor = new FormProcessor();
                return processor.UpdateFormInstance(formInstance);
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating form instance: " + ex.Message);
            }
        }

        [WebMethod]
        public static bool SubmitFormInstance(FormInstance formInstance)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(companyId))
                    return false;

                // Set submission details
                formInstance.Status = "Submitted";
                formInstance.SubmittedDateTime = DateTime.Now;
                formInstance.CompletedDateTime = DateTime.Now;
                
                var processor = new FormProcessor();
                bool result = processor.UpdateFormInstance(formInstance);
                
                if (result)
                {
                    // Log form submission
                    processor.LogFormUsage(
                        formInstance.Id, 
                        formInstance.TemplateId, 
                        formInstance.AppointmentId, 
                        "Submitted", 
                        userId, 
                        companyId,
                        "Form submitted successfully"
                    );
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error submitting form instance: " + ex.Message);
            }
        }

        #endregion
    }
}