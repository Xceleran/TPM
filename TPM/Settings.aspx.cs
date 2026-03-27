using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using FSM.Helper;

namespace FSM
{
    public class Status
    {
        public int StatusID { get; set; }
        public string StatusName { get; set; }
    }
    public partial class Settings : Page
    {

        string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["CompanyID"] == null)
                {
                    Response.Redirect("~/Dashboard.aspx", true);
                }
                else
                {
                    string CompanyID = Session["CompanyID"].ToString();
                    hdCompanyID.Value = CompanyID;
                    LoadSMSSettings();
                }
            }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Status> GetStatuses()
        {
            var statuses = new List<Status>();
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];

            string sql = @"
            SELECT [StatusID], [StatusName] 
            FROM [msSchedulerV3].[dbo].[tbl_Status] 
            WHERE CompanyID=@CompanyID AND StatusName != 'FA-ID Sent'
            ORDER BY 
                CASE StatusName
                    WHEN 'Pending' THEN 1
                    WHEN 'Scheduled' THEN 2
                    WHEN 'Dispatched' THEN 3
                    WHEN 'In-Route' THEN 4
                    WHEN 'Arrived' THEN 5
                    WHEN 'Completed' THEN 6
                    WHEN 'Closed' THEN 7
                    WHEN 'On-Hold' THEN 8
                    WHEN 'Cancelled' THEN 9
                    ELSE 99
                END;";

            try
            {
                using (var con = new SqlConnection(connStrJobs))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    con.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            statuses.Add(new Status
                            {
                                StatusID = Convert.ToInt32(dr["StatusID"]),
                                StatusName = dr["StatusName"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR in GetStatuses: " + ex.ToString());
            }
            return statuses;
        }
        #region SMS Settings Methods

        private void LoadSMSSettings()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT * FROM tbl_FSMSMSSettings WHERE CompanyId = @CompanyId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        PendingYN.Checked = dr["PendingYN"] != DBNull.Value && Convert.ToBoolean(dr["PendingYN"]);
                        txtPending.Value = dr["PendingText"] != DBNull.Value ? dr["PendingText"].ToString() : "";

                        CancelledYN.Checked = dr["CancelledYN"] != DBNull.Value && Convert.ToBoolean(dr["CancelledYN"]);
                        txtCancelled.Value = dr["CancelledText"] != DBNull.Value ? dr["CancelledText"].ToString() : "";

                        ProgressYN.Checked = dr["ProgressYN"] != DBNull.Value && Convert.ToBoolean(dr["ProgressYN"]);
                        txtProgress.Value = dr["ProgressText"] != DBNull.Value ? dr["ProgressText"].ToString() : "";

                        ScheduledYN.Checked = dr["ScheduledYN"] != DBNull.Value && Convert.ToBoolean(dr["ScheduledYN"]);
                        txtScheduled.Value = dr["ScheduledText"] != DBNull.Value ? dr["ScheduledText"].ToString() : "";

                        ClosedYN.Checked = dr["ClosedYN"] != DBNull.Value && Convert.ToBoolean(dr["ClosedYN"]);
                        txtClosed.Value = dr["ClosedText"] != DBNull.Value ? dr["ClosedText"].ToString() : "";

                        CompletedYN.Checked = dr["CompletedYN"] != DBNull.Value && Convert.ToBoolean(dr["CompletedYN"]);
                        txtCompleted.Value = dr["CompletedText"] != DBNull.Value ? dr["CompletedText"].ToString() : "";
                    }
                }
            }
        }

        protected void SubmitData_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();


                    string deleteQuery = "DELETE FROM tbl_FSMSMSSettings WHERE CompanyId = @CompanyId";
                    SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);
                    deleteCmd.ExecuteNonQuery();


                    string insertQuery = @"
                INSERT INTO tbl_FSMSMSSettings
                (CompanyId, PendingYN, PendingText, CancelledYN, CancelledText, ProgressYN, ProgressText, 
                 ScheduledYN, ScheduledText, ClosedYN, ClosedText, CompletedYN, CompletedText, 
                  CreateDate)
                VALUES
                (@CompanyId, @PendingYN, @PendingText, @CancelledYN, @CancelledText, @ProgressYN, @ProgressText, 
                 @ScheduledYN, @ScheduledText, @ClosedYN, @ClosedText, @CompletedYN, @CompletedText, 
                  GETDATE()) ";

                    SqlCommand cmd = new SqlCommand(insertQuery, conn);

                    cmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);


                    cmd.Parameters.AddWithValue("@PendingYN", PendingYN.Checked);
                    cmd.Parameters.AddWithValue("@PendingText", string.IsNullOrEmpty(txtPending.Value) ? (object)DBNull.Value : txtPending.Value);

                    cmd.Parameters.AddWithValue("@CancelledYN", CancelledYN.Checked);
                    cmd.Parameters.AddWithValue("@CancelledText", string.IsNullOrEmpty(txtCancelled.Value) ? (object)DBNull.Value : txtCancelled.Value);

                    cmd.Parameters.AddWithValue("@ProgressYN", ProgressYN.Checked);
                    cmd.Parameters.AddWithValue("@ProgressText", string.IsNullOrEmpty(txtProgress.Value) ? (object)DBNull.Value : txtProgress.Value);

                    cmd.Parameters.AddWithValue("@ScheduledYN", ScheduledYN.Checked);
                    cmd.Parameters.AddWithValue("@ScheduledText", string.IsNullOrEmpty(txtScheduled.Value) ? (object)DBNull.Value : txtScheduled.Value);

                    cmd.Parameters.AddWithValue("@ClosedYN", ClosedYN.Checked);
                    cmd.Parameters.AddWithValue("@ClosedText", string.IsNullOrEmpty(txtClosed.Value) ? (object)DBNull.Value : txtClosed.Value);

                    cmd.Parameters.AddWithValue("@CompletedYN", CompletedYN.Checked);
                    cmd.Parameters.AddWithValue("@CompletedText", string.IsNullOrEmpty(txtCompleted.Value) ? (object)DBNull.Value : txtCompleted.Value);

                    cmd.ExecuteNonQuery();
                }

                string success = "Swal.fire('Saved!', 'Your SMS settings have been saved successfully.', 'success')" +
                                 ".then(function(){ window.location.replace('Settings.aspx'); });";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SuccessAlertScript", success, true);
            }
            catch (Exception ex)
            {
                string error = $"Swal.fire('Error!', '{ex.Message.Replace("'", "\\'")}', 'error');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", error, true);
            }
        }

        #endregion

        #region Custom Fields WebMethods

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetFields()
        {
            try
            {
                string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
                var fields = new List<object>();
                string sql = "SELECT FieldID, FieldName, FieldType, FieldOptions, IsActive FROM [msSchedulerV3].[dbo].[CustomFields] ORDER BY FieldName";

                using (var con = new SqlConnection(connStrJobs))
                using (var cmd = new SqlCommand(sql, con))
                {
                    con.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            fields.Add(new
                            {
                                FieldId = dr["FieldID"],
                                FieldName = dr["FieldName"].ToString(),
                                FieldType = dr["FieldType"].ToString(),
                                Options = dr["FieldOptions"]?.ToString() ?? "[]",
                                IsActive = Convert.ToBoolean(dr["IsActive"])
                            });
                        }
                    }
                }
                return new { success = true, data = fields };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AJAX Error in GetFields: " + ex.Message);
                return new { success = false, message = "Server error while fetching fields: " + ex.Message };
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object Save_TPMCommunicationSettings(CommunicationSettings communicationSettings)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();

            try
            {
                string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
                using (var con = new SqlConnection(connStrJobs))
                {
                    con.Open();
                    SqlCommand cmd;
                    
                        cmd = new SqlCommand(
                            @"Delete From [msSchedulerV3].[dbo].[tbl_TPMCommunicationSettings] where CompanyID=@CompanyID and messageType =@messageType;
                            INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMCommunicationSettings] ([CompanyID], StatusName,messageType, SendEmail, [SendSMS], [EmailTemplate],emailSubject, [SMSTemplate]) 
                              VALUES (@CompanyID, @StatusName,@messageType, @SendEmail, @SendSMS, @EmailTemplate,@emailSubject, @SMSTemplate)", con);
                   
                    cmd.Parameters.AddWithValue("@CompanyID", companyId.Trim());
                    cmd.Parameters.AddWithValue("@StatusName", communicationSettings.triggerStatus);
                    cmd.Parameters.AddWithValue("@messageType", communicationSettings.messageType);
                    
                    cmd.Parameters.AddWithValue("@SendEmail", communicationSettings.emailEnabled);
                    cmd.Parameters.AddWithValue("@SendSMS", communicationSettings.smsEnabled);
                    cmd.Parameters.AddWithValue("@EmailTemplate", communicationSettings.emailContent);
                    cmd.Parameters.AddWithValue("@EmailSubject", communicationSettings.emailSubject);
                    cmd.Parameters.AddWithValue("@SMSTemplate", communicationSettings.smsContent);
                    cmd.ExecuteNonQuery();
                }
                return new { success = true, message = "Field saved successfully." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AJAX Error in SaveField: " + ex.Message);
                return new { success = false, message = "An error occurred while saving the field." };
            }
        }
      

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static CommunicationSettings loadTemplateData(CommunicationSettings communicationSettings)
        {
            var _CommunicationSettings = new CommunicationSettings();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
          
            try
            {
              
                string strSQL = @"SELECT * FROM [msSchedulerV3].dbo.tbl_TPMCommunicationSettings WHERE CompanyID=@companyid AND messageType=@messageType";
                DataTable dataTable = new DataTable();
                db.Open();
              
                db.AddParameter("@companyid", companyid, SqlDbType.NVarChar);
                db.AddParameter("@messageType", communicationSettings.messageType, SqlDbType.NVarChar);
                db.ExecuteParam(strSQL, out dataTable);
                db.Close();

                if (dataTable.Rows.Count > 0)
                {
                    foreach (DataRow dr in dataTable.Rows)
                    {
                        _CommunicationSettings = new CommunicationSettings
                        {
                
                            emailContent = dr["EmailTemplate"].ToString(),
                            emailSubject = dr["emailSubject"].ToString(),
                            emailEnabled = dr["SendEmail"].ToString().ToLower(),
                            smsContent = dr["SMSTemplate"].ToString(),
                            smsEnabled = dr["SendSMS"].ToString().ToLower()
                        };
                    }
                }
               
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
                return _CommunicationSettings;
            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return _CommunicationSettings;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveField(int fieldId, string fieldName, string fieldType, string fieldOptions, bool isActive)
        {
            try
            {
                string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
                using (var con = new SqlConnection(connStrJobs))
                {
                    con.Open();
                    SqlCommand cmd;
                    if (fieldId == 0)
                    {
                        cmd = new SqlCommand(
                            @"INSERT INTO [msSchedulerV3].[dbo].[CustomFields] (FieldName, FieldType, FieldOptions, AppliesTo, IsActive, CreatedDateTime) 
                              VALUES (@FieldName, @FieldType, @FieldOptions, @AppliesTo, @IsActive, GETDATE())", con);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            @"UPDATE [msSchedulerV3].[dbo].[CustomFields] 
                              SET FieldName=@FieldName, FieldType=@FieldType, FieldOptions=@FieldOptions, AppliesTo=@AppliesTo, IsActive=@IsActive 
                              WHERE FieldID=@FieldID", con);
                        cmd.Parameters.AddWithValue("@FieldID", fieldId);
                    }
                    cmd.Parameters.AddWithValue("@FieldName", fieldName.Trim());
                    cmd.Parameters.AddWithValue("@FieldType", fieldType);
                    cmd.Parameters.AddWithValue("@FieldOptions", string.IsNullOrWhiteSpace(fieldOptions) ? (object)DBNull.Value : fieldOptions);
                    cmd.Parameters.AddWithValue("@AppliesTo", "Job");
                    cmd.Parameters.AddWithValue("@IsActive", isActive);
                    cmd.ExecuteNonQuery();
                }
                return new { success = true, message = "Field saved successfully." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AJAX Error in SaveField: " + ex.Message);
                return new { success = false, message = "An error occurred while saving the field." };
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object DeleteField(int fieldId)
        {
            try
            {
                string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
                using (var con = new SqlConnection(connStrJobs))
                {
                    con.Open();
                    var cmd = new SqlCommand("DELETE FROM [msSchedulerV3].[dbo].[AppointmentCustomFields] WHERE FieldID=@FieldID; DELETE FROM [msSchedulerV3].[dbo].[CustomFields] WHERE FieldID=@FieldID;", con);
                    cmd.Parameters.AddWithValue("@FieldID", fieldId);
                    cmd.ExecuteNonQuery();
                }
                return new { success = true, message = "Field deleted successfully." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AJAX Error in DeleteField: " + ex.Message);
                return new { success = false, message = "An error occurred while deleting the field." };
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object ToggleFieldActive(int fieldId)
        {
            try
            {
                string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
                using (var con = new SqlConnection(connStrJobs))
                {
                    con.Open();
                    var cmd = new SqlCommand("UPDATE [msSchedulerV3].[dbo].[CustomFields] SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE FieldID=@FieldID", con);
                    cmd.Parameters.AddWithValue("@FieldID", fieldId);
                    cmd.ExecuteNonQuery();
                }
                return new { success = true, message = "Field status updated." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AJAX Error in ToggleFieldActive: " + ex.Message);
                return new { success = false, message = "An error occurred while updating the field status." };
            }
        }

        #endregion
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetFaMasterTemplate()
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
            {
                return new { success = false, message = "Your session has expired. Please log in again." };
            }

            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"];
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                    string sql = "SELECT EnableSms, EnableMms, StandardContent, DaysBeforeAppointment FROM tbl_FaMessageMasterTemplate WHERE CompanyID = @CompanyID";
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                return new
                                {
                                    success = true,
                                    data = new
                                    {
                                        enableSms = Convert.ToBoolean(dr["EnableSms"]),
                                        enableMms = Convert.ToBoolean(dr["EnableMms"]),
                                        standardContent = dr["StandardContent"].ToString(),
                                        daysBeforeAppointment = Convert.ToInt32(dr["DaysBeforeAppointment"])
                                    }
                                };
                            }
                            else
                            {
                                return new
                                {
                                    success = true,
                                    data = new
                                    {
                                        enableSms = true,
                                        enableMms = false,
                                        standardContent = "", // Default value
                                        daysBeforeAppointment = 0 // Default value
                                    }
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR in GetFaMasterTemplate: " + ex.ToString());
                return new { success = false, message = "An error occurred while loading the template." };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveFaMasterTemplate(bool enableSms, bool enableMms, string standardContent, int daysBeforeAppointment)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
            {
                return new { success = false, message = "Your session has expired. Please log in again." };
            }

            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"];
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();

                    // This SQL statement performs an "UPSERT" (Update or Insert)
                    string sql = @"
                        IF EXISTS (SELECT 1 FROM tbl_FaMessageMasterTemplate WHERE CompanyID = @CompanyID)
                        BEGIN
                            -- Update existing record
                            UPDATE tbl_FaMessageMasterTemplate
                            SET EnableSms = @EnableSms,
                                EnableMms = @EnableMms,
                                StandardContent = @StandardContent,
                                DaysBeforeAppointment = @DaysBeforeAppointment,
                                LastModifiedDate = GETDATE()
                            WHERE CompanyID = @CompanyID;
                        END
                        ELSE
                        BEGIN
                            -- Insert new record
                            INSERT INTO tbl_FaMessageMasterTemplate (CompanyID, EnableSms, EnableMms, StandardContent, DaysBeforeAppointment, LastModifiedDate)
                            VALUES (@CompanyID, @EnableSms, @EnableMms, @StandardContent, @DaysBeforeAppointment, GETDATE());
                        END";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        cmd.Parameters.AddWithValue("@EnableSms", enableSms);
                        cmd.Parameters.AddWithValue("@EnableMms", enableMms);
                        cmd.Parameters.AddWithValue("@StandardContent", standardContent ?? ""); // Ensure it's not null
                        cmd.Parameters.AddWithValue("@DaysBeforeAppointment", daysBeforeAppointment);

                        cmd.ExecuteNonQuery();
                    }
                }
                return new { success = true, message = "Master template saved successfully." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR in SaveFaMasterTemplate: " + ex.ToString());
                return new { success = false, message = "An error occurred while saving the template." };
            }
        }

        public class FaProfile
        {
            public int ProfileID { get; set; }
            public int ResourceID { get; set; } 
            public string FaName { get; set; }
            public string MobilePhone { get; set; }
            public string CustomContent { get; set; }
            public string PictureUrl { get; set; }
            public bool IsActive { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetFaProfiles()
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
            {
                return new { success = false, message = "Session expired." };
            }

            var profiles = new List<FaProfile>();
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"]; 
                using (var con = new SqlConnection(connStr))
                {
                    string sql = "SELECT ProfileID, ResourceID, FaName, MobilePhone, CustomContent, PictureUrl, IsActive FROM tbl_FaProfile WHERE CompanyID = @CompanyID AND IsActive = 1 ORDER BY FaName";
                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        con.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                profiles.Add(new FaProfile
                                {
                                    ProfileID = Convert.ToInt32(dr["ProfileID"]),
                                    ResourceID = dr["ResourceID"] != DBNull.Value ? Convert.ToInt32(dr["ResourceID"]) : 0,
                                    FaName = dr["FaName"].ToString(),
                                    MobilePhone = dr["MobilePhone"].ToString(),
                                    CustomContent = dr["CustomContent"].ToString(),
                                    PictureUrl = dr["PictureUrl"].ToString(),
                                    IsActive = Convert.ToBoolean(dr["IsActive"])
                                });
                            }
                        }
                    }
                }
                return new { success = true, data = profiles };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR in GetFaProfiles: " + ex.ToString());
                return new { success = false, message = "An error occurred while loading profiles." };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SaveFaProfile(int profileId, string faName, string mobilePhone, string customContent)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId))
            {
                return new { success = false, message = "Session expired." };
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(faName))
            {
                return new { success = false, message = "Field Agent Name is required." };
            }

            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"]; 
                using (var con = new SqlConnection(connStr))
                {
                    con.Open();
                    string sql;
                    if (profileId == 0) 
                    {
                        sql = @"INSERT INTO tbl_FaProfile (CompanyID, FaName, MobilePhone, CustomContent, IsActive, PictureUrl) 
                                OUTPUT INSERTED.ProfileID, INSERTED.FaName, INSERTED.MobilePhone, INSERTED.CustomContent, INSERTED.PictureUrl, INSERTED.IsActive
                                VALUES (@CompanyID, @FaName, @MobilePhone, @CustomContent, 1, @PictureUrl);";
                    }
                    else 
                    {
                        sql = @"UPDATE tbl_FaProfile 
                                SET FaName = @FaName, MobilePhone = @MobilePhone, CustomContent = @CustomContent 
                                WHERE ProfileID = @ProfileID AND CompanyID = @CompanyID;
                                SELECT ProfileID, FaName, MobilePhone, CustomContent, PictureUrl, IsActive FROM tbl_FaProfile WHERE ProfileID = @ProfileID;";
                    }

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        cmd.Parameters.AddWithValue("@FaName", faName);
                        cmd.Parameters.AddWithValue("@MobilePhone", mobilePhone ?? "");
                        cmd.Parameters.AddWithValue("@CustomContent", customContent ?? "");

                        if (profileId == 0)
                        {
                            cmd.Parameters.AddWithValue("@PictureUrl", "https://via.placeholder.com/50");
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ProfileID", profileId);
                        }
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                var savedProfile = new FaProfile
                                {
                                    ProfileID = Convert.ToInt32(dr["ProfileID"]),
                                    FaName = dr["FaName"].ToString(),
                                    MobilePhone = dr["MobilePhone"].ToString(),
                                    CustomContent = dr["CustomContent"].ToString(),
                                    PictureUrl = dr["PictureUrl"].ToString(),
                                    IsActive = Convert.ToBoolean(dr["IsActive"])
                                };
                                return new { success = true, data = savedProfile };
                            }
                        }
                    }
                }
                return new { success = false, message = "Could not save the profile." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR in SaveFaProfile: " + ex.ToString());
                return new { success = false, message = "A server error occurred while saving the profile." };
            }
        }


    }
    public class CommunicationSettings
    {
        public string messageType { get; set; }
        public string triggerStatus { get; set; }
        public string emailEnabled { get; set; }
        public string emailContent { get; set; }
        public string emailSubject { get; set; }
        
        public string smsEnabled { get; set; }
        public string smsContent { get; set; }
        public string ynEnabled { get; set; }
        public string yesResponse { get; set; }
        public string noResponse { get; set; }
        public string sendFaId { get; set; }
   

    }
}

