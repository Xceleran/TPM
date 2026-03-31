using FSM.Entity.Customer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Web;
using System.Web.Http.Results;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Configuration;


namespace FSM
{
    public partial class Customer : System.Web.UI.Page
    {
        string CompanyID = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("Dashboard.aspx");
            }
             SetLaunchCecUrl();
            if (!IsPostBack)
            {
                CompanyID = Session["CompanyID"].ToString();
                LoadData();
            }
        }
        // In Customer.aspx.cs

        private void SetLaunchCecUrl()
        {
            LaunchCecButton.Visible = false;
            return;
            try
            {
                string userId = Session["LoginUser"] as string;
                string companyId = Session["CompanyID"] as string;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                {
                    LaunchCecButton.Visible = false;
                    return;
                }

                string sessionString = $"{userId}|{companyId}";
                string newGuid = Guid.NewGuid().ToString();

                string sql = $"INSERT INTO XinatorCentral.dbo.tbl_Login (SessionGuid, SessionString) VALUES ('{newGuid}', '{sessionString}')";

                Database db = new Database();
                db.UpdateSql(sql);

                string accountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"];
                if (string.IsNullOrEmpty(accountsUrl))
                {
                    LaunchCecButton.Visible = false;
                    return;
                }

                string cecBaseUrl = accountsUrl.Replace("AccountsXinator", "cec");
                string redirectUrl = HttpUtility.UrlEncode("/cec/customers.aspx");

                LaunchCecButton.HRef = $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating CEC SSO URL: " + ex.Message);
                LaunchCecButton.Visible = false;
            }
        }

        public void LoadData()
        {
            string companyid = Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                string sql = @"SELECT [StatusID], [StatusName] 
                      FROM [msSchedulerV3].[dbo].[tbl_Status] 
                      WHERE CompanyID = @CompanyID
                      ORDER BY 
                          CASE StatusName
                              WHEN 'Pending' THEN 1
                              WHEN 'Scheduled' THEN 2
                              WHEN 'Dispatched' THEN 3
                              WHEN 'In-Route' THEN 4
                              WHEN 'FA-ID Sent' THEN 5
                              WHEN 'Arrived' THEN 6
                              WHEN 'Completed' THEN 7
                              WHEN 'Closed' THEN 8
                              WHEN 'On-Hold' THEN 9
                              WHEN 'Cancelled' THEN 10
                              ELSE 99
                          END;";

                DataTable _status = new DataTable();
                db.Open();
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out _status);
                db.Close();

                // Change "Scheduled" to "Confirmed" for display purposes
                foreach (DataRow row in _status.Rows)
                {
                    if (row["StatusName"].ToString() == "Scheduled")
                    {
                        row["StatusName"] = "Confirmed";
                    }
                }

                if (_status.Rows.Count > 0)
                {
                    statusFilter.DataSource = _status;
                    statusFilter.DataTextField = "StatusName";
                    statusFilter.DataValueField = "StatusName";
                    statusFilter.DataBind();
                    statusFilter.Items.Insert(0, new ListItem("All Statuses", ""));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading status data: {ex.Message}");
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string LoadCustomers(int draw, int start, int length, string searchValue, string sortColumn, string sortDirection, string cslViewFilter = "all", bool hideNoAppointments = false)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            int totalRecords = 0;
            Database db = new Database();
            DataTable dt = new DataTable();
            var customers = new List<CustomerEntity>();
            try
            {
              
      

                string finalSql = $@"
                  
                        SELECT
                            c.*,
                            apptData.StatusName,
                            apptData.LatestAppointmentID AS LatestAppointmentID -- Added LatestAppointmentID
                        FROM [msSchedulerV3].[dbo].[tbl_Customer] c
                        OUTER APPLY (
                            SELECT TOP 1
                                CASE
                                    WHEN a.Status = 'Deleted' THEN 'N/A'
                                    WHEN s.StatusName = 'Scheduled' THEN 'Confirmed'
                                    WHEN s.StatusName IS NOT NULL THEN s.StatusName
                                    ELSE a.Status
                                END AS StatusName,
                                a.ApptID AS LatestAppointmentID -- Select the Appointment ID
                            FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Status] s ON TRY_CAST(a.Status AS INT) = s.StatusID AND a.CompanyID = s.CompanyID
                            WHERE a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
                            ORDER BY a.ApptDateTime DESC
                        ) AS apptData
                        WHERE c.warrentycompanyid > 0 and  c.CompanyID = '{companyid}'
                   
                     ";

                // Apply searchValue filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    finalSql += $" AND (cst.FirstName LIKE '%{searchValue}%' OR cst.LastName LIKE '%{searchValue}%' OR cst.Email LIKE '%{searchValue}%')";
                }

                // Apply additional filters
                if (cslViewFilter == "current")
                {
                    finalSql += @"AND EXISTS (
                        SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                        WHERE a.CustomerID = c.CustomerID
                          AND a.CompanyID = c.CompanyID
                          AND a.Status NOT IN ('Deleted', 'Closed', 'Cancelled')
                          AND a.ApptDateTime >= CAST(GETDATE() AS DATE)
                    ) ";
                }
                if (hideNoAppointments)
                {
                    finalSql += " and apptData.LatestAppointmentID  > 0 ";
                }
                if (sortColumn == "fullname")
                {
                    sortColumn = "FirstName";
                }
                finalSql += $" ORDER BY {sortColumn} {sortDirection} OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY;";
            
                DataSet dataSet = db.Get_DataSet(finalSql, companyid);
                dt = dataSet.Tables[0];

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        customers.Add(new CustomerEntity
                        {
                            CompanyID = dr["CompanyID"].ToString(),
                            CustomerID = dr["CustomerID"].ToString(),
                            BusinessID = Convert.ToInt32(dr["BusinessID"]),
                            CustomerGuid = dr["CustomerGuid"].ToString(),
                            Address1 = dr["Address1"].ToString(),
                            Address2 = dr["Address2"].ToString(),
                            FirstName = dr["FirstName"].ToString(),
                            //FirstName2 = dr["FirstName2"].ToString(),
                            LastName = dr["LastName"].ToString(),
                            fullname = dr["FirstName"].ToString() + ' ' + dr["LastName"].ToString(),
                            //LastName2 = dr["LastName2"].ToString(),
                            //Title = dr["Title"].ToString(),
                            //Title2 = dr["Title2"].ToString(),
                            JobTitle = dr["JobTitle"].ToString(),
                            JobTitle2 = dr["JobTitle2"].ToString(),
                            City = dr["City"].ToString(),
                            State = dr["State"].ToString(),
                            ZipCode = dr["ZipCode"].ToString(),
                            Phone = dr["Phone"].ToString(),
                            Mobile = dr["Mobile"].ToString(),
                            Email = dr["Email"].ToString(),
                            // Notes = dr["Notes"].ToString(),
                            CompanyName = dr["CompanyName"].ToString(),
                            //CompanyName2 = dr["CompanyName2"].ToString(),
                            //BusinessName = dr["BusinessName"].ToString(),
                            IsBusinessContact = Convert.ToBoolean(dr["IsBusinessContact"]),
                            IsPrimaryContact = Convert.ToBoolean(dr["IsPrimaryContact"]),
                            IsDealer = Convert.ToBoolean(dr["IsDealer"]),
                            DealerID = dr["DealerID"].ToString(),
                            CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"]),
                            CallPopAppId = dr["CallPopAppId"].ToString(),
                            QboId = dr["QboId"].ToString(),
                            CreatedCompanyID = dr["CreatedCompanyID"].ToString(),
                            StatusName = dr["StatusName"].ToString()
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCustomers method: {ex.Message}"); // ADDED for full exception logging
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = customers
            };

            return JsonConvert.SerializeObject(response);
        }
        public class CustomFieldData
        {
            public int FieldId { get; set; }
            public string Value { get; set; }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateAppointmentWithCustomFields(Appointment appointment, List<CustomFieldData> customFieldValues)
        {
            // Use the existing logic from Appointments.aspx or similar if available
            // For simplicity, we can call the SaveCustomFieldData method if it exists here
            if (customFieldValues != null)
            {
                SaveCustomFieldData(Convert.ToInt32(appointment.AppoinmentId), customFieldValues);
            }

            return UpdateAppointment(appointment);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomFieldData(int appointmentId, List<CustomFieldData> customFieldValues)
        {
            string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
            using (var con = new System.Data.SqlClient.SqlConnection(connStrJobs))
            {
                con.Open();
                var transaction = con.BeginTransaction();
                try
                {
                    string deleteSql = "DELETE FROM [msSchedulerV3].[dbo].[AppointmentCustomFields] WHERE AppointmentID = @AppointmentID";
                    using (var deleteCmd = new System.Data.SqlClient.SqlCommand(deleteSql, con, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@AppointmentID", appointmentId);
                        deleteCmd.ExecuteNonQuery();
                    }

                    if (customFieldValues != null)
                    {
                        foreach (var fieldData in customFieldValues)
                        {
                            if (string.IsNullOrEmpty(fieldData.Value) || fieldData.Value == "[]") continue;
                            string insertSql = @"INSERT INTO [msSchedulerV3].[dbo].[AppointmentCustomFields] (AppointmentID, FieldID, FieldValue, LastUpdated)
                                            VALUES (@AppointmentID, @FieldID, @FieldValue, GETDATE())";
                            using (var cmd = new System.Data.SqlClient.SqlCommand(insertSql, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@AppointmentID", appointmentId);
                                cmd.Parameters.AddWithValue("@FieldID", fieldData.FieldId);
                                cmd.Parameters.AddWithValue("@FieldValue", fieldData.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine("Error in SaveCustomFieldData: " + ex.Message);
                    return false;
                }
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateAppointment(Appointment appointment)
        {
            try
            {
                string companyId = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database();

                // Get old status to check for changes
                string oldStatus = "";
                db.Open();
                DataTable dtOld = new DataTable();
                string checkSql = "SELECT Status FROM [msSchedulerV3].[dbo].[tbl_Appointment] WHERE ApptID = @ApptID AND CompanyID = @CompanyID";
                db.AddParameter("@ApptID", appointment.AppoinmentId, SqlDbType.Int);
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                db.ExecuteParam(checkSql, out dtOld);
                if (dtOld.Rows.Count > 0) oldStatus = dtOld.Rows[0]["Status"].ToString();
                if (oldStatus == "0" || string.IsNullOrEmpty(oldStatus)) oldStatus = "Pending";
                db.Close();

                // Clear parameters from the status check query before adding update parameters
                db.Command.Parameters.Clear();

                string sql = @"UPDATE [msSchedulerV3].[dbo].[tbl_Appointment] SET
                            ServiceType = @ServiceType,
                            ResourceID = @ResourceID,
                            Status = @Status,
                            TicketStatus = @TicketStatus,
                            ApptDateTime = @ApptDateTime,
                            StartDateTime = @StartDateTime,
                            EndDateTime = @EndDateTime,
                            TimeSlot = @TimeSlot,
                            Hour = @Hour,
                            Minute = @Minute,
                            Note = @Note
                           WHERE ApptID = @ApptID AND CompanyID = @CompanyID";

                db.AddParameter("@ServiceType", appointment.ServiceType, SqlDbType.NVarChar);
                db.AddParameter("@ResourceID", appointment.ResourceID, SqlDbType.Int);
                string statusToSave = appointment.Status;
                if (statusToSave == "0" || string.IsNullOrEmpty(statusToSave)) statusToSave = "Pending";
                db.AddParameter("@Status", statusToSave, SqlDbType.NVarChar);
                db.AddParameter("@TicketStatus", appointment.TicketStatus ?? "", SqlDbType.NVarChar);
                db.AddParameter("@ApptDateTime", appointment.RequestDate, SqlDbType.DateTime);
                string dateFormatString = "MM/dd/yyyy hh:mm tt";
                object startDtValue = DBNull.Value;
                object endDtValue = DBNull.Value;
                if (!string.IsNullOrEmpty(appointment.StartDateTime))
                {
                    DateTime parsed;
                    if (DateTime.TryParseExact(appointment.StartDateTime, dateFormatString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
                        startDtValue = parsed;
                    else if (DateTime.TryParse(appointment.StartDateTime, out parsed))
                        startDtValue = parsed;
                }
                if (!string.IsNullOrEmpty(appointment.EndDateTime))
                {
                    DateTime parsed;
                    if (DateTime.TryParseExact(appointment.EndDateTime, dateFormatString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
                        endDtValue = parsed;
                    else if (DateTime.TryParse(appointment.EndDateTime, out parsed))
                        endDtValue = parsed;
                }
                db.AddParameter("@StartDateTime", startDtValue, SqlDbType.DateTime);
                db.AddParameter("@EndDateTime", endDtValue, SqlDbType.DateTime);
                db.AddParameter("@TimeSlot", appointment.TimeSlot, SqlDbType.NVarChar);
                db.AddParameter("@Hour", appointment.Hour, SqlDbType.Int);
                db.AddParameter("@Minute", appointment.Minute, SqlDbType.Int);
                db.AddParameter("@Note", appointment.Note, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", appointment.SiteId, SqlDbType.Int);
                db.AddParameter("@ApptID", appointment.AppoinmentId, SqlDbType.Int);
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);

                db.Open();
                bool success = db.UpdateSql(sql);

                if (success)
                {
                    // Log status history if changed
                    if (!string.IsNullOrEmpty(appointment.Status) && !string.IsNullOrEmpty(oldStatus) && oldStatus != appointment.Status)
                    {
                        try
                        {
                            string historySql = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_AppointmentStatusHistory]
                                               ([AppointmentId], [CompanyID], [PreviousStatus], [NewStatus], [StatusChangeDateTime], [ChangedBy], [Notes], [CreatedDateTime])
                                               VALUES
                                               (@ApptID, @CompanyID, @PreviousStatus, @NewStatus, GETDATE(), @ChangedBy, @Note, GETDATE())";

                            db.Command.Parameters.Clear();
                            db.AddParameter("@ApptID", appointment.AppoinmentId, SqlDbType.NVarChar);
                            db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                            db.AddParameter("@PreviousStatus", oldStatus, SqlDbType.NVarChar);
                            db.AddParameter("@NewStatus", appointment.Status, SqlDbType.NVarChar);
                            string currentUser = HttpContext.Current.Session["LoginUser"] as string ?? "System";
                            db.AddParameter("@ChangedBy", currentUser, SqlDbType.NVarChar);
                            db.AddParameter("@Note", (object)appointment.Note ?? DBNull.Value, SqlDbType.NVarChar);

                            db.UpdateSql(historySql);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error logging history in UpdateAppointment (Customer): {ex.Message}");
                        }
                    }
                }

                db.Close();
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in UpdateAppointment: " + ex.Message);
                return false;
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetServiceTypes()
        {
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            var list = new List<object>();

            try
            {
               
                DataTable dt = new DataTable();
                string sql = @"SELECT ServiceTypeID, ServiceName 
                               FROM tbl_ServiceType 
                                WHERE CompanyID = @CompanyID AND (Source != 'FSM')
                               ORDER BY ServiceName";
             //   db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                //db.ExecuteParam(sql, out dt);
                //db.Close();
                DataSet dataSet = db.Get_DataSet(sql, companyId);

                dt = dataSet.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        ServiceTypeID = row["ServiceTypeID"],
                        ServiceName = row["ServiceName"]
                    });
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetResources()
        {
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            var list = new List<object>();

            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT Id, Name 
                               FROM tbl_Resources 
                               WHERE CompanyID = @CompanyID 
                               ORDER BY Name";
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        Id = row["Id"],
                        Name = row["Name"]
                    });
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetAppointmentStatuses()
        {
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            var list = new List<object>();

            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT StatusID, StatusName, CalenderColor 
                               FROM tbl_Status 
                               WHERE CompanyID = @CompanyID AND StatusName != 'FA-ID Sent'
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
                                   END";
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                foreach (DataRow row in dt.Rows)
                {
                    string name = row["StatusName"].ToString();
                    if (name == "Scheduled") name = "Confirmed";
                    list.Add(new
                    {
                        StatusID = row["StatusID"],
                        StatusName = name,
                        Color = row["CalenderColor"] != DBNull.Value ? row["CalenderColor"].ToString() : "#3b82f6"
                    });
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return list;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetTicketStatuses()
        {
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            var list = new List<object>();

            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT StatusID, StatusName 
                               FROM tbl_TicketStatus 
                               WHERE CompanyID = @CompanyID";
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        StatusID = row["StatusID"],
                        StatusName = row["StatusName"]
                    });
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return list;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetActiveCustomFields(int apptId)
        {
            if (HttpContext.Current.Session["CompanyID"] == null)
            {
                return new List<object>();
            }

            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];
            var fields = new List<object>();

            string sql = @"
            SELECT 
                cf.FieldID, 
                cf.FieldName, 
                cf.FieldType, 
                cf.FieldOptions,
                acf.FieldValue
            FROM 
                [msSchedulerV3].[dbo].[CustomFields] cf
            LEFT JOIN 
                [msSchedulerV3].[dbo].[AppointmentCustomFields] acf 
                ON cf.FieldID = acf.FieldID AND acf.AppointmentID = @AppointmentID
            WHERE 
                cf.IsActive = 1 AND cf.CompanyId = @CompanyID
            ORDER BY 
                cf.FieldName";

            try
            {
                using (var con = new System.Data.SqlClient.SqlConnection(connStrJobs))
                using (var cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@AppointmentID", apptId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
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
                                Options = dr["FieldOptions"] == DBNull.Value ? "[]" : dr["FieldOptions"].ToString(),
                                Value = dr["FieldValue"] == DBNull.Value ? null : dr["FieldValue"].ToString()
                            });
                        }
                    }
                }
                return fields;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetActiveCustomFields: " + ex.ToString());
                return new List<object>();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetAppointmentDetails(string appointmentId)
        {
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            object result = null;

            try
            {
                //db.Open();
                DataTable dt = new DataTable();
                string sql = @"
                            SELECT 
                                appt.ApptID, 
                                appt.CustomerID,
                                appt.SiteID,
                                appt.Note, 
                                appt.TimeSlot, 
                                appt.Hour, 
                                appt.Minute,
                                CONVERT(VARCHAR(10), appt.ApptDateTime, 120) as RequestDate,
                                appt.ResourceID,
                                appt.ServiceType AS ServiceTypeName,
                                st.ServiceTypeID,
                                st.CalenderColor AS ServiceColor,
                                appt.Status AS StatusName,
                                sts.CalenderColor AS StatusColor,
                                appt.TicketStatus AS TicketStatusName,
                                appt.StartDateTime,
                                appt.EndDateTime,
                                CASE 
                                    WHEN appt.Status = 'Scheduled' THEN 'Confirmed'
                                    ELSE COALESCE(sts.StatusName, appt.Status) 
                                END AS DisplayStatus,
                                c.FirstName,
                                c.LastName,
                                c.CompanyName,
                                c.Address1 AS MainAddress,
                                c.City AS MainCity,
                                c.State AS MainState,
                                c.ZipCode AS MainZip,
                                c.Country AS MainCountry,
                                c.Phone AS MainPhone,
                                c.Mobile AS MainMobile,
                                c.Email AS MainEmail,
                                cs.SiteName,
                                cs.Address AS SiteAddress,
                                cs.City AS SiteCity,
                                cs.State AS SiteState,
                                cs.Zip AS SiteZip,
                                cs.Country AS SiteCountry,
                                cs.Contact AS SiteContact,
                                cs.PhoneNumber AS SitePhone,
                                cs.MobileNumber AS SiteMobile,
                                cs.Email AS SiteEmail
                            FROM [msSchedulerV3].[dbo].[tbl_Appointment] appt
                            LEFT JOIN [msSchedulerV3].[dbo].[tbl_ServiceType] st ON appt.CompanyID = st.CompanyID 
                                 AND (TRY_CAST(appt.ServiceType AS INT) = st.ServiceTypeID OR appt.ServiceType = st.ServiceName)
                            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Status] sts ON appt.CompanyID = sts.CompanyID 
                                 AND (CASE WHEN appt.Status = '0' OR appt.Status IS NULL OR appt.Status = '' THEN 1 ELSE TRY_CAST(appt.Status AS INT) END = sts.StatusID OR appt.Status = sts.StatusName)
                            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON appt.CustomerID = c.CustomerID and appt.CompanyID = c.CompanyID
                            LEFT JOIN [msSchedulerV3].[dbo].[tbl_CustomerSite] cs ON appt.SiteID = cs.Id and appt.CompanyID = cs.CompanyID
                            WHERE appt.ApptID = @ApptID AND appt.CompanyID = @CompanyID";

                db.AddParameter("@ApptID", appointmentId, SqlDbType.NVarChar);
              //  db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                DataSet dataSet = db.Get_DataSet(sql, companyId);
                //db.ExecuteParam(sql, out dt);
                //db.Close();
                dt = dataSet.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    int hr = row["Hour"] != DBNull.Value ? Convert.ToInt32(row["Hour"]) : 0;
                    int min = row["Minute"] != DBNull.Value ? Convert.ToInt32(row["Minute"]) : 0;
                    if (hr == 0 && min == 0) hr = 1; // Default fallback

                    // Determine if this appointment has a site (check SiteID, not SiteName)
                    bool hasSite = row["SiteID"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["SiteID"].ToString()) && row["SiteID"].ToString() != "0";

                    string contactName = hasSite ? row["SiteContact"].ToString() : (row["FirstName"].ToString() + " " + row["LastName"].ToString());
                    if (string.IsNullOrWhiteSpace(contactName) && !hasSite) contactName = row["CompanyName"].ToString();

                    result = new
                    {
                        ApptID = row["ApptID"],
                        CustomerID = row["CustomerID"],
                        SiteID = row["SiteID"] != DBNull.Value ? row["SiteID"] : 0,
                        Note = row["Note"],
                        TimeSlot = row["TimeSlot"],
                        Hour = row["Hour"] != DBNull.Value ? row["Hour"] : 1,
                        Minute = row["Minute"] != DBNull.Value ? row["Minute"] : 0,
                        Date = row["RequestDate"],
                        ResourceID = row["ResourceID"] != DBNull.Value ? row["ResourceID"] : 0,
                        ServiceTypeID = row["ServiceTypeID"] != DBNull.Value ? row["ServiceTypeID"] : 0,
                        Status = row["StatusName"],
                        DisplayStatus = row["DisplayStatus"],
                        TicketStatus = row["TicketStatusName"],
                        Duration = $"{hr} Hr : {min} Min",
                        StartDateTime = row["StartDateTime"] != DBNull.Value ? Convert.ToDateTime(row["StartDateTime"]).ToString("MM/dd/yyyy hh:mm tt") : "",
                        EndDateTime = row["EndDateTime"] != DBNull.Value ? Convert.ToDateTime(row["EndDateTime"]).ToString("MM/dd/yyyy hh:mm tt") : "",
                        StatusColor = row["StatusColor"] != DBNull.Value ? row["StatusColor"].ToString() : "#3b82f6",
                        ServiceColor = row["ServiceColor"] != DBNull.Value ? row["ServiceColor"].ToString() : "#3b82f6",

                        // New Fields
                        CustomerName = row["FirstName"].ToString() + " " + row["LastName"].ToString(),
                        SiteName = hasSite ? row["SiteName"].ToString() : "",
                        ContactName = contactName,
                        Address = hasSite ? row["SiteAddress"].ToString() : row["MainAddress"].ToString(),
                        City = hasSite ? row["SiteCity"].ToString() : row["MainCity"].ToString(),
                        State = hasSite ? row["SiteState"].ToString() : row["MainState"].ToString(),
                        Zip = hasSite ? row["SiteZip"].ToString() : row["MainZip"].ToString(),
                        Country = hasSite ? row["SiteCountry"].ToString() : row["MainCountry"].ToString(),
                        Phone = hasSite ? row["SitePhone"].ToString() : row["MainPhone"].ToString(),
                        Mobile = hasSite ? row["SiteMobile"].ToString() : row["MainMobile"].ToString(),
                        Email = hasSite ? row["SiteEmail"].ToString() : row["MainEmail"].ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return result;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string LoadCustomers_OLD(int draw, int start, int length, string searchValue, string sortColumn, string sortDirection, string cslViewFilter = "all", bool hideNoAppointments = false)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            int totalRecords = 0;
            Database db = new Database();
            DataTable dt = new DataTable();
            var customers = new List<CustomerEntity>();
            try
            {
                string customerStatusQueryBase = $@"
    WITH CustomerStatusCTE AS (
        SELECT
            c.CustomerID,
            c.CompanyID,
            c.FirstName,
            c.LastName,
            c.Email,     
            apptData.StatusName,
            apptData.LatestAppointmentID AS LatestAppointmentID -- Added LatestAppointmentID
        FROM [msSchedulerV3].[dbo].[tbl_Customer] c
        OUTER APPLY (
            SELECT TOP 1
                CASE
                    WHEN a.Status = 'Deleted' THEN 'N/A'
                    WHEN s.StatusName = 'Scheduled' THEN 'Confirmed'
                    WHEN s.StatusName IS NOT NULL THEN s.StatusName
                    ELSE a.Status
                END AS StatusName,
                a.ApptID AS LatestAppointmentID -- Select the Appointment ID
            FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Status] s ON TRY_CAST(a.Status AS INT) = s.StatusID AND a.CompanyID = s.CompanyID
            WHERE a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
            ORDER BY a.ApptDateTime DESC
        ) AS apptData
        WHERE     c.warrentycompanyid > 0 and c.CompanyID = '{companyid}'
    )
    SELECT CustomerID, CompanyID, FirstName, LastName, Email, StatusName, LatestAppointmentID FROM CustomerStatusCTE
    WHERE 1=1";

                string customerStatusQueryForCountAndFilter = customerStatusQueryBase;

                // Apply searchValue filter to the base CTE selection
                if (!string.IsNullOrEmpty(searchValue))
                {
                    customerStatusQueryForCountAndFilter += $" AND (FirstName LIKE '%{searchValue}%' OR LastName LIKE '%{searchValue}%' OR Email LIKE '%{searchValue}%')";
                }

                // Filter for Current Appointments vs All Customers
                if (cslViewFilter == "current")
                {
                    customerStatusQueryForCountAndFilter += @"AND EXISTS (
                        SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                        WHERE a.CustomerID = CustomerStatusCTE.CustomerID
                          AND a.CompanyID = CustomerStatusCTE.CompanyID
                          AND a.Status NOT IN ('Deleted', 'Closed', 'Cancelled')
                          AND a.ApptDateTime >= CAST(GETDATE() AS DATE)
                    ) ";
                }
                if (hideNoAppointments)
                {
                    customerStatusQueryForCountAndFilter += "AND StatusName != 'N/A' ";
                }

                // --- Query execution part ---
                // --- Conditional Count Query Construction and Execution ---
                string effectiveCountSql;
                if (cslViewFilter == "current" || hideNoAppointments)
                {
                    // If complex filters are active, use the full CTE for accurate counting
                    effectiveCountSql = $@"
    WITH CustomerStatusCTE AS (
        SELECT
            c.CustomerID,
            c.CompanyID,
            c.FirstName,
            c.LastName,
            c.Email,
            ISNULL((SELECT TOP 1
                CASE
                    WHEN a.Status = 'Deleted' THEN 'N/A'
                    WHEN s.StatusName = 'Scheduled' THEN 'Confirmed'
                    WHEN s.StatusName IS NOT NULL THEN s.StatusName
                    ELSE a.Status
                END
            FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Status] s ON TRY_CAST(a.Status AS INT) = s.StatusID AND a.CompanyID = s.CompanyID
            WHERE a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
            ORDER BY a.ApptDateTime DESC), 'N/A') AS StatusName
        FROM [msSchedulerV3].[dbo].[tbl_Customer] c
        WHERE c.warrentycompanyid > 0 and  c.CompanyID = '{companyid}'
    )
    SELECT COUNT(cst.CustomerID)
    FROM CustomerStatusCTE cst
    WHERE 1=1   ";

                    // Apply searchValue filter to the CTE result
                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        effectiveCountSql += $" AND (cst.FirstName LIKE '%{searchValue}%' OR cst.LastName LIKE '%{searchValue}%' OR cst.Email LIKE '%{searchValue}%')";
                    }

                    // Apply additional filters
                    if (cslViewFilter == "current")
                    {
                        effectiveCountSql += @"AND EXISTS (
                            SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                            WHERE a.CustomerID = cst.CustomerID
                              AND a.CompanyID = cst.CompanyID
                              AND a.Status NOT IN ('Deleted', 'Closed', 'Cancelled')
                              AND a.ApptDateTime >= CAST(GETDATE() AS DATE)
                        ) ";
                    }
                    if (hideNoAppointments)
                    {
                        effectiveCountSql += "AND cst.StatusName != 'N/A' ";
                    }
                }
                else
                {
                    // If no complex filters, use a simpler count query
                    effectiveCountSql = $@"
    SELECT COUNT(c.CustomerID)
    FROM [msSchedulerV3].[dbo].[tbl_Customer] c
    WHERE c.CompanyID = '{companyid}'
    {(string.IsNullOrEmpty(searchValue) ? "" : $"AND (c.FirstName LIKE '%{searchValue}%' OR c.LastName LIKE '%{searchValue}%' OR c.Email LIKE '%{searchValue}%')")}";
                }

                System.Diagnostics.Debug.WriteLine($"countSql (effective): {effectiveCountSql}"); // Added debug line
                db.Open();
                object result = db.ExecuteScalar(effectiveCountSql);
                if (result != null)
                {
                    totalRecords = Convert.ToInt32(result);
                }
                db.Close();
                System.Diagnostics.Debug.WriteLine($"totalRecords after countSql (effective): {totalRecords}"); // Added debug line
                // --- End of Conditional Count Query Construction and Execution ---

                string finalSql = $@"
    WITH CustomerStatusCTE AS (
        SELECT
            c.CustomerID,
            c.CompanyID,
            c.FirstName,
            c.LastName,-- Added for sorting and filtering in CustomerStatusCTE
            c.Email,     -- Added for sorting and filtering in CustomerStatusCTE
            apptData.StatusName,
            apptData.LatestAppointmentID AS LatestAppointmentID -- Added LatestAppointmentID
        FROM [msSchedulerV3].[dbo].[tbl_Customer] c
        OUTER APPLY (
            SELECT TOP 1
                CASE
                    WHEN a.Status = 'Deleted' THEN 'N/A'
                    WHEN s.StatusName = 'Scheduled' THEN 'Confirmed'
                    WHEN s.StatusName IS NOT NULL THEN s.StatusName
                    ELSE a.Status
                END AS StatusName,
                a.ApptID AS LatestAppointmentID -- Select the Appointment ID
            FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
            LEFT JOIN [msSchedulerV3].[dbo].[tbl_Status] s ON TRY_CAST(a.Status AS INT) = s.StatusID AND a.CompanyID = s.CompanyID
            WHERE a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
            ORDER BY a.ApptDateTime DESC
        ) AS apptData
        WHERE c.warrentycompanyid > 0 and  c.CompanyID = '{companyid}'
    )
    SELECT
        c.*,
        cst.StatusName,
        cst.LatestAppointmentID -- Select LatestAppointmentID for final result
    FROM [msSchedulerV3].[dbo].[tbl_Customer] c
    JOIN CustomerStatusCTE cst ON c.CustomerID = cst.CustomerID AND c.CompanyID = cst.CompanyID
    WHERE 1=1  ";

                // Apply searchValue filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    finalSql += $" AND (cst.FirstName LIKE '%{searchValue}%' OR cst.LastName LIKE '%{searchValue}%' OR cst.Email LIKE '%{searchValue}%')";
                }

                // Apply additional filters
                if (cslViewFilter == "current")
                {
                    finalSql += @"AND EXISTS (
                        SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                        WHERE a.CustomerID = c.CustomerID
                          AND a.CompanyID = c.CompanyID
                          AND a.Status NOT IN ('Deleted', 'Closed', 'Cancelled')
                          AND a.ApptDateTime >= CAST(GETDATE() AS DATE)
                    ) ";
                }
                if (hideNoAppointments)
                {
                    finalSql += "AND cst.StatusName != 'N/A' ";
                }
                if (sortColumn == "fullname")
                {
                    sortColumn = "FirstName";
                }
                    finalSql += $" ORDER BY {sortColumn} {sortDirection} OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY;";
                System.Diagnostics.Debug.WriteLine($"finalSql: {finalSql}"); // Added debug line
                db.Open();
                db.Execute(finalSql, out dt);
                db.Close();
                System.Diagnostics.Debug.WriteLine($"dt.Rows.Count after finalSql: {dt.Rows.Count}"); // Added debug line
                // --- End of Query execution part ---


                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        customers.Add(new CustomerEntity
                        {
                            CompanyID = dr["CompanyID"].ToString(),
                            CustomerID = dr["CustomerID"].ToString(),
                            BusinessID = Convert.ToInt32(dr["BusinessID"]),
                            CustomerGuid = dr["CustomerGuid"].ToString(),
                            Address1 = dr["Address1"].ToString(),
                            Address2 = dr["Address2"].ToString(),
                            FirstName = dr["FirstName"].ToString(),
                            //FirstName2 = dr["FirstName2"].ToString(),
                            LastName = dr["LastName"].ToString(),
                            fullname = dr["FirstName"].ToString() + ' ' + dr["LastName"].ToString(),
                            //LastName2 = dr["LastName2"].ToString(),
                            //Title = dr["Title"].ToString(),
                            //Title2 = dr["Title2"].ToString(),
                            JobTitle = dr["JobTitle"].ToString(),
                            JobTitle2 = dr["JobTitle2"].ToString(),
                            City = dr["City"].ToString(),
                            State = dr["State"].ToString(),
                            ZipCode = dr["ZipCode"].ToString(),
                            Phone = dr["Phone"].ToString(),
                            Mobile = dr["Mobile"].ToString(),
                            Email = dr["Email"].ToString(),
                            // Notes = dr["Notes"].ToString(),
                            CompanyName = dr["CompanyName"].ToString(),
                            //CompanyName2 = dr["CompanyName2"].ToString(),
                            //BusinessName = dr["BusinessName"].ToString(),
                            IsBusinessContact = Convert.ToBoolean(dr["IsBusinessContact"]),
                            IsPrimaryContact = Convert.ToBoolean(dr["IsPrimaryContact"]),
                            IsDealer = Convert.ToBoolean(dr["IsDealer"]),
                            DealerID = dr["DealerID"].ToString(),
                            CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"]),
                            CallPopAppId = dr["CallPopAppId"].ToString(),
                            QboId = dr["QboId"].ToString(),
                            CreatedCompanyID = dr["CreatedCompanyID"].ToString(),
                            StatusName = dr["StatusName"].ToString()
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCustomers method: {ex.Message}"); // ADDED for full exception logging
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = customers
            };

            return JsonConvert.SerializeObject(response);
        }

        public CustomerEntity GetCustomerDetails(string CustomerGuid, string customerId,int draw, int start, int length, string searchValue, string sortColumn, string sortDirection)
        {
            string companyid = Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            var customer = new CustomerEntity();

            string TotalDueForInvoice = "0";
            string TotalDueForEstimate = "0";
            string TotalEstimate = "0";
            string TotalInvoice = "0";
            string TotalAppoinment = "0";

            try
            {
                string Sql = "SELECT CompanyID,CustomerID,FirstName,LastName,CustomerGuid,Title,CompanyName,JobTitle,IsPrimaryContact,Notes," +
              "Address1,City,State,ZipCode,Phone,Mobile,Email,BusinessID, " +
              "FORMAT( (select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.dbo.tbl_invoice where Type='Invoice' and CompnyID='" + companyid + "' and CustomerID='" + customerId + "'), 'N2') as TotalDueForInvoice," +
              "FORMAT((select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.dbo.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + companyid + "' and CustomerID='" + customerId + "'), 'N2') as TotalDueForEstimate," +
              "(select count(Type) from msSchedulerV3.dbo.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + companyid + "' and CustomerID='" + customerId + "') as TotalEstimate," +
              "(select count(Type) from msSchedulerV3.dbo.tbl_invoice where Type='Invoice' and CompnyID='" + companyid + "' and CustomerID='" + customerId + "') as TotalInvoice," +
              "(select count(CompanyID) from msSchedulerV3.dbo.tbl_Appointment where CompanyID='" + companyid + "' and CustomerID='" + customerId + "') as TotalAppoinment " +
              " FROM msSchedulerV3.dbo.tbl_Customer ";

                if (string.IsNullOrEmpty(CustomerGuid))
                {
                    Sql += " where CompanyID='" + companyid + "' and CustomerID = '" + customerId + "'";
                }
                else
                {
                    Sql += " where CompanyID='" + companyid + "' and CustomerGuid = '" + CustomerGuid + "'";
                }

                db.Open();
                db.Execute(Sql, out dt);
                db.Close();
                string busnessID = "0";

                if (dt.Rows.Count > 0)
                {
                    busnessID = dt.Rows[0]["BusinessID"].ToString();
                    customer.FirstName = dt.Rows[0]["FirstName"].ToString();
                    customer.LastName = dt.Rows[0]["LastName"].ToString();
                    customer.Title = dt.Rows[0]["title"].ToString();
                    customer.JobTitle = dt.Rows[0]["JobTitle"].ToString();
                    customer.Address1 = dt.Rows[0]["Address1"].ToString();
                    customer.City = dt.Rows[0]["City"].ToString();
                    customer.State = dt.Rows[0]["State"].ToString();
                    customer.ZipCode = dt.Rows[0]["ZipCode"].ToString();
                    customer.Phone = dt.Rows[0]["Phone"].ToString();
                    customer.Mobile = dt.Rows[0]["Mobile"].ToString();
                    customer.Email = dt.Rows[0]["Email"].ToString();
                    TotalDueForInvoice = dt.Rows[0]["TotalDueForInvoice"].ToString();
                    TotalDueForEstimate = dt.Rows[0]["TotalDueForEstimate"].ToString();
                    TotalEstimate = dt.Rows[0]["TotalEstimate"].ToString();
                    TotalInvoice = dt.Rows[0]["TotalInvoice"].ToString();
                    TotalAppoinment = dt.Rows[0]["TotalAppoinment"].ToString();
                    customer.Notes = dt.Rows[0]["Notes"].ToString();
                    customer.CompanyName = dt.Rows[0]["CompanyName"].ToString();
                    customer.CustomerID = dt.Rows[0]["CustomerID"].ToString();
                    customer.CustomerGuid = dt.Rows[0]["CustomerGuid"].ToString();
                }

                customer.CustomFields = new Dictionary<string, string>();
                customer.CustomFields.Add("TotalDueForInvoice", TotalDueForInvoice.ToString());
                customer.CustomFields.Add("TotalDueForEstimate", TotalDueForEstimate.ToString());
                customer.CustomFields.Add("TotalEstimate", TotalEstimate.ToString());
                customer.CustomFields.Add("TotalInvoice", TotalInvoice.ToString());
                customer.CustomFields.Add("TotalAppoinment", TotalAppoinment.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return customer;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetCustomerSiteData(string customerId,int draw, int start, int length, string searchValue, string sortColumn, string sortDirection, string appointmentStartDate = "", string appointmentStatus = "")
        {
            var sites = new List<CustomerSite>();


            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            int totalRecords = 0;
            DataTable dt = new DataTable();
            searchValue = searchValue.Replace("'", "");
            try
            {
                //db.Open();
                string strSQL = @"SELECT st.Id,st.CustomerID,st.SiteName,st.FirstName,st.CustomerGuid,st.LastName,st.Address,st.Country,st.State,st.Zip,st.Contact,st.Email,st.PhoneNumber,st.Note,COUNT(appt.CompanyID) AS appointment_count 
                            FROM [msSchedulerV3].dbo.tbl_CustomerSite st LEFT JOIN [msSchedulerV3].[dbo].[tbl_Appointment] appt 
                    ON  appt.CompanyID = st.CompanyID and appt.siteid = st.id and  appt.CustomerID = st.CustomerID      WHERE st.CompanyID='" + companyid + "' AND st.CustomerID='" + customerId + "' ";

                if (!string.IsNullOrEmpty(searchValue))
                {
                    strSQL += $" AND (st.SiteName LIKE '%{searchValue}%' OR st.FirstName LIKE '%{searchValue}%' OR st.Email LIKE '%{searchValue}%') ";
                }

                if (!string.IsNullOrEmpty(appointmentStartDate))
                {
                    strSQL += $" AND CONVERT(DATE, COALESCE(appt.StartDateTime, appt.ApptDateTime)) = '{appointmentStartDate}' ";
                }
                if (!string.IsNullOrEmpty(appointmentStatus))
                {
                    // Handle status as either StatusName or StatusID via tbl_Status
                    strSQL += $" AND appt.status = '{appointmentStatus}' ";
                }

                strSQL += $" GROUP BY st.Id,st.CustomerID,st.SiteName,st.FirstName,st.CustomerGuid,st.LastName,st.Address,st.Country,st.State,st.Zip,st.Contact,st.Email,st.PhoneNumber,st.Note ";
                strSQL += $" ORDER BY st.SiteName {sortDirection} OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY;";

                strSQL += @"SELECT count(st.Id) as totalRecords  FROM [msSchedulerV3].dbo.tbl_CustomerSite st LEFT JOIN [msSchedulerV3].[dbo].[tbl_Appointment] appt 
                    ON  appt.CompanyID = st.CompanyID and appt.siteid = st.id and  appt.CustomerID = st.CustomerID  WHERE st.CompanyID='" + companyid + "' AND st.CustomerID='" + customerId + "'";

                if (!string.IsNullOrEmpty(appointmentStartDate))
                {
                    strSQL += $" AND CONVERT(DATE, COALESCE(appt.StartDateTime, appt.ApptDateTime)) = '{appointmentStartDate}' ";
                }
                if (!string.IsNullOrEmpty(appointmentStatus))
                {
                    // Handle status as either StatusName or StatusID via tbl_Status
                    strSQL += $" AND appt.status = '{appointmentStatus}' ";
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    strSQL += $" AND (st.SiteName LIKE '%{searchValue}%' OR st.FirstName LIKE '%{searchValue}%' OR st.Email LIKE '%{searchValue}%') ;";
                }
                else
                {
                    strSQL += $" ;";
                }

                DataSet dataSet = db.Get_DataSet(strSQL, companyid);
                dt = dataSet.Tables[0];

                if (dataSet.Tables[1].Rows.Count > 0)
                {
                    totalRecords = Convert.ToInt32(dataSet.Tables[1].Rows[0]["totalRecords"]);
                }
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sites.Add(new CustomerSite
                        {
                            Id = Convert.ToInt32(dr["Id"]),
                            CompanyID = companyid,
                            CustomerID = dr["CustomerID"].ToString(),
                            CustomerGuid = dr["CustomerGuid"].ToString(),
                            SiteName = dr["SiteName"].ToString() ?? "",
                            FirstName = dr["FirstName"].ToString() ?? "",
                            LastName = dr["LastName"].ToString() ?? "",
                            Address = dr["Address"].ToString() ?? "",
                            Country = dr["Country"].ToString() ?? "",
                            State = LocationHelper.GetFullName(dr["State"].ToString() ?? ""),
                            Zip = dr["Zip"].ToString() ?? "",
                            Contact = dr["Contact"].ToString() ?? "",
                            Email = dr["Email"].ToString() ?? "",
                            IsActive = true,
                            PhoneNumber = dr["PhoneNumber"].ToString() ?? "",
                            TotalAppointment = Convert.ToInt32(dr["appointment_count"]),
                            Note = dr["Note"].ToString() ?? ""
                        });
                    }
                }
                else
                {
                    DataTable customerDt = new DataTable();
                    db.Open();
                    string customerSql = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_Customer] WHERE CustomerID = @CustomerID AND CompanyID = @CompanyID;";
                    db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                    db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                    db.ExecuteParam(customerSql, out customerDt);
                    db.Close();

                    if (customerDt.Rows.Count > 0)
                    {
                        DataRow cust = customerDt.Rows[0];
                        sites.Add(new CustomerSite
                        {
                            Id = 0,
                            CompanyID = companyid,
                            CustomerID = cust["CustomerID"].ToString(),
                            CustomerGuid = cust["CustomerGuid"].ToString(),
                            SiteName = "Customer Location (Default)",
                            FirstName = cust["FirstName"].ToString(),
                            LastName = cust["LastName"].ToString(),
                            Address = string.Join(", ", new[] { cust["Address1"].ToString(), cust["City"].ToString(), cust["State"].ToString(), cust["ZipCode"].ToString() }.Where(s => !string.IsNullOrEmpty(s))),
                            PhoneNumber = cust["Phone"].ToString(),
                            Email = cust["Email"].ToString(),
                            IsActive = true,
                            Note = "This is the primary customer location."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();

            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }

            var response = new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = sites
            };

            return JsonConvert.SerializeObject(response);
        }
        public static class LocationHelper
        {
            private static readonly Dictionary<string, string> AbbrToFullName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
             {"AL", "Alabama"}, {"AK", "Alaska"}, {"AZ", "Arizona"}, {"AR", "Arkansas"}, {"CA", "California"},
             {"CO", "Colorado"}, {"CT", "Connecticut"}, {"DE", "Delaware"}, {"DC", "District of Columbia"},
             {"FL", "Florida"}, {"GA", "Georgia"}, {"HI", "Hawaii"}, {"ID", "Idaho"}, {"IL", "Illinois"},
             {"IN", "Indiana"}, {"IA", "Iowa"}, {"KS", "Kansas"}, {"KY", "Kentucky"}, {"LA", "Louisiana"},
             {"ME", "Maine"}, {"MD", "Maryland"}, {"MA", "Massachusetts"}, {"MI", "Michigan"}, {"MN", "Minnesota"},
             {"MS", "Mississippi"}, {"MO", "Missouri"}, {"MT", "Montana"}, {"NE", "Nebraska"}, {"NV", "Nevada"},
             {"NH", "New Hampshire"}, {"NJ", "New Jersey"}, {"NM", "New Mexico"}, {"NY", "New York"},
             {"NC", "North Carolina"}, {"ND", "North Dakota"}, {"OH", "Ohio"}, {"OK", "Oklahoma"}, {"OR", "Oregon"},
             {"PA", "Pennsylvania"}, {"RI", "Rhode Island"}, {"SC", "South Carolina"}, {"SD", "South Dakota"},
             {"TN", "Tennessee"}, {"TX", "Texas"}, {"UT", "Utah"}, {"VT", "Vermont"}, {"VA", "Virginia"},
             {"WA", "Washington"}, {"WV", "West Virginia"}, {"WI", "Wisconsin"}, {"WY", "Wyoming"},
             {"AB", "Alberta"}, {"BC", "British Columbia"}, {"MB", "Manitoba"}, {"NB", "New Brunswick"},
             {"NL", "Newfoundland and Labrador"}, {"NS", "Nova Scotia"}, {"NT", "Northwest Territories"},
             {"NU", "Nunavut"}, {"ON", "Ontario"}, {"PE", "Prince Edward Island"}, {"QC", "Quebec"},
             {"SK", "Saskatchewan"}, {"YT", "Yukon"}
        };

            private static readonly Dictionary<string, string> FullNameToAbbr =
                AbbrToFullName.ToDictionary(kp => kp.Value, kp => kp.Key, StringComparer.OrdinalIgnoreCase);

            public static string GetFullName(string code)
            {
                if (string.IsNullOrEmpty(code)) return code;
                return AbbrToFullName.TryGetValue(code, out string fullName) ? fullName : code;
            }

            public static string GetAbbreviation(string fullName)
            {
                if (string.IsNullOrEmpty(fullName)) return fullName;
                return FullNameToAbbr.TryGetValue(fullName, out string abbr) ? abbr : fullName;
            }
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteCustomerSite(int siteId)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            bool success = false;
            try
            {
                db.Open();
                string strSQL = @"DELETE FROM [msSchedulerV3].dbo.tbl_CustomerSite 
                          WHERE Id = @Id AND CompanyID = @CompanyID";
                db.AddParameter("@Id", siteId, SqlDbType.Int);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                success = db.UpdateSql(strSQL);
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
            return success;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomerSiteData(CustomerSite site)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            if (!string.IsNullOrEmpty(site.CustomerID))
            {
                site.State = LocationHelper.GetAbbreviation(site.State);

                if (site.Id > 0)
                {
                    return UpdateCustomerSiteInfo(site);
                }
                else
                {
                    return InsertCustomerSiteInfo(site);
                }
            }
            return false;
        }


        public static bool InsertCustomerSiteInfo(CustomerSite site)
        {
            bool success = false;
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();

                string strSQL = @"INSERT INTO [msSchedulerV3].dbo.tbl_CustomerSite
        (CompanyID, CustomerID, CustomerGuid, SiteName, FirstName, LastName, PhoneNumber, Email, Contact, Address, Country, State, Zip, Note, IsActive) output INSERTED.ID
        VALUES (@CompanyID, @CustomerID, @CustomerGuid, @SiteName, @FirstName, @LastName, @PhoneNumber, @Email, @Contact, @Address, @Country, @State, @Zip, @Note, @IsActive)";

                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", site.CustomerID, SqlDbType.NVarChar);
                db.AddParameter("@CustomerGuid", site.CustomerGuid, SqlDbType.NVarChar);
                db.AddParameter("@SiteName", site.SiteName, SqlDbType.NVarChar);
                db.AddParameter("@FirstName", site.FirstName, SqlDbType.NVarChar);
                db.AddParameter("@LastName", site.LastName, SqlDbType.NVarChar);
                db.AddParameter("@PhoneNumber", site.PhoneNumber, SqlDbType.NVarChar);
                db.AddParameter("@Email", site.Email, SqlDbType.NVarChar);
                db.AddParameter("@Contact", site.Contact, SqlDbType.NVarChar);
                db.AddParameter("@Address", site.Address, SqlDbType.NVarChar);
                db.AddParameter("@Country", site.Country, SqlDbType.NVarChar);
                db.AddParameter("@State", site.State, SqlDbType.NVarChar);
                db.AddParameter("@Zip", site.Zip, SqlDbType.NVarChar);
                db.AddParameter("@Note", site.Note, SqlDbType.NVarChar);
                db.AddParameter("@IsActive", site.IsActive, SqlDbType.Bit);

                object result = db.ExecuteScalarData(strSQL);
                if (result != null)
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
            return success;
        }

        public static bool UpdateCustomerSiteInfo(CustomerSite site)
        {
            bool success = false;
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                // The strSQL variable
                string strSQL = @"UPDATE [msSchedulerV3].dbo.tbl_CustomerSite SET 
                    SiteName = @SiteName,
                    FirstName = @FirstName,
                    LastName = @LastName,
                    PhoneNumber = @PhoneNumber,
                    Email = @Email,
                    Contact = @Contact,
                    Address = @Address,
                    Country = @Country,
                    State = @State,
                    Zip = @Zip,
                    Note = @Note,
                    IsActive = @IsActive 
                  WHERE Id=@Id and CustomerID = @CustomerID";
                // The db.AddParameter calls
                db.AddParameter("@SiteName", site.SiteName, SqlDbType.NVarChar);
                db.AddParameter("@FirstName", site.FirstName, SqlDbType.NVarChar);
                db.AddParameter("@LastName", site.LastName, SqlDbType.NVarChar);
                db.AddParameter("@PhoneNumber", site.PhoneNumber, SqlDbType.NVarChar);
                db.AddParameter("@Email", site.Email, SqlDbType.NVarChar);
                db.AddParameter("@Contact", site.Contact, SqlDbType.NVarChar);
                db.AddParameter("@Address", site.Address, SqlDbType.NVarChar);
                db.AddParameter("@Country", site.Country, SqlDbType.NVarChar);
                db.AddParameter("@State", site.State, SqlDbType.NVarChar);
                db.AddParameter("@Zip", site.Zip, SqlDbType.NVarChar);
                db.AddParameter("@Note", site.Note, SqlDbType.NVarChar);
                db.AddParameter("@IsActive", site.IsActive, SqlDbType.Bit);
                db.AddParameter("@Id", site.Id, SqlDbType.Int);
                db.AddParameter("@CustomerID", site.CustomerID, SqlDbType.NVarChar); // Ensure this is present

                success = db.UpdateSql(strSQL);
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
                                    return success;
                                }
                        
                                [WebMethod]
                                [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
                                public static bool AddCustomer(CustomerEntity customer)
                                {
                                    bool success = false;
                                    string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                                    Database db = new Database();
                                    try
                                    {
                                        db.Open();
                                        string strSQL = @"INSERT INTO [msSchedulerV3].dbo.tbl_Customer (CompanyID, FirstName, LastName, Email, Phone) 
                                                         VALUES (@CompanyID, @FirstName, @LastName, @Email, @Phone)";
                        
                                        db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                                        db.AddParameter("@FirstName", customer.FirstName, SqlDbType.NVarChar);
                                        db.AddParameter("@LastName", customer.LastName, SqlDbType.NVarChar);
                                        db.AddParameter("@Email", customer.Email, SqlDbType.NVarChar);
                                        db.AddParameter("@Phone", customer.Phone, SqlDbType.NVarChar);
                        
                                        success = db.UpdateSql(strSQL);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error adding customer: " + ex.Message);
                                        success = false;
                                    }
                                    finally
                                    {
                                        db.Close();
                                    }
                                    return success;
                                }
                        
                                [WebMethod]
                                [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
                                public static bool UpdateCustomer(CustomerEntity customer)
                                {
                                    bool success = false;
                                    string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                                    Database db = new Database();
                                    try
                                    {
                                        db.Open();
                                        string strSQL = @"UPDATE [msSchedulerV3].dbo.tbl_Customer SET 
                                            FirstName = @FirstName,
                                            LastName = @LastName,
                                            Email = @Email,
                                            Phone = @Phone
                                          WHERE CustomerID=@CustomerID and CompanyID = @CompanyID";
                        
                                        db.AddParameter("@FirstName", customer.FirstName, SqlDbType.NVarChar);
                                        db.AddParameter("@LastName", customer.LastName, SqlDbType.NVarChar);
                                        db.AddParameter("@Email", customer.Email, SqlDbType.NVarChar);
                                        db.AddParameter("@Phone", customer.Phone, SqlDbType.NVarChar);
                                        db.AddParameter("@CustomerID", customer.CustomerID, SqlDbType.NVarChar);
                                        db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                        
                                        success = db.UpdateSql(strSQL);
                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error updating customer: " + ex.Message);
                                        success = false;
                                    }
                                    finally
                                    {
                                        db.Close();
                                    }
                                    return success;
                                }
                        
                                [WebMethod]
                                [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
                                public static bool UpdateCustomerFromDefaultSite(CustomerSite site)
                                {
                                    bool success = false;
                                    string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                                    Database db = new Database();
                                    try
                                    {
                                        db.Open();
                                        string strSQL = @"UPDATE [msSchedulerV3].dbo.tbl_Customer SET 
                                            FirstName = @FirstName,
                                            LastName = @LastName,
                                            Phone = @PhoneNumber,
                                            Email = @Email,
                                            Address1 = @Address,
                                            Country = @Country,
                                            State = @State,
                                            ZipCode = @Zip
                                          WHERE CustomerID=@CustomerID and CompanyID = @CompanyID";
                        
                                        db.AddParameter("@FirstName", site.FirstName, SqlDbType.NVarChar);
                                        db.AddParameter("@LastName", site.LastName, SqlDbType.NVarChar);
                                        db.AddParameter("@PhoneNumber", site.PhoneNumber, SqlDbType.NVarChar);
                                        db.AddParameter("@Email", site.Email, SqlDbType.NVarChar);
                                        db.AddParameter("@Address", site.Address, SqlDbType.NVarChar);
                                        db.AddParameter("@Country", site.Country, SqlDbType.NVarChar);
                                        db.AddParameter("@State", site.State, SqlDbType.NVarChar);
                                        db.AddParameter("@Zip", site.Zip, SqlDbType.NVarChar);
                                        db.AddParameter("@CustomerID", site.CustomerID, SqlDbType.NVarChar);
                                        db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                        
                                        success = db.UpdateSql(strSQL);
                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Error updating customer from default site: " + ex.Message);
                                        success = false;
                                    }
                                    finally
                                    {
                                        db.Close();
                                    }
                                    return success;
                                }
        public class EmailData
        {
            public string to { get; set; }
            public string cc { get; set; }
            public string bcc { get; set; }
            public string subject { get; set; }
            public string body { get; set; }
            public string customerID { get; set; }
        }
        public class Appointment
        {
            public string AppoinmentId { get; set; }
            public string CustomerID { get; set; }
            public string ServiceType { get; set; }
            public int ResourceID { get; set; }
            public string Status { get; set; }
            public string TicketStatus { get; set; }
            public string RequestDate { get; set; }
            public string StartDateTime { get; set; }
            public string EndDateTime { get; set; }
            public string TimeSlot { get; set; }
            public int Hour { get; set; }
            public int Minute { get; set; }
            public string Note { get; set; }
            public int SiteId { get; set; }
        }

    }
                        
                        
                        }
