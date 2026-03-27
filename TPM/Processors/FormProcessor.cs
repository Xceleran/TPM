using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using FSM.Entity.Forms;
using FSM.Entity.Customer;
using FSM.Entity.Appoinments;

namespace FSM.Processors
{
    public class FormProcessor
    {
        private Database db;
        private JavaScriptSerializer serializer;

        public FormProcessor()
        {
            // Use the specific ConnStrJobs connection string from web.config
            string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
            db = new Database(connectionString);
            serializer = new JavaScriptSerializer();
        }

        #region Form Templates Management

        public List<FormTemplate> GetAllTemplates(string companyId)
        {
            var templates = new List<FormTemplate>();

            try
            {
                db.Init("sp_Forms_GetAllTemplates");
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                if (db.Execute())
                {
                    while (db.Reader.Read())
                    {
                        templates.Add(new FormTemplate
                        {
                            Id = db.GetInt("Id"),
                            CompanyID = db.GetString("CompanyID"),
                            TemplateName = db.GetString("TemplateName"),
                            Description = db.GetString("Description"),
                            Category = db.GetString("Category"),
                            IsAutoAssignEnabled = db.GetBoolean("IsAutoAssignEnabled"),
                            AutoAssignServiceTypes = db.GetString("AutoAssignServiceTypes"),
                            FormStructure = db.GetString("FormStructure"),
                            RequireSignature = db.GetBoolean("RequireSignature"),
                            RequireTip = db.GetBoolean("RequireTip"),
                            IsActive = db.GetBoolean("IsActive"),
                            CreatedBy = db.GetString("CreatedBy"),
                            CreatedDateTime = db.GetDateTime("CreatedDateTime"),
                            UpdatedBy = db.GetString("UpdatedBy"),
                            UpdatedDateTime = db.Reader["UpdatedDateTime"] as DateTime?
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form templates: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return templates;
        }

        public FormTemplate GetTemplate(int templateId, string companyId)
        {
            FormTemplate template = null;

            try
            {
                db.Init("sp_Forms_GetTemplate");
                db.AddParameter("@TemplateId", templateId, SqlDbType.Int);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                if (db.Execute() && db.Reader.Read())
                {
                    template = new FormTemplate
                    {
                        Id = db.GetInt("Id"),
                        CompanyID = db.GetString("CompanyID"),
                        TemplateName = db.GetString("TemplateName"),
                        Description = db.GetString("Description"),
                        Category = db.GetString("Category"),
                        IsAutoAssignEnabled = db.GetBoolean("IsAutoAssignEnabled"),
                        AutoAssignServiceTypes = db.GetString("AutoAssignServiceTypes"),
                        FormStructure = db.GetString("FormStructure"),
                        RequireSignature = db.GetBoolean("RequireSignature"),
                        RequireTip = db.GetBoolean("RequireTip"),
                        IsActive = db.GetBoolean("IsActive"),
                        CreatedBy = db.GetString("CreatedBy"),
                        CreatedDateTime = db.GetDateTime("CreatedDateTime"),
                        UpdatedBy = db.GetString("UpdatedBy"),
                        UpdatedDateTime = db.Reader["UpdatedDateTime"] as DateTime?
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form template: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return template;
        }

        public int SaveTemplate(FormTemplate template)
        {
            try
            {
                db.Init("sp_Forms_SaveTemplate");
                db.AddParameter("@Id", template.Id, SqlDbType.Int);
                db.AddParameter("@CompanyID", template.CompanyID, SqlDbType.VarChar);
                db.AddParameter("@TemplateName", template.TemplateName, SqlDbType.VarChar);
                db.AddParameter("@Description", template.Description ?? "", SqlDbType.VarChar);
                db.AddParameter("@Category", template.Category ?? "", SqlDbType.VarChar);
                db.AddParameter("@IsAutoAssignEnabled", template.IsAutoAssignEnabled, SqlDbType.Bit);
                db.AddParameter("@AutoAssignServiceTypes", template.AutoAssignServiceTypes ?? "", SqlDbType.NVarChar);
                db.AddParameter("@FormStructure", template.FormStructure ?? "", SqlDbType.NVarChar);
                db.AddParameter("@RequireSignature", template.RequireSignature, SqlDbType.Bit);
                db.AddParameter("@RequireTip", template.RequireTip, SqlDbType.Bit);
                db.AddParameter("@IsActive", template.IsActive, SqlDbType.Bit);
                db.AddParameter("@CreatedBy", template.CreatedBy ?? "", SqlDbType.VarChar);
                db.AddParameter("@UpdatedBy", template.UpdatedBy ?? "", SqlDbType.VarChar);
                db.AddParameter("@NewId", 0, 50, SqlDbType.Int);

                if (db.ExecuteCommand())
                {
                    return Convert.ToInt32(db.Command.Parameters["@NewId"].Value);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving form template: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return 0;
        }

        #endregion

        #region Auto-Assignment Logic

        public List<FormTemplate> GetAutoAssignedForms(string serviceType, string companyId)
        {
            var templates = new List<FormTemplate>();
            Database db = new Database();

            try
            {
                db.Init("sp_Forms_GetAutoAssignedTemplates");
                db.AddParameter("@ServiceType", serviceType, SqlDbType.VarChar);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                if (db.Execute())
                {
                    while (db.Reader.Read())
                    {
                        templates.Add(new FormTemplate
                        {
                            Id = db.GetInt("Id"),
                            CompanyID = db.GetString("CompanyID"),
                            TemplateName = db.GetString("TemplateName"),
                            Description = db.GetString("Description"),
                            Category = db.GetString("Category"),
                            RequireSignature = db.GetBoolean("RequireSignature"),
                            RequireTip = db.GetBoolean("RequireTip"),
                            FormStructure = db.GetString("FormStructure")
                            // Note: Priority field is returned but not used in entity - used for ORDER BY in stored procedure
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving auto-assigned forms: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return templates;
        }

        #endregion

        #region Form Instances Management

        public int CreateFormInstance(FormInstance instance)
        {
            try
            {
                db.Init("sp_Forms_CreateInstance");
                db.AddParameter("@CompanyID", instance.CompanyID, SqlDbType.VarChar);
                db.AddParameter("@TemplateId", instance.TemplateId, SqlDbType.Int);
                db.AddParameter("@AppointmentId", instance.AppointmentId, SqlDbType.VarChar);
                db.AddParameter("@CustomerID", instance.CustomerID, SqlDbType.VarChar);
                db.AddParameter("@FormData", instance.FormData ?? "", SqlDbType.NVarChar);
                db.AddParameter("@Status", instance.Status ?? "Pending", SqlDbType.VarChar);
                db.AddParameter("@FilledBy", instance.FilledBy ?? "", SqlDbType.VarChar);
                db.AddParameter("@NewId", 0, 50, SqlDbType.Int);

                if (db.ExecuteCommand())
                {
                    int newId = Convert.ToInt32(db.Command.Parameters["@NewId"].Value);

                    // Log the creation
                    LogFormUsage(newId, instance.TemplateId, instance.AppointmentId, "Created", instance.FilledBy, instance.CompanyID);

                    return newId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating form instance: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return 0;
        }

        public bool UpdateFormInstance(FormInstance instance)
        {
            try
            {
                db.Init("sp_Forms_UpdateInstance");
                db.AddParameter("@Id", instance.Id, SqlDbType.Int);
                db.AddParameter("@FormData", instance.FormData ?? "", SqlDbType.NVarChar);
                db.AddParameter("@Status", instance.Status, SqlDbType.VarChar);
                db.AddParameter("@TechnicianSignature", instance.TechnicianSignature, SqlDbType.VarBinary);
                db.AddParameter("@CustomerSignature", instance.CustomerSignature, SqlDbType.VarBinary);
                db.AddParameter("@TipAmount", instance.TipAmount, SqlDbType.Decimal);
                db.AddParameter("@TipNotes", instance.TipNotes ?? "", SqlDbType.VarChar);
                db.AddParameter("@CompletedDateTime", instance.CompletedDateTime, SqlDbType.DateTime);
                db.AddParameter("@SubmittedDateTime", instance.SubmittedDateTime, SqlDbType.DateTime);
                db.AddParameter("@StoredFilePath", instance.StoredFilePath ?? "", SqlDbType.VarChar);
                db.AddParameter("@StoredFileName", instance.StoredFileName ?? "", SqlDbType.VarChar);

                return db.ExecuteCommand();
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating form instance: " + ex.Message);
            }
            finally
            {
                db.Close();
            }
        }

        public List<FormInstance> GetAppointmentForms(string appointmentId, string companyId)
        {
            var instances = new List<FormInstance>();

            string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
            db = new Database(connectionString);
            try
            {
                db.Init("sp_Forms_GetAppointmentForms");
                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.VarChar);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                if (db.Execute())
                {
                    while (db.Reader.Read())
                    {
                        var instance = new FormInstance
                        {
                            Id = db.GetInt("Id"),
                            CompanyID = db.GetString("CompanyID"),
                            TemplateId = db.GetInt("TemplateId"),
                            AppointmentId = db.GetString("AppointmentId"),
                            CustomerID = db.GetString("CustomerID"),
                            FormData = db.GetString("FormData"),
                            Status = db.GetString("Status"),
                            TipAmount = db.Reader["TipAmount"] as decimal?,
                            TipNotes = db.GetString("TipNotes"),
                            FilledBy = db.GetString("FilledBy"),
                            StartedDateTime = db.Reader["StartedDateTime"] as DateTime?,
                            CompletedDateTime = db.Reader["CompletedDateTime"] as DateTime?,
                            SubmittedDateTime = db.Reader["SubmittedDateTime"] as DateTime?,
                            IsSynced = db.GetBoolean("IsSynced"),
                            StoredFilePath = db.GetString("StoredFilePath"),
                            StoredFileName = db.GetString("StoredFileName")
                        };

                        // Try to get template name if available from joined query
                        try
                        {
                            instance.TemplateName = db.GetString("TemplateName");
                        }
                        catch
                        {
                            // If TemplateName column is not available, get it separately
                            var template = GetTemplate(instance.TemplateId, companyId);
                            instance.TemplateName = template?.TemplateName ?? $"Form #{instance.TemplateId}";
                        }

                        instances.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving appointment forms: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return instances;
        }

        #endregion

        #region Auto-Fill Logic

        public string GenerateAutoFilledFormData(int templateId, string appointmentId, string customerId, string companyId)
        {
            try
            {
                // Get template structure
                var template = GetTemplate(templateId, companyId);
                if (template == null || string.IsNullOrEmpty(template.FormStructure))
                    return "{}";

                // Get customer data
                var customerProcessor = new CustomerProcessor();
                var customer = customerProcessor.GetCustomerDetails(customerId, companyId);

                // Get appointment data
                var appointment = GetAppointmentData(appointmentId, companyId);

                // Parse form structure and auto-fill
                var formStructure = serializer.DeserializeObject(template.FormStructure);
                var autoFilledData = new Dictionary<string, object>();

                // Auto-fill logic based on field configuration
                if (formStructure is Dictionary<string, object> structure &&
                    structure.ContainsKey("fields") &&
                    structure["fields"] is object[] fields)
                {
                    foreach (var fieldObj in fields)
                    {
                        if (fieldObj is Dictionary<string, object> field &&
                            field.ContainsKey("autoFillSource") &&
                            !string.IsNullOrEmpty(field["autoFillSource"].ToString()))
                        {
                            string fieldName = field["name"].ToString();
                            string autoFillSource = field["autoFillSource"].ToString();

                            autoFilledData[fieldName] = GetAutoFillValue(autoFillSource, customer, appointment);
                        }
                    }
                }

                return serializer.Serialize(autoFilledData);
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating auto-filled form data: " + ex.Message);
            }
        }

        private object GetAutoFillValue(string source, CustomerEntity customer, dynamic appointment)
        {
            try
            {
                string[] parts = source.Split('.');
                if (parts.Length != 2) return "";

                string entity = parts[0].ToLower();
                string property = parts[1];

                switch (entity)
                {
                    case "customer":
                        return GetCustomerProperty(customer, property);
                    case "appointment":
                        return GetAppointmentProperty(appointment, property);
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        private object GetCustomerProperty(CustomerEntity customer, string property)
        {
            if (customer == null) return "";

            switch (property.ToLower())
            {
                case "firstname": return customer.FirstName ?? "";
                case "lastname": return customer.LastName ?? "";
                case "fullname": return $"{customer.FirstName} {customer.LastName}".Trim();
                case "email": return customer.Email ?? "";
                case "phone": return customer.Phone ?? "";
                case "mobile": return customer.Mobile ?? "";
                case "address1": return customer.Address1 ?? "";
                case "address2": return customer.Address2 ?? "";
                case "city": return customer.City ?? "";
                case "state": return customer.State ?? "";
                case "zipcode": return customer.ZipCode ?? "";
                case "companyname": return customer.CompanyName ?? "";
                default: return "";
            }
        }

        private object GetAppointmentProperty(dynamic appointment, string property)
        {
            if (appointment == null) return "";

            try
            {
                switch (property.ToLower())
                {
                    case "servicetype": return appointment.ServiceType ?? "";
                    case "requestdate": return appointment.RequestDate ?? "";
                    case "timeslot": return appointment.TimeSlot ?? "";
                    case "resourcename": return appointment.ResourceName ?? "";
                    case "status": return appointment.Status ?? "";
                    case "note": return appointment.Note ?? "";
                    default: return "";
                }
            }
            catch
            {
                return "";
            }
        }

        private dynamic GetAppointmentData(string appointmentId, string companyId)
        {
            try
            {
                db.Init("sp_Appointments_GetById");
                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.VarChar);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                if (db.Execute() && db.Reader.Read())
                {
                    return new
                    {
                        AppoinmentId = db.GetString("AppoinmentId"),
                        ServiceType = db.GetString("ServiceType"),
                        RequestDate = db.GetString("RequestDate"),
                        TimeSlot = db.GetString("TimeSlot"),
                        ResourceName = db.GetString("ResourceName"),
                        Status = db.GetString("Status"),
                        Note = db.GetString("Note")
                    };
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - return null so auto-fill continues with available data
            }
            finally
            {
                db.Close();
            }

            return null;
        }

        #endregion

        #region Usage Logging

        public void LogFormUsage(int formInstanceId, int templateId, string appointmentId, string action, string performedBy, string companyId, string notes = "")
        {
            try
            {
                db.Init("sp_Forms_LogUsage");
                db.AddParameter("@FormInstanceId", formInstanceId, SqlDbType.Int);
                db.AddParameter("@TemplateId", templateId, SqlDbType.Int);
                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.VarChar);
                db.AddParameter("@Action", action, SqlDbType.VarChar);
                db.AddParameter("@PerformedBy", performedBy, SqlDbType.VarChar);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);
                db.AddParameter("@Notes", notes, SqlDbType.VarChar);
                db.AddParameter("@DeviceInfo", GetDeviceInfo(), SqlDbType.VarChar);

                db.ExecuteCommand();
            }
            catch
            {
                // Don't throw on logging errors to avoid disrupting main workflow
            }
            finally
            {
                db.Close();
            }
        }

        private string GetDeviceInfo()
        {
            try
            {
                var request = System.Web.HttpContext.Current?.Request;
                if (request != null)
                {
                    return $"IP: {request.UserHostAddress}, UserAgent: {request.UserAgent}";
                }
            }
            catch { }

            return "Unknown";
        }

        public List<FormUsageLog> GetUsageLog(string companyId, int? templateId = null, string appointmentId = null)
        {
            var logs = new List<FormUsageLog>();

            try
            {
                db.Init("sp_Forms_GetUsageLog");
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);
                db.AddParameter("@TemplateId", templateId, SqlDbType.Int);
                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.VarChar);

                if (db.Execute())
                {
                    while (db.Reader.Read())
                    {
                        logs.Add(new FormUsageLog
                        {
                            Id = db.GetInt("Id"),
                            CompanyID = db.GetString("CompanyID"),
                            FormInstanceId = db.GetInt("FormInstanceId"),
                            TemplateId = db.GetInt("TemplateId"),
                            AppointmentId = db.GetString("AppointmentId"),
                            Action = db.GetString("Action"),
                            PerformedBy = db.GetString("PerformedBy"),
                            ActionDateTime = db.GetDateTime("ActionDateTime"),
                            Notes = db.GetString("Notes"),
                            DeviceInfo = db.GetString("DeviceInfo")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving usage log: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return logs;
        }

        #endregion

        public string GetFormStructureFromResponse(int templateId, string companyId, int appointmentId, int customerId)
        {
            string formStructure = null;

            try
            {
                string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
                db = new Database(connectionString);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT FormStructure FROM myServiceJobs.dbo.FormResponse 
                        WHERE TemplateId = @TemplateId AND AppointmentID = @AppointmentID
                        AND CustomerId = @CustomerId AND CompanyID = @CompanyID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TemplateId", templateId);
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        cmd.Parameters.AddWithValue("@AppointmentID", appointmentId);
                        cmd.Parameters.AddWithValue("@CustomerId", customerId);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                formStructure = reader["FormStructure"].ToString();
                             
                            }
                            else
                            {
                                return "";
                            }
                        }
                    }
                }
                 
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form structure: " + ex.Message);
            }
            finally
            {
                db.Close();
            }

            return formStructure;
        }
    } 
}