using FSM.Entity.Customer;
using FSM.Entity.Enums;
using FSM.Models.LoginModels;
using FSM.Processors;
using FSM.SMSService;
using Newtonsoft.Json;
using SelectPdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using TPM.Entity;

namespace TPM
{
 
    public partial class TpDetail : System.Web.UI.Page
    {
        public string Job_TitleDataTable;
        private static string connstr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("logout.aspx");
            }
            //Nizam
            if (Session["RDFrom"] != null)
            {
                hdSrc.Value = Session["RDFrom"].ToString();
            }
            if (!IsPostBack)
            {

                LoginObject _loginObject = (LoginObject)Session["LoginObj"];
                btnMMS.Visible = _loginObject.IsMMSAllowed;

                div_country.Visible = Session["CompanyType"].ToString().ToLower() == "lhg";

                CreateStateProvience();
                //div_state.Visible = Session["CompanyType"].ToString().ToLower() == "lhg";
                div_province.Visible = Session["CompanyType"].ToString().ToLower() == "lhg";

                div_PrimaryContact.Visible = false;

                div_details.Visible = (bool)Session["IsAireMaster"];
                //if ((bool)HttpContext.Current.Session["IsLoggedinFromPRO"])
                //{
                //    div_details.Visible = (bool)Session["IsLoggedinFromPRO"];
                //}

                if (Request.QueryString["r"] != null)
                {
                    RetVal.Value = Request.Params["r"].ToString();
                }
                bool IsBusinessContactLoaded = false;
                if (Request.QueryString["BusinessGuID"] != null)
                {
                    string companyID = HttpContext.Current.Session["CompanyID"].ToString();

                    BusinessID.Value = Request.QueryString["BusinessGuID"].ToString();

                    BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                    BusinessContacts businessContacts = businessContactProcessor.GetBusinessContactByGuid(companyID, Request.QueryString["BusinessGuID"]);
                    if (businessContacts.CustomerID != null)
                    {
                        IsBusinessContactLoaded = true;
                        Get_BusinessContact(businessContacts.CustomerID);
                    }
                }

                if (Request.QueryString["Mode"] != null)
                {
                    hdMode.Value = Request.Params["Mode"].ToString();
                }
                CustomerID.Value = "0";
                if (Request.QueryString["cGid"] != null)
                {
                    LoadCustomerDetail(Request.QueryString["cGid"].ToString());
                }
                if (Request.QueryString["cid"] != null)
                {
                    if (Request.QueryString["cid"].ToString() != "0")
                    {
                        CustomerID.Value = Request.Params["cid"].ToString();
                        LoadCustomerDetail("");
                    }
                    else
                    {
                        CustomerID.Value = "0";
                    }
                }
                else
                {
                    if (!IsBusinessContactLoaded)
                    {
                        Get_BusinessContact("");
                    }
                }


                if (Session["IsCecPro"] != null)
                {
                    bool IsPro = (bool)Session["IsCecPro"];
                    div_Business.Visible = IsPro;
                }
                Load_Initial_Data();
                if (Session["CompanyType"].ToString() == "LHG" || Session["IsPCS"] != null && (bool)Session["IsPCS"] || Session["mXP"] != null && (bool)Session["mXP"] || Session["TEST"] != null && (bool)Session["TEST"] && CustomerID.Value != "0" || Session["Demo"] != null && (bool)Session["Demo"] && CustomerID.Value != "0")
                {
                    siteLHG.Visible = true;
                    billingAddressLHG.Visible = true;
                }
                else
                {
                    siteLHG.Visible = false;
                    billingAddressLHG.Visible = false;
                }
            }
            else
            {
                // On postback, ensure tags are loaded
                LoadTags();
                // Refresh selectpicker after postback
                ScriptManager.RegisterStartupScript(this, this.GetType(), "RefreshSelectPickerPostBack", "setTimeout(function() { if (typeof initializeSelectPicker === 'function') { initializeSelectPicker(); } }, 500);", true);
            }
        }

        [WebMethod]
        public static void DeleteCustomer(string CustomerID)
        {
            Database db = new Database(connstr);
            DataTable dt = new DataTable();
            string Sql = "";
            string customerID = Common.CleanInput(CustomerID);
            string companyID = HttpContext.Current.Session["CompanyID"].ToString();
            string customerGuid = db.ExecuteScalar("Select CustomerGuid from msSchedulerV3.dbo.tbl_Customer where CompanyID='" + companyID + "' and CustomerID='" + customerID + "';  ");
            Sql = "Delete from msSchedulerV3.dbo.tbl_Customer where CompanyID='" + companyID + "' and CustomerID ='" + customerID + "'";
            db.Execute(Sql);
            Sql = "Update myServiceJobs.dbo.Customers set IsDeleted= 1 where CompanyID='" + companyID + "' and Id ='" + customerGuid + "'";
            db.Execute(Sql);
            //Response.Redirect("CustomerList.aspx?m=2");
            //  ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "window.location.href='CustomerList.aspx?m=2'", true);
        }

        public void LoadCustomerDetail(string CustomerGuid)
        {
            // Ensure tags are loaded before setting selected value
            if (ddlTag.Items.Count == 0)
            {
                LoadTags();
            }

            CustomerProcessor customerProcessor = new CustomerProcessor();

            string customerid = CustomerID.Value;
            string companyid = Session["CompanyID"].ToString();
            //CustomerEntity customerEntity = customerProcessor.GetCustomerByid(CustomerID.Value, companyid);
            //if (customerEntity != null)
            //{
            //    customerid = customerEntity.CustomerID;
            //    CustomerID.Value = customerEntity.CustomerID;
            //}
            string Sql = "SELECT CompanyID,CustomerID,FirstName,LastName,CustomerGuid,Title,CompanyName,JobTitle,IsPrimaryContact,Notes,Country," +
                "Address1,City,State,ZipCode,Phone,Mobile,Email,BusinessID,CSLTagId,CSLTagString, " +
                "FORMAT( (select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.dbo.tbl_invoice where Type='Invoice' and CompnyID='" + companyid + "' and CustomerID='" + customerid + "'), 'N2') as TotalDueForInvoice," +
                "FORMAT((select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.dbo.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + companyid + "' and CustomerID='" + customerid + "'), 'N2') as TotalDueForEstimate," +
                "(select count(Type) from msSchedulerV3.dbo.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + companyid + "' and CustomerID='" + customerid + "') as TotalEstimate," +
                "(select count(Type) from msSchedulerV3.dbo.tbl_invoice where Type='Invoice' and CompnyID='" + companyid + "' and CustomerID='" + customerid + "') as TotalInvoice," +
                "(select count(CompanyID) from msSchedulerV3.dbo.tbl_Appointment where CompanyID='" + companyid + "' and CustomerID='" + customerid + "') as TotalAppoinment " +
                " FROM msSchedulerV3.dbo.tbl_Customer ";
            if (string.IsNullOrEmpty(CustomerGuid))
            {
                Sql += " where CompanyID='" + companyid + "' and CustomerID = '" + customerid + "'";
            }
            else
            {
                Sql += " where CompanyID='" + companyid + "' and CustomerGuid = '" + CustomerGuid + "'";
            }

            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            // db.Open();
            db.Execute(Sql, out dt);
            db.Close();
            string busnessID = "0";
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    busnessID = dr["BusinessID"].ToString();
                    fname.Value = dr["FirstName"].ToString();
                    lname.Value = dr["LastName"].ToString();
                    txt_title.Text = dr["title"].ToString();
                    txt_JobTitle.Text = dr["JobTitle"].ToString();
                    address1.Value = dr["Address1"].ToString();
                    city.Value = dr["City"].ToString();
                    state.Value = dr["State"].ToString();
                    zip.Value = dr["ZipCode"].ToString();
                    phone.Value = dr["Phone"].ToString();
                    mobile.Value = dr["Mobile"].ToString();
                    email.Value = dr["Email"].ToString();
                    TotalDueForInvoice.Value = dr["TotalDueForInvoice"].ToString();
                    TotalDueForEstimate.Value = dr["TotalDueForEstimate"].ToString();
                    TotalEstimate.InnerText = dr["TotalEstimate"].ToString();
                    TotalInvoice.InnerText = dr["TotalInvoice"].ToString();
                    TotalAppoinment.Value = dr["TotalAppoinment"].ToString();
                    txt_notes.Value = dr["Notes"].ToString();
                    txt_CompanyName.Text = dr["CompanyName"].ToString();
                    CustomerID.Value = dr["CustomerID"].ToString();
                    hdCustomerGUID.Value = dr["CustomerGuid"].ToString();
                    country.Value = dr["Country"].ToString().Trim();
                    string tagString = dr["CSLTagString"].ToString();   // ex: "1,3,7"

                    // First, clear all selections
                    foreach (ListItem item in ddlTag.Items)
                    {
                        item.Selected = false;
                    }

                    // Then, set selected tags from database
                    if (!string.IsNullOrEmpty(tagString))
                    {
                        string[] selectedTags = tagString.Split(',');

                        foreach (string tagId in selectedTags)
                        {
                            string trimmedTagId = tagId.Trim();
                            if (!string.IsNullOrEmpty(trimmedTagId))
                            {
                                ListItem item = ddlTag.Items.FindByValue(trimmedTagId);
                                if (item != null)
                                {
                                    item.Selected = true;
                                }
                            }
                        }
                    }
                    // Load CSLTagId and set dropdown
                    //if (dr["CSLTagId"] != DBNull.Value && !string.IsNullOrEmpty(dr["CSLTagId"].ToString()))
                    //{
                    //    string tagId = dr["CSLTagId"].ToString();
                    //    // Check if the tag exists in the dropdown before setting
                    //    if (ddlTag.Items.FindByValue(tagId) != null)
                    //    {
                    //        ddlTag.SelectedValue = tagId;
                    //    }
                    //    else
                    //    {
                    //        ddlTag.SelectedValue = "0";
                    //    }
                    //}
                    //else
                    //{
                    //    ddlTag.SelectedValue = "0";
                    //}

                    province.Value = dr["State"].ToString(); // Load province if Canada
                    // Adjust visibility based on country for lhg
                    if (Session["CompanyType"]?.ToString().ToLower() == "lhg")
                    {
                        div_state.Visible = true;
                        div_province.Visible = true;

                        if (dr["Country"].ToString() == "Canada")
                        {
                            //div_state.Visible = false;
                            //div_province.Visible = true;
                            lbl_province.InnerText = "Province";
                            lb_zip.InnerText = "Postal Code";
                        }
                        else
                        {
                            //div_state.Visible = true;
                            //div_province.Visible = false;
                            lbl_state.InnerText = "State";
                            lb_zip.InnerText = "Zip Code";
                        }
                    }
                    if (Convert.ToBoolean(dr["IsPrimaryContact"].ToString()))
                    {
                        if (dr["BusinessID"].ToString() != "0")
                        {
                            div_PrimaryContact.Visible = true;
                            btn_delete.Visible = false;
                        }
                    }
                    else
                    {
                        div_PrimaryContact.Visible = false;
                    }
                }
            }
            Get_BusinessContact(busnessID);
        }

        public bool SaveCustomer()
        {
            try
            {
                CustomerProcessor customerProcessor = new CustomerProcessor();
                CustomerEntity customerEntity = new CustomerEntity();

                if (!string.IsNullOrEmpty(BusinessID.Value))
                {
                    BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                    var businessContact = businessContactProcessor.GetBusinessContactByGuid(Session["CompanyID"].ToString(), BusinessID.Value);
                    customerEntity.BusinessName = businessContact.BusinessName;
                    customerEntity.BusinessID = Convert.ToInt32(businessContact.CustomerID);
                }
                else
                {
                    customerEntity.BusinessID = Convert.ToInt32(se_Business.Value);
                    customerEntity.BusinessName = "";
                }

                string customerGuid = Guid.NewGuid().ToString().ToUpper();

                customerEntity.CompanyID = Session["CompanyID"].ToString();
                customerEntity.FirstName = fname.Value.ToString();
                customerEntity.LastName = lname.Value.ToString();
                customerEntity.Title = txt_title.Text.ToString();
                customerEntity.JobTitle = txt_JobTitle.Text.ToString();
                customerEntity.Address1 = address1.Value.ToString();
                customerEntity.City = city.Value.ToString();
                customerEntity.State = Session["CompanyType"]?.ToString().ToLower() == "lhg" && country.Value == "Canada" ? province.Value : state.Value;
                customerEntity.ZipCode = zip.Value.ToString();
                customerEntity.Phone = phone.Value.ToString();
                customerEntity.Mobile = mobile.Value.ToString();
                customerEntity.Email = email.Value.ToString();
                customerEntity.CustomerGuid = customerGuid;
                customerEntity.Notes = txt_notes.Value.ToString();
                customerEntity.CompanyName = txt_CompanyName.Text.ToString();
                customerEntity.BusinessName = txt_CompanyName.Text.ToString();
                customerEntity.Country = country.Value.ToString();

                // Set CSLTagId from dropdown
                //if (!string.IsNullOrEmpty(ddlTag.SelectedValue) && ddlTag.SelectedValue != "0")
                //{
                //    customerEntity.CSLTagId = Convert.ToInt32(ddlTag.SelectedValue);
                //}
                //else
                //{
                //    customerEntity.CSLTagId = null;
                //}
                string tagsString = "";
                string[] selectedTags = Request.Form.GetValues(ddlTag.UniqueID);

                if (selectedTags != null && selectedTags.Length > 0)
                {
                    tagsString = string.Join(",", selectedTags);
                }
                customerEntity.CSLTagString = tagsString;
                if (customerProcessor.Add_Customer(customerEntity))
                {
                    Database db = new Database(connstr);

                    string companyID = Session["CompanyID"].ToString();
                    string FirstName = Common.CleanInput(fname.Value.ToString());
                    string LastName = Common.CleanInput(lname.Value.ToString());
                    string Address = Common.CleanInput(address1.Value.ToString());
                    string City = Common.CleanInput(city.Value.ToString());
                    string State = Common.CleanInput(Session["CompanyType"]?.ToString().ToLower() == "lhg" && country.Value == "Canada" ? province.Value : state.Value);
                    string ZipCode = Common.CleanInput(zip.Value.ToString());
                    string Phone = Common.CleanInput(phone.Value.ToString());
                    string Mobile = Common.CleanInput(mobile.Value.ToString());
                    string Email = Common.CleanInput(email.Value.ToString());

                    string dtsnow = "";
                    dtsnow = DateTime.Now.ToString("yy-MM-dd");

                    string timevalue = "23" + ":" + "59" + ":" + "59";

                    string dtsvalue = dtsnow + " " + timevalue;

                    string Sql = "INSERT INTO myServiceJobs.dbo.Customers " +
                            "(CompanyId,[Notes],[Address2],Id,Name,Address1,City,State,Zip,PhoneWork,Mobile,[IsDeleted],CountryCode,CreatedDate,ModifiedDate,Email) " +
                            " VALUES (" +
                            "'" + companyID + "'," +
                            "''," +
                            "''," +
                            "'" + customerGuid + "'," +
                            "'" + FirstName + " " + LastName + "'," +
                            "'" + Address + "'," +
                            "'" + City + "'," +
                            "'" + State + "'," +
                            "'" + ZipCode + "'," +
                            "'" + Phone + "'," +
                            "'" + Mobile + "'," +
                        "'" + "0" + "'," +
                        "'" + (country.Value.Trim() == "Canada" ? "CA" : "US") + "'," +
                        "SYSDATETIMEOFFSET()," +
                        "SYSDATETIMEOFFSET()," +
                        "'" + Email + "')";
                    //db.Execute(Sql);
                    //db.Close();
                }
            }
            catch { return false; }
            return true;
        }

        public void Set_JobTitle()
        {
            try
            {
                List<string> Emp = new List<string>();
                string companyID = HttpContext.Current.Session["CompanyID"].ToString();
                string query = string.Format("select distinct [JobTitle] FROM [msSchedulerV3].[dbo].[tbl_Customer] where  CompanyID='" + companyID + "'");

                Database db = new Database(connstr);
                DataTable dt = new DataTable();
                db.Execute(query, out dt);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        Emp.Add(dr["JobTitle"].ToString());
                    }
                }
                System.Web.Script.Serialization.JavaScriptSerializer oSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                Job_TitleDataTable = oSerializer.Serialize(Emp);
            }
            catch { }
        }

        public void UpdateCustomer()
        {
            CustomerProcessor customerProcessor = new CustomerProcessor();
            CustomerEntity customerEntity = new CustomerEntity();

            if (!string.IsNullOrEmpty(BusinessID.Value))
            {
                BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                var businessContact = businessContactProcessor.GetBusinessContactByGuid(Session["CompanyID"].ToString(), BusinessID.Value);
                customerEntity.BusinessName = businessContact.BusinessName;
                customerEntity.BusinessID = Convert.ToInt32(businessContact.BusinessID);
            }
            else
            {
                customerEntity.BusinessID = Convert.ToInt32(se_Business.Value);
                customerEntity.BusinessName = "";
            }

            customerEntity.CompanyID = Session["CompanyID"].ToString();
            customerEntity.CustomerID = CustomerID.Value;
            customerEntity.FirstName = fname.Value.ToString();
            customerEntity.LastName = lname.Value.ToString();
            customerEntity.Title = txt_title.Text.ToString();
            customerEntity.JobTitle = txt_JobTitle.Text.ToString();
            customerEntity.Address1 = address1.Value.ToString();
            customerEntity.City = city.Value.ToString();
            customerEntity.Country = Session["CompanyType"].ToString().ToLower() == "lhg" ? country.Value.Trim() : "USA";
            customerEntity.State = Session["CompanyType"].ToString().ToLower() == "lhg" && country.Value.Trim().Equals("Canada") ? province.Value.Trim() : state.Value.Trim();
            customerEntity.ZipCode = zip.Value.ToString();
            customerEntity.Phone = phone.Value.ToString();
            customerEntity.Mobile = mobile.Value.ToString();
            customerEntity.Email = email.Value.ToString();
            customerEntity.Notes = txt_notes.Value.ToString();
            customerEntity.CompanyName = txt_CompanyName.Text.ToString();
            //customerEntity.CustomerGuid = customerGuid;

            //// Set CSLTagId from dropdown
            //if (!string.IsNullOrEmpty(ddlTag.SelectedValue) && ddlTag.SelectedValue != "0")
            //{
            //    customerEntity.CSLTagId = Convert.ToInt32(ddlTag.SelectedValue);
            //}
            //else
            //{
            //    customerEntity.CSLTagId = null;
            //}

            string tagsString = "";
            string[] selectedTags = Request.Form.GetValues(ddlTag.UniqueID);

            if (selectedTags != null && selectedTags.Length > 0)
            {
                tagsString = string.Join(",", selectedTags);
            }
            customerEntity.CSLTagString = tagsString;
            customerProcessor.Update_Customer(customerEntity);

            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "ShowSuccessMsgForCustomer();", true);
        }

        protected void btn_Save_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CustomerID.Value))
            {
                CustomerID.Value = "0";
            }
            if (CustomerID.Value == "0")
            {
                if (SaveCustomer())
                {
                    // Reload tags after save
                    LoadTags();
                    if (!string.IsNullOrEmpty(BusinessID.Value))
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "ShowSuccessMsgForLinkedCustomer(); initializeSelectPicker();", true);
                    }
                    else
                    {
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "ShowSuccessMsgForCustomer(); initializeSelectPicker();", true);
                    }
                }
            }
            else
            {
                UpdateCustomer();
                // Reload tags after update
                LoadTags();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "RefreshSelectPicker", "initializeSelectPicker();", true);
            }
        }

     

        private void CreateStateProvience()
        {
            // Populate province dropdown for Canada
            province.Items.Clear();
            province.Items.Add(new ListItem("Select Province", "select"));
            CommonClass commonClass = new CommonClass();
            IEnumerable<States> provinces = commonClass.ListOfProvience();
            foreach (States _province in provinces)
            {
                province.Items.Add(new ListItem(_province.Name, _province.Abbreviations));
            }

            // Adjust state/province labels based on company type and country
            if (Session["CompanyType"]?.ToString().ToLower() == "lhg")
            {
                // Default to USA (show state, hide province)
                //div_state.Visible = true;
                //div_province.Visible = false;
                lbl_state.InnerText = "State";
                lb_zip.InnerText = "Zip Code";
            }
            else
            {
                // Non-lhg companies: use existing logic
                LoginObject loginObject = (LoginObject)HttpContext.Current.Session["LoginObj"];
                if (loginObject.Addresstype == AddressType.Canada)
                {
                    state.Items.Clear();
                    lbl_state.InnerText = "Province";
                    lb_zip.InnerText = "Postal Code";
                    state.Items.Add(new ListItem("Select Province", ""));
                    foreach (States _state in provinces)
                    {
                        state.Items.Add(new ListItem(_state.Name, _state.Abbreviations));
                    }
                    //div_state.Visible = true;
                    //div_province.Visible = false;
                }
                else
                {
                    //div_state.Visible = true;
                    //div_province.Visible = false;
                    lbl_state.InnerText = "State";
                    lb_zip.InnerText = "Zip Code";
                }
            }
        }

        private void Get_BusinessContact(string busnessID)
        {
            BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
            string companyid = Session["CompanyID"].ToString();
            List<BusinessContacts> businessContacts = businessContactProcessor.GetAllBusinessContactsByCompany(companyid);

            foreach (BusinessContacts businessContact in businessContacts)
            {
                se_Business.Items.Add(new ListItem(businessContact.BusinessName, businessContact.CustomerID));
            }

            se_Business.Value = busnessID;
        }

        private void Load_Initial_Data()
        {
            try
            {
                string companyID = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(connStr);

                string sql = @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_SalesRep] WHERE CompanyID=@CompanyID;";
                sql += @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_LeadSource] WHERE CompanyID=@CompanyID;";
                sql += @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_LeadType] WHERE CompanyID=@CompanyID;";
                sql += @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_SalesStatus] WHERE CompanyID=@CompanyID;";
                sql += @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_ProjectType] WHERE CompanyID=@CompanyID;";
                sql += @"SELECT [ID], [Title] FROM [msSchedulerV3].[dbo].[tbl_ProjectStatus] WHERE CompanyID=@CompanyID;";

                DataSet dataSet = db.Get_DataSet(sql, companyID);

                // SalesRep
                SalesRep.DataSource = dataSet.Tables[0];
                SalesRep.DataBind();
                SalesRep.DataTextField = "Title";
                SalesRep.DataValueField = "ID";
                SalesRep.DataBind();
                SalesRep.Items.Insert(0, new ListItem("Select", "0"));

                // tbl_LeadSource
                leadSourceStatusDropdown.DataSource = dataSet.Tables[1];
                leadSourceStatusDropdown.DataBind();
                leadSourceStatusDropdown.DataTextField = "Title";
                leadSourceStatusDropdown.DataValueField = "ID";
                leadSourceStatusDropdown.DataBind();
                leadSourceStatusDropdown.Items.Insert(0, new ListItem("Select", "0"));

                // leadType
                leadTypeDropdown.DataSource = dataSet.Tables[2];
                leadTypeDropdown.DataBind();
                leadTypeDropdown.DataTextField = "Title";
                leadTypeDropdown.DataValueField = "ID";
                leadTypeDropdown.DataBind();
                leadTypeDropdown.Items.Insert(0, new ListItem("Select", "0"));

                // SalesStatus
                salesStatusDropdown.DataSource = dataSet.Tables[3];
                salesStatusDropdown.DataBind();
                salesStatusDropdown.DataTextField = "Title";
                salesStatusDropdown.DataValueField = "ID";
                salesStatusDropdown.DataBind();
                salesStatusDropdown.Items.Insert(0, new ListItem("Select", "0"));

                // projectType
                projectTypeDropdown.DataSource = dataSet.Tables[4];
                projectTypeDropdown.DataBind();
                projectTypeDropdown.DataTextField = "Title";
                projectTypeDropdown.DataValueField = "ID";
                projectTypeDropdown.DataBind();
                projectTypeDropdown.Items.Insert(0, new ListItem("Select", "0"));

                // tbl_ProjectStatus
                projectStatus.DataSource = dataSet.Tables[5];
                projectStatus.DataBind();
                projectStatus.DataTextField = "Title";
                projectStatus.DataValueField = "ID";
                projectStatus.DataBind();
                projectStatus.Items.Insert(0, new ListItem("Select", "0"));

                // Load Tags
                LoadTags();
            }
            catch { }
        }

        private void LoadTags()
        {
            try
            {
                // Preserve selected tag IDs before clearing
                List<string> selectedTagIds = new List<string>();
                foreach (ListItem item in ddlTag.Items)
                {
                    if (item.Selected)
                    {
                        selectedTagIds.Add(item.Value);
                    }
                }

                // Clear existing items first
                ddlTag.Items.Clear();

                string companyId = Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
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
                                ListItem listItem = new ListItem(name, id);
                                // Restore selected state if this tag was previously selected
                                if (selectedTagIds.Contains(id))
                                {
                                    listItem.Selected = true;
                                }
                                ddlTag.Items.Add(listItem);
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

        [Obsolete]
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
                    // strFolder = Server.MapPath("./" + _Path); Get the name of the file that is posted.
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
            //string exception = " Swal.fire('" + returnMessage + "', '', 'Successfully');";
            //ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);

            string exception = " Swal.fire('Email sent successfully.', '', 'Successfully');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
        }

        #endregion customer standerd mail

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomerSiteData(CustomerSite site)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            if (!string.IsNullOrEmpty(site.CustomerID))
            {
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
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string strSQL = @"INSERT INTO [msSchedulerV3].dbo.tbl_CustomerSite
                        (CompanyID, CustomerID, CustomerGuid, SiteName, Address, Contact,Email,PhoneNumber, Note, IsActive,FirstName,LastName,Country,State,Zip) output INSERTED.ID
                        VALUES (@CompanyID, @CustomerID, @CustomerGuid, @SiteName, @Address, @Contact,@Email,@PhoneNumber, @Note, @IsActive,@FirstName,@LastName,@Country,@State,@Zip)";
                db.AddParameter("@CompanyID", companyid, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", site.CustomerID, SqlDbType.NVarChar);
                db.AddParameter("@CustomerGuid", site.CustomerGuid, SqlDbType.NVarChar);
                db.AddParameter("@SiteName", site.SiteName, SqlDbType.NVarChar);
                db.AddParameter("@Address", site.Address, SqlDbType.NVarChar);
                db.AddParameter("@Contact", site.Contact, SqlDbType.NVarChar);
                db.AddParameter("@Email", site.Email, SqlDbType.NVarChar);
                db.AddParameter("@PhoneNumber", site.PhoneNumber, SqlDbType.NVarChar);
                db.AddParameter("@Note", site.Note, SqlDbType.NVarChar);
                db.AddParameter("@IsActive", site.IsActive, SqlDbType.Bit);
                db.AddParameter("@FirstName", site.FirstName, SqlDbType.NVarChar);
                db.AddParameter("@LastName", site.LastName, SqlDbType.NVarChar);
                db.AddParameter("@Country", site.Country, SqlDbType.NVarChar);
                db.AddParameter("@State", site.State, SqlDbType.NVarChar);
                db.AddParameter("@Zip", site.Zip, SqlDbType.NVarChar);

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
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connStr);
            try
            {
                db.Open();
                string strSQL = @"UPDATE [msSchedulerV3].dbo.tbl_CustomerSite SET 
                                    SiteName = @SiteName,
                                    Address = @Address,
                                    Contact = @Contact,
                                    Email=@Email,
                                    PhoneNumber=@PhoneNumber,
                                    Note = @Note,
                                    FirstName =@FirstName,
                                    LastName = @LastName,
                                    Country = @Country,
                                    State = @State,
                                    Zip = @Zip,
                                    IsActive = @IsActive WHERE Id=@Id and CustomerID = @CustomerID";
                db.AddParameter("@SiteName", site.SiteName, SqlDbType.NVarChar);
                db.AddParameter("@CustomerID", site.CustomerID, SqlDbType.NVarChar);
                db.AddParameter("@Address", site.Address, SqlDbType.NVarChar);
                db.AddParameter("@Contact", site.Contact, SqlDbType.NVarChar);
                db.AddParameter("@Email", site.Email, SqlDbType.NVarChar);
                db.AddParameter("@PhoneNumber", site.PhoneNumber, SqlDbType.NVarChar);
                db.AddParameter("@Note", site.Note, SqlDbType.NVarChar);
                db.AddParameter("@FirstName", site.FirstName, SqlDbType.NVarChar);
                db.AddParameter("@LastName", site.LastName, SqlDbType.NVarChar);
                db.AddParameter("@Country", site.Country, SqlDbType.NVarChar);
                db.AddParameter("@State", site.State, SqlDbType.NVarChar);
                db.AddParameter("@Zip", site.Zip, SqlDbType.NVarChar);
                db.AddParameter("@IsActive", site.IsActive, SqlDbType.Bit);
                db.AddParameter("@Id", site.Id, SqlDbType.Int);
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
            return true;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerSite> GetCustomerSiteData(string customerID)
        {
            try
            {
                string errorMsg = string.Empty;
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(connStr);

                string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerSite] where CustomerID='" + customerID + "' and CompanyID='" + companyid + "'";

                DataTable dt = new DataTable(connStr);
                db.Execute(sqlAppDtl, out dt);
                List<CustomerSite> returnList = new List<CustomerSite>();
                returnList = ConvertDataTable<CustomerSite>(dt);
                return returnList;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerSite> GetCustomerSiteDataBySiteId(string siteId)
        {
            try
            {
                string errorMsg = string.Empty;
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(connStr);

                string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerSite] where Id='" + siteId + "' and CompanyID='" + companyid + "'";

                DataTable dt = new DataTable(connStr);
                db.Execute(sqlAppDtl, out dt);
                List<CustomerSite> returnList = new List<CustomerSite>();
                returnList = ConvertDataTable<CustomerSite>(dt);
                return returnList;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerBillingAddress> GetCustomerBillAddressDataByBillAddressId(string billAddressId)
        {
            try
            {
                string errorMsg = string.Empty;
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(connStr);

                string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerBillingAddress] where ID='" + billAddressId + "'";

                DataTable dt = new DataTable(connStr);
                db.Execute(sqlAppDtl, out dt);
                List<CustomerBillingAddress> returnList = new List<CustomerBillingAddress>();
                returnList = ConvertDataTable<CustomerBillingAddress>(dt);
                return returnList;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }



        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static bool SaveCustomerBillingAddress(CustomerBillingAddress billingAddressInfo)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            if (billingAddressInfo.Customer_ID != Guid.Empty)
            {
                if (billingAddressInfo.ID != Guid.Empty)
                {
                    return UpdateCustomerBillingAddressInfo(billingAddressInfo);
                }
                else
                {
                    return InsertCustomerBillingAddressInfo(billingAddressInfo);
                }
            }

            return false;
        }
        public static bool InsertCustomerBillingAddressInfo(CustomerBillingAddress billingAddressInfo)
        {
            bool success = false;
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connStr);
            SqlConnection Conn = new SqlConnection(connStr);
            try
            {
                string billAdd_Guid = Guid.NewGuid().ToString();
                Hashtable ht = new Hashtable();
                ht.Add("ID", billAdd_Guid);
                ht.Add("Address", billingAddressInfo.Address);
                ht.Add("City", billingAddressInfo.City);
                ht.Add("ZipCode", billingAddressInfo.ZipCode);
                ht.Add("State", billingAddressInfo.State);
                ht.Add("Customer_ID", billingAddressInfo.Customer_ID);
                ht.Add("isActive", billingAddressInfo.IsActive);

                db.InsertByCommand(ht, "tbl_CustomerBillingAddress", Conn);
                db.CommitTransaction();
                success = true;
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

        public static bool UpdateCustomerBillingAddressInfo(CustomerBillingAddress billingAddressInfo)
        {
            bool success = false;
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connStr);
            SqlConnection Conn = new SqlConnection(connStr);
            try
            {
                Hashtable ht = new Hashtable();

                db.BeginTransaction();

                ht.Add("ID", billingAddressInfo.ID);
                ht.Add("Address", billingAddressInfo.Address);
                ht.Add("City", billingAddressInfo.City);
                ht.Add("ZipCode", billingAddressInfo.ZipCode);
                ht.Add("State", billingAddressInfo.State);
                ht.Add("Customer_ID", billingAddressInfo.Customer_ID);
                ht.Add("isActive", billingAddressInfo.IsActive);
                Conn.Open();
                db.UpdateByCommand(ht, "tbl_CustomerBillingAddress", "ID='" + billingAddressInfo.ID + "'", Conn);
                Conn.Close();
                db.CommitTransaction();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }
            finally
            {
                db.Close();
            }
            return true;
        }

        //Data Table to Json List
        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    T item = GetItem<T>(row);
                    data.Add(item);
                }
            }
            catch (Exception ex)
            {

            }
            return data;
        }

        public static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name.ToLower() == column.ColumnName.ToLower())
                    {
                        var newValue = dr[column.ColumnName].ToString();
                        if (!string.IsNullOrEmpty(newValue))
                        {
                            Type propertyType = pro.PropertyType;
                            Type realPropertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                            object newPropertyValue = Convert.ChangeType(dr[column.ColumnName], realPropertyType);
                            //propInfo.SetValue(myObjectFromDB, newPropertyValue);

                            if (!string.IsNullOrEmpty(newPropertyValue.ToString()))
                            {
                                pro.SetValue(obj, newPropertyValue, null);
                            }
                        }
                    }
                    else
                        continue;
                }
            }
            return obj;
        }

        [WebMethod]
        public static string GetAllTemplates()
        {
            Database db = new Database(connstr);
            string sql = "SELECT Id, TemplateName FROM msSchedulerV3.dbo.FormTemplates WHERE IsActive = 1";
            DataTable dt = new DataTable();
            db.Execute(sql, out dt);

            List<object> list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new
                {
                    Id = row["Id"].ToString(),
                    TemplateName = row["TemplateName"].ToString()
                });
            }

            return new JavaScriptSerializer().Serialize(list);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetTemplateHtmlById(string templateId, int customerId)
        {
            Database db = new Database(connstr);
            templateId = Common.CleanInput(templateId);
            string sql = $"SELECT HtmlBody FROM msSchedulerV3.dbo.FormTemplates WHERE Id = '{templateId}'";
            string htmlFromDB = db.ExecuteScalar(sql) ?? "";

            return ReplaceFeildsForHtml(htmlFromDB, Convert.ToInt32(templateId), customerId);
        }
        private static string ReplaceFeildsForHtml(string htmlTemplate, int templateID, int customerId)
        {
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database(connstr);
            string sql = $"SELECT * FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId = {customerId} and companyid = '{companyid}';";
            DataTable dt = new DataTable();
            DataTable dtCompany = new DataTable();
            db.Execute(sql, out dt);

            string sqlForCompany = $"SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = '{companyid}';";
            db.Execute(sqlForCompany, out dtCompany);
            System.Data.DataRow company = dtCompany.Rows[0];

            string CompanyName = company["CompanyName"].ToString();
            string CompanyAddress = company["Address"].ToString();
            string CompanyPhone = company["Phone"].ToString();
            string CompanyEmail = company["Email"].ToString();
            string companyLogoFile = company["LogoFile"].ToString();
            string logoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + companyLogoFile);


            if (!File.Exists(logoPath))
                logoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            string logoBase64 = ImageToBase64(logoPath);


            string locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/locationicon.png");
            if (!File.Exists(locationIconPath))
                locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string locationIconBase64 = ImageToBase64(locationIconPath);


            string phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/phoneicon.png");
            if (!File.Exists(phoneIconPath))
                phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string phoneIconBase64 = ImageToBase64(phoneIconPath);


            string emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/emailicon.png");
            if (!File.Exists(emailIconPath))
                emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string emailIconBase64 = ImageToBase64(emailIconPath);


            if (dt.Rows.Count == 0) return "";

            string html = htmlTemplate;
            html = html.Replace("{{CustomerName}}", dt.Rows[0]["FirstName"].ToString() + " " + dt.Rows[0]["LastName"].ToString())
                                       .Replace("{{Phone}}", dt.Rows[0]["Phone"].ToString())
                                       .Replace("{{Email}}", dt.Rows[0]["Email"].ToString())
                                       .Replace("{{Address}}", dt.Rows[0]["Address1"].ToString() + ", " + dt.Rows[0]["City"].ToString() + ", " + dt.Rows[0]["State"].ToString() + ", " + dt.Rows[0]["Country"].ToString())
                                       .Replace("{{Company}}", dt.Rows[0]["CompanyName"].ToString())
                                       .Replace("{{CompanyName}}", dt.Rows[0]["CompanyName"].ToString())
                                       .Replace("{{Today}}", DateTime.Now.ToString("MM/dd/yyyy"))
                                       .Replace("{{image}}", logoBase64)
                                       .Replace("{{locationIcon}}", locationIconBase64)
                                       .Replace("{{phoneIcon}}", phoneIconBase64)
                                       .Replace("{{emailIcon}}", emailIconBase64)
                                       .Replace("{{companyAddress}}", CompanyAddress)
                                       .Replace("{{companyPhone}}", CompanyPhone)
                                       .Replace("{{companyEmail}}", CompanyEmail);
            if (templateID == 5)
            {
                string sqlForEsimateApproval = $"SELECT Number  FROM msSchedulerV3.dbo.tbl_invoice WHERE CustomerId = {customerId} and compnyid = '{companyid}' and Type='Estimate'";
                DataTable dtEstimate = new DataTable();
                db.Execute(sqlForEsimateApproval, out dtEstimate);
                if (dtEstimate.Rows.Count == 0) return "<h3 style=\"color: red;\">No Estimate Under This Customer</h3>";
                html = html.Replace("{{EstimateNumber}}", dtEstimate.Rows[0]["Number"].ToString());
            }
            if (templateID == 7 || templateID == 4)
            {
                string sqlForWorkOrder = $@"
                 SELECT a.note As AppointmentNote,r.Name as ResourceName,startdatetime,endDatetime,*
                 FROM msSchedulerV3.dbo.tbl_Appointment AS a 
                 INNER JOIN msSchedulerV3.dbo.tbl_ServiceType AS st ON a.servicetype = st.ServiceTypeID
                INNER JOIN msSchedulerV3.dbo.tbl_Resources AS r ON a.ResourceID = r.ID
                WHERE a.customerid = {customerId} 
                AND a.CompanyID = '{companyid}' 
                AND a.status != 'Deleted'";
                DataTable dtWorkOrder = new DataTable();
                db.Execute(sqlForWorkOrder, out dtWorkOrder);
                if (dtWorkOrder.Rows.Count == 0) return "<h3 style=\"color: red;\">No Services Under This Customer!</h3>";
                html = html.Replace("{{ServiceType}}", dtWorkOrder.Rows[0]["ServiceName"].ToString())
                           .Replace("{{JobDescription}}", dtWorkOrder.Rows[0]["AppointmentNote"].ToString())
                           .Replace("{{TechnicianName}}", dtWorkOrder.Rows[0]["ResourceName"].ToString())
                           .Replace("{{TechnicianName}}", dtWorkOrder.Rows[0]["ResourceName"].ToString())
                           .Replace("{{StartTime}}", dtWorkOrder.Rows[0]["startdatetime"].ToString())
                           .Replace("{{EndTime}}", dtWorkOrder.Rows[0]["endDatetime"].ToString())
                           .Replace("{{WorkOrderNumber}}", dtWorkOrder.Rows[0]["AppoinmentUId"].ToString())
                           .Replace("{{JobID}}", dtWorkOrder.Rows[0]["AppoinmentUId"].ToString());
            }

            return html;
        }

        private static string ImageToBase64(string imagePath)
        {
            if (!File.Exists(imagePath))
                return "";

            byte[] imgBytes = File.ReadAllBytes(imagePath);
            string extension = Path.GetExtension(imagePath).Replace(".", "").ToLower();
            return $"data:image/{extension};base64,{Convert.ToBase64String(imgBytes)}";
        }

        [WebMethod]
        public static string GetTemplateById(string templateId)
        {

            Database db = new Database(connstr);
            templateId = Common.CleanInput(templateId);
            string sql = $"SELECT * FROM msSchedulerV3.dbo.FormTemplates WHERE Id = '{templateId}'";
            DataTable dt = new DataTable();
            db.Execute(sql, out dt);

            if (dt.Rows.Count == 0) return "";

            var obj = new
            {
                Id = dt.Rows[0]["Id"].ToString(),
                TemplateName = dt.Rows[0]["TemplateName"].ToString(),
                HtmlBody = dt.Rows[0]["HtmlBody"].ToString()
            };


            return new JavaScriptSerializer().Serialize(obj);
        }

        [WebMethod]
        public static void SaveTemplate(string id, string name, string html)
        {
            Database db = new Database(connstr);
            id = Common.CleanInput(id);
            name = Common.CleanInput(name);
            html = html?.Replace("'", "''");

            string sql;
            if (string.IsNullOrEmpty(id) || id == "0")
            {
                sql = $"INSERT INTO msSchedulerV3.dbo.FormTemplates (TemplateName, HtmlBody, IsActive) VALUES ('{name}', '{html}', 1)";
            }
            else
            {
                sql = $"UPDATE msSchedulerV3.dbo.FormTemplates SET TemplateName = '{name}', HtmlBody = '{html}' WHERE Id = '{id}'";
            }
            db.Execute(sql);
        }

        [WebMethod]
        public static void DeleteTemplate(string templateId)
        {
            Database db = new Database(connstr);
            templateId = Common.CleanInput(templateId);
            string sql = $"UPDATE msSchedulerV3.dbo.FormTemplates SET IsActive = 0 WHERE Id = '{templateId}'";
            db.Execute(sql);
        }

        [WebMethod]
        public static string GeneratePDF(string html, string templateName)
        {
            try
            {
                string fileName = $"{templateName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string folder = HttpContext.Current.Server.MapPath("~/EmailHistoryContent/temp/");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, fileName);

                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(html);
                doc.Save(filePath);
                doc.Close();

                return fileName;
            }
            catch
            {
                return "";
            }
        }

        [WebMethod]
        public static string EmailFormPdfToCustomer(string html, string templateName, string customerId, string email, string customerName)
        {
            try
            {
                string companyId = HttpContext.Current.Session["CompanyID"].ToString();
                string fileName = $"{templateName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                // ✅ Save PDF
                string folderPath = HttpContext.Current.Server.MapPath($"~/EmailHistoryContent/{customerId}/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, fileName);

                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument doc = converter.ConvertHtmlString(html);
                doc.Save(filePath);
                doc.Close();

                // ✅ Generate and Insert FormRequest
                Guid token = Guid.NewGuid();
                Database db = new Database(connstr);
                string cleanHtml = html.Replace("'", "''"); // escape quotes

                string sql = $@"
                INSERT INTO msSchedulerV3.dbo.FormRequests (Token, CustomerId, TemplateId, Email, HtmlContent, IsSubmitted)
                VALUES ('{token}', '{customerId}', 0, '{email}', '{cleanHtml}', 0)";
                db.Execute(sql);

                // ✅ Email link
                //string formLink = $"https://yourdomain.com/ViewFormSign.aspx?token={token}";
                string formLink = $"http://localhost:33746/ViewFormSign.aspx?token={token}";

                // ✅ Email sending
                List<EmailContent> attachments = new List<EmailContent>
        {
            new EmailContent
            {
                FileName = fileName,
                FileUrl = $"~/EmailHistoryContent/{customerId}/{fileName}"
            }
        };

                string subject = $"Form: {templateName}";
                string body = $@"
            Dear {customerName},<br/><br/>
            Please review the attached form and click the link below to sign and submit it:<br/>
            <a href='{formLink}' target='_blank'>Click here to sign the form</a><br/><br/>
            Thank you!";

                EmailProcessor emailProcessor = new EmailProcessor();
                emailProcessor.SendHtmlFormattedEmail(companyId, customerId, "Form Email", subject, body, email, "", "", attachments);

                return "OK";
            }
            catch
            {
                return "ERR";
            }
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerBillingAddress> GetCustomerBillingAddress(string customerID)
        {
            try
            {
                var result = new List<CustomerBillingAddress>();
                if (string.IsNullOrWhiteSpace(customerID))
                {
                    return result;
                }
                string errorMsg = string.Empty;
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string companyid = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(connStr);

                string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerBillingAddress] where Customer_ID='" + customerID + "'";

                DataTable dt = new DataTable(connStr);
                db.Execute(sqlAppDtl, out dt);
                result = ConvertDataTable<CustomerBillingAddress>(dt);
                return result;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CustomerNote> CustomerNotes(string customerGuid)
        {
            try
            {
                var result = new List<CustomerNote>();

                if (string.IsNullOrWhiteSpace(customerGuid))
                    return result;

                string companyId = HttpContext.Current.Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                Database db = new Database(connStr);
                string sqlGetCustomerId = @"SELECT CustomerID 
                                    FROM tbl_Customer 
                                    WHERE CustomerGuid = '" + customerGuid + @"' 
                                      AND CompanyID = '" + companyId + "'";

                DataTable dtCust = new DataTable();
                db.Execute(sqlGetCustomerId, out dtCust);

                if (dtCust.Rows.Count == 0)
                    return result;

                int customerId = Convert.ToInt32(dtCust.Rows[0]["CustomerID"]);

                string sqlNotes = @"SELECT * 
                            FROM tbl_Note 
                            WHERE CustomerId = '" + customerId + @"' 
                              AND CompanyId = '" + companyId + "'";

                DataTable dtNotes = new DataTable();
                db.Execute(sqlNotes, out dtNotes);

                result = ConvertDataTable<CustomerNote>(dtNotes);

                return result;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<TagModel> GetTags()
        {
            try
            {
                List<TagModel> list = new List<TagModel>();

                string companyId = HttpContext.Current.Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                Database db = new Database(connStr);

                string sql = @"SELECT Id, Name 
                       FROM  [msSchedulerV3].[dbo].[Tbl_CSLTag]
                       WHERE CompanyId = '" + companyId + "'";

                DataTable dt = new DataTable();
                db.Execute(sql, out dt);

                foreach (DataRow row in dt.Rows)
                {
                    TagModel tag = new TagModel();
                    tag.Id = Convert.ToInt32(row["Id"]);
                    tag.Name = row["Name"].ToString();

                    list.Add(tag);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string AddNote(string customerGuid, string noteText, string tagIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerGuid) || string.IsNullOrWhiteSpace(noteText))
                    return "Customer or Note is missing.";

                string companyId = HttpContext.Current.Session["CompanyID"].ToString();
                string userId = HttpContext.Current.Session["LoginUser"]?.ToString() ?? "";

                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                Database db = new Database(connStr);

                // 1️⃣ Get CustomerID from GUID
                string sqlGetCustomerId = @"SELECT CustomerID 
                                    FROM tbl_Customer 
                                    WHERE CustomerGuid = '" + customerGuid + "' AND CompanyID = '" + companyId + "'";
                DataTable dtCust = new DataTable();
                db.Execute(sqlGetCustomerId, out dtCust);

                if (dtCust.Rows.Count == 0)
                    return "Customer not found.";

                int customerId = Convert.ToInt32(dtCust.Rows[0]["CustomerID"]);

                // 2️⃣ Insert note
                string sqlInsertNote = @"INSERT INTO tbl_Note 
                                 (Description, CreatedAt, CustomerId, CompanyId, UserId, TagId)
                                 VALUES
                                 (@Description, GETDATE(), @CustomerId, @CompanyId, @UserId, @TagId)";

                // Use comma-separated tags as multiple inserts
                string[] tags = tagIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string tagId in tags)
                {
                    string sql = sqlInsertNote
                                 .Replace("@Description", "'" + noteText.Replace("'", "''") + "'")
                                 .Replace("@CustomerId", customerId.ToString())
                                 .Replace("@CompanyId", "'" + companyId + "'")
                                 .Replace("@UserId", "'" + userId + "'")
                                 .Replace("@TagId", tagId);
                    db.Execute(sql, out _);
                }

                // If no tags selected, insert with TagId = NULL
                if (tags.Length == 0)
                {
                    string sql = sqlInsertNote
                                 .Replace("@Description", "'" + noteText.Replace("'", "''") + "'")
                                 .Replace("@CustomerId", customerId.ToString())
                                 .Replace("@CompanyId", "'" + companyId + "'")
                                 .Replace("@UserId", "'" + userId + "'")
                                 .Replace("@TagId", "NULL");
                    db.Execute(sql, out _);
                }

                return "Note added successfully.";
            }
            catch (Exception ex)
            {
                throw new HttpException(ex.Message);
            }
        }

        protected void btnSendSMS_Click(object sender, EventArgs e)
        {
            try
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
                string _CustomerID = CustomerID.Value;
                string SmsBody = txtSMS.Value;
                string Mobile = mobile.Value;

                _CustomerID = Common.CleanInput(_CustomerID);
                SmsBody = Common.CleanInput(SmsBody);
                Mobile = Common.CleanInput(Mobile);

                TwilioSMSService smsService = new TwilioSMSService();
                bool result = smsService.SendCustomerAdHocSMS(CompanyID, _CustomerID, SmsBody, Mobile);

                string response;
                if (result == true)
                {

                    response = "Swal.fire('SMS Sent Successfully', '', 'success').then(() => { CloseSMSPopup(); });";
                }
                else
                {
                    response = "Swal.fire('Something went wrong, Please try again.', '', 'success').then(() => { CloseSMSPopup(); });";
                }

                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", response, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"Swal.fire('Error: {ex.Message}', '', 'warning');", true);
            }

        }
        protected void btnSendMMS_Click(object sender, EventArgs e)
        {
            try
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
                string _CustomerID = CustomerID.Value;
                string _mobile = mobile.Value;
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
               // bool result = twilio.SendInvoiceMMS(CompanyID, _CustomerID, mmsBody, _mobile, filePath, mmsUrl);

                string response = "";
                //if (result)
                //{
                //    response = "Swal.fire('MMS Sent Successfully', '', 'success').then(() => { CloseMMSPopup(); });";
                //}

                //else
                //{
                    response = "Something went wrong, Please try again.', '', 'success').then(() => { CloseMMSPopup(); });";
                //}
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", response, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"Swal.fire('Error: {ex.Message}', '', 'warning');", true);
            }
        }

        public class TagModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class CustomerNote
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }

            public int? CSLId { get; set; }
            public int? CustomerId { get; set; }
            public int? AppointmentId { get; set; }

            public string CompanyId { get; set; }
            public string UserId { get; set; }

            public int? TagId { get; set; }
        }
    }
}