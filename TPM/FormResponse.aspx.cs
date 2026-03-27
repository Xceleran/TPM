using FSM.Models.FormResponseModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using FSM.Processors;

namespace FSM
{
    public partial class FormResponse : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
       public static string GetFormStructure(int templateId)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
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

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveFormResponse(string responses, int templateId, string companyId, int apptId, int cId)
        {
            try
            {
                //var session = HttpContext.Current?.Session;
                //string companyId = session?["CompanyID"]?.ToString();
                //if (string.IsNullOrEmpty(companyId))
                //{
                //    return new { success = false, message = "Company ID missing" };
                //}

                //int templateId = 1;
                string formStructure = responses ?? "[]";
                // Attempt to decode Base64 if provided to bypass request validation issues
                try
                {
                    // Heuristic: if it's valid Base64, decode; otherwise keep as-is
                    byte[] bytes = Convert.FromBase64String(formStructure);
                    string decoded = System.Text.Encoding.UTF8.GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(decoded) && (decoded.TrimStart().StartsWith("[") || decoded.TrimStart().StartsWith("{")))
                    {
                        formStructure = decoded;
                    }
                }
                catch { /* ignore if not base64 */ }
                int? appointmentId = null;
                int customerId = 2;

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
                        cmd.Parameters.AddWithValue("@AppointmentID", (object)appointmentId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerID", customerId);

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
    }
}