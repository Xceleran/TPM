
using FSM.Entity.Enums;
using FSM.Models.LoginModels;
using FSM.Processors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TPM.Entity;

namespace TPM
{
    public partial class BusinessContact : System.Web.UI.Page
    {
        public string CustomerID { get; private set; }
        private static string connstr = ConfigurationManager.AppSettings["ConnString"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("logout.aspx");
            }
            if (IsPostBack == false)
            {
               
              
                CreateStateProvience();

                div_country.Visible = Session["CompanyType"].ToString().ToLower() == "lhg";
                div_province.Visible = Session["CompanyType"].ToString().ToLower() == "lhg";

                // Load Tags
                LoadTags();

                
                 if (Request.QueryString["WGID"] != null)
                {
                    LoadBusinessIdByWarrentyCompany(Request.Params["WGID"].ToString().Trim());

                 
                }
                if (Request.QueryString["r"] != null)
                {
                    RetVal.Value = Request.Params["r"].ToString();
                }
                div_details.Visible = (bool)Session["IsAireMaster"];
                
                    div_details.Visible = true;
               
                if(!string.IsNullOrEmpty( CustomerID))
                {
                    Load_Business_Conatct(CustomerID);
                    div_More.Visible = true;
                    hdMode.Value = "Add";
                }
                if (Request.QueryString["id"] != null)
                {
                    BusinessGuID.Value = Request.Params["id"].ToString();
                    // BusinessID.Value = Request.Params["id"].ToString();
                    Load_Business_Conatct("");
                    div_More.Visible = true;
                    hdMode.Value = "Add";
                }
                else if (Request.QueryString["Bid"] != null)
                {
                    Load_Business_Conatct(Request.QueryString["Bid"]);
                    div_More.Visible = true;
                    hdMode.Value = "Add";
                }
                else
                {
                    div_More.Visible = false;
                    btn_Delete.Visible = false;
                }
                Load_Initial_Data();

                //Nizam
                RM_details.Visible = (bool)Session["IsAireMaster"];
            }
            else
            {
                // On postback, ensure tags are loaded
                LoadTags();
                // Refresh selectpicker after postback
                ScriptManager.RegisterStartupScript(this, this.GetType(), "RefreshSelectPickerPostBack", "setTimeout(function() { if (typeof initializeSelectPicker === 'function') { initializeSelectPicker(); } }, 500);", true);
            }
        }
        private void LoadBusinessIdByWarrentyCompany(string WGID)
        {
            try
            {


                string companyId = Session["CompanyID"].ToString();
               
                Database db = new Database(connstr);

                string sql = @"SELECT    tbl_WarrantyCompany.WarrantyCompanyUID, tbl_WarrentyCompanyCustomer.CompanyID, tbl_WarrentyCompanyCustomer.CustomerID, tbl_WarrentyCompanyCustomer.WarrentyCompanyID
                                    FROM            [msSchedulerV3].[dbo].tbl_WarrantyCompany INNER JOIN
                         [msSchedulerV3].[dbo].tbl_WarrentyCompanyCustomer ON tbl_WarrantyCompany.WarrantyCompanyUID = tbl_WarrentyCompanyCustomer.WarrentyCompanyID
                       WHERE CompanyId = @CompanyId and tbl_WarrantyCompany.WarrantyCompanyGuID=@GuID";

                DataTable dt = new DataTable();
               

                db.Open();
                db.Command.Parameters.Clear();
                db.AddParameter("@CompanyId", companyId, SqlDbType.NVarChar);
                db.AddParameter("@GuID", WGID, SqlDbType.NVarChar);

                db.ExecuteParam(sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        CustomerID = row["CustomerID"].ToString();
                    }
                }
                // Manually add items for HtmlSelect control

            }
            catch (Exception ex)
            {
                // Log error - you might want to add proper logging here
                System.Diagnostics.Debug.WriteLine("Error loading tags: " + ex.Message);
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

        private void Load_Initial_Data()
        {
            try
            {
                string companyID = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database();

                string sql = @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_SalesRep] where  CompanyID=@CompanyID;";
                sql += @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_LeadSource] where  CompanyID=@CompanyID;";
                sql += @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_LeadType] where  CompanyID=@CompanyID;";
                sql += @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_SalesStatus] where  CompanyID=@CompanyID;";
                sql += @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_ProjectType] where  CompanyID=@CompanyID;";
                sql += @"SELECT  [ID],[Title] FROM [msSchedulerV3].[dbo].[tbl_ProjectStatus] where  CompanyID=@CompanyID;";

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
            }
            catch { }
        }

        public void Load_Business_Conatct(string bid)
        {
            string CompanyID = Session["CompanyID"].ToString();

            try
            {
                BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();

                BusinessContacts businessContact = null;
                if (string.IsNullOrEmpty(bid))

                {
                    businessContact = businessContactProcessor.GetBusinessContactByGuid(CompanyID, BusinessGuID.Value);
                }
                else
                {
                    businessContact = businessContactProcessor.GetBusinessContactById(CompanyID, bid);
                }

                BusinessGuID.Value = businessContact.CustomerGuid;
                hdCustomerGUID.Value = businessContact.CustomerGuid;

                txt_BusinessName.Value = businessContact.BusinessName;
                txt_Title.Text = businessContact.Title;

           

                address1.Value = businessContact.Address1;
                address2.Value = businessContact.Address2;
                txt_FirstName.Value = businessContact.FirstName;
                txt_LastName.Value = businessContact.LastName;
                city.Value = businessContact.City;
                country.Value = businessContact.Country;
                state.Value = businessContact.State;
                province.Value = businessContact.State;
                zip.Value = businessContact.ZipCode;
                phone.Value = businessContact.Phone;
                mobile.Value = businessContact.Mobile;
                email.Value = businessContact.Email;
                BusinessID.Value = businessContact.CustomerID;
                hf_BusinessGuid.Value = businessContact.CustomerGuid;
                txt_notes.Value = businessContact.Notes;
                txt_rmid.Value = businessContact.AMCustomerId;
                txt_custcode.Value = businessContact.CustomerCode;
                
                // Load tags if needed
                if (ddlTag.Items.Count == 0)
                {
                    LoadTags();
                }
                
                // Clear all selections first
                foreach (ListItem item in ddlTag.Items)
                {
                    item.Selected = false;
                }
                
                // Set selected tags from CSLTagString (comma-separated)
                string tagString = businessContact.CSLTagString ?? "";
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

                var businessContacts = businessContactProcessor.GetBusinessContacts(CompanyID, BusinessID.Value);

              
                PrimaryCustomerid.Value = BusinessID.Value;
                BindBusinessContacts(businessContacts.Where(n => n.IsPrimaryContact == false).ToList());
                div_More.Visible = true;

                string CustomerID = businessContact.CustomerID;
                string Sql = "Select FORMAT( (select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.tbl_invoice where Type='Invoice' and CompnyID='" + CompanyID + "' and CustomerID='" + CustomerID + "'), 'N2') as TotalDueForInvoice," +
               "FORMAT((select ISNULL(Sum(Total-AmountCollect),0) from msSchedulerV3.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + CompanyID + "' and CustomerID='" + CustomerID + "'), 'N2') as TotalDueForEstimate," +
               "(select count(Type) from msSchedulerV3.tbl_invoice where Type='Proposal' and IsConverted = 0 and CompnyID='" + CompanyID + "' and CustomerID='" + CustomerID + "') as TotalEstimate," +
               "(select count(Type) from msSchedulerV3.tbl_invoice where Type='Invoice' and CompnyID='" + CompanyID + "' and CustomerID='" + CustomerID + "') as TotalInvoice," +
               "(select count(CompanyID) from msSchedulerV3.tbl_Appointment where CompanyID='" + CompanyID + "' and CustomerID='" + CustomerID + "') as TotalAppoinment ";

                Database db = new Database();
                DataTable dt = new DataTable();
                //  db.Open();
                db.Execute(Sql, out dt);
                db.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        TotalDueForInvoice.Value = dr["TotalDueForInvoice"].ToString();
                        TotalDueForEstimate.Value = dr["TotalDueForEstimate"].ToString();
                        TotalEstimate.InnerText = dr["TotalEstimate"].ToString();
                        TotalInvoice.InnerText = dr["TotalInvoice"].ToString();
                        TotalAppoinment.Value = dr["TotalAppoinment"].ToString();
                    }
                }
            }
            catch { }
        }

        private StringBuilder table = new StringBuilder();

        private void BindBusinessContacts(List<BusinessContacts> businessContacts)
        {
            table.Append("<div><table id='example' class='table table-striped table-bordered nowrap'  style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial;width:100%' >");

            table.Append("<thead><tr><th>First Name</th><th>Last Name</th> <th>Address</th><th>Mobile</th><th>Phone</th><th>Email</th><th>...</th></thead>");

            int i = 0;
            foreach (BusinessContacts businessContact in businessContacts)
            {
                i++;
                //  string sUrl = "Customer.aspx?ID=" + businessContact.CustomerGuid;
                string sUrl = "customerDetail.aspx?m=2&cid=" + businessContact.CustomerID;
                table.Append("<tr>");

                table.Append("<td><a style='color:#526288' href='" + sUrl + "'>" + businessContact.FirstName + "</a></td>");
                table.Append("<td><a style='color:#526288' href='" + sUrl + "'>" + businessContact.LastName + "</a></td>");

                table.Append("<td>" + businessContact.Address1 + ", " + businessContact.City + ", " + businessContact.State + " " + businessContact.ZipCode + ", " + businessContact.Country + "</td>");
                table.Append("<td><a style='color:#526288' href='tel: " + businessContact.Mobile + "'>" + businessContact.Mobile + "</a></td>");
                table.Append("<td><a style='color:#526288' href='tel: " + businessContact.Phone + "'>" + businessContact.Phone + "</a></td>");
                table.Append("<td><a href='mailto:" + businessContact.Email + "'>" + businessContact.Email + "</a></td>");
                table.Append(@"<td><div class='dropdown'>
                                  <button class='btn btn-secondary btn-sm dropdown-toggle' type='button' id='Action" + i.ToString() + @"' data-bs-toggle='dropdown' aria-expanded='false'>
                                    <i class='fas fa-align-justify'></i>
                                  </button>
                                  <ul class='dropdown-menu' aria-labelledby='Action" + i.ToString() + @"'>" +
                                "<li><a class='dropdown-item' href='calendar.aspx?CustomerID=" + businessContact.CustomerID + "'>Create Appointment</a></li>" +
                                "<li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId=" + businessContact.CustomerGuid + "&InType=Invoice'>Create Invoice</a></li>" +
                                "<li><a class='dropdown-item' href='Invoice.aspx?InvNum=0&cId=" + businessContact.CustomerGuid + "&InType=Proposal'>Create Estimate</a></li>" +
                                "<li><a class='dropdown-item' href='Invoices.aspx?Id=" + businessContact.CustomerID + "&Type=Invoice'>View Invoice</a></li>" +
                                "<li><a class='dropdown-item' href='Invoices.aspx?Id=" + businessContact.CustomerID + "&Type=Proposal'>View Proposal</a></li>" +
                                "<li><a class='dropdown-item' href='View_Appointment.aspx?Id=" + businessContact.CustomerID + "'>View Appointment</a></li>" +
                                "<li><hr class='dropdown-divider'></li>" +
                                "<li><a class='dropdown-item' href='CustomerFiles.aspx?Id=" + businessContact.CustomerID + "'>View Files</a></li>" +
                                "<li><a class='dropdown-item' href='EmailHistory_List.aspx?Id=" + businessContact.CustomerID + "&Type=Proposal'>View E-Mails</a></li>" +
                                "<li><hr class='dropdown-divider'></li>" +
                                "<li><a class='dropdown-item' onclick='OpenMailPopUp(\"" + businessContact.CustomerID + "\")' href='#'>Send Email</a></li>" +
                                "<li><a class='dropdown-item' onclick='OpenSurveyMailPopUp(\"" + businessContact.Email + "\",\"" + businessContact.CustomerID + "\")' href='#'>Send Survey Email</a></li>" +
                              "</ul></div></td>");

                table.Append("</tr>");
            }

            table.Append(" </tbody>");
            table.Append("</table></div>");

            ListTable.Controls.Add(new Literal { Text = table.ToString() });
            BindBusinessContactsTomain(businessContacts);
        }

        protected void btn_Save_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(BusinessGuID.Value))
            {
                Add_New_BusinessContact();
                // Reload tags after save
                LoadTags();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "ShowSuccessMsgForBusinessCustomer(); initializeSelectPicker();", true);
            }
            else
            {
                Update_BusinessContact();
                // Reload tags after update
                LoadTags();
                ScriptManager.RegisterStartupScript(this, this.GetType(), "RefreshSelectPicker", "initializeSelectPicker();", true);
            }

            // Response.Redirect("CustomerList.aspx?m=2");
        }

        public void Add_New_BusinessContact()
        {
            BusinessContacts businessContact = new BusinessContacts();

            string guid = Guid.NewGuid().ToString().ToUpper().Trim();
            businessContact.CompanyID = Session["CompanyID"].ToString();
            businessContact.BusinessName = txt_BusinessName.Value;
            businessContact.Title = txt_Title.Text;
            businessContact.FirstName = txt_FirstName.Value;
            businessContact.LastName = txt_LastName.Value;
            businessContact.Address1 = address1.Value;
            businessContact.Address2 = address2.Value;
            businessContact.City = city.Value;
            businessContact.Notes = txt_notes.Value.ToString();
            businessContact.Country = Session["CompanyType"].ToString().ToLower() == "lhg" ? country.Value.Trim() : "USA"; ;
            businessContact.State = Session["CompanyType"].ToString().ToLower() == "lhg" && country.Value.Trim().Equals("Canada") ? province.Value.Trim() : state.Value.Trim();
            businessContact.ZipCode = zip.Value;
            businessContact.Phone = phone.Value;
            businessContact.Mobile = mobile.Value;
            businessContact.Email = email.Value;
            businessContact.CustomerGuid = guid;
            businessContact.CustomerCode = txt_custcode.Value;
            
            // Set CSLTagString from multi-select dropdown
            string tagsString = "";
            string[] selectedTags = Request.Form.GetValues(ddlTag.UniqueID);
            if (selectedTags != null && selectedTags.Length > 0)
            {
                tagsString = string.Join(",", selectedTags);
            }
            businessContact.CSLTagString = tagsString;
            // Keep CSLTagId for backward compatibility (use first tag if any)
            if (!string.IsNullOrEmpty(tagsString))
            {
                string[] tags = tagsString.Split(',');
                if (tags.Length > 0 && !string.IsNullOrEmpty(tags[0].Trim()))
                {
                    businessContact.CSLTagId = Convert.ToInt32(tags[0].Trim());
                }
                else
                {
                    businessContact.CSLTagId = null;
                }
            }
            else
            {
                businessContact.CSLTagId = null;
            }

            BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
            businessContactProcessor.AddBusinessContact(businessContact);
            BusinessGuID.Value = guid;
            //if (businessContactProcessor.AddBusinessContact(businessContact))
            //{
            //    div_More.Visible = true;
            //    BusinessContacts businessContactPrimary = new BusinessContacts();
            //    businessContactPrimary.IsPrimaryContact = true;
            //    businessContactPrimary.CompanyID = Session["CompanyID"].ToString();
            //    businessContactPrimary.FirstName = txt_ContactFirstName.Value;
            //    businessContactPrimary.LastName = txt_ContactLastName.Value;
            //    businessContactPrimary.Address1 = txt_Contactaddress1.Value;

            //    businessContactPrimary.Title = txt_title.Text;
            //    businessContactPrimary.JobTitle = txt_JobTitle.Text;
            //    businessContactPrimary.City = txt_Contactcity.Value;
            //    businessContactPrimary.State = txt_ContactState.Value;
            //    businessContactPrimary.ZipCode = txt_Contactzip.Value;
            //    businessContactPrimary.Phone = txt_Contactphone.Value;
            //    businessContactPrimary.Mobile = txt_Contactmobile.Value;
            //    businessContactPrimary.Email = txt_Contactemail.Value;
            //    businessContactPrimary.BusinesGuid = guid;
            //    businessContactProcessor.AddBusinessPrimaryContact(businessContactPrimary);
            //}
        }

        public void Update_BusinessContact()
        {
            BusinessContacts businessContact = new BusinessContacts();

            businessContact.CustomerID = BusinessID.Value.ToString();
            businessContact.CompanyID = Session["CompanyID"].ToString();
            businessContact.BusinessName = txt_BusinessName.Value;
            businessContact.Title = txt_Title.Text;
            businessContact.FirstName = txt_FirstName.Value;
            businessContact.LastName = txt_LastName.Value;
            businessContact.Address1 = address1.Value;
            businessContact.Notes = txt_notes.Value.ToString();
            businessContact.Address2 = address2.Value;
            businessContact.City = city.Value;
            businessContact.Country = Session["CompanyType"].ToString().ToLower() == "lhg" ? country.Value.Trim() : "USA"; ;
            businessContact.State = Session["CompanyType"].ToString().ToLower() == "lhg" && country.Value.Trim().Equals("Canada") ? province.Value.Trim() : state.Value.Trim();
            businessContact.ZipCode = zip.Value;
            businessContact.Phone = phone.Value;
            businessContact.Mobile = mobile.Value;
            businessContact.Email = email.Value;
            businessContact.CustomerCode = txt_custcode.Value;
            
            // Set CSLTagString from multi-select dropdown
            string tagsString = "";
            string[] selectedTags = Request.Form.GetValues(ddlTag.UniqueID);
            if (selectedTags != null && selectedTags.Length > 0)
            {
                tagsString = string.Join(",", selectedTags);
            }
            businessContact.CSLTagString = tagsString;
            // Keep CSLTagId for backward compatibility (use first tag if any)
            if (!string.IsNullOrEmpty(tagsString))
            {
                string[] tags = tagsString.Split(',');
                if (tags.Length > 0 && !string.IsNullOrEmpty(tags[0].Trim()))
                {
                    businessContact.CSLTagId = Convert.ToInt32(tags[0].Trim());
                }
                else
                {
                    businessContact.CSLTagId = null;
                }
            }
            else
            {
                businessContact.CSLTagId = null;
            }

            BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
            businessContactProcessor.Update_BusinessContact(businessContact);

            //BusinessContacts businessContactPrimary = new BusinessContacts();

            //businessContactPrimary.IsPrimaryContact = true;
            //businessContactPrimary.CustomerID = PrimaryCustomerid.Value;
            //businessContactPrimary.CompanyID = Session["CompanyID"].ToString();
            //businessContactPrimary.FirstName = txt_ContactFirstName.Value;
            //businessContactPrimary.LastName = txt_ContactLastName.Value;
            //businessContactPrimary.Address1 = txt_Contactaddress1.Value;
            //businessContactPrimary.City = txt_Contactcity.Value;
            //businessContactPrimary.Title = txt_title.Text;
            //businessContactPrimary.JobTitle = txt_JobTitle.Text;
            //businessContactPrimary.State = txt_ContactState.Value;
            //businessContactPrimary.ZipCode = txt_Contactzip.Value;
            //businessContactPrimary.Phone = txt_Contactphone.Value;
            //businessContactPrimary.Mobile = txt_Contactmobile.Value;
            //businessContactPrimary.Email = txt_Contactemail.Value;

            //if (PrimaryCustomerid.Value == "0" || string.IsNullOrEmpty(PrimaryCustomerid.Value))
            //{
            //    businessContactPrimary.BusinesGuid = BusinessGuID.Value;
            //    businessContactProcessor.AddBusinessPrimaryContact(businessContactPrimary);
            //}

            //else
            //{
            //    businessContactProcessor.Update_PrimaryContact(businessContactPrimary);
            //}
        }

        [System.Web.Services.WebMethod]
        public static void DeleteCustomer(string BusinessID)
        {
            BusinessContacts businessContact = new BusinessContacts();

            businessContact.BusinessID = BusinessID;
            businessContact.CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
            businessContactProcessor.Delete_BusinessContact(businessContact);
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

        private void BindBusinessContactsTomain(List<BusinessContacts> businessContacts)
        {
            if (businessContacts == null || businessContacts.Count == 0)
                return; // Exit if there are no contacts

            // Take only the first row
            BusinessContacts businessContact = businessContacts.First();

            // Update input fields with the first contact's details
            txt_FirstName.Value = businessContact.FirstName;
            txt_LastName.Value = businessContact.LastName;
            email.Value = businessContact.Email;
        }
    }
}