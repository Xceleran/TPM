using FSM.Entity.Customer;
using FSM.Models.LoginModels;
using FSM.SMSService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace TPM
{
  
    public partial class TpList : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        static string connStrStatic = ConfigurationManager.AppSettings["ConnString"].ToString();

        string CompanyTag = "";
        string CompanyName = "";

        StringBuilder table = new StringBuilder();
        protected void Page_Load(object sender, EventArgs e)
        {



            if (Session["CompanyID"] == null)
            {
                Response.Redirect("logout.aspx");
            }
            if (Page.IsPostBack == false)
            {

             

                //if (Session["IsCecPro"] != null)
                //{
                //    bool IsPro = (bool)Session["IsCecPro"];
                //    div_SearchFor.Visible = IsPro;
                //    Add_new_Customer.Visible = !IsPro;
                //    Add_new_Customer_dropdown.Visible = IsPro;
                //    if (!IsPro)
                //    {
                //        SearchBy.Items.RemoveAt(2);
                //    }

                //}

            //    spn_AiremasterSync.Visible = false;


                string searchPerameter = "";
                string searchValue = "";

               
                    ddl_SearchFor.SelectedValue = "Business";
                    SearchBy.SelectedValue = "BusinessName";
               

                try
                { if (Request.Params["Type"] != null) ddl_SearchFor.SelectedValue = Request.Params["Type"].ToString(); }
                catch { }
                string selectedTags = "";
               
                    LoadBusinessontactTable(searchPerameter, searchValue, selectedTags);
                


                LoadSurvey();
                LoadTags();
              

               

            }
        }

    
       
        public void LoadBusinessontactTable(string searchPerameter, string searchValue, string selectedTags)
        {
            searchPerameter = Common.CleanInput(searchPerameter);
            searchValue = Common.CleanInput(searchValue);
            table.Append("<div><table id='example' class='table table-striped table-bordered nowrap'  style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial;width:100%' >");



            string IssearchValue = "";
            bool IsSearchForContact = false;


            string query = " ";
            string checkCompanyID = string.Empty;
            string orderby = string.Empty;


            table.Append("<thead><tr><th>...</th><th>Business Name</th><th>Title</th><th>Address</th><th>Mobile</th><th>Phone</th><th>Email</th></thead>");
            query = @"  SELECT  [CompanyID]
                              ,[BusinessID],CustomerID
                              ,[CustomerGuid]
                              ,[BusinessName]
                              ,(FirstName +' '+ LastName) as name
                              ,CustomerGuid
                              ,[Address1],Title
                              ,City,State,ZipCode
                              ,[Phone]
                              ,[Mobile]
                              ,[Email] FROM [msSchedulerV3].[dbo].[tbl_Customer] where IsBusinessContact=1 ";


            if (!string.IsNullOrEmpty(searchPerameter.Trim()) && !string.IsNullOrEmpty(searchValue.Trim()))
            {
                if (searchPerameter == "All") IssearchValue = "  and 1=1 ";
                if (searchPerameter == "BusinessName") IssearchValue = " and ( BusinessName like '" + searchValue.Trim() + "%') ";
                if (searchPerameter == "FirstName") IssearchValue = " and ( FirstName like '" + searchValue.Trim() + "%') ";
                if (searchPerameter == "LastName") IssearchValue = " and ( LastName like '" + searchValue + "%' ) ";

                if (searchPerameter == "City") IssearchValue = " and  City like '%" + searchValue.Trim() + "%' ";
                if (searchPerameter == "JobTitle") IssearchValue = " and  JobTitle like '%" + searchValue.Trim() + "%' ";
                if (searchPerameter == "Address") IssearchValue = " and concat(Address1 , ', ' , City , ', ', State , ' ' , ZipCode) like '%" + searchValue.Trim() + "%' ";
                if (searchPerameter == "Mobile") IssearchValue = " and  Mobile like '%" + searchValue.Trim() + "%' ";
                if (searchPerameter == "Phone") IssearchValue = " and  Phone like '%" + searchValue.Trim() + "%' ";
                if (searchPerameter == "Email") IssearchValue = " and  Email like '%" + searchValue.Trim() + "%' ";

            }
            // Search by multiple tags using CSLTagString
            if (!string.IsNullOrEmpty(selectedTags))
            {
                string[] tagIds = selectedTags.Split(',');

                if (tagIds.Length > 0)
                {
                    string tagCondition = " and (";

                    for (int tagIndex = 0; tagIndex < tagIds.Length; tagIndex++)
                    {
                        string tagId = tagIds[tagIndex].Trim();

                        if (!string.IsNullOrEmpty(tagId))
                        {
                            if (tagIndex > 0) tagCondition += " OR ";

                            // Check if CSLTagString contains this tag ID safely
                            tagCondition +=
                                "(CSLTagString = '" + tagId +
                                "' OR CSLTagString LIKE '" + tagId + ",%'" +
                                " OR CSLTagString LIKE '%," + tagId + ",%'" +
                                " OR CSLTagString LIKE '%," + tagId + "')";
                        }
                    }

                    tagCondition += ")";
                    IssearchValue += tagCondition;
                }
            }

            checkCompanyID = " and CompanyID= '" + Session["CompanyID"].ToString() + "' and WarrentyCompanyID is not null";
            orderby = " order by BusinessName  asc";







            query = query + checkCompanyID + IssearchValue + orderby;
            Database db = new Database(connStr);


            DataTable dt = new DataTable();




            DataSet dataSet = db.Get_DataSet(query, Session["CompanyID"].ToString());
            dt = dataSet.Tables[0];



            table.Append(" <tbody>");

            int i = 0;
            if (dt != null)
            {
                if (dt.Rows.Count > 0)

                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        i++;
                        //string sUrl = "Customer.aspx?ID=" + dr["CustomerGuid"].ToString();


                        table.Append("<tr>");
                        //table.Append(@"<td><div class='dropdown'>
                        //          <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action" + i.ToString() + @"' data-bs-toggle='dropdown' aria-expanded='false'>
                        //            <i class='fas fa-align-justify'></i>
                        //          </button>
                        //          <ul class='dropdown-menu' aria-labelledby='Action" + i.ToString() + @"'>" +
                        //              "<li><a class='dropdown-item' href='customerDetail.aspx?BusinessGuID=" + dr["CustomerGuid"].ToString() + "'>Add Link Contact</a></li>" +
                        //            "</ul></div></td>");
                        table.Append(@"<td><div class='dropdown'>
                                  <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action" + i.ToString() + @"' data-bs-toggle='dropdown' aria-expanded='false'>
                                    <i class='fas fa-align-justify'></i>
                                  </button>
                                  <ul class='dropdown-menu' aria-labelledby='Action" + i.ToString() + @"'>" +
                                     "<li><a class='dropdown-item' href='#'>Access Portal</ a></li>" +
                                       "<li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId=" + dr["CustomerGuid"].ToString() + "&InType=Invoice'>Create Invoice</a></li>" +
                                 
                                     "<li><a class='dropdown-item' href='#'>View Invoice</a></li>" +
                                     "<li><a class='dropdown-item' href='#'>View Files</a></li>" +
                                       "<li><hr class='dropdown-divider'></li>" +
                                     "<li><a class='dropdown-item' href='#'>Send E-Mail</a></li>" +
                                     "<li><a class='dropdown-item' href='#'>E-Mail History</a></li>" +
                                      "<li><a class='dropdown-item' href='#'>Send Text</a></li>" +
                                       "<li><a class='dropdown-item' href='#'>Send SMS</a></li>" +
                                       "<li><a class='dropdown-item' href='#'>Text History</a></li>" );
                                     

                        table.Append("</ul></div></td>");
                        table.Append("<td><a style='color:#526288' href='BusinessContact.aspx?id=" + dr["CustomerGuid"].ToString() + "'>" + (dr["BusinessName"].ToString()) + "</a></td>");

                        string FullAddress = string.Empty;
                        table.Append("<td>" + dr["Title"].ToString() + "</td>");

                        if (!string.IsNullOrEmpty(dr["Address1"].ToString()))
                        {
                            FullAddress += dr["Address1"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["City"].ToString()))
                        {

                            FullAddress += dr["City"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["State"].ToString()))
                        {
                            FullAddress += dr["State"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["ZipCode"].ToString()))
                        {
                            FullAddress += dr["ZipCode"].ToString();
                        }

                        if (FullAddress.EndsWith(", "))
                        {
                            FullAddress = FullAddress.Substring(0, FullAddress.Length - 2);
                        }

                        table.Append("<td>" + FullAddress + "</td>");
                        table.Append("<td><a style='color:#526288' href='tel: " + dr["Mobile"].ToString() + "'>" + dr["Mobile"].ToString() + "</a></td>");
                        table.Append("<td><a style='color:#526288' href='tel: " + dr["Phone"].ToString() + "'>" + dr["Phone"].ToString() + "</a></td>");
                        table.Append("<td><a href='mailto:" + dr["Email"].ToString() + "'>" + dr["Email"].ToString() + "</a></td>");



                        table.Append("</tr>");
                    }
                }

            }
            table.Append(" </tbody>");
            table.Append("</table></div>");
            ListTable.Controls.Add(new Literal { Text = table.ToString() });


        }

        public void LoadTable(string searchPerameter, string searchValue, string SearchFor, string selectedTags)
        {
            searchPerameter = Common.CleanInput(searchPerameter);
            searchValue = Common.CleanInput(searchValue);
            table.Append("<div><table id='example' class='table table-striped table-bordered nowrap'  style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial;width:100%' >");



            string IssearchValue = "";
            bool IsSearchForContact = false;
            if (!string.IsNullOrEmpty(SearchFor.Trim()))
            {
                if (SearchFor == "Contact") IsSearchForContact = true;
            }

            string query = " select CustomerID,CustomerGuid,BusinessID,BusinessName,FirstName + ' ' + LastName as name,jobtitle,Address1,City,State,ZipCode,Phone,Mobile,Email  from tbl_Customer where IsBusinessContact=0 ";
            string checkCompanyID = string.Empty;
            string orderby = string.Empty;


            if (IsSearchForContact)
            {
                table.Append("<thead><tr><th>...</th><th>Name</th><th>Job Title</th><th>Address</th><th>Mobile</th><th>Phone</th><th>Email</th></thead>");

                query = " select CustomerID,CustomerGuid,BusinessID,BusinessName,FirstName + ' ' + LastName as name,jobtitle,Address1,City,State,ZipCode,Phone,Mobile,Email  from tbl_Customer where IsBusinessContact=0 and BusinessID != '0' ";


                //query = @"  SELECT tbl_BusinessContact.BusinessID,tbl_BusinessContact.BusinessName,tbl_BusinessContact.BusinesGuid,tbl_Customer.CustomerID,tbl_Customer.CustomerGuid, 
                //        tbl_Customer.FirstName + ' ' +tbl_Customer.LastName as name,tbl_Customer.jobtitle,tbl_Customer.Address1 + ', ' + tbl_Customer.City + ', ' +tbl_Customer.State + ' ' +tbl_Customer.ZipCode as Address, 
                //        tbl_Customer.Phone,tbl_Customer.Mobile,tbl_Customer.Email,tbl_Customer.CreatedDateTime,tbl_Customer.IsPrimaryContact,tbl_Customer.QboId,tbl_Customer.CallPopAppId
                //        FROM [msSchedulerV3].[dbo].tbl_BusinessContact INNER JOIN [msSchedulerV3].[dbo].tbl_Customer ON tbl_BusinessContact.BusinessID = tbl_Customer.BusinessID where 1=1";


                if (!string.IsNullOrEmpty(searchPerameter.Trim()) && !string.IsNullOrEmpty(searchValue.Trim()))
                {
                    if (searchPerameter == "All") IssearchValue = "  and 1=1 ";
                    if (searchPerameter == "FirstName") IssearchValue = " and ( FirstName like '" + searchValue.Trim() + "%') ";
                    if (searchPerameter == "LastName") IssearchValue = " and ( LastName like '" + searchValue + "%' ) ";
                    if (searchPerameter == "BusinessName") IssearchValue = " and ( BusinessName like '%" + searchValue.Trim() + "%') ";
                    if (searchPerameter == "City") IssearchValue = " and  City like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "JobTitle") IssearchValue = " and  JobTitle like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Address") IssearchValue = " and concat(Address1 , ', ' , City , ', ', State , ' ' , ZipCode) like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Mobile") IssearchValue = " and  Mobile like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Phone") IssearchValue = " and  Phone like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Email") IssearchValue = " and  Email like '%" + searchValue.Trim() + "%' ";

                }
                // Search by multiple tags using CSLTagString
                if (!string.IsNullOrEmpty(selectedTags))
                {
                    string[] tagIds = selectedTags.Split(',');

                    if (tagIds.Length > 0)
                    {
                        string tagCondition = " and (";

                        for (int tagIndex = 0; tagIndex < tagIds.Length; tagIndex++)
                        {
                            string tagId = tagIds[tagIndex].Trim();

                            if (!string.IsNullOrEmpty(tagId))
                            {
                                if (tagIndex > 0) tagCondition += " OR ";

                                // Check if CSLTagString contains this tag ID safely
                                tagCondition +=
                                    "(CSLTagString = '" + tagId +
                                    "' OR CSLTagString LIKE '" + tagId + ",%'" +
                                    " OR CSLTagString LIKE '%," + tagId + ",%'" +
                                    " OR CSLTagString LIKE '%," + tagId + "')";
                            }
                        }

                        tagCondition += ")";
                        IssearchValue += tagCondition;
                    }
                }

                checkCompanyID = " and CompanyID= '" + Session["CompanyID"].ToString() + "'";
                orderby = " order by FirstName  asc;";
            }
            else
            {
                table.Append("<thead><tr><th>...</th><th>Name</th><th>Job Title</th><th>Address</th><th>Mobile</th><th>Phone</th><th>Email</th></thead>");
                if (!string.IsNullOrEmpty(searchPerameter.Trim()) && !string.IsNullOrEmpty(searchValue.Trim()))
                {
                    if (searchPerameter == "All") IssearchValue = "  and 1=1 ";
                    if (searchPerameter == "FirstName") IssearchValue = " and ( FirstName like '" + searchValue.Trim() + "%') ";
                    if (searchPerameter == "LastName") IssearchValue = " and ( LastName like '" + searchValue + "%' ) ";
                    if (searchPerameter == "BusinessName") IssearchValue = " and ( BusinessName like '%" + searchValue.Trim() + "%') ";
                    if (searchPerameter == "City") IssearchValue = " and  City like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "JobTitle") IssearchValue = " and  JobTitle like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Address") IssearchValue = " and concat(Address1 , ', ' , City , ', ', State , ' ' , ZipCode) like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Mobile") IssearchValue = " and  Mobile like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Phone") IssearchValue = " and  Phone like '%" + searchValue.Trim() + "%' ";
                    if (searchPerameter == "Email") IssearchValue = " and  Email like '%" + searchValue.Trim() + "%' ";

                }
                // Search by multiple tags using CSLTagString
                if (!string.IsNullOrEmpty(selectedTags))
                {
                    string[] tagIds = selectedTags.Split(',');

                    if (tagIds.Length > 0)
                    {
                        string tagCondition = " and (";

                        for (int tagIndex = 0; tagIndex < tagIds.Length; tagIndex++)
                        {
                            string tagId = tagIds[tagIndex].Trim();

                            if (!string.IsNullOrEmpty(tagId))
                            {
                                if (tagIndex > 0) tagCondition += " OR ";

                                // Check if CSLTagString contains this tag ID safely
                                tagCondition +=
                                    "(CSLTagString = '" + tagId +
                                    "' OR CSLTagString LIKE '" + tagId + ",%'" +
                                    " OR CSLTagString LIKE '%," + tagId + ",%'" +
                                    " OR CSLTagString LIKE '%," + tagId + "')";
                            }
                        }

                        tagCondition += ")";
                        IssearchValue += tagCondition;
                    }
                }
                checkCompanyID = " and CompanyID= '" + Session["CompanyID"].ToString() + "'";
                orderby = " order by FirstName  asc;";
            }






            query = query + checkCompanyID + IssearchValue + orderby;
            Database db = new Database(connStr);


            DataTable dt = new DataTable();




            DataSet dataSet = db.Get_DataSet(query, Session["CompanyID"].ToString());
            dt = dataSet.Tables[0];




            table.Append(" <tbody>");

            int i = 0;
            if (dt != null)
            {
                if (dt.Rows.Count > 0)

                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        i++;
                        //string sUrl = "Customer.aspx?ID=" + dr["CustomerGuid"].ToString();



                        string sUrl = "customerDetail.aspx?m=2&cid=" + dr["CustomerID"].ToString();

                        table.Append("<tr>");
                        table.Append(@"<td><div class='dropdown'>
                                  <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action" + i.ToString() + @"' data-bs-toggle='dropdown' aria-expanded='false'>
                                    <i class='fas fa-align-justify'></i>
                                  </button>
                                  <ul class='dropdown-menu' aria-labelledby='Action" + i.ToString() + @"'>" +
                                      "<li><a class='dropdown-item' href='calendar.aspx?CustomerID=" + dr["CustomerID"].ToString() + "'>Create Appointment</a></li>" +
                                      "<li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId=" + dr["CustomerGuid"].ToString() + "&InType=Invoice'>Create Invoice</a></li>" +
                                      "<li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId=" + dr["CustomerGuid"].ToString() + "&InType=Proposal'>Create Estimate</a></li>" +
                                        "<li><hr class='dropdown-divider'></li>" +
                                      "<li><a class='dropdown-item' href='Invoices.aspx?Id=" + dr["CustomerID"].ToString() + "&Type=Invoice&Form=CustomerList'>Invoices</a></li>" +
                                      "<li><a class='dropdown-item' href='Invoices.aspx?Id=" + dr["CustomerID"].ToString() + "&Type=Proposal&Form=CustomerList'>Estimates</a></li>" +
                                      "<li><a class='dropdown-item' href='View_Appointment.aspx?Id=" + dr["CustomerID"].ToString() + "&Form=CustomerList'>Appointments</a></li>" +
                                      "<li><hr class='dropdown-divider'></li>" +
                                      "<li><a class='dropdown-item' href='CustomerFiles.aspx?Id=" + dr["CustomerID"].ToString() + "'>Files</a></li>" +
                                      "<li><a class='dropdown-item' href='EmailHistory_List.aspx?Id=" + dr["CustomerID"].ToString() + "&Type=Proposal'>E-Mails</a></li>" +
                                      "<li><hr class='dropdown-divider'></li>" +
                                      "<li><a class='dropdown-item' onclick='OpenMailPopUp(\"" + dr["CustomerID"].ToString() + "\")' href='#'>Send Email</a></li>" +
                                      "<li><a class='dropdown-item' onclick='OpenSurveyMailPopUp(\"" + dr["Email"].ToString() + "\",\"" + dr["CustomerID"].ToString() + "\")' href='#'>Send Rating Email</a></li>" +
                                       "<li><hr class='dropdown-divider'></li>" +
                                        "<li><a class='dropdown-item' onclick='OpenSMSHistory(\"" + dr["Mobile"].ToString() + "\",\"" + dr["name"].ToString() + "\",\"" + dr["CustomerID"].ToString() + "\")' href='#'>SMS Chat</a></li>" +

                                      "<li><a class='dropdown-item' onclick='OpenAllHistory(\"" + dr["Mobile"].ToString() + "\",\"" + dr["name"].ToString() + "\",\"" + dr["CustomerID"].ToString() + "\")' href='#'>Text History</a></li>" +

                                       "<li><a class='dropdown-item' onclick='OpenSMSPopUp(\"" + dr["Mobile"].ToString() + "\",\"" + dr["CustomerID"].ToString() + "\")' href='#'>Send Texts</a></li>");
                        LoginObject _loginObject = (LoginObject)Session["LoginObj"];

                        if (_loginObject.IsMMSAllowed)
                        {
                            table.Append("<li><a class='dropdown-item' onclick='OpenMMSPopUp(\"" + dr["Mobile"].ToString() + "\",\"" + dr["CustomerID"].ToString() + "\")' href='#'>Send MMS</a></li>");
                        }

                        table.Append("</ul></div></td>");
                        //if (IsSearchForContact)
                        //{
                        //    table.Append("<td><a style='color:#526288' href='BusinessContact.aspx?Bid=" + dr["BusinessID"].ToString() + "'>" + (dr["BusinessName"].ToString()) + "</a></td>");
                        //}

                        table.Append("<td><a style='color:#526288' href='" + sUrl + "'>" + (dr["name"].ToString()) + "</a></td>");
                        table.Append("<td><a style='color:#526288' href='" + sUrl + "'>" + (dr["Jobtitle"].ToString()) + "</a></td>");

                        string FullAddress = string.Empty;

                        if (!string.IsNullOrEmpty(dr["Address1"].ToString()))
                        {
                            FullAddress += dr["Address1"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["City"].ToString()))
                        {

                            FullAddress += dr["City"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["State"].ToString()))
                        {
                            FullAddress += dr["State"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(dr["ZipCode"].ToString()))
                        {
                            FullAddress += dr["ZipCode"].ToString();
                        }

                        if (FullAddress.EndsWith(", "))
                        {
                            FullAddress = FullAddress.Substring(0, FullAddress.Length - 2);
                        }


                        table.Append("<td>" + FullAddress + "</td>");
                        table.Append("<td><a style='color:#526288' href='tel: " + dr["Mobile"].ToString() + "'>" + dr["Mobile"].ToString() + "</a></td>");
                        table.Append("<td><a style='color:#526288' href='tel: " + dr["Phone"].ToString() + "'>" + dr["Phone"].ToString() + "</a></td>");
                        table.Append("<td><a href='mailto:" + dr["Email"].ToString() + "'>" + dr["Email"].ToString() + "</a></td>");



                        table.Append("</tr>");
                    }
                }

            }
            table.Append(" </tbody>");
            table.Append("</table></div>");
            ListTable.Controls.Add(new Literal { Text = table.ToString() });


        }


        [WebMethod]
        public static List<CustomerEntity> GetAllCustomer()
        {
            List<CustomerEntity> _CustomerData = new List<CustomerEntity>();



            try
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());



                string Sql = "SELECT  CompanyID,CustomerGuid,CustomerID,City,State,ZipCode,title,FirstName,LastName,jobtitle,BusinessName,Address1," +
                             " Address1 + ', ' + City + ', ' + State + ' ' + ZipCode as Address," +
                             " Phone,Mobile,Email,Country FROM tbl_Customer " +
                             " where  CompanyID='" + CompanyID + "' order by FirstName asc; ";


                DataTable dt = new DataTable();
                db.Execute(Sql, out dt);



                foreach (DataRow _dr in dt.Rows)
                {
                    CustomerEntity _customer = new CustomerEntity();
                    _customer.CustomerID = _dr["CustomerID"].ToString(); 
                    _customer.Title = _dr["title"].ToString();
                    _customer.BusinessName = _dr["BusinessName"].ToString();
                    _customer.Fullname = _dr["FirstName"].ToString() + " " + _dr["LastName"].ToString();
                    _customer.FirstName = _dr["FirstName"].ToString();
                    _customer.LastName = _dr["LastName"].ToString();
                    _customer.Jobtitle = _dr["jobtitle"].ToString();
                    _customer.Address = _dr["Address"].ToString();
                    _customer.Email = _dr["Email"].ToString();
                    _customer.Phone = _dr["Phone"].ToString();
                    _customer.Mobile = _dr["Mobile"].ToString();
                    _customer.Address1 = _dr["Address1"].ToString();
                    _customer.City = _dr["City"].ToString();
                    _customer.State = _dr["State"].ToString();
                    _customer.ZipCode = _dr["ZipCode"].ToString();
                    _customer.CustomerGuid = _dr["CustomerGuid"].ToString();
                    _customer.Country = _dr["Country"].ToString();

                    _CustomerData.Add(_customer);

                }
            }
            catch { }
            return _CustomerData;
        }
        protected void Search_Click(object sender, EventArgs e)
        {
            string searchPerameter = "";
            string searchValue = "";
            string selectedTags = "";

            searchPerameter = SearchBy.SelectedValue;
            searchValue = SearchValue.Text;

            // Get multiple selected tags
            string[] selectedTagArray = Request.Form.GetValues(ddlTag.UniqueID);
            if (selectedTagArray != null && selectedTagArray.Length > 0)
            {
                selectedTags = string.Join(",", selectedTagArray);
            }

            searchPerameter = Common.CleanInput(searchPerameter);
            searchValue = Common.CleanInput(searchValue);



            if (ddl_SearchFor.SelectedValue.Trim() == "Business")
            {
                LoadBusinessontactTable(searchPerameter, searchValue, selectedTags);
            }
            else
            {
                LoadTable(searchPerameter, searchValue, ddl_SearchFor.SelectedValue.Trim(), selectedTags);
            }




        }
        #region customer standerd mail
        [WebMethod]
        public static string FillEmailModal(string CustomerID)
        {
            EmailProcessor emailProcessor = new EmailProcessor();
            EmailCommunication emailcomm = new EmailCommunication();
            emailcomm = emailProcessor.FillEmailPopUp(CustomerID);
            if (emailcomm != null)
                return JsonConvert.SerializeObject(emailcomm);


            return "0";
        }
        protected void btnSendMail_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string txtCustomerID = CustomerID.Value;
            string txtEmailBody = EmailBody.Value;
            string txtEmailSubject = _EmailSubject.Value;
            string RecepientCCEmail = _CC.Value;
            string RecepientBCCEmail = _BCC.Value;
            string RecepientToEmail = _To.Value;

            txtCustomerID = Common.CleanInput(txtCustomerID);
            txtEmailBody = Common.CleanInput(txtEmailBody);
            txtEmailSubject = Common.CleanInput(txtEmailSubject);
            RecepientCCEmail = Common.CleanInput(RecepientCCEmail);
            RecepientBCCEmail = Common.CleanInput(RecepientBCCEmail);
            RecepientToEmail = Common.CleanInput(RecepientToEmail);

            List<HttpPostedFile> files = new List<HttpPostedFile>();

            string strFileName;
            string strFilePath;
            string strFolder;

            List<EmailContent> emailContents = new List<EmailContent>();
            string _Path = "~/EmailHistoryContent/" + txtCustomerID + "/";
            strFolder = Server.MapPath(_Path);
            if (!Directory.Exists(strFolder))
            {
                Directory.CreateDirectory(strFolder);
            }

            if (file.HasFile)
            {
                int i = 0;
                foreach (HttpPostedFile f in file.PostedFiles)
                {
                    EmailContent emac = new EmailContent();
                    // strFolder = Server.MapPath("./" + _Path);
                    // Get the name of the file that is posted.
                    string fileuploaddate = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
                    strFileName = f.FileName;
                    strFileName = System.IO.Path.GetFileName(strFileName);

                    // Create the directory if it does not exist.

                    // Save the uploaded file to the server.
                    string extension = System.IO.Path.GetExtension(f.FileName);
                    string newfilename = fileuploaddate + "_" + i + "_" + strFileName;

                    f.SaveAs(strFolder + newfilename);
                    //strFilePath = strFolder + fileuploaddate+"_" + strFileName;
                    //f.SaveAs(strFilePath);
                    emac.FileName = newfilename;
                    emac.FileUrl = _Path + newfilename;
                    emailContents.Add(emac);
                    i++;
                }

            }
            EmailProcessor emailProcessor = new EmailProcessor();
            string returnMessage = emailProcessor.SendHtmlFormattedEmail(CompanyID, txtCustomerID, "Customer Email", txtEmailSubject, txtEmailBody, RecepientToEmail, RecepientCCEmail, RecepientBCCEmail, emailContents);
            string exception = " Swal.fire('" + returnMessage + "', '', 'Successfully');window.location.replace('CustomerList.aspx?m=2');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);


        }
        #endregion
        public void LoadSurvey()
        {
            string sqlT = @" Select Id, Title from tbl_Survey where CompanyID='" + Session["CompanyID"].ToString() + "'";
            DataTable dtR = new DataTable();
            Database dbR = new Database(connStr);
            dbR.Execute(sqlT, out dtR);
            dbR.Close();
            if (dtR.Rows.Count > 0)
            {
                optSurvey.DataSource = dtR;
                optSurvey.DataValueField = "Id";
                optSurvey.DataTextField = "Title";
                optSurvey.DataBind();

            }
            optSurvey.Items.Insert(0, new ListItem("Select Ratings", ""));

        }
        [WebMethod]
        public static List<string> optSurvey_Changed(string SurveyID, string CustomerID)
        {
            EmailProcessor emailProcessor = new EmailProcessor();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            List<string> surveymail = new List<string>();
            SurveyID = Common.CleanInput(SurveyID);
            //string SurveyID = optSurvey.SelectedValue;
            string sqlT = @" Select* from tbl_Survey where Id='" + SurveyID + "'  and CompanyID='" + companyid + "'";
            DataTable dtR = new DataTable();
            Database dbR = new Database(connStrStatic);
            dbR.Execute(sqlT, out dtR);
            dbR.Close();
            if (dtR.Rows.Count > 0)
            {
                DataRow dr = dtR.Rows[0];
                surveymail.Add(dr["EmailSubject"].ToString());
                string emailbody = dr["EmailBody"].ToString();

                emailbody = emailProcessor.stringRepalace(emailbody, CustomerID, companyid, "");
                surveymail.Add(emailbody);

            }
            return surveymail;
        }
        protected void lnkFollowUP_Click1(object sender, EventArgs e)
        {
            string CompanyID = Session["CompanyID"].ToString();
            string EmailSubject = txt_EmailSubject.Text;
            string EmailBody = txt_EmailBody.Text;
            string TextMessage = "";
            string EmailTo = txt_EmailTO.Text;
            string SurveyID = optSurvey.SelectedValue;
            string custid = txt_CustID.Value;

            bool success = SendSurveyMail(custid, CompanyID, EmailSubject, EmailBody, TextMessage, EmailTo, SurveyID);
            if (success)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "scr", "javascript: Swal.fire('Server/Rating mail sent successfully.'); location.href='CustomerList.aspx?m=2'", true);
                //lbl_Msg.InnerText = "Server/Rating mail sent successfully.";
            }
        }
        [WebMethod]
        public bool SendSurveyMail(string customerID, string companyid, string EmailSubject, string EmailBody, string TextMsg, string EmailTo, string SurveyID)
        {
            customerID = Common.CleanInput(customerID);
            companyid = Common.CleanInput(companyid);
            EmailSubject = Common.CleanInput(EmailSubject);
            EmailBody = Common.CleanInput(EmailBody);
            TextMsg = Common.CleanInput(TextMsg);
            EmailTo = Common.CleanInput(EmailTo);
            SurveyID = Common.CleanInput(SurveyID);

            bool SentSuccessfully = false;

            Database db = new Database(connStr);

            string customerSql = @"SELECT * FROM [tbl_Customer] WHERE (CustomerID = N'" + customerID + "' and Email='" + EmailTo + "' and CompanyID='" + companyid + "')";
            DataTable dt_appt = new DataTable();
            db.Execute(customerSql, out dt_appt);
            string Mobile = "";


            string FirstName = "";

            string LastName = "";
            string CustomerID = customerID;
            if (dt_appt.Rows.Count > 0)
            {
                DataRow ds_appt = dt_appt.Rows[0];
                //string EmailTo = ds_appt["Email"].ToString();
                Mobile = ds_appt["Mobile"].ToString();

                FirstName = ds_appt["FirstName"].ToString();

                LastName = ds_appt["LastName"].ToString();
                // string BookingID = ds_appt["BookingID"].ToString();
                CustomerID = ds_appt["CustomerID"].ToString();
                companyid = ds_appt["CompanyID"].ToString();
            }
            else
            {
                customerSql = @"SELECT * FROM [tbl_Customer] WHERE  Email='" + EmailTo + "'";
                dt_appt = new DataTable();
                db.Execute(customerSql, out dt_appt);
                if (dt_appt.Rows.Count > 0)
                {
                    DataRow ds_appt = dt_appt.Rows[0];
                    Mobile = ds_appt["Mobile"].ToString();
                    FirstName = ds_appt["FirstName"].ToString();
                    LastName = ds_appt["LastName"].ToString();
                    CustomerID = ds_appt["CustomerID"].ToString();
                    companyid = ds_appt["CompanyID"].ToString();
                }
            }

            //from survey tbl
            string SurveyEmailBody = EmailBody;
            string SurveyEmailSubject = EmailSubject;

            string SurveyText = TextMsg;
            string CompanyGUID = rtnString("select CompanyGUID from tbl_Company where CompanyID='" + companyid + "'");

            if (!string.IsNullOrEmpty(SurveyEmailBody))
            {

                string url = ConfigurationManager.AppSettings["SurveyResponse"].ToString() + CompanyGUID + "&CustomerID=" + CustomerID + "&CompanyID=" + companyid + "&Email=" + EmailTo + "&SurveyID=" + SurveyID + "";


                if (!string.IsNullOrEmpty(EmailTo))
                {
                    //HttpContext.Current.Session["CustomerID"] = CustomerID;
                    string BodyText = SurveyEmailBody;

                    //BodyText = Regex.Replace(BodyText, @"\r\n?|\n", "<br/>");

                    SurveyEmailBody = BodyText;
                    EmailProcessor emailProcessor = new EmailProcessor();
                    Session["SurveyUrl"] = url;
                    List<EmailContent> emailContents = new List<EmailContent>();
                    emailProcessor.SendHtmlFormattedEmail(companyid, customerID, "Rating", EmailSubject, SurveyEmailBody, EmailTo, "", "", emailContents);
                    SentSuccessfully = true;
                }
                if (!string.IsNullOrWhiteSpace(SurveyText) && !string.IsNullOrWhiteSpace(Mobile))
                {



                    SurveyText = SurveyText + "Please click the below link to perticipate survay..." + url;
                    //SendText

                    // SendText(Mobile, SurveyText, "Survey", companyid);
                }
            }
            return SentSuccessfully;
        }

        //public void SendText(string sMobile, string sSMSText, string sSMSType, string CompanyID)
        //{
        //    string response = "";
        //    string strSQL = "";

        //    try
        //    {
        //        // WriteLog("Sending call, PhoneNumber: " + PhoneNumber + " Company: " + CompanyID + " WO: " + WorkOrder);

        //        ASCIIEncoding encoding = new ASCIIEncoding();

        //        string JPUserID = ConfigurationManager.AppSettings["JPUser"].ToString();
        //        string JPPassword = ConfigurationManager.AppSettings["JPPassword"].ToString();
        //        string SMSID = "sms" + Guid.NewGuid().ToString().Substring(0, 8);

        //        string postData = "USER=" + JPUserID;
        //        postData += "&PASSWORD=" + JPPassword;
        //        postData += "&PHONE=" + sMobile;
        //        postData += "&MESSAGE=" + sSMSText;
        //        postData += "&UniID=" + SMSID;

        //        byte[] data = encoding.GetBytes(postData);

        //        // Prepare web request...
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://cportal.justpressone.com/WebService/SendText/index.cfm");
        //        request.Method = "POST";
        //        request.ContentType = "application/x-www-form-urlencoded";
        //        request.ContentLength = data.Length;

        //        Stream newStream = request.GetRequestStream();
        //        // Send the data.
        //        newStream.Write(data, 0, data.Length);
        //        newStream.Close();

        //        HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
        //        Stream responseStream = httpWebResponse.GetResponseStream();
        //        StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
        //        response = reader.ReadToEnd();

        //        responseStream.Close();
        //        reader.Close();

        //        strSQL = "INSERT INTO tbl_SMSLog " +
        //              "(CompanyID,SMSID,MobileNumber,SMSType,Message,Response) " +
        //              " VALUES (" +
        //             "'" + CompanyID + "'," +
        //             "'" + SMSID + "'," +
        //             "'" + sMobile + "'," +
        //             "'" + sSMSType + "'," +
        //             "'" + sSMSText + "'," +
        //             "'" + response + "')";


        //        SqlCommand command_MsgTrack = new SqlCommand(strSQL, connection);
        //        command_MsgTrack.Connection.Open();
        //        command_MsgTrack.ExecuteNonQuery();
        //        command_MsgTrack.Connection.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        //Response.Write(ex.Message);
        //    }

        //}
        public string rtnString(string sqlStr)
        {

            try
            {
                using (SqlConnection sqlCon = new SqlConnection(connStr))
                {
                    sqlCon.Open();
                    SqlCommand sqlCmd = new SqlCommand(sqlStr, sqlCon);
                    SqlDataReader sqlDr = sqlCmd.ExecuteReader();
                    while (sqlDr.Read())
                    {
                        if (sqlDr[0] != DBNull.Value)
                        {
                            return sqlDr[0].ToString();
                        }
                    }
                }
                return String.Empty;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        protected void btnSendSMS_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string CustomerID = txtCustomerId.Value;
            string SmsBody = txtSMS.Value;
            string Mobile = txtMobile.Value;

            CustomerID = Common.CleanInput(CustomerID);
            SmsBody = Common.CleanInput(SmsBody);
            Mobile = Common.CleanInput(Mobile);

            TwilioSMSService smsService = new TwilioSMSService();
            bool result = smsService.SendCustomerAdHocSMS(CompanyID, CustomerID, SmsBody, Mobile);

            string exception;
            if (result == true)
            {
                exception = " Swal.fire('SMS Sent Successfully', '', 'Success');window.location.replace('CustomerList.aspx?m=2');";
            }
            else
            {
                exception = " Swal.fire('Something went wrong, Please try again.', '', 'Warning');window.location.replace('CustomerList.aspx?m=2');";
            }

            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);


        }
        protected void btnSendMMS_Click(object sender, EventArgs e)
        {
            try
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
                string _CustomerID = txtCustId.Value;
                string mobile = txtCustMob.Value;
                string mmsBody = txtMMSBody.Value;
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
                string mmsUrl = baseUrl + "EmailHistoryContent/" + _CustomerID + "/" + uniqueFileName;
                string filePath = "EmailHistoryContent/" + _CustomerID + "/" + uniqueFileName;

                // Send MMS
                TwilioSMSService twilio = new TwilioSMSService();
              //  bool result = twilio.SendInvoiceMMS(CompanyID, _CustomerID, mmsBody, mobile, filePath, mmsUrl);
                bool result = twilio.SendCustomerMMS(CompanyID, _CustomerID, mmsBody, mobile, filePath, mmsUrl);
                string response = "";
                if (result)
                {
                    response = " Swal.fire('MMS Sent Successfully', '', 'Success'); window.location.replace('CustomerList.aspx?m=2');";
                }
                else
                {
                    response = " Swal.fire('Something went wrong, Please try again.', '', 'Warning'); window.location.replace('CustomerList.aspx?m=2');";
                }
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", response, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"Swal.fire('Error: {ex.Message}', '', 'warning'); window.location.replace('CustomerList.aspx?m=2');", true);


            }
        }
        private void LoadTags()
        {
            try
            {
                // Clear existing items first
                ddlTag.Items.Clear();

                string companyId = Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
                Database db = new Database(connStr);

                string sql = @"SELECT Id, Name 
                       FROM  [msSchedulerV3].[dbo].[Tbl_CSLTag]
                       WHERE CompanyId = '" + companyId + @"'
                       ORDER BY Name";

                DataTable dt = new DataTable();
                db.Execute(sql, out dt);
                db.Close();

                // Manually add items for HtmlSelect control
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Id"] != null && row["Name"] != null)
                        {
                            string id = row["Id"].ToString();
                            string name = row["Name"].ToString();
                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                            {
                                ddlTag.Items.Add(new ListItem(name, id));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error - you might want to add proper logging here
                System.Diagnostics.Debug.WriteLine("Error loading tags: " + ex.Message);
            }
        }
    }
}