
using FSM.Entity.Customer;
using FSM.Helper;
using FSM.Models.LoginModels;
using FSM.Processors;
using Intuit.Ipp.Core;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using TPM;

namespace TPM
{
    public partial class Invoice : System.Web.UI.Page
    {
     
        private static Random random = new Random();
        private string CompanyGUID = "";
        private string CompanyID = "";
        private string CompanyName = "";
        private bool IsQuickBookEnabled = false;
        private string Qut_Number = "";
        private StringBuilder table = new StringBuilder();
        private string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        protected void Page_Load(object sender, EventArgs e)
        {

             if (Session["CompanyID"] == null)
                Response.Redirect("logout.aspx");

            if (Page.IsPostBack == false)
            {

                QBOManager qBOManager = new QBOManager();


                hd_IsQuickBookEnabled.Value = "0";


                LoginObject _loginObject = (LoginObject)Session["LoginObj"];
                btnMMS.Visible = _loginObject.IsMMSAllowed;

                if (Session["CompanyID"] != null) CompanyID = Session["CompanyID"].ToString();
                if (Session["CompanyName"] != null) CompanyName = Session["CompanyName"].ToString();
                if (Session["CompanyGUID"] != null) CompanyGUID = Session["CompanyGUID"].ToString();

                hdCompanyID.Value = CompanyID;
                hdCompanyName.Value = CompanyName;
                hdCompanyGUID.Value = CompanyGUID;
                string _CustomerId = Request.Params["cId"];
                string InvoiceNumber = Request.Params["InvoiceNumber"];
                string _InvGuidId = Request.Params["InvNum"];

                _CustomerId = Common.CleanInput(_CustomerId);

                CustomerProcessor customerProcessor = new CustomerProcessor();

                if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = _CustomerId }))
                {
                }
                else
                {
                    string exception = " Swal.fire('Invalid Customer data.', '', 'Successfully');window.location.href='Dashboard.aspx';";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
                    return;
                }

                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string InvId = "0";
                Database db = new Database(connStr);
                if (!string.IsNullOrEmpty(InvoiceNumber))
                {
                    InvId = db.ExecuteScalar("select ID from tbl_invoice where Number='" + InvoiceNumber + "' and CompnyID='" + CompanyID + "'");

                }

                SV_CustomeID.Value = _CustomerId;
                // Indicator.Value = Request.Params["InType"];
                Indicator.Value = Common.CleanInput(Request.Params["InType"]);
                AppointmentID.Value = Common.CleanInput(Request.Params["AppID"]);
                btn_ConvertToInvocie.Visible = false;

                ViewState["invId"] = _InvGuidId;
                if (InvId != "0" && (_InvGuidId == null || _InvGuidId == "0"))
                {
                    _InvGuidId = InvId;
                    ViewState["invId"] = InvId;
                }
                ViewState["custId"] = _CustomerId;
                ViewState["invType"] = Request.Params["InType"];
                ViewState["appID"] = Request.Params["AppID"];

                FromInvList.Value = Request.Params["FromInvList"];
                FromInvoices.Value = Request.Params["FromInvoices"];
                FromCustomer.Value = Request.Params["FormType"];
                FormType.Value = Request.Params["FromCustomer"];


                if (Request.Params["InType"] == "Proposal" && _InvGuidId != null && _InvGuidId != "0")
                {
                    PageTitle.Text = " Proposal/Estimate";
                    btn_ConvertToInvocie.Visible = true;
                    div_Expirationdate.Visible = true;
                    //reqDepSec.Visible = true;
                }

                else if (Request.Params["InType"] == "Estimate" && _InvGuidId != null && _InvGuidId != "0")
                {
                    PageTitle.Text = " Proposal/Estimate";
                    btn_ConvertToInvocie.Visible = true;
                    div_Expirationdate.Visible = true;
                    //reqDepSec.Visible = true;
                }
                else
                {
                    div_Expirationdate.Visible = false;
                    PageTitle.Text = "Invoice";
                    btn_ConvertToInvocie.Visible = false;
                    //reqDepSec.Visible = false;
                }
                if (Request.Params["InType"] == "Proposal" || Request.Params["InType"] == "Estimate")
                {
                    PageTitle.Text = "Proposal/Estimate";
                    div_Expirationdate.Visible = true;
                    // Added By Munem
                    btnSendProposalMail.Visible = true;
                    btnSendInvoiceMail.Visible = false;
                    //end
                    // reqDepSec.Visible = true;
                }
                else
                {
                    div_Expirationdate.Visible = false;
                    PageTitle.Text = "Invoice";
                    //Added By Munem
                    btnSendProposalMail.Visible = false;
                    btnSendInvoiceMail.Visible = true;
                    //End
                    // reqDepSec.Visible = false;
                }

                Qut_Number = _InvGuidId;

                if (Qut_Number == "0" || Qut_Number == "Proposal" || Qut_Number == "Invoice" || Qut_Number == "" || Qut_Number == null)
                {
                    Qut_Number = string.Empty;
                }

                HdInvoiceId.Value = "";
                HdQboId.Value = "0";

                Load_InitialData(Qut_Number, _CustomerId);


                if (_InvGuidId != null && _InvGuidId != "0")
                {
                    _InvoiceNo.Value = _InvGuidId;
                }
                if (Request.Params["InType"] != null && Request.Params["InType"].ToString() == "Invoice")
                {
                    _Type.Value = Request.Params["InType"].ToString();

                    btnSendInvoiceMail.Visible = true;
                    btnSendProposalMail.Visible = false;
                }
                if (Request.Params["InType"] != null && Request.Params["InType"].ToString() == "Proposal")
                {
                    _Type.Value = Request.Params["InType"].ToString();

                    btnSendInvoiceMail.Visible = false;
                    btnSendProposalMail.Visible = true;
                }
                if (Session["IsWisetackEnabled"] != null)
                {
                    wisetackSignup.Visible = false;
                }
                else
                {
                    wisetackSignup.Visible = false;
                }

                if (Session["CanAccessInvoice"] != null)
                {
                    if (!Convert.ToBoolean(Session["CanAccessInvoice"]))
                    {
                        btn_ConvertToInvocie.Visible = false;
                        _SubmitInvoice.Visible = false;
                        invPermission.Visible = true;
                    }
                    else
                    {
                        invPermission.Visible = false;
                    }
                }
                else
                {
                    btn_ConvertToInvocie.Visible = false;
                    _SubmitInvoice.Visible = false;
                    invPermission.Visible = true;
                }
                if (Session["isShowWisetack"] == null)
                {
                    wisetackSignup.Visible = false;
                }
                if (Session["CompanyType"].ToString() == "LHG")
                {
                    isLHG.Value = true.ToString();
                }
                if (Session["CompanyType"].ToString() == "LHG")
                {
                    isLHG.Value = true.ToString();
                }
                if (Session["IsPCS"] != null && (bool)Session["IsPCS"])
                {
                    IsPCS.Value = "True";
                    divLocation.Visible = true;
                    divClass.Visible = true;
                }
                else
                {
                    IsPCS.Value = "False";
                    divLocation.Visible = false;
                    divClass.Visible = false;
                }
                Database dbJob = new Database(connStr);
                string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                string reqDepoAmtDollar = dbJob.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
                decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

                txtDepoReq.Text = reqAmt.ToString();

                if (Session["CompanyType"].ToString() == "LHG")
                {
                    invImg.Src = "images/LHGInvoice.png";
                    estImg.Src = "images/LHGEstimate.png";
                }


                //   string _Query = @" SELECT tbl_Invoice.id as InvoiceID,tbl_Customer.CustomerGuid,tbl_Customer.FirstName + ' ' +tbl_Customer.LastName as FullName, dbo.tbl_Invoice.InvoiceType,
                //                  tbl_Customer.qboid as QBOCustomerId,dbo.tbl_Invoice.CustomerId, isnull(tbl_Invoice.DepositAmount,0.00) as DepositAmount, dbo.tbl_Invoice.qboid,
                //                  dbo.tbl_Customer.City, dbo.tbl_Invoice.Number,CONVERT(VARCHAR(10), tbl_Invoice.InvoiceDate, 101) as InvoiceDate,
                //                  dbo.tbl_Invoice.Subtotal, dbo.tbl_Invoice.[IsConverted],dbo.tbl_Invoice.[ConvertedInvocieID], isNULL(tbl_Invoice.RequestedDepoAmt,0)reqDep,
                //                  (tbl_Invoice.Total-tbl_Invoice.AmountCollect)as Due,tbl_Invoice.InvoiceDate,dbo.tbl_Invoice.AmountCollect,dbo.tbl_Invoice.Discount, dbo.tbl_Invoice.Total, dbo.tbl_Invoice.Tax, 
                //                  dbo.tbl_Invoice.Status,dbo.tbl_Invoice.Type  FROM [msSchedulerV3].dbo.tbl_Customer JOIN
                //                  [msSchedulerV3].dbo.tbl_Invoice ON dbo.tbl_Customer.CustomerID = dbo.tbl_Invoice.CustomerId AND dbo.tbl_Customer.CompanyID = dbo.tbl_Invoice.CompnyID

                //where  tbl_Invoice.[CompnyID] = '" + CompanyID + "' and tbl_Invoice.[ID]= '" + _InvoiceNo.Value + "'";

                //   string Status = "";
                //   Database db = new Database(connStr);
                //   DataTable dt = new DataTable();
                //   DataSet dataSet = db.Get_DataSet(_Query, Session["CompanyID"].ToString());
                //   dt = dataSet.Tables[0];

                //   if (dt.Rows.Count > 0)
                //   {
                //       if (Convert.ToDouble(dt.Rows[0]["Total"]) - Convert.ToDouble(dt.Rows[0]["AmountCollect"]) <= 0)
                //       {
                //           Status = "Paid";
                //       }
                //       if (Convert.ToBoolean(dt.Rows[0]["IsConverted"].ToString()))
                //       {
                //           Status = "Converted To Invoice";
                //       }
                //       else
                //       {
                //           Status = "Unpaid";
                //       }

                //       string invId = dt.Rows[0]["InvoiceID"].ToString();
                //       string custId = dt.Rows[0]["CustomerID"].ToString();
                //       string QboId = dt.Rows[0]["QboId"].ToString();
                //       string QBOCustomerId = dt.Rows[0]["QBOCustomerId"].ToString();
                //       string depositAmt = dt.Rows[0]["DepositAmount"].ToString();
                //       decimal dueAmount = Convert.ToDecimal(dt.Rows[0]["Due"]);
                //       string number = dt.Rows[0]["Number"].ToString();
                //       paymentButtonArea.InnerHtml = LoadPaymentButtonArea(invId, custId, QboId, QBOCustomerId, depositAmt, dueAmount, Status, number);
                //   }
            }
            else
            {
                Database dbJob = new Database(connStr);
                string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "'");
                string reqDepoAmtDollar = dbJob.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
                decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

                if (txtDepoReq.Text == "" || txtDepoReq.Text == "0") txtDepoReq.Text = reqAmt.ToString();
            }
        }


        [WebMethod(EnableSession = false)]
        public static string ConvertInvoice(string invNo = "")
        {
            try
            {
                string _CmpId = HttpContext.Current.Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                Database db = new Database(connStr);
                //  db.Open();
                string sql = "";

                sql = "select A.*,isnull(QboId,0) QboIds from tbl_Invoice A where A.CompnyID='" + _CmpId + "' and A.id = '" + invNo + "' ";
                DataTable dtMast;
                db.Execute(sql, out dtMast);

                string sqlPay = "select * from tbl_Payment where invocieid = '" + invNo + "'";
                DataTable dtPay;
                DataRow dr = null;
                db.Execute(sqlPay, out dtPay);


                if (dtMast.Rows.Count > 0) dr = dtMast.Rows[0];



                sql = "select * from tbl_InvoiceDetails where RefId = '" + invNo + "'  and companyid ='" + HttpContext.Current.Session["CompanyID"].ToString() + "'";
                DataTable dtDet;
                db.Execute(sql, out dtDet);
                string pId = ""; string pName = ""; string pQty = ""; string AllTaxable = ""; string pRate = ""; string pDes = ""; string sDate = ""; string spTag = "";
                int i = 0;

                foreach (DataRow _DataRow in dtDet.Rows)
                {
                    if (i > 0)
                        spTag = "@";
                    i += 1;
                    pId += spTag + _DataRow["ItemId"].ToString().Trim();
                    pName += spTag + _DataRow["ItemName"].ToString().Trim();
                    pQty += spTag + _DataRow["Quantity"].ToString().Trim();
                    AllTaxable += spTag + _DataRow["IsTaxable"].ToString().Trim();
                    pRate += spTag + _DataRow["uPrice"].ToString().Trim();
                    var pDesc = "n/a";
                    if (!string.IsNullOrEmpty(_DataRow["Description"].ToString()))
                    {
                        pDesc = _DataRow["Description"].ToString();
                    }

                    pDes += spTag + pDesc.Trim();// dr["Description"].ToString().Trim();
                    sDate += spTag + _DataRow["ServiceDate"].ToString().Trim();
                }

                string[] _PID = pId.Split('@');
                string[] _PName = pName.Split('@');
                string[] _Qty = pQty.Split('@');
                string[] _Rate = pRate.Split('@');
                string[] _Desc = pDes.Split('@');
                string[] _SDate = sDate.Split('@');
                string[] _AllTaxable = AllTaxable.Split('@');

                string InvoiceID = Guid.NewGuid().ToString().ToUpper();

                string newInvoiceNumber = getNextNumber(true);
                // For Estimate Data
                sql = @" Update tbl_Invoice set ConvertedInvocieNumber='" + newInvoiceNumber + "',ConvertedInvocieID ='" + InvoiceID + "', IsConverted = 1 where id = '" + invNo + "';";

                // For Invoice Table DaTa
                sql += @"Insert Into  [msSchedulerV3].[dbo].[tbl_Invoice]
                                    ([ID]
                                          ,[Number]
                                          ,[CompanyID]
                                          ,[CompnyID]
                                          ,[DisplayNumber]
                                          ,[CustomerId]
                                          ,[UserId]
                                          ,[Subtotal]
                                          ,[Discount]
                                          ,[Tax]
                                          ,[Total]
                                          ,[Status]
                                          ,[InvoiceType]
                                          ,[ModifiedDate]
                                          ,[ModifiedBy]
                                          ,[Note]
                                          ,[LoanStatus]
                                          ,[CreatedDate]
                                          ,[CreatedBy]
                                          ,[InvoiceDate]
                                          ,[AmountCollect]
                                          ,[TaxType]
                                          ,[AppointmentId]
                                          ,[Type]
                                          ,[IsConverted]
                                          ,[ConvertedInvocieID]
                                          ,[QboId]
                                          ,[DiscountRate]
                                          ,[DiscountOption]
                                          ,[QboEstimateId]
                                          ,[ExpirationDate]
                                          ,[SyncToken]
                                          ,[QboPaymentID]
                                          ,[DepositAmount] 
                                          ,RequestedDepoAmt
                                          ,ReqDepoPercent
                                          ,RequestedAmtType)
                                      SELECT '" + InvoiceID + @"'
                                          ,'" + newInvoiceNumber + @"'
                                          ,[CompanyID]
                                          ,[CompnyID]
                                          ,[DisplayNumber]
                                          ,[CustomerId]
                                          ,[UserId]
                                          ,[Subtotal]
                                          ,[Discount]
                                          ,[Tax]
                                          ,[Total]
                                          ,[Status]
                                          ,[InvoiceType]
                                          ,GETDATE()
                                          ,[ModifiedBy]
                                          ,[Note]
                                          ,[LoanStatus]
                                          ,GETDATE()
                                          ,[CreatedBy]
                                          ,[InvoiceDate]
                                          ,[AmountCollect]
                                          ,[TaxType]
                                          ,[AppointmentId]
                                          ,'Invoice'
                                          ,'0'
                                          ,''
                                          ,'0'
                                          ,[DiscountRate]
                                          ,[DiscountOption]
                                          ,'0'
                                          ,[ExpirationDate]
                                          ,'0'
                                          ,'0'
                                          ,[DepositAmount]
                                          ,RequestedDepoAmt
                                          ,ReqDepoPercent
                                          ,RequestedAmtType 
                                      FROM [msSchedulerV3].[dbo].[tbl_Invoice] where id = '" + invNo + "';";

                // For Invocie Detail Table Data
                sql += @"Insert INTO  [msSchedulerV3].[dbo].[tbl_InvoiceDetails]
                                        (
                                        [RefId]
                                        ,[companyid]
                                        ,[InvoiceId]
                                        ,[InvoiceNumber]
                                        ,[ItemId]
                                        ,[Quantity]
                                        ,[uPrice]
                                        ,[ItemName]
                                        ,[Description]
                                        ,[CreatedBy]
                                        ,[ModifiedDate]
                                        ,[ModifiedBy]
                                        ,[TotalPrice]
                                        ,[createddate]
                                        ,[ItemTyId]
                                        ,[IsTaxable]
                                        ,[ServiceDate]
                                        )
                                        SELECT '" + InvoiceID + @"'
                                              ,[companyid]
                                              ,[InvoiceId]
                                              ,[InvoiceNumber]
                                              ,[ItemId]
                                              ,[Quantity]
                                              ,[uPrice]
                                              ,[ItemName]
                                              ,[Description]
                                              ,[CreatedBy]
                                              ,GETDATE()
                                              ,[ModifiedBy]
                                              ,[TotalPrice]
                                              ,GETDATE()
                                              ,[ItemTyId]
                                              ,[IsTaxable]
                                              ,[ServiceDate]
                                          FROM [msSchedulerV3].[dbo].[tbl_InvoiceDetails] where RefId = '" + invNo + "';";

                db.Execute(sql);



                foreach (DataRow drPay in dtPay.Rows)
                {
                    string sqlForInsertPay = @"INSERT INTO [dbo].[tbl_Payment] " +
           "([Companyid],[InvocieId],[Amount],[CheckName],[CheckNumber],[Type],[IsDeposit],[Source],[createdDate],[QboId],[PaymentRefNum],[RMPaymentId]) VALUES (" + "'" + (_CmpId ?? "") + "', " + "'" + (InvoiceID ?? "") + "', " +
           (drPay["Amount"] != DBNull.Value ? Convert.ToDecimal(drPay["Amount"]).ToString() : "0") + ", " +
           "'" + (drPay["CheckName"] != DBNull.Value ? drPay["CheckName"].ToString().Replace("'", "''") : "") + "', " +
           "'" + (drPay["CheckNumber"] != DBNull.Value ? drPay["CheckNumber"].ToString().Replace("'", "''") : "") + "', " +
           "'" + (drPay["Type"] != DBNull.Value ? drPay["Type"].ToString().Replace("'", "''") : "") + "', " +
           (drPay["IsDeposit"] != DBNull.Value ? Convert.ToBoolean(drPay["IsDeposit"]) ? "1" : "0" : "0") + ", " +
           "'" + (drPay["Source"] != DBNull.Value ? drPay["Source"].ToString().Replace("'", "''") + " From Estimate Deposit" : "From Estimate Deposit") + "', " +
           (drPay["createdDate"] != DBNull.Value ? "'" + Convert.ToDateTime(drPay["createdDate"]).ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL") + ", " +
           "0, " +
           "'" + (drPay["PaymentRefNum"] != DBNull.Value ? drPay["PaymentRefNum"].ToString().Replace("'", "''") : "") + "', " +
           (drPay["RMPaymentId"] != DBNull.Value ? Convert.ToInt32(drPay["RMPaymentId"]).ToString() : "NULL") +
           ");";
                    db.Execute(sqlForInsertPay);

                }



                //end

                string QboInvoiceId = "0";

                //try
                //{
                //    QBOSettins qBoStngPost = new QBOSettins();
                //    QBOManager qBOManager = new QBOManager();
                //    if (qBOManager.VerifyCompanySetting(_CmpId, ref qBoStngPost))
                //    {
                //        // QBOManager.RemoveEstimate(qBoStngPost, dr["QboEstimateId"].ToString());
                //        string SyncToken = "";
                //        string QboPaymentID = "0";

                //        string Status = "0";
                //        if ((Convert.ToDouble(dr["Total"].ToString()) - Convert.ToDouble(dr["AmountCollect"].ToString())) <= 0)
                //        {
                //            Status = "1";
                //        }

                //        ServiceContext serviceContext = null;
                //        string qboClassId = dr["QboClassId"] != DBNull.Value ? dr["QboClassId"].ToString() : "0";
                //        string qboLocationId = dr["QboLocationId"] != DBNull.Value ? dr["QboLocationId"].ToString() : "0";

                //        // Only send QboClassId and QboLocationId if IsPCS is true
                //        string qboClassIdForQbo = "0";
                //        string qboLocationIdForQbo = "0";
                //        if (HttpContext.Current.Session["IsPCS"] != null && (bool)HttpContext.Current.Session["IsPCS"])
                //        {
                //            qboClassIdForQbo = qboClassId;
                //            qboLocationIdForQbo = qboLocationId;
                //        }

                //        QboInvoiceId = qBOManager.CreateInvoiceQbo(serviceContext, ref QboPaymentID, ref SyncToken, Status, dr["QboEstimateId"].ToString(), qBoStngPost, pId, pName, pDes, sDate, pQty, pRate, AllTaxable, dr["CustomerId"].ToString(), dr["InvoiceDate"].ToString(),
                //            dr["Note"].ToString(), dr["DiscountOption"].ToString(), dr["DiscountRate"].ToString(), dr["Discount"].ToString(),
                //            dr["TaxType"].ToString(), dr["Tax"].ToString(), dr["Total"].ToString(), newInvoiceNumber, "0", qboClassIdForQbo, qboLocationIdForQbo);
                //        if (QboInvoiceId != "0")
                //        {
                //            sql = "update tbl_Invoice set QboId='" + QboInvoiceId + "',SyncToken='" + SyncToken + "' " +
                //                " where id='" + InvoiceID + "'" +
                //                " and CompnyID='" + _CmpId + "'; ";

                //            db.Execute(sql);
                //        }
                //    }
                //    else
                //    {
                //        // sql = @" Update tbl_Invoice set Type = 'Invoice',Number = '" + InvoiceID + "' where id = '" + invNo + "';";
                //        sql = @" Update tbl_Invoice set Type = 'Proposal' where id = '" + invNo + "';";
                //        db.Execute(sql);
                //    }

                //    //Mohsin
                //    if (qBOManager.VerifyCompanySetting(_CmpId, ref qBoStngPost))
                //    {
                //        ServiceContext serviceContext = null;
                //        qBOManager.SyncPaymentToQBO(serviceContext, qBoStngPost, _CmpId);
                //    }
                //    //end
                //}
                //catch (Exception ex) { }

                db.Close();


                return "Proposal has been converted to Invoice #" + newInvoiceNumber;
            }
            catch (Exception ex) { return ex.Message; }
        }

        [WebMethod(EnableSession = false)]
        public static QboCommon GetInvoiceDetailsById(string iId = "")
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            SqlConnection conn = new SqlConnection(connStr);

            string sql = "Select Id,Name from Items  where IsDeleted=0  and CompanyId=@CompanyID order by Name asc; ";
            Database db = new Database(connStr);
            DataTable dtPrd = new DataTable();

            sql += @"select ItemId,ItemName,Description,Quantity,uPrice,ServiceDate,IsTaxable   from tbl_InvoiceDetails where RefId = '" + iId + "' and companyid =@CompanyID order by CAST(NULLIF(LineNum,'') AS INT) asc ;";
            DataTable dt_InvoiceItems = new DataTable();

            sql += @"select INVM.DiscountOption,INVM.DiscountRate,INVM.Discount,INVM.TaxType,Tx.Rate,INVM.Tax,INVM.Total,INVM.Note,INVM.ReqDepoPercent,INVM.RequestedAmtType,ISNULL(INVM.QboClassId,0) as QboClassId,ISNULL(INVM.QboLocationId,0) as QboLocationId
            from tbl_Invoice INVM left join  Taxes Tx on  INVM.TaxType = tx.Id
            where INVM.id='" + iId + "' and INVM.CompnyID =@CompanyID";
            DataTable dt_Invoice = new DataTable();

            DataSet dataSet = db.Get_DataSet(sql, CompanyID);
            dtPrd = dataSet.Tables[0];
            dt_InvoiceItems = dataSet.Tables[1];
            dt_Invoice = dataSet.Tables[2];

            QboCommon iRet = new FSM.Helper.QboCommon();
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
                NewRow += "<td><input id='id_Tax" + Cid + "' type='checkbox' class='idInput itmTax'" + txChecked + " onchange='TaxRecal()'/></td>";

                NewRow += "<td><a href='javascript: void(0);' onclick='removeThis(this)' title='Del'>Del</a></td>";
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
                iRet.ReqDepoPercent = drInv["ReqDepoPercent"].ToString();
                iRet.ReqAmtType = drInv["RequestedAmtType"].ToString();
                iRet.QboClassId = drInv["QboClassId"].ToString();
                iRet.QboLocationId = drInv["QboLocationId"].ToString();
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

        public static string GetJobsCustomerId(string _Id, string _CompanyID)
        {
            Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());
            //  db.Open();
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
            //Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());
            //db.Open();
            //string Sql = "select Id,Name,QboId from Items where Id='" + _Id + "' and CompanyID='" + _CompanyID + "'";
            //DataTable dt;
            //db.Execute(Sql, out dt);
            //db.Close();
            //DataRow dr = dt.Rows[0];

            Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
            dbJobs.Open();
            string Sql = "select Id,Name from Items where Id='" + _Id + "' and CompanyId='" + _CompanyID + "' order by Name asc;";
            DataTable dtJobs = new DataTable();
            dbJobs.Execute(Sql, out dtJobs);
            dbJobs.Close();
            if (dtJobs.Rows.Count > 0)
                return dtJobs.Rows[0]["Id"].ToString();

            //if (!string.IsNullOrEmpty(dr["QboId"].ToString()) && dr["QboId"].ToString() != "0")
            //{
            //    Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
            //    dbJobs.Open();
            //    Sql = "select Id,Name,QboId from Items where QboId=" + dr["QboId"].ToString() + " and CompanyId='" + _CompanyID + "'";
            //    DataTable dtJobs = new DataTable();
            //    dbJobs.Execute(Sql, out dtJobs);
            //    dbJobs.Close();
            //    if (dtJobs.Rows.Count > 0)
            //        return dtJobs.Rows[0]["Id"].ToString();
            //}
            return "";
        }

        public static string getNextNumber(bool IsInvocie)
        {
            DataTable table;
            string New_Number = string.Empty;

            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                Database db = new Database(connStr);
                //  db.Open();

                string sql = @"SELECT [InvoicePrefix]
                          ,[InvoiceNumberSeed]
                          ,[EstimatePrefix]
                          ,[EstimateNumberSeed]
                      FROM [msSchedulerV3].[dbo].[tbl_Company]" +
                            " Where CompanyID= '" + CompanyID + "'";

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
        }

        [WebMethod(EnableSession = false)]
        public static QboCommon GetProductById(string pId = "")
        {
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string query = "Select * from Items  where 1=1 and IsDeleted=0 and Id = '" + pId + "' and CompanyId='" + HttpContext.Current.Session["CompanyID"].ToString() + "' order by Name asc; ";
            Database db = new Database(connStr);
            //  db.Open();
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

        [WebMethod(EnableSession = false)]
        public static string GetQboItemData()
        {
            try
            {
                string _Cid = HttpContext.Current.Session["CompanyID"].ToString();
                string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
                string query = "Select * from Items  where 1=1 and IsDeleted=0 and CompanyId='" + _Cid + "' order by Name asc; ";
                Database db = new Database(connStr);
                //  db.Open();
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

        [WebMethod(EnableSession = false)]
        public static QboCommon GetTaxRateById(string pId = "")
        {
            if (pId == "")
                pId = "0";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            string query = " Select Name, Rate from Taxes where Id='" + pId + "' and CompanyId='" + HttpContext.Current.Session["CompanyID"].ToString() + "'";

            Database db = new Database(connStr);
            //  db.Open();

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

        [WebMethod(EnableSession = false)]
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
            string InvoiceID = "", string _QboId = "", bool isNewTotal = false, string reqDepoAmt = "", string reqDepoPercent = "", string requestedAmtType = "", string QboClassId = "0", string QboLocationId = "0")
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
            reqDepoAmt = Common.CleanInput(reqDepoAmt);
            reqDepoPercent = Common.CleanInput(reqDepoPercent);
            requestedAmtType = Common.CleanInput(requestedAmtType);
            QboClassId = Common.CleanInput(QboClassId);
            QboLocationId = Common.CleanInput(QboLocationId);
            if (HttpContext.Current.Session["CompanyType"]?.ToString().ToLower() == "lhg" && _Indicator == "Proposal")
            {
                _Indicator = "Estimate";
            }
            _AppointmentID = Common.CleanInput(_AppointmentID);
            InvoiceID = Common.CleanInput(InvoiceID);
            _QboId = Common.CleanInput(_QboId);

            if (string.IsNullOrEmpty(_QboId)) _QboId = "0";
            if (string.IsNullOrEmpty(QboClassId)) QboClassId = "0";
            if (string.IsNullOrEmpty(QboLocationId)) QboLocationId = "0";
            if (isNewTotal)
            {
                if (HttpContext.Current.Session["IsWisetackEnabled"] != null)
                {
                    //WiseteckServicesSoapClient ws = new WiseteckServicesSoapClient();
                    //ws.DeleteLoanInfo(_CompanyId, InvoiceID);
                }
            }
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            DateTime expirationDate = DateTime.Now.AddDays(7);
            try
            {
                expirationDate = Convert.ToDateTime(_ExpirationDate);
            }
            catch { }
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

                if (disAmt == "")
                    disAmt = "0";
                if (taxAmt == "")
                    taxAmt = "0";
                if (TaxTp == "")
                    taxAmt = "0";
                if (reqDepoAmt == "")
                    reqDepoAmt = "0";
                if (reqDepoPercent == "")
                    reqDepoPercent = "0";

                double _Tax = Convert.ToDouble(taxAmt);
                taxAmt = _Tax.ToString("0.00");
                double tL = (Convert.ToDouble(subTotal) + _Tax) - (Convert.ToDouble(disAmt));

                decimal depoAmt = 0, depoPercent = 0, amtType = 0;
                decimal.TryParse(reqDepoAmt, out depoAmt);
                decimal.TryParse(reqDepoPercent, out depoPercent);
                decimal.TryParse(requestedAmtType, out amtType);

                string sqlJobInvoice = "";
                string sqlJobInvoiceDet = "";
                DateTime InvoiceDate = new DateTime(DateTime.Now.Year,
                                                     DateTime.Now.Month,
                                                     DateTime.Now.Day,
                                                     23, 59, 59, 999);



                //Insert For Scheduler DB
                string sql = @"begin ";
                bool IsExisting = false;
                if (string.IsNullOrEmpty(InvoiceID))
                {
                    InvoiceID = Guid.NewGuid().ToString().ToUpper();

                    sql = sql + @" Insert into tbl_Invoice (ID,CompnyID, TaxType, Number, CustomerId, Note, UserId, Subtotal,DiscountOption,DiscountRate,Discount, Tax, Total, Status, InvoiceDate,ExpirationDate, InvoiceType, CreatedDate, CreatedBy,AmountCollect, Type , AppointmentId,QboId,RequestedDepoAmt,ReqDepoPercent,RequestedAmtType,QboClassId,QboLocationId) values
                                ( '" + InvoiceID + "','" + _CompanyId + "' , '" + TaxTp + "', '" + qNum + "', '" + CustomerId + "', '" + _Note.Replace("'", "''") + "', '" + HttpContext.Current.Session["LoginUser"].ToString() + "', '"
                                 + subTotal + "','" + discountOpt + "', '" + disRt + "','" + disAmt + "', '" + taxAmt + "', '" + tL + "', '" + "1" + "', '" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + expirationDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + "" + "', '"
                                 + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "', 'CEC' ," + _Paid + ",'" + _Indicator + "','" + _AppointmentID + "'," + QboInvoiceId + "," + Convert.ToDecimal(reqDepoAmt) + "," + Convert.ToInt32(reqDepoPercent) + "," + Convert.ToInt32(requestedAmtType) + "," + (string.IsNullOrEmpty(QboClassId) || QboClassId == "0" ? "NULL" : QboClassId) + "," + (string.IsNullOrEmpty(QboLocationId) || QboLocationId == "0" ? "NULL" : QboLocationId) + ") ;";
                }
                else
                {
                    IsExisting = true;
                    sql += @"  Update tbl_Invoice set Subtotal= '" + subTotal + "',TaxType= '" + TaxTp + "',Note= '" + _Note.Replace("'", "''") + "',DiscountOption='" + discountOpt + "',DiscountRate='" + disRt + "', Discount = '" + disAmt + "', Tax = '" + taxAmt + "', Total = '" + tL + "',RequestedDepoAmt=" + depoAmt + ",ReqDepoPercent=" + depoPercent + ",RequestedAmtType=" + amtType + ", InvoiceDate = '" + _Date + "',QboClassId=" + (string.IsNullOrEmpty(QboClassId) || QboClassId == "0" ? "NULL" : QboClassId) + ",QboLocationId=" + (string.IsNullOrEmpty(QboLocationId) || QboLocationId == "0" ? "NULL" : QboLocationId) + " ";
                    sql = sql + @" where   CompnyID = '" + _CompanyId + "' and ID='" + InvoiceID + "';";
                    sql = sql + @" delete from tbl_InvoiceDetails  where Refid = '" + InvoiceID + "' and companyid='" + _CompanyId + "' ;";
                }

                for (int j = 0; j < _PID.Length - 1; j++)
                {
                    decimal _TLPrice = Convert.ToDecimal(_Rate[j]) * Convert.ToDecimal(_Qty[j]);

                    sql = sql + @" insert into tbl_InvoiceDetails (companyid,RefId, ItemId,LineNum, ItemName, Description,ServiceDate, Quantity, uPrice , TotalPrice,  IsTaxable, ItemTyId,CreatedDate, CreatedBy) values
                        ('" + HttpContext.Current.Session["CompanyID"].ToString() + "','" + InvoiceID + "', '" + _PID[j] + "', '" + (j + 1) + "', '" + _PName[j].Replace("'", "''") + "', '" + _Desc[j].Replace("'", "''") + "',  '" + _SDate[j] + "','" + _Qty[j] + "', '" + _Rate[j] + "' , '" + _TLPrice + "',  '"
                        + _AllTaxable[j].Trim().Replace("No", "NON") + "', '" + "" + "', '" + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + "" + "') ;";
                }

                sql = sql + " return end;";
                //  db.Open();
                db.Execute(sql);
                db.Close();
                //End Scheduler DB

                //Insert For Jobs DB
                if (string.IsNullOrEmpty(InvoiceID))
                {
                    Database dbJobs = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());

                    bool isAllDataFoundJobs = true;
                    try
                    {
                        string JobCustId = GetJobsCustomerId(CustomerId, _CompanyId);
                        string JobsUserId = GetJobsUsertId(HttpContext.Current.Session["LoginUser"].ToString());
                        if (string.IsNullOrEmpty(JobCustId))
                            isAllDataFoundJobs = false;
                        else
                        {
                            string InvoiceUID = Guid.NewGuid().ToString("n").Substring(0, 8);

                            string InvStatus = "0";
                            if (_Indicator != "Invoice")
                            {
                                InvStatus = "1";
                            }
                            sqlJobInvoice = @"INSERT INTO [myServiceJobs].[dbo].[Invoices]
                                                            ([Number],[InvoiceUID],[CompanyId],[CustomerId],[UserId],[Subtotal],
                                                            [Discount],[Tax],[Total],[Status],[InvoiceDate],[InvoiceType],
                                                            [CreatedDate],[ModifiedDate]) values
                         ('" + qNum + "' ,'" + RandomString(8) + "' , '" + _CompanyId + "', " +
                     "(select CustomerGuid from [msSchedulerV3].[dbo].tbl_Customer where [CustomerID] = '" + CustomerId + "' and CompanyID='" + _CompanyId + "')" +
                     ", " + JobsUserId + ", '" +
                      subTotal + "', '" + disAmt + "', '" + taxAmt + "', '" + tL + "','0','" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt", CultureInfo.InvariantCulture) + "', '" + InvStatus + "', SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET());";

                            for (int i = 0; i < _PID.Length - 1; i++)
                            {
                                decimal _TLPrice = Convert.ToDecimal(_Rate[i]) * Convert.ToDecimal(_Qty[i]);
                                string Job_PID = GetJobsProductId(_PID[i], _CompanyId);
                                string Taxable = _AllTaxable[i].ToUpper() == "YES" ? "1" : "0";
                                sqlJobInvoiceDet += @" insert into InvoiceItems (InvoiceNumber,ItemId, Quantity, UnitPrice, ItemName , ItemDescription,  IsTaxable, CreatedDate,ModifiedDate) values
                                        ('" + qNum + "', '" + Job_PID + "', '" + _Qty[i] + "', '" + _Rate[i] + "' , '" + _PName[i] + "', '" + _Desc[i] + "'," +
                                        Taxable + "," + "SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET()) ; ";
                            }
                        }
                    }
                    catch { isAllDataFoundJobs = false; }

                    try
                    {
                        dbJobs.Open();
                        if (_CompanyId.All(char.IsNumber) && isAllDataFoundJobs)
                        {
                            string JobsInvoice = " begin " + sqlJobInvoice + sqlJobInvoiceDet + " return end;";
                            dbJobs.Execute(JobsInvoice);
                        }
                    }
                    catch (Exception ex) { }
                    //---------------End Jobs DB
                    dbJobs.Close();
                }

                //Inserting Into Quickbooks
                //try
                //{
                //    if (_Indicator == "Invoice")
                //    {
                //        QBOSettins qBoStngPost = new QBOSettins();
                //        QBOManager qBOManager = new QBOManager();
                //        if (qBOManager.VerifyCompanySetting(_CompanyId, ref qBoStngPost))
                //        {
                //            string SyncToken = "";
                //            string QboPaymentID = "0";
                //            ServiceContext serviceContext = null;
                //            if (CustomerQboID == "0")
                //            {
                //                qBOManager.CustomerSyncToQBO(serviceContext, qBoStngPost, _CompanyId);
                //            }

                //            // Only send QboClassId and QboLocationId if IsPCS is true
                //            string qboClassIdForQbo = "0";
                //            string qboLocationIdForQbo = "0";
                //            if (HttpContext.Current.Session["IsPCS"] != null && (bool)HttpContext.Current.Session["IsPCS"])
                //            {
                //                qboClassIdForQbo = QboClassId;
                //                qboLocationIdForQbo = QboLocationId;
                //            }

                //            QboInvoiceId = qBOManager.CreateInvoiceQbo(serviceContext, ref QboPaymentID, ref SyncToken, "0",
                //                "", qBoStngPost, pId, pName, pDes, sDate, pQty, pRate
                //                , AllTaxable, CustomerId, _Date, _Note, discountOpt,
                //                disRt, disAmt, TaxTp, taxAmt, TotalAmount, qNum, _QboId, qboClassIdForQbo, qboLocationIdForQbo);

                //            if (QboInvoiceId != "0")
                //            {
                //                if (!IsExisting)
                //                {
                //                    sql = "update tbl_Invoice set QboId='" + QboInvoiceId + "',SyncToken='" + SyncToken + "' " +
                //                   " where ID='" + InvoiceID + "'" +
                //                   " and CompnyID='" + _CompanyId + "' ";
                //                }
                //                else
                //                {
                //                    sql = "update tbl_Invoice SyncToken='" + SyncToken + "' " +
                //                " where ID='" + InvoiceID + "'" +
                //             " and CompnyID='" + _CompanyId + "' ";
                //                }

                //                db.Execute(sql);
                //                db.Close();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        QBOSettins qBoStngPost = new QBOSettins();

                //        string SyncToken = "";
                //        QBOManager qBOManager = new QBOManager();
                //        if (qBOManager.VerifyCompanySetting(_CompanyId, ref qBoStngPost))
                //        {
                //            ServiceContext serviceContext = null;
                //            if (CustomerQboID == "0")
                //            {
                //                qBOManager.CustomerSyncToQBO(serviceContext, qBoStngPost, _CompanyId);
                //            }
                //            // Only send QboClassId and QboLocationId if IsPCS is true
                //            string qboClassIdForQbo = "0";
                //            string qboLocationIdForQbo = "0";
                //            if (HttpContext.Current.Session["IsPCS"] != null && (bool)HttpContext.Current.Session["IsPCS"])
                //            {
                //                qboClassIdForQbo = QboClassId;
                //                qboLocationIdForQbo = QboLocationId;
                //            }

                //            QboInvoiceId = qBOManager.CreateEstimateQbo(serviceContext, ref SyncToken, qBoStngPost, expirationDate, pId, pName, pDes, sDate, pQty, pRate, AllTaxable, CustomerId, "", _Date, _Note, discountOpt, disRt, disAmt, TaxTp, "", taxAmt, TotalAmount, qNum, _QboId, qboClassIdForQbo, qboLocationIdForQbo);
                //            if (QboInvoiceId != "0")
                //            {
                //                if (!IsExisting)
                //                {
                //                    sql = "update tbl_Invoice set QboEstimateId='" + QboInvoiceId + "',SyncToken='" + SyncToken + "' " +
                //                 " where ID='" + InvoiceID + "'" +
                //                  " and CompnyID='" + _CompanyId + "' ";
                //                }
                //                else
                //                {
                //                    sql = "update tbl_Invoice SyncToken='" + SyncToken + "' " +
                //                 " where ID='" + InvoiceID + "'" +
                //                  " and CompnyID='" + _CompanyId + "' ";
                //                }

                //                db.Execute(sql);
                //                db.Close();
                //            }
                //        }
                //    }
                //}
                //catch { }

                return "Data saved successfully.";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void LoadSavedInvoiceInfo(String _CustomerId, string Qut_Number)
        {
            _CustomerId = Common.CleanInput(_CustomerId);
            Qut_Number = Common.CleanInput(Qut_Number);

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            SqlConnection conn = new SqlConnection(connStr);

            string sql = "  Select  ID, RefId, ItemId, ItemName, Description, IsTaxable, Quantity, uPrice, TotalPrice from tbl_InvoiceDetails" +
                     " Where RefId = '" + Qut_Number + "' and companyid ='" + HttpContext.Current.Session["CompanyID"].ToString() + "' order by CAST(NULLIF(LineNum,'') AS INT) asc ";

            conn.Open();
            SqlCommand comm = new SqlCommand(sql, conn);
            SqlDataAdapter d = new SqlDataAdapter(comm);
            DataTable dt = new DataTable();
            d.Fill(dt);
            conn.Close();
            Control InvoiceList = FindControl("tblNewInvoice");
            // var table = (HtmlTable)tblNewInvoice.FindControl("tblNewInvoice");
            if (dt != null)
            {
                int i = 0;
                //table.Append("<tbody class='itemsCell'>");
                foreach (DataRow dr in dt.Rows)
                {
                    i++;
                    table.Append("<tr>");

                    // table.Append("<td>" + dr["ItemName"].ToString() + "</td>");
                    table.Append("<td class='tdList'>" + i + "</td><td><input type='hidden' id='idPrd" + i + "' value = " + dr["ItemId"].ToString() + " /> " + dr["ItemName"].ToString() + "</td> ");
                    var pDesc = "n/a";
                    if (!string.IsNullOrEmpty(dr["Description"].ToString()))
                    {
                        pDesc = dr["Description"].ToString();
                    }

                    table.Append("<td>" + pDesc + "</td>");
                    table.Append("<td>" + dr["Quantity"].ToString() + "</td>");
                    table.Append("<td>" + dr["uPrice"].ToString() + "</td>");
                    table.Append("<td>" + dr["TotalPrice"].ToString() + "</td>");
                    if (dr["IsTaxable"].ToString().Trim() == "True")
                        table.Append("<td>Yes</td>");
                    else if (dr["IsTaxable"].ToString().Trim() == "False")
                        table.Append("<td>No</td>");
                    else table.Append("<td>" + dr["IsTaxable"].ToString().Trim() + "&nbsp;</td>");
                    table.Append("<td><a class='fa fa-trash' href='javascript: void(0);' onclick='removeThis(this)' style='font-size:30px;color:red'></a></td>");
                    table.Append("</tr>");
                }
                // table.Append("</tbody>");
            }

            InvoiceList.Controls.Add(new Literal { Text = table.ToString() });
        }


        protected void btnBack_Click(object sender, EventArgs e)
        {
            string _CustomerId = Request.Params["cId"];

            Database dbJob = new Database(connStr);
            string customerID = dbJob.ExecuteScalar("select CustomerID from tbl_Customer where CustomerGuid='" + _CustomerId + "' and CompanyID= '" + Session["CompanyID"].ToString() + "'");


            string AppointmentID = Request.Params["AppID"];
            string Flag = Request.Params["InType"];
            string FromInvoice = Request.Params["FromInvList"];
            string FromInvoices = Request.Params["FromInvoices"];
            string InvType = Request.Params["InType"];
            string FormType = Request.Params["FormType"];
            string FromCustomer = Request.Params["FromCustomer"];
            string FromCustomerDetail = Request.Params["FromCustomerDetail"];
            if (Flag == "Proposal" || Flag == "Invoice" || Flag == "Estimate")
            {
                if (FromInvoice == "1") Response.Redirect("InvoiceList.aspx?m=1" + "&Id=" + _CustomerId + "&Type=" + Flag);
                else if (FromInvoices == "1") Response.Redirect("Invoices.aspx?Id=" + customerID.ToString() + "&Type=" + InvType + "&Form=" + FormType + "");
                else if (FromCustomerDetail == "1") Response.Redirect("customerDetail.aspx?m=2" + "&cid=" + customerID);
                else if (string.IsNullOrEmpty(AppointmentID))
                {
                    //Response.Redirect("InvoiceList.aspx?m=1" + "&Id=" + _CustomerId + "&Type=" + Flag);

                    Response.Redirect("CustomerList.aspx?m=2");
                }
                else
                {
                    if (!string.IsNullOrEmpty(FromCustomer))
                    {
                        //Response.Redirect("InvoiceList.aspx?m=1" + "&Id=" + _CustomerId + "&Type=" + Flag);

                        Response.Redirect("Invoices.aspx?Id=" + customerID + "&Type=" + Flag);
                    }
                    else
                    {
                        Response.Redirect("appointmentDetail.aspx?m=1&cid=" + _CustomerId + "&appt=" + AppointmentID + "&Form=" + FormType + "");
                    }
                }
            }
            else
            {
                Response.Redirect("Appointmentlist.aspx");
            }
        }

        protected void btnBack_ClickConvertInvoice()
        {
            string _Flag = Request.Params["Y"];
            string _CustomerId = Request.Params["cId"];
            string _appt = Request.Params["AppID"];
            if (string.IsNullOrEmpty(_appt) || _appt == "0")
            {
                Response.Redirect("InvoiceList.aspx?Type=Invoice" + "&Id=" + _CustomerId);
            }
            else
            {
                Response.Redirect("appointmentDetail.aspx?m=1" + "&cid=" + _CustomerId + "&appt=" + _appt);
            }
        }

        protected void btnSave_Click_ConvertToInvoice(object sender, EventArgs e)
        {
            string _CustomerId = Request.Params["cId"];

            string _Type = Request.Params["InType"];
            string _Number = Request.Params["InvNum"];

            if (_Number == null || _Number == "") _Number = ViewState["invId"].ToString();

            ConvertInvoice(_Number);

            _Type = Common.CleanInput(_Type);
            _CustomerId = Common.CleanInput(_CustomerId);
            _Number = Common.CleanInput(_Number);

            lblmessage.Text = "Proposal/Estimate has been Convert to Invoice successfully";
            lblmessage.Visible = true;
            btnBack_ClickConvertInvoice();
        }

      
        private string LoadPaymentButtonArea(string invId, string custId, string QboId, string QBOCustomerId, string depositAmt, decimal dueAmount, string Status, string number)
        {
            string paySec = string.Empty;
            paySec = @"<div class='dropdown'>
                    <button class='btn btn-secondary btn-transparentdark mt-3 w-100' type='button' id='Action' data-bs-toggle='dropdown' aria-expanded='false'>
                          Request Deposit
                    </button>
                    <ul class='dropdown-menu' aria-labelledby='Action'>" +
                    "<li><a class='dropdown-item' href='#' onclick=CollectInvDtl('1','" + dueAmount.ToString("0.00") + "','" + invId.Trim() + "','" + Status + "')>Collect Deposit (Cash/Check)</a></li>" +
                    "<li><a class='dropdown-item' href='#' onclick=CollectInvDtl('0','" + dueAmount.ToString("0.00") + "','" + invId.Trim().Trim() + "','" + Status + "')>Collect Payment (Cash/Check)</a></li>" +
                    "<li><a class='dropdown-item' href='#' onclick=ViewDepositList('" + invId + "')>View Deposit/Payment List</a></li>" +
                    "<li><hr class='dropdown-divider'></li>" +
                    "<li><a class='dropdown-item' href='#' onclick=CollectWithMerchant('" + custId.Trim() + "','" + depositAmt.Trim() + "','" + dueAmount.ToString("0.00").Trim() + "','" + invId.Trim() + "','" + QboId + "','" + QBOCustomerId.ToString().Trim() + "','true','" + number.Trim() + "','" + Status + "')>Collect Deposit (Merchant)</a></li>" +
                    "<li><a class='dropdown-item' onclick=CollectWithMerchant('" + custId.Trim() + "','" + depositAmt.Trim() + "','" + dueAmount.ToString("0.00").Trim() + "','" + invId.Trim() + "','" + QboId + "','" + QBOCustomerId.ToString().Trim() + "','false','" + number.Trim() + "','" + Status + "')>Collect Payment (Merchant)</a></li>" +
                    "</ul></div>";

            return paySec;
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

        private void FillCustomerAndInvoiceInfo(string _CustomerId, string InvoiceId)
        {
            DataTable dt;
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);
            // db.Open();

            string sql = @"Select C.id as InvoiceId, A.CompanyID, A.CustomerID, A.FirstName, A.LastName, A.Address1 + ', ' + A.City + ', ' + A.State + ' ' + A.ZipCode as Address,
                        A.ZipCode, A.Phone,A.BusinessID, A.Mobile, A.Email, A.CreatedDateTime  , isnull( B.SubTotal,0)SubTotal, ISNULL(C.Tax,0)Tax,C.DiscountOption,isnull(C.DiscountRate,0) DiscountRate, ISNULL(C.Discount,0)Discount,C.Note,C.AmountCollect,convert(varchar, C.ExpirationDate, 101) as ExpirationDate, convert(varchar, C.InvoiceDate, 101) as InvoiceDate, C.Number,C.TaxType,
                        isnull(( (B.SubTotal + ISNULL(C.Tax,0))- (ISNULL(C.Discount,0) )),0)Total,isnull(A.QboId,0) QboId,isnull(C.QboId,0) QboInvoiceId,isnull(C.QboEstimateId,0) QboEstimateId
                        from tbl_Customer A
                        left outer join tbl_Invoice C on C.CompnyID=A.CompanyID and A.CustomerID=C.CustomerId " + " and C.id='" + InvoiceId + "'";
            sql = sql + @" left outer join (Select RefId,Sum(TotalPrice)SubTotal from tbl_InvoiceDetails
                        group by RefId) B on B.RefId = '" + InvoiceId + "'" + " Where A.CompanyID = '" + Session["CompanyID"].ToString() + "' And A.CustomerID='" + _CustomerId + "'";

            db.Execute(sql, out dt);
            //  db.Close();
            string invDate = "";
            invDate = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            string BusinessID = "0";

            string _ExpirationDate = "";
            _ExpirationDate = DateTime.Now.AddDays(42).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];

                if (dr["InvoiceDate"].ToString() != null && dr["InvoiceDate"].ToString() != "")
                    invDate = dr["InvoiceDate"].ToString();
                else
                    invDate = DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                BusinessID = dr["BusinessID"].ToString();

                if (dr["ExpirationDate"].ToString() != null && dr["ExpirationDate"].ToString() != "")
                    _ExpirationDate = dr["ExpirationDate"].ToString();
                else
                    _ExpirationDate = DateTime.Now.AddDays(42).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

                txtFirstName.Text = dr["FirstName"].ToString() + " " + dr["LastName"].ToString();
                Address.Text = "Address : " + dr["Address"].ToString();
                Contact.Text = "Phone # " + dr["Phone"].ToString() + ", " + dr["Mobile"].ToString() + " <br/> Email : " + dr["Email"].ToString();

                Number.Text = dr["Number"].ToString();
                txt_InvocieNumber.Text = dr["Number"].ToString(); ;
                HdInvoiceId.Value = dr["InvoiceId"].ToString();

                hf_QboEstimateId.Value = dr["QboEstimateId"].ToString();
                HdQboId.Value = dr["QboInvoiceId"].ToString();
            }

            if (BusinessID != "0")
            {
                BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                BusinessContacts businessContact = businessContactProcessor.GetBusinessContactById(Session["CompanyID"].ToString(), BusinessID);

                txtFirstName.Text = businessContact.BusinessName;
                Address.Text = "Address : " + businessContact.Address1 + " " + businessContact.City + " " + businessContact.State + " " + businessContact.ZipCode;
                Contact.Text = "Phone # " + businessContact.Phone + ", " + businessContact.Mobile + " <br/> Email : " + businessContact.Email;
            }
            Date.Text = invDate;
            ExpirationDate.Text = _ExpirationDate;
        }

        private void Load_InitialData(string InvoiceID, string _CustomerId)
        {
            string CompanyID = Session["CompanyID"].ToString();

            BindQboLocations(CompanyID);
            BindQboClasses(CompanyID);
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

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
                    btn_ConvertToInvocie.Visible = false;
                    _SubmitInvoice.Visible = false;
                }
            }
            else
            {
                if (Request.Params["InType"] == "Proposal")
                {
                    Qut_Number = getNextNumber(false);
                }
                else
                {
                    Qut_Number = getNextNumber(true);
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
            if (!String.IsNullOrEmpty(dr["Phone"].ToString()) || !String.IsNullOrEmpty(dr["Mobile"].ToString()))
            {
                string phone = dr["Phone"].ToString().Trim();
                string mobile = dr["Mobile"].ToString().Trim();

                if (string.IsNullOrEmpty(phone))
                {
                    // Use mobile if phone is empty
                    customerPhone.Value = EnsureStartsWithPlus(mobile);
                }
                else if (string.IsNullOrEmpty(mobile))
                {
                    // Use phone if mobile is empty
                    customerPhone.Value = EnsureStartsWithPlus(phone);
                }
                else
                {
                    // If both are not empty, prefer phone
                    customerPhone.Value = EnsureStartsWithPlus(phone);
                }


            }
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
        string EnsureStartsWithPlus(string number)
        {
            if (!string.IsNullOrWhiteSpace(number) && !number.StartsWith("+"))
            {
                return "+" + number;
            }
            return number;
        }
        //prevoius
        //private void LoadTaxData(DataTable dtTax)
        //{
        //    try
        //    {
        //        if (dtTax.Rows.Count > 0)
        //        {
        //            optTaxRate.DataSource = dtTax;
        //            optTaxRate.DataValueField = "Id";
        //            optTaxRate.DataTextField = "Name";
        //            optTaxRate.DataBind();
        //            optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
        //        }
        //        else
        //            optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
        //    }
        //    catch (Exception ex)
        //    {
        //        optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
        //    }
        //}
        private void LoadTaxData(DataTable dtTax)
        {
            try
            {
                optTaxRate.Items.Clear(); // Clear existing items first

                if (dtTax.Rows.Count > 0)
                {
                    optTaxRate.DataSource = dtTax;
                    optTaxRate.DataValueField = "Id";
                    optTaxRate.DataTextField = "Name";
                    optTaxRate.DataBind();

                    // Insert default option at the top
                    optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
                    //Added By Munem
                    if (Session["CompanyType"].ToString() == "LHG")
                    {
                        var gstItem = dtTax.AsEnumerable()
                                      .FirstOrDefault(row => row["Name"].ToString() == "GST");

                        if (gstItem != null)
                        {
                            string gstId = gstItem["Id"].ToString();
                            optTaxRate.Value = gstId;
                        }
                        else
                        {
                            optTaxRate.SelectedIndex = 0; // Default selection
                        }
                    }
                    // end
                }
                else
                {
                    optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
                    optTaxRate.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                optTaxRate.Items.Clear();
                optTaxRate.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Tax Rate", ""));
                optTaxRate.SelectedIndex = 0;
            }
        }

        #region Send mail

        [WebMethod]
        public static string FillEmailModal(string CustomerID, string InvoiceNo, int FromInvDtl = 0)
        {
            EmailProcessor emailProcessor = new EmailProcessor();
            EmailCommunication emailcomm = new EmailCommunication();
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            CustomerProcessor customerProcessor = new CustomerProcessor();

            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = CustomerID }))
            {
                CustomerID = customerProcessor.GetCustomerByGuid(CustomerID, CompanyID).CustomerID;
            }
            else
            {
                return "Invalid Customer data.";
            }
            emailcomm = emailProcessor.FillEmailPopUp(CustomerID, InvoiceNo, FromInvDtl);
            if (emailcomm != null)
                return JsonConvert.SerializeObject(emailcomm);

            return "0";
        }

        protected void btn_PrintPdf_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            string _CustomerID = CustomerID.Value;
            string txtEmailBody = EmailBody.Value;
            string txtEmailSubject = _EmailSubject.Value;
            string RecepientCCEmail = _CC.Value;
            string RecepientBCCEmail = _BCC.Value;
            string RecepientToEmail = _To.Value;
            string InvoiceNo = _InvoiceNo.Value;
            string docType = _Type.Value;

            txtEmailBody = Common.CleanInput(txtEmailBody);
            txtEmailSubject = Common.CleanInput(txtEmailSubject);
            RecepientCCEmail = Common.CleanInput(RecepientCCEmail);
            RecepientBCCEmail = Common.CleanInput(RecepientBCCEmail);
            RecepientToEmail = Common.CleanInput(RecepientToEmail);
            //added by nazifa [7-31]
            Database db = new Database();
            bool isSendPaymentLink = IsSendPaymentLink.Checked;
            string CSPaymentLink = "";
            if (isSendPaymentLink == true)
            {
                string textCustomerID = db.ExecuteScalar("select CustomerID from msSchedulerV3.dbo.tbl_Customer   where CustomerGuid='" + _CustomerID + "' and CompanyID='" + CompanyID + "'  ;");

                string amount = db.ExecuteScalar("select (Total-AmountCollect)as Amount from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string CustomerName = db.ExecuteScalar("select concat(FirstName,' ',LastName) as customerName  from msSchedulerV3.dbo.tbl_Customer where CustomerId=" + textCustomerID + " and CompanyId='" + CompanyID + "' ");

                string amountCollect = db.ExecuteScalar("select isNULL(AmountCollect,0)as amountCollect from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");
                string reqDepoAmount = db.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string reqDepoAmtDollarAmt = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from  msSchedulerV3.dbo.tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmount == "0" || reqDepoAmount == "0.000000") reqDepoAmount = reqDepoAmtDollarAmt;

                decimal reqDAmt = Convert.ToDecimal(reqDepoAmount);

                if (reqDAmt > 0) amount = reqDepoAmount;

                //CSPaymentLink = TPM.EmailProcessor.GetCSPaymentLink(CompanyID, textCustomerID, InvoiceNo, CustomerName, RecepientToEmail, amount);
            }

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database dbJob = new Database(connStr);
            string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            string reqDepoAmtDollar = dbJob.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
            decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

            //end by nazifa
            List<HttpPostedFile> files = new List<HttpPostedFile>();

            CustomerProcessor customerProcessor = new CustomerProcessor();

            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = _CustomerID }))
            {
                _CustomerID = customerProcessor.GetCustomerByGuid(_CustomerID, CompanyID).CustomerID;
            }

            EmailProcessor emailProcessor = new EmailProcessor();
            string invoiceCreated = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string pdfFileName = string.Empty;
            byte[] invoicepdf = null;

            //if (hf_CurrentPdfType.Value == "AsaInvoice")
            //{
            //    invoicepdf = emailProcessor.InvoicePdfForASA(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, reqAmt);
            //    pdfFileName = "_Invoice.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "AsaQuote")
            //{
            //    invoicepdf = emailProcessor.InvoicePdfForASA(_CustomerID, InvoiceNo, "Proposal", CSPaymentLink, reqAmt);
            //    pdfFileName = "_Estimate.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "Invoice")
            //{
            //    pdfFileName = "_Invoice.pdf";
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, false, reqAmt);
            //}
            //else if (hf_CurrentPdfType.Value == "Estimate")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Estimate", CSPaymentLink, false, reqAmt);
            //    pdfFileName = "_Estimate.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "InvoiceWithoutDiscount")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, true, reqAmt);
            //    pdfFileName = "_Invoice.pdf";
            //}

            string strFolder;

            string _Path = "~/EmailHistoryContent/" + _CustomerID + "/";
            strFolder = System.Web.HttpContext.Current.Server.MapPath(_Path);
            if (!Directory.Exists(strFolder))
            {
                Directory.CreateDirectory(strFolder);
            }
            string invoicestrFilePath = strFolder + invoiceCreated + pdfFileName;
            File.WriteAllBytes(invoicestrFilePath, invoicepdf);

            string strUrl = "EmailHistoryContent/" + _CustomerID + "/" + invoiceCreated + pdfFileName;
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "popup", "window.open('" + strUrl + "','_blank')", true);
        }

        [Obsolete]
        protected void btnSendInvoiceMail_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string txtCustomerID = CustomerID.Value;
            string txtEmailBody = EmailBody.Value;
            string txtEmailSubject = _EmailSubject.Value;
            string RecepientCCEmail = _CC.Value;
            string RecepientBCCEmail = _BCC.Value;
            string RecepientToEmail = _To.Value;
            string InvoiceNo = _InvoiceNo.Value;

            txtCustomerID = Common.CleanInput(txtCustomerID);
            txtEmailBody = Common.CleanInput(txtEmailBody);
            txtEmailSubject = Common.CleanInput(txtEmailSubject);
            RecepientCCEmail = Common.CleanInput(RecepientCCEmail);
            RecepientBCCEmail = Common.CleanInput(RecepientBCCEmail);
            RecepientToEmail = Common.CleanInput(RecepientToEmail);
            List<HttpPostedFile> files = new List<HttpPostedFile>();
            // Invoice_List inv = new Invoice_List();
            //added by nazifa [7-31]
            Database db = new Database();
            bool isSendPaymentLink = IsSendPaymentLink.Checked;
            string CSPaymentLink = "";
            if (isSendPaymentLink == true)
            {
                string textCustomerID = db.ExecuteScalar("select CustomerID from msSchedulerV3.dbo.tbl_Customer   where CustomerGuid='" + txtCustomerID + "' and CompanyID='" + CompanyID + "'  ;");

                string amount = db.ExecuteScalar("select (Total-AmountCollect)as Amount from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string CustomerName = db.ExecuteScalar("select concat(FirstName,' ',LastName) as customerName  from msSchedulerV3.dbo.tbl_Customer where CustomerId=" + textCustomerID + " and CompanyId='" + CompanyID + "' ");

                string amountCollect = db.ExecuteScalar("select isNULL(AmountCollect,0)as amountCollect from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");
                string reqDepoAmount = db.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");


                string reqDepoAmtDollarAmt = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from  msSchedulerV3.dbo.tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmount == "0" || reqDepoAmount == "0.000000") reqDepoAmount = reqDepoAmtDollarAmt;

                decimal reqDAmt = Convert.ToDecimal(reqDepoAmount);

                if (reqDAmt > 0) amount = reqDepoAmount;
                //CSPaymentLink = EmailProcessor.GetCSPaymentLink(CompanyID, textCustomerID, InvoiceNo, CustomerName, RecepientToEmail, amount);
            }
            //end by nazifa

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database dbJob = new Database(connStr);
            string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            string reqDepoAmtDollar = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
            decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

            EmailProcessor emailProcessor = new EmailProcessor();
            //emailProcessor.GetWiseTackPaymentLink(InvoiceNo, txtCustomerID);

            CustomerProcessor customerProcessor = new CustomerProcessor();

            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = txtCustomerID }))
            {
                txtCustomerID = customerProcessor.GetCustomerByGuid(txtCustomerID, CompanyID).CustomerID;
            }

            string returnMessage = "";
            string pdf_FileName = "_Proposal.pdf";

            byte[] invoicepdf = null;

            //if (hf_CurrentPdfType.Value == "AsaInvoice")
            //{
            //    pdf_FileName = "_Invoice.pdf";
            //    invoicepdf = emailProcessor.InvoicePdfForASA(txtCustomerID, InvoiceNo, "Invoice", CSPaymentLink, reqAmt);
            //}
            //if (hf_CurrentPdfType.Value == "AsaQuote")
            //{
            //    invoicepdf = emailProcessor.InvoicePdfForASA(txtCustomerID, InvoiceNo, "Proposal", CSPaymentLink, reqAmt);
            //}
            //if (hf_CurrentPdfType.Value == "Invoice")
            //{
            //    pdf_FileName = "_Invoice.pdf";
            //    invoicepdf = emailProcessor.InvoicePdf(txtCustomerID, InvoiceNo, "Invoice", CSPaymentLink, false, reqAmt);
            //}
            //if (hf_CurrentPdfType.Value == "Estimate")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(txtCustomerID, InvoiceNo, "Estimate", CSPaymentLink, false, reqAmt);
            //    pdf_FileName = "_Estimate.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "InvoiceWithoutDiscount")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(txtCustomerID, InvoiceNo, "Invoice", CSPaymentLink, true, reqAmt);
            //    pdf_FileName = "_Invoice.pdf";
            //}

            string strFileName;
            string strFilePath;
            string strFolder;
            string invoiceCreated = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string _Path = "~/EmailHistoryContent/" + txtCustomerID + "/";
            strFolder = Server.MapPath(_Path);
            if (!Directory.Exists(strFolder))
            {
                Directory.CreateDirectory(strFolder);
            }
            string invoicestrFilePath = strFolder + invoiceCreated + pdf_FileName;
            System.IO.File.WriteAllBytes(invoicestrFilePath, invoicepdf);

            //string _Path = "EmailHistoryContent/" + txtCustomerID + "/";

            List<EmailContent> emailContents = new List<EmailContent>();
            EmailContent emac = new EmailContent();
            emac.FileName = invoiceCreated + pdf_FileName;
            emac.FileUrl = _Path + invoiceCreated + pdf_FileName;
            emailContents.Add(emac);
            // strFolder = Server.MapPath( _Path);
            if (file.HasFile)
            {
                int i = 0;
                foreach (HttpPostedFile f in file.PostedFiles)
                {
                    emac = new EmailContent();
                    // strFolder = Server.MapPath("./" + _Path);
                    // Get the name of the file that is posted.
                    string fileuploaddate = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
                    strFileName = f.FileName;
                    strFileName = Path.GetFileName(strFileName);

                    // Create the directory if it does not exist.

                    // Save the uploaded file to the server.
                    string extension = Path.GetExtension(f.FileName);
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
            //if (invoicepdf.Length > 0)
            //{
            //    returnMessage = emailProcessor.SendHtmlFormattedEmail(CompanyID, txtCustomerID, "Invoice Email", txtEmailSubject, txtEmailBody, RecepientToEmail, RecepientCCEmail, RecepientBCCEmail, files, invoicepdf, "invoice.pdf");
            //}
            //else
            //{
            returnMessage = emailProcessor.SendHtmlFormattedEmail(CompanyID, txtCustomerID, "Invoice Email", txtEmailSubject, txtEmailBody, RecepientToEmail, RecepientCCEmail, RecepientBCCEmail, emailContents);

            //}

            string exception = " Swal.fire('Email sent successfully.', '', 'Successfully');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
        }

        [Obsolete]
        protected void btnSendProposalMail_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string _CustomerId = CustomerID.Value;
            string txtEmailBody = EmailBody.Value;
            string txtEmailSubject = _EmailSubject.Value;
            string RecepientCCEmail = _CC.Value;
            string RecepientBCCEmail = _BCC.Value;
            string RecepientToEmail = _To.Value;
            string InvoiceNo = _InvoiceNo.Value;

            _CustomerId = Common.CleanInput(_CustomerId);
            txtEmailBody = Common.CleanInput(txtEmailBody);
            txtEmailSubject = Common.CleanInput(txtEmailSubject);
            RecepientCCEmail = Common.CleanInput(RecepientCCEmail);
            RecepientBCCEmail = Common.CleanInput(RecepientBCCEmail);
            RecepientToEmail = Common.CleanInput(RecepientToEmail);
            List<HttpPostedFile> files = new List<HttpPostedFile>();
            //Invoice_List inv = new Invoice_List();

            //added by nazifa [7-31]
            Database db = new Database();
            bool isSendPaymentLink = IsSendPaymentLink.Checked;
            string CSPaymentLink = "";
            if (isSendPaymentLink == true)
            {
                string textCustomerID = db.ExecuteScalar("select CustomerID from msSchedulerV3.dbo.tbl_Customer   where CustomerGuid='" + _CustomerId + "' and CompanyID='" + CompanyID + "'  ;");

                string amount = db.ExecuteScalar("select (Total-AmountCollect)as Amount from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string CustomerName = db.ExecuteScalar("select concat(FirstName,' ',LastName) as customerName  from msSchedulerV3.dbo.tbl_Customer where CustomerId=" + textCustomerID + " and CompanyId='" + CompanyID + "' ");

                string amountCollect = db.ExecuteScalar("select isNULL(AmountCollect,0)as amountCollect from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");
                string reqDepoAmount = db.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");


                string reqDepoAmtDollarAMt = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from  msSchedulerV3.dbo.tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmount == "0" || reqDepoAmount == "0.000000") reqDepoAmount = reqDepoAmtDollarAMt;

                decimal reqDAmt = Convert.ToDecimal(reqDepoAmount);

                if (reqDAmt > 0) amount = reqDepoAmount;
                //CSPaymentLink = EmailProcessor.GetCSPaymentLink(CompanyID, textCustomerID, InvoiceNo, CustomerName, RecepientToEmail, amount);
            }
            //end by nazifa

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database dbJob = new Database(connStr);
            string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");
            string reqDepoAmtDollar = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
            decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

            EmailProcessor emailProcessor = new EmailProcessor();
            //emailProcessor.GetWiseTackPaymentLink(InvoiceNo, _CustomerId);
            string returnMessage = "";
            string strFileName;
            string strFilePath;
            string strFolder;

            CustomerProcessor customerProcessor = new CustomerProcessor();

            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = _CustomerId }))
            {
                _CustomerId = customerProcessor.GetCustomerByGuid(_CustomerId, CompanyID).CustomerID;
            }

            string pdf_FileName = "_Proposal.pdf";
            byte[] proposalpdf = null;

            //if (hf_CurrentPdfType.Value == "AsaInvoice")
            //{
            //    proposalpdf = emailProcessor.InvoicePdfForASA(_CustomerId, InvoiceNo, "Invoice", CSPaymentLink, reqAmt);
            //    pdf_FileName = "_Invoice.pdf";
            //}
            //if (hf_CurrentPdfType.Value == "AsaQuote")
            //{
            //    proposalpdf = emailProcessor.InvoicePdfForASA(_CustomerId, InvoiceNo, "Proposal", CSPaymentLink, reqAmt);
            //}
            //if (hf_CurrentPdfType.Value == "Invoice")
            //{
            //    pdf_FileName = "_Invoice.pdf";
            //    proposalpdf = emailProcessor.InvoicePdf(_CustomerId, InvoiceNo, "Invoice", CSPaymentLink, false, reqAmt);
            //}
            //if (hf_CurrentPdfType.Value == "Estimate")
            //{
            //    proposalpdf = emailProcessor.InvoicePdf(_CustomerId, InvoiceNo, "Estimate", CSPaymentLink, false, reqAmt);
            //}
            //else if (hf_CurrentPdfType.Value == "InvoiceWithoutDiscount")
            //{
            //    proposalpdf = emailProcessor.InvoicePdf(_CustomerId, InvoiceNo, "Invoice", CSPaymentLink, true, reqAmt);
            //    pdf_FileName = "_Invoice.pdf";
            //}

            string proposalCreated = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string _Path = "~/EmailHistoryContent/" + _CustomerId + "/";

            strFolder = Server.MapPath(_Path);

            //strFolder = Server.MapPath(_Path);
            if (!Directory.Exists(strFolder))
            {
                Directory.CreateDirectory(strFolder);
            }
            string proposalFilePath = strFolder + proposalCreated + pdf_FileName;
            System.IO.File.WriteAllBytes(proposalFilePath, proposalpdf);

            // string _Path = "~/EmailHistoryContent/" + txtCustomerID + "/";

            List<EmailContent> emailContents = new List<EmailContent>();
            EmailContent emac = new EmailContent();
            emac.FileName = proposalCreated + pdf_FileName;
            emac.FileUrl = _Path + proposalCreated + pdf_FileName;
            emailContents.Add(emac);

            if (file.HasFile)
            {
                int i = 0;
                foreach (HttpPostedFile f in file.PostedFiles)
                {
                    emac = new EmailContent();
                    // strFolder = Server.MapPath("./" + _Path);
                    // Get the name of the file that is posted.
                    string fileuploaddate = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
                    strFileName = f.FileName;
                    strFileName = Path.GetFileName(strFileName);

                    // Create the directory if it does not exist.

                    // Save the uploaded file to the server.
                    string extension = Path.GetExtension(f.FileName);
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
            //if (proposalpdf.Length < 0)
            //{
            //    returnMessage = emailProcessor.SendHtmlFormattedEmail(CompanyID, txtCustomerID, "Proposal Email", txtEmailSubject, txtEmailBody, RecepientToEmail, RecepientCCEmail, RecepientBCCEmail, files, proposalpdf, "Proposal.pdf");
            //}
            //else
            //{
            returnMessage = emailProcessor.SendHtmlFormattedEmail(CompanyID, _CustomerId, "Proposal Email", txtEmailSubject, txtEmailBody, RecepientToEmail, RecepientCCEmail, RecepientBCCEmail, emailContents);

            //}

            string exception = " Swal.fire('Email sent successfully.', '', 'Successfully');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", exception, true);
        }

        #endregion Send mail

        protected void btn_SendMMS_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            string _CustomerID = CustomerID.Value;
            string RecepientToEmail = _To.Value;
            string InvoiceNo = _InvoiceNo.Value;
            string mmsBody = txtMMSBody.Value;

            RecepientToEmail = Common.CleanInput(RecepientToEmail);

            Database db = new Database();
            bool isSendPaymentLink = IsSendPaymentLink.Checked;
            string CSPaymentLink = "";
            if (isSendPaymentLink == true)
            {
                string textCustomerID = db.ExecuteScalar("select CustomerID from msSchedulerV3.dbo.tbl_Customer   where CustomerGuid='" + _CustomerID + "' and CompanyID='" + CompanyID + "'  ;");

                string amount = db.ExecuteScalar("select (Total-AmountCollect)as Amount from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string CustomerName = db.ExecuteScalar("select concat(FirstName,' ',LastName) as customerName  from msSchedulerV3.dbo.tbl_Customer where CustomerId=" + textCustomerID + " and CompanyId='" + CompanyID + "' ");

                string amountCollect = db.ExecuteScalar("select isNULL(AmountCollect,0)as amountCollect from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");
                string reqDepoAmount = db.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from msSchedulerV3.dbo.tbl_Invoice where CustomerId=" + textCustomerID + " and CompnyID='" + CompanyID + "' and ID='" + InvoiceNo + "'");

                string reqDepoAmtDollarAmt = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from  msSchedulerV3.dbo.tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

                if (reqDepoAmount == "0" || reqDepoAmount == "0.000000") reqDepoAmount = reqDepoAmtDollarAmt;

                decimal reqDAmt = Convert.ToDecimal(reqDepoAmount);

                if (reqDAmt > 0) amount = reqDepoAmount;

                //CSPaymentLink = EmailProcessor.GetCSPaymentLink(CompanyID, textCustomerID, InvoiceNo, CustomerName, RecepientToEmail, amount);
            }

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database dbJob = new Database(connStr);
            string reqDepoAmt = dbJob.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            string reqDepoAmtDollar = dbJob.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where id='" + _InvoiceNo.Value + "' and CompnyID='" + CompanyID + "'");

            if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollar;
            decimal reqAmt = Convert.ToDecimal(reqDepoAmt);

            //end by nazifa
            List<HttpPostedFile> files = new List<HttpPostedFile>();

            CustomerProcessor customerProcessor = new CustomerProcessor();
            string mobile = "";
            mobile = mmsPhone.Value;
            if (customerProcessor.CheckIfValidCustomer(new CustomerEntity { CompanyID = CompanyID, CustomerGuid = _CustomerID }))
            {
                //mobile = customerProcessor.GetCustomerByGuid(_CustomerID, CompanyID).Mobile;
                _CustomerID = customerProcessor.GetCustomerByGuid(_CustomerID, CompanyID).CustomerID;

            }
            if (string.IsNullOrWhiteSpace(mobile))
            {
                string alert = "Swal.fire('Customer mobile number not found! Please add first.', '', 'warning');CloseMMSPopup();";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "NoMobileAlert", alert, true);
                return;
            }
            EmailProcessor emailProcessor = new EmailProcessor();
            string invoiceCreated = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string pdfFileName = string.Empty;
            byte[] invoicepdf = null;

            //if (hf_CurrentPdfType.Value == "AsaInvoice")
            //{
            //    invoicepdf = emailProcessor.InvoicePdfForASA(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, reqAmt);
            //    pdfFileName = "_Invoice.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "AsaQuote")
            //{
            //    invoicepdf = emailProcessor.InvoicePdfForASA(_CustomerID, InvoiceNo, "Proposal", CSPaymentLink, reqAmt);
            //    pdfFileName = "_Estimate.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "Invoice")
            //{
            //    pdfFileName = "_Invoice.pdf";
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, false, reqAmt);
            //}
            //else if (hf_CurrentPdfType.Value == "Estimate")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Estimate", CSPaymentLink, false, reqAmt);
            //    pdfFileName = "_Estimate.pdf";
            //}
            //else if (hf_CurrentPdfType.Value == "InvoiceWithoutDiscount")
            //{
            //    invoicepdf = emailProcessor.InvoicePdf(_CustomerID, InvoiceNo, "Invoice", CSPaymentLink, true, reqAmt);
            //    pdfFileName = "_Invoice.pdf";
            //}

            string strFolder;

            string _Path = "~/EmailHistoryContent/" + _CustomerID + "/";
            strFolder = System.Web.HttpContext.Current.Server.MapPath(_Path);
            if (!Directory.Exists(strFolder))
            {
                Directory.CreateDirectory(strFolder);
            }
            string invoicestrFilePath = strFolder + invoiceCreated + pdfFileName;
            File.WriteAllBytes(invoicestrFilePath, invoicepdf);
            string baseUrl = ConfigurationManager.AppSettings["baseurl"];
            string mmsUrl = baseUrl + "EmailHistoryContent/" + _CustomerID + "/" + invoiceCreated + pdfFileName;
            string filePath = "EmailHistoryContent/" + _CustomerID + "/" + invoiceCreated + pdfFileName;
            //TwilioSMSService twilio = new TwilioSMSService();
            //bool result = twilio.SendInvoiceMMS(CompanyID, _CustomerID, mmsBody, mobile, filePath, mmsUrl);
            //string response;
            //if (result == true)
            //{
            //    response = " Swal.fire('MMS Sent Successfully', '', 'Success');CloseMMSPopup();";
            //}
            //else
            //{
            //    response = " Swal.fire('Something went wrong, Please try again.', '', 'Warning');CloseMMSPopup();";
            //}

            //ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", response, true);


        }
        private void BindQboLocations(string companyId)
        {


            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT QboLocationID, Name FROM [msSchedulerV3].[dbo].Tbl_QboLocation where companyId= '" + companyId + "' ORDER BY Name";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                qboLocation.DataSource = dt;
                qboLocation.DataTextField = "Name";
                qboLocation.DataValueField = "QboLocationID";
                qboLocation.DataBind();
                qboLocation.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-- Select Location --", ""));
            }
        }

        private void BindQboClasses(string companyId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT QboClassId, Name FROM [msSchedulerV3].[dbo].Tbl_QboClass  where companyId= '" + companyId + "' ORDER BY Name";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                qboClass.DataSource = dt;
                qboClass.DataTextField = "Name";
                qboClass.DataValueField = "QboClassId";
                qboClass.DataBind();
                qboClass.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-- Select Class --", ""));
            }
        }

    }
}