using FSM.Helper;
using FSM.Entity.Appoinments;
using FSM.Entity.Customer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using FSM.Models.AppoinmentModel;
using System.Configuration;
using FSM.Processors;
using FSM.Entity.Forms;
using FSM.SMSService;
using System.Data.SqlClient;
using System.Security.Policy;
using System.Diagnostics;
using TPM.Processors;

namespace TPM
{
    public class BatchUpdatePayload
    {
        public List<int> appointmentIds { get; set; }
        public int newStatusId { get; set; }
        public string companyId { get; set; }
    }

    public partial class Appointments : System.Web.UI.Page
    {
        public string GoogleMapsApiKey { get; private set; }
        string CompanyID = "";

        private static bool IsResourceRequired(int statusId)
        {
            // Status IDs requiring a resource: 2 (Scheduled - assigns tech), 3 (Dispatched), 4 (In-Route), 5 (Arrived), 6 (Completed), 7 (Closed)
            int[] requiredStatuses = { 2, 3, 5 };
            return requiredStatuses.Contains(statusId);
        }

        /// <summary>
        /// Process email/SMS communication when appointment status changes
        /// </summary>
        private static void ProcessStatusCommunication(int appointmentId, string oldStatus, string newStatus, int resourceId, string companyId)
        {
            try
            {
                ConsoleLog($"[Appointments] ProcessStatusCommunication: ApptID={appointmentId}, OldStatus={oldStatus}, NewStatus={newStatus}, ResourceID={resourceId}");

                // Get status names from StatusID
                string oldStatusName = GetStatusName(oldStatus, companyId);
                string newStatusName = GetStatusName(newStatus, companyId);

                int oldStatusId = 0;
                int newStatusId = 0;
                int.TryParse(oldStatus, out oldStatusId);
                int.TryParse(newStatus, out newStatusId);

                ConsoleLog($"[Appointments] Status names: Old={oldStatusName}({oldStatusId}), New={newStatusName}({newStatusId})");

                // Only process if status actually changed
                if (oldStatusId == newStatusId && oldStatusName.Equals(newStatusName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleLog($"[Appointments] Status unchanged, skipping communication processing");
                    return;
                }

                // Initialize processor and process
                var processor = new AppointmentStatusCommunicationProcessor(companyId);
                processor.ProcessStatusChange(appointmentId, oldStatusId, newStatusId, oldStatusName, newStatusName, resourceId > 0 ? (int?)resourceId : null);

                ConsoleLog($"[Appointments] Status communication processing completed");
            }
            catch (Exception ex)
            {
                ConsoleLog($"[Appointments] ERROR in ProcessStatusCommunication: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get status name from StatusID or StatusName string
        /// </summary>
        private static string GetStatusName(string statusValue, string companyId)
        {
            try
            {
                if (string.IsNullOrEmpty(statusValue) || statusValue == "0")
                    return "Pending";

                // Try to parse as StatusID
                int statusId = 0;
                if (int.TryParse(statusValue, out statusId))
                {
                    string connStr = ConfigurationManager.AppSettings["ConnString"];
                    string sql = "SELECT StatusName FROM tbl_Status WHERE StatusID = @StatusID AND CompanyID = @CompanyID";
                    using (var conn = new SqlConnection(connStr))
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@StatusID", statusId);
                        cmd.Parameters.AddWithValue("@CompanyID", companyId);
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return result.ToString();
                    }
                }

                // If not found or not a number, return as-is (might be StatusName already)
                return statusValue;
            }
            catch (Exception ex)
            {
                ConsoleLog($"[Appointments] ERROR getting status name: {ex.Message}");
                return statusValue ?? "Unknown";
            }
        }

        private static void ConsoleLog(string message)
        {
            string logMessage = $"[Appointments] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            Debug.WriteLine(logMessage);
            System.Diagnostics.Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            this.GoogleMapsApiKey = ConfigurationManager.AppSettings["GoogleMapsApiKey"];
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("Dashboard.aspx");
            }

            if (!IsPostBack)
            {
                SetCecSsoUrl();
                LoadData();
            }
        }


        private void SetCecSsoUrl()
        {
            try
            {
                string userId = Session["LoginUser"] as string;
                string companyId = Session["CompanyID"] as string;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                {
                    cecAppointmentsLink.Visible = false;
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
                    cecAppointmentsLink.Visible = false;
                    return;
                }

                string cecBaseUrl = accountsUrl.Replace("AccountsXinator", "cec");
                string redirectUrl = HttpUtility.UrlEncode("/cec/calendar.aspx?m=3");

                cecAppointmentsLink.NavigateUrl = $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating CEC SSO URL: " + ex.Message);
                cecAppointmentsLink.Visible = false;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetActiveCustomFields(int apptId)
        {
            if (HttpContext.Current.Session["CompanyID"] == null)
            {
                return new { success = false, message = "Session expired" };
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
                    cf.IsActive = 1 AND cf.CompanyId = @CompanyID ORDER BY  cf.FieldName";

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

                System.Diagnostics.Debug.WriteLine("FATAL ERROR in GetActiveCustomFields: " + ex.ToString());



                throw new Exception("Server-side error in GetActiveCustomFields. Check debug output for details.");
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SendFaIdToFieldAgents(int appointmentId, int[] profileIds)
        {
            try
            {
                string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return new { success = false, message = "Session expired." };
                if (profileIds == null || profileIds.Length == 0)
                    return new { success = false, message = "Please select at least one Field Agent." };
                var processor = new AppointmentStatusCommunicationProcessor(companyId);
                processor.SendMessageToCustomerFrmFaId(appointmentId, profileIds.ToList());
                return new { success = true, message = "FA-ID sent to selected Field Agent(s)." };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SendFaIdToFieldAgents ERROR: " + ex.Message);
                return new { success = false, message = ex.Message };
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<AppointmentModel> LoadAppoinments(string searchValue, string fromDate, string toDate, string today, string customerId = null, string siteId = null, string resourceGroupId = null)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAppoinments received: searchValue='{searchValue}', fromDate='{fromDate}', toDate='{toDate}', today='{today}', customerId='{customerId}', siteId='{siteId}', resourceGroupId='{resourceGroupId}'");

            var appoinments = new List<AppointmentModel>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                string whereCondition = "WHERE apt.CompanyID = @CompanyID AND apt.IsApproved = 1 and apt.Status != 'Deleted' AND (apt.SchedulingCal IS NULL OR apt.SchedulingCal = 'FSM') AND (srv.Source IS NULL OR srv.Source != 'CEC') ";
                string joinCondition = "";

                if (!string.IsNullOrEmpty(resourceGroupId) && resourceGroupId != "all")
                {
                    joinCondition += " INNER JOIN [myServiceJobs].[dbo].[tbl_ResourceGroupMapping] rgm ON apt.ResourceID = rgm.ResourceID ";
                    whereCondition += " AND rgm.ResourceGroupID = @ResourceGroupID AND rgm.IsActive = 1 ";
                }

                if (!string.IsNullOrEmpty(customerId))
                {
                    whereCondition += "AND apt.CustomerID = @CustomerID ";
                }
                if (!string.IsNullOrEmpty(siteId))
                {
                    if (siteId == "0")
                    {
                        whereCondition += "AND (apt.SiteId = 0 OR apt.SiteId IS NULL) ";
                    }
                    else
                    {
                        whereCondition += "AND apt.SiteId = @SiteID ";
                    }
                }
                if (!string.IsNullOrEmpty(searchValue))
                {
                    whereCondition += @"AND (
                cus.BusinessName LIKE @SearchValue OR 
                cus.FirstName LIKE @SearchValue OR 
                cus.LastName LIKE @SearchValue OR 
                srv.ServiceName LIKE @SearchValue OR 
                rs.Name LIKE @SearchValue OR 
                cus.Mobile LIKE @SearchValue OR 
                cus.Phone LIKE @SearchValue OR 
                cs.Address LIKE @SearchValue OR 
                cus.Address1 LIKE @SearchValue
            ) ";
                }
                if (!string.IsNullOrEmpty(today))
                {

                    whereCondition += "AND CONVERT(DATE, COALESCE(apt.StartDateTime, apt.ApptDateTime)) = @DateFilter ";
                }
                else if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {

                    whereCondition += "AND CONVERT(DATE, COALESCE(apt.StartDateTime, apt.ApptDateTime)) BETWEEN @FromDate AND @ToDate ";
                }

                db.Open();
                DataTable dt = new DataTable();


                string sql = $@"
            SELECT  
                cus.FirstName, cus.LastName, cus.CustomerGuid, cus.BusinessID, cus.BusinessName, cus.IsBusinessContact, 
                cus.CustomerID, cus.Email, cus.Phone, cus.Mobile, cus.Address1, cus.City, cus.State, cus.ZipCode, cus.Country,
                apt.CompanyID, apt.ApptID, apt.ResourceID, apt.ServiceType AS StoredServiceType, apt.CreatedDateTime, 
                CONVERT(VARCHAR(10), apt.ApptDateTime, 120) as RequestDate, apt.Note, apt.TimeSlot,
                apt.StartDateTime, apt.EndDateTime, apt.Hour, apt.Minute, rs.Name as ResourceName, 
                srv.ServiceTypeID,
                srv.ServiceName,
                srv.CalenderColor as ServiceColor,
                sts.StatusID AS AppointmentStatusID,
                CASE
                    WHEN sts.StatusName = 'Scheduled' THEN 'Confirmed'
                    ELSE COALESCE(sts.StatusName, apt.Status, 'Unknown')
                END AS AppoinmentStatus,
                COALESCE(sts.CalenderColor, '#3b82f6') AS StatusColor, 
                tkt.StatusID AS TicketStatusID,
                COALESCE(tkt.StatusName, apt.TicketStatus, 'Unknown') AS TicketStatus,
                cs.Id as SiteId, cs.SiteName, cs.Address as SiteAddress, cs.Contact as SiteContact,
    cs.Email as SiteEmail, cs.PhoneNumber as SitePhoneNumber, cs.Note as SiteNote, cs.IsActive as SiteIsActive,
    cs.State as SiteState, cs.Zip as SiteZip, cs.Country as SiteCountry, cs.City as SiteCity
            FROM 
                tbl_Appointment apt
            LEFT JOIN 
                tbl_Customer cus ON apt.CustomerID = cus.CustomerID AND apt.CompanyID = cus.CompanyID
            LEFT JOIN 
                tbl_CustomerSite cs ON apt.SiteId = cs.Id AND apt.CustomerID = cs.CustomerID
            LEFT JOIN 
                tbl_Resources as rs ON apt.ResourceID = rs.Id AND apt.CompanyID = rs.CompanyID
            LEFT JOIN 
                tbl_ServiceType AS srv ON apt.CompanyID = srv.CompanyID AND 
                    (TRY_CAST(apt.ServiceType AS INT) = srv.ServiceTypeID OR apt.ServiceType = srv.ServiceName)
            LEFT JOIN 
                tbl_Status AS sts ON apt.CompanyID = sts.CompanyID AND 
                    (CASE WHEN apt.Status = '0' OR apt.Status IS NULL OR apt.Status = '' THEN 1 ELSE TRY_CAST(apt.Status AS INT) END = sts.StatusID OR apt.Status = sts.StatusName)
            LEFT JOIN 
                tbl_TicketStatus AS tkt ON apt.CompanyID = tkt.CompanyID AND 
                    (CASE WHEN apt.TicketStatus = '0' OR apt.TicketStatus IS NULL OR apt.TicketStatus = '' THEN 1 ELSE TRY_CAST(apt.TicketStatus AS INT) END = tkt.StatusID OR apt.TicketStatus = tkt.StatusName)
            {joinCondition}
            {whereCondition}
            ORDER BY 
                apt.ApptDateTime DESC";

                // Add all parameters BEFORE executing the query
                db.Command.Parameters.AddWithValue("@CompanyID", companyid);
                if (!string.IsNullOrEmpty(searchValue)) db.Command.Parameters.AddWithValue("@SearchValue", "%" + searchValue + "%");
                if (!string.IsNullOrEmpty(today)) db.Command.Parameters.AddWithValue("@DateFilter", today);
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {
                    db.Command.Parameters.AddWithValue("@FromDate", fromDate);
                    db.Command.Parameters.AddWithValue("@ToDate", toDate);
                }

                if (!string.IsNullOrEmpty(resourceGroupId) && resourceGroupId != "all")
                {
                    db.Command.Parameters.AddWithValue("@ResourceGroupID", resourceGroupId);
                }

                if (!string.IsNullOrEmpty(customerId))
                {
                    db.Command.Parameters.AddWithValue("@CustomerID", customerId);
                }
                if (!string.IsNullOrEmpty(siteId) && siteId != "0")
                {
                    db.Command.Parameters.AddWithValue("@SiteID", siteId);
                }

                // Execute query AFTER all parameters are added
                db.ExecuteParam(sql, out dt);

                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var appoinment = new AppointmentModel();

                        appoinment.CompanyID = companyid;
                        appoinment.CustomerGuid = row.Field<string>("CustomerGuid") ?? "";
                        appoinment.CustomerID = row["CustomerID"].ToString();
                        appoinment.FirstName = row.Field<string>("FirstName") ?? "";
                        appoinment.LastName = row.Field<string>("LastName") ?? "";
                        appoinment.BusinessID = row.Field<int?>("BusinessID") ?? 0;
                        appoinment.BusinessName = row.Field<string>("BusinessName") ?? "";
                        appoinment.IsBusinessContact = row.Field<bool?>("IsBusinessContact") ?? false;
                        appoinment.Phone = row.Field<string>("Phone") ?? "";
                        appoinment.Mobile = row.Field<string>("Mobile") ?? "";
                        appoinment.ZipCode = row.Field<string>("ZipCode") ?? "";
                        appoinment.State = LocationHelper.GetFullName(row.Field<string>("State") ?? "");
                        appoinment.City = row.Field<string>("City") ?? "";
                        appoinment.Address1 = row.Field<string>("Address1") ?? "";
                        appoinment.Country = row.Field<string>("Country") ?? "";
                        appoinment.Email = row.Field<string>("Email") ?? "";
                        appoinment.CustomerName = row.Field<string>("FirstName") + " " + row.Field<string>("LastName");
                        appoinment.AppoinmentId = row["ApptID"].ToString();
                        appoinment.Note = row.Field<string>("Note") ?? "";
                        appoinment.ResourceName = row.Field<string>("ResourceName") ?? "";
                        appoinment.ResourceID = row.Field<int?>("ResourceID") ?? 0;
                        appoinment.RequestDate = row.Field<string>("RequestDate") ?? "";

                        if (row["StartDateTime"] != DBNull.Value)
                        {
                            DateTime startDateTime = Convert.ToDateTime(row["StartDateTime"]);
                            appoinment.StartDateTime = startDateTime.ToString("MM/dd/yyyy hh:mm tt");

                            int minute = startDateTime.Minute < 30 ? 0 : 30;
                            DateTime timeSlotStart = new DateTime(startDateTime.Year, startDateTime.Month, startDateTime.Day,
                                startDateTime.Hour, minute, 0);
                            DateTime timeSlotEnd = timeSlotStart.AddMinutes(30);
                            appoinment.TimeSlot = string.Format("{0} - {1}", timeSlotStart.ToString("h:mm tt"),
                                timeSlotEnd.ToString("h:mm tt"));
                        }
                        else
                        {
                            appoinment.TimeSlot = row.Field<string>("TimeSlot") ?? "";
                        }
                        appoinment.AppoinmentDate = row.Field<string>("RequestDate") ?? "";

                        appoinment.ServiceType = row.Field<string>("ServiceName") ?? row.Field<string>("StoredServiceType") ?? "N/A";
                        appoinment.ServiceTypeID = row.Field<int?>("ServiceTypeID") ?? 0;
                        appoinment.AppoinmentStatus = row.Field<string>("AppoinmentStatus") ?? "N/A";
                        appoinment.AppoinmentStatusID = row.Field<int?>("AppointmentStatusID") ?? 0;
                        appoinment.TicketStatus = row.Field<string>("TicketStatus") ?? "N/A";
                        appoinment.TicketStatusID = row.Field<int?>("TicketStatusID") ?? 0;

                        appoinment.ServiceColor = row.Field<string>("ServiceColor") ?? "#3b82f6";
                        appoinment.StatusColor = row.Field<string>("StatusColor") ?? "#3b82f6";
                        if (row["StartDateTime"] != DBNull.Value) appoinment.StartDateTime = Convert.ToDateTime(row["StartDateTime"]).ToString("MM/dd/yyyy hh:mm tt");
                        if (row["EndDateTime"] != DBNull.Value) appoinment.EndDateTime = Convert.ToDateTime(row["EndDateTime"]).ToString("MM/dd/yyyy hh:mm tt");

                        appoinment.SiteId = row["SiteId"] != DBNull.Value ? row["SiteId"].ToString() : "0";
                        appoinment.SiteName = row.Field<string>("SiteName") ?? "";
                        appoinment.SiteAddress = row.Field<string>("SiteAddress") ?? "";
                        appoinment.SiteContact = row.Field<string>("SiteContact") ?? "";
                        appoinment.SiteEmail = row.Field<string>("SiteEmail") ?? "";
                        appoinment.SitePhoneNumber = row.Field<string>("SitePhoneNumber") ?? "";
                        appoinment.SiteNote = row.Field<string>("SiteNote") ?? "";
                        appoinment.SiteIsActive = row.Field<bool?>("SiteIsActive") ?? false;
                        appoinment.SiteCity = row.Field<string>("SiteCity") ?? "";


                        if (row["Hour"] != DBNull.Value && row["Hour"] != null)
                        {
                            int hr = row.Field<int?>("Hour") ?? 0;
                            int min = row.Field<int?>("Minute") ?? 0;
                            appoinment.Duration = $"{hr} Hr : {min} Min";
                        }

                        else if (appoinment.ServiceTypeID > 0)
                        {
                            appoinment.Duration = CalculateDuration(appoinment.ServiceTypeID);
                        }

                        else
                        {
                            appoinment.Duration = "1 Hr : 0 Min";
                        }



                        appoinments.Add(appoinment);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in LoadAppoinments: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }
                // Return empty list on error to prevent page crash
                return new List<AppointmentModel>();
            }
            finally
            {
                if (db != null)
                {
                    db.Close();
                }
            }
            return appoinments;
        }


        public class AppointmentUpdateViewModel
        {
            public FSM.Entity.Appoinments.Appointment AppointmentData { get; set; }
            public FSM.Entity.Customer.CustomerSite SiteData { get; set; }
            public List<CustomFieldData> CustomFields { get; set; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateAppointmentWithViewModel(AppointmentUpdateViewModel viewModel)
        {
            if (viewModel == null || viewModel.AppointmentData == null)
            {
                return false;
            }

            if (HttpContext.Current.Session["CompanyID"] == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateAppointmentWithViewModel Error: Session expired or CompanyID is null.");
                return false;
            }

            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            SqlConnection con = null;
            SqlTransaction transaction = null;
            bool success = false;
            int appointmentId = 0;
            string oldStatus = "";
            int resourceId = 0;

            try
            {
                con = new SqlConnection(db.ConnectionString);
                con.Open();
                transaction = con.BeginTransaction();

                appointmentId = Convert.ToInt32(viewModel.AppointmentData.AppoinmentId);
                int statusId = viewModel.AppointmentData.StatusID;
                resourceId = viewModel.AppointmentData.ResourceID;

                // Validate resource assignment if required for status
                if (IsResourceRequired(statusId) && resourceId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateAppointmentWithViewModel blocked: Status {statusId} requires a resource.");
                    transaction.Rollback();
                    return false;
                }

                var appointment = viewModel.AppointmentData;
                var setClauses = new List<string>();

                var cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.Transaction = transaction;

                cmd.Parameters.AddWithValue("@CompanyID", CompanyID);
                cmd.Parameters.AddWithValue("@ApptID", appointmentId);

                if (!string.IsNullOrEmpty(appointment.ServiceType)) { setClauses.Add("[ServiceType] = @ServiceType"); cmd.Parameters.AddWithValue("@ServiceType", appointment.ServiceType); }
                if (!string.IsNullOrEmpty(appointment.TimeSlot)) { setClauses.Add("[TimeSlot] = @TimeSlot"); cmd.Parameters.AddWithValue("@TimeSlot", appointment.TimeSlot); }
                // Save TimeSlotId to sync with Jobs-Scheduler tbl_TimeBlocks
                if (appointment.TimeSlotId > 0) { setClauses.Add("[TimeSlotId] = @TimeSlotId"); cmd.Parameters.AddWithValue("@TimeSlotId", appointment.TimeSlotId); }
                if (!string.IsNullOrEmpty(appointment.RequestDate)) { setClauses.Add("[ApptDateTime] = @ApptDateTime"); cmd.Parameters.AddWithValue("@ApptDateTime", Convert.ToDateTime(appointment.RequestDate)); }
                if (appointment.ResourceID >= 0) { setClauses.Add("[ResourceID] = @ResourceID"); cmd.Parameters.AddWithValue("@ResourceID", appointment.ResourceID > 0 ? (object)appointment.ResourceID : DBNull.Value); }
                if (appointment.StatusID > 0) { setClauses.Add("[Status] = @Status"); cmd.Parameters.AddWithValue("@Status", appointment.StatusID.ToString()); }
                if (appointment.TicketStatusID > 0) { setClauses.Add("[TicketStatus] = @TicketStatus"); cmd.Parameters.AddWithValue("@TicketStatus", appointment.TicketStatusID.ToString()); }
                if (appointment.Note != null) { setClauses.Add("[Note] = @Note"); cmd.Parameters.AddWithValue("@Note", appointment.Note); }
                if (appointment.SiteId > 0) { setClauses.Add("[SiteId] = @SiteId"); cmd.Parameters.AddWithValue("@SiteId", appointment.SiteId); } else { setClauses.Add("[SiteId] = NULL"); }
                if (!string.IsNullOrEmpty(appointment.StartDateTime)) { setClauses.Add("[StartDateTime] = @StartDateTime"); cmd.Parameters.AddWithValue("@StartDateTime", Convert.ToDateTime(appointment.StartDateTime)); }
                if (!string.IsNullOrEmpty(appointment.EndDateTime)) { setClauses.Add("[EndDateTime] = @EndDateTime"); cmd.Parameters.AddWithValue("@EndDateTime", Convert.ToDateTime(appointment.EndDateTime)); }
                if (appointment.Hour >= 0) { setClauses.Add("[Hour] = @Hour"); cmd.Parameters.AddWithValue("@Hour", appointment.Hour); }
                if (appointment.Minute >= 0) { setClauses.Add("[Minute] = @Minute"); cmd.Parameters.AddWithValue("@Minute", appointment.Minute); }

                if (appointment.StatusID > 0)
                {
                    using (var checkCmd = new SqlCommand("SELECT Status FROM [msSchedulerV3].[dbo].[tbl_Appointment] WHERE ApptID = @ApptID AND CompanyID = @CompanyID", con, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@ApptID", appointmentId);
                        checkCmd.Parameters.AddWithValue("@CompanyID", CompanyID);
                        var result = checkCmd.ExecuteScalar();
                        if (result != null) oldStatus = result.ToString();
                    }
                }

                if (setClauses.Any())
                {
                    cmd.CommandText = $"UPDATE [msSchedulerV3].[dbo].[tbl_Appointment] SET {string.Join(", ", setClauses)} WHERE [ApptID] = @ApptID AND [CompanyID] = @CompanyID;";
                    cmd.ExecuteNonQuery();

                    // Manually log history if Status changed
                    if (appointment.StatusID > 0 && !string.IsNullOrEmpty(oldStatus) && oldStatus != appointment.StatusID.ToString())
                    {
                        try
                        {
                            string historySql = @"INSERT INTO [msSchedulerV3].[dbo].[tbl_AppointmentStatusHistory]
                                           ([AppointmentId], [CompanyID], [PreviousStatus], [NewStatus], [StatusChangeDateTime], [ChangedBy], [Notes], [CreatedDateTime])
                                           VALUES
                                           (@ApptID, @CompanyID, @PreviousStatus, @NewStatus, GETDATE(), @ChangedBy, @Note, GETDATE())";

                            using (var histCmd = new SqlCommand(historySql, con, transaction))
                            {
                                histCmd.Parameters.AddWithValue("@ApptID", appointmentId);
                                histCmd.Parameters.AddWithValue("@CompanyID", CompanyID);
                                histCmd.Parameters.AddWithValue("@PreviousStatus", oldStatus);
                                histCmd.Parameters.AddWithValue("@NewStatus", appointment.StatusID.ToString());

                                string currentUser = HttpContext.Current.Session["LoginUser"] as string ?? "System";
                                histCmd.Parameters.AddWithValue("@ChangedBy", currentUser);

                                if (appointment.Note != null)
                                    histCmd.Parameters.AddWithValue("@Note", appointment.Note);
                                else
                                    histCmd.Parameters.AddWithValue("@Note", DBNull.Value);

                                histCmd.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error logging history: {ex.Message}");
                        }
                    }
                }

                var site = viewModel.SiteData;
                if (site != null && site.Id > 0)
                {
                    var siteCmd = new SqlCommand();
                    siteCmd.Connection = con;
                    siteCmd.Transaction = transaction;

                    siteCmd.CommandText = @"UPDATE [msSchedulerV3].[dbo].[tbl_CustomerSite] SET
                                    Address = @Address,
                                    City = @City,
                                    Country = @Country,
                                    State = @State,
                                    Zip = @Zip
                                 WHERE Id = @SiteId AND CustomerID = @CustomerID AND CompanyID = @CompanyID;";

                    siteCmd.Parameters.AddWithValue("@Address", site.Address);
                    siteCmd.Parameters.AddWithValue("@City", site.City ?? "");
                    siteCmd.Parameters.AddWithValue("@Country", site.Country);
                    siteCmd.Parameters.AddWithValue("@State", site.State);
                    siteCmd.Parameters.AddWithValue("@Zip", site.Zip);
                    siteCmd.Parameters.AddWithValue("@SiteId", site.Id);
                    siteCmd.Parameters.AddWithValue("@CustomerID", appointment.CustomerID);
                    siteCmd.Parameters.AddWithValue("@CompanyID", CompanyID);

                    siteCmd.ExecuteNonQuery();
                }

                if (viewModel.CustomFields != null)
                {
                    // Call internal overload that accepts connection and transaction
                    SaveCustomFieldDataInternal(appointmentId, viewModel.CustomFields, con, transaction);
                }

                transaction.Commit();
                success = true;
            }
            catch (Exception ex)
            {
                if (transaction != null && transaction.Connection != null) transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"UpdateAppointment Error: {ex.ToString()}");
                return false;
            }
            finally
            {
                if (con != null && con.State == ConnectionState.Open) con.Close();
                if (con != null) con.Dispose();
            }

            // Process status communication AFTER successful transaction commit
            if (success && viewModel.AppointmentData.StatusID > 0 && !string.IsNullOrEmpty(oldStatus) && oldStatus != viewModel.AppointmentData.StatusID.ToString())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[Appointments] Status changed from {oldStatus} to {viewModel.AppointmentData.StatusID}. Processing communication...");
                    ProcessStatusCommunication(appointmentId, oldStatus, viewModel.AppointmentData.StatusID.ToString(), resourceId, CompanyID);
                }
                catch (Exception commEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Appointments] ERROR processing status communication: {commEx.Message}");
                }
            }

            return success;
        }




        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ServiceType> GetServiceTypesWithColors()
        {
            var serviceTypes = new List<ServiceType>();
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();

            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT ServiceTypeID, ServiceName, CalenderColor 
                       FROM tbl_ServiceType 
                       WHERE CompanyID = @CompanyID AND (Source IS NULL OR Source != 'CEC')
                       ORDER BY ServiceName";

                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var serviceType = new ServiceType();
                        serviceType.ServiceTypeID = row.Field<int>("ServiceTypeID");
                        serviceType.ServiceName = row.Field<string>("ServiceName") ?? "";
                        serviceType.CalendarColor = row.Field<string>("CalenderColor") ?? "#3b82f6"; // Default blue
                        serviceTypes.Add(serviceType);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                db.Close();
            }

            return serviceTypes;
        }

        public class ServiceType
        {
            public int ServiceTypeID { get; set; }
            public string ServiceName { get; set; }
            public string CalendarColor { get; set; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<StatusWithColor> GetStatusesWithColors()
        {
            var statuses = new List<StatusWithColor>();
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();

            try
            {
                db.Open();
                DataTable dt = new DataTable();
                // Note: This query will fail if CalenderColor column doesn't exist yet
                // Run ADD_STATUS_COLORS.sql first to add the column
                string sql = @"SELECT StatusID, StatusName, ISNULL(CalenderColor, '#3b82f6') AS CalenderColor
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

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var status = new StatusWithColor();
                        status.StatusID = row.Field<int>("StatusID");
                        status.StatusName = row.Field<string>("StatusName") ?? "";
                        // Change "Scheduled" to "Confirmed" for display
                        if (status.StatusName == "Scheduled")
                        {
                            status.StatusName = "Confirmed";
                        }
                        status.CalendarColor = row.Field<string>("CalenderColor") ?? "#3b82f6"; // Default blue
                        statuses.Add(status);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetStatusesWithColors: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                // If CalenderColor column doesn't exist, return empty list
                // The UI will handle this gracefully and use default colors
                if (ex.Message.Contains("CalenderColor") || ex.Message.Contains("Invalid column"))
                {
                    System.Diagnostics.Debug.WriteLine("NOTE: CalenderColor column not found. Please run ADD_STATUS_COLORS.sql to add the column.");
                }
            }
            finally
            {
                db.Close();
            }

            return statuses;
        }

        public class StatusWithColor
        {
            public int StatusID { get; set; }
            public string StatusName { get; set; }
            public string CalendarColor { get; set; }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static int GetServiceTypeIDByName(string serviceName, string companyId)
        {
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = "SELECT ServiceTypeID FROM tbl_ServiceType WHERE ServiceName = @ServiceName AND CompanyID = @CompanyID";

                db.AddParameter("@ServiceName", serviceName, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyId, SqlDbType.NVarChar);

                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0]["ServiceTypeID"]);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                db.Close();
            }
            return 0;
        }
        public void LoadData()
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                string Sql = "SELECT ServiceTypeID, ServiceName, CalenderColor From msSchedulerV3.dbo.tbl_ServiceType where CompanyID = '" + companyid + "' AND (Source IS NULL OR Source != 'CEC') order by ServiceName asc;";
                Sql += @"SELECT [StatusID], [StatusName] 
         FROM [msSchedulerV3].[dbo].[tbl_Status] 
         WHERE CompanyID='" + companyid + @"' AND StatusName != 'FA-ID Sent'
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
                Sql += @"SELECT [StatusID], [StatusName] FROM [msSchedulerV3].[dbo].[tbl_TicketStatus] where CompanyID= '" + companyid + "';";

                DataTable _ServiceType = new DataTable();
                DataTable _Status = new DataTable();
                DataTable _ticketStatus = new DataTable();
                DataSet dataSet = db.Get_DataSet(Sql, companyid);
                _ServiceType = dataSet.Tables[0];
                _Status = dataSet.Tables[1];

                // Change "Scheduled" to "Confirmed" for display purposes
                foreach (DataRow row in _Status.Rows)
                {
                    if (row["StatusName"].ToString() == "Scheduled")
                    {
                        row["StatusName"] = "Confirmed";
                    }
                }
                _ticketStatus = dataSet.Tables[2];

                if (_ServiceType.Rows.Count > 0)
                {
                    var listItem = new ListItem("Select Appointment Type", "all");

                    ServiceTypeFilter.DataSource = _ServiceType;
                    ServiceTypeFilter.DataTextField = "ServiceName";
                    ServiceTypeFilter.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter.DataBind();
                    ServiceTypeFilter.Items.Insert(0, listItem);

                    ServiceTypeFilter_ResourceView.DataSource = _ServiceType;
                    ServiceTypeFilter_ResourceView.DataTextField = "ServiceName";
                    ServiceTypeFilter_ResourceView.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_ResourceView.DataBind();
                    ServiceTypeFilter_ResourceView.Items.Insert(0, listItem);

                    ServiceTypeFilter_ListView.DataSource = _ServiceType;
                    ServiceTypeFilter_ListView.DataTextField = "ServiceName";
                    ServiceTypeFilter_ListView.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_ListView.DataBind();
                    ServiceTypeFilter_ListView.Items.Insert(0, listItem);

                    ServiceTypeFilter_MapView.DataSource = _ServiceType;
                    ServiceTypeFilter_MapView.DataTextField = "ServiceName";
                    ServiceTypeFilter_MapView.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_MapView.DataBind();
                    ServiceTypeFilter_MapView.Items.Insert(0, listItem);

                    ServiceTypeFilter_2.DataSource = _ServiceType;
                    ServiceTypeFilter_2.DataTextField = "ServiceName";
                    ServiceTypeFilter_2.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_2.DataBind();
                    ServiceTypeFilter_2.Items.Insert(0, new ListItem("Select a Service", "all"));

                    ServiceTypeFilter_Edit.DataSource = _ServiceType;
                    ServiceTypeFilter_Edit.DataTextField = "ServiceName";
                    ServiceTypeFilter_Edit.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_Edit.DataBind();
                    ServiceTypeFilter_Edit.Items.Insert(0, new ListItem("Select a Service", ""));

                    ServiceTypeFilter_Resource.DataSource = _ServiceType;
                    ServiceTypeFilter_Resource.DataTextField = "ServiceName";
                    ServiceTypeFilter_Resource.DataValueField = "ServiceTypeID";
                    ServiceTypeFilter_Resource.DataBind();
                    ServiceTypeFilter_Resource.Items.Insert(0, new ListItem("Select a Service", "all"));

                    dispatchGroupMapView.Items.Clear();
                    dispatchGroupMapView.Items.Insert(0, new ListItem("All resource group", "all"));

                    DataTable _Resources = new DataTable();
                    Sql = @"SELECT [Id], [Name] FROM [msSchedulerV3].[dbo].[tbl_Resources] WHERE CompanyID = '" + companyid + "' ORDER BY Name;";
                    _Resources = db.Get_DataSet(Sql, companyid).Tables[0];
                    individualResourceFilterMapView.DataSource = _Resources;
                    individualResourceFilterMapView.DataTextField = "Name";
                    individualResourceFilterMapView.DataValueField = "Id";
                    individualResourceFilterMapView.DataBind();
                    individualResourceFilterMapView.Items.Insert(0, new ListItem("All individual resources", "all"));
                }

                if (_Status.Rows.Count > 0)
                {
                    var listItem = new ListItem("Select a Status", "all");

                    StatusTypeFilter_DateView.DataSource = _Status;
                    StatusTypeFilter_DateView.DataTextField = "StatusName";
                    StatusTypeFilter_DateView.DataValueField = "StatusID";
                    StatusTypeFilter_DateView.DataBind();
                    StatusTypeFilter_DateView.Items.Insert(0, listItem);

                    StatusTypeFilter_ResourceView.DataSource = _Status;
                    StatusTypeFilter_ResourceView.DataTextField = "StatusName";
                    StatusTypeFilter_ResourceView.DataValueField = "StatusID";
                    StatusTypeFilter_ResourceView.DataBind();
                    StatusTypeFilter_ResourceView.Items.Insert(0, listItem);

                    StatusTypeFilter_ListView.DataSource = _Status;
                    StatusTypeFilter_ListView.DataTextField = "StatusName";
                    StatusTypeFilter_ListView.DataValueField = "StatusID";
                    StatusTypeFilter_ListView.DataBind();
                    StatusTypeFilter_ListView.Items.Insert(0, listItem);

                    StatusTypeFilter_MapView.DataSource = _Status;
                    StatusTypeFilter_MapView.DataTextField = "StatusName";
                    StatusTypeFilter_MapView.DataValueField = "StatusID";
                    StatusTypeFilter_MapView.DataBind();
                    StatusTypeFilter_MapView.Items.Insert(0, listItem);

                    StatusTypeFilter.DataSource = _Status;
                    StatusTypeFilter.DataTextField = "StatusName";
                    StatusTypeFilter.DataValueField = "StatusID";
                    StatusTypeFilter.DataBind();
                    StatusTypeFilter.Items.Insert(0, new ListItem("Active appointments", ""));
                    StatusTypeFilter.Items.Insert(1, new ListItem("All (incl. Closed/Cancelled)", "all_inclusive"));

                    StatusTypeFilter_Resource.DataSource = _Status;
                    StatusTypeFilter_Resource.DataTextField = "StatusName";
                    StatusTypeFilter_Resource.DataValueField = "StatusID";
                    StatusTypeFilter_Resource.DataBind();
                    StatusTypeFilter_Resource.Items.Insert(0, new ListItem("Active appointments", ""));
                    StatusTypeFilter_Resource.Items.Insert(1, new ListItem("All (incl. Closed/Cancelled)", "all_inclusive"));

                    StatusTypeFilter_Edit.DataSource = _Status;
                    StatusTypeFilter_Edit.DataTextField = "StatusName";
                    StatusTypeFilter_Edit.DataValueField = "StatusID";
                    StatusTypeFilter_Edit.DataBind();
                    StatusTypeFilter_Edit.Items.Insert(0, new ListItem("Select a status..", ""));
                }

                if (_ticketStatus.Rows.Count > 0)
                {
                    var listItem = new ListItem("Select a Ticket Status", "all");

                    TicketStatusFilter_DateView.DataSource = _ticketStatus;
                    TicketStatusFilter_DateView.DataTextField = "StatusName";
                    TicketStatusFilter_DateView.DataValueField = "StatusID";
                    TicketStatusFilter_DateView.DataBind();
                    TicketStatusFilter_DateView.Items.Insert(0, listItem);

                    TicketStatusFilter_ResourceView.DataSource = _ticketStatus;
                    TicketStatusFilter_ResourceView.DataTextField = "StatusName";
                    TicketStatusFilter_ResourceView.DataValueField = "StatusID";
                    TicketStatusFilter_ResourceView.DataBind();
                    TicketStatusFilter_ResourceView.Items.Insert(0, listItem);

                    TicketStatusFilter_ListView.DataSource = _ticketStatus;
                    TicketStatusFilter_ListView.DataTextField = "StatusName";
                    TicketStatusFilter_ListView.DataValueField = "StatusID";
                    TicketStatusFilter_ListView.DataBind();
                    TicketStatusFilter_ListView.Items.Insert(0, listItem);

                    TicketStatusFilter_MapView.DataSource = _ticketStatus;
                    TicketStatusFilter_MapView.DataTextField = "StatusName";
                    TicketStatusFilter_MapView.DataValueField = "StatusID";
                    TicketStatusFilter_MapView.DataBind();
                    TicketStatusFilter_MapView.Items.Insert(0, listItem);

                    TicketStatusFilter_Edit.DataSource = _ticketStatus;
                    TicketStatusFilter_Edit.DataTextField = "StatusName";
                    TicketStatusFilter_Edit.DataValueField = "StatusID";
                    TicketStatusFilter_Edit.DataBind();
                    TicketStatusFilter_Edit.Items.Insert(0, new ListItem("Select a ticket statuse..", ""));
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                db.Close();
            }
        }



        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<TimeSlot> GetTimeSlots()
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var timeSlots = new List<TimeSlot>();
            Database db = new Database();
            
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                // Load actual time blocks from tbl_TimeBlocks for the dropdown (same as Jobs-Scheduler uses)
                string sql = @"SELECT ID, 
                                     TimeBlock + ' ( ' + CONVERT(varchar(10), StartTime, 100) + ' - ' + CONVERT(varchar(10), EndTime, 100) + ' )' AS TimeBlockSchedule,
                                     TimeBlock + ' ( ' + CONVERT(varchar(10), StartTime, 100) + ' )' AS TimeBlock,
                                     FORMAT(StartTime, 'hh:mm tt') AS StartTime,
                                     FORMAT(EndTime, 'hh:mm tt') AS EndTime
                              FROM [msSchedulerV3].[dbo].[tbl_TimeBlocks] 
                              WHERE [IsDeleted] = 0 AND [IsFromCalender] = 0 AND CompanyID = @CompanyID
                              ORDER BY StartTime";
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();
                
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var timeSlot = new TimeSlot
                        {
                            ID = row.Field<int>("ID"),
                            StartTime = row.Field<string>("StartTime"),
                            EndTime = row.Field<string>("EndTime"),
                            TimeBlock = row.Field<string>("TimeBlock"),
                            TimeBlockSchedule = row.Field<string>("TimeBlockSchedule"),
                            CompanyID = CompanyID
                        };
                        timeSlots.Add(timeSlot);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetTimeSlots (tbl_TimeBlocks): " + ex.Message);
                if (db != null) db.Close();
            }
            finally
            {
                if (db != null) db.Close();
            }
            
            return timeSlots;
        }
        
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<TimeSlot> GetCalendarGridTimeSlots()
        {
            // Returns fixed 30-minute intervals for calendar grid display (7:00 AM - 7:00 PM)
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var timeSlots = new List<TimeSlot>();
            var startTime = new TimeSpan(7, 0, 0);
            var endTime = new TimeSpan(19, 0, 0);
            var interval = new TimeSpan(0, 30, 0);
            
            for (var time = startTime; time < endTime; time = time.Add(interval))
            {
                var endOfSlot = time.Add(interval);
                var timeSlot = new TimeSlot
                {
                    ID = 0,
                    StartTime = DateTime.Today.Add(time).ToString("hh:mm tt"),
                    EndTime = DateTime.Today.Add(endOfSlot).ToString("hh:mm tt"),
                    TimeBlock = DateTime.Today.Add(time).ToString("h:mm tt"),
                    TimeBlockSchedule = string.Format("{0} - {1}", DateTime.Today.Add(time).ToString("h:mm tt"),
                         DateTime.Today.Add(endOfSlot).ToString("h:mm tt")),
                    CompanyID = CompanyID
                };
                timeSlots.Add(timeSlot);
            }
            return timeSlots;
        }
        
        public class TimeSlot
        {
            public int ID { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string TimeBlockSchedule { get; set; }
            public string TimeBlock { get; set; }
            public string CompanyID { get; set; }
        }

        public class ResourceGroup
        {
            public int Id { get; set; }
            public string GroupName { get; set; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<ResourceGroup> GetResourceGroups()
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var resourceGroups = new List<ResourceGroup>();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT [Id], [GroupName] FROM [myServiceJobs].[dbo].[tbl_ResourceGroups] WHERE CompanyID = @CompanyID AND IsActive = 1 ORDER BY GroupName;";
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var resourceGroup = new ResourceGroup();
                        resourceGroup.Id = row.Field<int>("Id");
                        resourceGroup.GroupName = row.Field<string>("GroupName") ?? "";
                        resourceGroups.Add(resourceGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                db.Close();
                return resourceGroups;
            }
            finally
            {
                db.Close();
            }
            return resourceGroups;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Resource> GetResourcesByServiceType(int serviceTypeId)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var resources = new List<Resource>();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                // Join tbl_Resources with the mapping table tbl_Resource_ServiceType
                string sql = @"
                    SELECT r.[Id], r.[Name] 
                    FROM [msSchedulerV3].[dbo].[tbl_Resources] r
                    INNER JOIN [msSchedulerV3].[dbo].[tbl_Resource_ServiceType] rs ON r.Id = rs.ResourceID
                    WHERE rs.ServiceTypeId = @ServiceTypeId AND r.CompanyID = @CompanyID
                    ORDER BY r.Name;";

                db.AddParameter("@ServiceTypeId", serviceTypeId, SqlDbType.Int);
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var resource = new Resource();
                        resource.Id = row.Field<int?>("Id") ?? 0;
                        resource.ResourceName = row.Field<string>("Name") ?? "";
                        resources.Add(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                db.Close();
                return resources;
            }
            finally
            {
                db.Close();
            }
            return resources;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Resource> GetResourcess()
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var resources = new List<Resource>();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT  [Id], [Name] FROM[msSchedulerV3].[dbo].[tbl_Resources] where companyid = '" + CompanyID + "' Order By Name;";

                db.Execute(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var resource = new Resource();
                        resource.Id = row.Field<int?>("Id") ?? 0;
                        resource.ResourceName = row.Field<string>("Name") ?? "";
                        resources.Add(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                db.Close();
                return resources;
            }
            finally
            {
                db.Close();
            }
            return resources;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateAppointmentDuration(int AppoinmentId, string StartDateTime, string EndDateTime, int Hour, int Minute)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(CompanyID))
            {
                System.Diagnostics.Debug.WriteLine("UpdateAppointmentDuration Error: Session expired or CompanyID is null.");
                return false;
            }

            Database db = new Database();
            try
            {
                db.Open();
                string sql = @"
                    UPDATE [msSchedulerV3].[dbo].[tbl_Appointment]
                    SET 
                        StartDateTime = @StartDateTime,
                        EndDateTime = @EndDateTime,
                        Hour = @Hour,
                        Minute = @Minute
                    WHERE 
                        ApptID = @AppoinmentId AND CompanyID = @CompanyID;";

                db.AddParameter("@AppoinmentId", AppoinmentId, SqlDbType.Int);
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.VarChar);
                db.AddParameter("@StartDateTime", Convert.ToDateTime(StartDateTime), SqlDbType.DateTime);
                db.AddParameter("@EndDateTime", Convert.ToDateTime(EndDateTime), SqlDbType.DateTime);
                db.AddParameter("@Hour", Hour, SqlDbType.Int);
                db.AddParameter("@Minute", Minute, SqlDbType.Int);

                bool success = db.UpdateSql(sql);
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateAppointmentDuration WebMethod: {ex.Message}");
                return false;
            }
            finally
            {
                db.Close();
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Boolean UpdateAppointment(Appointment appointment)
        {
            bool success = false;
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string companyName = HttpContext.Current.Session["CompanyName"].ToString();
            Database db = new Database();

            try
            {
                // Get old status to check for changes
                string oldStatus = "";
                db.Open();
                DataTable dtOld = new DataTable();
                string checkSql = "SELECT Status FROM [msSchedulerV3].[dbo].[tbl_Appointment] WHERE ApptID = @ApptID AND CompanyID = @CompanyID";
                db.AddParameter("@ApptID", appointment.AppoinmentId, SqlDbType.Int);
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.ExecuteParam(checkSql, out dtOld);
                if (dtOld.Rows.Count > 0) oldStatus = dtOld.Rows[0]["Status"].ToString();
                if (oldStatus == "0" || string.IsNullOrEmpty(oldStatus)) oldStatus = "Pending";
                db.Close();

                var setClauses = new List<string>();

                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", CompanyID);
                db.Command.Parameters.AddWithValue("@ApptID", appointment.AppoinmentId);

                // Validate resource assignment if status requires it
                if (!string.IsNullOrEmpty(appointment.Status))
                {
                    if (int.TryParse(appointment.Status, out int statusId) && IsResourceRequired(statusId))
                    {
                        if (appointment.ResourceID <= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"UpdateAppointment blocked: Status {statusId} requires a resource.");
                            return false;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(appointment.ServiceType))
                {
                    setClauses.Add("[ServiceType] = @ServiceType");
                    db.Command.Parameters.AddWithValue("@ServiceType", appointment.ServiceType);
                }
                if (!string.IsNullOrEmpty(appointment.TimeSlot))
                {
                    setClauses.Add("[TimeSlot] = @TimeSlot");
                    db.Command.Parameters.AddWithValue("@TimeSlot", appointment.TimeSlot);
                }
                // Save TimeSlotId to sync with Jobs-Scheduler tbl_TimeBlocks
                if (appointment.TimeSlotId > 0)
                {
                    setClauses.Add("[TimeSlotId] = @TimeSlotId");
                    db.Command.Parameters.AddWithValue("@TimeSlotId", appointment.TimeSlotId);
                }
                if (!string.IsNullOrEmpty(appointment.RequestDate))
                {
                    setClauses.Add("[ApptDateTime] = @ApptDateTime");
                    db.Command.Parameters.AddWithValue("@ApptDateTime", Convert.ToDateTime(appointment.RequestDate));
                }
                if (!string.IsNullOrEmpty(appointment.Status))
                {
                    string statusToSave = appointment.Status;
                    if (statusToSave == "0") statusToSave = "Pending";
                    
                    setClauses.Add("[Status] = @Status");
                    db.Command.Parameters.AddWithValue("@Status", statusToSave);
                }
                if (appointment.ResourceID >= 0)
                {
                    setClauses.Add("[ResourceID] = @ResourceID");
                    db.Command.Parameters.AddWithValue("@ResourceID", appointment.ResourceID > 0 ? (object)appointment.ResourceID : DBNull.Value);
                }
                if (!string.IsNullOrEmpty(appointment.TicketStatus))
                {
                    // This ensures only an integer ID is saved for TicketStatus.
                    if (int.TryParse(appointment.TicketStatus, out int ticketStatusId) && ticketStatusId > 0)
                    {
                        setClauses.Add("[TicketStatus] = @TicketStatus");
                        db.Command.Parameters.AddWithValue("@TicketStatus", ticketStatusId);
                    }
                }

                if (appointment.Note != null)
                {
                    setClauses.Add("[Note] = @Note");
                    db.Command.Parameters.AddWithValue("@Note", appointment.Note);
                }
                if (appointment.SiteId > 0)
                {
                    setClauses.Add("[SiteId] = @SiteId");
                    db.Command.Parameters.AddWithValue("@SiteId", appointment.SiteId);
                }
                else
                {

                    setClauses.Add("[SiteId] = NULL");
                }
                if (!string.IsNullOrEmpty(appointment.StartDateTime))
                {
                    setClauses.Add("[StartDateTime] = @StartDateTime");
                    db.Command.Parameters.AddWithValue("@StartDateTime", Convert.ToDateTime(appointment.StartDateTime));
                }
                if (!string.IsNullOrEmpty(appointment.EndDateTime))
                {
                    setClauses.Add("[EndDateTime] = @EndDateTime");
                    db.Command.Parameters.AddWithValue("@EndDateTime", Convert.ToDateTime(appointment.EndDateTime));
                }

                if (setClauses.Count == 0)
                {
                    return true;
                }

                string strSQL = $@"UPDATE [msSchedulerV3].[dbo].[tbl_Appointment]
                           SET {string.Join(", ", setClauses)}
                           WHERE [ApptID] = @ApptID AND [CompanyID] = @CompanyID;";

                success = db.UpdateSql(strSQL);

                if (success == true)
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
                            db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                            db.AddParameter("@PreviousStatus", oldStatus, SqlDbType.NVarChar);
                            db.AddParameter("@NewStatus", appointment.Status, SqlDbType.NVarChar);
                            string currentUser = HttpContext.Current.Session["LoginUser"] as string ?? "System";
                            db.AddParameter("@ChangedBy", currentUser, SqlDbType.NVarChar);
                            db.AddParameter("@Note", (object)appointment.Note ?? DBNull.Value, SqlDbType.NVarChar);

                            db.UpdateSql(historySql);

                            // Process status communication (email/SMS) after status change using new processor
                            try
                            {
                                int apptId = 0;
                                int.TryParse(appointment.AppoinmentId, out apptId);
                                int resourceId = appointment.ResourceID;
                                ConsoleLog($"[Appointments.UpdateAppointment] Status changed from {oldStatus} to {appointment.Status}. Processing communication...");
                                ProcessStatusCommunication(apptId, oldStatus, appointment.Status, resourceId, CompanyID);
                            }
                            catch (Exception commEx)
                            {
                                ConsoleLog($"[Appointments.UpdateAppointment] ERROR processing status communication: {commEx.Message}\n{commEx.StackTrace}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error logging history in UpdateAppointment: {ex.Message}");
                        }
                    }

                    // Legacy SMS sending (kept for backward compatibility, but new processor handles it)
                    try
                    {
                        TwilioSMSService twilioSMS = new TwilioSMSService();
                        twilioSMS.SendAppointmentSMS(
                            appointment.AppoinmentId,
                            appointment.CustomerID,
                            appointment.Status,
                            CompanyID,
                            companyName,
                            appointment.RequestDate,
                            appointment.TimeSlot,
                            appointment.ResourceID
                        );
                    }
                    catch (Exception smsEx)
                    {

                        System.Diagnostics.Debug.WriteLine($"NON-FATAL ERROR: Failed to send SMS for ApptID {appointment.AppoinmentId}. Details: {smsEx.Message}");
                    }
                }
            }
            finally
            {
                db.Close();
            }
            return success;
        }




        public static CustomerEntity GetCustomerDetails(string customerId)
        {

            if (string.IsNullOrEmpty(customerId))
            {
                return null;
            }

            var customer = new CustomerEntity();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT CustomerGuid, FirstName, LastName, Phone, Mobile, Email, Address1, City, State, ZipCode, Country, CreatedDateTime 
               FROM [msSchedulerV3].[dbo].[tbl_Customer] 
               WHERE CustomerID = @CustomerID AND CompanyID = @CompanyID;";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    DataRow dataRow = dt.Rows[0];
                    customer.CustomerGuid = dataRow.Field<string>("CustomerGuid") ?? "";
                    customer.FirstName = dataRow.Field<string>("FirstName") ?? "";
                    customer.LastName = dataRow.Field<string>("LastName") ?? "";
                    customer.Phone = dataRow.Field<string>("Phone") ?? "";
                    customer.Mobile = dataRow.Field<string>("Mobile") ?? "";
                    customer.Email = dataRow.Field<string>("Email") ?? "";
                    customer.City = dataRow.Field<string>("City") ?? "";
                    customer.State = dataRow.Field<string>("State") ?? "";
                    customer.ZipCode = dataRow.Field<string>("ZipCode") ?? "";
                    customer.Country = dataRow.Field<string>("Country") ?? "";
                    customer.Address1 = dataRow.Field<string>("Address1") ?? "";
                    customer.CreatedDateTime = dataRow.Field<DateTime?>("CreatedDateTime") ?? DateTime.MinValue;
                }
                else
                {

                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetCustomerDetails: " + ex.Message);
                db.Close();
                return null;
            }
            return customer;
        }

        public static CustomerSite GetCustomerSitebyId(string customerId, int siteId)
        {
            var site = new CustomerSite();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                db.Open();
                string strSQL = @"SELECT Id, SiteName, Address, City, Note, IsActive, CreatedDateTime, FirstName, LastName, PhoneNumber, Email, State, Zip, Country 
                          FROM [msSchedulerV3].dbo.tbl_CustomerSite 
                          WHERE Id = @SiteID AND CustomerID = @CustomerID;";
                db.AddParameter("@SiteID", siteId, SqlDbType.Int);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(strSQL, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow dataRow = dt.Rows[0];
                    site.Id = dataRow.Field<int?>("Id") ?? 0;
                    site.SiteName = dataRow.Field<string>("SiteName") ?? "";
                    site.Address = dataRow.Field<string>("Address") ?? "";
                    site.Note = dataRow.Field<string>("Note") ?? "";
                    site.IsActive = dataRow.Field<bool?>("IsActive") ?? false;
                    site.CreatedDateTime = dataRow["CreatedDateTime"] as DateTime?;
                    site.FirstName = dataRow.Field<string>("FirstName") ?? "";
                    site.LastName = dataRow.Field<string>("LastName") ?? "";
                    site.Contact = $"{site.FirstName} {site.LastName}".Trim();
                    site.PhoneNumber = dataRow.Field<string>("PhoneNumber") ?? "";
                    site.Email = dataRow.Field<string>("Email") ?? "";
                    site.City = dataRow.Field<string>("City") ?? "";
                    site.State = dataRow.Field<string>("State") ?? "";
                    site.Zip = dataRow.Field<string>("Zip") ?? "";
                    site.Country = dataRow.Field<string>("Country") ?? "";

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetCustomerSitebyId (Appointments.aspx.cs): " + ex.Message);
            }
            finally
            {
                db.Close();
            }
            return site;
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetDuration(int serviceTypeID)
        {
            var duration = "0";
            if (serviceTypeID > 0)
            {
                duration = CalculateDuration(serviceTypeID);
            }
            return duration;
        }


        public static string CalculateDuration(int serviceTypeID)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            var duration = "0";
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"select Hour, Minute from [msSchedulerV3].[dbo].[tbl_ServiceType] where CompanyID = '" + CompanyID + "' and  ServiceTypeID ='" + serviceTypeID + "';";

                db.Execute(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    int hr = row.Field<int?>("Hour") ?? 0;
                    int min = row.Field<int?>("Minute") ?? 0;
                    duration = $"{hr} Hr : {min} Min";
                }
            }

            catch (Exception ex)
            {
                db.Close();
                return duration;
            }
            finally
            {
                db.Close();
            }
            return duration;
        }

        [WebMethod]
        public static bool UpdateAttachedForms(string appointmentId, string customerId, List<int> formIds)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = System.Web.HttpContext.Current.Session["UserID"]?.ToString();

                if (string.IsNullOrEmpty(companyId))
                    return false;

                // Convert formIds list to comma-separated string
                string formIdsString = formIds != null && formIds.Count > 0
                    ? string.Join(",", formIds)
                    : "";

                string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("sp_Appointments_UpdateAttachedForms", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@AppointmentId", appointmentId);
                        command.Parameters.AddWithValue("@CompanyID", companyId);
                        command.Parameters.AddWithValue("@FormIds", formIdsString);
                        command.Parameters.AddWithValue("@UpdatedBy", userId ?? "System");

                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating attached forms: {ex.Message}");
                return false;
            }
        }

        [WebMethod]
        public static bool SendFormsViaEmail(string appointmentId, string customerEmail)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return false;

                // Get appointment and customer details
                var appointment = GetAppointmentDetails(appointmentId, companyId);
                if (appointment == null)
                    return false;

                // Get attached forms
                var formProcessor = new FSM.Processors.FormProcessor();
                var forms = formProcessor.GetAppointmentForms(appointmentId, companyId);

                if (forms.Count == 0)
                {
                    throw new Exception("No forms attached to this appointment");
                }

                // Generate email content
                string subject = $"Forms for Appointment #{appointmentId}";
                string body = GenerateFormsEmailBody(appointment, forms);

                // Send email (you'll need to implement your email service)
                return SendEmail(customerEmail, subject, body, appointmentId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending forms via email: " + ex.Message);
            }
        }

        [WebMethod]
        public static bool SendFormsViaSMS(string appointmentId, string customerPhone)
        {
            try
            {
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return false;

                // Get appointment details
                var appointment = GetAppointmentDetails(appointmentId, companyId);
                if (appointment == null)
                    return false;

                // Generate SMS content
                string message = GenerateFormsSMSMessage(appointment, appointmentId);

                // Send SMS (you'll need to implement your SMS service)
                return SendSMS(customerPhone, message, appointmentId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending forms via SMS: " + ex.Message);
            }
        }

        private static dynamic GetAppointmentDetails(string appointmentId, string companyId)
        {
            Database db = new Database();

            string connectionString = ConfigurationManager.AppSettings["ConnStrJobs"].ToString();
            db = new Database(connectionString);
            try
            {
                db.Init("sp_Appointments_GetDetails");
                db.AddParameter("@AppointmentId", appointmentId, System.Data.SqlDbType.VarChar);
                db.AddParameter("@CompanyID", companyId, System.Data.SqlDbType.VarChar);

                if (db.Execute() && db.Reader.Read())
                {
                    return new
                    {
                        AppointmentId = db.GetString("AppointmentId"),
                        CustomerName = db.GetString("CustomerName"),
                        CustomerEmail = db.GetString("CustomerEmail"),
                        CustomerPhone = db.GetString("CustomerPhone"),
                        ServiceType = db.GetString("ServiceType"),
                        RequestDate = db.GetString("RequestDate"),
                        TimeSlot = db.GetString("TimeSlot")
                    };
                }
            }
            finally
            {
                db.Close();
            }
            return null;
        }

        private static string GenerateFormsEmailBody(dynamic appointment, List<FSM.Entity.Forms.FormInstance> forms)
        {
            var body = $@"
                <html>
                <body>
                    <h2>Forms for Your Appointment</h2>
                    <p>Dear {appointment.CustomerName},</p>
                    <p>Please find the forms for your upcoming appointment:</p>
                    <ul>
                        <li><strong>Service:</strong> {appointment.ServiceType}</li>
                        <li><strong>Date:</strong> {appointment.RequestDate}</li>
                        <li><strong>Time:</strong> {appointment.TimeSlot}</li>
                    </ul>
                    <h3>Forms to Complete:</h3>
                    <ul>";

            foreach (var form in forms)
            {
                string templateName = form.TemplateName ?? $"Form #{form.TemplateId}";
                string templateId = HttpUtility.UrlEncode(form.TemplateId.ToString());
                string customerId = HttpUtility.UrlEncode(form.CustomerID ?? "");
                string apptID = HttpUtility.UrlEncode(appointment.AppointmentId);
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                // Build URL with encoded parameters
                string link = $"http://localhost:62934/FormResponse.aspx?templateId={templateId}&companyId={companyId}&cId={customerId}&apptId={apptID}";
                body += $"<li>{templateName} - Status: {form.Status} -> " +
                $"<a href='{link}' target='_blank'>Click here to submit response</a></li>";
            }

            body += @"
                    </ul>
                    <p>Please complete these forms before your appointment.</p>
                    <p>Thank you!</p>
                </body>
                </html>";

            return body;
        }

        private static string GenerateFormsSMSMessage(dynamic appointment, string appointmentId)
        {
            return $"Forms available for your appointment on {appointment.RequestDate} at {appointment.TimeSlot}. " +
                   $"Service: {appointment.ServiceType}. Please check your email or contact us for details. Ref: {appointmentId}";
        }

        private static bool SendEmail(string toEmail, string subject, string body, string appointmentId)
        {
            try
            {
                // Implement your email sending logic here
                // This is a placeholder - you'll need to integrate with your email service
                // Examples: SendGrid, SMTP, etc.

                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();

                // Configure SMTP settings from web.config
                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["SMTP"];
                string smtpPort = System.Configuration.ConfigurationManager.AppSettings["Port"];
                string smtpAuthUser = System.Configuration.ConfigurationManager.AppSettings["SmtpAuthUser"];
                if (string.IsNullOrEmpty(smtpAuthUser)) smtpAuthUser = System.Configuration.ConfigurationManager.AppSettings["SmtpUser"];
                string smtpPass = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];

                if (!string.IsNullOrEmpty(smtpServer))
                {
                    smtp.Host = smtpServer;
                    smtp.Port = int.Parse(smtpPort ?? "587");
                    smtp.EnableSsl = true;
                    smtp.Credentials = new System.Net.NetworkCredential(smtpAuthUser, smtpPass);

                    string fromEmail = System.Configuration.ConfigurationManager.AppSettings["SmtpUser"];
                    if (string.IsNullOrEmpty(fromEmail)) fromEmail = smtpAuthUser;
                    mail.From = new System.Net.Mail.MailAddress(fromEmail);
                    mail.To.Add(toEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    smtp.Send(mail);
                    return true;
                }

                // If no SMTP configured, log the attempt
                System.Diagnostics.Debug.WriteLine($"Email would be sent to: {toEmail}, Subject: {subject}");
                return true; // Return true for demo purposes
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Email send error: {ex.Message}");
                return false;
            }
        }

        private static bool SendSMS(string phoneNumber, string message, string appointmentId)
        {
            try
            {
                // Implement your SMS sending logic here
                // This is a placeholder - you'll need to integrate with your SMS service
                // Examples: Twilio, AWS SNS, etc.

                // Log the SMS attempt
                System.Diagnostics.Debug.WriteLine($"SMS would be sent to: {phoneNumber}, Message: {message}");
                return true; // Return true for demo purposes
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS send error: {ex.Message}");
                return false;
            }
        }
        [WebMethod]
        public static FormTemplate GetFormStructure(int templateId)
        {
            try
            {
                var forObj = new FormTemplate();
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return forObj;

                var processor = new FormProcessor();
                var template = processor.GetTemplate(templateId, companyId);
                if (template != null)
                {
                    forObj = template;
                }
                return forObj;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form structure: " + ex.Message);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateAppointmentWithCustomFields(Appointment appointment, List<CustomFieldData> customFieldValues)
        {

            if (customFieldValues != null)
            {
                SaveCustomFieldData(Convert.ToInt32(appointment.AppoinmentId), customFieldValues);
            }

            return UpdateAppointment(appointment);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SendAppointmentToFA(string appointmentId)
        {
            if (HttpContext.Current.Session["CompanyID"] == null)
            {
                System.Diagnostics.Debug.WriteLine("SendToFA Error: Session expired or CompanyID is null.");
                return false;
            }
            string companyId = HttpContext.Current.Session["CompanyID"].ToString();

            if (string.IsNullOrEmpty(appointmentId) || !int.TryParse(appointmentId, out int apptIdInt))
            {
                System.Diagnostics.Debug.WriteLine($"SendToFA Error: Invalid appointmentId format: {appointmentId}");
                return false;
            }

            Database db = new Database();
            try
            {
                db.Open();
                string statusQuery = "SELECT StatusID FROM tbl_Status WHERE StatusName = 'FA-ID Sent' AND CompanyID = @CompanyID";
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);
                object statusIdObj = db.ExecuteScalar(statusQuery);
                db.Close();

                if (statusIdObj == null || statusIdObj == DBNull.Value)
                {
                    System.Diagnostics.Debug.WriteLine($"FATAL: The status 'FA-ID Sent' does not exist in tbl_Status for CompanyID {companyId}.");
                    return false;
                }

                int statusId = Convert.ToInt32(statusIdObj);

                db.Open();
                string updateSql = "UPDATE tbl_Appointment SET Status = @StatusID WHERE ApptID = @ApptID AND CompanyID = @CompanyID";
                db.Command.Parameters.Clear();
                db.AddParameter("@StatusID", statusId, SqlDbType.Int);
                db.AddParameter("@ApptID", apptIdInt, SqlDbType.Int);
                db.AddParameter("@CompanyID", companyId, SqlDbType.VarChar);

                bool success = db.UpdateSql(updateSql);
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendAppointmentToFA WebMethod: {ex.Message}");
                return false;
            }
            finally
            {
                if (db != null && db.Connection.State == ConnectionState.Open)
                {
                    db.Close();
                }
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomFieldData(int appointmentId, List<CustomFieldData> customFieldValues)
        {
            if (HttpContext.Current.Session["CompanyID"] == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveCustomFieldData Error: Session expired or CompanyID is null.");
                return false;
            }

            string sessionCompanyId = HttpContext.Current.Session["CompanyID"].ToString();
            string connStrJobs = ConfigurationManager.AppSettings["ConnStrJobs"];

            using (var con = new SqlConnection(connStrJobs))
            {
                con.Open();

                // Verify that the appointment belongs to the logged-in company
                string verifySql = "SELECT COUNT(1) FROM [msSchedulerV3].[dbo].[tbl_Appointment] WHERE ApptID = @ApptID AND CompanyID = @CompanyID";
                using (var verifyCmd = new SqlCommand(verifySql, con))
                {
                    verifyCmd.Parameters.AddWithValue("@ApptID", appointmentId);
                    verifyCmd.Parameters.AddWithValue("@CompanyID", sessionCompanyId);
                    int count = (int)verifyCmd.ExecuteScalar();
                    if (count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveCustomFieldData Security Error: Unauthorized access attempt for ApptID {appointmentId} by CompanyID {sessionCompanyId}");
                        return false;
                    }
                }

                SqlTransaction transaction = con.BeginTransaction();
                try
                {
                    bool result = SaveCustomFieldDataInternal(appointmentId, customFieldValues, con, transaction);
                    if (result)
                    {
                        transaction.Commit();
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine("Error in SaveCustomFieldData: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Internal version of SaveCustomFieldData that participates in an existing transaction.
        /// </summary>
        private static bool SaveCustomFieldDataInternal(int appointmentId, List<CustomFieldData> customFieldValues, SqlConnection con, SqlTransaction transaction)
        {
            try
            {
                string deleteSql = "DELETE FROM [msSchedulerV3].[dbo].[AppointmentCustomFields] WHERE AppointmentID = @AppointmentID";
                using (var deleteCmd = new SqlCommand(deleteSql, con, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@AppointmentID", appointmentId);
                    deleteCmd.ExecuteNonQuery();
                }

                if (customFieldValues != null)
                {
                    foreach (var fieldData in customFieldValues)
                    {
                        if (string.IsNullOrEmpty(fieldData.Value) || fieldData.Value == "[]") continue;

                        string insertSql = @"
                            INSERT INTO [msSchedulerV3].[dbo].[AppointmentCustomFields] (AppointmentID, FieldID, FieldValue, LastUpdated)
                            VALUES (@AppointmentID, @FieldID, @FieldValue, GETDATE())";

                        using (var cmd = new SqlCommand(insertSql, con, transaction))
                        {
                            cmd.Parameters.AddWithValue("@AppointmentID", appointmentId);
                            cmd.Parameters.AddWithValue("@FieldID", fieldData.FieldId);
                            cmd.Parameters.AddWithValue("@FieldValue", fieldData.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in SaveCustomFieldDataInternal: " + ex.Message);
                throw; // Rethrow to let the caller handle the transaction
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerSite> GetSitesForCustomer(string customerId)
        {
            var sites = new List<CustomerSite>();
            if (string.IsNullOrEmpty(customerId))
            {
                return sites;
            }

            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                db.Open();
                string strSQL = @"SELECT Id, SiteName, Address, City, State, Zip, Country 
                          FROM [msSchedulerV3].[dbo].[tbl_CustomerSite] 
                          WHERE CompanyID = @CompanyID AND CustomerID = @CustomerID 
                          ORDER BY SiteName";

                db.Command.Parameters.Clear();
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(strSQL, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sites.Add(new CustomerSite
                        {
                            Id = Convert.ToInt32(dr["Id"]),
                            SiteName = dr["SiteName"].ToString() ?? "",
                            Address = dr["Address"].ToString() ?? "",
                            City = dr["City"].ToString() ?? "",
                            // *** FIX: Reading the new data and translating the State to its full name for the UI ***
                            State = LocationHelper.GetFullName(dr["State"].ToString() ?? ""),
                            Zip = dr["Zip"].ToString() ?? "",
                            Country = dr["Country"].ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetSitesForCustomer: " + ex.Message);
                if (db != null) db.Close();
            }
            return sites;
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
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool BatchUpdateAppointmentStatus(BatchUpdatePayload payload)
        {
            if (payload == null || payload.appointmentIds == null || payload.appointmentIds.Count == 0 || payload.newStatusId <= 0 || string.IsNullOrEmpty(payload.companyId))
            {
                System.Diagnostics.Debug.WriteLine("BatchUpdate Error: Invalid payload received on server.");
                return false;
            }
            string connectionString = ConfigurationManager.AppSettings["ConnString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                System.Diagnostics.Debug.WriteLine("FATAL ERROR: Connection string 'ConnString' is missing from web.config.");
                return false;
            }

            var idParameters = new List<string>();
            for (int i = 0; i < payload.appointmentIds.Count; i++)
            {
                idParameters.Add($"@ApptID{i}");
            }

            // Resolve Status ID to Name for consistent logging
            string newStatusName = "";
            Database dbStatus = new Database();
            try {
                dbStatus.Open();
                DataTable dtStatus = new DataTable();
                string statusNameSql = "SELECT StatusName FROM [msSchedulerV3].[dbo].[tbl_Status] WHERE StatusID = @StatusID AND CompanyID = @CompanyID";
                dbStatus.AddParameter("@StatusID", payload.newStatusId, SqlDbType.Int);
                dbStatus.AddParameter("@CompanyID", payload.companyId, SqlDbType.NVarChar);
                dbStatus.ExecuteParam(statusNameSql, out dtStatus);
                if (dtStatus.Rows.Count > 0) {
                    newStatusName = dtStatus.Rows[0]["StatusName"].ToString();
                    if (newStatusName == "Scheduled") newStatusName = "Confirmed";
                }
            } finally {
                dbStatus.Close();
            }

            if (string.IsNullOrEmpty(newStatusName)) {
                System.Diagnostics.Debug.WriteLine($"BatchUpdate Error: Could not resolve StatusID {payload.newStatusId} to a name.");
                return false;
            }

            // Backend validation for batch updates
            if (IsResourceRequired(payload.newStatusId))
            {
                // We need to check if any of these appointments are currently unassigned
                // For safety, we block the entire batch if any lack a resource
                string checkSql = $"SELECT COUNT(*) FROM [msSchedulerV3].[dbo].[tbl_Appointment] WHERE [CompanyID] = @CompanyID AND ([ResourceID] IS NULL OR [ResourceID] = 0) AND [ApptID] IN ({string.Join(", ", idParameters)})";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(checkSql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompanyID", payload.companyId);
                        for (int i = 0; i < payload.appointmentIds.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@ApptID{i}", payload.appointmentIds[i]);
                        }
                        con.Open();
                        int unassignedCount = (int)cmd.ExecuteScalar();
                        if (unassignedCount > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"BatchUpdate blocked: {unassignedCount} appointments lack a resource for status {payload.newStatusId}.");
                            return false;
                        }
                    }
                }
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_BatchUpdateAppointmentStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        cmd.Parameters.AddWithValue("@AppointmentIds", string.Join(",", payload.appointmentIds));
                        cmd.Parameters.AddWithValue("@NewStatus", newStatusName);
                        cmd.Parameters.AddWithValue("@CompanyID", payload.companyId);
                        
                        string currentUser = HttpContext.Current.Session["LoginUser"] as string ?? "System";
                        cmd.Parameters.AddWithValue("@ChangedBy", currentUser);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        ConsoleLog($"[BatchUpdateAppointmentStatus] Updated {payload.appointmentIds.Count} appointments to StatusID={payload.newStatusId} ({newStatusName})");

                        // Process status communication for each appointment using new processor
                        var processor = new AppointmentStatusCommunicationProcessor(payload.companyId);
                        foreach (int apptId in payload.appointmentIds)
                        {
                            try
                            {
                                // Get old status for each appointment
                                string oldStatus = "";
                                int oldStatusId = 0;
                                using (var checkConn = new SqlConnection(connectionString))
                                {
                                    // Note: We can't get old status after update, so we'll use the stored procedure's history
                                    // For now, we'll process with newStatusId and let processor handle it
                                    ConsoleLog($"[BatchUpdateAppointmentStatus] Processing communication for ApptID={apptId}, NewStatus={newStatusName}({payload.newStatusId})");
                                    processor.ProcessStatusChange(apptId, 0, payload.newStatusId, "", newStatusName, null);
                                }
                            }
                            catch (Exception commEx)
                            {
                                ConsoleLog($"[BatchUpdateAppointmentStatus] ERROR processing communication for ApptID={apptId}: {commEx.Message}\n{commEx.StackTrace}");
                            }
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("FATAL SQL EXCEPTION in BatchUpdateAppointmentStatus: " + ex.ToString());
                        return false;
                    }
                }
            }
        }

        private static string MapStatusNameToSmsCode(string statusName)
        {
            if (string.IsNullOrEmpty(statusName)) return "";
            switch (statusName.ToLowerInvariant())
            {
                case "pending": return "1";
                case "confirmed":
                case "scheduled": return "2";
                case "cancelled": return "3";
                case "closed": return "4";
                case "installation in progress":
                case "progress": return "5";
                case "completed": return "6";
                default: return "";
            }
        }

        private static dynamic GetAppointmentDetailsForSms(int appointmentId, string companyId, string connectionString)
        {
            string sql = @"SELECT apt.CustomerID, apt.ResourceID, apt.ApptDateTime, apt.TimeSlot
                FROM [msSchedulerV3].[dbo].[tbl_Appointment] apt
                WHERE apt.ApptID = @ApptID AND apt.CompanyID = @CompanyID";
            using (var con = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@ApptID", appointmentId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        string requestDate = dr["ApptDateTime"] != DBNull.Value
                            ? Convert.ToDateTime(dr["ApptDateTime"]).ToString("MM/dd/yyyy") : "";
                        return new
                        {
                            CustomerID = dr["CustomerID"].ToString(),
                            ResourceID = dr["ResourceID"] != DBNull.Value ? Convert.ToInt32(dr["ResourceID"]) : 0,
                            RequestDate = requestDate,
                            TimeSlot = dr["TimeSlot"]?.ToString() ?? ""
                        };
                    }
                }
            }
            return null;
        }



        public class CustomFieldData
        {
            public int FieldId { get; set; }
            public string Value { get; set; }
        }

        [WebMethod]
        public static string GetCustomerResponseOnForms(int templateId, int appointmentId, int customerId)
        {
            try
            {
                string formStructure = "";
                string companyId = System.Web.HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                    return formStructure;

                var processor = new FormProcessor();
                var template = processor.GetFormStructureFromResponse(templateId, companyId, appointmentId, customerId);
                if (template != null)
                {
                    formStructure = template;
                }
                return formStructure;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving form structure: " + ex.Message);
            }
        }

        public class CslDrawerData
        {
            public CustomerEntity CustomerInfo { get; set; }
            public CustomerSite SiteInfo { get; set; }
            public List<AppointmentModel> Appointments { get; set; }
            public List<CustomerInvoice> Invoices { get; set; }
            public List<Equipment> Equipment { get; set; }
            public List<CustomerDetails.NoteViewModel> Notes { get; set; }
            public List<object> Pictures { get; set; }
            public List<object> Files { get; set; }
            public List<object> MaintenanceAgreements { get; set; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static CslDrawerData GetCslDrawerData(string customerId, int siteId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return null;
            }

            var data = new CslDrawerData();
            try
            {
                data.CustomerInfo = CustomerDetails.GetCustomerDetails(customerId);

                if (data.CustomerInfo == null)
                {
                    // If customer info is not found, we can't proceed.
                    // Log this as an error.
                    System.Diagnostics.Debug.WriteLine($"Error in GetCslDrawerData: Customer with ID {customerId} not found.");
                    return null;
                }

                if (siteId == 0)
                {
                    data.SiteInfo = new CustomerSite
                    {
                        Id = 0,
                        SiteName = "Customer Location (Default)",
                        Address = string.Join(", ", new[] { data.CustomerInfo.Address1, data.CustomerInfo.City, data.CustomerInfo.State, data.CustomerInfo.ZipCode }.Where(s => !string.IsNullOrEmpty(s))),
                        FirstName = data.CustomerInfo.FirstName,
                        LastName = data.CustomerInfo.LastName,
                        PhoneNumber = data.CustomerInfo.Phone,
                        Email = data.CustomerInfo.Email,
                        IsActive = true
                    };
                }
                else
                {
                    data.SiteInfo = CustomerDetails.GetCustomerSitebyId(customerId, siteId);
                }

                data.Appointments = CustomerDetails.GetCustomerAppoinmets(customerId, siteId);
                data.Invoices = CustomerDetails.GetCustomerInvoices(customerId);
                data.Notes = CustomerDetails.GetCustomerNotes(customerId, siteId);

                if (!string.IsNullOrEmpty(data.CustomerInfo.CustomerGuid))
                {
                    data.Equipment = CustomerDetails.GetSiteEquipmentData(siteId, data.CustomerInfo.CustomerGuid);
                }

                // Fetch Pictures, Files, and Maintenance Agreements
                try
                {
                    var pictures = CustomerDetails.GetSitePictures(customerId, siteId);
                    data.Pictures = pictures.Cast<object>().ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading pictures in GetCslDrawerData: {ex.Message}");
                    data.Pictures = new List<object>();
                }

                try
                {
                    var files = CustomerDetails.GetSiteFiles(customerId, siteId);
                    data.Files = files.Cast<object>().ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading files in GetCslDrawerData: {ex.Message}");
                    data.Files = new List<object>();
                }

                try
                {
                    var agreements = CustomerDetails.GetMaintenanceAgreements(customerId, siteId);
                    data.MaintenanceAgreements = agreements.Cast<object>().ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading maintenance agreements in GetCslDrawerData: {ex.Message}");
                    data.MaintenanceAgreements = new List<object>();
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetCslDrawerData: {ex.Message}");
                return null;
            }

            return data;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCslNote(string customerId, int siteId, string appointmentId, string description, string taggedTo, string taggedFrom)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = HttpContext.Current.Session["UserID"]?.ToString() ?? HttpContext.Current.User?.Identity?.Name ?? "System";

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(description))
                {
                    return false;
                }

                Database db = new Database();
                try
                {
                    db.Open();

                    string sql = @"
                        INSERT INTO [msSchedulerV3].[dbo].[tbl_Note]
                        (CustomerId, SiteId, AppointmentId, CompanyId, Description, TaggedFrom, TaggedTo, CreatedBy, CreatedAt)
                        VALUES (@CustomerId, @SiteId, @AppointmentId, @CompanyId, @Description, @TaggedFrom, @TaggedTo, @CreatedBy, GETDATE())";

                    db.AddParameter("@CustomerId", customerId, SqlDbType.NVarChar);
                    db.AddParameter("@SiteId", siteId, SqlDbType.Int);
                    db.AddParameter("@AppointmentId", string.IsNullOrEmpty(appointmentId) ? (object)DBNull.Value : appointmentId, SqlDbType.NVarChar);
                    db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                    db.AddParameter("@Description", description, SqlDbType.NVarChar);
                    db.AddParameter("@TaggedFrom", taggedFrom ?? "FSM", SqlDbType.NVarChar);
                    db.AddParameter("@TaggedTo", string.IsNullOrEmpty(taggedTo) ? (object)DBNull.Value : taggedTo, SqlDbType.NVarChar);
                    db.AddParameter("@CreatedBy", userId, SqlDbType.NVarChar);

                    int rowsAffected = db.ExecuteNonQuery(sql);
                    db.Close();

                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in SaveCslNote: {ex.Message}");
                    db.Close();
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveCslNote: {ex.Message}");
                return false;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerInvoice> GetAppointmentInvoices(string appointmentId)
        {
            var invoices = new List<CustomerInvoice>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(appointmentId))
            {
                return invoices;
            }

            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();

                string sql = @"
            SELECT 
                inv.ID, 
                inv.Number,
                inv.Subtotal,
                ISNULL(inv.AmountCollect, 0.00) as AmountCollect,
                ISNULL(inv.DepositAmount, 0.00) as DepositAmount,
                inv.Discount, 
                inv.Tax, 
                (inv.Total - ISNULL(inv.AmountCollect, 0.00)) as Due, 
                inv.Type, 
                CONVERT(VARCHAR(10), COALESCE(inv.InvoiceDate, inv.CreatedDate, inv.ExpirationDate), 101) as InvoiceDate,
                inv.Total, 
                inv.AppointmentId,
                cust.CustomerGuid
            FROM tbl_Invoice as inv
            LEFT JOIN tbl_Customer as cust 
              ON inv.CustomerID = cust.CustomerID AND inv.CompnyID = cust.CompanyID
            WHERE inv.AppointmentId = @AppointmentId AND inv.CompnyID = @CompanyID;";

                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);

                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var invoice = new CustomerInvoice();
                        invoice.ID = row.Field<string>("ID") ?? "";
                        invoice.InvoiceNumber = row.Field<string>("Number") ?? "";
                        invoice.InvoiceType = row.Field<string>("Type") ?? "";
                        invoice.AppointmentId = row.Field<string>("AppointmentId") ?? "";
                        invoice.CustomerGuid = row.Field<string>("CustomerGuid") ?? "";
                        invoice.Total = row["Total"].ToString() ?? "0.0";
                        invoice.Subtotal = row["Subtotal"].ToString() ?? "0.0";
                        invoice.Due = row["Due"].ToString() ?? "0.0";
                        invoice.Discount = row["Discount"].ToString() ?? "0.0";
                        invoice.Tax = row["Tax"].ToString() ?? "0.0";
                        invoice.DepositAmount = row["DepositAmount"].ToString() ?? "0.0";
                        invoice.InvoiceDate = row.Field<string>("InvoiceDate") ?? "";

                        if ((Convert.ToDouble(row["Total"].ToString()) - Convert.ToDouble(row["AmountCollect"].ToString())) <= 0)
                            invoice.InvoiceStatus = "Paid";
                        else
                            invoice.InvoiceStatus = "Unpaid";

                        if (!string.IsNullOrEmpty(invoice.ID) && !string.IsNullOrEmpty(invoice.CustomerGuid))
                        {
                            string inTypeForUrl = (invoice.InvoiceType == "Proposal") ? "Estimate" : invoice.InvoiceType;
                            invoice.ExternalLink =
                                $"https://testsite.myserviceforce.com/cec/Invoice.aspx?InvNum={invoice.ID}&cId={invoice.CustomerGuid}&InType={inTypeForUrl}&AppID={invoice.AppointmentId}&FromInvoices=1";
                        }
                        else
                        {
                            invoice.ExternalLink = "";
                        }

                        invoices.Add(invoice);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAppointmentInvoices: {ex.Message}");
            }
            finally { db.Close(); }
            return invoices;
        }

    }
}