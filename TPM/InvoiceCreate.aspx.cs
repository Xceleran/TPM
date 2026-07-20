using FSM.Helper;
using FSM.Entity.Customer;
using FSM.Processors;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FSM
{
    public partial class InvoiceCreate : System.Web.UI.Page
    {
        string CompanyID = "";
        string CompanyName = "";
        string CompanyGUID = "";
        string Qut_Number = "";
        bool IsQuickBookEnabled = false;
        StringBuilder table = new StringBuilder();
        static QBOSettins qBoStng;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("Dashboard.aspx");
                return;
            }
            if (!IsPostBack)
            {
                CompanyID = Session["CompanyID"].ToString();
                QBOManager qBOManager = new QBOManager();

                IsQuickBookEnabled = qBOManager.VerifyCompanySetting(Session["CompanyID"].ToString(), ref qBoStng);
                hd_IsQuickBookEnabled.Value = IsQuickBookEnabled.ToString();

                if (Session["CompanyName"] != null) CompanyName = Session["CompanyName"].ToString();
                if (Session["CompanyGUID"] != null) CompanyGUID = Session["CompanyGUID"].ToString();

                hdCompanyID.Value = CompanyID;
                hdCompanyName.Value = CompanyName;
                hdCompanyGUID.Value = CompanyGUID;

                string _CustomerId = Common.CleanInput(Request.Params["cId"]);
                string woIdParam = Common.CleanInput(Request.Params["woId"]);

                if (string.IsNullOrEmpty(_CustomerId) && !string.IsNullOrEmpty(woIdParam))
                {
                    _CustomerId = ResolveCustomerGuidFromWorkOrder(CompanyID, woIdParam);
                }

                if (string.IsNullOrEmpty(_CustomerId))
                {
                    string script = "alert('Customer is required. Assign the warranty company or open invoice from a customer/work order.');window.location.href='ThirdPartyProviders.aspx';";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", script, true);
                    return;
                }

                CustomerProcessor customerProcessor = new CustomerProcessor();
                if (!customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = _CustomerId }))
                {
                    string exception = "alert('Invalid Customer data.');window.location.href='Dashboard.aspx';";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
                    return;
                }

                SV_CustomeID.Value = _CustomerId;
                Indicator.Value = Common.CleanInput(Request.Params["InType"]);
                AppointmentID.Value = Common.CleanInput(Request.Params["AppID"] ?? Request.Params["woId"]);
                //btn_ConvertToInvocie.Visible = false;
                if (Request.Params["InType"] == "Proposal" && Request.Params["InvNum"] != null)
                {
                    PageTitle.Text = " Proposal/Estimate";
                    //btn_ConvertToInvocie.Visible = true;
                    div_Expirationdate.Visible = true;
                }
                else
                {
                    div_Expirationdate.Visible = false;
                    PageTitle.Text = "Invoice";
                    //btn_ConvertToInvocie.Visible = false;
                }
                if (Request.Params["InType"] == "Proposal")
                {
                    PageTitle.Text = "Proposal/Estimate";
                    div_Expirationdate.Visible = true;
                }
                else
                {
                    div_Expirationdate.Visible = false;
                    PageTitle.Text = "Invoice";

                }

                Qut_Number = Request.Params["InvNum"];

                if (Qut_Number == "0" || Qut_Number == "Proposal" || Qut_Number == "Invoice" || Qut_Number == "" || Qut_Number == null)
                {
                    Qut_Number = string.Empty;
                }

                HdInvoiceId.Value = "";
                HdQboId.Value = "0";

                Load_InitialData(Qut_Number, _CustomerId);

                if (Request.Params["InvNum"] != null)
                {
                    _InvoiceNo.Value = Request.Params["InvNum"];
                }
                if (Request.Params["InType"] != null && Request.Params["InType"].ToString() == "Invoice")
                {
                    _Type.Value = Request.Params["InType"].ToString();

                    //btnSendInvoiceMail.Visible = true;
                    //btnSendProposalMail.Visible = false;
                }
                if (Request.Params["InType"] != null && Request.Params["InType"].ToString() == "Proposal")
                {
                    _Type.Value = Request.Params["InType"].ToString();

                    //btnSendInvoiceMail.Visible = false;
                    //btnSendProposalMail.Visible = true;
                }

                //if (Session["CanAccessInvoice"] != null)
                //{
                //    if (!Convert.ToBoolean(Session["CanAccessInvoice"]))
                //    {
                //        //btn_ConvertToInvocie.Visible = false;
                //        _SubmitInvoice.Visible = false;
                //        invPermission.Visible = true;
                //    }
                //    else
                //    {
                //        invPermission.Visible = false;
                //    }
                //}
                //else
                //{
                //    //btn_ConvertToInvocie.Visible = false;
                //    _SubmitInvoice.Visible = false;
                //    invPermission.Visible = true;
                //}
            }

        }
        private void Load_InitialData(string InvoiceID, string _CustomerId)
        {
            string CompanyID = Session["CompanyID"].ToString();
            Database db = new Database();
            string Sql = @" Select Name, Id from Taxes where IsDeleted ='0'  and CompanyId=@CompanyID ";
            if (IsQuickBookEnabled)
            {
                Sql += " and isnull(qboid,0) not in(0) ";
            }

            Sql += " order by Name asc ;";

            Sql += @" SELECT  [CompanyID]
                          ,[CustomerID]
                          ,[CustomerGuid]
                          ,[Title]
                          ,[FirstName]
                          ,BusinessName,IsBusinessContact
                          ,[LastName]
                          ,[JobTitle]
                          ,[Address1]
                          ,[Address2]
                          ,[City]
                          ,[State]
                          ,[ZipCode]
                          ,[Phone]
                          ,[Mobile]
                          ,[Email]
                          ,[QboId]
                          ,[BusinessID]
                          ,[SyncToken]
                      FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyId=@CompanyID and CustomerGuid='" + _CustomerId + "';";

            txt_Deposit.Value = "0.00";
            txt_Due.Value = "0.00";
            if (!string.IsNullOrEmpty(InvoiceID))
            {
                Sql += @"SELECT [ID]
                          ,[Number]
                          ,[InvoiceDate]
                          ,[QboId]
                           ,IsConverted
                          ,[DiscountRate]
                          ,[QboEstimateId]
                           ,isnull([AmountCollect],0.00) as DepositAmount,Discount,Tax,(Total- (isnull(AmountCollect,0.00))) as Due
                          ,[ExpirationDate],isnull(PONO,'') PONO
                      FROM [msSchedulerV3].[dbo].[tbl_Invoice] Where CompnyID =@CompanyID And ID='" + InvoiceID + "'";

            }
            DataSet dataSet = db.Get_DataSet(Sql, CompanyID);

            LoadTaxData(dataSet.Tables[0]);
            LoadCustomerData(dataSet.Tables[1]);

            string invDate = "";
            invDate = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            string _ExpirationDate = "";
            _ExpirationDate = DateTime.Now.AddDays(42).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);


            if (!string.IsNullOrEmpty(InvoiceID))
            {
                DataRow dr = dataSet.Tables[2].Rows[0];
                try
                {
                    if (dr["InvoiceDate"].ToString() != null && dr["InvoiceDate"].ToString() != "")
                        invDate = Convert.ToDateTime(dr["InvoiceDate"]).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                    else
                        invDate = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                    if (dr["ExpirationDate"].ToString() != null && dr["ExpirationDate"].ToString() != "")
                        _ExpirationDate = Convert.ToDateTime(dr["ExpirationDate"]).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                    else
                        _ExpirationDate = DateTime.Now.AddDays(42).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                }
                catch { }

                txt_Deposit.Value = dr["DepositAmount"].ToString();
                txt_Due.Value = dr["Due"].ToString();

                Number.Text = dr["Number"].ToString();
                txt_InvocieNumber.Text = dr["Number"].ToString(); ;
                HdInvoiceId.Value = dr["ID"].ToString();
                lblPONO.Text = dr["PONO"].ToString();
                hf_QboEstimateId.Value = dr["QboEstimateId"].ToString();
                HdQboId.Value = dr["QboId"].ToString();

                if (Convert.ToBoolean(dr["IsConverted"]))
                {
                    //btn_ConvertToInvocie.Visible = false;
                    _SubmitInvoice.Visible = false;
                }
            }
            else
            {
                if (Request.Params["InType"] == "Proposal")
                {
                    Qut_Number = GenerateInvocieNumber(false);
                }
                else
                {
                    Qut_Number = GenerateInvocieNumber(true);
                }
                Number.Text = Qut_Number;
                txt_InvocieNumber.Text = Qut_Number;
            }
            Date.Text = invDate;
            ExpirationDate.Text = _ExpirationDate;
        }
        private void LoadCustomerData(DataTable dataTable)
        {
            DataRow dr = dataTable.Rows[0];
            txtFirstName.Text = dr["FirstName"].ToString() + " " + dr["LastName"].ToString();

            string FullAddress = string.Empty;
            hf_CustomerQboID.Value = dr["QboId"].ToString();
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

            Address.Text = "Address : " + FullAddress;
            Contact.Text = "Phone # " + dr["Phone"].ToString() + ", " + dr["Mobile"].ToString() + " <br/> Email : " + dr["Email"].ToString();

            string BusinessID = dr["BusinessID"].ToString();

            if (BusinessID != "0")
            {
                BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                BusinessContacts businessContact = businessContactProcessor.GetBusinessContactById(Session["CompanyID"].ToString(), BusinessID);

                txtFirstName.Text = businessContact.BusinessName;
                Address.Text = "Address : " + businessContact.Address1 + " " + businessContact.City + " " + businessContact.State + " " + businessContact.ZipCode;
                Contact.Text = "Phone # " + businessContact.Phone + ", " + businessContact.Mobile + " <br/> Email : " + businessContact.Email;
            }
            if (Convert.ToBoolean(dr["IsBusinessContact"]))
            {
                txtFirstName.Text = dr["BusinessName"].ToString();
            }
        }
        private void LoadTaxData(DataTable dtTax)
        {
            try
            {
                if (dtTax.Rows.Count > 0)
                {
                    optTaxRate.DataSource = dtTax;
                    optTaxRate.DataValueField = "Id";
                    optTaxRate.DataTextField = "Name";
                    optTaxRate.DataBind();
                    optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
                }
                else
                    optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
            }
            catch (Exception ex)
            {
                optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
            }
        }
        public static string GenerateInvocieNumber(bool IsInvocie)
        {
            DataTable table;
            string New_Number = string.Empty;
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            Database db = new Database();
            try
            {
                db.Open();
                string sql = @"SELECT [InvoicePrefix], [InvoiceNumberSeed], [EstimatePrefix], [EstimateNumberSeed] FROM [msSchedulerV3].[dbo].[tbl_Company]" +
                            "Where CompanyID= '" + CompanyID + "'";

                db.Execute(sql, out table);
                if (table.Rows.Count > 0)
                {
                    if (IsInvocie)
                    {
                        Int64 invoiceNumberSeed = Convert.ToInt64(table.Rows[0]["InvoiceNumberSeed"]) + 1;
                        string invoicePrefix = table.Rows[0]["InvoicePrefix"].ToString();

                        if (invoiceNumberSeed < 10001)
                        {
                            // ensure Invoice number start 10001
                            invoiceNumberSeed = 10001;
                        }

                        New_Number = string.Format("{0}-{1}-{2}", invoicePrefix, CompanyID, invoiceNumberSeed);

                        sql = @"Update [msSchedulerV3].[dbo].[tbl_Company] set InvoiceNumberSeed=" + invoiceNumberSeed + " Where CompanyID= '" + CompanyID + "'";

                        db.Execute(sql);

                    }
                    else
                    {
                        Int64 EstimateNumberSeed = Convert.ToInt64(table.Rows[0]["EstimateNumberSeed"]) + 1;
                        string EstimatePrefix = table.Rows[0]["EstimatePrefix"].ToString();

                        if (EstimateNumberSeed < 10001)
                        {
                            EstimateNumberSeed = 10001;
                        }

                        New_Number = string.Format("{0}-{1}-{2}", EstimatePrefix, CompanyID, EstimateNumberSeed);

                        sql = @"Update [msSchedulerV3].[dbo].[tbl_Company] set EstimateNumberSeed=" + EstimateNumberSeed + " Where CompanyID= '" + CompanyID + "'";

                        db.Execute(sql);
                    }
                }
                db.Close();
                return New_Number;
            }
            catch
            {
                return DateTime.Now.ToString("hmmss");
            }
            finally
            {
                db.Close();
            }

        }

        // THIS IS THE NEW METHOD
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static QboCommon GetInvoiceDetailsById(string iId = "")
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            SqlConnection conn = new SqlConnection(connStr);

            string sql = "Select Id,Name from Items  where IsDeleted=0  and CompanyId=@CompanyID order by Name asc; ";
            Database db = new Database(connStr);
            DataTable dtPrd = new DataTable();

            sql += @"select ItemId,ItemName,Description,Quantity,uPrice,ServiceDate,IsTaxable from tbl_InvoiceDetails where RefId = '" + iId + "' and companyid =@CompanyID order by CAST(NULLIF(LineNum,'') AS INT) asc ;";
            DataTable dt_InvoiceItems = new DataTable();

            // UPDATED SQL: Added the three new fields for requested deposit
            sql += @"select INVM.DiscountOption, INVM.DiscountRate, INVM.Discount, INVM.TaxType, Tx.Rate, INVM.Tax, INVM.Total, INVM.Note,
            INVM.RequestedDepoAmt, INVM.ReqDepoPercent, INVM.RequestedAmtType
            from tbl_Invoice INVM left join Taxes Tx on INVM.TaxType = tx.Id
            where INVM.id='" + iId + "' and INVM.CompnyID =@CompanyID";
            DataTable dt_Invoice = new DataTable();

            DataSet dataSet = db.Get_DataSet(sql, CompanyID);
            dtPrd = dataSet.Tables[0];
            dt_InvoiceItems = dataSet.Tables[1];
            dt_Invoice = dataSet.Tables[2];

            QboCommon iRet = new QboCommon();
            var NewRow = "";
            int Cid = 1;

            foreach (DataRow dr in dt_InvoiceItems.Rows)
            {
                var pId = dr["ItemId"].ToString();
                var pName = dr["ItemName"].ToString();
                var pDesc = "n/a";
                if (!string.IsNullOrEmpty(dr["Description"].ToString()))
                {
                    pDesc = dr["Description"].ToString();
                }

                var pQty = dr["Quantity"].ToString();
                var pRate = dr["uPrice"].ToString();
                var SDate = dr["ServiceDate"].ToString();
                decimal dlAmount = Convert.ToDecimal(pQty) * Convert.ToDecimal(pRate);

                string OptionData = "<option value=''>Select Item</option>";
                string slted = "";
                string txChecked = "";
                foreach (DataRow drP in dtPrd.Rows)
                {
                    slted = "";
                    if (drP["Id"].ToString() == dr["ItemId"].ToString())
                        slted = "selected='selected'";
                    OptionData += "<option " + slted + " value ='" + drP["Id"].ToString() + "'>" + drP["Name"].ToString() + "</option>";
                }
                NewRow += "<tr class='invTR'><td>" + Cid + "</td><td><select id='id_Name" + Cid + "' value =" + dr["ItemId"].ToString() + " class='allInputs idSelect form-select' onchange='FillItemsInfo(this)'>" + OptionData + "</select></td>";
                NewRow += "<td><input id='id_Desc" + Cid + "' type='text' value ='" + pDesc + "' class='idInput form-control itmDesc' /></td>";
                NewRow += "<td><input id='id_ServDate" + Cid + "' type='date' value =" + SDate + " class='idInput form-control srvDate' /></td>";
                NewRow += "<td><input id='id_Qty" + Cid + "' type='number'  value =" + pQty + " class='idInput form-control itmQty' onchange='CalLineTotal(this)' /></td>";
                NewRow += "<td><input id='id_Rate" + Cid + "' type='number' onfocusout='FormatDecimal(this)' value =" + pRate + " onclick='removeZero(this)'  class='idInput form-control itmRate' onchange='CalLineTotal(this)' /></td>";
                NewRow += "<td><input id='id_Amount" + Cid + "' type='number' readonly value =" + dlAmount.ToString("0.00") + " class='idInput form-control itmAmount' readonly /></td>";

                txChecked = "";
                if (dr["IsTaxable"].ToString().Trim() == "TAX")
                    txChecked = "checked";
                txChecked = "";
                if (dr["IsTaxable"].ToString().Trim() == "TAX")
                    txChecked = "checked";

                NewRow += @"<td>
                <div class='tax-toggle-container'>
                    <label class='toggle-switch'>
                        <input id='id_Tax" + Cid + @"' type='checkbox' class='idInput itmTax' onchange='TaxRecal()' " + txChecked + @">
                        <span class='slider-tax'></span>
                    </label>
                </div>
            </td>";

                NewRow += "<td><a href='javascript: void(0);' onclick='removeThis(this)' title='Del' class='delete-icon'><i class='fa fa-trash'></i></a></td>";
                NewRow += "</tr>";
                Cid += 1;
            }

            if (dt_Invoice.Rows.Count > 0)
            {
                DataRow drInv = dt_Invoice.Rows[0];
                iRet.DisType = drInv["DiscountOption"].ToString();
                iRet.DisRate = drInv["DiscountRate"].ToString();
                iRet.DisAmount = drInv["Discount"].ToString();

                iRet.TaxId = drInv["TaxType"].ToString();
                iRet.TaxRate = drInv["Rate"].ToString();
                iRet.TotalTax = drInv["Tax"].ToString();
                iRet.Notes = drInv["Note"].ToString();

                // ADDED: Populate the new properties
                iRet.ReqAmtType = drInv["RequestedAmtType"].ToString();
                iRet.ReqDepoAmt = drInv["RequestedDepoAmt"].ToString();
                iRet.ReqDepoPercent = drInv["ReqDepoPercent"].ToString();

                iRet.Quantity = Cid.ToString();
                iRet.TableRow = NewRow;
            }
            else
            {
                iRet.Quantity = Cid.ToString();
            }

            string OptData = "<option value=''>Select Item</option>";
            foreach (DataRow dr in dtPrd.Rows)
            {
                OptData += "<option value='" + dr["Id"].ToString() + "'>" + dr["Name"].ToString() + "</option>";
            }
            iRet.OptionData = OptData;
            return iRet;
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string GetQboItemData()
        {
            try
            {
                string _Cid = HttpContext.Current.Session["CompanyID"].ToString();
                string query = "Select * from Items where IsDeleted=0 and CompanyId='" + _Cid + "' order by Name asc; ";
                Database db = new Database();
                db.Open();
                DataTable dtPrd = new DataTable();
                db.Execute(query, out dtPrd);
                db.Close();
                string OptionData = "<option value=''>Select Item</option>";
                foreach (DataRow dr in dtPrd.Rows)
                {
                    OptionData += "<option value='" + dr["Id"].ToString() + "'>" + dr["Name"].ToString() + "</option>";
                }
                return OptionData;
            }
            catch (Exception ex) { return ex.Message; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static QboCommon GetTaxRateById(string pId = "")
        {
            if (pId == "")
                pId = "0";

            string query = " Select Name, Rate from Taxes where Id='" + pId + "' and CompanyId='" + HttpContext.Current.Session["CompanyID"].ToString() + "'";

            Database db = new Database();
            db.Open();
            DataTable dtPrd = new DataTable();
            db.Execute(query, out dtPrd);
            db.Close();

            QboCommon iRet = new QboCommon();
            iRet.Rate = "0";
            if (dtPrd.Rows.Count > 0)
            {
                DataRow dr = dtPrd.Rows[0];
                iRet.Rate = dr["Rate"].ToString();
            }
            return iRet;
        }
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static QboCommon GetProductById(string pId = "")
        {
            string query = "Select * from Items where  IsDeleted=0 and Id = '" + pId + "' and CompanyId='" + HttpContext.Current.Session["CompanyID"].ToString() + "' order by Name asc; ";
            Database db = new Database();
            db.Open();
            DataTable dtPrd = new DataTable();
            db.Execute(query, out dtPrd);
            db.Close();

            QboCommon iRet = new QboCommon();
            if (dtPrd.Rows.Count > 0)
            {
                DataRow dr = dtPrd.Rows[0];
                iRet.Description = dr["Description"].ToString();
                iRet.Quantity = "1";
                iRet.Rate = dr["Price"].ToString();
                iRet.Amount = dr["Price"].ToString();
                iRet.Taxable = dr["IsTaxable"].ToString();
            }
            return iRet;
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string InvoiceSubmit(string pId = "",
            string pName = "",
            string sDate = "",
            string pQty = "",
            string AllTaxable = "",
            string pRate = "",
            string pDes = "",
            string CustomerId = "",
            string CustomerQboID = "",
            string qNum = "",
            string subTotal = "",
            string discountOpt = "",
            string disRt = "",
            string disAmt = "",
            string TaxTp = "",
            string taxAmt = "",
            string TotalAmount = "",
            string _ExpirationDate = "",
            string _Date = "", string _Note = "", string _CompanyId = "",
            float _Paid = 0, string _Indicator = "", string _AppointmentID = "",
            string InvoiceID = "", string _QboId = "", bool isNewTotal = false,

            string reqAmount = "", string requestedAmtType = "")
        {
            CustomerProcessor customerProcessor = new CustomerProcessor();
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = CustomerId }))
            {
                CustomerId = customerProcessor.GetCustomerByGuid(CustomerId, CompanyID).CustomerID;
            }
            else
            {
                return "Invalid Customer data.";
            }

            // Clean all inputs
            pId = Common.CleanInput(pId);
            pName = Common.CleanInput(pName);
            sDate = Common.CleanInput(sDate);
            pQty = Common.CleanInput(pQty);
            AllTaxable = Common.CleanInput(AllTaxable);
            pRate = Common.CleanInput(pRate);
            pDes = Common.CleanInput(pDes);
            CustomerId = Common.CleanInput(CustomerId);
            qNum = Common.CleanInput(qNum);
            subTotal = Common.CleanInput(subTotal);
            discountOpt = Common.CleanInput(discountOpt);
            disRt = Common.CleanInput(disRt);
            disAmt = Common.CleanInput(disAmt);
            taxAmt = Common.CleanInput(taxAmt);
            TotalAmount = Common.CleanInput(TotalAmount);
            _Date = Common.CleanInput(_Date);
            _Note = Common.CleanInput(_Note);
            _CompanyId = Common.CleanInput(_CompanyId);
            _Indicator = Common.CleanInput(_Indicator);
            _AppointmentID = Common.CleanInput(_AppointmentID);
            InvoiceID = Common.CleanInput(InvoiceID);
            _QboId = Common.CleanInput(_QboId);
            reqAmount = Common.CleanInput(reqAmount);
            requestedAmtType = Common.CleanInput(requestedAmtType);

            if (string.IsNullOrEmpty(_QboId)) _QboId = "0";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            DateTime expirationDate = DateTime.Now.AddDays(7);
            try { expirationDate = Convert.ToDateTime(_ExpirationDate); } catch { }

            try
            {
                string[] _PID = pId.Split('@');
                string[] _PName = pName.Split('@');
                string[] _Qty = pQty.Split('@');
                string[] _Rate = pRate.Split('@');
                string[] _Desc = pDes.Split('@');
                string[] _SDate = sDate.Split('@');
                string[] _AllTaxable = AllTaxable.Split('@');

                if (_PID.Length == 0 || string.IsNullOrEmpty(pId))
                {
                    return "At least one item needs to be added.";
                }
                string QboInvoiceId = "0";
                Database db = new Database(connStr);
                if (string.IsNullOrEmpty(disAmt)) disAmt = "0";
                if (string.IsNullOrEmpty(taxAmt)) taxAmt = "0";
                if (string.IsNullOrEmpty(TaxTp)) taxAmt = "0";
                if (string.IsNullOrEmpty(reqAmount)) reqAmount = "0";
                if (string.IsNullOrEmpty(requestedAmtType)) requestedAmtType = "1";

                double _Tax = Convert.ToDouble(taxAmt);
                taxAmt = _Tax.ToString("0.00");
                double tL = (Convert.ToDouble(subTotal) + _Tax) - (Convert.ToDouble(disAmt));

                decimal depoAmt = decimal.Parse(reqAmount);
                int depoType = int.Parse(requestedAmtType);

                DateTime InvoiceDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59, 999);

                string sql = @"begin ";
                bool IsExisting = false;
                if (string.IsNullOrEmpty(InvoiceID))
                {
                    InvoiceID = Guid.NewGuid().ToString().ToUpper();
                    // UPDATED INSERT Statement
                    sql += @"INSERT INTO tbl_Invoice (ID, CompnyID, TaxType, Number, CustomerId, Note, UserId, Subtotal, DiscountOption, DiscountRate, Discount, Tax, Total, Status, InvoiceDate, ExpirationDate, InvoiceType, CreatedDate, CreatedBy, AmountCollect, Type, AppointmentId, QboId, RequestedDepoAmt, ReqDepoPercent, RequestedAmtType) VALUES 
                     ('" + InvoiceID + "','" + _CompanyId + "','" + TaxTp + "','" + qNum + "','" + CustomerId + "','" + _Note.Replace("'", "''") + "','" + HttpContext.Current.Session["LoginUser"].ToString() + "','"
                             + subTotal + "','" + discountOpt + "','" + disRt + "','" + disAmt + "','" + taxAmt + "','" + tL + "','1','" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "','" + expirationDate.ToString("yyyy-MM-dd h:mm:ss tt") + "','','"
                             + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "','CEC'," + _Paid + ",'" + _Indicator + "','" + _AppointmentID + "'," + QboInvoiceId + ","
                             + (depoType == 2 ? depoAmt : 0) + "," // RequestedDepoAmt ($)
                             + (depoType == 1 ? depoAmt : 0) + "," // ReqDepoPercent (%)
                             + depoType + ");"; // RequestedAmtType
                }
                else
                {
                    IsExisting = true;
                    // UPDATED UPDATE Statement
                    sql += @"UPDATE tbl_Invoice SET Subtotal='" + subTotal + "', TaxType='" + TaxTp + "', Note='" + _Note.Replace("'", "''") + "', DiscountOption='" + discountOpt + "', DiscountRate='" + disRt + "', Discount='" + disAmt + "', Tax='" + taxAmt + "', Total='" + tL + "', InvoiceDate='" + _Date + "', "
                         + "RequestedDepoAmt = " + (depoType == 2 ? depoAmt : 0) + ", "
                         + "ReqDepoPercent = " + (depoType == 1 ? depoAmt : 0) + ", "
                         + "RequestedAmtType = " + depoType
                         + " WHERE CompnyID = '" + _CompanyId + "' AND ID='" + InvoiceID + "';";
                    sql += @"DELETE FROM tbl_InvoiceDetails WHERE Refid = '" + InvoiceID + "' AND companyid='" + _CompanyId + "';";
                }

                for (int j = 0; j < _PID.Length - 1; j++)
                {
                    decimal _TLPrice = Convert.ToDecimal(_Rate[j]) * Convert.ToDecimal(_Qty[j]);
                    sql += @"INSERT INTO tbl_InvoiceDetails (companyid, RefId, ItemId, LineNum, ItemName, Description, ServiceDate, Quantity, uPrice, TotalPrice, IsTaxable, ItemTyId, CreatedDate, CreatedBy) VALUES
                     ('" + HttpContext.Current.Session["CompanyID"].ToString() + "','" + InvoiceID + "','" + _PID[j] + "','" + (j + 1) + "','" + _PName[j].Replace("'", "''") + "','" + _Desc[j].Replace("'", "''") + "','" + _SDate[j] + "','" + _Qty[j] + "','" + _Rate[j] + "','" + _TLPrice + "','"
                             + _AllTaxable[j].Trim().Replace("No", "NON") + "','','" + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "','');";
                }

                sql += " end;"; // Use "end" instead of "return end" for better compatibility
                db.Execute(sql);
                db.Close();

                try
                {
                    var invSvc = new FSM.Processors.TPMInvoiceService();
                    int woId = 0;
                    if (!string.IsNullOrEmpty(_AppointmentID))
                    {
                        var woProc = new FSM.Processors.WorkOrderProcessor();
                        var wo = woProc.GetByAppointmentId(_CompanyId, int.Parse(_AppointmentID));
                        woId = wo.Id;
                    }
                    if (woId > 0)
                    {
                        invSvc.CreateTpmInvoiceRecord(_CompanyId, woId, InvoiceID, qNum, decimal.Parse(subTotal), decimal.Parse(taxAmt), Convert.ToDecimal(tL),
                            HttpContext.Current.Session["LoginUser"]?.ToString());
                        invSvc.SyncToQbo(_CompanyId, InvoiceID, "0", Convert.ToDecimal(tL), qNum);
                    }
                }
                catch { }

                return "Data saved successfully.";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static QboCommon GetDepositsById(string iId)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sql = @"SELECT [Type], [Source], [CheckName], [CheckNumber], [Amount], CONVERT(varchar, createdDate, 101) as PaymentDate 
                   FROM tbl_Payment 
                   WHERE InvocieId = '" + iId + "' AND CompanyId = '" + CompanyID + @"' 
                   ORDER BY createdDate DESC";

      
            DataSet dataSet = db.Get_DataSet(sql, CompanyID);
            DataTable dt = dataSet.Tables[0];

            StringBuilder tableRows = new StringBuilder();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    tableRows.Append("<tr>");
                    tableRows.Append("<td>" + HttpUtility.HtmlEncode(dr["Type"]) + "</td>");
                    tableRows.Append("<td>" + HttpUtility.HtmlEncode(dr["Source"]) + "</td>");
                    tableRows.Append("<td>" + HttpUtility.HtmlEncode(dr["CheckName"]) + "</td>");
                    tableRows.Append("<td>" + HttpUtility.HtmlEncode(dr["CheckNumber"]) + "</td>");
                    tableRows.Append("<td>$" + Convert.ToDecimal(dr["Amount"]).ToString("N2") + "</td>");
                    tableRows.Append("<td>" + HttpUtility.HtmlEncode(dr["PaymentDate"]) + "</td>");
                    tableRows.Append("</tr>");
                }
            }
            else
            {
                tableRows.Append("<tr><td colspan='6' class='text-center'>No payments or deposits found for this invoice.</td></tr>");
            }

            return new QboCommon { TableRow = tableRows.ToString() };
        }

        public static string GetJobsCustomerId(string _Id, string _CompanyID)
        {
            Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());
            string Sql = "select QboId,CustomerID,FirstName,LastName from tbl_Customer where customerid='" + _Id + "' and CompanyID='" + _CompanyID + "'";
            DataTable dt;
            db.Execute(Sql, out dt);
            db.Close();
            DataRow dr = dt.Rows[0];
            Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
            dbJobs.Open();
            string fName = dr["FirstName"].ToString() + " " + dr["LastName"].ToString();
            Sql = "select Id from Customers where Name='" + fName + "' and CompanyId='" + _CompanyID + "'";
            DataTable dtJobs = new DataTable();
            dbJobs.Execute(Sql, out dtJobs);
            dbJobs.Close();
            if (dtJobs.Rows.Count > 0)
                return dtJobs.Rows[0]["Id"].ToString();

            return "";
        }
        public static string GetJobsProductId(string _Id, string _CompanyID)
        {
            Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
            dbJobs.Open();
            string Sql = "select Id,Name from Items where Id='" + _Id + "' and CompanyId='" + _CompanyID + "' order by Name asc;";
            DataTable dtJobs = new DataTable();
            dbJobs.Execute(Sql, out dtJobs);
            dbJobs.Close();
            if (dtJobs.Rows.Count > 0)
                return dtJobs.Rows[0]["Id"].ToString();
            return "";
        }
        private static string GetJobsUsertId(string _Id)
        {
            Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
            dbJobs.Open();
            string Sql = "select * from Users where username='" + _Id + "'";
            DataTable dtJobs = new DataTable();
            dbJobs.Execute(Sql, out dtJobs);
            dbJobs.Close();
            if (dtJobs.Rows.Count > 0)
                return dtJobs.Rows[0]["Id"].ToString();
            return "";
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string ResolveCustomerGuidFromWorkOrder(string companyId, string workOrderId)
        {
            int woId;
            if (!int.TryParse(workOrderId, out woId) || woId <= 0)
                return "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            using (var con = new SqlConnection(connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 c.CustomerGuid
                      FROM [msSchedulerV3].[dbo].[tbl_WorkOrders] wo
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_Appointment] a ON a.ApptID = wo.AppointmentId AND a.CompanyID = wo.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON c.CustomerID = a.CustomerID AND c.CompanyID = a.CompanyID
                      WHERE wo.CompanyID = @CompanyID AND wo.Id = @WorkOrderId", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WorkOrderId", woId);
                return cmd.ExecuteScalar()?.ToString() ?? "";
            }
        }
    }

    public class QBOSettins
    {
        public string AccessToken = "";
        public string ConnectedCompanyName = "";
        public string RefreshToken = "";
        public string FileID = "";
        public bool isQBConnected = false;
    }
}