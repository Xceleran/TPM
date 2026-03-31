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
using System.Data.SqlClient;
using FSM;
using FSM.SMSService;
using System.IO;
using FSM.Models.AppoinmentModel;

namespace TPM
{
    public partial class AppoinementList : System.Web.UI.Page
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
               // LoadData();
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
        public static string LoadAppointments(string SearchBy = "",
          string SearchFor = "",
          string SearchFrom = "",
          string SearchTo = "",
          string SearchCriteria = "",
          string SearchByWarrantyCompany = "",
         string  wantedStatus ="")
        {
            List<CustomerEntity> _List = new List<CustomerEntity>();

            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            try
            {


                if (SearchByWarrantyCompany == "0")
                {
                    SearchByWarrantyCompany = "ALL";
                }





                DateTime _SearchFrom = DateTime.Now.AddDays(-30);
                if (String.IsNullOrEmpty((SearchFrom)))
                {
                    _SearchFrom = DateTime.Now.AddDays(-60);
                }


                DateTime _SearchTo = DateTime.Now.AddDays(30);

                if (String.IsNullOrEmpty((SearchTo)))
                {
                    _SearchTo = DateTime.Now.AddDays(60);
                }


                Database db = new Database();

                DataSet dataSet = new DataSet();

                string _SpName = string.Empty;
                _SpName = "Sp_GetAppointmnetData";



                try
                {
                    String ConnString = db.ConnectionString;
                    using (SqlConnection conn = new SqlConnection(ConnString))
                    using (SqlDataAdapter adapter = new SqlDataAdapter())
                    {
                        var cmd = new SqlCommand(_SpName, conn);
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.Add("@CompanyId", System.Data.SqlDbType.NVarChar, 200).Value = companyid;
                        cmd.Parameters.Add("@SearchBy", System.Data.SqlDbType.NVarChar, 200).Value = SearchBy;
                        cmd.Parameters.Add("@SearchText", System.Data.SqlDbType.NVarChar, 200).Value = SearchFor;
                        cmd.Parameters.Add("@From", System.Data.SqlDbType.DateTime).Value = _SearchFrom;
                        cmd.Parameters.Add("@To", System.Data.SqlDbType.DateTime).Value = _SearchTo;
                        cmd.Parameters.Add("@IsWarrantyCompany", System.Data.SqlDbType.DateTime).Value = _SearchTo;
                        cmd.Parameters.Add("@Status", System.Data.SqlDbType.NVarChar).Value = wantedStatus;

                        cmd.CommandTimeout = 900;
                        adapter.SelectCommand = cmd;

                        // you don't need to open it with Fill
                        adapter.Fill(dataSet);
                    }
                }
                catch (Exception ex) { }

                foreach (DataRow dataRow in dataSet.Tables[0].Rows)
                {
                    _List.Add(new CustomerEntity
                    {
                        SiteName = dataRow["SiteName"].ToString(),
                        CustomerName = dataRow["CustomerName"].ToString(),
                        Address1 = dataRow["Address1"].ToString(),
                        Apptstatus = Convert.ToBoolean(dataRow["IsApproved"]) ? "Accept" : "Pending",
                        Email = dataRow["Email"].ToString(),
                        CustomerID = dataRow["CustomerID"].ToString(),
                        CustomerGuid = dataRow["CustomerGuid"].ToString(),
                        City = dataRow["City"].ToString(),
                        State = dataRow["State"].ToString(),
                        ZipCode = dataRow["ZipCode"].ToString(),
                        Phone = dataRow["Phone"].ToString(),
                        Notes = dataRow["Note"].ToString(),
                        SchedulingCal = dataRow["SchedulingCal"].ToString(),
                        ApptID = dataRow["ApptID"].ToString(),
                        IsApproved = Convert.ToBoolean(dataRow["IsApproved"]),
                        SiteID = dataRow["SiteID"].ToString(),
                        ApptDateTime = dataRow["ApptDateTime"].ToString(),
                        
                        Mobile = dataRow["Mobile"].ToString().ToUpper().Trim()
                    });
                }
            }
            catch (Exception ex)
            {

            }



            var _response = new
            {
                data = _List
            };

            return JsonConvert.SerializeObject(_response);

           
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Boolean ApptStatusChanged_Event(string ApptID,string ApptStatus, string CustomerID,string SiteID)
        {
            var sites = new List<CustomerSite>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                AppointmentStatus myStatus;
                Enum.TryParse(ApptStatus, out myStatus);
                string strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=1 WHERE CompanyID=@CompanyID AND ApptID=@ApptID";
                db.Open();
                switch (myStatus)
                {
                    case AppointmentStatus.Accept:
                        {
                            strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=1 WHERE CompanyID=@CompanyID AND ApptID=@ApptID";
                            break;
                        }
                    case AppointmentStatus.Confirm:
                        {
                            strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=1 WHERE CompanyID=@CompanyID AND ApptID=@ApptID";
                            break;
                        }
                    case AppointmentStatus.Cancel:
                        {
                            strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=0 WHERE CompanyID=@CompanyID AND ApptID=@ApptID";
                            break;
                        }
                    default: break;
                }


                db.Command.Parameters.Clear();
              
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@ApptID", ApptID, SqlDbType.NVarChar);
                db.Command.CommandText = strSQL;
                db.ExecuteCommand();
                db.Close();
                DataSet dataSet =  null;
                if (myStatus == AppointmentStatus.Accept)
                {

                    var _CommunicationSettings = new CommunicationSettings();
                   

                    try
                    {

                         strSQL = @"SELECT * FROM [msSchedulerV3].dbo.tbl_TPMCommunicationSettings WHERE CompanyID=@CompanyID AND messageType=@messageType;";
                        strSQL += @"SELECT [Email],[MobileNumber] FROM [msSchedulerV3].dbo.[tbl_CustomerSite] WHERE CompanyID=@CompanyID AND CustomerID=@CustomerID and Id=@SiteID;";
                        DataTable dataTable = new DataTable();

                        db.Command.Parameters.Clear();
                      
                        db.AddParameter("@messageType", "AcceptTPWorkOrder", SqlDbType.NVarChar);
                        db.AddParameter("@CustomerID", CustomerID, SqlDbType.NVarChar);
                        db.AddParameter("@SiteID", SiteID, SqlDbType.NVarChar);
                  

                       dataSet=  db.Get_DataSet(strSQL, companyid);
                        
                        dataTable = dataSet.Tables[0];
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
                       
                    }
                    finally
                    {
                        if (db.Connection.State == ConnectionState.Open) db.Close();
                    }
                    if(Convert.ToBoolean(_CommunicationSettings.smsEnabled))
                    {
                        try
                        {
                            string SiteMobileNumber = dataSet.Tables[1].Rows[0]["MobileNumber"].ToString();
                            TwilioSMSService smsService = new TwilioSMSService();
                            smsService.SendSMS(SiteMobileNumber, _CommunicationSettings.smsContent, companyid);
                        }
                        catch { }

                    }
                    if (Convert.ToBoolean(_CommunicationSettings.emailEnabled))
                    {
                        EmailProcessor emp = new EmailProcessor();

                        string SiteEmail = dataSet.Tables[1].Rows[0]["Email"].ToString();
                        emp.SendHtmlFormattedEmail(companyid, CustomerID, _CommunicationSettings.emailSubject , _CommunicationSettings.emailContent , "",
                            SiteEmail, "", "", new List<EmailContent>());

                    }



                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
                return false;
            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return true;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static Boolean SchedulingCalendarChanged_Event(string ApptID, string SchedulingEvent)
        {
            var sites = new List<CustomerSite>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                TPM.SchedulingEvent myStatus;
                Enum.TryParse(SchedulingEvent, out myStatus);
                string strSQL = @"";
                db.Open();
                switch (myStatus)
                {
                    case TPM.SchedulingEvent.FSM:
                        {
                            strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=1,SchedulingCal=@SchedulingCal WHERE CompanyID=@CompanyID AND ApptID=@ApptID";

                            break;
                        }
                    case TPM.SchedulingEvent.CEC:
                        {
                            strSQL = @"Update [msSchedulerV3].dbo.tbl_Appointment set IsApproved=1,SchedulingCal=@SchedulingCal WHERE CompanyID=@CompanyID AND ApptID=@ApptID";
                            break;
                        }
                   
                    default: break;
                }
                
                 db.Command.Parameters.Clear();
                db.AddParameter("@SchedulingCal", SchedulingEvent, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@ApptID", ApptID, SqlDbType.NVarChar);
                db.Command.CommandText = strSQL;
                db.ExecuteCommand();
                db.Close();

                if (myStatus == TPM.SchedulingEvent.FSM)
                {

                }
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
                return false;
            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return true;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerSite> Get_Message(string ApptID)
        {
            var sites = new List<CustomerSite>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                db.Open();
                string strSQL = @"SELECT isnull(Note,'') as Note FROM [msSchedulerV3].dbo.tbl_Appointment WHERE CompanyID='" + companyid + "' AND ApptID='" + ApptID + "'";
                db.Execute(strSQL, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sites.Add(new CustomerSite
                        {
                            
                            Note = dr["Note"].ToString() ?? "",
                           
                        });
                    }
                }
                
            }
            catch (Exception ex)
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
                return sites;
            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return sites;
        }
        public CustomerEntity GetCustomerDetails(string CustomerGuid, string customerId)
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
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<AppointmentModel> GetCustomerAppoinmetsForView(string customerId, int siteId)
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

                // Show ALL appointments for the customer across all sites
                // Handle CustomerID as both string and integer for compatibility
                string sql = @"SELECT CONVERT(VARCHAR(10), apt.ApptDateTime, 101) AS ApptDateTimeConverted,apt.ApptID,srv.ServiceName,  
    CASE WHEN apt.Status = 'Deleted' THEN 'N/A' WHEN sts.StatusName = 'Scheduled' THEN 'Confirmed' ELSE sts.StatusName END AS AppStatus, 
    tkt.StatusName AS AppTicketStatus
    FROM tbl_Appointment AS apt 
   -- LEFT JOIN tbl_Resources AS rsc ON apt.ResourceID = rsc.Id AND apt.CompanyID = rsc.CompanyID
    LEFT JOIN tbl_ServiceType AS srv ON apt.ServiceType = srv.ServiceTypeID AND apt.CompanyID = srv.CompanyID
    LEFT JOIN tbl_Status AS sts ON ISNULL(TRY_CAST(apt.Status AS INT), 0) = sts.StatusID AND apt.CompanyID = sts.CompanyID
    LEFT JOIN tbl_TicketStatus AS tkt ON ISNULL(TRY_CAST(apt.TicketStatus AS INT), 0)= tkt.StatusID AND apt.CompanyID = tkt.CompanyID
    WHERE CAST(apt.CustomerID AS NVARCHAR(50)) = @CustomerID 
    AND apt.CompanyID = @CompanyID   AND apt.siteId = @siteId 
    --AND (apt.SchedulingCal IS NULL OR apt.SchedulingCal != 'CEC')
    ORDER BY apt.ApptDateTime DESC;";

                //             sql = @"SELECT top 4 CONVERT(VARCHAR(10), apt.ApptDateTime, 101) AS ApptDateTimeConverted,  
                //apt.*, apt.Note, rsc.Name AS ResourceName, srv.ServiceName, 
                //CASE WHEN apt.Status = 'Deleted' THEN 'N/A' WHEN sts.StatusName = 'Scheduled' THEN 'Confirmed' ELSE sts.StatusName END AS AppStatus, 
                //tkt.StatusName AS AppTicketStatus
                //FROM tbl_Appointment AS apt 
                //LEFT JOIN tbl_Resources AS rsc ON apt.ResourceID = rsc.Id AND apt.CompanyID = rsc.CompanyID
                //LEFT JOIN tbl_ServiceType AS srv ON apt.ServiceType = srv.ServiceTypeID AND apt.CompanyID = srv.CompanyID
                //LEFT JOIN tbl_Status AS sts ON ISNULL(TRY_CAST(apt.Status AS INT), 0) = sts.StatusID AND apt.CompanyID = sts.CompanyID
                //LEFT JOIN tbl_TicketStatus AS tkt ON ISNULL(TRY_CAST(apt.TicketStatus AS INT), 0)= tkt.StatusID AND apt.CompanyID = tkt.CompanyID
                //WHERE apt.CompanyID = @CompanyID  
                //ORDER BY apt.ApptDateTime DESC;";


                db.AddParameter("@siteId", siteId, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", customerId, SqlDbType.NVarChar);
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);

                System.Diagnostics.Debug.WriteLine($"GetCustomerAppoinmets: Searching for CustomerID: '{customerId}', CompanyID: '{companyid}'");

                db.ExecuteParam(sql, out dt);
                db.Close();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            var appoinment = new AppointmentModel();
                            appoinment.AppoinmentId = row["ApptID"]?.ToString() ?? "";
                            appoinment.CustomerID = customerId;
                            appoinment.CompanyID = companyid;
                            appoinment.AppoinmentStatus = row.Field<string>("AppStatus") ?? "";
                            //   appoinment.TicketStatus = row.Field<string>("AppTicketStatus") ?? "";
                            //  appoinment.ResourceName = row.Field<string>("ResourceName") ?? "";
                            appoinment.ServiceType = row.Field<string>("ServiceName") ?? "";
                            appoinment.RequestDate = row.Field<string>("ApptDateTimeConverted") ?? "";
                            //appoinment.TimeSlot = row.Field<string>("TimeSlot") ?? "";
                            appoinment.AppoinmentDate = row.Field<string>("ApptDateTimeConverted") ?? "";
                            // appoinment.Note = row.Field<string>("Note") ?? "";



                            appoinments.Add(appoinment);
                        }
                        catch (Exception rowEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing appointment row: {rowEx.Message}");
                            // Continue with next row
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"GetCustomerAppoinmets: Found {appoinments.Count} appointments for CustomerID={customerId}, SiteID={siteId}");
            }
            catch (Exception ex)
            {
                db.Close();
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerAppoinmets: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Return empty list instead of null to prevent JavaScript errors
                return new List<AppointmentModel>();
            }
            finally
            {

            }
            return appoinments;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerSite> GetCustomerSiteData(string customerId,string SiteId)
        {
            var sites = new List<CustomerSite>();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            DataTable dt = new DataTable();
            try
            {
                db.Open();
                string strSQL = @"SELECT  * FROM [msSchedulerV3].dbo.tbl_CustomerSite WHERE CompanyID='" + companyid + "' AND id='" + SiteId 
                    + "' AND CustomerID='" + customerId + "' order by SiteName";
                db.Execute(strSQL, out dt);
                db.Close();

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
                            PhoneNumber = dr["PhoneNumber"].ToString() ?? "",
                            MobileNumber = dr["MobileNumber"].ToString() ?? "",
                            Note = dr["Note"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            CreatedDateTime = Convert.ToDateTime(dr["CreatedDateTime"])
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
                return sites;
            }
            finally
            {
                if (db.Connection.State == ConnectionState.Open) db.Close();
            }
            return sites;
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
    }
    public enum AppointmentStatus
    {
        Accept, Confirm, Cancel
    }
    public enum SchedulingEvent
    {
        FSM, CEC
    }
    
    public class MessageItem
    {
        public string a { get; set; }
        public string b { get; set; }
        public string c { get; set; }
        public string d { get; set; }
        public string e { get; set; }
        public string f { get; set; }
        public string g { get; set; }
        public string h { get; set; }


    }
}