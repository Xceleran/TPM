using FSM.Entity;
using FSM.Entity.Appoinments;
using FSM.Entity.Customer;
using FSM.Entity.Enums;
using FSM.Entity.Notes;
using FSM.Models.AppoinmentModel;
using FSM.Processors;
using FSM.SMSService;
using FSM.Helper;
using Intuit.Ipp.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using static FSM.Customer;

namespace TPM
{
    [System.Web.Script.Services.ScriptService]
    public partial class CustomerDetails : System.Web.UI.Page
    {
        protected global::System.Web.UI.WebControls.Label lblStreetAddress;
        protected global::System.Web.UI.WebControls.Label lblCity;
        protected global::System.Web.UI.WebControls.Label lblState;
        protected global::System.Web.UI.WebControls.Label lblZip;
        protected global::System.Web.UI.WebControls.Label lblCountry;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Handle file downloads FIRST, before any other processing
            // This must happen before any output is sent to the response
            string downloadType = Request.QueryString["type"];
            string idParam = Request.QueryString["id"];
            string isDownload = Request.QueryString["download"];
            if (!string.IsNullOrEmpty(downloadType) && !string.IsNullOrEmpty(idParam))
            {
                if (int.TryParse(idParam, out int fileId))
                {
                    // Only handle downloads on GET requests (not postbacks)
                    if (!IsPostBack)
                    {
                        DownloadFile(downloadType, fileId, isDownload == "1");
                        return; // Exit early, don't process page load
                    }
                }
            }
            if (Session["CompanyID"] == null) { Response.Redirect("Dashboard.aspx"); }

            if (IsPostBack) { return; }

            string customerId = Request.QueryString["custId"];
            string siteIdStr = Request.QueryString["siteId"];
            string appointmentId = Request.QueryString["appointmentId"];
            int.TryParse(siteIdStr, out int siteId);

            if (string.IsNullOrEmpty(customerId)) { return; }

            SetEmailHistoryRedirectUrl(customerId);

            var customer = GetCustomerDetails(customerId);
            if (customer == null) { return; }

            lblCustomerGuid.Text = customer.CustomerGuid;
            lblCustomerId.Text = customerId;
            lblSiteId.Text = siteIdStr;
            lblAppointmentId.Text = appointmentId ?? "";

            // Format customer name: "Last Name, First Name" or Business Name
            string customerNameDisplay = "";
            if (!string.IsNullOrEmpty(customer.BusinessName))
            {
                customerNameDisplay = customer.BusinessName;
            }
            else if (!string.IsNullOrEmpty(customer.LastName) || !string.IsNullOrEmpty(customer.FirstName))
            {
                customerNameDisplay = $"{customer.FirstName} {customer.LastName}".Trim();
            }
            else
            {
                customerNameDisplay = customer.FirstName + " " + customer.LastName;
            }

            if (siteId == 0)
            {
                lblSiteName.Text = customerNameDisplay + " <span style='font-size: smaller;'>(Customer Location - Default)</span>";
                lblSiteNameTable.Text = customerNameDisplay + " <span style='font-size: smaller;'>(Customer Location - Default)</span>";
                lblCustomerName.Text = customerNameDisplay;
                lblCustomerLocation.Text = "Customer Location (Default)";
                lblContact.Text = customer.FirstName + " " + customer.LastName;
                hlPhone.Text = Common.GetFormatedPhoneNumber(customer.Phone); 
                hlPhone.NavigateUrl = "tel:" + customer.Phone;
                hlMobile.Text = Common.GetFormatedPhoneNumber(customer.Mobile); 
                hlMobile.NavigateUrl = "tel:" + customer.Mobile;
                hlEmail.Text = customer.Email;
                hlEmail.NavigateUrl = "mailto:" + customer.Email;
                lblAddress.Text = string.Join(", ", new[] { customer.Address1, customer.City, LocationHelper.GetFullName(customer.State), customer.ZipCode }.Where(s => !string.IsNullOrEmpty(s)));

                lblStreetAddress.Text = customer.Address1 ?? "";
                lblCity.Text = customer.City ?? "";
                lblState.Text = LocationHelper.GetFullName(customer.State) ?? "";
                lblZip.Text = customer.ZipCode ?? "";
                lblCountry.Text = "USA"; // Default for customer

                lblActive.Text = "Active";
                lblNote.Text = "This is the primary customer location.";
                lblCreatedOn.Text = customer.CreatedDateTime.ToString("MM-dd-yyyy");
            }
            else
            {
                var site = GetCustomerSitebyId(customerId, siteId);
                if (site != null && site.Id > 0)
                {
                    lblSiteName.Text = site.SiteName;
                    lblSiteNameTable.Text = site.SiteName;
                    lblCustomerName.Text = customerNameDisplay;
                    lblCustomerLocation.Text = site.SiteName;
                    lblContact.Text = site.FirstName + " " + site.LastName;
                    hlPhone.Text = Common.GetFormatedPhoneNumber(site.PhoneNumber);
                    hlPhone.NavigateUrl = "tel:" + site.PhoneNumber;
                    hlMobile.Text = Common.GetFormatedPhoneNumber(site.MobileNumber); 
                    hlMobile.NavigateUrl = "tel:" + site.MobileNumber;
                    hlEmail.Text = site.Email;
                    hlEmail.NavigateUrl = "mailto:" + site.Email;
                    lblAddress.Text = site.Address;
                    lbl_City.Text = site.City;
                    lbl_State.Text =  LocationHelper.GetFullName(site.State);
                    lbl_Zip.Text = site.Zip;
                    lblStreetAddress.Text = site.Address ?? "";
                    lblCity.Text = site.City ?? "";
                    lblState.Text = site.State ?? "";
                    lblZip.Text = site.Zip ?? "";
                    lblCountry.Text = site.Country ?? "";

                    lblActive.Text = site.IsActive ? "Active" : "Disabled";
                    lblNote.Text = site.Note;
                    lblCreatedOn.Text = site.CreatedDateTime?.ToString("MM-dd-yyyy");
                }
            }

            LoadData();
        }

        private void SetEmailHistoryRedirectUrl(string customerId)
        {
            try
            {
                string userId = HttpContext.Current.Session["LoginUser"] as string;
                string companyId = HttpContext.Current.Session["CompanyID"] as string;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                {
                    btnEmailHistory.Visible = false;
                    return;
                }

                string sessionString = $"{userId}|{companyId}";
                string newGuid = Guid.NewGuid().ToString();

                string sql = $"INSERT INTO XinatorCentral.dbo.tbl_Login (SessionGuid, SessionString) VALUES ('{newGuid}', '{sessionString}')";

                Database db = new Database();
                db.UpdateSql(sql);

                string cecBaseUrl = System.Configuration.ConfigurationManager.AppSettings["cecBaseUrl"];
               

                string redirectUrl = HttpUtility.UrlEncode($"EmailHistory_List.aspx?Id={customerId}");

                btnEmailHistory.HRef = $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating CEC SSO Email History URL: " + ex.Message);
                btnEmailHistory.Visible = false;
            }
        }




        public void LoadData()
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                string Sql = @"SELECT  [StatusID],[StatusName] FROM [msSchedulerV3].[dbo].[tbl_TicketStatus] where CompanyID= '" + companyid + "';";
                Sql += @"SELECT  [StatusID],[StatusName] FROM [msSchedulerV3].[dbo].[tbl_Status] where CompanyID='" + companyid + "';";

                DataTable _ticketStatus = new DataTable();
                DataTable _appStatus = new DataTable();
                DataSet dataSet = db.Get_DataSet(Sql, companyid);

                _ticketStatus = dataSet.Tables[0];
                _appStatus = dataSet.Tables[1];

                // Change "Scheduled" to "Confirmed" for display purposes
                foreach (DataRow row in _appStatus.Rows)
                {
                    if (row["StatusName"].ToString() == "Scheduled")
                    {
                        row["StatusName"] = "Confirmed";
                    }
                }
                if (_ticketStatus.Rows.Count > 0)
                {
                    ticketStatus.DataSource = _ticketStatus;
                    ticketStatus.DataBind();
                    ticketStatus.DataTextField = "StatusName";
                    ticketStatus.DataValueField = "StatusName";
                    ticketStatus.DataBind();
                    ticketStatus.Items.Insert(0, new ListItem("All Ticket Status", ""));
                }
                if (_appStatus.Rows.Count > 0)
                {
                    apptFilter.DataSource = _appStatus;
                    apptFilter.DataBind();
                    apptFilter.DataTextField = "StatusName";
                    apptFilter.DataValueField = "StatusName";
                    apptFilter.DataBind();
                    apptFilter.Items.Insert(0, new ListItem("All Status", ""));
                }
            }

            catch (Exception ex) { }
        }
        public static CustomerEntity GetCustomerDetails(string customerId)
        {
            var customer = new CustomerEntity();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_Customer] WHERE CustomerID = @CustomerID AND CompanyID = @CompanyID;";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    DataRow dataRow = dt.Rows[0];
                    //customer.BusinessID = dataRow.Field<int?>("BusinessID") ?? 0;
                    //customer.Address1 = dataRow.Field<string>("Address1") ?? "";
                    //customer.Address2 = dataRow.Field<string>("Address2") ?? "";
                    //customer.CompanyName = dataRow.Field<string>("CompanyName") ?? "";
                    customer.CustomerGuid = dataRow.Field<string>("CustomerGuid") ?? "";
                    customer.Address1 = dataRow.Field<string>("Address1") ?? "";
                    customer.City = dataRow.Field<string>("City") ?? "";
                    customer.State = dataRow.Field<string>("State") ?? "";
                    customer.ZipCode = dataRow.Field<string>("ZipCode") ?? "";
                    customer.FirstName = dataRow.Field<string>("FirstName") ?? "";
                    customer.LastName = dataRow.Field<string>("LastName") ?? "";
                    customer.BusinessName = dataRow.Field<string>("BusinessName") ?? "";
                    customer.Phone = dataRow.Field<string>("Phone") ?? "";
                    customer.Mobile = dataRow.Field<string>("Mobile") ?? "";
                    customer.Email = dataRow.Field<string>("Email") ?? "";
                    customer.CreatedDateTime = Convert.ToDateTime(dataRow["CreatedDateTime"]);

                }
            }
            catch (Exception ex)
            {
                db.Close();
                return customer;
            }
            return customer;
        }



        public static CustomerSite GetCustomerSitebyId(string customerId, int siteId)
        {
            var site = new CustomerSite();
            Database db = new Database();
            DataTable dt = new DataTable();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            try
            {
                db.Open();
                string strSQL = @"SELECT * FROM [msSchedulerV3].dbo.tbl_CustomerSite WHERE companyid=@Companyid and  Id = @SiteID AND CustomerID = @CustomerID;";
                db.AddParameter("@SiteID", siteId, SqlDbType.Int);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@Companyid", companyid, SqlDbType.NVarChar);
                db.ExecuteParam(strSQL, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow dataRow = dt.Rows[0];
                    site.Id = dataRow.Field<int?>("Id") ?? 0;
                    site.SiteName = dataRow.Field<string>("SiteName") ?? "";
                    site.Address = dataRow.Field<string>("Address") ?? "";
                    site.Country = dataRow.Field<string>("Country") ?? "";
                    site.State = LocationHelper.GetFullName(dataRow.Field<string>("State") ?? "");
                    site.Zip = dataRow.Field<string>("Zip") ?? "";
                    site.Note = dataRow.Field<string>("Note") ?? "";
                    site.IsActive = dataRow.Field<bool?>("IsActive") ?? false;
                    site.CreatedDateTime = dataRow["CreatedDateTime"] as DateTime?;
                    site.FirstName = dataRow.Field<string>("FirstName") ?? "";
                    site.LastName = dataRow.Field<string>("LastName") ?? "";
                    site.PhoneNumber = dataRow.Field<string>("PhoneNumber") ?? "";
                    site.Email = dataRow.Field<string>("Email") ?? "";
                    site.City = dataRow.Field<string>("City") ?? "";
                    site.MobileNumber = dataRow.Field<string>("MobileNumber") ?? "";

                    // SYNC-ON-LOAD: If this is a "Default" site, ensure it's in sync with the parent customer
                    if (site.SiteName == "Customer Location (Default)")
                    {
                        var customer = GetCustomerDetails(customerId);
                        if (customer != null)
                        {
                            site.FirstName = customer.FirstName;
                            site.LastName = customer.LastName;
                            site.Address = customer.Address1;
                            site.City = customer.City;
                            site.State = LocationHelper.GetFullName(customer.State);
                            site.Zip = customer.ZipCode;
                            site.PhoneNumber = customer.Phone;
                            site.Email = customer.Email;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerSitebyId: {ex.Message}");
                return null;
            }
            finally
            {
                db.Close();
            }
            return site;
        }


        public static CustomerSummeryCount GetCustomerSummery(string customerId)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            var customerData = new CustomerSummeryCount();
            customerData.CompanyID = companyid;
            customerData.CustomerID = customerId;
            try
            {
                db.Open();
                DataTable dt1 = new DataTable();
                string invoiceQuery = @"select Total, AmountCollect FROM [msSchedulerV3].[dbo].[tbl_Invoice] WHERE Type = 'Invoice' AND CustomerID = @CustomerID and CompnyID=@CompanyID";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(invoiceQuery, out dt1);

                if (dt1.Rows.Count > 0)
                {
                    foreach (DataRow row in dt1.Rows)
                    {
                        if ((Convert.ToDouble(row["Total"].ToString()) - Convert.ToDouble(row["AmountCollect"].ToString())) <= 0)
                        {
                            customerData.PaidInvoices++;
                        }
                        else
                        {
                            customerData.UnpaidInvoices++;
                        }
                    }
                }

                db.Command.Parameters.Clear();
                DataTable dt2 = new DataTable();
                string estimateQuery = @"select count(*) as EstimateCount FROM [msSchedulerV3].[dbo].[tbl_Invoice] where Type='Proposal' AND CustomerID = @CustomerID AND CompnyID = @CompanyID;";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(estimateQuery, out dt2);
                if (dt2.Rows.Count > 0)
                {
                    DataRow dataRow = dt2.Rows[0];
                    customerData.Estimates = dataRow.Field<int?>("EstimateCount") ?? 0;
                }
                db.Command.Parameters.Clear();
                DataTable dt3 = new DataTable();
                string appointmentQuery = @"SELECT Status FROM tbl_Appointment WHERE CompanyID = @CompanyID AND CustomerID = @CustomerID;";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.ExecuteParam(appointmentQuery, out dt3);
                if (dt3.Rows.Count > 0)
                {
                    foreach (DataRow row in dt3.Rows)
                    {
                        string statusStr = row.Field<string>("Status");
                        if (Enum.TryParse<WorkOrderStatus>(statusStr, out var status))
                        {
                            switch (status)
                            {
                                case WorkOrderStatus.Pending:
                                    customerData.PendingAppointments++;
                                    break;
                                case WorkOrderStatus.Scheduled:
                                    customerData.ScheduledAppointments++;
                                    break;
                                case WorkOrderStatus.Closed:
                                    customerData.CompletedAppointments++;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                db.Close();
            }

            catch (Exception ex)
            {
                db.Close();
                return customerData;
            }

            return customerData;
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<AppointmentModel> GetCustomerAppoinmets(string customerId, int siteId)
        {
            var appoinments = new List<AppointmentModel>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetCustomerAppoinmets: CompanyID is missing from session");
                return appoinments;
            }
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();

                // Updated query to include ServiceTypeID for fallback logic
                // Updated query to include ServiceTypeID for fallback logic and AppointmentPrefix




                string sql = @"
                    SELECT
                        apt.ApptID,apt.AppoinmentUId, apt.Note, apt.TimeSlot, apt.Hour, apt.Minute,
                        apt.StartDateTime, apt.EndDateTime,
                        CONVERT(VARCHAR(10), apt.ApptDateTime, 120) as RequestDate,
                        COALESCE(rsc.Name, NULLIF(LTRIM(RTRIM(CAST(apt.ResourceID AS NVARCHAR))), '0'), '') as ResourceName,
                        srv.ServiceTypeID,
                        COALESCE(NULLIF(srv.ServiceName, ''), NULLIF(apt.ServiceType, ''), '') AS ServiceName,
                        srv.CalenderColor AS ServiceColor,
                        sts.CalenderColor AS StatusColor,
                        CASE
                            WHEN sts.StatusName = 'Scheduled' THEN 'Confirmed'
                            ELSE COALESCE(sts.StatusName, apt.Status, 'Unknown')
                        END AS AppoinmentStatus,
                        COALESCE(NULLIF(tkt.StatusName, ''), NULLIF(apt.TicketStatus, ''), '') AS TicketStatus,
                        gen.AppointmentPrefix
                    FROM
                        tbl_Appointment apt
                    LEFT JOIN
                        tbl_Resources as rsc ON apt.ResourceID = rsc.Id AND apt.CompanyID = rsc.CompanyID
                    LEFT JOIN
                        tbl_ServiceType AS srv ON apt.CompanyID = srv.CompanyID AND (TRY_CAST(apt.ServiceType AS INT) = srv.ServiceTypeID OR apt.ServiceType = srv.ServiceName)
                    LEFT JOIN
                        tbl_Status AS sts ON apt.CompanyID = sts.CompanyID AND
                            (CASE WHEN apt.Status = '0' OR apt.Status IS NULL OR apt.Status = '' THEN 1 ELSE TRY_CAST(apt.Status AS INT) END = sts.StatusID OR apt.Status = sts.StatusName)
                    LEFT JOIN
                        tbl_TicketStatus AS tkt ON apt.CompanyID = tkt.CompanyID AND
                            (CASE WHEN apt.TicketStatus = '0' OR apt.TicketStatus IS NULL OR apt.TicketStatus = '' THEN 1 ELSE TRY_CAST(apt.TicketStatus AS INT) END = tkt.StatusID OR apt.TicketStatus = tkt.StatusName)
                    LEFT JOIN
                        tbl_AppointmentAutoGenerate gen ON apt.CompanyID = gen.CompanyID
                    WHERE apt.CompanyID = @CompanyID AND apt.CustomerID = @CustomerID
                      AND apt.SiteID = @SiteID
                      AND apt.Status != 'Deleted'
                      AND (apt.SchedulingCal = 'FSM')
                    ORDER BY
                        apt.ApptDateTime DESC";

                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", siteId, SqlDbType.Int);

                db.ExecuteParam(sql, out dt);

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            var appoinment = new AppointmentModel();
                            // Format Appointment ID
                            //string rawApptId = row["ApptID"]?.ToString() ?? "";
                            //string prefix = row.Table.Columns.Contains("AppointmentPrefix") && !string.IsNullOrEmpty(row["AppointmentPrefix"]?.ToString()) ? row["AppointmentPrefix"].ToString() : "APPT";
                            //appoinment.AppoinmentId = !string.IsNullOrEmpty(rawApptId) ? $"{prefix}-{companyid}-{rawApptId}" : "";
                            appoinment.AppoinmentUId = row["AppoinmentUId"]?.ToString() ?? "";
                            appoinment.AppoinmentId = row["ApptID"]?.ToString() ?? "";
                            appoinment.CustomerID = customerId;
                            appoinment.CompanyID = companyid;
                            appoinment.AppoinmentStatus = row.Field<string>("AppoinmentStatus") ?? "";
                            appoinment.TicketStatus = row.Field<string>("TicketStatus") ?? "";
                            appoinment.ResourceName = row.Field<string>("ResourceName") ?? "";
                            appoinment.ServiceType = row.Field<string>("ServiceName") ?? "";
                            appoinment.RequestDate = row.Field<string>("RequestDate") ?? "";

                            if (row["StartDateTime"] != DBNull.Value)
                            {
                                DateTime startDateTime = Convert.ToDateTime(row["StartDateTime"]);
                                appoinment.StartDateTime = startDateTime.ToString("MM/dd/yyyy hh:mm tt");
                            }

                            string dbTimeSlot = row.Field<string>("TimeSlot") ?? "";
                            if (!string.IsNullOrEmpty(dbTimeSlot))
                            {
                                appoinment.TimeSlot = dbTimeSlot;
                            }
                            else if (row["StartDateTime"] != DBNull.Value)
                            {
                                DateTime startDateTime = Convert.ToDateTime(row["StartDateTime"]);
                                appoinment.TimeSlot = startDateTime.ToString("h:mm tt");
                            }
                            else
                            {
                                appoinment.TimeSlot = "";
                            }
                            if (row["EndDateTime"] != DBNull.Value) appoinment.EndDateTime = Convert.ToDateTime(row["EndDateTime"]).ToString("MM/dd/yyyy hh:mm tt");

                            appoinment.AppoinmentDate = row.Field<string>("RequestDate") ?? "";
                            appoinment.Note = row.Field<string>("Note") ?? "";
                            appoinment.StatusColor = row["StatusColor"] != DBNull.Value ? row["StatusColor"].ToString() : "#3b82f6";
                            appoinment.ServiceColor = row["ServiceColor"] != DBNull.Value ? row["ServiceColor"].ToString() : "#3b82f6";

                            int serviceTypeId = row.Field<int?>("ServiceTypeID") ?? 0;

                            // Replicating the fallback logic from Appointments.aspx.cs
                            if (row["Hour"] != DBNull.Value && row["Hour"] != null)
                            {
                                appoinment.timerequired_Hour = row.Field<int?>("Hour") ?? 0;
                                appoinment.timerequired_Minute = row.Field<int?>("Minute") ?? 0;
                            }
                            else if (serviceTypeId > 0)
                            {
                                var duration = CalculateDurationForAppointment(serviceTypeId);
                                appoinment.timerequired_Hour = duration.Item1;
                                appoinment.timerequired_Minute = duration.Item2;
                            }
                            else
                            {
                                appoinment.timerequired_Hour = 1; // Default fallback
                                appoinment.timerequired_Minute = 0;
                            }

                            appoinments.Add(appoinment);
                        }
                        catch (Exception rowEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing appointment row: {rowEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerAppoinmets: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<AppointmentModel>();
            }
            finally
            {
                if (db != null) { db.Close(); }
            }
            return appoinments;
        }

        public static Tuple<int, int> CalculateDurationForAppointment(int serviceTypeID)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();
                string sql = @"select Hour, Minute from [msSchedulerV3].[dbo].[tbl_ServiceType] where CompanyID = @CompanyID and ServiceTypeID = @ServiceTypeID;";
                db.AddParameter("@CompanyID", CompanyID, SqlDbType.NVarChar);
                db.AddParameter("@ServiceTypeID", serviceTypeID, SqlDbType.Int);
                db.ExecuteParam(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    int hr = row.Field<int?>("Hour") ?? 0;
                    int min = row.Field<int?>("Minute") ?? 0;
                    return Tuple.Create(hr, min);
                }
            }
            catch (Exception)
            {
                if (db.Connection.State == System.Data.ConnectionState.Open) db.Close();
            }
            // Default duration if not found or on error
            return Tuple.Create(1, 0);
        }

        public class StatusHistoryViewModel
        {
            public string StatusName { get; set; }
            public string StatusFromName { get; set; }
            public string Timestamp { get; set; }
            public string ChangedBy { get; set; }
            public string Note { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<StatusHistoryViewModel> GetAppointmentStatusHistory(string appointmentId)
        {
            var history = new List<StatusHistoryViewModel>();

            try
            {
                // Strip prefix if formatted ID (e.g., APPT-101-505 -> 505)
                if (!string.IsNullOrEmpty(appointmentId) && appointmentId.Contains("-"))
                {
                    string[] parts = appointmentId.Split('-');
                    appointmentId = parts[parts.Length - 1];
                }

                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"GetAppointmentStatusHistory called with: appointmentId={appointmentId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(appointmentId))
                {
                    System.Diagnostics.Debug.WriteLine($"GetAppointmentStatusHistory: Validation failed - CompanyID: {companyid}, AppointmentId: {appointmentId}");
                    return history;
                }

                Database db = new Database();
                DataTable dt = new DataTable();

                // UPDATED SQL QUERY to use tbl_AppointmentStatusHistory
                string sql = @"
                    SELECT 
                        h.StatusChangeDateTime AS ChangedAt,
                        h.ChangedBy,
                        h.Notes AS Note,
                        COALESCE(s_to.StatusName, h.NewStatus) AS StatusToName,
                        COALESCE(s_from.StatusName, h.PreviousStatus) AS StatusFromName
                    FROM 
                        [msSchedulerV3].[dbo].[tbl_AppointmentStatusHistory] h
                    LEFT JOIN 
                        [msSchedulerV3].[dbo].[tbl_Status] s_to ON h.CompanyID = s_to.CompanyID AND (CASE WHEN h.NewStatus = '0' OR h.NewStatus IS NULL OR h.NewStatus = '' THEN 1 ELSE TRY_CAST(h.NewStatus AS INT) END = s_to.StatusID OR h.NewStatus = s_to.StatusName)
                    LEFT JOIN 
                        [msSchedulerV3].[dbo].[tbl_Status] s_from ON h.CompanyID = s_from.CompanyID AND (CASE WHEN h.PreviousStatus = '0' OR h.PreviousStatus IS NULL OR h.PreviousStatus = '' THEN 1 ELSE TRY_CAST(h.PreviousStatus AS INT) END = s_from.StatusID OR h.PreviousStatus = s_from.StatusName)
                    WHERE 
                        h.AppointmentId = @AppointmentId AND h.CompanyID = @CompanyID
                    ORDER BY 
                        h.StatusChangeDateTime DESC";

                // Initialize Command if needed (ExecuteParam uses db.Command.Parameters)
                if (db.Command == null)
                {
                    db.Command = new System.Data.SqlClient.SqlCommand();
                }

                // Clear any existing parameters
                db.Command.Parameters.Clear();

                db.AddParameter("@AppointmentId", appointmentId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);

                // ExecuteParam creates its own connection, so we don't need db.Open() or db.Close()
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetAppointmentStatusHistory: Found {dt.Rows.Count} history records");

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string statusTo = row.Field<string>("StatusToName") ?? "Unknown";
                        string statusFrom = row.Field<string>("StatusFromName") ?? "N/A";

                        // Map 'Scheduled' to 'Confirmed' for display
                        if (string.Equals(statusTo, "Scheduled", StringComparison.OrdinalIgnoreCase)) statusTo = "Confirmed";
                        if (string.Equals(statusFrom, "Scheduled", StringComparison.OrdinalIgnoreCase)) statusFrom = "Confirmed";

                        history.Add(new StatusHistoryViewModel
                        {
                            StatusName = statusTo,
                            StatusFromName = statusFrom,
                            Timestamp = row.Field<DateTime?>("ChangedAt")?.ToString("MM/dd/yyyy hh:mm tt") ?? "N/A",
                            ChangedBy = row.Field<string>("ChangedBy") ?? "System",
                            Note = row.Field<string>("Note") ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading status history: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            return history;
        }


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerInvoice> GetCustomerInvoices(string customerId)
        {
            var invoices = new List<CustomerInvoice>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetCustomerInvoices: CompanyID is missing from session");
                return invoices;
            }
            Database db = new Database();
            try
            {
                db.Open();
                DataTable dt = new DataTable();

                // InvoiceDate now falls back to CreatedDate/ExpirationDate if null
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
                cust.CustomerGuid,
                gen.AppointmentPrefix
            FROM tbl_Invoice as inv
            LEFT JOIN tbl_Customer as cust 
              ON inv.CustomerID = cust.CustomerID AND inv.CompnyID = cust.CompanyID
            LEFT JOIN tbl_AppointmentAutoGenerate gen
              ON inv.CompnyID = gen.CompanyID
            WHERE inv.CustomerID = @CustomerID AND inv.CompnyID = @CompanyID;";

                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);

                db.ExecuteParam(sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var invoice = new CustomerInvoice();

                        // mappings
                        invoice.ID = row.Field<string>("ID") ?? "";
                        invoice.InvoiceNumber = row.Field<string>("Number") ?? "";
                        invoice.InvoiceType = row.Field<string>("Type") ?? "";

                        // Format Appointment ID: Prefix-CompanyID-SeedNumber
                        string rawApptId = row.Field<string>("AppointmentId");
                        if (!string.IsNullOrEmpty(rawApptId))
                        {
                            string prefix = row.Table.Columns.Contains("AppointmentPrefix") ? (row["AppointmentPrefix"]?.ToString() ?? "APPT") : "APPT";
                            invoice.AppointmentId = $"{prefix}-{companyid}-{rawApptId}";
                        }
                        else
                        {
                            invoice.AppointmentId = "";
                        }

                        invoice.CustomerGuid = row.Field<string>("CustomerGuid") ?? "";
                        invoice.Total = row["Total"].ToString() ?? "0.0";
                        invoice.Subtotal = row["Subtotal"].ToString() ?? "0.0";
                        invoice.Due = row["Due"].ToString() ?? "0.0";
                        invoice.Discount = row["Discount"].ToString() ?? "0.0";
                        invoice.Tax = row["Tax"].ToString() ?? "0.0";
                        invoice.DepositAmount = row["DepositAmount"].ToString() ?? "0.0";

                        // map the computed date
                        invoice.InvoiceDate = row.Field<string>("InvoiceDate") ?? "";

                        // existing status logic
                        if ((Convert.ToDouble(row["Total"].ToString()) - Convert.ToDouble(row["AmountCollect"].ToString())) <= 0)
                            invoice.InvoiceStatus = "Paid";
                        else
                            invoice.InvoiceStatus = "Unpaid";

                        // existing external link logic
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
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerInvoices: {ex.Message}");
                return null;
            }
            finally { db.Close(); }
            return invoices;
        }



        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<Equipment> GetSiteEquipmentData(int siteId, string customerGuid)
        {
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid))
            {
                System.Diagnostics.Debug.WriteLine("GetSiteEquipmentData: CompanyID is missing from session");
                return new List<Equipment>();
            }
            var equipments = new List<Equipment>();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                db.Open();
                // Show equipment for the specific site only, join with EquipmentType to get the description
                string strSQL = @"SELECT eqp.*, cus.CustomerID, cus.FirstName, cus.LastName, et.equipmentTypeDesc
                                FROM [msSchedulerV3].dbo.tbl_Equipment eqp 
                                LEFT JOIN [msSchedulerV3].dbo.tbl_Customer cus ON eqp.CustomerGuid = cus.CustomerGuid
                                LEFT JOIN [msSchedulerV3].dbo.tbl_EquipmentType et ON eqp.EquipmentTypeID = et.equipmentTypeID
                                WHERE eqp.CustomerGuid=@CustomerGuid AND eqp.CompanyID = @CompanyID AND eqp.SiteId = @SiteId order by eqp.CreatedDateTime desc;";

                db.AddParameter("@CustomerGuid", customerGuid, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", siteId, SqlDbType.Int);

                db.ExecuteParam(strSQL, out dt);

                System.Diagnostics.Debug.WriteLine($"GetSiteEquipmentData: Found {dt?.Rows?.Count ?? 0} equipment items for CustomerGuid={customerGuid}, SiteId={siteId}");

                db.Close();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        equipments.Add(new Equipment
                        {
                            Id = Convert.ToInt32(dr["Id"]),
                            SiteId = dr["SiteId"] != DBNull.Value ? Convert.ToInt32(dr["SiteId"]) : 0,
                            CustomerGuid = customerGuid,
                            CustomerName = dr.Field<string>("FirstName") + " " + dr.Field<string>("LastName"),
                            CustomerID = dr["CustomerID"].ToString() ?? "",
                            Make = dr["Make"].ToString() ?? "",
                            Barcode = dr["Barcode"].ToString() ?? "",
                            SerialNumber = dr["SerialNumber"].ToString() ?? "",
                            Model = dr["Model"].ToString() ?? "",
                            Notes = dr["Notes"].ToString() ?? "",
                            EquipmentTypeID = dr["EquipmentTypeID"] != DBNull.Value ? Convert.ToInt32(dr["EquipmentTypeID"]) : 0,
                            EquipmentType = dr["equipmentTypeDesc"].ToString() ?? "",
                            CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"]),
                            WarrantyStart = dr["WarrantyStart"] != DBNull.Value && !string.IsNullOrEmpty(dr["WarrantyStart"].ToString())
                                   ? Convert.ToDateTime(dr["WarrantyStart"]).ToString("yyyy-MM-dd") : string.Empty,
                            WarrantyEnd = dr["WarrantyEnd"] != DBNull.Value && !string.IsNullOrEmpty(dr["WarrantyEnd"].ToString())
                                   ? Convert.ToDateTime(dr["WarrantyEnd"]).ToString("yyyy-MM-dd") : string.Empty,
                            LaborWarrantyStart = dr["LaborWarrantyStart"] != DBNull.Value && !string.IsNullOrEmpty(dr["LaborWarrantyStart"].ToString())
                                   ? Convert.ToDateTime(dr["LaborWarrantyStart"]).ToString("yyyy-MM-dd") : string.Empty,
                            LaborWarrantyEnd = dr["LaborWarrantyEnd"] != DBNull.Value && !string.IsNullOrEmpty(dr["LaborWarrantyEnd"].ToString())
                                   ? Convert.ToDateTime(dr["LaborWarrantyEnd"]).ToString("yyyy-MM-dd") : string.Empty,
                            InstallDate = dr["InstallDate"] != DBNull.Value && !string.IsNullOrEmpty(dr["InstallDate"].ToString())
                                   ? Convert.ToDateTime(dr["InstallDate"]).ToString("yyyy-MM-dd") : string.Empty,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetSiteEquipmentData: {ex.Message}");
                return null;
            }
            finally
            {
                db.Close();
            }
            return equipments;
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveEquipmentData(Equipment equipment)
        {
            if (equipment.Id > 0)
            {
                return UpdateEquipment(equipment);
            }
            else
            {
                return InsertEquipment(equipment);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<EquipmentType> GetEquipmentTypes()
        {
            List<EquipmentType> equipmentTypes = new List<EquipmentType>();
            Database db = new Database();
            try
            {
                db.Open();
                string strSQL = @"SELECT TOP (1000) [equipmentTypeID], [equipmentTypeDesc], [createdBy], [createdDateTime], [modifiedBy], [modifiedDateTime], [updateStatus]
                                  FROM [msSchedulerV3].[dbo].[tbl_EquipmentType]
                                  WHERE [updateStatus] IS NULL OR [updateStatus] <> 'D'
                                  ORDER BY [equipmentTypeDesc]";
                System.Diagnostics.Debug.WriteLine($"GetEquipmentTypes SQL: {strSQL}");
                System.Data.DataTable dt = new System.Data.DataTable();
                db.ExecuteParam(strSQL, out dt);

                System.Diagnostics.Debug.WriteLine($"GetEquipmentTypes: Found {dt?.Rows?.Count ?? 0} equipment types");

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        equipmentTypes.Add(new EquipmentType
                        {
                            Id = dr["equipmentTypeID"] != DBNull.Value ? dr["equipmentTypeID"].ToString() : "",
                            TypeName = dr["equipmentTypeDesc"].ToString() ?? "",
                            CreatedBy = dr["createdBy"]?.ToString() ?? "",
                            UpdatedBy = dr["modifiedBy"]?.ToString() ?? "",
                            CreatedDateTime = dr["createdDateTime"] != DBNull.Value ? Convert.ToDateTime(dr["createdDateTime"]) : (DateTime?)null,
                            UpdateDateTime = dr["modifiedDateTime"] != DBNull.Value ? Convert.ToDateTime(dr["modifiedDateTime"]) : (DateTime?)null
                        });
                        System.Diagnostics.Debug.WriteLine($"  - Added: {dr["equipmentTypeDesc"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEquipmentTypes: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                db.Close();
            }
            return equipmentTypes;
        }

        public static bool InsertEquipment(Equipment equipment)
        {
            bool success = false;
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                string strSQL = @"INSERT INTO [msSchedulerV3].dbo.tbl_Equipment
                        (CompanyID, CustomerID, CustomerGuid, SiteId, Make, Model, Notes, EquipmentTypeID, Barcode, SerialNumber, WarrantyStart, WarrantyEnd, LaborWarrantyStart, LaborWarrantyEnd, InstallDate) output INSERTED.ID
                        VALUES (@CompanyID, @CustomerID, @CustomerGuid, @SiteId, @Make, @Model, @Notes, @EquipmentTypeID, @Barcode, @SerialNumber, @WarrantyStart, @WarrantyEnd, @LaborWarrantyStart, @LaborWarrantyEnd, @InstallDate)";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", equipment.CustomerID, SqlDbType.NVarChar);
                db.AddParameter("@CustomerGuid", equipment.CustomerGuid, SqlDbType.NVarChar);
                db.AddParameter("@Make", equipment.Make, SqlDbType.NVarChar);
                db.AddParameter("@Model", equipment.Model, SqlDbType.NVarChar);
                db.AddParameter("@Notes", equipment.Notes, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", equipment.SiteId, SqlDbType.Int);
                db.AddParameter("@EquipmentTypeID", equipment.EquipmentTypeID, SqlDbType.Int);
                db.AddParameter("@Barcode", equipment.Barcode, SqlDbType.NVarChar);
                db.AddParameter("@SerialNumber", equipment.SerialNumber, SqlDbType.NVarChar);
                db.AddParameter("@WarrantyStart", string.IsNullOrEmpty(equipment.WarrantyStart) ? DBNull.Value : (object)equipment.WarrantyStart, SqlDbType.DateTime);
                db.AddParameter("@WarrantyEnd", string.IsNullOrEmpty(equipment.WarrantyEnd) ? DBNull.Value : (object)equipment.WarrantyEnd, SqlDbType.DateTime);
                db.AddParameter("@LaborWarrantyStart", string.IsNullOrEmpty(equipment.LaborWarrantyStart) ? DBNull.Value : (object)equipment.LaborWarrantyStart, SqlDbType.DateTime);
                db.AddParameter("@LaborWarrantyEnd", string.IsNullOrEmpty(equipment.LaborWarrantyEnd) ? DBNull.Value : (object)equipment.LaborWarrantyEnd, SqlDbType.DateTime);
                db.AddParameter("@InstallDate", string.IsNullOrEmpty(equipment.InstallDate) ? DBNull.Value : (object)equipment.InstallDate, SqlDbType.DateTime);
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

        public static bool UpdateEquipment(Equipment equipment)
        {
            bool success = false;
            Database db = new Database();
            try
            {
                db.Open();
                string strSQL = @"UPDATE [msSchedulerV3].dbo.tbl_Equipment SET
                                    Make = @Make,
                                    EquipmentTypeID = @EquipmentTypeID,
                                    Barcode = @Barcode,
                                    SerialNumber = @SerialNumber,
                                    Model = @Model,
                                    Notes = @Notes,
                                    InstallDate = @InstallDate,
                                    WarrantyStart = @WarrantyStart,
                                    WarrantyEnd = @WarrantyEnd,
                                    LaborWarrantyStart = @LaborWarrantyStart,
                                    LaborWarrantyEnd = @LaborWarrantyEnd
                                    WHERE Id=@Id and SiteId = @SiteId";
                db.AddParameter("@Make", equipment.Make, SqlDbType.NVarChar);
                db.AddParameter("@EquipmentTypeID", equipment.EquipmentTypeID, SqlDbType.Int);
                db.AddParameter("@Barcode", equipment.Barcode, SqlDbType.NVarChar);
                db.AddParameter("@SerialNumber", equipment.SerialNumber, SqlDbType.NVarChar);
                db.AddParameter("@Model", equipment.Model, SqlDbType.NVarChar);
                db.AddParameter("@Notes", equipment.Notes, SqlDbType.NVarChar);
                db.AddParameter("@InstallDate", string.IsNullOrEmpty(equipment.InstallDate) ? DBNull.Value : (object)equipment.InstallDate, SqlDbType.NVarChar);
                db.AddParameter("@WarrantyStart", string.IsNullOrEmpty(equipment.WarrantyStart) ? DBNull.Value : (object)equipment.WarrantyStart, SqlDbType.NVarChar);
                db.AddParameter("@WarrantyEnd", string.IsNullOrEmpty(equipment.WarrantyEnd) ? DBNull.Value : (object)equipment.WarrantyEnd, SqlDbType.NVarChar);
                db.AddParameter("@LaborWarrantyStart", string.IsNullOrEmpty(equipment.LaborWarrantyStart) ? DBNull.Value : (object)equipment.LaborWarrantyStart, SqlDbType.NVarChar);
                db.AddParameter("@LaborWarrantyEnd", string.IsNullOrEmpty(equipment.LaborWarrantyEnd) ? DBNull.Value : (object)equipment.LaborWarrantyEnd, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", equipment.SiteId, SqlDbType.Int);
                db.AddParameter("@Id", equipment.Id, SqlDbType.Int);
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
        public static bool DeleteEquipment(int equipmentId)
        {
            bool success = false;
            Database db = new Database();
            try
            {
                db.Open();
                string strSQL = @"DELETE FROM [msSchedulerV3].dbo.tbl_Equipment WHERE Id=@Id";
                db.AddParameter("@Id", equipmentId, SqlDbType.Int);
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


        //protected void invoiceCreate_Click(object sender, EventArgs e)
        //{
        //    string cid = lblCustomerGuid.Text.ToString();
        //    string InType = "Invoice";
        //    Response.Redirect($"InvoiceCreate.aspx?InvNum=0&cId={cid}&InType={InType}");
        //}

        //protected void estimateCreate_Click(object sender, EventArgs e)
        //{
        //    string cid = lblCustomerGuid.Text.ToString();
        //    string InType = "Proposal";
        //    Response.Redirect($"InvoiceCreate.aspx?InvNum=0&cId={cid}&InType={InType}");
        //}

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<NoteViewModel> GetCustomerNotes(string customerId, int siteId)
        {
            var notes = new List<NoteViewModel>();
            string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId))
            {
                return notes;
            }

            Database db = new Database();
            DataTable dt = new DataTable();

            try
            {
                // Query notes from database - show ALL notes for the customer across all sites
                // Use COALESCE/ISNULL for columns that may not exist to prevent errors
                // Convert CustomerId and AppointmentId to strings if they are integers
                string sql = @"
                    SELECT 
                        n.Id,
                        n.Description,
                        n.Reference,
                        n.CreatedAt,
                        COALESCE(NULLIF(n.UserId, ''), NULLIF(n.CreatedBy, ''), '') as UserId,
                        COALESCE(n.TaggedTo, '') as TaggedTo,
                        COALESCE(n.TaggedFrom, 'FSM') as TaggedFrom,
                        COALESCE(CAST(n.AppointmentId AS NVARCHAR(50)), '') as AppointmentId,
                        CAST(n.CustomerId AS NVARCHAR(50)) as CustomerId,
                        n.CompanyId,
                        COALESCE(n.SiteId, 0) as SiteId
                    FROM [msSchedulerV3].[dbo].[tbl_Note] n
                    WHERE n.CustomerId = @CustomerId 
                      AND n.CompanyId = @CompanyId and n.SiteID = @siteId and  n.TaggedFrom = 'FSM' 
                    ORDER BY n.CreatedAt DESC";

                // Initialize Command if needed (ExecuteParam uses db.Command.Parameters)
                if (db.Command == null)
                {
                    db.Command = new System.Data.SqlClient.SqlCommand();
                }

                // Clear any existing parameters
                db.Command.Parameters.Clear();

                // Add parameters (SiteId not needed - showing all customer notes)
                db.AddParameter("@CustomerId", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyId", companyid, SqlDbType.NVarChar);
                db.AddParameter("@siteId", siteId, SqlDbType.NVarChar);

                // ExecuteParam creates its own connection, so we don't need db.Open() or db.Close()
                db.ExecuteParam(sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var note = new NoteViewModel
                        {
                            Id = row.Field<int?>("Id") ?? 0,
                            Description = row.Field<string>("Description") ?? "",
                            Reference = row.Table.Columns.Contains("Reference") ? (row.Field<string>("Reference") ?? "") : "",
                            CreatedAt = row.Field<DateTime?>("CreatedAt")?.ToString("MM/dd/yyyy HH:mm") ?? "",
                            UserId = row.Field<string>("UserId") ?? "",
                            TaggedTo = row.Field<string>("TaggedTo") ?? "",
                            TaggedFrom = row.Field<string>("TaggedFrom") ?? "FSM",
                            // Handle both string and int types for AppointmentId and CustomerId
                            AppointmentId = row["AppointmentId"] != null && row["AppointmentId"] != DBNull.Value
                                ? row["AppointmentId"].ToString()
                                : "",
                            CustomerId = row["CustomerId"] != null && row["CustomerId"] != DBNull.Value
                                ? row["CustomerId"].ToString()
                                : ""
                        };
                        notes.Add(note);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerNotes: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                // Return empty list on error so UI doesn't break
                return notes;
            }
            // No finally block needed - ExecuteParam manages its own connection
            return notes;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateCustomerNote(int noteId, string description, string taggedTo, string taggedFrom)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"UpdateCustomerNote called with: noteId={noteId}, description length={description?.Length ?? 0}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || noteId <= 0 || string.IsNullOrEmpty(description))
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCustomerNote: Validation failed - CompanyID: {companyid}, NoteId: {noteId}, Description length: {description?.Length ?? 0}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                // UPDATED SQL QUERY
                string sql = @"
            UPDATE [msSchedulerV3].[dbo].[tbl_Note]
            SET [Description] = @Description,
                [TaggedFrom] = @TaggedFrom,
                [TaggedTo] = @TaggedTo
            WHERE [Id] = @NoteId AND [CompanyId] = @CompanyId";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@NoteId", System.Data.SqlDbType.Int) { Value = noteId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyId", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Description", System.Data.SqlDbType.NVarChar) { Value = description });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TaggedFrom", System.Data.SqlDbType.NVarChar) { Value = taggedFrom ?? "FSM" });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TaggedTo", System.Data.SqlDbType.NVarChar) { Value = string.IsNullOrEmpty(taggedTo) ? (object)DBNull.Value : taggedTo });

                    System.Diagnostics.Debug.WriteLine($"UpdateCustomerNote: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"UpdateCustomerNote: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCustomerNote: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteCustomerNote(int noteId)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                if (string.IsNullOrEmpty(companyid) || noteId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteCustomerNote: Validation failed - CompanyID: {companyid}, NoteId: {noteId}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                string sql = @"
                    DELETE FROM [msSchedulerV3].[dbo].[tbl_Note]
                    WHERE Id = @NoteId AND CompanyId = @CompanyId";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@NoteId", System.Data.SqlDbType.Int) { Value = noteId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyId", System.Data.SqlDbType.NVarChar) { Value = companyid });

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"DeleteCustomerNote: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteCustomerNote: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        public class NoteViewModel
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public string CreatedAt { get; set; }
            public string UserId { get; set; }
            public string TaggedTo { get; set; }
            public string TaggedFrom { get; set; }
            public string AppointmentId { get; set; }
            public string Reference { get; set; }
            public string CustomerId { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomerNote(int noteId, string customerId, int siteId, string description, string reference, string taggedTo, string taggedFrom)
        {
            Database db = null;
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = HttpContext.Current.Session["LoginUser"]?.ToString() ?? HttpContext.Current.Session["UserID"]?.ToString() ?? HttpContext.Current.User?.Identity?.Name ?? "System";

                System.Diagnostics.Debug.WriteLine($"SaveCustomerNote called with: customerId={customerId}, siteId={siteId}, description length={description?.Length ?? 0}, reference={reference}, companyid={companyid}, userId={userId}");

                if (string.IsNullOrEmpty(companyid))
                {
                    System.Diagnostics.Debug.WriteLine("SaveCustomerNote: CompanyID is missing from session");
                    return false;
                }

                if (string.IsNullOrEmpty(customerId))
                {
                    System.Diagnostics.Debug.WriteLine("SaveCustomerNote: customerId is null or empty");
                    return false;
                }

                if (string.IsNullOrEmpty(description))
                {
                    System.Diagnostics.Debug.WriteLine("SaveCustomerNote: description is null or empty");
                    return false;
                }


                db = new Database();
                string connString = db.ConnectionString;

                string sql = "";
                if (noteId > 0)
                {
                    sql = @"
                    UPDATE [msSchedulerV3].[dbo].[tbl_Note]
                    SET [Description] = @Description, 
                        [Reference] = @Reference,
                        [SiteId] = @SiteId,
                        [UserId] = @UserId,
                        [CreatedBy] = @CreatedBy
                    WHERE [Id] = @NoteId AND [CompanyId] = @CompanyId";
                }
                else
                {
                    sql = @"
                    INSERT INTO [msSchedulerV3].[dbo].[tbl_Note]
                    ([Description], [Reference], [CreatedAt], [CustomerId], [AppointmentId], [CompanyId], 
                     [UserId], [TagId], [SiteId], [TaggedFrom], [TaggedTo], [CreatedBy])
                    VALUES (@Description, @Reference, GETDATE(), @CustomerId, NULL, @CompanyId, 
                            @UserId, NULL, @SiteId, @TaggedFrom, @TaggedTo, @CreatedBy)";
                }

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    // Add parameters - matching column names exactly
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Description", System.Data.SqlDbType.NVarChar) { Value = description });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Reference", System.Data.SqlDbType.NVarChar) { Value = (object)reference ?? DBNull.Value });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CustomerId", System.Data.SqlDbType.NVarChar) { Value = customerId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyId", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@UserId", System.Data.SqlDbType.NVarChar) { Value = userId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@SiteId", System.Data.SqlDbType.Int) { Value = siteId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CreatedBy", System.Data.SqlDbType.NVarChar) { Value = userId });

                    if (noteId > 0)
                    {
                        cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@NoteId", System.Data.SqlDbType.Int) { Value = noteId });
                    }
                    else
                    {
                        // TaggedFrom and TaggedTo are removed from UI, setting to NULL/Default for INSERT
                        cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TaggedFrom", System.Data.SqlDbType.NVarChar) { Value = "FSM" });
                        cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@TaggedTo", System.Data.SqlDbType.NVarChar) { Value = (object)DBNull.Value });
                    }

                    System.Diagnostics.Debug.WriteLine($"SaveCustomerNote: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"SaveCustomerNote: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveCustomerNote: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
            finally
            {
                // Connection is automatically closed by the using statement
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveMaintenanceAgreement(string customerId, int siteId, string fileName, string fileContent, string expirationDate, string alarmDate, bool alarmSet)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = HttpContext.Current.Session["UserID"]?.ToString() ?? HttpContext.Current.User?.Identity?.Name ?? "System";

                System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement called with: customerId={customerId}, siteId={siteId}, fileName={fileName}, fileContent length={fileContent?.Length ?? 0}, companyid={companyid}, userId={userId}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileContent))
                {
                    System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement: Validation failed - CompanyID: {companyid}, CustomerID: {customerId}, FileName: {fileName}, FileContent length: {fileContent?.Length ?? 0}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                // Convert base64 to bytes
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(fileContent);
                    System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement: Converted base64 to bytes, length: {fileBytes.Length}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement: Error converting base64: {ex.Message}");
                    return false;
                }

                DateTime? expDate = null;
                if (!string.IsNullOrEmpty(expirationDate)) expDate = DateTime.Parse(expirationDate);

                DateTime? almDate = null;
                if (!string.IsNullOrEmpty(alarmDate)) almDate = DateTime.Parse(alarmDate);

                string sql = @"
                    INSERT INTO [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements]
                    (CompanyID, CustomerID, SiteId, FileName, FileContent, UploadDate, CreatedBy, ExpirationDate, AlarmDate, AlarmSet)
                    VALUES (@CompanyID, @CustomerID, @SiteId, @FileName, @FileContent, GETDATE(), @CreatedBy, @ExpirationDate, @AlarmDate, @AlarmSet)";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CustomerID", System.Data.SqlDbType.NVarChar) { Value = customerId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@SiteId", System.Data.SqlDbType.Int) { Value = siteId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileName", System.Data.SqlDbType.NVarChar) { Value = fileName });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileContent", System.Data.SqlDbType.VarBinary) { Value = fileBytes });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CreatedBy", System.Data.SqlDbType.NVarChar) { Value = userId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@ExpirationDate", System.Data.SqlDbType.DateTime) { Value = (object)expDate ?? DBNull.Value });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@AlarmDate", System.Data.SqlDbType.DateTime) { Value = (object)almDate ?? DBNull.Value });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@AlarmSet", System.Data.SqlDbType.Bit) { Value = alarmSet });

                    System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"SaveMaintenanceAgreement: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveMaintenanceAgreement: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateMaintenanceAgreement(int agreementId, string expirationDate, string alarmDate, bool alarmSet)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyid) || agreementId <= 0) return false;

                Database db = new Database();
                DateTime? expDate = null;
                if (!string.IsNullOrEmpty(expirationDate) && expirationDate != "-") expDate = DateTime.Parse(expirationDate);

                DateTime? almDate = null;
                if (!string.IsNullOrEmpty(alarmDate) && alarmDate != "-") almDate = DateTime.Parse(alarmDate);

                string sql = @"
                    UPDATE [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements]
                    SET ExpirationDate = @ExpirationDate, 
                        AlarmDate = @AlarmDate, 
                        AlarmSet = @AlarmSet
                    WHERE Id = @Id AND CompanyID = @CompanyID";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(db.ConnectionString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = agreementId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@ExpirationDate", System.Data.SqlDbType.DateTime) { Value = (object)expDate ?? DBNull.Value });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@AlarmDate", System.Data.SqlDbType.DateTime) { Value = (object)almDate ?? DBNull.Value });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@AlarmSet", System.Data.SqlDbType.Bit) { Value = alarmSet });

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateMaintenanceAgreement: {ex.Message}");
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetFileContent(int fileId, string type)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyid))
                {
                    return new { status = "error", message = "Unauthorized" };
                }

                Database db = new Database();
                byte[] fileContent = null;
                string fileName = "";
                string contentType = "application/octet-stream";

                string sql = "";
                switch (type.ToLower())
                {
                    case "agreement":
                        sql = "SELECT FileName, FileContent FROM [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements] WHERE Id = @Id AND CompanyID = @CompanyID";
                        contentType = "application/pdf";
                        break;
                    case "picture":
                        sql = "SELECT FileName, FileContent FROM [msSchedulerV3].[dbo].[tbl_Pictures] WHERE Id = @Id AND CompanyID = @CompanyID";
                        break;
                    case "file":
                        sql = "SELECT FileName, FileType, FileContent FROM [msSchedulerV3].[dbo].[tbl_Files] WHERE Id = @Id AND CompanyID = @CompanyID";
                        break;
                    default:
                        return new { status = "error", message = "Invalid type" };
                }

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(db.ConnectionString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = fileId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });

                    conn.Open();
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileName = reader["FileName"]?.ToString() ?? "file";
                            fileContent = (byte[])reader["FileContent"];

                            if (type.ToLower() == "file" && reader["FileType"] != DBNull.Value)
                            {
                                contentType = reader["FileType"]?.ToString();
                            }
                            else if (type.ToLower() == "picture")
                            {
                                string ext = System.IO.Path.GetExtension(fileName).ToLower();
                                switch (ext)
                                {
                                    case ".jpg": case ".jpeg": contentType = "image/jpeg"; break;
                                    case ".png": contentType = "image/png"; break;
                                    case ".gif": contentType = "image/gif"; break;
                                    case ".bmp": contentType = "image/bmp"; break;
                                    default: contentType = "image/jpeg"; break;
                                }
                            }

                            return new
                            {
                                status = "success",
                                fileName = fileName,
                                contentType = contentType,
                                content = Convert.ToBase64String(fileContent)
                            };
                        }
                    }
                }

                return new { status = "error", message = "File not found" };
            }
            catch (Exception ex)
            {
                return new { status = "error", message = ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteMaintenanceAgreement(int agreementId)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"DeleteMaintenanceAgreement called with: agreementId={agreementId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || agreementId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteMaintenanceAgreement: Validation failed - CompanyID: {companyid}, AgreementId: {agreementId}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                string sql = @"
                    DELETE FROM [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements]
                    WHERE Id = @AgreementId AND CompanyID = @CompanyID";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@AgreementId", System.Data.SqlDbType.Int) { Value = agreementId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });

                    System.Diagnostics.Debug.WriteLine($"DeleteMaintenanceAgreement: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"DeleteMaintenanceAgreement: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteMaintenanceAgreement: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        public class MaintenanceAgreementViewModel
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FileUrl { get; set; }
            public string Name { get; set; }
            public int? AppointmentId { get; set; }
            public string TaggedFrom { get; set; }
            public string TaggedTo { get; set; }
            public string UploadDate { get; set; }
            public string ExpirationDate { get; set; }
            public string AlarmDate { get; set; }
            public bool AlarmSet { get; set; }
            public bool AlarmTriggered { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<MaintenanceAgreementViewModel> GetMaintenanceAgreements(string customerId, int siteId)
        {
            var agreements = new List<MaintenanceAgreementViewModel>();

            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"GetMaintenanceAgreements called with: customerId={customerId}, siteId={siteId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId))
                {
                    System.Diagnostics.Debug.WriteLine($"GetMaintenanceAgreements: Validation failed - CompanyID: {companyid}, CustomerID: {customerId}");
                    return agreements;
                }

                Database db = new Database();
                DataTable dt = new DataTable();

                // Show ALL maintenance agreements for the customer across all sites
                // Handle CustomerID as both string and integer for compatibility
                // Note: tbl_MaintenanceAgreements table doesn't have AppointmentId, TaggedFrom, TaggedTo, UploadedFrom, UploadedTo columns
                string sql = @"
                    SELECT 
                        Id,
                        FileName,
                        UploadDate,
                        CustomerID,
                        SiteId,
                        ExpirationDate,
                        AlarmDate,
                        AlarmSet,
                        AlarmTriggered,
                        NULL as AppointmentId,
                        'FSM' as TaggedFrom,
                        NULL as TaggedTo,
                        NULL as UploadedFrom,
                        NULL as UploadedTo
                    FROM [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements]
                    WHERE CAST(CustomerID AS NVARCHAR(50)) = @CustomerID and SiteId = @SiteId
                      AND CompanyID = @CompanyID
                    ORDER BY UploadDate DESC";

                // Initialize Command if needed (ExecuteParam uses db.Command.Parameters)
                if (db.Command == null)
                {
                    db.Command = new System.Data.SqlClient.SqlCommand();
                }

                // Clear any existing parameters
                db.Command.Parameters.Clear();

                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", siteId, SqlDbType.NVarChar);
                // SiteId parameter removed - showing all customer agreements

                // ExecuteParam creates its own connection, so we don't need db.Open() or db.Close()
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetMaintenanceAgreements: Found {dt.Rows.Count} agreements for customer {customerId}");

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var agreement = new MaintenanceAgreementViewModel
                        {
                            Id = row.Field<int>("Id"),
                            FileName = row.Field<string>("FileName") ?? "",
                            FileUrl = $"/fsm/CustomerDetails.aspx?type=agreement&id={row.Field<int>("Id")}",
                            Name = row.Field<string>("FileName") ?? "",
                            AppointmentId = row.Table.Columns.Contains("AppointmentId") ? row.Field<int?>("AppointmentId") : null,
                            TaggedFrom = row.Table.Columns.Contains("TaggedFrom") ? row.Field<string>("TaggedFrom") : "FSM",
                            TaggedTo = row.Table.Columns.Contains("TaggedTo") ? row.Field<string>("TaggedTo") : null,
                            UploadDate = row.Table.Columns.Contains("UploadDate") ? row.Field<DateTime?>("UploadDate")?.ToString("MM/dd/yyyy HH:mm") ?? "" : "",
                            ExpirationDate = row.Table.Columns.Contains("ExpirationDate") ? row.Field<DateTime?>("ExpirationDate")?.ToString("MM/dd/yyyy") ?? "" : "",
                            AlarmDate = row.Table.Columns.Contains("AlarmDate") ? row.Field<DateTime?>("AlarmDate")?.ToString("MM/dd/yyyy hh:mm tt") ?? "" : "",
                            AlarmSet = row.Table.Columns.Contains("AlarmSet") ? row.Field<bool>("AlarmSet") : false,
                            AlarmTriggered = row.Table.Columns.Contains("AlarmTriggered") ? row.Field<bool>("AlarmTriggered") : false
                        };
                        agreements.Add(agreement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetMaintenanceAgreements: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            return agreements;
        }

        public class CslDrawerData
        {
            public CustomerEntity CustomerInfo { get; set; }
            public CustomerSite SiteInfo { get; set; }
            public List<AppointmentModel> Appointments { get; set; }
            public List<CustomerInvoice> Invoices { get; set; }
            public List<Equipment> Equipment { get; set; }
            public List<NoteViewModel> Notes { get; set; }
            public List<PictureViewModel> Pictures { get; set; }
            public List<FileViewModel> Files { get; set; }
            public List<MaintenanceAgreementViewModel> MaintenanceAgreements { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static CslDrawerData GetCslDrawerData(string customerId, int siteId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                System.Diagnostics.Debug.WriteLine("GetCslDrawerData: CustomerId is missing");
                return null;
            }

            var data = new CslDrawerData();
            try
            {
                data.CustomerInfo = GetCustomerDetails(customerId);

                if (data.CustomerInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetCslDrawerData: Customer with ID {customerId} not found.");
                    return null;
                }

                if (siteId == 0)
                {
                    data.SiteInfo = new CustomerSite
                    {
                        Id = 0,
                        SiteName = "Customer Location (Default)",
                        Address = data.CustomerInfo.Address1,
                        City = data.CustomerInfo.City,
                        State = LocationHelper.GetFullName(data.CustomerInfo.State),
                        Zip = data.CustomerInfo.ZipCode,
                        FirstName = data.CustomerInfo.FirstName,
                        LastName = data.CustomerInfo.LastName,
                        PhoneNumber = data.CustomerInfo.Phone,
                        Email = data.CustomerInfo.Email,
                        IsActive = true
                    };
                }
                else
                {
                    data.SiteInfo = GetCustomerSitebyId(customerId, siteId);
                }

                data.Appointments = GetCustomerAppoinmets(customerId, siteId);
                data.Invoices = GetCustomerInvoices(customerId);
                data.Notes = GetCustomerNotes(customerId, siteId);

                if (!string.IsNullOrEmpty(data.CustomerInfo.CustomerGuid))
                {
                    data.Equipment = GetSiteEquipmentData(siteId, data.CustomerInfo.CustomerGuid);
                }
                else
                {
                    data.Equipment = new List<Equipment>();
                }

                // Fetch Pictures, Files, and Maintenance Agreements
                try
                {
                    data.Pictures = GetSitePictures(customerId, siteId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading pictures in GetCslDrawerData: {ex.Message}");
                    data.Pictures = new List<PictureViewModel>();
                }

                try
                {
                    data.Files = GetSiteFiles(customerId, siteId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading files in GetCslDrawerData: {ex.Message}");
                    data.Files = new List<FileViewModel>();
                }

                try
                {
                    data.MaintenanceAgreements = GetMaintenanceAgreements(customerId, siteId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading maintenance agreements in GetCslDrawerData: {ex.Message}");
                    data.MaintenanceAgreements = new List<MaintenanceAgreementViewModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCslDrawerData: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }

            return data;
        }

        public class PictureViewModel
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FileUrl { get; set; }
            public string UploadDate { get; set; }
            public string UploadedBy { get; set; }
            public int? AppointmentId { get; set; }
            public string Reference { get; set; }
            public string TaggedFrom { get; set; }
            public string TaggedTo { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<PictureViewModel> GetSitePictures(string customerId, int siteId)
        {
            var pictures = new List<PictureViewModel>();

            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"GetSitePictures called with: customerId={customerId}, siteId={siteId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId))
                {
                    System.Diagnostics.Debug.WriteLine($"GetSitePictures: Validation failed - CompanyID: {companyid}, CustomerID: {customerId}");
                    return pictures;
                }

                Database db = new Database();
                DataTable dt = new DataTable();

                // Show ALL pictures for the customer across all sites
                // Handle CustomerID as both string and integer for compatibility
                // Note: tbl_Pictures table doesn't have AppointmentId, TaggedFrom, TaggedTo, UploadedFrom, UploadedTo columns
                string sql = @"
                    SELECT
                        Id,
                        FileName,
                        PictureURL,
                        UploadDate,
                        COALESCE(NULLIF(UploadedBy, ''), 'System') as UploadedBy,
                        SiteId,
                        AppointmentId,
                        Reference,
                        'FSM' as TaggedFrom,
                        NULL as TaggedTo,
                        NULL as UploadedFrom,
                        NULL as UploadedTo
                    FROM [msSchedulerV3].[dbo].[tbl_Pictures]
                    WHERE CAST(CustomerID AS NVARCHAR(50)) = @CustomerID
                      AND CompanyID = @CompanyID and SiteId = @SiteId
                    ORDER BY UploadDate DESC";

                // Initialize Command if needed (ExecuteParam uses db.Command.Parameters)
                if (db.Command == null)
                {
                    db.Command = new System.Data.SqlClient.SqlCommand();
                }

                // Clear any existing parameters
                db.Command.Parameters.Clear();

                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", siteId, SqlDbType.NVarChar);
                // SiteId parameter removed - showing all customer pictures

                // ExecuteParam creates its own connection, so we don't need db.Open() or db.Close()
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetSitePictures: Found {dt.Rows.Count} pictures for customer {customerId}");

                string appPath = HttpContext.Current.Request.ApplicationPath.TrimEnd('/');

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string pictureUrl = row.Table.Columns.Contains("PictureURL") && row["PictureURL"] != DBNull.Value
                            ? row.Field<string>("PictureURL") : null;

                        string fileUrl;
                        if (!string.IsNullOrEmpty(pictureUrl) && pictureUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            // Full absolute URL (new format)
                            fileUrl = pictureUrl;
                        }
                        else if (!string.IsNullOrEmpty(pictureUrl))
                        {
                            // Relative path (legacy format)
                            fileUrl = appPath + "/" + pictureUrl;
                        }
                        else
                        {
                            fileUrl = appPath + $"/CustomerDetails.aspx?type=picture&id={row.Field<int>("Id")}";
                        }

                        pictures.Add(new PictureViewModel
                        {
                            Id = row.Field<int>("Id"),
                            FileName = row.Field<string>("FileName") ?? "",
                            FileUrl = fileUrl,
                            UploadDate = row.Field<DateTime?>("UploadDate")?.ToString("MM/dd/yyyy HH:mm") ?? "",
                            UploadedBy = row.Field<string>("UploadedBy") ?? "System",
                            AppointmentId = row.Table.Columns.Contains("AppointmentId") && row["AppointmentId"] != DBNull.Value ? (int?)Convert.ToInt32(row["AppointmentId"]) : null,
                            Reference = row.Table.Columns.Contains("Reference") ? (row.Field<string>("Reference") ?? "") : "",
                            TaggedFrom = row.Table.Columns.Contains("TaggedFrom") ? row.Field<string>("TaggedFrom") : "FSM",
                            TaggedTo = row.Table.Columns.Contains("TaggedTo") ? row.Field<string>("TaggedTo") : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetSitePictures: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            return pictures;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveSitePicture(string customerId, int siteId, string fileName, string fileContent, string reference)
        {
            string fullPath = null;
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = HttpContext.Current.Session["LoginUser"]?.ToString() ?? HttpContext.Current.Session["UserID"]?.ToString() ?? HttpContext.Current.User?.Identity?.Name ?? "System";

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileContent))
                {
                    return false;
                }

                // Convert base64 to bytes
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(fileContent);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveSitePicture: Error converting base64: {ex.Message}");
                    return false;
                }

                // Save file to disk
                string relativePath = "FSMPictures/" + companyid + "/" + customerId + "/";
                string folderPath = HttpContext.Current.Server.MapPath("~/" + relativePath);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string safeFileName = Path.GetFileName(fileName);
                fullPath = Path.Combine(folderPath, safeFileName);
                if (File.Exists(fullPath))
                {
                    safeFileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + safeFileName;
                    fullPath = Path.Combine(folderPath, safeFileName);
                }

                File.WriteAllBytes(fullPath, fileBytes);

                // Build full absolute URL from current request
                var request = HttpContext.Current.Request;
                string baseUrl = request.Url.Scheme + "://" + request.Url.Authority + request.ApplicationPath.TrimEnd('/') + "/";
                string pictureUrl = baseUrl + relativePath + safeFileName;

                Database db = new Database();
                string connString = db.ConnectionString;

                string sql = @"
                    INSERT INTO [msSchedulerV3].[dbo].[tbl_Pictures]
                    (CompanyID, CustomerID, SiteId, FileName, PictureURL, UploadDate, UploadedBy, Reference, Tag)
                    VALUES (@CompanyID, @CustomerID, @SiteId, @FileName, @PictureURL, GETDATE(), @UploadedBy, @Reference, @Tag)";

                using (SqlConnection conn = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new SqlParameter("@CompanyID", SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new SqlParameter("@CustomerID", SqlDbType.NVarChar) { Value = customerId });
                    cmd.Parameters.Add(new SqlParameter("@SiteId", SqlDbType.Int) { Value = siteId });
                    cmd.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar) { Value = safeFileName });
                    cmd.Parameters.Add(new SqlParameter("@PictureURL", SqlDbType.NVarChar) { Value = pictureUrl });
                    cmd.Parameters.Add(new SqlParameter("@UploadedBy", SqlDbType.NVarChar) { Value = userId });
                    cmd.Parameters.Add(new SqlParameter("@Reference", SqlDbType.NVarChar) { Value = (object)reference ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@Tag", SqlDbType.NVarChar) { Value = "FSM" });

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected <= 0 && fullPath != null && File.Exists(fullPath))
                    {
                        File.Delete(fullPath); // Cleanup file if DB insert failed
                    }

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                // Cleanup file on error
                if (fullPath != null && File.Exists(fullPath))
                {
                    try { File.Delete(fullPath); } catch { }
                }
                System.Diagnostics.Debug.WriteLine($"Error in SaveSitePicture: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteSitePicture(int pictureId)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                if (string.IsNullOrEmpty(companyid) || pictureId <= 0)
                {
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                // First get the PictureURL so we can delete the physical file
                string pictureUrl = null;
                string selectSql = @"SELECT PictureURL FROM [msSchedulerV3].[dbo].[tbl_Pictures]
                    WHERE Id = @PictureId AND CompanyID = @CompanyID";

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    using (SqlCommand selectCmd = new SqlCommand(selectSql, conn))
                    {
                        selectCmd.Parameters.Add(new SqlParameter("@PictureId", SqlDbType.Int) { Value = pictureId });
                        selectCmd.Parameters.Add(new SqlParameter("@CompanyID", SqlDbType.NVarChar) { Value = companyid });
                        object result = selectCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            pictureUrl = result.ToString();
                        }
                    }

                    // Now delete the DB record
                    string deleteSql = @"DELETE FROM [msSchedulerV3].[dbo].[tbl_Pictures]
                        WHERE Id = @PictureId AND CompanyID = @CompanyID";

                    using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn))
                    {
                        deleteCmd.Parameters.Add(new SqlParameter("@PictureId", SqlDbType.Int) { Value = pictureId });
                        deleteCmd.Parameters.Add(new SqlParameter("@CompanyID", SqlDbType.NVarChar) { Value = companyid });

                        int rowsAffected = deleteCmd.ExecuteNonQuery();

                        // Delete physical file after successful DB delete
                        if (rowsAffected > 0 && !string.IsNullOrEmpty(pictureUrl))
                        {
                            try
                            {
                                string physicalPath = HttpContext.Current.Server.MapPath("~/" + pictureUrl);
                                if (File.Exists(physicalPath))
                                {
                                    File.Delete(physicalPath);
                                }
                            }
                            catch (Exception fileEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"DeleteSitePicture: Failed to delete file: {fileEx.Message}");
                            }
                        }

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteSitePicture: {ex.Message}");
                return false;
            }
        }

        public class FileViewModel
        {
            public int Id { get; set; }
            public string FileName { get; set; }
            public string FileType { get; set; }
            public string FileUrl { get; set; }
            public string UploadDate { get; set; }
            public string UploadedBy { get; set; }
            public long FileSize { get; set; }
            public int? AppointmentId { get; set; }
            public string Reference { get; set; }
            public string TaggedFrom { get; set; }
            public string TaggedTo { get; set; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<FileViewModel> GetSiteFiles(string customerId, int siteId)
        {
            var files = new List<FileViewModel>();

            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"GetSiteFiles called with: customerId={customerId}, siteId={siteId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId))
                {
                    System.Diagnostics.Debug.WriteLine($"GetSiteFiles: Validation failed - CompanyID: {companyid}, CustomerID: {customerId}");
                    return files;
                }

                Database db = new Database();
                DataTable dt = new DataTable();

                // Show ALL files for the customer across all sites
                // Handle CustomerID as both string and integer for compatibility
                // Note: tbl_Files table doesn't have AppointmentId, TaggedFrom, TaggedTo, UploadedFrom, UploadedTo columns
                string sql = @"
                    SELECT 
                        Id,
                        FileName,
                        FileType,
                        FileSize,
                        UploadDate,
                        Reference,
                        COALESCE(NULLIF(UploadedBy, ''), 'System') as UploadedBy,
                        SiteId,
                        AppointmentId,
                        'FSM' as TaggedFrom,
                        NULL as TaggedTo,
                        NULL as UploadedFrom,
                        NULL as UploadedTo
                    FROM [msSchedulerV3].[dbo].[tbl_Files]
                    WHERE CAST(CustomerID AS NVARCHAR(50)) = @CustomerID 
                      AND CompanyID = @CompanyID and SiteId= @SiteId
                    ORDER BY UploadDate DESC";

                // Initialize Command if needed (ExecuteParam uses db.Command.Parameters)
                if (db.Command == null)
                {
                    db.Command = new System.Data.SqlClient.SqlCommand();
                }

                // Clear any existing parameters
                db.Command.Parameters.Clear();

                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@SiteId", siteId, SqlDbType.NVarChar);
                // SiteId parameter removed - showing all customer files

                // ExecuteParam creates its own connection, so we don't need db.Open() or db.Close()
                db.ExecuteParam(sql, out dt);

                System.Diagnostics.Debug.WriteLine($"GetSiteFiles: Found {dt.Rows.Count} files for customer {customerId}");

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        files.Add(new FileViewModel
                        {
                            Id = row.Field<int>("Id"),
                            FileName = row.Field<string>("FileName") ?? "",
                            FileType = row.Field<string>("FileType") ?? "Unknown",
                            FileUrl = $"/fsm/CustomerDetails.aspx?type=file&id={row.Field<int>("Id")}",
                            UploadDate = row.Field<DateTime?>("UploadDate")?.ToString("MM/dd/yyyy HH:mm") ?? "",
                            UploadedBy = row.Field<string>("UploadedBy") ?? "System",
                            FileSize = row.Field<long?>("FileSize") ?? 0,
                            AppointmentId = row.Table.Columns.Contains("AppointmentId") && row["AppointmentId"] != DBNull.Value ? (int?)Convert.ToInt32(row["AppointmentId"]) : null,
                            Reference = row.Table.Columns.Contains("Reference") ? (row.Field<string>("Reference") ?? "") : "",
                            TaggedFrom = row.Table.Columns.Contains("TaggedFrom") ? row.Field<string>("TaggedFrom") : "FSM",
                            TaggedTo = row.Table.Columns.Contains("TaggedTo") ? row.Field<string>("TaggedTo") : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetSiteFiles: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            return files;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveSiteFile(string customerId, int siteId, string fileName, string fileType, long fileSize, string fileContent, string reference)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();
                string userId = HttpContext.Current.Session["LoginUser"]?.ToString() ?? HttpContext.Current.Session["UserID"]?.ToString() ?? HttpContext.Current.User?.Identity?.Name ?? "System";

                System.Diagnostics.Debug.WriteLine($"SaveSiteFile called with: customerId={customerId}, siteId={siteId}, fileName={fileName}, fileType={fileType}, fileSize={fileSize}, fileContent length={fileContent?.Length ?? 0}, reference={reference}, companyid={companyid}, userId={userId}");

                if (string.IsNullOrEmpty(companyid) || string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileContent))
                {
                    System.Diagnostics.Debug.WriteLine($"SaveSiteFile: Validation failed - CompanyID: {companyid}, CustomerID: {customerId}, FileName: {fileName}, FileContent length: {fileContent?.Length ?? 0}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                // Convert base64 to bytes
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(fileContent);
                    System.Diagnostics.Debug.WriteLine($"SaveSiteFile: Converted base64 to bytes, length: {fileBytes.Length}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveSiteFile: Error converting base64: {ex.Message}");
                    return false;
                }

                string sql = @"
                    INSERT INTO [msSchedulerV3].[dbo].[tbl_Files]
                    (CompanyID, CustomerID, SiteId, FileName, FileType, FileSize, FileContent, UploadDate, UploadedBy, Reference)
                    VALUES (@CompanyID, @CustomerID, @SiteId, @FileName, @FileType, @FileSize, @FileContent, GETDATE(), @UploadedBy, @Reference)";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CustomerID", System.Data.SqlDbType.NVarChar) { Value = customerId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@SiteId", System.Data.SqlDbType.Int) { Value = siteId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileName", System.Data.SqlDbType.NVarChar) { Value = fileName });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileType", System.Data.SqlDbType.NVarChar) { Value = fileType ?? "Unknown" });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileSize", System.Data.SqlDbType.BigInt) { Value = fileSize });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileContent", System.Data.SqlDbType.VarBinary) { Value = fileBytes });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@UploadedBy", System.Data.SqlDbType.NVarChar) { Value = userId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Reference", System.Data.SqlDbType.NVarChar) { Value = (object)reference ?? DBNull.Value });

                    System.Diagnostics.Debug.WriteLine($"SaveSiteFile: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"SaveSiteFile: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveSiteFile: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool DeleteSiteFile(int fileId)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"DeleteSiteFile called with: fileId={fileId}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || fileId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteSiteFile: Validation failed - CompanyID: {companyid}, FileId: {fileId}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                string sql = @"
                    DELETE FROM [msSchedulerV3].[dbo].[tbl_Files]
                    WHERE Id = @FileId AND CompanyID = @CompanyID";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileId", System.Data.SqlDbType.Int) { Value = fileId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });

                    System.Diagnostics.Debug.WriteLine($"DeleteSiteFile: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"DeleteSiteFile: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteSiteFile: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool UpdateSiteFile(int fileId, string fileName)
        {
            try
            {
                string companyid = HttpContext.Current.Session["CompanyID"]?.ToString();

                System.Diagnostics.Debug.WriteLine($"UpdateSiteFile called with: fileId={fileId}, fileName={fileName}, companyid={companyid}");

                if (string.IsNullOrEmpty(companyid) || fileId <= 0 || string.IsNullOrEmpty(fileName))
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateSiteFile: Validation failed - CompanyID: {companyid}, FileId: {fileId}, FileName: {fileName}");
                    return false;
                }

                Database db = new Database();
                string connString = db.ConnectionString;

                string sql = @"
                    UPDATE [msSchedulerV3].[dbo].[tbl_Files]
                    SET FileName = @FileName
                    WHERE Id = @FileId AND CompanyID = @CompanyID";

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandTimeout = 900;

                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileId", System.Data.SqlDbType.Int) { Value = fileId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@FileName", System.Data.SqlDbType.NVarChar) { Value = fileName });

                    System.Diagnostics.Debug.WriteLine($"UpdateSiteFile: Executing SQL with {cmd.Parameters.Count} parameters");

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine($"UpdateSiteFile: Rows affected = {rowsAffected}");

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSiteFile: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        private void DownloadFile(string fileType, int fileId, bool forceDownload = false)
        {
            try
            {
                string companyid = Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyid))
                {
                    Response.Clear();
                    Response.StatusCode = 401;
                    Response.StatusDescription = "Unauthorized";
                    Response.End();
                    return;
                }

                Database db = new Database();
                string connString = db.ConnectionString;
                byte[] fileContent = null;
                string fileName = "";
                string contentType = "application/octet-stream";

                string sql = "";
                string fileContentColumn = "FileContent";

                switch (fileType.ToLower())
                {
                    case "agreement":
                        sql = @"SELECT FileName, FileContent FROM [msSchedulerV3].[dbo].[tbl_MaintenanceAgreements] 
                                WHERE Id = @Id AND CompanyID = @CompanyID";
                        contentType = "application/pdf";
                        break;
                    case "picture":
                        sql = @"SELECT FileName, FileContent, PictureURL FROM [msSchedulerV3].[dbo].[tbl_Pictures]
                                WHERE Id = @Id AND CompanyID = @CompanyID";
                        contentType = "image/jpeg"; // Default, will be determined from file
                        break;
                    case "file":
                        sql = @"SELECT FileName, FileType, FileContent FROM [msSchedulerV3].[dbo].[tbl_Files] 
                                WHERE Id = @Id AND CompanyID = @CompanyID";
                        break;
                    default:
                        Response.Clear();
                        Response.StatusCode = 400;
                        Response.StatusDescription = "Bad Request";
                        Response.End();
                        return;
                }

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", System.Data.SqlDbType.Int) { Value = fileId });
                    cmd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@CompanyID", System.Data.SqlDbType.NVarChar) { Value = companyid });

                    conn.Open();
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileName = reader["FileName"]?.ToString() ?? "file";

                            // For pictures with PictureURL, redirect to the static file
                            if (fileType.ToLower() == "picture")
                            {
                                try
                                {
                                    string pictureUrl = reader["PictureURL"]?.ToString();
                                    if (!string.IsNullOrEmpty(pictureUrl))
                                    {
                                        string appPath = Request.ApplicationPath.TrimEnd('/');
                                        Response.Redirect(appPath + "/" + pictureUrl, true);
                                        return;
                                    }
                                }
                                catch (System.Threading.ThreadAbortException) { throw; }
                                catch { }
                            }

                            if (fileType.ToLower() == "file" && reader["FileType"] != DBNull.Value)
                            {
                                string fileTypeFromDb = reader["FileType"]?.ToString();
                                if (!string.IsNullOrEmpty(fileTypeFromDb))
                                {
                                    contentType = fileTypeFromDb;
                                }
                            }
                            else if (fileType.ToLower() == "picture")
                            {
                                // Determine content type from file extension
                                string ext = System.IO.Path.GetExtension(fileName).ToLower();
                                switch (ext)
                                {
                                    case ".jpg":
                                    case ".jpeg":
                                        contentType = "image/jpeg";
                                        break;
                                    case ".png":
                                        contentType = "image/png";
                                        break;
                                    case ".gif":
                                        contentType = "image/gif";
                                        break;
                                    case ".bmp":
                                        contentType = "image/bmp";
                                        break;
                                    default:
                                        contentType = "image/jpeg";
                                        break;
                                }
                            }

                            try
                            {
                                if (reader[fileContentColumn] != DBNull.Value)
                                {
                                    fileContent = (byte[])reader[fileContentColumn];

                                    // Fix for old records where base64 string was stored as UTF-8 bytes
                                    // instead of decoded image bytes. Detect and re-decode.
                                    if (fileType.ToLower() == "picture" && fileContent != null && fileContent.Length > 0)
                                    {
                                        try
                                        {
                                            string possibleBase64 = System.Text.Encoding.UTF8.GetString(fileContent);
                                            if (possibleBase64.StartsWith("iVBOR") || possibleBase64.StartsWith("/9j/") ||
                                                possibleBase64.StartsWith("R0lGO") || possibleBase64.StartsWith("Qk"))
                                            {
                                                fileContent = Convert.FromBase64String(possibleBase64);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                if (fileContent == null || fileContent.Length == 0)
                {
                    Response.Clear();
                   // Response.StatusCode = 404;
                    Response.StatusDescription = "Not Found";
                    Response.End();
                    return;
                }

                // Clear any existing output and headers BEFORE setting new ones
                Response.ClearHeaders();
                Response.ClearContent();
                Response.BufferOutput = true;

                // Set response headers
                Response.ContentType = contentType;
                string disposition = forceDownload ? "attachment" : "inline";
                Response.AddHeader("Content-Disposition", $"{disposition}; filename=\"{fileName}\"");
                Response.AddHeader("Content-Length", fileContent.Length.ToString());
                Response.BinaryWrite(fileContent);
                Response.Flush();
                Response.End();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Only set status if headers haven't been sent
                if (!Response.HeadersWritten)
                {
                    try
                    {
                        Response.Clear();
                        Response.StatusCode = 500;
                        Response.StatusDescription = "Internal Server Error";
                        Response.End();
                    }
                    catch
                    {
                        // If we can't set headers, just end the response
                        Response.End();
                    }
                }
                else
                {
                    Response.End();
                }
            }
        }



        [WebMethod]
        public static bool SendEmail()
        {
            try
            {
                var context = HttpContext.Current;
                var request = context.Request;

                var emailData = new EmailData
                {
                    to = request.Form["to"],
                    cc = request.Form["cc"],
                    bcc = request.Form["bcc"],
                    subject = request.Form["subject"],
                    body = request.Form["body"],
                    customerID = request.Form["customerID"]
                };

                MailMessage mailMessage = new MailMessage();
                mailMessage.To.Add(emailData.to);

                if (!string.IsNullOrEmpty(emailData.cc))
                {
                    mailMessage.CC.Add(emailData.cc);
                }
                if (!string.IsNullOrEmpty(emailData.bcc))
                {
                    mailMessage.Bcc.Add(emailData.bcc);
                }

                mailMessage.Subject = emailData.subject;
                mailMessage.Body = emailData.body;
                mailMessage.IsBodyHtml = true;

                if (request.Files.Count > 0)
                {
                    var file = request.Files[0];
                    var attachment = new Attachment(file.InputStream, file.FileName);
                    mailMessage.Attachments.Add(attachment);
                }

                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = System.Configuration.ConfigurationManager.AppSettings["SMTP"];
                smtpClient.Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Port"]);
                smtpClient.EnableSsl = true;
                string smtpAuthUser = System.Configuration.ConfigurationManager.AppSettings["SmtpAuthUser"];
                if (string.IsNullOrEmpty(smtpAuthUser)) smtpAuthUser = System.Configuration.ConfigurationManager.AppSettings["SmtpUser"];
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpAuthUser, System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"]);

                smtpClient.Send(mailMessage);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string SendCustomerEmail(string customerId, string emailTo, string emailCC, string emailBCC, string subject, string body, string attachmentFileName, string attachmentFileContent, string attachmentFileType)
        {
            try
            {
                string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
                if (string.IsNullOrEmpty(companyId))
                {
                    System.Diagnostics.Debug.WriteLine("SendCustomerEmail Error: CompanyID is missing from session.");
                    return "Error: CompanyID is missing from session.";
                }

               EmailProcessor emailProcessor;
                try
                {
                    emailProcessor = new EmailProcessor();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SendCustomerEmail Error instantiating EmailProcessor: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SendCustomerEmail Inner Exception: {ex.InnerException.Message}");
                    }
                    return $"Error initializing email sender: {ex.Message}";
                }

                List<EmailContent> emailContents = null;

                if (!string.IsNullOrEmpty(attachmentFileName) && !string.IsNullOrEmpty(attachmentFileContent))
                {
                    emailContents = new List<EmailContent>();
                    emailContents.Add(new EmailContent
                    {
                        FileName = attachmentFileName,
                        FileContent = Convert.FromBase64String(attachmentFileContent),
                        FileType = attachmentFileType,
                        FileUrl = "" // FileUrl is not used when content is embedded
                    });
                }

                string result = emailProcessor.SendHtmlFormattedEmail(
                    companyId,
                    customerId,
                    "Customer Email", // EmailType
                    subject,
                    body,
                    emailTo,
                    emailCC,
                    emailBCC,
                    emailContents
                );
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return $"Error: {ex.Message}";
            }
        }

        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]

        //public static string GetAuthVerifyUrl(string customerId, string customerName, string customerPhone)
        //{
        //    string accountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"];
        //    string cecBaseUrl = accountsUrl.Replace("AccountsXinator", "cec");
        //    string redirectRaw =
        //        $"/CEC/CustomerTextHistory.aspx?mobile={customerPhone}&name={customerName}&customerId={customerId}";

        //    string redirectUrl = HttpUtility.UrlEncode(redirectRaw);

        //    string newGuid = Guid.NewGuid().ToString();

        //    return $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
        //}
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetAuthVerifyUrl(string customerId, string customerName, string customerPhone)
        {
            try
            {

                string userId = HttpContext.Current.Session["LoginUser"] as string;
                string companyId = HttpContext.Current.Session["CompanyID"] as string;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
                    return null;


                string sessionString = $"{userId}|{companyId}";
                string newGuid = Guid.NewGuid().ToString();


                string sql = $"INSERT INTO XinatorCentral.dbo.tbl_Login (SessionGuid, SessionString) VALUES ('{newGuid}', '{sessionString}')";

                Database db = new Database();
                db.UpdateSql(sql);


                string accountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"];
                if (string.IsNullOrEmpty(accountsUrl))
                    return null;

                string cecBaseUrl = accountsUrl.Replace("AccountsXinator", "cec");


                string redirectRaw =
                    $"/CEC/CustomerTextHistory.aspx?mobile={customerPhone}&name={customerName}&customerId={customerId}";

                string redirectUrl = HttpUtility.UrlEncode(redirectRaw);


                return $"{cecBaseUrl}AuthVerify.aspx?id={newGuid}&redirect={redirectUrl}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error creating CEC SSO URL: " + ex.Message);
                return null;
            }
        }

        protected void btnSendSMS_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate session
                if (Session["CompanyID"] == null)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Session expired. Please login again.', '', 'error');", true);
                    return;
                }

                string CompanyID = Session["CompanyID"].ToString();
                string CustomerID = txtCustomerId.Value;
                string SmsBody = txtSMS.Text;
                string Mobile = txtMobile.Text;

                // Server-side validation
                if (string.IsNullOrWhiteSpace(CustomerID))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Customer ID is required.', '', 'warning');", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SmsBody))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('SMS message cannot be empty.', '', 'warning');", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(Mobile))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Phone/Mobile number is required.', '', 'warning');", true);
                    return;
                }

                CustomerID = Common.CleanInput(CustomerID);
                SmsBody = Common.CleanInput(SmsBody);
                Mobile = Common.CleanInput(Mobile);

                TwilioSMSService smsService = new TwilioSMSService();
                bool result = smsService.SendCustomerAdHocSMS(CompanyID, CustomerID, SmsBody, Mobile);

                string exception;
                if (result == true)
                {
                    // Clear the SMS text field after successful send
                    txtSMS.Text = "";
                    exception = " Swal.fire('SMS Sent Successfully', '', 'success');";
                }
                else
                {
                    exception = " Swal.fire('Something went wrong, Please try again.', '', 'warning');";
                }

                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", $" Swal.fire('Error: {ex.Message}', '', 'error');", true);
            }
        }

        protected void btnSendMMS_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate session
                if (Session["CompanyID"] == null)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Session expired. Please login again.', '', 'error');", true);
                    return;
                }

                string CompanyID = Session["CompanyID"].ToString();
                string _CustomerID = txtCustId.Value;
                string mobile = txtCustMob.Text;
                string mmsBody = txtMMSBody.Text;

                // Server-side validation
                if (string.IsNullOrWhiteSpace(_CustomerID))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Customer ID is required.', '', 'warning');", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(mmsBody))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('MMS message cannot be empty.', '', 'warning');", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Phone/Mobile number is required.', '', 'warning');", true);
                    return;
                }

                if (!fuAttachment.HasFile)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", " Swal.fire('Please select a file to attach.', '', 'warning');", true);
                    return;
                }

                mmsBody = Common.CleanInput(mmsBody);

                string _Path = "~/EmailHistoryContent/" + _CustomerID + "/";
                string strFolder = System.Web.HttpContext.Current.Server.MapPath(_Path);

                if (!Directory.Exists(strFolder))
                {
                    Directory.CreateDirectory(strFolder);
                }

                FileInfo fi = new FileInfo(fuAttachment.FileName);
                string ext = fi.Extension;

                string uniqueFileName = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() + "_" + _CustomerID + ext;
                string savedFilePath = System.IO.Path.Combine(strFolder, uniqueFileName);

                fuAttachment.SaveAs(savedFilePath);

                string baseUrl = ConfigurationManager.AppSettings["baseurl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath.TrimEnd('/');
                }
                baseUrl = baseUrl.TrimEnd('/');
                string mmsUrl = baseUrl + "/EmailHistoryContent/" + _CustomerID + "/" + uniqueFileName;
                string filePath = "EmailHistoryContent/" + _CustomerID + "/" + uniqueFileName;

                // Send MMS
                TwilioSMSService twilio = new TwilioSMSService();
                bool result = twilio.SendCustomerMMS(CompanyID, _CustomerID, mmsBody, mobile, filePath, mmsUrl);

                string response = "";
                if (result)
                {
                    // Clear the MMS fields after successful send
                    txtMMSBody.Text = "";
                    response = " Swal.fire('MMS Sent Successfully', '', 'success');";
                }
                else
                {
                    response = " Swal.fire('Something went wrong, Please try again.', '', 'warning');";
                }
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", response, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", $" Swal.fire('Error: {ex.Message}', '', 'error');", true);
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetDuration(int serviceTypeID)
        {
            var duration = "0";
            if (serviceTypeID > 0)
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database();
                try
                {
                    db.Open();
                    DataTable dt = new DataTable();
                    string sql = @"select Hour, Minute from [msSchedulerV3].[dbo].[tbl_ServiceType] where CompanyID = '" + CompanyID + "' and ServiceTypeID ='" + serviceTypeID + "';";
                    db.Execute(sql, out dt);
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
                    return duration;
                }
                finally
                {
                    db.Close();
                }
            }
            return duration;
        }
    }
}