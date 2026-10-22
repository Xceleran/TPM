using FSM.Helper;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.Exception;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace FSM.Processors
{
    public class QBOManager
    {
        public bool VerifyCompanySetting(string CompanyID, ref QBOSettins qboSettings)
        {
            bool bRetVal = false;

            try
            {
                Database db = new Database();
                //  db.Open();
                string Sql = "select AccessToken,RefreshToken,BQOFileID,CompanyID,ConnectedCompanyName from [myServiceJobs].[dbo].[QBOCompanySettings] where CompanyID='" + CompanyID + "'";

                DataTable dt;

                db.Execute(Sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    qboSettings = new QBOSettins();
                    qboSettings.AccessToken = dr["AccessToken"].ToString();
                    qboSettings.RefreshToken = dr["RefreshToken"].ToString();
                    qboSettings.FileID = dr["BQOFileID"].ToString();
                    qboSettings.ConnectedCompanyName = dr["ConnectedCompanyName"].ToString();
                    qboSettings.isQBConnected = true;
                    bRetVal = true;
                }
                db.Close();
            }
            catch (Exception ex)
            {

            }

            return bRetVal;
        }

        public bool SaveCompanySetting(string CompanyID, string AccessToken, string RefreshToken, string BQOFileID)
        {
            bool bRetVal = false;

            Database db = new Database();
            string Sql = "";
            try
            {



                Sql = "Delete From [myServiceJobs].[dbo].[QBOCompanySettings] Where CompanyID = '" + CompanyID + "';";
                Sql += "INSERT INTO [myServiceJobs].[dbo].[QBOCompanySettings] " +
                      "(CompanyID,UserID,AccessToken,RefreshToken,BQOFileID) " +
                      " VALUES (" +
                      "'" + CompanyID + "'," +
                      "'" + HttpContext.Current.Session["LoginUser"].ToString() + "'," +
                      "'" + AccessToken + "'," +
                      "'" + RefreshToken + "'," +
                      "'" + BQOFileID + "');";

                db.Execute(Sql);

                bRetVal = true;




            }
            catch (Exception ex)
            {

                HttpContext.Current.Response.Write(Sql);
            }

            db.Close();

            return bRetVal;
        }

        public CompanyInfo GetQboCompanyInfo(ServiceContext serviceContext)
        {
            CompanyInfo companyInfo = null;
            try
            {

                QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
                companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").FirstOrDefault();
            }
            catch { }

            return companyInfo;
        }
        public bool DeleteCompanySetting(string CompanyID)
        {

            string clientId = ConfigurationManager.AppSettings["clientId"];
            string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
            string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
            string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];

            bool bRetVal = false;



            try
            {
                QBOSettins qbs = new QBOSettins();
                QBOManager qBOManager = new QBOManager();
                bool IsQuickBookEnabled = qBOManager.VerifyCompanySetting(CompanyID, ref qbs);
                //  ServiceContext context = QBOManager.GetServiceContext(qbs, CompanyID);
                OAuth2Client oauthClient = new OAuth2Client(clientId, clientSecret, redirectURI, appEnvironment);
                var tokenResp = oauthClient.RevokeTokenAsync(qbs.RefreshToken).GetAwaiter().GetResult();

            }
            catch
            {

            }

            try
            {
                Database db = new Database();
                //  db.Open();
                string Sql = "Delete From [myServiceJobs].[dbo].[QBOCompanySettings] Where CompanyID = '" + CompanyID + "'";
                db.Execute(Sql);
                bRetVal = true;
                db.Close();
            }
            catch { }

            return bRetVal;
        }

        public ServiceContext GetServiceContext(QBOSettins qboSettings, string CompanyID)
        {
            //First Refresh the AccessToken
            RefreshAccessToken(qboSettings, CompanyID);
            ServiceContext serviceContext = null;
            if (!string.IsNullOrWhiteSpace(qboSettings.AccessToken))
            {
                //Get the QBO ServiceContext

                OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(qboSettings.AccessToken);
                serviceContext = new ServiceContext(qboSettings.FileID, IntuitServicesType.QBO, oauthValidator);

                string QBOSandBox = ConfigurationManager.AppSettings["QBOSandBox"];

                if (QBOSandBox == "1")
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://sandbox-quickbooks.api.intuit.com/";
                else
                    serviceContext.IppConfiguration.BaseUrl.Qbo = "https://quickbooks.api.intuit.com/";//prod

                serviceContext.IppConfiguration.MinorVersion.Qbo = "28";
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            }
            return serviceContext;
        }

        public void RefreshAccessToken(QBOSettins qboSettings, string CompanyID)
        {
            try
            {
                //string redirectURI = "https://developer.intuit.com/v2/OAuth2Playground/RedirectUrl";// ConfigurationManager.AppSettings["redirectURI"];
                //string clientID = "ABBRytiACpmR3OA8qsU2OJdPcVvwOox16HsdYgYoZmumb8Znyj";// ConfigurationManager.AppSettings["clientID"];
                //string clientSecret = "FjA4orDUHYfXssbDUYlgtviBom3YDnmlcDn3ghN3";// ConfigurationManager.AppSettings["clientSecret"];



                //redirectURI = "http://localhost:33746/logout.aspx";
                //clientID = "ABD7cXAkLVgeqNuYOyYh6SnNG54h4Zh9L0bVoZJ622Fcj0qPAh";
                //clientSecret = "n4LR02r0hxE4gvjqPSr0Vw0DgjO9CFFTINz78Bb1";

                string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
                string clientID = ConfigurationManager.AppSettings["clientID"];
                string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
                string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];

                OAuth2Client client = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
                TokenResponse tokenResp = client.RefreshTokenAsync(qboSettings.RefreshToken).GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(tokenResp.AccessToken))
                {
                    Database db = new Database();
                    //  db.Open();
                    string sql = "Update [myServiceJobs].[dbo].[QBOCompanySettings] set " +
                                         " AccessToken = '" + tokenResp.AccessToken + "'," +
                                         " RefreshToken = '" + tokenResp.RefreshToken + "'" +
                                         " Where CompanyID = '" + CompanyID + "'";
                    db.Execute(sql);
                    db.Close();

                    qboSettings.AccessToken = tokenResp.AccessToken;
                    qboSettings.RefreshToken = tokenResp.RefreshToken;
                }
                else
                {
                    qboSettings.AccessToken = "";
                    qboSettings.RefreshToken = "";
                }
            }
            catch (Exception ex)
            {
                qboSettings.AccessToken = "";
                qboSettings.RefreshToken = "";
            }

        }
        // Create Invoice into Qbo - Yuvi
        public string CreateEstimateQbo(ServiceContext context, ref string SyncToken, QBOSettins qBoStngPost, DateTime expirationDate,
            string pId = "", string pName = "", string pDesc = "", string sDate = "",
            string pQty = "", string pRate = "", string pTax = "",
            string CustomerID = "", string cName = "",
            string trDate = "", string Notes = "",
            string disId = "", string DRate = "0", string Discount = "0",
            string TaxRefId = "", string TaxRefName = "", string TotalTax = "0",
            string TotalAmount = "0", string InvoiceNumberDoc = "", string QBOID = "0", string QboClassId = "0", string QboLocationId = "0", string InvoiceID = "0")
        {

            bool isDuplicate_DocNumber = false;
            QBOSettins qBoStng = new QBOSettins();
            ServiceContext serviceContext;


            if (context != null)
            {
                serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            }
            else
            {
                serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            }

            serviceContext.IppConfiguration.MinorVersion.Qbo = "62";

            DataService dataService = new DataService(serviceContext);
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
            //  db.Open();

            Intuit.Ipp.Data.Estimate estimate = new Intuit.Ipp.Data.Estimate();

            try
            {
                string[] _PID = pId.Split('@');
                string[] _PName = pName.Split('@');
                string[] _Qty = pQty.Split('@');
                string[] _Rate = pRate.Split('@');
                string[] _Tax = pTax.Split('@');
                string[] _PDesc = pDesc.Split('@');
                string[] _SDate = sDate.Split('@');

                if (string.IsNullOrEmpty(TotalTax))
                    TotalTax = "0";
                if (pId.Trim().Length == 0)
                {
                    return "-11";
                }
                if (_PID.Length == 0)
                {
                    return "-11";
                }

                //DocNumber - QBO Only, otherwise use DocNumber



                string Sql = @"select QboId,CustomerID,FirstName,LastName from tbl_Customer Where CompanyID = '" + HttpContext.Current.Session["CompanyID"].ToString() + "' And CustomerID='" + CustomerID + "'";
                DataTable dt;
                db.Execute(Sql, out dt);
                string QBOCustomerID = "0"; string CustomerName = "";
                if (dt.Rows.Count > 0)
                {
                    QBOCustomerID = dt.Rows[0]["QboId"].ToString();

                    if (QBOCustomerID == "0")
                    {
                        return "0";
                    }
                    string Name = dt.Rows[0]["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dt.Rows[0]["LastName"].ToString().Trim()))
                        Name += " " + dt.Rows[0]["LastName"].ToString().Trim();

                    Name = Name.Replace("  ", " ");

                    CustomerName = Name;
                }
                else { return "0"; }
                estimate.CustomerRef = new ReferenceType()
                {
                    name = CustomerName,
                    Value = QBOCustomerID
                };

                QueryService<CompanyInfo> querySvc = new QueryService<CompanyInfo>(serviceContext);
                //   CompanyInfo companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM CompanyInfo").FirstOrDefault();

                if (QBOID != "0")
                {
                    if (!string.IsNullOrEmpty(QBOID))
                    {
                        string EXISTING_INVOICE_QUERYBYID = string.Format("select * from Estimate where id = '{0}'", QBOID);
                        var queryService = new QueryService<Intuit.Ipp.Data.Estimate>(serviceContext);
                        Intuit.Ipp.Data.Estimate objInvoiceFound = queryService.ExecuteIdsQuery(EXISTING_INVOICE_QUERYBYID).FirstOrDefault<Intuit.Ipp.Data.Estimate>();
                        estimate.Id = objInvoiceFound.Id;
                        estimate.SyncToken = objInvoiceFound.SyncToken;
                    }

                }

                estimate.DocNumber = InvoiceNumberDoc;
                estimate.AutoDocNumber = true;
                estimate.AutoDocNumberSpecified = true;


                //TxnDate
                try
                {
                    estimate.TxnDate = Convert.ToDateTime(trDate, CultureInfo.InvariantCulture);
                    estimate.TxnDateSpecified = true;
                }
                catch { }


                //PrivateNote
                estimate.PrivateNote = Notes;

                //invoice.DiscountRate = Convert.ToDecimal(DRate);
                //invoice.DiscountAmt = Convert.ToDecimal(Discount);
                estimate.TotalAmt = Convert.ToDecimal(TotalAmount);


                var lines = new List<Line>();

                for (int i = 0; i < _PID.Length; i++)
                {
                    if (!string.IsNullOrEmpty(_PID[i]))
                    {
                        Line invoiceLine = new Line();
                        invoiceLine.Description = _PDesc[i];
                        decimal dlAmount = Convert.ToDecimal(_Rate[i]) * Convert.ToDecimal(_Qty[i]);
                        invoiceLine.Amount = dlAmount;
                        invoiceLine.LineNum = (i + 1).ToString();
                        invoiceLine.AmountSpecified = true;
                        invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                        invoiceLine.DetailTypeSpecified = true;
                        SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();

                        //Check With QBO
                        string ItemIdQbo = "0"; string ItemNameQbo = ""; string ItemNameDesc = "";
                        ItemCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), _PID[i], ref ItemIdQbo, ref ItemNameQbo, ref ItemNameDesc, serviceContext);
                        // invoiceLine.Description = ItemNameDesc;
                        lineSalesItemLineDetail.ItemRef = new ReferenceType()
                        {
                            name = ItemNameQbo,
                            Value = ItemIdQbo
                        };
                        if (!string.IsNullOrEmpty(_SDate[i].ToString()))
                            lineSalesItemLineDetail.ServiceDate = Convert.ToDateTime(_SDate[i].ToString());
                        lineSalesItemLineDetail.AnyIntuitObject = Convert.ToDecimal(_Rate[i]);
                        lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                        lineSalesItemLineDetail.Qty = Convert.ToDecimal(_Qty[i]);
                        lineSalesItemLineDetail.QtySpecified = true;

                        lineSalesItemLineDetail.TaxCodeRef = new ReferenceType()
                        {
                            Value = _Tax[i]
                        };

                        lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date;
                        lineSalesItemLineDetail.ServiceDateSpecified = true;
                        invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;

                        lines.Add(invoiceLine);


                        //Discount Part
                        if (i == _PID.Length - 2)
                        {
                            try
                            {
                                Intuit.Ipp.Data.Account DisRef = AccountCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), "Discounts given");
                                if (DisRef != null)
                                {
                                    DiscountLineDetail iDis = new DiscountLineDetail();

                                    iDis.DiscountAccountRef = new ReferenceType()
                                    {
                                        name = DisRef.Name,
                                        Value = DisRef.Id
                                    };
                                    if (disId == "1")
                                    {
                                        iDis.PercentBased = true;
                                        iDis.DiscountPercentSpecified = true;
                                    }
                                    else
                                    {
                                        iDis.PercentBased = false;
                                        iDis.DiscountPercentSpecified = true;
                                    }

                                    iDis.DiscountPercent = Convert.ToDecimal(DRate);


                                    Line DisLine = new Line();
                                    DisLine.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                                    DisLine.DetailTypeSpecified = true;

                                    DisLine.Amount = Convert.ToDecimal(Discount);
                                    DisLine.AmountSpecified = true;

                                    DisLine.AnyIntuitObject = iDis;
                                    lines.Add(DisLine);

                                }
                            }
                            catch { }
                        }
                    }

                }

                //Assign Line Item to Invoice
                estimate.Line = lines.ToArray();

                if (!string.IsNullOrEmpty(TaxRefId))
                {
                    try
                    {
                        //Check With QBO
                        TaxCode RefTaxCode = null;
                        TaxCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), TaxRefId, ref RefTaxCode);
                        TxnTaxDetail txnTaxDetail = new TxnTaxDetail();

                        txnTaxDetail.TxnTaxCodeRef = new ReferenceType()
                        {
                            name = RefTaxCode.Name,
                            Value = RefTaxCode.Id
                        };

                        Line taxLine = new Line();
                        taxLine.DetailType = LineDetailTypeEnum.TaxLineDetail;
                        TaxLineDetail taxLineDetail = new TaxLineDetail();
                        taxLineDetail.TaxRateRef = RefTaxCode.SalesTaxRateList.TaxRateDetail[0].TaxRateRef;
                        taxLine.AnyIntuitObject = taxLineDetail;
                        taxLine.Amount = Convert.ToDecimal(TotalTax);
                        txnTaxDetail.TaxLine = new Line[] { taxLine };
                        estimate.TxnTaxDetail = txnTaxDetail;
                    }
                    catch
                    {
                        estimate.TxnTaxDetail = null;
                    }
                }
                else
                    estimate.TxnTaxDetail = null;









                estimate.SalesTermRef = null;

                //DueDate
                estimate.DueDate = Convert.ToDateTime(trDate, CultureInfo.InvariantCulture);
                estimate.DueDateSpecified = true;

                //ARAccountRef
                Intuit.Ipp.Data.Account ArRef = AccountCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), "Accounts Receivable (A/R)");
                if (ArRef != null)
                {
                    estimate.ARAccountRef = new ReferenceType()
                    {
                        name = ArRef.Name,
                        Value = ArRef.Id
                    };
                }

                //invoice.ARAccountRef = new ReferenceType()
                //{
                //    name = "Accounts Receivable (A/R)",
                //    Value = "84"
                //};

                // Set ClassRef if QboClassId is provided
                if (!string.IsNullOrEmpty(QboClassId) && QboClassId != "0")
                {
                    try
                    {
                        estimate.ClassRef = new ReferenceType()
                        {
                            Value = QboClassId
                        };
                    }
                    catch { }
                }

                // Set DepartmentRef (Location) if QboLocationId is provided
                if (!string.IsNullOrEmpty(QboLocationId) && QboLocationId != "0")
                {
                    try
                    {
                        estimate.DepartmentRef = new ReferenceType()
                        {
                            Value = QboLocationId
                        };
                    }
                    catch { }
                }

                estimate.ExpirationDate = expirationDate;
                estimate.ExpirationDateSpecified = true;

                if (QBOID == "0")
                {
                    //ServiceContext serviceContext = QBOManager.GetServiceContext(qbs, mc.CompanyID);
                    //DataService dataService = new DataService(serviceContext);
                    Intuit.Ipp.Data.Estimate invoiceAdded = dataService.Add(estimate);
                    QBOID = invoiceAdded.Id;
                    SyncToken = invoiceAdded.SyncToken;

                }
                else
                {
                    //DataService dataService = new DataService(serviceContext);
                    Intuit.Ipp.Data.Estimate UpdateEntity = dataService.Update(estimate);
                    QBOID = UpdateEntity.Id;
                    SyncToken = UpdateEntity.SyncToken;
                }

                return QBOID;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    if (innerException.ErrorCode == "6140")
                    {
                        isDuplicate_DocNumber = true;
                    }
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);
                }
                else
                {
                    return "-111";
                }

            }

            if (isDuplicate_DocNumber)
            {
                if (QBOID == "0")
                {
                    try
                    {
                        estimate.DocNumber = InvoiceNumberDoc;

                        if (estimate.DocNumber.Contains('-'))
                        {
                            string[] parts = estimate.DocNumber.Split('-');
                            if (parts.Length > 1)
                            {
                                if (parts.Length > 2)
                                {
                                    estimate.DocNumber = parts[0] + "-" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper() + "-" + parts[2];
                                }
                                else
                                {
                                    estimate.DocNumber = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper() + "-" + parts[1];
                                }

                                // The hyphen existed and the string was split
                            }

                        }
                        else
                        {
                            estimate.DocNumber = Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper() + "-" + estimate.DocNumber;
                        }




                        Intuit.Ipp.Data.Estimate invoiceAdded = dataService.Add(estimate);
                        if (invoiceAdded != null)
                        {

                            if (invoiceAdded.Id != null)
                            {
                                QBOID = invoiceAdded.Id;
                            }
                            if (invoiceAdded.SyncToken != null)
                            {
                                SyncToken = invoiceAdded.SyncToken;
                            }
                            try
                            {
                                string Sql = "update tbl_Invoice set Number='" + estimate.DocNumber + "'" +
                              " where id='" + InvoiceID + "'" +
                              " and CompnyID='" + HttpContext.Current.Session["CompanyID"].ToString() + "' ";

                                db.Execute(Sql);
                            }
                            catch { }
                        }

                        return QBOID;
                    }
                    catch (Exception ex)
                    {
                        return "-111";
                    }


                }
                return QBOID;
            }
            else
            {
                return QBOID;
            }
        }
        public bool RemoveEstimate(QBOSettins qBoStngPost, string EstimateID)
        {
            bool result = false;

            try
            {
                QBOManager qBOManager = new QBOManager();
                ServiceContext serviceContext = qBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";

                string EXISTING_ESTIMATE_QUERYBYID = string.Format("select * from Estimate where id = '{0}'", EstimateID);
                var queryService = new QueryService<Estimate>(serviceContext);
                Estimate objEstimateFound = queryService.ExecuteIdsQuery(EXISTING_ESTIMATE_QUERYBYID).FirstOrDefault<Estimate>();
                //If Invoice found on Quickbooks online
                if (objEstimateFound != null)
                {
                    DataService dataService = new DataService(serviceContext);
                    Estimate DeleteEntity = dataService.Delete<Intuit.Ipp.Data.Estimate>(objEstimateFound);
                    if (DeleteEntity != null && !string.IsNullOrEmpty(DeleteEntity.Id) && DeleteEntity.status == EntityStatusEnum.Deleted)
                    {
                        //you can write Database code here
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }


            return result;
        }
        public void MakePayment(string EstimateID, decimal Amount, string CustomerID, QBOSettins qBoStngPost)
        {

            QBOManager qBOManager = new QBOManager();

            ServiceContext serviceContext = qBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());


            Payment ObjPayment = new Payment();

            ObjPayment.TotalAmtSpecified = true;
            ObjPayment.TotalAmt = Convert.ToDecimal(Amount);
            ObjPayment.CustomerRef = new ReferenceType();
            ObjPayment.CustomerRef.Value = CustomerID;

            //List LineList = new List();
            List<Line> LineList = new List<Line>();
            Line line = new Line();

            //List linkedTxnList = new List();
            List<LinkedTxn> linkedTxnList = new List<LinkedTxn>();
            LinkedTxn linkedTxn = new LinkedTxn();



            linkedTxn.TxnType = "Estimate";
            linkedTxn.TxnId = EstimateID;
            linkedTxnList.Add(linkedTxn);

            line.AmountSpecified = true;
            line.Amount = Convert.ToDecimal(Amount);
            line.LinkedTxn = linkedTxnList.ToArray();
            LineList.Add(line);
            ObjPayment.Line = LineList.ToArray();

            DataService dataService = new DataService(serviceContext);
            Payment PaymentAdd = dataService.Add(ObjPayment);
        }
        public void SyncPaymentToQBO(ServiceContext serviceContext, QBOSettins qBoStngPost, string companyid)
        {
            string Qry = @"";

            Qry = @"SELECT tbl_Payment.Companyid,tbl_Payment.id, tbl_Payment.createdDate,
                                    tbl_Payment.Amount,tbl_Payment.InvocieId,
                                    tbl_Payment.QboId as paymentQboid, 
                                    tbl_Invoice.QboId AS InvocieQboID,
                                    tbl_Invoice.Number AS DocNumber,
                                    tbl_Invoice.Type,
									tbl_Customer.QboId as CustomerQboID,
                                    tbl_Invoice.QboEstimateId as EstimateQBoID
                                    FROM  tbl_Payment INNER JOIN tbl_Invoice 
                                    ON tbl_Payment.Companyid = tbl_Invoice.CompnyID AND tbl_Payment.InvocieId = tbl_Invoice.ID
									INNER JOIN tbl_Customer 
                                    ON tbl_Invoice.CompnyID = tbl_Customer.CompanyID AND tbl_Invoice.CustomerID = tbl_Customer.CustomerID
                                    where tbl_Payment.CompanyID=@CompanyID and  tbl_Payment.QboId = 0; ";


            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
            //  db.Open();

            DataTable dt = new DataTable();


            string sql = String.Empty;
            DataSet dataSet = db.Get_DataSet(Qry, companyid);
            dt = dataSet.Tables[0];
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    if (serviceContext == null)
                    {
                        serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                    }
                }

                DataService dataService = new DataService(serviceContext);

                foreach (DataRow dr in dt.Rows)
                {
                    try
                    {


                        string paymentId = Guid.NewGuid().ToString("N").Substring(0, 20);

                        string reference = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();

                        PaymentMethod paymentMethod = new PaymentMethod();
                        Payment payment = null;
                        if (dr["Type"].ToString() == "Invoice")
                        {
                            payment = new Payment
                            {
                                PaymentRefNum = reference,
                                DocNumber = dr["DocNumber"].ToString(),
                                PaymentTypeSpecified = true,
                                PaymentType = PaymentTypeEnum.Cash,
                                UnappliedAmt = 0,
                                UnappliedAmtSpecified = false,
                                TotalAmt = Convert.ToDecimal(dr["Amount"].ToString()),
                                TotalAmtSpecified = true,
                                ProcessPayment = true,
                                ProcessPaymentSpecified = true,
                                TxnDateSpecified = true,
                                TxnDate = Convert.ToDateTime(dr["createdDate"].ToString()),
                                CurrencyRef = new ReferenceType { name = "US Dollar", Value = "USD" },
                                Line = new Line[] { new Line
                           {
                                          Amount = Convert.ToDecimal(dr["Amount"].ToString()),
                                          AmountSpecified = true,
                                          DetailType = LineDetailTypeEnum.PaymentLineDetail,
                                          DetailTypeSpecified = true,
                                          LinkedTxn = new LinkedTxn[] { new LinkedTxn { TxnId = dr["InvocieQboID"].ToString(), TxnType = "Invoice" } } }
                           },
                                CustomerRef = new ReferenceType { Value = dr["CustomerQboID"].ToString() }
                            };
                        }
                        else
                        {
                            reference = dr["DocNumber"].ToString();
                            payment = new Payment
                            {
                                PaymentRefNum = dr["DocNumber"].ToString(),
                                PaymentTypeSpecified = true,
                                PaymentType = PaymentTypeEnum.Cash,
                                UnappliedAmt = 0,
                                UnappliedAmtSpecified = false,
                                TotalAmt = Convert.ToDecimal(dr["Amount"].ToString()),
                                TotalAmtSpecified = true,
                                ProcessPayment = true,
                                ProcessPaymentSpecified = true,
                                TxnDateSpecified = true,
                                TxnDate = Convert.ToDateTime(dr["createdDate"].ToString()),
                                CurrencyRef = new ReferenceType { name = "US Dollar", Value = "USD" },
                                CustomerRef = new ReferenceType { Value = dr["CustomerQboID"].ToString() }
                            };
                        }



                        Payment results = dataService.Add<Payment>(payment);
                        sql += "Update tbl_Payment set QboId=" + results.Id + ",PaymentRefNum='" + reference + "' where companyid='" + companyid + "' and id='" + dr["id"].ToString() + "';";


                    }
                    catch (Intuit.Ipp.Exception.IdsException ex)
                    {
                        sql += "Update tbl_Payment set QboId='-1',PaymentRefNum='NONE' where companyid='" + companyid + "' and id='" + dr["id"].ToString() + "';";

                        string errDetail = "";
                        var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                        if (innerException != null)
                        {
                            errDetail = innerException.Detail;
                            //throw new ApplicationException(innerException.Detail);
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(sql))
                {
                    db.Execute(CleanInput(sql));
                }


            }
            catch (Exception ex) { }


        }
        // Create Invoice into Qbo - Yuvi
        public string CreateInvoiceQbo(ServiceContext context, ref string QboPaymentID, ref string SyncToken, string Status, string QboEstimateid, QBOSettins qBoStngPost,
            string pId = "", string pName = "", string pDesc = "", string sDate = "",
            string pQty = "", string pRate = "", string pTax = "",
            string CustomerID = "",
            string trDate = "", string Notes = "", string PrivateNote = "",
            string disId = "", string DRate = "0", string Discount = "0",
            string TaxRefId = "", string TotalTax = "0",
            string TotalAmount = "0", string InvoiceNumberDoc = "",
            string QBOId = "0", string QboClassId = "0", string QboLocationId = "0", string InvoiceID = "0")
        {

            bool isDuplicate_DocNumber = false;
            QBOSettins qBoStng = new QBOSettins();
            ServiceContext serviceContext;


            if (context != null)
            {
                serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            }
            else
            {
                serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            }

            serviceContext.IppConfiguration.MinorVersion.Qbo = "62";

            DataService dataService = new DataService(serviceContext);
            Intuit.Ipp.Data.Invoice invoice = new Intuit.Ipp.Data.Invoice();
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
            try
            {
                string[] _PID = pId.Split('@');
                string[] _PName = pName.Split('@');
                string[] _Qty = pQty.Split('@');
                string[] _Rate = pRate.Split('@');
                string[] _Tax = pTax.Trim().Replace("No", "NON").Split('@');
                string[] _PDesc = pDesc.Split('@');
                string[] _SDate = sDate.Split('@');

                if (string.IsNullOrEmpty(TotalTax))
                    TotalTax = "0";
                if (pId.Trim().Length == 0)
                {
                    return "-1";
                }



                //DocNumber - QBO Only, otherwise use DocNumber
                Intuit.Ipp.Data.Estimate _Estimate = new Intuit.Ipp.Data.Estimate();
                //  invoice.EInvoiceStatus = ETransactionStatusEnum.Paid;
                if (!string.IsNullOrEmpty(QboEstimateid))
                {
                    if (QboEstimateid != "0")
                    {
                        List<LinkedTxn> linkedTxns = new List<LinkedTxn>();
                        linkedTxns.Add(new LinkedTxn { TxnId = QboEstimateid, TxnType = "Estimate" });
                        invoice.LinkedTxn = linkedTxns.ToArray();
                    }

                }

                if (Status == "1")
                {
                    invoice.EInvoiceStatus = ETransactionStatusEnum.Paid;
                    invoice.EInvoiceStatusSpecified = true;

                }


                if (QBOId != "0")
                {
                    if (!string.IsNullOrEmpty(QBOId))
                    {
                        string EXISTING_INVOICE_QUERYBYID = string.Format("select * from Invoice where id = '{0}'", QBOId);
                        var queryService = new QueryService<Intuit.Ipp.Data.Invoice>(serviceContext);
                        Intuit.Ipp.Data.Invoice objInvoiceFound = queryService.ExecuteIdsQuery(EXISTING_INVOICE_QUERYBYID).FirstOrDefault<Intuit.Ipp.Data.Invoice>();
                        invoice.Id = objInvoiceFound.Id;
                        invoice.SyncToken = objInvoiceFound.SyncToken;
                    }


                }

                invoice.DocNumber = InvoiceNumberDoc;
                invoice.AutoDocNumber = true;
                invoice.AutoDocNumberSpecified = true;

                //TxnDate
                try
                {
                    invoice.TxnDate = Convert.ToDateTime(trDate, CultureInfo.InvariantCulture);
                    invoice.TxnDateSpecified = true;
                }
                catch { }

                //PrivateNote
                invoice.PrivateNote = PrivateNote;
                MemoRef memoRef = new MemoRef();
                memoRef.Value = Notes;

                invoice.CustomerMemo = memoRef;
                //invoice.DiscountRate = Convert.ToDecimal(DRate);
                //invoice.DiscountAmt = Convert.ToDecimal(Discount);
                invoice.TotalAmt = Convert.ToDecimal(TotalAmount);


                var lines = new List<Line>();

                for (int i = 0; i < _PID.Length; i++)
                {
                    if (!string.IsNullOrEmpty(_PID[i]))
                    {
                        Line invoiceLine = new Line();
                        invoiceLine.Description = _PDesc[i];
                        decimal dlAmount = Convert.ToDecimal(_Rate[i]) * Convert.ToDecimal(_Qty[i]);
                        invoiceLine.Amount = dlAmount;
                        invoiceLine.LineNum = (i + 1).ToString();
                        invoiceLine.AmountSpecified = true;
                        invoiceLine.DetailType = LineDetailTypeEnum.SalesItemLineDetail;
                        invoiceLine.DetailTypeSpecified = true;
                        SalesItemLineDetail lineSalesItemLineDetail = new SalesItemLineDetail();

                        //Check With QBO
                        string ItemIdQbo = "0"; string ItemNameQbo = ""; string ItemNameDesc = "";
                        ItemCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), _PID[i], ref ItemIdQbo, ref ItemNameQbo, ref ItemNameDesc, serviceContext);
                        // invoiceLine.Description = ItemNameDesc;

                        lineSalesItemLineDetail.ItemRef = new ReferenceType()
                        {
                            name = ItemNameQbo,
                            Value = ItemIdQbo
                        };
                        if (!string.IsNullOrEmpty(_SDate[i].ToString()))
                        {
                            try
                            {
                                lineSalesItemLineDetail.ServiceDate = Convert.ToDateTime(_SDate[i].ToString());
                            }
                            catch { lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date; }
                        }
                        else
                        {
                            lineSalesItemLineDetail.ServiceDate = DateTime.Now.Date;
                        }
                        lineSalesItemLineDetail.AnyIntuitObject = Convert.ToDecimal(_Rate[i]);
                        lineSalesItemLineDetail.ItemElementName = ItemChoiceType.UnitPrice;
                        lineSalesItemLineDetail.Qty = Convert.ToDecimal(_Qty[i]);
                        lineSalesItemLineDetail.QtySpecified = true;


                        lineSalesItemLineDetail.TaxCodeRef = new ReferenceType()
                        {
                            Value = _Tax[i]
                        };


                        lineSalesItemLineDetail.ServiceDateSpecified = true;
                        invoiceLine.AnyIntuitObject = lineSalesItemLineDetail;

                        lines.Add(invoiceLine);

                        //Discount Part
                        if (i == _PID.Length - 2)
                        {
                            try
                            {
                                Intuit.Ipp.Data.Account DisRef = AccountCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), "Discounts given");
                                if (DisRef != null)
                                {
                                    DiscountLineDetail iDis = new DiscountLineDetail();

                                    iDis.DiscountAccountRef = new ReferenceType()
                                    {
                                        name = DisRef.Name,
                                        Value = DisRef.Id
                                    };
                                    if (disId == "1")
                                    {
                                        iDis.PercentBased = true;
                                        iDis.DiscountPercentSpecified = true;
                                    }
                                    else
                                    {
                                        iDis.PercentBased = false;
                                        iDis.DiscountPercentSpecified = true;
                                    }

                                    iDis.DiscountPercent = Convert.ToDecimal(DRate);


                                    Line DisLine = new Line();
                                    DisLine.DetailType = LineDetailTypeEnum.DiscountLineDetail;
                                    DisLine.DetailTypeSpecified = true;

                                    DisLine.Amount = Convert.ToDecimal(Discount);
                                    DisLine.AmountSpecified = true;

                                    DisLine.AnyIntuitObject = iDis;
                                    lines.Add(DisLine);

                                }
                            }
                            catch { }
                        }
                    }

                }

                //Assign Line Item to Invoice
                invoice.Line = lines.ToArray();

                if (!string.IsNullOrEmpty(TaxRefId))
                {
                    try
                    {
                        //Check With QBO
                        TaxCode RefTaxCode = null;
                        TaxCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), TaxRefId, ref RefTaxCode);

                        TxnTaxDetail txnTaxDetail = new TxnTaxDetail();

                        txnTaxDetail.TxnTaxCodeRef = new ReferenceType()
                        {
                            name = RefTaxCode.Name,
                            Value = RefTaxCode.Id
                        };

                        Line taxLine = new Line();
                        taxLine.DetailType = LineDetailTypeEnum.TaxLineDetail;
                        TaxLineDetail taxLineDetail = new TaxLineDetail();
                        taxLineDetail.TaxRateRef = RefTaxCode.SalesTaxRateList.TaxRateDetail[0].TaxRateRef;
                        taxLine.AnyIntuitObject = taxLineDetail;
                        taxLine.Amount = Convert.ToDecimal(TotalTax);
                        txnTaxDetail.TaxLine = new Line[] { taxLine };

                        invoice.TxnTaxDetail = txnTaxDetail;
                        invoice.TxnTaxDetail.TotalTax = Convert.ToDecimal(TotalTax);
                        invoice.TxnTaxDetail.TotalTaxSpecified = true;

                        invoice.GlobalTaxCalculationSpecified = false;
                        invoice.GlobalTaxCalculation = GlobalTaxCalculationEnum.TaxExcluded;



                    }
                    catch (Exception ex)
                    {
                        invoice.TxnTaxDetail = null;

                        try
                        {

                            TxnTaxDetail txnTaxDetail = new TxnTaxDetail();
                            invoice.TxnTaxDetail = txnTaxDetail;
                            invoice.TxnTaxDetail.TotalTax = Convert.ToDecimal(TotalTax);
                            invoice.TxnTaxDetail.TotalTaxSpecified = true;

                            invoice.GlobalTaxCalculationSpecified = false;
                            invoice.GlobalTaxCalculation = GlobalTaxCalculationEnum.TaxExcluded;
                        }
                        catch { }

                    }
                }
                else
                    invoice.TxnTaxDetail = null;





                string QBOCustomerID = "0"; string CustomerName = "";


                //  db.Open();
                string Sql = @"select QboId,CustomerID,FirstName,LastName from tbl_Customer Where CompanyID = '" + HttpContext.Current.Session["CompanyID"].ToString() + "' And CustomerID='" + CustomerID + "'";
                DataTable dt;
                db.Execute(Sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    QBOCustomerID = dt.Rows[0]["QboId"].ToString();
                    string Name = dt.Rows[0]["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dt.Rows[0]["LastName"].ToString().Trim()))
                        Name += " " + dt.Rows[0]["LastName"].ToString().Trim();


                    if (QBOCustomerID == "0")
                    {
                        CustomerCheckTEST(context, HttpContext.Current.Session["CompanyID"].ToString(), dt.Rows[0]["CustomerID"].ToString(), ref QBOCustomerID, ref Name);
                    }



                    Name = Name.Replace("  ", " ");

                    CustomerName = Name;
                }

                invoice.CustomerRef = new ReferenceType()
                {
                    name = CustomerName,
                    Value = QBOCustomerID
                };



                invoice.SalesTermRef = null;

                //DueDate
                try
                {
                    invoice.DueDate = Convert.ToDateTime(trDate, CultureInfo.InvariantCulture);
                    invoice.DueDateSpecified = true;


                }
                catch { }



                //ARAccountRef
                Intuit.Ipp.Data.Account ArRef = AccountCodeCheck(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString(), "Accounts Receivable (A/R)");
                if (ArRef != null)
                {
                    invoice.ARAccountRef = new ReferenceType()
                    {
                        name = ArRef.Name,
                        Value = ArRef.Id
                    };
                }

                // Set ClassRef if QboClassId is provided
                if (!string.IsNullOrEmpty(QboClassId) && QboClassId != "0")
                {
                    try
                    {
                        invoice.ClassRef = new ReferenceType()
                        {
                            Value = QboClassId
                        };
                    }
                    catch { }
                }

                // Set DepartmentRef (Location) if QboLocationId is provided
                if (!string.IsNullOrEmpty(QboLocationId) && QboLocationId != "0")
                {
                    try
                    {
                        invoice.DepartmentRef = new ReferenceType()
                        {
                            Value = QboLocationId
                        };
                    }
                    catch { }
                }

                //invoice.ARAccountRef = new ReferenceType()
                //{
                //    name = "Accounts Receivable (A/R)",
                //    Value = "84"
                //};


                //Intuit.Ipp.Data.Invoice invoiceAdded = dataService.Add(invoice);



                if (QBOId == "0")
                {
                    //ServiceContext serviceContext = QBOManager.GetServiceContext(qbs, mc.CompanyID);
                    //DataService dataService = new DataService(serviceContext);
                    Intuit.Ipp.Data.Invoice invoiceAdded = dataService.Add(invoice);
                    if (invoiceAdded != null)
                    {

                        if (invoiceAdded.Id != null)
                        {
                            QBOId = invoiceAdded.Id;
                        }
                        if (invoiceAdded.SyncToken != null)
                        {
                            SyncToken = invoiceAdded.SyncToken;
                        }
                    }



                }
                else
                {
                    //DataService dataService = new DataService(serviceContext);

                    try
                    {
                        Intuit.Ipp.Data.Invoice UpdateEntity = dataService.Update(invoice);
                        QBOId = UpdateEntity.Id;
                        SyncToken = UpdateEntity.SyncToken;

                        //if (Status == "1" && QboPaymentID == "0")
                        //{
                        //    string paymentName = "Cash";
                        //    // paymentType.Name
                        //    string paymentId = Guid.NewGuid().ToString("N").Substring(0, 20); ; // paymentType.Id

                        //    string reference = Guid.NewGuid().ToString("N").Substring(0, 20); ;

                        //    // string invoiceId = ""; // invoice.Id

                        //    Line[] invoiceLines = lines.Select(m => new Line()
                        //    {
                        //        AnyIntuitObject = m.AnyIntuitObject,
                        //        Amount = Convert.ToDecimal(TotalAmount),
                        //        AmountSpecified = true,
                        //        CustomField = m.CustomField,
                        //        Id = m.Id,
                        //        LineNum = m.LineNum,
                        //        Description = m.Description,
                        //        DetailType = LineDetailTypeEnum.PaymentLineDetail, //m.DetailType,
                        //        DetailTypeSpecified = true, //m.DetailTypeSpecified,
                        //        LinkedTxn = new List<LinkedTxn>
                        //{
                        //    new LinkedTxn()
                        //    {
                        //        TxnId = QBOId,
                        //        TxnType = TxnTypeEnum.Invoice.ToString() // TODO: Should this be ReceivePayment?
                        //    },
                        //}.ToArray(), //m.LinkedTxn,
                        //        LineEx = m.LineEx
                        //    }).ToArray();

                        //    Payment payment = new Payment
                        //    {
                        //        ProcessPayment = false,
                        //        CustomerRef = new ReferenceType { name = CustomerName, Value = QBOCustomerID },
                        //        // CurrencyRef = new ReferenceType { type = "Currency", Value = "CAD" },
                        //        TotalAmt = Convert.ToDecimal(TotalAmount),
                        //        TotalAmtSpecified = true,
                        //        // DepositToAccountRef = ArRef,
                        //        Line = invoiceLines, // Variable is for debugging purposes - it should be inline or call a method. 
                        //        PaymentRefNum = reference,
                        //        PaymentMethodRef = new ReferenceType()
                        //        {
                        //            name = paymentName,
                        //            Value = paymentId,
                        //        }
                        //    };
                        //    Payment results = dataService.Add<Payment>(payment);
                        //    QboPaymentID = results.Id;
                        //}
                    }
                    catch
                    {
                        return QBOId;
                    }

                }

                return QBOId;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    if (innerException.ErrorCode == "6140")
                    {
                        isDuplicate_DocNumber = true;
                    }
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);
                }
                else
                {
                    return "-111";
                }

            }

            if (isDuplicate_DocNumber)
            {
                if (QBOId == "0")
                {
                    try
                    {
                        invoice.DocNumber = InvoiceNumberDoc;

                        if (invoice.DocNumber.Contains('-'))
                        {
                            string[] parts = invoice.DocNumber.Split('-');
                            if (parts.Length > 1)
                            {
                                if (parts.Length > 2)
                                {
                                    invoice.DocNumber = parts[0] + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper() + "-" + parts[2];
                                }
                                else
                                {
                                    invoice.DocNumber = Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper() + parts[1];
                                }

                                // The hyphen existed and the string was split
                            }

                        }
                        else
                        {
                            invoice.DocNumber = Guid.NewGuid().ToString("N").Substring(0, 3).ToUpper() + "-" + invoice.DocNumber;
                        }




                        Intuit.Ipp.Data.Invoice invoiceAdded = dataService.Add(invoice);
                        if (invoiceAdded != null)
                        {

                            if (invoiceAdded.Id != null)
                            {
                                QBOId = invoiceAdded.Id;
                            }
                            if (invoiceAdded.SyncToken != null)
                            {
                                SyncToken = invoiceAdded.SyncToken;
                            }


                            try
                            {
                                string Sql = "update tbl_Invoice set Number='" + invoice.DocNumber + "'" +
                              " where id='" + InvoiceID + "'" +
                              " and CompnyID='" + HttpContext.Current.Session["CompanyID"].ToString() + "' ";

                                db.Execute(Sql);
                            }
                            catch { }



                        }

                        return QBOId;
                    }
                    catch (Exception ex)
                    {
                        return "-111";
                    }


                }
                return QBOId;
            }
            else
            {
                return QBOId;
            }
        }



        public string UpdateQboInvoiceById(QBOSettins qBoStngPost, string CompanyID, string QboId)
        {
            try
            {
                QBOManager qBOManager = new QBOManager();
                ServiceContext context = qBOManager.GetServiceContext(qBoStngPost, CompanyID);
                string EXISTING_INVOICE_QUERYBYID = string.Format("select * from Invoice where id = '{0}'", QboId);
                var queryService = new QueryService<Intuit.Ipp.Data.Invoice>(context);
                Intuit.Ipp.Data.Invoice objInvoiceFound = queryService.ExecuteIdsQuery(EXISTING_INVOICE_QUERYBYID).FirstOrDefault<Intuit.Ipp.Data.Invoice>();

                if (objInvoiceFound != null)
                {
                    Intuit.Ipp.Data.Invoice ObjInvoice = new Intuit.Ipp.Data.Invoice();
                    ObjInvoice.Id = objInvoiceFound.Id;
                    ObjInvoice.SyncToken = objInvoiceFound.SyncToken;
                    ObjInvoice.CustomerRef = new ReferenceType();
                    ObjInvoice.CustomerRef = objInvoiceFound.CustomerRef;
                    List<Line> LineList = new List<Line>();

                    for (int i = 0; i < objInvoiceFound.Line.Count(); i++)
                    {
                        if (objInvoiceFound.Line[i].DetailType == LineDetailTypeEnum.SalesItemLineDetail)
                        {
                            objInvoiceFound.Line[i].Amount = 150;
                            objInvoiceFound.Line[i].Description = "This Description is for invoice";
                            LineList.Add(objInvoiceFound.Line[i]);
                        }
                    }

                    ObjInvoice.Line = LineList.ToArray();
                    DataService dataService = new DataService(context);
                    Intuit.Ipp.Data.Invoice UpdateEntity = dataService.Update<Intuit.Ipp.Data.Invoice>(ObjInvoice);
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message.ToString();

                return str;
            }
            return "";
        }

        public bool CheckExistInvoice(QBOSettins qBoStngPost, string CompanyID, string Id)
        {
            string Msg = "";
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();
                string Sql = @"select * from tbl_Invoice A Where A.CompnyID = '" + CompanyID + "' And A.id='" + Id + "'";
                DataTable dt;
                db.Execute(Sql, out dt);
                db.Close();
                if (dt.Rows.Count > 0)
                {
                    string QboId = dt.Rows[0]["QboId"].ToString();
                    QBOManager qBOManager = new QBOManager();
                    ServiceContext context = qBOManager.GetServiceContext(qBoStngPost, CompanyID);
                    string EXISTING_INVOICE_QUERYBYID = string.Format("select * from Invoice where id = '{0}'", QboId);
                    var queryService = new QueryService<Intuit.Ipp.Data.Invoice>(context);
                    Intuit.Ipp.Data.Invoice objInvoiceFound = queryService.ExecuteIdsQuery(EXISTING_INVOICE_QUERYBYID).FirstOrDefault<Intuit.Ipp.Data.Invoice>();

                    //context.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                    //context.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                    //context.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;
                    //context.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;




                    if (objInvoiceFound != null)
                    {
                        DataService dataService = new DataService(context);
                        //context.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;
                        //context.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Xml;

                        context.IppConfiguration.Message.Request.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;
                        context.IppConfiguration.Message.Response.SerializationFormat = Intuit.Ipp.Core.Configuration.SerializationFormat.Json;

                        Intuit.Ipp.Data.Invoice DeleteEntity = dataService.Delete<Intuit.Ipp.Data.Invoice>(objInvoiceFound);
                        if (DeleteEntity != null && !string.IsNullOrEmpty(DeleteEntity.Id) && DeleteEntity.status == EntityStatusEnum.Deleted)
                        {
                            return true;
                        }
                    }
                }
            }

            catch (IdsException ex)
            {
                //return ex.Message.ToString();
                return true;

            }
            return false;
        }

        public void ItemCheck(QBOSettins qboSettings,
            string CompanyID,
            string Id,
            ref string QboId,
            ref string QboName,
            ref string QboDesc,
             ServiceContext context)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

                string Sql = "select Id,Name,Description,Barcode, (case when IsTaxable = 'FALSE' then 'NO' else 'YES' end )as IsTaxable,Price,QboId from Items where Id='" + Id + "' and CompanyID='" + CompanyID + "' and IsDeleted=0 ";
                DataTable dt;
                db.Execute(Sql, out dt);
                DataRow dr = null;
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        dr = dt.Rows[0];
                        QboId = dr["QboId"].ToString();
                        QboName = dr["Name"].ToString();
                        QboDesc = dr["Description"].ToString();
                        return;
                    }

                }
                bool isExists = false;
                if (dr != null)
                {
                    string qboQuery = "select * from Item ";

                    QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(context);




                    List<Intuit.Ipp.Data.Item> listItems = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Item>();

                    foreach (Intuit.Ipp.Data.Item ilst in listItems)
                    {
                        if (!string.IsNullOrEmpty(dr["QboId"].ToString()) && dr["QboId"].ToString() != "0")
                        {
                            if (ilst.Id == dr["QboId"].ToString())
                            {
                                QboId = dr["QboId"].ToString();
                                QboName = dr["Name"].ToString();
                                QboDesc = dr["Description"].ToString();
                                return;
                            }
                        }
                    }

                    foreach (Intuit.Ipp.Data.Item ilst in listItems)
                    {
                        // Update QBo Id
                        if (ilst.Name == dr["Name"].ToString())
                        {
                            try
                            {
                                QboId = ilst.Id;
                                QboName = ilst.Name;

                                Sql = "update Items set QboId=" + ilst.Id + " where Id='" + Id + "' and CompanyID='" + CompanyID + "'";
                                // msSchedulerV3 DB
                                //  db.Open();
                                db.Execute(Sql);
                                db.Close();


                                // myServiceJobs DB
                                if (CompanyID.All(char.IsNumber))
                                {
                                    db = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
                                    //  db.Open();

                                    Sql = "select Id from Items where Name='" + dr["Name"].ToString() + "' and CompanyID='" + CompanyID + "' ";
                                    DataTable dtJobItem;
                                    db.Execute(Sql, out dtJobItem);
                                    if (dtJobItem.Rows.Count > 0)
                                    {
                                        string jId = dtJobItem.Rows[0]["Id"].ToString();
                                        Sql = "update Items set QboId=" + ilst.Id + " where Id='" + jId + "' and CompanyID='" + CompanyID + "'";
                                        db.Execute(Sql);
                                    }
                                    db.Close();
                                }

                                isExists = true;
                                break;
                            }
                            catch { }
                        }
                    }
                }


                if (!isExists)
                {
                    bool isTaxable = dr["IsTaxable"].ToString() == "YES" ? true : false;
                    Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                    Itm.Name = dr["Name"].ToString();
                    Itm.Description = dr["Description"].ToString();
                    Itm.Taxable = isTaxable;
                    Itm.UnitPrice = decimal.Parse(dr["Price"].ToString());

                    Itm.TypeSpecified = true;
                    Itm.Type = ItemTypeEnum.Service;

                    Itm.TrackQtyOnHand = false;
                    Itm.TrackQtyOnHandSpecified = false;
                    Itm.QtyOnHandSpecified = false;
                    Itm.QtyOnHand = 0;
                    Itm.InvStartDateSpecified = true;
                    Itm.InvStartDate = DateTime.Now;
                    Itm.UnitPriceSpecified = true;
                    Itm.PurchaseDesc = "";
                    Itm.PurchaseCostSpecified = true;
                    Itm.PurchaseCost = 0;


                    QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                    var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                    var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income).FirstOrDefault();
                    if (AssetAccountRef != null)
                    {
                        Itm.IncomeAccountRef = new ReferenceType();
                        Itm.IncomeAccountRef.Value = AssetAccountRef.Id;
                    }


                    //QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                    //var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                    //var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.OtherCurrentAsset && x.Name == "Inventory Asset").FirstOrDefault();
                    //if (AssetAccountRef != null)
                    //{
                    //    Itm.AssetAccountRef = new ReferenceType();
                    //    Itm.AssetAccountRef.Value = AssetAccountRef.Id;
                    //}

                    //var IncomeAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income && x.Name == "Sales of Product Income").FirstOrDefault();
                    //if (IncomeAccountRef != null)
                    //{
                    //    Itm.IncomeAccountRef = new ReferenceType();
                    //    Itm.IncomeAccountRef.Value = IncomeAccountRef.Id;
                    //}

                    //var ExpenseAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.CostofGoodsSold && x.Name == "Cost of Goods Sold").FirstOrDefault();
                    //if (ExpenseAccountRef != null)
                    //{
                    //    Itm.ExpenseAccountRef = new ReferenceType();
                    //    Itm.ExpenseAccountRef.Value = ExpenseAccountRef.Id;
                    //}

                    DataService dataService = new DataService(context);
                    Intuit.Ipp.Data.Item itemAdd = dataService.Add(Itm);

                    QboId = itemAdd.Id;
                    QboName = dr["Name"].ToString();
                    QboDesc = dr["Description"].ToString();
                    // msSchedulerV3 DB
                    //  db.Open();
                    Sql = "update Items set QboId=" + itemAdd.Id + " where Id='" + Id + "' and CompanyID='" + CompanyID + "'";
                    db.Execute(Sql);
                    db.Close();

                }
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    // throw new ApplicationException(innerException.Detail);
                }
            }
        }
        public void ItemCheck_Back(QBOSettins qboSettings, string CompanyID, string Id, ref string QboId, ref string QboName)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();
                string Sql = "select Id,Name,Description,Barcode, (case when IsTaxable = 'FALSE' then 'NO' else 'YES' end )as IsTaxable,Price,QboId from Items where Id='" + Id + "' and CompanyID='" + CompanyID + "' and IsDeleted=0 ";
                DataTable dt;
                db.Execute(Sql, out dt);
                db.Close();
                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(dr["QboId"].ToString()) && dr["QboId"].ToString() != "0")
                {
                    QboId = dr["QboId"].ToString();
                    QboName = dr["Name"].ToString();
                }
                else
                {
                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    string qboQuery = "select * from Item ";
                    QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(context);
                    List<Intuit.Ipp.Data.Item> listItems = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Item>();
                    bool isExists = false;
                    foreach (Intuit.Ipp.Data.Item ilst in listItems)
                    {
                        // Update QBo Id
                        if (ilst.Name == dr["Name"].ToString())
                        {
                            try
                            {
                                QboId = ilst.Id;
                                QboName = ilst.Name;

                                Sql = "update Items set QboId=" + ilst.Id + " where Id='" + Id + "' and CompanyID='" + CompanyID + "'";
                                // msSchedulerV3 DB
                                db.Execute(Sql);
                                db.Close();


                                // myServiceJobs DB
                                if (CompanyID.All(char.IsNumber))
                                {
                                    db = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
                                    //  db.Open();
                                    Sql = "update Items set QboId=" + ilst.Id + " where Id='" + Id + "' and CompanyID='" + CompanyID + "'";
                                    db.Execute(Sql);
                                    db.Close();
                                }

                                isExists = true;
                                break;
                            }
                            catch { }
                        }
                    }
                    if (!isExists)
                    {
                        bool isTaxable = dr["IsTaxable"].ToString() == "YES" ? true : false;
                        Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                        Itm.Name = dr["Name"].ToString();
                        Itm.Description = dr["Description"].ToString();
                        Itm.Taxable = isTaxable;
                        Itm.UnitPrice = decimal.Parse(dr["Price"].ToString());

                        Itm.TypeSpecified = true;
                        Itm.Type = ItemTypeEnum.Inventory;
                        Itm.TrackQtyOnHand = true;
                        Itm.TrackQtyOnHandSpecified = true;
                        Itm.QtyOnHandSpecified = true;
                        Itm.QtyOnHand = 0;
                        Itm.InvStartDateSpecified = true;
                        Itm.InvStartDate = DateTime.Now;
                        Itm.UnitPriceSpecified = true;
                        Itm.PurchaseDesc = "";
                        Itm.PurchaseCostSpecified = true;
                        Itm.PurchaseCost = 0;


                        QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                        var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                        var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income).FirstOrDefault();
                        if (AssetAccountRef != null)
                        {
                            Itm.IncomeAccountRef = new ReferenceType();
                            Itm.IncomeAccountRef.Value = AssetAccountRef.Id;
                        }

                        DataService dataService = new DataService(context);
                        Intuit.Ipp.Data.Item itemAdd = dataService.Add(Itm);

                        QboId = itemAdd.Id;
                        QboName = dr["Name"].ToString();


                    }
                }
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);
                }
            }
        }
        private void SyncBusinessCotactToQbo(ServiceContext context, string CompanyID)
        {

            if (HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg")
            {
                SyncBussinessContactToQboForLHG(context, CompanyID);
            }
            else
            {
                SyncBussinessContactToQboForOthers(context, CompanyID);
            }


        }

        private void SyncBussinessContactToQboForOthers(ServiceContext context, string CompanyID)
        {
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

            string Sql = @"select * from tbl_Customer Where QboId='0' and IsBusinessContact=1 and  [CompanyID] = @CompanyID and CustomerID in (select CustomerID from tbl_Invoice where CompnyID = '" + CompanyID + "') ;";


            DataSet dataSet = db.Get_DataSet(Sql, CompanyID);

            string InsertSql = string.Empty;
            if (dataSet.Tables[0] != null)
            {
                foreach (DataRow dr in dataSet.Tables[0].Rows)
                {
                    try
                    {
                        string Name = dr["BusinessName"].ToString().Trim();
                        string custId = dr["CustomerID"].ToString().Trim();
                        Name = Name.Replace("  ", " ");

                        Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
                        _Customer.Title = "";
                        _Customer.CompanyName = Name;


                        Name = dr["FirstName"].ToString().Trim();
                        if (!string.IsNullOrEmpty(dr["LastName"].ToString().Trim()))
                            Name += " " + dr["LastName"].ToString().Trim();

                        Name = Name.Replace("  ", " ");


                        _Customer.Title = dr["title"].ToString().Trim();
                        _Customer.GivenName = Name;
                        _Customer.MiddleName = dr["LastName"].ToString().Trim();
                        _Customer.FamilyName = dr["FirstName"].ToString().Trim();




                        JobInfo jobInfo = new JobInfo();
                        //jobInfo.Description 
                        //Cust.JobInfo
                        _Customer.DisplayName = Name;
                        Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                        ObjEmail.Address = dr["Email"].ToString();
                        _Customer.PrimaryEmailAddr = ObjEmail;
                        _Customer.Active = true;

                        PhysicalAddress ObjAddress = new PhysicalAddress();
                        ObjAddress.PostalCode = dr["ZipCode"].ToString();
                        ObjAddress.Country = "";
                        ObjAddress.Line1 = dr["Address1"].ToString();
                        ObjAddress.City = dr["City"].ToString();
                        ObjAddress.CountrySubDivisionCode = dr["State"].ToString();
                        _Customer.BillAddr = ObjAddress;

                        TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                        ObjTelephoneNumber.FreeFormNumber = dr["Mobile"].ToString();
                        _Customer.PrimaryPhone = ObjTelephoneNumber;


                        // Identify As Business Contact

                        List<CustomField> customFields = new List<CustomField>();

                        CustomField customField = new CustomField();
                        customField.Type = CustomFieldTypeEnum.StringType;
                        customField.Name = "BusinessContact";
                        customField.DefinitionId = "BusinessContact";
                        customFields.Add(customField);

                        _Customer.CustomField = customFields.ToArray();

                        _Customer.ParentRef = null;

                        _Customer.Job = false;
                        _Customer.JobSpecified = false;



                        DataService dataService = new DataService(context);
                        Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(_Customer);

                        InsertSql += "update [msSchedulerV3].[dbo].[tbl_Customer] set QboId=" + CustomerAdd.Id + " Where CompanyID='" + CompanyID + "' and [CustomerID] = '" + dr["CustomerID"].ToString() + "';";

                    }
                    catch (Exception ex)
                    {

                    }

                }

            }

            if (!string.IsNullOrEmpty(InsertSql))
            {
                db.Execute(InsertSql);
            }

        }
        private void SyncBussinessContactToQboForLHG(ServiceContext context, string CompanyID)
        {
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

            string Sql = @"select * from tbl_Customer Where QboId='0' and IsBusinessContact=1 and  [CompanyID] = @CompanyID and CustomerID in (select CustomerID from tbl_Invoice where CompnyID = '" + CompanyID + "') ;";


            DataSet dataSet = db.Get_DataSet(Sql, CompanyID);

            string InsertSql = string.Empty;
            if (dataSet.Tables[0] != null)
            {
                foreach (DataRow dr in dataSet.Tables[0].Rows)
                {
                    try
                    {
                        string Name = dr["BusinessName"].ToString().Trim();
                        string custId = dr["CustomerID"].ToString().Trim();
                        string custGuidId = dr["CustomerGuid"].ToString().Trim();
                        Name = Name.Replace("  ", " ");



                        //string sqlCustInvEst = @"select * from tbl_Invoice where customerid = " + custId + " and compnyid = '" + CompanyID + "'";
                        //DataTable dtInv = new DataTable();
                        //db.Execute(sqlCustInvEst, out dtInv);

                        //if (dtInv.Rows.Count == 0) continue;

                        Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
                        _Customer.Title = "";
                        _Customer.CompanyName = Name;


                        Name = dr["FirstName"].ToString().Trim();
                        if (!string.IsNullOrEmpty(dr["LastName"].ToString().Trim()))
                            Name += " " + dr["LastName"].ToString().Trim();

                        Name = Name.Replace("  ", " ");


                        _Customer.Title = dr["title"].ToString().Trim();
                        _Customer.GivenName = Name;
                        _Customer.MiddleName = dr["LastName"].ToString().Trim();
                        _Customer.FamilyName = dr["FirstName"].ToString().Trim();




                        JobInfo jobInfo = new JobInfo();
                        //jobInfo.Description 
                        //Cust.JobInfo
                        _Customer.DisplayName = Name;
                        Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                        ObjEmail.Address = dr["Email"].ToString();
                        _Customer.PrimaryEmailAddr = ObjEmail;
                        _Customer.Active = true;



                        string sqlCustBillingAddress = @"select * from tbl_CustomerBillingAddress where isActive=1 and Customer_ID = '" + custGuidId + "'";
                        DataTable dtBillAddress = new DataTable();
                        db.Execute(sqlCustBillingAddress, out dtBillAddress);


                        //Sync Customer billing address to Qbo by Mohsin
                        if (dtBillAddress.Rows.Count > 0)
                        {
                            foreach (DataRow row in dtBillAddress.Rows)
                            {
                                PhysicalAddress ObjAddress = new PhysicalAddress();
                                ObjAddress.PostalCode = row["ZipCode"].ToString();
                                ObjAddress.Country = "";
                                ObjAddress.Line1 = row["Address"].ToString();
                                ObjAddress.City = row["City"].ToString();
                                ObjAddress.CountrySubDivisionCode = row["State"].ToString();
                                _Customer.BillAddr = ObjAddress;

                            }
                        }
                        else
                        {
                            PhysicalAddress ObjAddress = new PhysicalAddress();
                            ObjAddress.PostalCode = dr["ZipCode"].ToString();
                            ObjAddress.Country = "";
                            ObjAddress.Line1 = dr["Address1"].ToString();
                            ObjAddress.City = dr["City"].ToString();
                            ObjAddress.CountrySubDivisionCode = dr["State"].ToString();
                            _Customer.BillAddr = ObjAddress;
                        }
                        //end Sync Mohsin

                        TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                        ObjTelephoneNumber.FreeFormNumber = dr["Mobile"].ToString();
                        _Customer.PrimaryPhone = ObjTelephoneNumber;


                        // Identify As Business Contact

                        List<CustomField> customFields = new List<CustomField>();

                        CustomField customField = new CustomField();
                        customField.Type = CustomFieldTypeEnum.StringType;
                        customField.Name = "BusinessContact";
                        customField.DefinitionId = "BusinessContact";
                        customFields.Add(customField);

                        _Customer.CustomField = customFields.ToArray();

                        _Customer.ParentRef = null;

                        _Customer.Job = false;
                        _Customer.JobSpecified = false;



                        DataService dataService = new DataService(context);
                        Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(_Customer);

                        InsertSql += "update [msSchedulerV3].[dbo].[tbl_Customer] set QboId=" + CustomerAdd.Id + " Where CompanyID='" + CompanyID + "' and [CustomerID] = '" + dr["CustomerID"].ToString() + "';";

                    }
                    catch (Exception ex)
                    {

                    }

                }

            }

            if (!string.IsNullOrEmpty(InsertSql))
            {
                db.Execute(InsertSql);
            }

        }
        public bool CustomerUpdateToQBO(ServiceContext context,
            QBOSettins qBoStngPost,
            string CompanyID,
            string QboLastUpdatedTime)
        {
            try
            {

                ServiceContext serviceContext;
                if (context != null)
                {
                    serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                else
                {
                    serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";


                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

                string Sql = @"select * from tbl_Customer Where  QboId !='0' and  CompanyID = '" + CompanyID + "' and convert(date, UpdateDate, 103)  >='" + QboLastUpdatedTime + "' ;";


                DataTable dt;
                DataTable dt_Business = new DataTable();
                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                dt = dataSet.Tables[0];





                foreach (DataRow dr_Customer in dt.Rows)
                {
                    string Name = dr_Customer["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr_Customer["LastName"].ToString().Trim()))
                        Name += " " + dr_Customer["LastName"].ToString().Trim();

                    Name = Name.Replace("  ", " ");

                    Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
                    _Customer.Title = "";
                    _Customer.GivenName = Name;
                    _Customer.Id = dr_Customer["QboId"].ToString().Trim();
                    _Customer.SyncToken = dr_Customer["SyncToken"].ToString().Trim();
                    try
                    {
                        if (dr_Customer["BusinessID"].ToString() != "0")
                        {

                            foreach (DataRow dataRow in dt_Business.Rows)
                            {
                                if (dataRow["CustomerID"].ToString().Trim() == dr_Customer["BusinessID"].ToString())
                                {
                                    _Customer.CompanyName = dataRow["BusinessName"].ToString().Trim();

                                    _Customer.Job = true;
                                    _Customer.JobSpecified = true;
                                    _Customer.Level = 1;

                                    ReferenceType referenceType = new ReferenceType();
                                    referenceType.name = dataRow["BusinessName"].ToString().Trim();
                                    referenceType.Value = dataRow["QboId"].ToString().Trim();
                                    referenceType.type = "Business";

                                    _Customer.ParentRef = referenceType;


                                    if (Convert.ToBoolean(dr_Customer["IsPrimaryContact"].ToString()))
                                    {
                                        List<CustomField> customFields = new List<CustomField>();

                                        CustomField customField = new CustomField();
                                        customField.Type = CustomFieldTypeEnum.StringType;
                                        customField.Name = "PrimaryContact";
                                        customField.DefinitionId = "PrimaryContact";
                                        customFields.Add(customField);

                                        _Customer.CustomField = customFields.ToArray();
                                    }
                                }

                            }
                        }

                        _Customer.Title = dr_Customer["title"].ToString().Trim();
                        _Customer.GivenName = Name;
                        _Customer.MiddleName = dr_Customer["LastName"].ToString().Trim();
                        _Customer.FamilyName = dr_Customer["FirstName"].ToString().Trim();
                        JobInfo jobInfo = new JobInfo();
                        //jobInfo.Description 
                        //Cust.JobInfo
                        _Customer.DisplayName = Name;
                        Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                        ObjEmail.Address = dr_Customer["Email"].ToString();
                        _Customer.PrimaryEmailAddr = ObjEmail;
                        _Customer.Active = true;

                        PhysicalAddress ObjAddress = new PhysicalAddress();
                        ObjAddress.PostalCode = dr_Customer["ZipCode"].ToString();
                        ObjAddress.Country = "";
                        ObjAddress.Line1 = dr_Customer["Address1"].ToString();
                        ObjAddress.City = dr_Customer["City"].ToString();
                        ObjAddress.CountrySubDivisionCode = dr_Customer["State"].ToString();
                        _Customer.BillAddr = ObjAddress;

                        //  _Customer.par

                        TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                        ObjTelephoneNumber.FreeFormNumber = dr_Customer["Mobile"].ToString();
                        _Customer.PrimaryPhone = ObjTelephoneNumber;

                        DataService dataService = new DataService(context);
                        Intuit.Ipp.Data.Customer CustomerAdd = dataService.Update(_Customer);

                        Sql = "update tbl_Customer set SyncToken='" + CustomerAdd.SyncToken + "' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
                        db.Execute(Sql);
                    }
                    catch (Exception ex) { }


                }




            }
            catch (Exception ex)
            {
            }
            return true;
        }


        public bool CustomerSyncToQBO(ServiceContext context, QBOSettins qBoStngPost, string CompanyID)
        {
            //try
            //{

            //    ServiceContext serviceContext;
            //    if (context != null)
            //    {
            //        serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            //    }
            //    else
            //    {
            //        serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

            //    }
            //    serviceContext.IppConfiguration.MinorVersion.Qbo = "23";

            //    bool IsPro = false;
            //    if (HttpContext.Current.Session["IsCecPro"] != null)
            //    {
            //        IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
            //        if (IsPro)
            //        {
            //            SyncBusinessCotactToQbo(context, CompanyID);
            //        }
            //    }
            //    Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

            //    string Sql = @"select * from tbl_Customer Where BusinessID='0' and  QboId='0' and  CompanyID = '" + CompanyID + "';";

            //    if (HttpContext.Current.Session["IsCecPro"] != null)
            //    {
            //        IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
            //        if (IsPro)
            //        {
            //            Sql += @"SELECT [BusinessName],BusinessID,CustomerID,QboId FROM [msSchedulerV3].[dbo].[tbl_Customer] Where [CompanyID] = @CompanyID and [IsBusinessContact] = 1;";
            //        }
            //    }
            //    DataTable dt;
            //    DataTable dt_Business = new DataTable();
            //    DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
            //    dt = dataSet.Tables[0];
            //    if (IsPro)
            //    {
            //        dt_Business = dataSet.Tables[1];
            //    }




            //    foreach (DataRow dr_Customer in dt.Rows)
            //    {
            //        string Name = dr_Customer["FirstName"].ToString().Trim();
            //        if (!string.IsNullOrEmpty(dr_Customer["LastName"].ToString().Trim()))
            //            Name += " " + dr_Customer["LastName"].ToString().Trim();
            //        //From Mohsin vai 
            //        string custId = dr_Customer["CustomerID"].ToString().Trim();

            //        Name = Name.Replace("  ", " ");

            //        //string sqlCustInvEst = @"select * from tbl_Customer c inner join tbl_Invoice i on  c.CustomerID=i.CustomerId where  i.CustomerId='" + custId + "' and c.CompanyID='" + CompanyID + "'";
            //        string sqlCustInvEst = @"select * from tbl_Invoice where customerid = "+ custId + " and compnyid = '"+CompanyID+"'";
            //        DataTable dtInv = new DataTable();
            //        db.Execute(sqlCustInvEst, out dtInv);

            //        if (dtInv.Rows.Count == 0) continue;
            //        //end 
            //        //Name = Name.Replace("  ", " ");

            //        Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
            //        _Customer.Title = "";
            //        _Customer.GivenName = Name;
            //        try
            //        {
            //            if (dr_Customer["BusinessID"].ToString() != "0")
            //            {

            //                foreach (DataRow dataRow in dt_Business.Rows)
            //                {
            //                    if (dataRow["CustomerID"].ToString().Trim() == dr_Customer["BusinessID"].ToString())
            //                    {
            //                        _Customer.CompanyName = dataRow["BusinessName"].ToString().Trim();

            //                        _Customer.Job = true;
            //                        _Customer.JobSpecified = true;
            //                        _Customer.Level = 1;

            //                        ReferenceType referenceType = new ReferenceType();
            //                        referenceType.name = dataRow["BusinessName"].ToString().Trim();
            //                        referenceType.Value = dataRow["QboId"].ToString().Trim();
            //                        referenceType.type = "Business";

            //                        _Customer.ParentRef = referenceType;


            //                        if (Convert.ToBoolean(dr_Customer["IsPrimaryContact"].ToString()))
            //                        {
            //                            List<CustomField> customFields = new List<CustomField>();

            //                            CustomField customField = new CustomField();
            //                            customField.Type = CustomFieldTypeEnum.StringType;
            //                            customField.Name = "PrimaryContact";
            //                            customField.DefinitionId = "PrimaryContact";
            //                            customFields.Add(customField);

            //                            _Customer.CustomField = customFields.ToArray();
            //                        }
            //                    }

            //                }
            //            }

            //            _Customer.Title = dr_Customer["title"].ToString().Trim();
            //            _Customer.GivenName = Name;
            //            _Customer.MiddleName = dr_Customer["LastName"].ToString().Trim();
            //            _Customer.FamilyName = dr_Customer["FirstName"].ToString().Trim();
            //            JobInfo jobInfo = new JobInfo();
            //            //jobInfo.Description 
            //            //Cust.JobInfo
            //            _Customer.DisplayName = Name;
            //            Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
            //            ObjEmail.Address = dr_Customer["Email"].ToString();
            //            _Customer.PrimaryEmailAddr = ObjEmail;
            //            _Customer.Active = true;

            //            PhysicalAddress ObjAddress = new PhysicalAddress();
            //            ObjAddress.PostalCode = dr_Customer["ZipCode"].ToString();
            //            ObjAddress.Country = "";
            //            ObjAddress.Line1 = dr_Customer["Address1"].ToString();
            //            ObjAddress.City = dr_Customer["City"].ToString();
            //            ObjAddress.CountrySubDivisionCode = dr_Customer["State"].ToString();
            //            _Customer.BillAddr = ObjAddress;

            //            //  _Customer.par

            //            TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
            //            ObjTelephoneNumber.FreeFormNumber = dr_Customer["Mobile"].ToString();
            //            _Customer.PrimaryPhone = ObjTelephoneNumber;

            //            DataService dataService = new DataService(context);
            //            Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(_Customer);

            //            Sql = "update tbl_Customer set QboId='" + CustomerAdd.Id + "' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
            //            db.Execute(Sql);
            //        }
            //        catch {
            //            Sql = "update tbl_Customer set QboId='-11' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
            //            db.Execute(Sql);
            //        }


            //    }




            //}
            //catch (Exception ex)
            //{
            //}
            //return true;
            // Added By Munem
            bool status = false;
            if (HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg")
            {
                status = CustomerSyncToQBOForLHG(context, qBoStngPost, CompanyID);
            }
            else
            {
                status = CustomerSyncToQBOForOthers(context, qBoStngPost, CompanyID);
            }
            return status;
            //Munem
        }
        //Previous
        public bool CustomerSyncToQBOForOthers(ServiceContext context, QBOSettins qBoStngPost, string CompanyID)
        {
            try
            {

                ServiceContext serviceContext;
                if (context != null)
                {
                    serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                else
                {
                    serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";

                bool IsPro = false;
                if (HttpContext.Current.Session["IsCecPro"] != null)
                {
                    IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
                    if (IsPro)
                    {
                        SyncBusinessCotactToQbo(context, CompanyID);
                    }
                }
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

                string Sql = @"select * from tbl_Customer Where BusinessID='0' and  QboId='0' and  CompanyID = '" + CompanyID + "' and CustomerID in (select CustomerID from tbl_Invoice where CompnyID = '" + CompanyID + "') ;";

                if (HttpContext.Current.Session["IsCecPro"] != null)
                {
                    IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
                    if (IsPro)
                    {
                        Sql += @"SELECT [BusinessName],BusinessID,CustomerID,QboId FROM [msSchedulerV3].[dbo].[tbl_Customer] Where [CompanyID] = @CompanyID and [IsBusinessContact] = 1;";
                    }
                }
                DataTable dt;
                DataTable dt_Business = new DataTable();
                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                dt = dataSet.Tables[0];
                if (IsPro)
                {
                    dt_Business = dataSet.Tables[1];
                }




                foreach (DataRow dr_Customer in dt.Rows)
                {
                    string Name = dr_Customer["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr_Customer["LastName"].ToString().Trim()))
                        Name += " " + dr_Customer["LastName"].ToString().Trim();
                    Name = Name.Replace("  ", " ");

                    Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
                    _Customer.Title = "";
                    _Customer.GivenName = Name;
                    try
                    {
                        if (dr_Customer["BusinessID"].ToString() != "0")
                        {

                            foreach (DataRow dataRow in dt_Business.Rows)
                            {
                                if (dataRow["CustomerID"].ToString().Trim() == dr_Customer["BusinessID"].ToString())
                                {
                                    _Customer.CompanyName = dataRow["BusinessName"].ToString().Trim();

                                    _Customer.Job = true;
                                    _Customer.JobSpecified = true;
                                    _Customer.Level = 1;

                                    ReferenceType referenceType = new ReferenceType();
                                    referenceType.name = dataRow["BusinessName"].ToString().Trim();
                                    referenceType.Value = dataRow["QboId"].ToString().Trim();
                                    referenceType.type = "Business";

                                    _Customer.ParentRef = referenceType;


                                    if (Convert.ToBoolean(dr_Customer["IsPrimaryContact"].ToString()))
                                    {
                                        List<CustomField> customFields = new List<CustomField>();

                                        CustomField customField = new CustomField();
                                        customField.Type = CustomFieldTypeEnum.StringType;
                                        customField.Name = "PrimaryContact";
                                        customField.DefinitionId = "PrimaryContact";
                                        customFields.Add(customField);

                                        _Customer.CustomField = customFields.ToArray();
                                    }
                                }

                            }
                        }

                        _Customer.Title = dr_Customer["title"].ToString().Trim();
                        _Customer.GivenName = Name;
                        _Customer.MiddleName = dr_Customer["LastName"].ToString().Trim();
                        _Customer.FamilyName = dr_Customer["FirstName"].ToString().Trim();
                        JobInfo jobInfo = new JobInfo();
                        //jobInfo.Description 
                        //Cust.JobInfo
                        _Customer.DisplayName = Name;
                        Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                        ObjEmail.Address = dr_Customer["Email"].ToString();
                        _Customer.PrimaryEmailAddr = ObjEmail;
                        _Customer.Active = true;

                        PhysicalAddress ObjAddress = new PhysicalAddress();
                        ObjAddress.PostalCode = dr_Customer["ZipCode"].ToString();
                        ObjAddress.Country = "";
                        ObjAddress.Line1 = dr_Customer["Address1"].ToString();
                        ObjAddress.City = dr_Customer["City"].ToString();
                        ObjAddress.CountrySubDivisionCode = dr_Customer["State"].ToString();
                        _Customer.BillAddr = ObjAddress;

                        //  _Customer.par

                        TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                        ObjTelephoneNumber.FreeFormNumber = dr_Customer["Mobile"].ToString();
                        _Customer.PrimaryPhone = ObjTelephoneNumber;

                        DataService dataService = new DataService(serviceContext);
                        Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(_Customer);

                        Sql = "update tbl_Customer set QboId='" + CustomerAdd.Id + "' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
                        db.Execute(Sql);
                    }
                    catch
                    {
                        Sql = "update tbl_Customer set QboId='-11' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
                        db.Execute(Sql);
                    }


                }




            }
            catch (Exception ex)
            {
            }
            return true;
        }
        //

        //Modified for LHG
        public bool CustomerSyncToQBOForLHG(ServiceContext context, QBOSettins qBoStngPost, string CompanyID)
        {
            try
            {

                ServiceContext serviceContext;
                if (context != null)
                {
                    serviceContext = context; //QBOManager.GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                else
                {
                    serviceContext = GetServiceContext(qBoStngPost, HttpContext.Current.Session["CompanyID"].ToString());

                }
                serviceContext.IppConfiguration.MinorVersion.Qbo = "23";

                bool IsPro = false;
                if (HttpContext.Current.Session["IsCecPro"] != null)
                {
                    IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
                    if (IsPro)
                    {
                        SyncBusinessCotactToQbo(context, CompanyID);
                    }
                }
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

                string Sql = @"select * from tbl_Customer Where BusinessID='0' and  QboId='0' and  CompanyID = '" + CompanyID + "';";

                if (HttpContext.Current.Session["IsCecPro"] != null)
                {
                    IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
                    if (IsPro)
                    {
                        Sql += @"SELECT [BusinessName],BusinessID,CustomerID,QboId FROM [msSchedulerV3].[dbo].[tbl_Customer] Where [CompanyID] = @CompanyID and [IsBusinessContact] = 1;";
                    }
                }
                DataTable dt;
                DataTable dt_Business = new DataTable();
                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                dt = dataSet.Tables[0];
                if (IsPro)
                {
                    dt_Business = dataSet.Tables[1];
                }




                foreach (DataRow dr_Customer in dt.Rows)
                {
                    string Name = dr_Customer["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr_Customer["LastName"].ToString().Trim()))
                        Name += " " + dr_Customer["LastName"].ToString().Trim();
                    //From Mohsin vai 
                    string custId = dr_Customer["CustomerID"].ToString().Trim();
                    string custGuidId = dr_Customer["CustomerGuid"].ToString().Trim();
                    Name = Name.Replace("  ", " ");

                    //string sqlCustInvEst = @"select * from tbl_Customer c inner join tbl_Invoice i on  c.CustomerID=i.CustomerId where  i.CustomerId='" + custId + "' and c.CompanyID='" + CompanyID + "'";
                    //string sqlCustInvEst = @"select * from tbl_Invoice where customerid = " + custId + " and compnyid = '" + CompanyID + "'";
                    //DataTable dtInv = new DataTable();
                    //db.Execute(sqlCustInvEst, out dtInv);

                    //if (dtInv.Rows.Count == 0) continue;
                    //end 
                    //Name = Name.Replace("  ", " ");

                    string sqlCustBillingAddress = @"select * from tbl_CustomerBillingAddress where isActive=1 and Customer_ID = '" + custGuidId + "'";
                    DataTable dtBillAddress = new DataTable();
                    db.Execute(sqlCustBillingAddress, out dtBillAddress);

                    Intuit.Ipp.Data.Customer _Customer = new Intuit.Ipp.Data.Customer();
                    _Customer.Title = "";
                    _Customer.GivenName = Name;
                    try
                    {
                        if (dr_Customer["BusinessID"].ToString() != "0")
                        {

                            foreach (DataRow dataRow in dt_Business.Rows)
                            {
                                if (dataRow["CustomerID"].ToString().Trim() == dr_Customer["BusinessID"].ToString())
                                {
                                    _Customer.CompanyName = dataRow["BusinessName"].ToString().Trim();

                                    _Customer.Job = true;
                                    _Customer.JobSpecified = true;
                                    _Customer.Level = 1;

                                    ReferenceType referenceType = new ReferenceType();
                                    referenceType.name = dataRow["BusinessName"].ToString().Trim();
                                    referenceType.Value = dataRow["QboId"].ToString().Trim();
                                    referenceType.type = "Business";

                                    _Customer.ParentRef = referenceType;


                                    if (Convert.ToBoolean(dr_Customer["IsPrimaryContact"].ToString()))
                                    {
                                        List<CustomField> customFields = new List<CustomField>();

                                        CustomField customField = new CustomField();
                                        customField.Type = CustomFieldTypeEnum.StringType;
                                        customField.Name = "PrimaryContact";
                                        customField.DefinitionId = "PrimaryContact";
                                        customFields.Add(customField);

                                        _Customer.CustomField = customFields.ToArray();
                                    }
                                }

                            }
                        }

                        _Customer.Title = dr_Customer["title"].ToString().Trim();
                        _Customer.GivenName = Name;
                        _Customer.MiddleName = dr_Customer["LastName"].ToString().Trim();
                        _Customer.FamilyName = dr_Customer["FirstName"].ToString().Trim();
                        JobInfo jobInfo = new JobInfo();
                        //jobInfo.Description 
                        //Cust.JobInfo
                        _Customer.DisplayName = Name;
                        Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                        ObjEmail.Address = dr_Customer["Email"].ToString();
                        _Customer.PrimaryEmailAddr = ObjEmail;
                        _Customer.Active = true;


                        //Sync Customer billing address to Qbo by Mohsin
                        if (dtBillAddress.Rows.Count > 0)
                        {
                            foreach (DataRow row in dtBillAddress.Rows)
                            {
                                PhysicalAddress ObjAddress = new PhysicalAddress();
                                ObjAddress.PostalCode = row["ZipCode"].ToString();
                                ObjAddress.Country = "";
                                ObjAddress.Line1 = row["Address"].ToString();
                                ObjAddress.City = row["City"].ToString();
                                ObjAddress.CountrySubDivisionCode = row["State"].ToString();
                                _Customer.BillAddr = ObjAddress;

                            }
                        }
                        else
                        {
                            PhysicalAddress ObjAddress = new PhysicalAddress();
                            ObjAddress.PostalCode = dr_Customer["ZipCode"].ToString();
                            ObjAddress.Country = "";
                            ObjAddress.Line1 = dr_Customer["Address1"].ToString();
                            ObjAddress.City = dr_Customer["City"].ToString();
                            ObjAddress.CountrySubDivisionCode = dr_Customer["State"].ToString();
                            _Customer.BillAddr = ObjAddress;
                        }
                        //end Sync Mohsin
                        //  _Customer.par

                        TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                        ObjTelephoneNumber.FreeFormNumber = dr_Customer["Mobile"].ToString();
                        _Customer.PrimaryPhone = ObjTelephoneNumber;

                        DataService dataService = new DataService(serviceContext);
                        Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(_Customer);

                        Sql = "update tbl_Customer set QboId='" + CustomerAdd.Id + "' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
                        db.Execute(Sql);
                    }
                    catch (Exception ex)
                    {
                        //throw ex;
                        Sql = "update tbl_Customer set QboId='-11' Where CompanyID='" + CompanyID + "' and CustomerID = '" + dr_Customer["CustomerID"].ToString() + "'";
                        db.Execute(Sql);
                    }


                }




            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }

        public void CustomerCheckTEST(ServiceContext context, string CompanyID, string Id, ref string QboId, ref string QboName)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();
                string Sql = @"select * from tbl_Customer A Where A.CompanyID = '" + CompanyID + "' And A.CustomerID='" + Id + "'";

                DataTable dt;
                db.Execute(Sql, out dt);

                DataRow dr = dt.Rows[0];
                // ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                //string qboQuery = "select * from Customer ";
                string qboQuery = "select * from Customer STARTPOSITION 1 MAXRESULTS 1000 ";
                QueryService<Intuit.Ipp.Data.Customer> qsItem = new QueryService<Intuit.Ipp.Data.Customer>(context);
                List<Intuit.Ipp.Data.Customer> listCustomer = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Customer>();
                bool isExists = false;
                foreach (Intuit.Ipp.Data.Customer ilst in listCustomer)
                {
                    if (!string.IsNullOrEmpty(dr["QboId"].ToString()))
                    {
                        if (ilst.Id == dr["QboId"].ToString())
                        {
                            QboId = dr["QboId"].ToString();
                            QboName = dr["FirstName"].ToString();
                            return;
                        }
                    }
                }


                foreach (Intuit.Ipp.Data.Customer ilst in listCustomer)
                {
                    // Update QBo Id
                    string Name = dr["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr["LastName"].ToString().Trim()))
                        Name += " " + dr["LastName"].ToString().Trim();
                    Name = Name.Replace("  ", " ");
                    string DisplayName = ilst.DisplayName.Replace("  ", " ");
                    if (DisplayName.ToUpper().Trim() == Name.ToUpper().Trim())
                    {
                        Sql = "update tbl_Customer set QboId=" + ilst.Id + " Where CompanyID='" + CompanyID + "' and CustomerID = '" + Id + "'";
                        db.Execute(Sql);
                        QboId = ilst.Id;
                        QboName = Name;
                        isExists = true;
                        break;
                    }


                }
                // Insert New 
                if (!isExists)
                {
                    string Name = dr["FirstName"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr["LastName"].ToString().Trim()))
                        Name += " " + dr["LastName"].ToString().Trim();

                    Name = Name.Replace("  ", " ");

                    Intuit.Ipp.Data.Customer Cust = new Intuit.Ipp.Data.Customer();
                    Cust.Title = "";
                    Cust.CompanyName = Name;
                    try
                    {
                        if (dr["BusinessID"].ToString() != "0")
                        {
                            string sql = "SELECT [BusinessName] FROM [msSchedulerV3].[dbo].[tbl_BusinessContact] where BusinessID = '" + dr["BusinessID"].ToString() + "'";
                            DataTable dtBusinessID;
                            db.Execute(sql, out dtBusinessID);

                            DataRow dr_dtBusinessID = dtBusinessID.Rows[0];
                            Cust.CompanyName = dr_dtBusinessID["BusinessName"].ToString().Trim();
                        }
                    }
                    catch { }

                    Cust.Title = dr["title"].ToString().Trim();
                    Cust.GivenName = Name;
                    Cust.MiddleName = dr["LastName"].ToString().Trim();
                    Cust.FamilyName = dr["FirstName"].ToString().Trim();
                    JobInfo jobInfo = new JobInfo();
                    //jobInfo.Description 
                    //Cust.JobInfo
                    Cust.DisplayName = Name;
                    Intuit.Ipp.Data.EmailAddress ObjEmail = new Intuit.Ipp.Data.EmailAddress();
                    ObjEmail.Address = dr["Email"].ToString();
                    Cust.PrimaryEmailAddr = ObjEmail;
                    Cust.Active = true;
                    PhysicalAddress ObjAddress = new PhysicalAddress();
                    ObjAddress.PostalCode = dr["ZipCode"].ToString();
                    ObjAddress.Country = "";
                    ObjAddress.Line1 = dr["Address1"].ToString();
                    ObjAddress.City = dr["City"].ToString();
                    Cust.BillAddr = ObjAddress;

                    // Cust.

                    TelephoneNumber ObjTelephoneNumber = new TelephoneNumber();
                    ObjTelephoneNumber.FreeFormNumber = dr["Mobile"].ToString();
                    Cust.PrimaryPhone = ObjTelephoneNumber;

                    DataService dataService = new DataService(context);
                    Intuit.Ipp.Data.Customer CustomerAdd = dataService.Add(Cust);

                    QboId = CustomerAdd.Id;
                    QboName = Name;

                    Sql = "update tbl_Customer set QboId=" + CustomerAdd.Id + " Where CompanyID='" + CompanyID + "' and CustomerID = '" + Id + "'";
                    db.Execute(Sql);
                }
                db.Close();
            }
            catch
            {
            }
        }

        public void TaxCodeCheck(QBOSettins qboSettings, string CompanyID, string Id, ref TaxCode RefTaxCode)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();
                string Sql = @"select * from Taxes A Where A.CompanyId = '" + CompanyID + "' And A.Id='" + Id + "'";

                DataTable dt;
                db.Execute(Sql, out dt);

                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(dr["QboId"].ToString()))
                {
                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    QueryService<Intuit.Ipp.Data.TaxCode> qsItem = new QueryService<Intuit.Ipp.Data.TaxCode>(context);
                    TaxCode stateTaxCode = qsItem.ExecuteIdsQuery("Select * From TaxCode Where id='" + dr["QboId"].ToString() + "' StartPosition 1 MaxResults 1").FirstOrDefault();
                    RefTaxCode = stateTaxCode;
                }
                db.Close();
            }
            catch
            {
            }
        }
        public Intuit.Ipp.Data.Account AccountCodeCheck(QBOSettins qboSettings, string CompanyID, string AccName)
        {
            try
            {
                ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                QueryService<Intuit.Ipp.Data.Account> qsItem = new QueryService<Intuit.Ipp.Data.Account>(context);
                Intuit.Ipp.Data.Account _Code = qsItem.ExecuteIdsQuery("Select * From Account Where Name='" + AccName + "' StartPosition 1 MaxResults 1").FirstOrDefault();
                return _Code;
            }
            catch
            {
                return null;
            }
        }
        public void TaxCodeCheck_Back(QBOSettins qboSettings, string CompanyID, string Id, ref string QboId, ref string QboName, ref TaxCode RefTaxCode)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();
                string Sql = @"select * from Taxes A Where A.CompanyId = '" + CompanyID + "' And A.Id='" + Id + "'";

                DataTable dt;
                db.Execute(Sql, out dt);

                DataRow dr = dt.Rows[0];


                if (!string.IsNullOrEmpty(dr["QboId"].ToString()))
                {
                    QboId = dr["QboId"].ToString();
                    QboName = dr["Name"].ToString();

                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    QueryService<Intuit.Ipp.Data.TaxCode> qsItem = new QueryService<Intuit.Ipp.Data.TaxCode>(context);
                    TaxCode stateTaxCode = qsItem.ExecuteIdsQuery("Select * From TaxCode Where Name='" + dr["Name"].ToString() + "' StartPosition 1 MaxResults 1").FirstOrDefault();
                    RefTaxCode = stateTaxCode;
                }
                else
                {
                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    string qboQuery = "select * from TaxRate where Name='" + dr["Name"].ToString() + "'";
                    QueryService<Intuit.Ipp.Data.TaxRate> qsItem = new QueryService<Intuit.Ipp.Data.TaxRate>(context);
                    List<Intuit.Ipp.Data.TaxRate> listCustomer = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.TaxRate>();
                    foreach (Intuit.Ipp.Data.TaxRate ilst in listCustomer)
                    {
                        // Insert New 
                        if (ilst.Name != dr["Name"].ToString())
                        {
                            Intuit.Ipp.Data.TaxRate Itm = new Intuit.Ipp.Data.TaxRate();
                            Itm.Name = dr["Name"].ToString();
                            Itm.Description = dr["Name"].ToString();
                            Itm.RateValue = decimal.Parse(dr["Rate"].ToString());
                            Itm.ActiveSpecified = true;
                            Itm.RateValueSpecified = true;
                            Itm.statusSpecified = true;
                            Itm.sparse = true;
                            Itm.domain = "domain Value";

                            DataService dataService = new DataService(context);
                            Intuit.Ipp.Data.TaxRate itemAdd = dataService.Add(Itm);

                            QboId = itemAdd.Id;
                            QboName = dr["Name"].ToString();
                        }
                        // Update QBo Id
                        else
                        {
                            Sql = "update Taxes set QboId=" + ilst.Id + " Where CompanyId='" + CompanyID + "' and Id = '" + Id + "'";
                            db.Execute(Sql);
                            QboId = ilst.Id;
                            QboName = ilst.Name;
                        }
                    }
                }
                db.Close();
            }
            catch
            {
            }
        }

        public void AccountTypeCheck(QBOSettins qboSettings, string CompanyID, string Id, ref string QboId)
        {
            try
            {
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrJobs"].ToString());
                //  db.Open();
                string Sql = @"select * from ItemTypes where Id='" + Id + "'";
                DataTable dt;
                db.Execute(Sql, out dt);

                DataRow dr = dt.Rows[0];
                if (!string.IsNullOrEmpty(dr["QboId"].ToString()))
                {
                    QboId = dr["QboId"].ToString();
                }
                else
                {
                    ServiceContext context = GetServiceContext(qboSettings, CompanyID);
                    string qboQuery = "select * from Account ";
                    QueryService<Intuit.Ipp.Data.Account> qsItem = new QueryService<Intuit.Ipp.Data.Account>(context);
                    List<Intuit.Ipp.Data.Account> listItem = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Account>();
                    foreach (Intuit.Ipp.Data.Account ilst in listItem)
                    {
                        if (ilst.Name == dr["Name"].ToString())
                        {
                            QboId = ilst.Id;
                        }
                    }

                    //Search Income Account if Not Found
                    if (string.IsNullOrEmpty(QboId))
                    {
                        foreach (Intuit.Ipp.Data.Account ilst in listItem)
                        {
                            if (ilst.Name == "Sales of Product Income")
                            {
                                QboId = ilst.Id;
                                break;
                            }
                        }
                        //Create Account if Not Found
                        if (string.IsNullOrEmpty(QboId))
                        {
                            Intuit.Ipp.Data.Account Itm = new Intuit.Ipp.Data.Account();
                            Itm.Name = "Sales of Product Income";
                            Itm.AccountTypeSpecified = true;
                            Itm.AccountType = AccountTypeEnum.Income;
                            Itm.statusSpecified = true;
                            DataService dataService = new DataService(context);
                            Intuit.Ipp.Data.Account itemAdd = dataService.Add(Itm);
                            QboId = itemAdd.Id;
                            Sql = "update ItemTypes set QboId=" + itemAdd.Id + " Where Id = '" + Id + "'";
                            db.Execute(Sql);
                        }
                    }
                }


                db.Close();
            }
            catch
            {
            }
        }

        // Item Synchronization
        public bool ItemSynchronization(QBOSettins qboSettings, string CompanyID, ServiceContext context, string QboLastUpdatedTime)
        {
            try
            {





                Int32 STARTPOSITION = 1;
                Int32 MAXRESULTS = 1000;



                for (int i = 1; i < 1000000; i++)
                {
                    string qboQuery = "Select * From Item Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    QueryService<Intuit.Ipp.Data.Item> qsItem = new QueryService<Intuit.Ipp.Data.Item>(context);
                    List<Intuit.Ipp.Data.Item> listItems = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Item>();

                    STARTPOSITION = i * MAXRESULTS;
                    if (listItems.Count > 0)
                    {
                        UpdateInsertLocalItemToQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrSch"].ToString(), listItems, context);

                        if (listItems.Count < MAXRESULTS)
                        {
                            if (Convert.ToBoolean(HttpContext.Current.Session["IsAireMaster"]) || Convert.ToBoolean(HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg"))
                            {
                                SyncItemToQBO(qboSettings, CompanyID, context);
                            }
                            i = 1000000;
                            return true;
                        }

                    }
                    else
                    {
                        //if (Convert.ToBoolean(HttpContext.Current.Session["IsAireMaster"]) || Convert.ToBoolean(HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg"))
                        //{
                        SyncItemToQBO(qboSettings, CompanyID, context);
                        //}
                        i = 1000000;
                        return true;
                    }

                }



                return true;

            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                //string errDetail = "";
                //var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                //if (innerException != null)
                //{
                //    errDetail = innerException.Detail;
                //}
                return false;
            }
        }
        private void SyncItemToQBO(QBOSettins qboSettings, string CompanyID, ServiceContext context)
        {


            try
            {

                QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income).FirstOrDefault();



                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

                string Sql = @"select Id,Name,Description,Barcode, IsTaxable,Quantity,Price,sku,Category from Items Where QboId='0'  and  [CompanyID] = @CompanyID";


                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);

                string InsertSql = string.Empty;
                if (dataSet.Tables[0] != null)
                {
                    foreach (DataRow dr in dataSet.Tables[0].Rows)
                    {
                        try
                        {
                            Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                            Itm.Name = Common.CleanInput(dr["Name"].ToString().Trim());
                            Itm.Description = Common.CleanInput(dr["Description"].ToString().Trim());
                            Itm.Taxable = Convert.ToBoolean(dr["IsTaxable"].ToString().Trim());
                            Itm.UnitPrice = decimal.Parse(dr["Price"].ToString().Trim());
                            Itm.TypeSpecified = true;
                            Itm.Type = ItemTypeEnum.Service;
                            //Itm.Type = ItemTypeEnum.Inventory;


                            Itm.TrackQtyOnHand = false;
                            Itm.TrackQtyOnHandSpecified = false;
                            Itm.QtyOnHandSpecified = false;
                            Itm.QtyOnHand = 0;
                            Itm.InvStartDateSpecified = true;
                            Itm.InvStartDate = DateTime.Now;
                            Itm.UnitPriceSpecified = true;
                            Itm.PurchaseDesc = "";
                            Itm.PurchaseCostSpecified = true;
                            Itm.PurchaseCost = 0;
                            Itm.Sku = dr["sku"].ToString().Trim();
                            Itm.QtyOnHand = Convert.ToDecimal(dr["Quantity"]);
                            //Itm.ItemCategoryType = dr["Category"].ToString().Trim();
                            //Itm.TrackQtyOnHand = true;
                            //Itm.TrackQtyOnHandSpecified = true;
                            //Itm.QtyOnHandSpecified = true;
                            //Itm.QtyOnHand = 0;
                            //Itm.InvStartDateSpecified = true;
                            //Itm.InvStartDate = DateTime.Now;
                            //Itm.UnitPriceSpecified = true;
                            //Itm.PurchaseDesc = "";
                            //Itm.PurchaseCostSpecified = true;
                            //Itm.PurchaseCost = 0;


                            //string AccTypeId = "";
                            //QBOManager.AccountTypeCheck(qBoStng, CompanyID, ItemTypeId.SelectedValue, ref AccTypeId);
                            //Itm.IncomeAccountRef = new ReferenceType();
                            //Itm.IncomeAccountRef.Value = AccTypeId;

                            if (AssetAccountRef != null)
                            {
                                Itm.IncomeAccountRef = new ReferenceType();
                                Itm.IncomeAccountRef.Value = AssetAccountRef.Id;
                            }

                            //Itm.IncomeAccountRef = new ReferenceType();
                            //Itm.IncomeAccountRef.Value = "1";

                            //Itm.IncomeAccountRef = new ReferenceType();
                            //Itm.IncomeAccountRef.Value = "79";

                            //Itm.ExpenseAccountRef = new ReferenceType();
                            //Itm.ExpenseAccountRef.Value = "80";

                            //Itm.AssetAccountRef = new ReferenceType();
                            //Itm.AssetAccountRef.Value = "81";

                            //QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(serviceContext);
                            //var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                            //var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.OtherCurrentAsset && x.Name == "Inventory Asset").FirstOrDefault();
                            //if (AssetAccountRef != null)
                            //{
                            //    Itm.AssetAccountRef = new ReferenceType();
                            //    Itm.AssetAccountRef.Value = AssetAccountRef.Id;
                            //}

                            //var IncomeAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income && x.Name == "Sales of Product Income").FirstOrDefault();
                            //if (IncomeAccountRef != null)
                            //{
                            //    Itm.IncomeAccountRef = new ReferenceType();
                            //    Itm.IncomeAccountRef.Value = IncomeAccountRef.Id;
                            //}

                            //var ExpenseAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.CostofGoodsSold && x.Name == "Cost of Goods Sold").FirstOrDefault();
                            //if (ExpenseAccountRef != null)
                            //{
                            //    Itm.ExpenseAccountRef = new ReferenceType();
                            //    Itm.ExpenseAccountRef.Value = ExpenseAccountRef.Id;
                            //}

                            DataService dataService = new DataService(context);
                            Intuit.Ipp.Data.Item Item = dataService.Add(Itm);

                            InsertSql += "update [msSchedulerV3].[dbo].[Items] set QboId=" + Item.Id + " Where CompanyID='" + CompanyID + "' and [Id] = '" + dr["Id"].ToString() + "';";






                        }
                        catch (Exception ex)
                        {
                            InsertSql += "update [msSchedulerV3].[dbo].[Items] set QboId=-1 Where CompanyID='" + CompanyID + "' and [Id] = '" + dr["Id"].ToString() + "';";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(InsertSql))
                {
                    db.Execute(InsertSql);
                }
            }
            catch (Exception ex)
            {

            }

        }
        private void UpdateInsertLocalItemToQBO(string CompanyID, string ConString, List<Intuit.Ipp.Data.Item> listItems, ServiceContext context)
        {
            try
            {
                Database db = new Database(ConString);
                //  db.Open();
                string Sql = "select Id,isnull(QboId,0) QboId from Items where CompanyID='" + CompanyID + "' and IsDeleted=0  ";
                DataTable dt;
                db.Execute(Sql, out dt);

                string InsertSql = string.Empty;
                bool isExist = false;
                foreach (Intuit.Ipp.Data.Item ilst in listItems)
                {
                    try
                    {
                        if (ilst.Active && ilst.Type != Intuit.Ipp.Data.ItemTypeEnum.Category)
                        {
                            isExist = false;
                            foreach (DataRow dr in dt.Rows)
                            {
                                if (dr["QboId"].ToString().Trim().ToUpper() == ilst.Id.Trim().ToUpper())
                                {
                                    try
                                    {

                                        if (Convert.ToUInt32(dr["QboId"].ToString()) == 0)
                                        {
                                            Sql = "update Items set QboId=" + ilst.Id + " Where CompanyID='" + CompanyID + "' and Id = '" + dr["Id"].ToString() + "'";
                                            db.Execute(Sql);
                                        }
                                        if (dr["QboId"].ToString() != ilst.Id.ToString())
                                        {
                                            Sql = "update Items set QboId=" + ilst.Id + " Where CompanyID='" + CompanyID + "' and Id = '" + dr["Id"].ToString() + "'";
                                            db.Execute(Sql);
                                        }
                                    }
                                    catch { }

                                    // Refresh the quantity-on-hand snapshot on every sync, not only on first insert,
                                    // so the invoice "Qty on Hand" column reflects current QBO stock.
                                    try
                                    {
                                        if (ilst.QtyOnHandSpecified)
                                        {
                                            Sql = "update Items set Quantity=" + ilst.QtyOnHand.ToString(CultureInfo.InvariantCulture)
                                                + " Where CompanyID='" + CompanyID + "' and Id = '" + dr["Id"].ToString() + "'";
                                            db.Execute(Sql);
                                        }
                                    }
                                    catch { }

                                    isExist = true;
                                }
                            }

                            if (!isExist)
                            {
                                string Taxable = ilst.Taxable == true ? "TRUE" : "FALSE";

                                string name = string.Empty;
                                if (ilst.Name != null)
                                {
                                    name = ilst.Name.Trim().Replace("'", "''");
                                }

                                string Description = string.Empty;
                                if (ilst.Description != null)
                                {
                                    Description = ilst.Description.Trim().Replace("'", "''");
                                }

                                InsertSql += "INSERT INTO  Items " +
                                               " (Name, Description, Barcode, ItemTypeId, Price, IsTaxable,CompanyId,QboId,IsDeleted,CreatedDate,Sku,Category,Quantity )" +
                                               " VALUES  ('" + name + "','" + Description + "','',5,'" + Convert.ToDouble(ilst.UnitPrice) + "','" + Taxable + "','" +
                                               CompanyID + "'," + ilst.Id + ",0,GETDATE(),'" + ilst.Sku + "','" + ilst.Type + "'," + ilst.QtyOnHand + ");";

                                //InsertSql += @" IF EXISTS (SELECT id  FROM [msSchedulerV3].[dbo].items WHERE name ='" + name + "' and companyid='" + CompanyID + "')" +
                                //                @"BEGIN
                                //                    UPDATE [msSchedulerV3].[dbo].items 
                                //                    SET QboId = '" + ilst.Id + "' WHERE name ='" + name + "' and companyid='" + CompanyID + @"'" +
                                //                " END; ";
                                //db.Execute(Sql);
                            }
                        }

                        // db.Close();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                }

                if (!string.IsNullOrEmpty(InsertSql))
                {
                    db.Execute(InsertSql);
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string InsertItemToQbo(DataRow dr, ServiceContext context)
        {
            try
            {
                bool isTaxable = dr["IsTaxable"].ToString() == "YES" ? true : false;
                Intuit.Ipp.Data.Item Itm = new Intuit.Ipp.Data.Item();
                Itm.Name = dr["Name"].ToString().Trim();
                Itm.Description = dr["Description"].ToString();
                Itm.Taxable = isTaxable;
                Itm.UnitPrice = decimal.Parse(dr["Price"].ToString());

                Itm.TypeSpecified = true;
                Itm.Type = ItemTypeEnum.Service;

                Itm.TrackQtyOnHand = false;
                Itm.TrackQtyOnHandSpecified = false;
                Itm.QtyOnHandSpecified = false;
                Itm.QtyOnHand = 0;
                Itm.InvStartDateSpecified = true;
                Itm.InvStartDate = DateTime.Now;
                Itm.UnitPriceSpecified = true;
                Itm.PurchaseDesc = "";
                Itm.PurchaseCostSpecified = true;
                Itm.PurchaseCost = 0;
                QueryService<Intuit.Ipp.Data.Account> querySvc = new QueryService<Intuit.Ipp.Data.Account>(context);
                var AccountList = querySvc.ExecuteIdsQuery("SELECT * FROM Account").ToList();
                var AssetAccountRef = AccountList.Where(x => x.AccountType == AccountTypeEnum.Income).FirstOrDefault();
                if (AssetAccountRef != null)
                {
                    Itm.IncomeAccountRef = new ReferenceType();
                    Itm.IncomeAccountRef.Value = AssetAccountRef.Id;
                }

                DataService dataService = new DataService(context);
                Intuit.Ipp.Data.Item itemAdd = dataService.Add(Itm);

                return itemAdd.Id;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;

                }
                return "0";
            }
        }


        // Customer Synchronization
        public bool CustomerSyncFromQBO(QBOSettins qboSettings,
            string CompanyID,
            ServiceContext context,
            string QboLastUpdatedTime,
            string QboCreatadDateTime)
        {
            try
            {

                Int32 STARTPOSITION = 1;
                Int32 MAXRESULTS = 700;

                Int32 _totalCustomer = 0;

                string strTemp = "";
                StringBuilder _sb = new StringBuilder();
                for (int i = 1; i < 1000000; i++)
                {
                    string qboQuery = string.Empty;

                    Int32 count = 0;
                    if (!string.IsNullOrEmpty(QboCreatadDateTime))
                    {
                        qboQuery = "Select * From Customer Where Active = True and  MetaData.CreateTime >= '" + QboCreatadDateTime + "' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    }
                    else
                    {
                        qboQuery = "Select * From Customer Where id > '36821' and  Active = True and MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    }

                    //qboQuery = "Select * From Customer where Id = '1144'  ";

                    //qboQuery = "Select * From Customer  STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();


                    // qboQuery = "Select * From Customer WHERE FullyQualifiedName LIKE '%POLITO%'  ";

                    // Output the total count




                    QueryService<Intuit.Ipp.Data.Customer> qsItem = new QueryService<Intuit.Ipp.Data.Customer>(context);
                    List<Intuit.Ipp.Data.Customer> listCustomer = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Customer>();

                    //List<Intuit.Ipp.Data.Customer> listCustomer2 =  listCustomer.Where(x => x.DisplayName.Contains("Fernando")).ToList();


                    //int totalCustomerCount = qsItem.ExecuteIdsQuery("SELECT COUNT(*) FROM Customer").Count();

                    string sb = "";
                    STARTPOSITION = i * MAXRESULTS;
                    if (listCustomer.Count > 0)
                    {
                        UpdateInsertCustomerFromQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrSch"].ToString(), listCustomer, context, out sb);
                        _sb.Append(sb);
                        count += listCustomer.Count;

                        //foreach (Intuit.Ipp.Data.Customer _QboCustomer in listCustomer)
                        //{
                        //    strTemp += " update tbl_Customer set Companyid='" + CompanyID + "-Keep" + "'" +
                        //                                    " where [QboId] ='" + _QboCustomer.Id + "' and CompanyID='" + CompanyID + "';" + Environment.NewLine;
                        //}



                        _totalCustomer += listCustomer.Count;
                        if (listCustomer.Count < MAXRESULTS)
                        {
                            string sds = _sb.ToString();
                            i = 1000000;
                            return true;
                        }
                    }
                    else
                    {
                        string sds = _sb.ToString();
                        i = 1000000;
                        return true;
                    }
                    //foreach (Intuit.Ipp.Data.Customer _QboCustomer in listCustomer)
                    //{
                    //    try
                    //    {
                    //        if (_QboCustomer.DisplayName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;

                    //        }
                    //    }
                    //    catch { }

                    //    try
                    //    {
                    //        if (_QboCustomer.FullyQualifiedName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }


                    //    try
                    //    {
                    //        if (_QboCustomer.GivenName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }
                    //    try
                    //    {
                    //        if (_QboCustomer.MiddleName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }


                    //    try
                    //    {
                    //        if (_QboCustomer.CompanyName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }

                    //    try
                    //    {
                    //        if (_QboCustomer.ContactName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }

                    //    try
                    //    {
                    //        if (_QboCustomer.AltContactName.Contains("Gabrielle"))
                    //        {
                    //            string sad = _QboCustomer.DisplayName;
                    //        }
                    //    }
                    //    catch { }

                    //}

                }



                return true;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);

                }
                return false;
            }
        }
        public bool SentEstimateToQBO(out string log, QBOSettins qboSettings, string CompanyID, ServiceContext context)
        {
            string error = string.Empty;
            log = error;
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
            //  db.Open();
            try
            {



                string Sql = @"SELECT [ID]
                          ,[Number]
                          ,[CompanyId]
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
                          ,[CreatedDate]
                          ,[CreatedBy]
                          ,[InvoiceDate]
                          ,[AmountCollect]
                          ,[TaxType]
                          ,[AppointmentId]
                          ,[Type]
                          ,[QboId]
                          ,[DiscountRate]
                          ,[DiscountOption]
                          ,[QboEstimateId]
                          ,[ExpirationDate]
                          ,QboPaymentID
                      FROM[msSchedulerV3].[dbo].[tbl_Invoice]  Where QboEstimateId = '0' and AutoSync = 1 and (Type='Proposal' or Type='Estimate') and CompnyID ='" + CompanyID + "'";
                DataTable dt = new DataTable();
                db.Execute(Sql, out dt);
                DataTable dt_InvoiceDetails = new DataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    string Type = dr["Type"].ToString().Trim();
                    string ID = dr["ID"].ToString().Trim();
                    string Number = dr["Number"].ToString().Trim();
                    string CId = dr["CustomerId"].ToString().Trim(); ;
                    string trDate = Convert.ToDateTime(dr["InvoiceDate"]).ToString("yyyy-MM-dd");
                    DateTime ExpirationDate = DateTime.Now;
                    if (!string.IsNullOrEmpty(dr["ExpirationDate"].ToString()))
                    {
                        ExpirationDate = Convert.ToDateTime(dr["ExpirationDate"]);
                    }


                    string Notes = dr["Note"].ToString().Trim();
                    string disId = dr["DiscountOption"].ToString().Trim();
                    string DRate = dr["DiscountRate"].ToString().Trim();
                    string Discount = dr["Discount"].ToString().Trim();
                    string TaxRefId = dr["TaxType"].ToString().Trim();
                    string TotalTax = dr["Tax"].ToString().Trim();
                    string TotalAmount = dr["Total"].ToString().Trim();
                    string InvoiceNumberDoc = dr["Number"].ToString().Trim();
                    string InvoiceId = dr["QboId"].ToString().Trim();
                    string QboId = dr["QboId"].ToString().Trim();

                    string Status = dr["Status"].ToString().Trim();
                    string QboEstimateId = dr["QboEstimateId"].ToString().Trim();


                    Sql = @"SELECT [ID]
                          ,[RefId]
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
                      FROM [msSchedulerV3].[dbo].[tbl_InvoiceDetails] where RefId = '" + ID + "'" +
                      " and companyid = '" + CompanyID + "' order by CAST(NULLIF(LineNum,'') AS INT) asc";

                    string pid = "";
                    string pName = "";
                    string pDesc = "";
                    string sDate = "";
                    string pQty = "";
                    string pRate = "";
                    string pTax = "";



                    if ((Type == "Proposal" || Type == "Estimate") && QboEstimateId == "0")
                    {

                        db.Execute(Sql, out dt_InvoiceDetails);
                        foreach (DataRow dr_InvDtls in dt_InvoiceDetails.Rows)
                        {
                            pid += dr_InvDtls["ItemId"].ToString() + "@";
                            pName += dr_InvDtls["ItemName"].ToString() + "@";
                            pDesc += dr_InvDtls["Description"].ToString() + "@";
                            sDate += dr_InvDtls["ServiceDate"].ToString() + "@";
                            pQty += dr_InvDtls["Quantity"].ToString() + "@";
                            pRate += dr_InvDtls["uPrice"].ToString() + "@";
                            pTax += dr_InvDtls["IsTaxable"].ToString().Trim().Replace("No", "NON") + "@";

                        }
                        string SyncToken = "";
                        try
                        {
                            string QboInvoiceId = CreateEstimateQbo(context, ref SyncToken, qboSettings, ExpirationDate, pid, pName,
                                pDesc, sDate, pQty, pRate, pTax, CId, "", trDate, Notes, disId, DRate, Discount, TaxRefId, "", TotalTax, TotalAmount, InvoiceNumberDoc, InvoiceId, "", "", ID);

                            if (QboInvoiceId != "0")
                            {
                                Sql = "update tbl_Invoice set QboEstimateId='" + QboInvoiceId + "',SyncToken='" + SyncToken + "' " +
                                   " where id='" + ID + "'" +
                                    " and CompnyID='" + CompanyID + "' ";

                                db.Execute(Sql);

                            }
                        }
                        catch (Exception ex)
                        {

                        }


                    }
                }
                db.Close();
                log += "dsa";
                return true;
            }
            catch (Exception ex)
            {
                log += ex.Message + ex.InnerException + ex.Source;
                db.Close();

                return false;
            }
        }
        public bool SentInvoiceToQBO(out string log,
            QBOSettins qboSettings,
            string CompanyID,
            ServiceContext context)
        {
            string error = string.Empty;
            log = error;
            Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
            //  db.Open();
            try
            {



                string Sql = @"SELECT [ID]
                          ,[Number]
                          ,[CompanyId]
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
                          ,[CreatedDate]
                          ,[CreatedBy]
                          ,[InvoiceDate]
                          ,[AmountCollect]
                          ,[TaxType]
                          ,[AppointmentId]
                          ,[Type]
                          ,[QboId]
                          ,[DiscountRate]
                          ,[DiscountOption]
                          ,[QboEstimateId]
                          ,[ExpirationDate],isnull(PONO,'') PONO
                          ,QboPaymentID
                          ,ISNULL(QboClassId, 0) as QboClassId
                          ,ISNULL(QboLocationId, 0) as QboLocationId 
                      FROM [msSchedulerV3].[dbo].[tbl_Invoice] Where QboId='0' AND Type='Invoice' and AutoSync = 1 and CompnyID='" + CompanyID + "';";
                DataTable dt = new DataTable();
                db.Execute(Sql, out dt);
                DataTable dt_InvoiceDetails = new DataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    string Type = dr["Type"].ToString().Trim();
                    string Number = dr["Number"].ToString().Trim();
                    string ID = dr["ID"].ToString().Trim();
                    string CId = dr["CustomerId"].ToString().Trim(); ;
                    string trDate = Convert.ToDateTime(dr["InvoiceDate"]).ToString("yyyy-MM-dd");
                    DateTime ExpirationDate = DateTime.Now;
                    if (!string.IsNullOrEmpty(dr["ExpirationDate"].ToString()))
                    {
                        ExpirationDate = Convert.ToDateTime(dr["ExpirationDate"]);
                    }

                    string QboClassId = dr["QboClassId"].ToString().Trim();
                    string QboLocationId = dr["QboLocationId"].ToString().Trim();

                    string Notes = dr["Note"].ToString().Trim();
                    if (!string.IsNullOrEmpty(dr["PONO"].ToString().Trim()))
                    {
                        Notes = Notes + Environment.NewLine + "PO: " + dr["PONO"].ToString().Trim();
                    }
                    string disId = dr["DiscountOption"].ToString().Trim();
                    string DRate = dr["DiscountRate"].ToString().Trim();
                    string Discount = dr["Discount"].ToString().Trim();
                    string TaxRefId = dr["TaxType"].ToString().Trim();
                    string TotalTax = dr["Tax"].ToString().Trim();
                    string TotalAmount = dr["Total"].ToString().Trim();
                    string InvoiceNumberDoc = dr["Number"].ToString().Trim();

                    string QboId = dr["QboId"].ToString().Trim();
                    string QboEstimateId = dr["QboEstimateId"].ToString().Trim();
                    string Status = dr["Status"].ToString().Trim();

                    if ((Convert.ToDouble(dr["Total"].ToString()) - Convert.ToDouble(dr["AmountCollect"].ToString())) <= 0)
                    {
                        Status = "1";

                    }
                    else
                    {
                        Status = "0";
                    }

                    Sql = @"SELECT [ID]
                          ,[RefId]
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
                      FROM [msSchedulerV3].[dbo].[tbl_InvoiceDetails] where RefId = '" + ID +
                      "' and companyid = '" + CompanyID + "' order by CAST(NULLIF(LineNum,'') AS INT) asc";


                    string pid = "";
                    string pName = "";
                    string pDesc = "";
                    string sDate = "";
                    string pQty = "";
                    string pRate = "";
                    string pTax = "";





                    if (Type == "Invoice" && QboId == "0")
                    {
                        db.Execute(Sql, out dt_InvoiceDetails);

                        foreach (DataRow dr_InvDtls in dt_InvoiceDetails.Rows)
                        {
                            pid += dr_InvDtls["ItemId"].ToString() + "@";
                            pName += dr_InvDtls["ItemName"].ToString() + "@";
                            pDesc += dr_InvDtls["Description"].ToString() + "@";
                            sDate += dr_InvDtls["ServiceDate"].ToString() + "@";
                            pQty += dr_InvDtls["Quantity"].ToString() + "@";
                            pRate += dr_InvDtls["uPrice"].ToString() + "@";
                            pTax += dr_InvDtls["IsTaxable"].ToString().Trim().Replace("No", "NON") + "@";

                        }
                        string SyncToken = "";
                        string QboPaymentID = dr["QboPaymentID"].ToString().Trim();

                        try
                        {
                            string QboInvoiceId = CreateInvoiceQbo(context, ref QboPaymentID, ref SyncToken, Status, QboEstimateId,
                                qboSettings, pid, pName, pDesc, sDate, pQty, pRate, pTax, CId, trDate, Notes,
                                disId, DRate, Discount, TaxRefId, TotalTax, TotalAmount, InvoiceNumberDoc, QboId, QboClassId, QboLocationId, ID);

                            if (QboInvoiceId != "0")
                            {
                                Sql = "update tbl_Invoice set QboId='" + QboInvoiceId + "',SyncToken='" + SyncToken + "',QboPaymentID='" + QboPaymentID + "'" +
                                    " where id='" + ID + "'" +
                                    " and CompnyID='" + CompanyID + "' ";

                                db.Execute(Sql);

                            }
                        }
                        catch (Exception ex)
                        {
                            Sql = "update tbl_Invoice set QboId='-11',SyncToken='" + SyncToken + "',QboPaymentID='" + QboPaymentID + "'" +
                                   " where id='" + ID + "'" +
                                   " and CompnyID='" + CompanyID + "' ";

                            db.Execute(Sql);
                        }


                    }

                }
                db.Close();
                log += "dsa";
                return true;
            }
            catch (Exception ex)
            {
                log += ex.Message + ex.InnerException + ex.Source;
                db.Close();

                return false;
            }
        }

        private void InsertUpdateBusinessContact(string CompanyID, string ConString,
            List<Intuit.Ipp.Data.Customer> listItems)
        {
            Database db = new Database(ConString);
            SqlConnection Conn = new SqlConnection(ConString);

            string Sql = "SELECT [CompanyID],[BusinessID],[QboId],SyncToken FROM [msSchedulerV3].[dbo].[tbl_Customer] Where [CompanyID] = @CompanyID and  [IsBusinessContact] = 1";
            DataTable dt_Business = new DataTable();

            DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
            dt_Business = dataSet.Tables[0];


            bool isExist = false;
            bool IsBusinessContact = false;
            Sql = string.Empty;
            string strTemp = string.Empty;
            foreach (Intuit.Ipp.Data.Customer _QboCustomer in listItems)
            {
                try
                {

                    isExist = false;
                    IsBusinessContact = false;
                    string SyncToken = "";
                    if (_QboCustomer.Active)
                    {
                        Int32 ChildCount = 0;

                        string CompanyName = String.Empty;

                        if (_QboCustomer.CompanyName != null)
                        {
                            if (_QboCustomer.ParentRef == null)
                            {
                                CompanyName = _QboCustomer.CompanyName;
                                IsBusinessContact = true;
                            }

                        }

                        if (_QboCustomer.ParentRef == null)
                        {
                            for (int i = 0; i < listItems.Count; i++)
                            {
                                if (listItems[i].ParentRef != null)
                                {
                                    if (listItems[i].ParentRef.Value == _QboCustomer.Id)
                                    {
                                        ChildCount++;

                                    }
                                }
                            }
                        }
                        if (ChildCount > 0)
                        {
                            IsBusinessContact = true;
                        }

                        foreach (DataRow dr in dt_Business.Rows)
                        {

                            if (_QboCustomer.Id == dr["QboId"].ToString().Trim())
                            {
                                isExist = true;
                                SyncToken = dr["SyncToken"].ToString().Trim();


                            }
                        }



                        string Address = "", Phone = "", email = "", city = "", zpCode = "", state = "";

                        if (_QboCustomer.BillAddr != null)
                        {
                            if (_QboCustomer.BillAddr.Line1 != null)
                            {
                                Address = _QboCustomer.BillAddr.Line1;
                            }
                            if (_QboCustomer.BillAddr.City != null)
                            {
                                city = _QboCustomer.BillAddr.City;
                            }
                            if (_QboCustomer.BillAddr.PostalCode != null)
                            {
                                zpCode = _QboCustomer.BillAddr.PostalCode;
                            }
                            if (_QboCustomer.BillAddr.CountrySubDivisionCode != null)
                            {
                                state = _QboCustomer.BillAddr.CountrySubDivisionCode;
                            }

                        }

                        if (string.IsNullOrEmpty(Address))
                        {

                            if (_QboCustomer.Title != null)
                            {
                                Address = _QboCustomer.Title + " ";
                            }
                            if (_QboCustomer.DisplayName != null)
                            {
                                Address += _QboCustomer.DisplayName + " ";
                            }
                            if (_QboCustomer.CompanyName != null)
                            {
                                Address += _QboCustomer.CompanyName;
                            }

                        }
                        if (_QboCustomer.PrimaryPhone != null)
                        {
                            if (_QboCustomer.PrimaryPhone.FreeFormNumber != null)
                            {
                                Phone = _QboCustomer.PrimaryPhone.FreeFormNumber;
                            }
                        }
                        string mobile = string.Empty;
                        if (_QboCustomer.Mobile != null)
                        {
                            if (_QboCustomer.Mobile.FreeFormNumber != null)
                            {
                                mobile = _QboCustomer.Mobile.FreeFormNumber;
                            }
                        }

                        if (_QboCustomer.PrimaryEmailAddr != null)
                        {
                            if (_QboCustomer.PrimaryEmailAddr.Address != null)
                            {
                                email = _QboCustomer.PrimaryEmailAddr.Address;
                            }
                        }

                        string[] _CName = _QboCustomer.DisplayName.Split(' ');
                        string LastName = "";
                        if (_CName.Length > 1)
                        {
                            for (int i = 1; i < _CName.Length; i++)
                            {
                                if (LastName != "")
                                    LastName += " ";
                                LastName += _CName[i];
                            }
                        }

                        string Title = string.Empty;
                        if (_QboCustomer.Title != null)
                        {

                            Title = _QboCustomer.Title;

                        }

                        string customer_ID = string.Empty;
                        if (!isExist && IsBusinessContact)
                        {

                            BusinessContacts businessContact = new BusinessContacts();

                            string guid = Guid.NewGuid().ToString().ToUpper().Trim();
                            customer_ID = guid;
                            businessContact.CompanyID = CompanyID;
                            businessContact.BusinessName = CleanInput(CompanyName);
                            businessContact.Title = CleanInput(Title.Replace("'", string.Empty));
                            businessContact.FirstName = CleanInput(_CName[0].Trim());
                            businessContact.LastName = CleanInput(LastName);
                            businessContact.Address1 = CleanInput(Address);
                            businessContact.Address2 = "";
                            businessContact.City = CleanInput(city);
                            businessContact.State = CleanInput(state);
                            businessContact.ZipCode = CleanInput(zpCode);
                            businessContact.Phone = CleanInput(Phone);
                            businessContact.Mobile = CleanInput(mobile);
                            businessContact.Email = CleanInput(email);
                            businessContact.CustomerGuid = CleanInput(guid);
                            businessContact.QboId = CleanInput(_QboCustomer.Id);



                            BusinessContactProcessor businessContactProcessor = new BusinessContactProcessor();
                            businessContactProcessor.AddBusinessContact(businessContact);

                            //if (businessContactProcessor.AddBusinessContact(businessContact))
                            //{

                            //    BusinessContacts businessContactPrimary = new BusinessContacts();
                            //    businessContactPrimary.IsPrimaryContact = true;
                            //    businessContactPrimary.CompanyID = CompanyID;
                            //    businessContactPrimary.FirstName = _CName;
                            //    businessContactPrimary.LastName = "";
                            //    businessContactPrimary.Address1 = Address;

                            //    businessContactPrimary.Title = "";
                            //    businessContactPrimary.JobTitle = "";
                            //    businessContactPrimary.City = city;
                            //    businessContactPrimary.State = state;
                            //    businessContactPrimary.ZipCode = zpCode;
                            //    businessContactPrimary.Phone = Phone;
                            //    businessContactPrimary.Mobile = mobile;
                            //    businessContactPrimary.Email = email;
                            //    businessContactPrimary.CustomerGuid = guid;
                            //    businessContactPrimary.QboId = _QboCustomer.Id;

                            //    businessContactProcessor.AddBusinessPrimaryContact(businessContactPrimary);
                            //}

                        }
                        else
                        {
                            if (IsBusinessContact)
                            {
                                if (SyncToken != _QboCustomer.SyncToken.Trim() && SyncToken != "")
                                {
                                    //strTemp += " update [tbl_BusinessContact] set FirstName='" + _CName[0].Trim().Replace("'", string.Empty) + "'";
                                    //strTemp += " , LastName='" + LastName.Trim().Replace("'", string.Empty) + "', title='" + Title.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "',Address1='" + Address.Replace("'", string.Empty) + "'";
                                    //strTemp += " , City='" + city.Replace("'", string.Empty) + "', ZipCode='" + zpCode.Replace("'", string.Empty) + "',Phone='" + Phone.Replace("'", string.Empty) + "'";
                                    //strTemp += " , Mobile='" + mobile.Replace("'", string.Empty) + "' ";
                                    //strTemp += " , State='" + state.Replace("'", string.Empty) + "' ";
                                    //strTemp += " , Email='" + email.Replace("'", string.Empty) + "' ";
                                    //strTemp += " where [QboId] ='" + _QboCustomer.Id + "' and CompanyID='" + CompanyID + "';";

                                }
                            }


                        }
                        if (IsBusinessContact && _QboCustomer.BillAddr != null)
                        {
                            if (HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg")
                            {
                                //billing address sync by Mohsin

                                db.BeginTransaction();
                                string billAdd_Guid = Guid.NewGuid().ToString();

                                string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_Customer] where QboId='" + _QboCustomer.Id + "' and CompanyID='" + CompanyID + "'";

                                DataTable dt = new DataTable();
                                db.Execute(sqlAppDtl, out dt);

                                string custGuid = "";

                                if (dt.Rows.Count > 0)
                                {
                                    custGuid = dt.Rows[0]["customerGuid"].ToString();
                                }
                                DataTable dtBillAdd = new DataTable();
                                if (!string.IsNullOrEmpty(custGuid))
                                {
                                    string sqlExtBilling = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerBillingAddress] where Customer_ID='" + custGuid + "'";
                                    db.Execute(sqlExtBilling, out dtBillAdd);
                                }

                                string billingAddressGuid = "";

                                Hashtable ht = new Hashtable();
                                if (dtBillAdd.Rows.Count > 0)
                                {
                                    billingAddressGuid = dtBillAdd.Rows[0]["ID"].ToString();
                                    db.BeginTransaction();

                                    ht.Add("ID", billAdd_Guid);
                                    ht.Add("Address", _QboCustomer.BillAddr.Line1);
                                    ht.Add("City", _QboCustomer.BillAddr.City);
                                    ht.Add("ZipCode", _QboCustomer.BillAddr.PostalCode);
                                    ht.Add("State", _QboCustomer.BillAddr.CountrySubDivisionCode);
                                    ht.Add("Customer_ID", custGuid);

                                    Conn.Open();
                                    db.UpdateByCommand(ht, "tbl_CustomerBillingAddress", "ID='" + billingAddressGuid + "'", Conn);
                                    Conn.Close();
                                    db.CommitTransaction();
                                }
                                else
                                {
                                    ht.Add("ID", billAdd_Guid);
                                    ht.Add("Address", _QboCustomer.BillAddr.Line1);
                                    ht.Add("City", _QboCustomer.BillAddr.City);
                                    ht.Add("ZipCode", _QboCustomer.BillAddr.PostalCode);
                                    ht.Add("State", _QboCustomer.BillAddr.CountrySubDivisionCode);
                                    ht.Add("Customer_ID", custGuid == "" ? customer_ID : custGuid);

                                    db.InsertByCommand(ht, "tbl_CustomerBillingAddress", Conn);
                                    db.CommitTransaction();
                                }

                                // end billing address
                            }
                        }
                    }
                }
                catch (Exception ex) { }
            }

            try
            {
                //if (!string.IsNullOrEmpty(Sql))
                //{
                //    db.Execute(Sql);
                //}
                //if (!string.IsNullOrEmpty(strTemp))
                //{
                //    db.Execute(strTemp);
                //}

            }
            catch (Exception ex) { }
        }
        private void UpdateInsertCustomerFromQBO(string CompanyID, string ConString,
            List<Intuit.Ipp.Data.Customer> listItems,
            ServiceContext context,
            out String sb)
        {

            sb = "";
            try
            {
                Database db = new Database(ConString);
                SqlConnection Conn = new SqlConnection(ConString);

                List<string> _QboCustomerID = new List<string>();
                foreach (Intuit.Ipp.Data.Customer _QboCustomer in listItems)
                {
                    _QboCustomerID.Add("'" + _QboCustomer.Id + "'");
                }

                string Sql = "select QboId,SyncToken from [msSchedulerV3].[dbo].[tbl_Customer]  Where CompanyID=@CompanyID " +
                        " and QboId in (" + string.Join(",", _QboCustomerID) + ");";

                DataTable dt_Customer = new DataTable();
                DataTable dt_Business = new DataTable();

                bool IsPro = false;
                //if (HttpContext.Current.Session["IsCecPro"] != null)
                //{
                //    IsPro = (bool)HttpContext.Current.Session["IsCecPro"];
                //    if (IsPro)
                //    {
                //        if (CompanyID != "13116")
                //        {
                //            //InsertUpdateBusinessContact(CompanyID, ConString, listItems);

                //            Sql += @"SELECT [CompanyID],[CustomerID],[QboId],[BusinessName] FROM [msSchedulerV3].[dbo].[tbl_Customer] Where [CompanyID] = @CompanyID and  [IsBusinessContact] = 1";

                //        }
                //    }
                //}
                //DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                //dt_Customer = dataSet.Tables[0];
                //if (IsPro)
                //{
                //    if (CompanyID != "13116")
                //    {
                //        dt_Business = dataSet.Tables[1];
                //    }
                //}

                bool isExist = false;
                Sql = string.Empty;
                string strTemp = string.Empty;
                foreach (Intuit.Ipp.Data.Customer _QboCustomer in listItems)
                {
                    try
                    {

                        isExist = false;

                        string SyncToken = "";
                        string QboSyncToken = "";
                        if (_QboCustomer.Active)
                        {

                            foreach (DataRow dr in dt_Customer.Rows)
                            {

                                if (_QboCustomer.Id == dr["QboId"].ToString().Trim())
                                {
                                    isExist = true;
                                    SyncToken = dr["SyncToken"].ToString().Trim();
                                }
                            }
                            QboSyncToken = _QboCustomer.SyncToken;
                            string Address = "", Phone = "", email = "", city = "", zpCode = "", state = "";

                            if (_QboCustomer.BillAddr != null)
                            {
                                if (_QboCustomer.BillAddr.Line1 != null)
                                {
                                    Address = _QboCustomer.BillAddr.Line1;
                                }
                                if (_QboCustomer.BillAddr.City != null)
                                {
                                    city = _QboCustomer.BillAddr.City;
                                }
                                if (_QboCustomer.BillAddr.PostalCode != null)
                                {
                                    zpCode = _QboCustomer.BillAddr.PostalCode;
                                }
                                if (_QboCustomer.BillAddr.CountrySubDivisionCode != null)
                                {
                                    state = _QboCustomer.BillAddr.CountrySubDivisionCode;
                                }

                            }

                            if (string.IsNullOrEmpty(Address))
                            {

                                if (_QboCustomer.Title != null)
                                {
                                    Address = _QboCustomer.Title + " ";
                                }
                                if (_QboCustomer.DisplayName != null)
                                {
                                    Address += _QboCustomer.DisplayName + " ";
                                }
                                if (_QboCustomer.CompanyName != null)
                                {
                                    Address += _QboCustomer.CompanyName;
                                }

                            }
                            if (_QboCustomer.MetaData.CreateTime > DateTime.Now)
                            {

                            }
                            if (_QboCustomer.PrimaryPhone != null)
                            {
                                if (_QboCustomer.PrimaryPhone.FreeFormNumber != null)
                                {
                                    Phone = _QboCustomer.PrimaryPhone.FreeFormNumber;
                                }
                            }
                            string mobile = string.Empty;
                            if (_QboCustomer.Mobile != null)
                            {
                                if (_QboCustomer.Mobile.FreeFormNumber != null)
                                {
                                    mobile = _QboCustomer.Mobile.FreeFormNumber;
                                }
                            }

                            if (_QboCustomer.PrimaryEmailAddr != null)
                            {
                                if (_QboCustomer.PrimaryEmailAddr.Address != null)
                                {
                                    email = _QboCustomer.PrimaryEmailAddr.Address;
                                }
                            }

                            string[] _CName = _QboCustomer.DisplayName.Split(' ');
                            string LastName = "";
                            if (_CName.Length > 1)
                            {
                                for (int i = 1; i < _CName.Length; i++)
                                {
                                    if (LastName != "")
                                        LastName += " ";
                                    LastName += _CName[i];
                                }
                            }
                            string Title = string.Empty;
                            if (_QboCustomer.Title != null)
                            {

                                Title = _QboCustomer.Title;

                            }

                            string BusinessID = "0";
                            string BusinessName = "";
                            // For Business linked Contact
                            if (IsPro)
                            {
                                if (_QboCustomer.ParentRef != null)
                                {
                                    if (_QboCustomer.ParentRef.Value != null)
                                    {
                                        foreach (DataRow dr in dt_Business.Rows)
                                        {
                                            if (_QboCustomer.ParentRef.Value.Trim() == dr["QboId"].ToString().Trim())
                                            {
                                                BusinessID = dr["CustomerID"].ToString().Trim();
                                                BusinessName = dr["BusinessName"].ToString().Trim();
                                            }
                                        }
                                    }
                                }
                            }

                            // Check If Business Contact
                            bool IsBusinessContact = false;
                            if (IsPro)
                            {

                                foreach (DataRow dr in dt_Business.Rows)
                                {
                                    if (_QboCustomer.Id.Trim() == dr["QboId"].ToString().Trim())
                                    {
                                        IsBusinessContact = true;
                                    }
                                }

                            }

                            string customer_ID = string.Empty;
                            if (!isExist && !IsBusinessContact)
                            {

                                string customer_Guid = Guid.NewGuid().ToString();
                                customer_ID = customer_Guid;


                                string strMaxId = "(select isnull(max(CustomerID),0)+1 from tbl_Customer where CompanyID='" + CompanyID + "')";
                                //Sql += "INSERT INTO tbl_Customer(BusinessName,BusinessID,SyncToken,CustomerGuid,CompanyID,CustomerID,title,FirstName,LastName, Address1,State,City,ZipCode,Phone,Mobile,Email,QboId,CreatedDateTime)" +
                                //               " VALUES  ('" + BusinessName + "','" + BusinessID + "','" + QboSyncToken + "','" + customer_Guid + "','" + CompanyID + "'," + strMaxId + ",'" + Title.Replace("'", string.Empty) + "','" + _CName[0].Trim().Replace("'", string.Empty) + "','" + LastName.Trim().Replace("'", string.Empty) +
                                //               "','" + Address.Replace("'", string.Empty) + "','" + state.Replace("'", string.Empty) + "','" +
                                //               city.Replace("'", string.Empty) + "','" +
                                //               zpCode.Replace("'", string.Empty) + "','" + Phone.Replace("'", string.Empty) + "','" + mobile.Replace("'", string.Empty) + "','" + email.Replace("'", string.Empty) + "'," + _QboCustomer.Id + ",GETDATE()); ";

                                Sql += "INSERT INTO tbl_Customer(BusinessName,BusinessID,SyncToken,CustomerGuid,CompanyID,CustomerID,title,FirstName,LastName, Address1,State,City,ZipCode,Phone,Mobile,Email,QboId,CreatedDateTime)" +
                                                  " VALUES  ('" + BusinessName + "','" + BusinessID + "','" + QboSyncToken + "','" + customer_Guid + "','" + CompanyID + "'," + strMaxId + ",'" + Title.Replace("'", string.Empty) + "','" + _CName[0].Trim().Replace("'", string.Empty) + "','" + LastName.Trim().Replace("'", string.Empty) +
                                                  "','" + Address.Replace("'", string.Empty) + "','" + state.Replace("'", string.Empty) + "','" +
                                                  city.Replace("'", string.Empty) + "','" +
                                                  zpCode.Replace("'", string.Empty) + "','" + Phone.Replace("'", string.Empty) + "','" + mobile.Replace("'", string.Empty) + "','" + email.Replace("'", string.Empty) + "'," + _QboCustomer.Id + ",GETDATE()); ";


                                //Sql += @" IF EXISTS (SELECT CustomerID  FROM [msSchedulerV3].[dbo].tbl_customer WHERE FirstName ='" + _CName[0].Trim().Replace("'", string.Empty).Replace(",","")+ "' and LastName='" + LastName.Trim().Replace("'", string.Empty).Replace(",",string.Empty) + "' and companyid='" + CompanyID + "')" +
                                //              @"BEGIN
                                //                    UPDATE [msSchedulerV3].[dbo].tbl_customer 
                                //                    SET qboid = '" + _QboCustomer.Id + "' WHERE FirstName ='" + _CName[0].Trim().Replace("'", string.Empty).Replace(",", "") + "' and LastName='" + LastName.Trim().Replace("'", string.Empty).Replace(",", string.Empty) + "' and companyid='" + CompanyID + @"'" +
                                //              " END; ";
                                //db.Execute(Sql);

                            }
                            else
                            {
                                if (!IsBusinessContact)
                                {
                                    //if (SyncToken != _QboCustomer.SyncToken.Trim())
                                    //{
                                    //    strTemp += " update tbl_Customer set FirstName='" + _CName[0].Trim().Replace("'", string.Empty) + "'";
                                    //    strTemp += " , LastName='" + LastName.Trim().Replace("'", string.Empty) + "', title='" + Title.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "',Address1='" + Address.Replace("'", string.Empty) + "'";
                                    //    strTemp += " , City='" + city.Replace("'", string.Empty) + "', ZipCode='" + zpCode.Replace("'", string.Empty) + "',Phone='" + Phone.Replace("'", string.Empty) + "'";
                                    //    strTemp += " , Mobile='" + mobile.Replace("'", string.Empty) + "' ";
                                    //    strTemp += " , State='" + state.Replace("'", string.Empty) + "' ";
                                    //    strTemp += " , SyncToken='" + QboSyncToken.Replace("'", string.Empty) + "' ";
                                    //    strTemp += " , Email='" + email.Replace("'", string.Empty) + "' ";
                                    //    strTemp += " where [QboId] ='" + _QboCustomer.Id + "' and CompanyID='" + CompanyID + "';";

                                    //}

                                }

                            }
                            if (!IsBusinessContact && _QboCustomer.BillAddr != null)
                            {
                                if (HttpContext.Current.Session["CompanyType"].ToString().ToLower() == "lhg")
                                {
                                    //billing address sync by Mohsin

                                    db.BeginTransaction();
                                    string billAdd_Guid = Guid.NewGuid().ToString();

                                    string sqlAppDtl = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_Customer] where QboId='" + _QboCustomer.Id + "' and CompanyID='" + CompanyID + "'";

                                    DataTable dt = new DataTable();
                                    db.Execute(sqlAppDtl, out dt);

                                    string custGuid = "";

                                    if (dt.Rows.Count > 0)
                                    {
                                        custGuid = dt.Rows[0]["customerGuid"].ToString();
                                    }
                                    DataTable dtBillAdd = new DataTable();
                                    if (!string.IsNullOrEmpty(custGuid))
                                    {
                                        string sqlExtBilling = @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_CustomerBillingAddress] where Customer_ID='" + custGuid + "'";
                                        db.Execute(sqlExtBilling, out dtBillAdd);
                                    }

                                    string billingAddressGuid = "";

                                    Hashtable ht = new Hashtable();
                                    if (dtBillAdd.Rows.Count > 0)
                                    {
                                        billingAddressGuid = dtBillAdd.Rows[0]["ID"].ToString();
                                        db.BeginTransaction();

                                        ht.Add("ID", billAdd_Guid);
                                        ht.Add("Address", _QboCustomer.BillAddr.Line1);
                                        ht.Add("City", _QboCustomer.BillAddr.City);
                                        ht.Add("ZipCode", _QboCustomer.BillAddr.PostalCode);
                                        ht.Add("State", _QboCustomer.BillAddr.CountrySubDivisionCode);
                                        ht.Add("Customer_ID", custGuid);

                                        Conn.Open();
                                        db.UpdateByCommand(ht, "tbl_CustomerBillingAddress", "ID='" + billingAddressGuid + "'", Conn);
                                        Conn.Close();
                                        db.CommitTransaction();
                                    }
                                    else
                                    {
                                        ht.Add("ID", billAdd_Guid);
                                        ht.Add("Address", _QboCustomer.BillAddr.Line1);
                                        ht.Add("City", _QboCustomer.BillAddr.City);
                                        ht.Add("ZipCode", _QboCustomer.BillAddr.PostalCode);
                                        ht.Add("State", _QboCustomer.BillAddr.CountrySubDivisionCode);
                                        ht.Add("Customer_ID", custGuid == "" ? customer_ID : custGuid);

                                        db.InsertByCommand(ht, "tbl_CustomerBillingAddress", Conn);
                                        db.CommitTransaction();
                                    }

                                    // end billing address
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                try
                {
                    if (!string.IsNullOrEmpty(Sql))
                    {
                        sb = Sql;
                        db.Execute(CleanInput(Sql));
                    }
                    if (!string.IsNullOrEmpty(CleanInput(strTemp)))
                    {
                        sb += strTemp;
                        //db.Execute(strTemp);
                    }

                }
                catch (Exception ex) { }

            }
            catch (Exception ex) { }
        }

        public bool EstimateSyncFromQBO(QBOSettins qboSettings, string CompanyID, ServiceContext context, string QboLastUpdatedTime)
        {
            try
            {
                Int32 STARTPOSITION = 1;
                Int32 MAXRESULTS = 1000;


                STARTPOSITION = 1;
                MAXRESULTS = 1000;


                for (int i = 1; i < 1000000; i++)
                {

                    string EstimateQuery = "Select * From Estimate Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    //EstimateQuery = "Select * From Estimate Where id = '38042' ";


                    // EstimateQuery = "Select * From Estimate where  DocNumber = '19332409' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();


                    // 19332409
                    //     19402283
                    // FOR COUNT
                    //QueryService<Intuit.Ipp.Data.Estimate> inService1 = new QueryService<Intuit.Ipp.Data.Estimate>(context);

                    //var count = inService1.ExecuteIdsQuery("select Count(*) from Estimate").Count();




                    QueryService<Intuit.Ipp.Data.Estimate> qsEstimate = new QueryService<Intuit.Ipp.Data.Estimate>(context);


                    List<Intuit.Ipp.Data.Estimate> listInvoice = qsEstimate.ExecuteIdsQuery(EstimateQuery).ToList<Intuit.Ipp.Data.Estimate>();


                    //int totalCustomerCount = qsEstimate.ExecuteIdsQuery("SELECT COUNT(*) FROM Estimate").Count();

                    //   List<Intuit.Ipp.Data.Estimate> listInvoice = qsEstimate.ExecuteIdsQuery(EstimateQuery).Where(x => x.CustomerRef.Value == "12051").ToList<Intuit.Ipp.Data.Estimate>();



                    STARTPOSITION = i * MAXRESULTS;
                    if (listInvoice.Count > 0)
                    {
                        if (Total_listPayment.Count == 0)
                        {
                            Total_listPayment = GetPayments(context, QboLastUpdatedTime);
                        }
                        ProcessEstimateFromQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrSch"].ToString(), listInvoice, Total_listPayment, context, false);

                        if (listInvoice.Count < MAXRESULTS)
                        {
                            i = 1000000;
                            return true;
                        }

                    }
                    else
                    {
                        i = 1000000;
                        return true;
                    }

                }


                //UpdateInsertInvoiceQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrJobs"].ToString(), listInvoice, listPayment, context, true);


            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);

                }
                return false;
            }
            return true;
        }
        public bool IsDateTime(string date)
        {
            if (string.IsNullOrEmpty(date)) return false;
            return DateTime.TryParse(date, out DateTime dateTime);
        }
        // Invoice Synchronization
        private List<Intuit.Ipp.Data.Payment> GetPayments(ServiceContext context, string QboLastUpdatedTime)
        {
            Int32 STARTPOSITION = 1;
            Int32 MAXRESULTS = 1000;
            List<Intuit.Ipp.Data.Payment> Total_listPayment = new List<Intuit.Ipp.Data.Payment>();
            for (int i = 1; i < 1000000; i++)
            {
                try
                {
                    QboLastUpdatedTime = Convert.ToDateTime("2020/08/01").AddYears(-1).ToString("yyyy-MM-ddTHH:mm:ss");
                }
                catch { }


                string qboPayement = "select * from Payment Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "'  STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString(); ;


                QueryService<Intuit.Ipp.Data.Payment> qsPayment = new QueryService<Intuit.Ipp.Data.Payment>(context);

                List<Intuit.Ipp.Data.Payment> listPayment = qsPayment.ExecuteIdsQuery(qboPayement).ToList<Intuit.Ipp.Data.Payment>();
                STARTPOSITION = i * MAXRESULTS;

                if (listPayment.Count > 0)
                {
                    Total_listPayment.AddRange(listPayment);
                    //UpdateInsertInvoiceQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrJobs"].ToString(), listInvoice, listPayment, context, true);

                    if (listPayment.Count < MAXRESULTS)
                    {
                        i = 1000000;

                    }

                }
                else
                {
                    i = 1000000;

                }

            }
            return Total_listPayment;
        }
        private void GetPaymentFromQBO(string CompanyID, List<Intuit.Ipp.Data.Payment> listPayment)
        {

            string query = string.Empty;

            try
            {
                foreach (Payment iPnt in listPayment)
                {
                    string TxnId = string.Empty;
                    foreach (Line ln in iPnt.Line)
                    {
                        foreach (LinkedTxn txln in ln.LinkedTxn)
                        {
                            if (!string.IsNullOrEmpty(txln.TxnId))
                            {
                                TxnId = txln.TxnId;
                            }

                            query += @"INSERT INTO msSchedulerV3.dbo.[tbl_Payment]
                           ([Companyid]
                           ,[InvocieId]
                           ,[Amount]
                           ,[Type]
                           ,[Source]
                           ,CheckName
                           ,CheckNumber
                           ,[QboId])
                         VALUES
                               ('" + CompanyID +
                               "',(select id FROM msSchedulerV3.dbo.tbl_Invoice WHERE CompnyID='" + CompanyID + "' and QboId='@TxnId')" +
                               ",'" + iPnt.TotalAmt +
                               "','" + iPnt.PaymentType.GetStringValue() +
                                "','" + "qbo" +
                               "','" + "" +
                                "','" + "" +
                               "'," + iPnt.DocNumber + ");";

                        }
                    }
                }
            }
            catch { }


            if (!string.IsNullOrEmpty(query))
            {
                Database db = new Database();
                // db.Execute(query);
            }




        }
        List<Intuit.Ipp.Data.Payment> Total_listPayment = new List<Intuit.Ipp.Data.Payment>();
        public bool InvoiceSyncFromQBO(QBOSettins qboSettings,
            string CompanyID,
            ServiceContext context,
            string QboLastUpdatedTime)
        {
            try
            {
                Int32 STARTPOSITION = 1;
                Int32 MAXRESULTS = 1000;


                StringBuilder _StringBuilder = new StringBuilder();


                for (int i = 1; i < 1000000; i++)
                {

                    string qboQuery = "Select * From Invoice Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "'  STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    //qboQuery = "Select * From Invoice where  DocNumber = '2614972' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();
                    // qboQuery = "Select * From Invoice where  id = '210' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();

                    //qboQuery = "Select * From Invoice where  id ='109163' STARTPOSITION " + STARTPOSITION.ToString() + " MAXRESULTS " + MAXRESULTS.ToString();



                    QueryService<Intuit.Ipp.Data.Invoice> qsInvoice = new QueryService<Intuit.Ipp.Data.Invoice>(context);





                    List<Intuit.Ipp.Data.Invoice> listInvoice = qsInvoice.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Invoice>();

                    //Intuit.Ipp.Data.Invoice sdsd = listInvoice.Where(n => n.DocNumber == "50173").FirstOrDefault();
                    //int totalCustomerCount = qsInvoice.ExecuteIdsQuery("SELECT COUNT(*) FROM Invoice").Count();


                    //List<Intuit.Ipp.Data.Invoice> awfasdfasf = qsInvoice.ExecuteIdsQuery(qboQuery).Where(x => x.DocNumber == "50173").ToList<Intuit.Ipp.Data.Invoice>();

                    STARTPOSITION = i * MAXRESULTS;

                    if (listInvoice.Count > 0)
                    {
                        if (Total_listPayment.Count == 0)
                        {
                            Total_listPayment = GetPayments(context, QboLastUpdatedTime);
                        }
                        string sb = "";
                        ProcessInvocieFromQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrSch"].ToString(), listInvoice, Total_listPayment, context, false, out sb);
                        _StringBuilder.Append(sb);
                        // GetPaymentFromQBO(CompanyID, Total_listPayment);
                        if (listInvoice.Count < MAXRESULTS)
                        {
                            string sds = _StringBuilder.ToString();
                            i = 1000000;
                            return true;
                        }
                        //UpdateInsertInvoiceQBO(CompanyID, ConfigurationManager.AppSettings["ConnStrJobs"].ToString(), listInvoice, listPayment, context, true);

                    }
                    else
                    {
                        string sds = _StringBuilder.ToString();
                        i = 1000000;
                        return true;
                    }

                }
                return true;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);

                }
                return false;
            }
        }

        private bool ProcessEstimateFromQBO(string CompanyID,
         string ConString, List<Intuit.Ipp.Data.Estimate> listItems,
         List<Payment> listPayment,
         ServiceContext context,
         bool dbJobs)
        {
            try
            {
                Database db = new Database(ConString);


                List<string> _QboEstimateId = new List<string>();
                List<string> _QboCustomerID = new List<string>();
                foreach (Intuit.Ipp.Data.Estimate ilst in listItems)
                {
                    _QboEstimateId.Add("'" + ilst.Id + "'");
                    _QboCustomerID.Add("'" + ilst.CustomerRef.Value + "'");
                }

                string Sql = "select Id,QboId from Items where  CompanyID=@CompanyID;";
                DataTable dt_Items;
                // db.Execute(Sql, out dt_Items);

                Sql += "select QboId,CustomerID from tbl_Customer where  CompanyID=@CompanyID " +
                     " and QboId in (" + string.Join(",", _QboCustomerID) + ");";

                DataTable dt_Customers;


                Sql += "select id,QboId,SyncToken,QboEstimateId from tbl_Invoice Where ([Type]='Proposal' or [Type]='Estimate') and CompnyID=@CompanyID " +
                        " and QboEstimateId in (" + string.Join(",", _QboEstimateId) + ");";

                if (dbJobs)
                    Sql = "select * from Invoices where CompnyID=@CompanyID;";

                DataTable dt_Invoice;



                Sql += "select Id,QboId from Taxes where  CompanyId=@CompanyID;";

                DataTable dt_Taxes;


                DataSet dataSet = db.Get_DataSet(Sql, CompanyID);
                dt_Items = dataSet.Tables[0];
                dt_Customers = dataSet.Tables[1];
                dt_Invoice = dataSet.Tables[2];
                dt_Taxes = dataSet.Tables[3];

                Sql = "";
                string itemInsertSql = "";
                bool isExist = false;
                Int32 totalRecordToInsert = 0;


                List<InsertQuery> insertQuerys = new List<InsertQuery>();
                foreach (Intuit.Ipp.Data.Estimate estimate in listItems)
                {
                    string InvoiceID = Guid.NewGuid().ToString().ToUpper();

                    bool IsRejected = false;
                    if (estimate.TxnStatus == "Rejected")
                    {
                        Console.WriteLine("This estimate has been declined.");
                        IsRejected = true;
                    }


                    InsertQuery insertQuery = new InsertQuery();
                    string SyncToken = "";
                    isExist = false;
                    foreach (DataRow dr in dt_Invoice.Rows)
                    {
                        if (estimate.Id == dr["QboEstimateId"].ToString().Trim())
                        {
                            isExist = true;
                            SyncToken = dr["SyncToken"].ToString().Trim();
                            InvoiceID = dr["id"].ToString().Trim();
                        }
                    }
                    itemInsertSql = "";
                    // Create Invoice From QBO
                    //if (!isExist)
                    //{

                    string ShipAddr = GetAddress(estimate.ShipAddr);

                    string BillAddr = GetAddress(estimate.BillAddr);

                    string CustomerID = "", _Note = estimate.PrivateNote, TaxTp = "", discountOpt = "2", DiscountRate = "0", Discount = "0";

                    // Extract QboClassId and QboLocationId from QBO Estimate
                    string qboClassId = "0";
                    string qboLocationId = "0";
                    if (estimate.ClassRef != null && !string.IsNullOrEmpty(estimate.ClassRef.Value))
                    {
                        qboClassId = estimate.ClassRef.Value;
                    }
                    if (estimate.DepartmentRef != null && !string.IsNullOrEmpty(estimate.DepartmentRef.Value))
                    {
                        qboLocationId = estimate.DepartmentRef.Value;
                    }

                    string _Paid = GetPaidAmount(estimate.Id, listPayment);
                    decimal TotalTax = 0;
                    try
                    {
                        if (estimate.TxnTaxDetail != null)
                        {
                            if (estimate.TxnTaxDetail.TotalTax != null)
                            {
                                TotalTax = estimate.TxnTaxDetail.TotalTax;
                            }
                            if (estimate.TxnTaxDetail.TxnTaxCodeRef != null)
                            {
                                TaxTp = estimate.TxnTaxDetail.TxnTaxCodeRef.Value;

                                foreach (DataRow dr in dt_Taxes.Rows)
                                {
                                    if (TaxTp.Trim() == dr["QboId"].ToString().Trim())
                                    {

                                        TaxTp = dr["id"].ToString().Trim();
                                    }
                                }

                            }
                        }

                    }
                    catch { }

                    if (_Note == null)
                    {
                        _Note = string.Empty;
                    }
                    // cID = GetCustomerId(ilst.CustomerRef.Value, CompanyID, ConString, dbJobs);


                    CustomerID = "0"; // GetProductId(LineProduct.ItemRef.Value, CompanyID, ConString);

                    foreach (DataRow dataRow in dt_Customers.Rows)
                    {
                        if (dataRow["QboId"].ToString() == estimate.CustomerRef.Value)
                        {
                            CustomerID = dataRow["CustomerID"].ToString();
                        }
                    }


                    Line[] PrdLIne = estimate.Line;
                    foreach (Line pLn in PrdLIne)
                    {
                        string TaxSt = "NON";
                        string LineNum = string.Empty;
                        if (pLn.LineNum != null)
                        {
                            LineNum = pLn.LineNum;
                        }

                        // To Check Discount
                        if (pLn.DetailType == Intuit.Ipp.Data.LineDetailTypeEnum.DiscountLineDetail)
                        {
                            DiscountLineDetail _DiscountLineDetail = null;

                            Discount = pLn.Amount.ToString();
                            DiscountRate = pLn.Amount.ToString();
                            if (pLn.AnyIntuitObject.GetType() == typeof(DiscountLineDetail))
                            {
                                _DiscountLineDetail = (DiscountLineDetail)pLn.AnyIntuitObject;
                            }
                            if (_DiscountLineDetail != null)
                            {
                                if (_DiscountLineDetail.PercentBased)
                                {
                                    discountOpt = "1";
                                    DiscountRate = _DiscountLineDetail.DiscountPercent.ToString();

                                }
                            }

                        }
                        // To Check ITEM for WT Discount
                        //if (pLn.Description != null)
                        //{
                        //    if (pLn.Description.ToLower() == "discount")
                        //    {
                        //        if (pLn.Amount != null)
                        //        {
                        //            Discount = Math.Abs(pLn.Amount).ToString();
                        //            DiscountRate = Math.Abs(pLn.Amount).ToString();

                        //        }


                        //    }
                        //}


                        try
                        {
                            string pId = string.Empty;
                            string pName = string.Empty;
                            decimal pQty = 0;
                            decimal pRate = 0;
                            string ItemDescription = "";
                            DateTime ServiceDate = DateTime.Now;

                            Intuit.Ipp.Data.SubTotalLineDetail subTotalLineDetail = null;

                            if (pLn.AnyIntuitObject.GetType() == typeof(SubTotalLineDetail))
                            {
                                subTotalLineDetail = (SubTotalLineDetail)pLn.AnyIntuitObject;
                            }
                            if (subTotalLineDetail != null)
                            {

                            }
                            SalesItemLineDetail LineProduct = null;
                            if (pLn.AnyIntuitObject.GetType() == typeof(SalesItemLineDetail))
                            {
                                LineProduct = (SalesItemLineDetail)pLn.AnyIntuitObject;
                            }

                            if (LineProduct != null)
                            {
                                //  pId = GetProductId(LineProduct.ItemRef.Value, CompanyID, ConString);

                                if (LineProduct.TaxCodeRef != null)
                                {
                                    if (LineProduct.TaxCodeRef.Value != null)
                                    {
                                        if (LineProduct.TaxCodeRef.Value == "TAX")
                                        {
                                            TaxSt = "TAX";
                                        }
                                    }
                                }

                                pId = "0"; // GetProductId(LineProduct.ItemRef.Value, CompanyID, ConString);

                                foreach (DataRow dataRow in dt_Items.Rows)
                                {
                                    if (dataRow["QboId"].ToString() == LineProduct.ItemRef.Value)
                                    {
                                        pId = dataRow["id"].ToString();
                                    }
                                }

                                if (LineProduct.ItemRef.name != null)
                                {
                                    pName = LineProduct.ItemRef.name.ToString().Replace("'", string.Empty).Replace(";", string.Empty).Replace("\"", string.Empty);
                                }

                                pQty = LineProduct.Qty;

                                try
                                {

                                    if (pLn.Description != null)
                                    {
                                        ItemDescription = pLn.Description.ToString().Replace(";", string.Empty).Replace("\"", string.Empty);
                                    }

                                    if (ItemDescription.Contains(","))
                                    {
                                        if (ItemDescription.Contains("code") &&
                                            ItemDescription.Contains("qty") &&
                                            ItemDescription.Contains("unit_type"))
                                        {
                                            string Item_Des = string.Empty;
                                            var reg = new Regex("\"([^\"]*)\"");
                                            var matches = reg.Matches(pLn.Description);
                                            foreach (var item in matches)
                                            {
                                                Item_Des += item.ToString();
                                            }

                                            ItemDescription = Item_Des.ToString().Replace(";", string.Empty).Replace("\"", string.Empty);
                                        }

                                    }

                                }
                                catch { }


                                try
                                {

                                    ServiceDate = LineProduct.ServiceDate;
                                }
                                catch { }


                                try
                                {
                                    if (LineProduct.ItemElementName == ItemChoiceType.UnitPrice)
                                    {
                                        pRate = (decimal)LineProduct.AnyIntuitObject;
                                    }


                                    pRate = (decimal)LineProduct.AnyIntuitObject;
                                }
                                catch { }

                                //if (pRate == 0)
                                //{
                                //if (pLn.Amount > 0)
                                //{
                                if (LineProduct.Qty > 0)
                                {
                                    pRate = pLn.Amount / LineProduct.Qty;
                                }
                                else
                                {
                                    pRate = pLn.Amount;
                                }

                                // }
                                //}

                                if (!dbJobs)

                                    itemInsertSql += @" insert into tbl_InvoiceDetails (LineNum,CompanyID,ServiceDate,Description,RefId, ItemId,ItemName, Quantity, uPrice , TotalPrice,  IsTaxable, ItemTyId,CreatedDate, CreatedBy) values
                                        ('" + LineNum + "','" + CompanyID + "','" + ServiceDate.ToString("yyyy-MM-dd") + "','" + ItemDescription.ToString().Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "','" + InvoiceID + "','" + pId + "','" + pName + "','" + Convert.ToDecimal(pQty) + "','" + Convert.ToDecimal(pRate) + "','" + pLn.Amount + "','"
                                            + TaxSt.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "','" + "" + "','" + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "','" + "QBO" + "');";
                                else
                                {
                                    TaxSt = "0";
                                    itemInsertSql += @" insert into InvoiceItems (InvoiceNumber,ItemId, Quantity, UnitPrice, ItemName , ItemDescription,  IsTaxable, CreatedDate,ModifiedDate) values
                                        ('" + estimate.DocNumber.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "', '" + pId + "', '" + pQty + "', '" + Convert.ToDecimal(pRate) + "' , '" + pName + "', '" + ItemDescription.Replace("'", "") + "'," + TaxSt + "," + @"
                                        SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET() ) ; ";
                                }

                            }






                        }
                        catch (Exception ex) { }

                    }

                    string TaxAmount = "0";
                    if (estimate.TxnTaxDetail != null)
                        TaxAmount = estimate.TxnTaxDetail.TotalTax.ToString();


                    decimal Subtotal = estimate.TotalAmt;


                    try
                    {
                        Subtotal = estimate.TotalAmt - (TotalTax - estimate.DiscountAmt);
                    }
                    catch { }



                    string ExpirationDate = "NULL";

                    string InvocieCraetedDate = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
                    if (estimate.MetaData != null)
                    {
                        if (estimate.MetaData.CreateTime != null)
                        {
                            InvocieCraetedDate = estimate.MetaData.CreateTime.ToString("yyyy-MM-dd h:mm:ss tt");
                            ExpirationDate = "'" + estimate.MetaData.CreateTime.AddDays(7).ToString("MM/dd/yyyy") + "'";
                        }
                    }


                    if (estimate.ExpirationDate != null)
                    {
                        DateTime dateTime = new DateTime(estimate.ExpirationDate.Year, estimate.ExpirationDate.Month, estimate.ExpirationDate.Day);
                        if (estimate.ExpirationDate.Year > 50)
                        {
                            ExpirationDate = "'" + estimate.ExpirationDate.ToString("MM/dd/yyyy") + "'";
                        }
                    }

                    if (!dbJobs)
                    {

                        if (!isExist)
                        {
                            //  getNextNumber(CompanyID);
                            //if (ilst.status != EntityStatusEnum.Deleted)
                            //{
                            totalRecordToInsert += 1;

                            string Insert = @" IF NOT  EXISTS (SELECT QboEstimateId FROM tbl_Invoice WHERE QboEstimateId = '" + estimate.Id + "' and CompnyID = '" + CompanyID + "' and CustomerId='" + CustomerID + "') " +
                         "BEGIN ";
                            Insert += @" Insert into tbl_Invoice (id,IsRejected,SyncToken,ExpirationDate,QboEstimateId, CompnyID , TaxType,
                                        Number, CustomerId, Note, UserId, Subtotal,DiscountOption,DiscountRate,Discount, Tax, Total, Status, InvoiceDate,
                                        InvoiceType, CreatedDate, CreatedBy,AmountCollect, Type , AppointmentId,QboId,BillTo,ShipTo,QboClassId,QboLocationId) values 
                                        ('" + InvoiceID + "','" + IsRejected + "','" + estimate.SyncToken.Trim() + "'," + ExpirationDate + ",'" + estimate.Id + "','" + CompanyID + "' , '" + TaxTp + "', '" + estimate.DocNumber + "', '" + CustomerID + "', '" + _Note.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "', '" + HttpContext.Current.Session["LoginUser"].ToString() + "', '"
                                  + Subtotal + "','" + discountOpt + "', '" + DiscountRate + "','" + Discount + "', '" + TaxAmount + "', '" + estimate.TotalAmt + "', '" + "1" + "', '" + estimate.TxnDate.ToString("MM/dd/yyyy") + "', '" + "" + "', '"
                                  + InvocieCraetedDate + "', 'QBO' ," + _Paid + ",'Proposal','','" + "0" + "','" +
                                   BillAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "','" +
                                   ShipAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "'," +
                                  (string.IsNullOrEmpty(qboClassId) || qboClassId == "0" ? "NULL" : qboClassId) + "," +
                                  (string.IsNullOrEmpty(qboLocationId) || qboLocationId == "0" ? "NULL" : qboLocationId) + ");";
                            Insert += Environment.NewLine;
                            insertQuery.InvoiceDetails = itemInsertSql;

                            Insert += Environment.NewLine + "END " + Environment.NewLine;



                            // Check if Estimate IS converted
                            if (estimate.LinkedTxn != null)
                            {
                                foreach (LinkedTxn linkedTxn in estimate.LinkedTxn)
                                {
                                    if (linkedTxn.TxnType.Trim() == "Invoice")
                                    {
                                        Insert += "Update tbl_Invoice set IsConverted=1,ConvertedInvocieID=(select ID from tbl_Invoice where [CompnyID]='" +
                                          CompanyID + "' and  QboId='" + linkedTxn.TxnId + "')" +
                                          ",ConvertedInvocieNumber=(select Number from tbl_Invoice where [CompnyID]='" +
                                          CompanyID + "' and  QboId='" + linkedTxn.TxnId + "') where [CompnyID]='" +
                                          CompanyID + "' and  ID='" + InvoiceID + "';";
                                    }
                                }
                            }


                            insertQuery.Invoice = Insert;
                            //}



                        }
                        else
                        {
                            if (estimate.LinkedTxn != null)
                            {
                                string strTemp = string.Empty;
                                foreach (LinkedTxn linkedTxn in estimate.LinkedTxn)
                                {
                                    if (linkedTxn.TxnType.Trim() == "Invoice")
                                    {
                                        strTemp = "Update tbl_Invoice set IsConverted=1,ConvertedInvocieID=(select ID from tbl_Invoice where [CompnyID]='" +
                                          CompanyID + "' and  QboId='" + linkedTxn.TxnId + "')" +
                                          ",ConvertedInvocieNumber=(select Number from tbl_Invoice where [CompnyID]='" +
                                          CompanyID + "' and  QboId='" + linkedTxn.TxnId + "') where [CompnyID]='" +
                                          CompanyID + "' and  ID='" + InvoiceID + "';";
                                    }
                                }
                                insertQuery.Invoice = strTemp;
                            }
                            //if (ilst.status == EntityStatusEnum.Deleted)
                            //{
                            //    string strTemp = string.Empty;
                            //    strTemp += "delete from tbl_Invoice where id='" + InvoiceID + "' and CompnyID='" + CompanyID + "';";
                            //    strTemp += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";
                            //    insertQuery.Invoice = strTemp;
                            //}


                            if (SyncToken != estimate.SyncToken.Trim() && SyncToken != "")
                            {
                                string strTemp = " update tbl_Invoice set TaxType='" + TaxTp + "'";
                                strTemp += " , CustomerId='" + CustomerID + "', Note='" + _Note.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "',Subtotal='" + estimate.TotalAmt + "'";
                                strTemp += " , DiscountOption='" + discountOpt + "', DiscountRate='" + DiscountRate + "',Tax='" + TaxAmount + "'";
                                strTemp += " , Total='" + estimate.TotalAmt + "' ";
                                strTemp += " , Discount='" + Discount + "' ";
                                strTemp += " , IsRejected='" + IsRejected + "' ";
                                strTemp += " , BillTo='" + BillAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "' ";
                                strTemp += " , ShipTo='" + ShipAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "' ";
                                //  strTemp += " , Number='" + ilst.DocNumber + "' ";

                                //  strTemp += " , AmountCollect='" + _Paid + "' ";DiscountRate + "','" + Discount
                                strTemp += " , Type='Proposal' ";
                                strTemp += " , Number='" + estimate.DocNumber.Trim() + "' ";
                                strTemp += " , SyncToken='" + estimate.SyncToken.Trim() + "' ";
                                strTemp += " , QboClassId=" + (string.IsNullOrEmpty(qboClassId) || qboClassId == "0" ? "NULL" : qboClassId);
                                strTemp += " , QboLocationId=" + (string.IsNullOrEmpty(qboLocationId) || qboLocationId == "0" ? "NULL" : qboLocationId);
                                strTemp += " where QboEstimateId='" + estimate.Id + "' and CompnyID='" + CompanyID + "';";



                                strTemp += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";

                                if (estimate.LinkedTxn == null)
                                {
                                    strTemp += "Update tbl_Invoice set IsConverted=0,ConvertedInvocieID='',ConvertedInvocieNumber='' where [CompnyID]='" +
                                      CompanyID + "' and  ID='" + InvoiceID + "';";
                                }
                                //if (ilst.status == EntityStatusEnum.Deleted)
                                //{
                                //    strTemp += "delete from tbl_Invoice where id='" + InvoiceID + "' and CompnyID='" + CompanyID + "';";
                                //    strTemp += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";
                                //}
                                insertQuery.Invoice = strTemp;
                                insertQuery.InvoiceDetails = itemInsertSql;
                            }
                        }
                    }
                    else
                    {
                        DateTime InvoiceDate = new DateTime(estimate.TxnDate.Year,
                                               estimate.TxnDate.Month,
                                                estimate.TxnDate.Day,
                                                23, 59, 59, 999);
                        string InvoiceUID = Guid.NewGuid().ToString("n").Substring(0, 8);



                        if (!isExist)
                        {
                            Sql += @"Insert into Invoices (InvoiceUID,CompanyId , Number, CustomerId, UserId, Subtotal, Discount, Tax, Total, Status, InvoiceDate,ModifiedDate, InvoiceType, CreatedDate) values 
                            ('" + InvoiceUID + "','" + CompanyID + "' , '" + estimate.DocNumber + "', '" + CustomerID + "', " + GetJobsUsertId(HttpContext.Current.Session["LoginUser"].ToString()) + ", '"
                          + estimate.TotalAmt + "', '" + Discount + "', '" + TaxAmount + "', '" + estimate.TotalAmt + "', '" + "0" + "', '" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + "" + "',SYSDATETIMEOFFSET());";
                            Sql += itemInsertSql;
                            totalRecordToInsert += 1;

                        }
                        else
                        {
                            if (SyncToken != estimate.SyncToken.Trim() && SyncToken != "")
                            {
                                string strTemp = " update Invoices set ";
                                strTemp += " CustomerId='" + CustomerID + "', Subtotal='" + estimate.TotalAmt + "'";
                                strTemp += " ,Discount='" + Discount + "',Tax='" + TaxAmount + "'";
                                strTemp += " , IsRejected='" + IsRejected + "' ";
                                strTemp += " , Total='" + estimate.TotalAmt + "' ";
                                strTemp += " , BillTo='" + BillAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "' ";
                                strTemp += " , ShipTo='" + ShipAddr.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "' ";
                                strTemp += " , QboClassId=" + (string.IsNullOrEmpty(qboClassId) || qboClassId == "0" ? "NULL" : qboClassId);
                                strTemp += " , QboLocationId=" + (string.IsNullOrEmpty(qboLocationId) || qboLocationId == "0" ? "NULL" : qboLocationId);
                                strTemp += " , Number='" + estimate.DocNumber.Trim() + "' ";
                                //   strTemp += " where Number='" + ilst.DocNumber + "' and CompanyId='" + CompanyID + "'";
                                strTemp += " where [QboId] ='" + estimate.Id + "' and CompnyID='" + CompanyID + "';";
                                Sql += strTemp;
                                Sql += "delete from InvoiceItems where InvoiceNumber='" + estimate.DocNumber + "'";
                                Sql += itemInsertSql;
                            }
                        }


                    }


                    if (SyncToken != estimate.SyncToken.Trim() || !isExist)
                    {

                    }
                    insertQuerys.Add(insertQuery);
                    //}
                    //}
                }


                for (int i = 0; i < insertQuerys.Count; i = i + 100)
                {
                    var items = insertQuerys.Skip(i).Take(100);
                    string mergedInsertQry = string.Empty;
                    foreach (InsertQuery insertQuery in items)
                    {
                        if (!string.IsNullOrEmpty(insertQuery.Invoice))
                        {


                            mergedInsertQry += insertQuery.Invoice;
                            mergedInsertQry += insertQuery.InvoiceDetails;
                        }
                    }
                    if (mergedInsertQry != "")
                    {
                        string Invoice = " begin " + mergedInsertQry + " return end;";

                        db.Execute(CleanInput(Invoice));
                        try
                        {
                            if (HttpContext.Current.Session["CompanyID"].ToString() == "13116")
                            {
                               
                            }

                        }
                        catch { }

                    }

                    // Do something with 100 or remaining items
                }
                if (totalRecordToInsert > 0)
                {
                    UpdateEstimateSeedNumber(CompanyID, totalRecordToInsert);
                }


                //  db.Close();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        private string GetAddress(PhysicalAddress address)
        {
            if (address == null) return "";

            string streetAddress = !string.IsNullOrEmpty(address?.Line1) ? address?.Line1 + ',' + Environment.NewLine : "";
            string city = !string.IsNullOrEmpty(address?.City) ? address?.City : "";
            string state = address?.CountrySubDivisionCode ?? "";
            string zipCode = !string.IsNullOrEmpty(address?.PostalCode) ? address?.PostalCode + "," + Environment.NewLine : "";
            string Line2 = address?.Line2 + Environment.NewLine ?? "";
            string Line3 = address?.Line3 + Environment.NewLine ?? "";
            string Line4 = address?.Line4 + Environment.NewLine ?? "";
            string Line5 = address?.Line5 + Environment.NewLine ?? "";




            return $"{streetAddress}  {Line2} {Line3} {Line4} {Line5} {city} {state} {zipCode} ";
        }
        public string CleanInput(string sInput, int iLength = 0)
        {
            if (sInput == null)
            {
                return "";
            }
            if (iLength > 0)
            {
                if (sInput.Length > iLength)
                {
                    sInput = sInput.Substring(0, iLength);
                }
            }

            sInput = sInput.Replace("--", "");
            sInput = sInput.Replace("<", "");
            sInput = sInput.Replace(">", "");
            sInput = sInput.Replace("html", "");
            sInput = sInput.Replace("*", "");
            sInput = sInput.Replace("href", "");
            sInput = sInput.Replace("&lt", "");
            sInput = sInput.Replace("&gt", "");
            sInput = sInput.Replace("&quot", "");
            sInput = sInput.Replace("alert", "");
            sInput = sInput.Replace("<script>", "");
            sInput = sInput.Replace("</script>", "");


            return sInput;

        }
        private string UpdateInvoceiSeedNumber(string CompanyID, Int32 TotalRecords)
        {
            DataTable table;
            string Number = string.Empty;
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
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
                    Int64 invoiceNumberSeed = Convert.ToInt64(table.Rows[0]["InvoiceNumberSeed"]) + TotalRecords + 1;
                    string invoicePrefix = table.Rows[0]["InvoicePrefix"].ToString();

                    if (invoiceNumberSeed < 10001)
                    {
                        // ensure Invoice number start 10001
                        invoiceNumberSeed = 10001;
                    }
                    sql = @"Update [msSchedulerV3].[dbo].[tbl_Company] set InvoiceNumberSeed=" + invoiceNumberSeed + " Where CompanyID= '" + CompanyID + "'";
                    db.Execute(sql);
                }
                db.Close();
                return Number;
            }
            catch
            {
                return DateTime.Now.ToString("hmmss");
            }

        }

        private string UpdateEstimateSeedNumber(string CompanyID, Int32 TotalRecords)
        {
            DataTable table;
            string Number = string.Empty;
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
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
                    Int64 invoiceNumberSeed = Convert.ToInt64(table.Rows[0]["EstimateNumberSeed"]) + TotalRecords + 1;

                    if (invoiceNumberSeed < 10001)
                    {
                        // ensure Invoice number start 10001
                        invoiceNumberSeed = 10001;
                    }
                    sql = @"Update [msSchedulerV3].[dbo].[tbl_Company] set EstimateNumberSeed=" + invoiceNumberSeed + " Where CompanyID= '" + CompanyID + "'";
                    db.Execute(sql);
                }
                db.Close();
                return Number;
            }
            catch
            {
                return DateTime.Now.ToString("hmmss");
            }

        }



        private void ProcessInvocieFromQBO(string CompanyID,
            string ConString, List<Intuit.Ipp.Data.Invoice> listItems,
            List<Payment> listPayment,
            ServiceContext context,
            bool dbJobs, out String sb)
        {
            sb = "";
            try
            {
                Database db = new Database(ConString);



                List<string> _QboInvoiceId = new List<string>();
                List<string> _QboCustomerID = new List<string>();
                //foreach (Intuit.Ipp.Data.Invoice ilst in listItems)
                //{
                //    _QboInvoiceId.Add("'" + ilst.Id + "'");
                //    _QboCustomerID.Add("'" + ilst.CustomerRef.Value + "'");
                //}


                string Sql = "select Id,QboId from Items where  CompanyID=@CompanyID;";
                DataTable dt_Items = new DataTable();
                // db.Execute(Sql, out dt_Items);

                //Sql += "select QboId,CustomerID from tbl_Customer where  CompanyID=@CompanyID" +
                //     " and QboId in (" + string.Join(",", _QboCustomerID) + ");";

                DataTable dt_Customers = new DataTable();


                //Sql += "select id,QboId,SyncToken,isnull(AmountCollect,0) as AmountCollect from tbl_Invoice Where [Type]='Invoice' and CompnyID=@CompanyID" +
                //     " and QboId in (" + string.Join(",", _QboInvoiceId) + ");";

                //if (dbJobs)
                //    Sql = "select * from Invoices where CompanyId=@CompanyID";

                DataTable dt_Invoice = new DataTable();



                Sql += "select Id,QboId from Taxes where  CompanyId=@CompanyID";

                DataTable dt_Taxes = new DataTable();
                DataSet dataSet = null;
                if (HttpContext.Current.Session["dataSet"] == null)
                {
                    dataSet = db.Get_DataSet(Sql, CompanyID);
                    HttpContext.Current.Session["dataSet"] = dataSet;
                }
                else
                {
                    dataSet = (DataSet)HttpContext.Current.Session["dataSet"];
                }




                dt_Items = dataSet.Tables[0];
                //dt_Customers = dataSet.Tables[1];
                //dt_Invoice = dataSet.Tables[2];
                dt_Taxes = dataSet.Tables[1];

                Sql = "";
                bool isExist = false;
                string itemInsertSql = "";
                Int32 totalRecordToInsert = 0;
                decimal AmountCollect = 0;
                for (int i = 0; i < listItems.Count; i++)
                {

                    InsertQuery insertQuery = new InsertQuery();

                    string InvoiceID = Guid.NewGuid().ToString().ToUpper();
                    string SyncToken = "";
                    isExist = false;
                    foreach (DataRow dr in dt_Invoice.Rows)
                    {
                        if (listItems[i].Id == dr["QboId"].ToString().Trim())
                        {
                            isExist = true;
                            SyncToken = dr["SyncToken"].ToString().Trim();
                            InvoiceID = dr["id"].ToString().Trim();
                            AmountCollect = Convert.ToDecimal(dr["AmountCollect"].ToString().Trim());
                        }
                    }

                    itemInsertSql = "";
                    string cID = "", _Note = listItems[i].PrivateNote, TaxTp = "", discountOpt = "2", DiscountRate = "0", Discount = "0"; ;

                    // Extract QboClassId and QboLocationId from QBO Invoice
                    string qboClassId = "0";
                    string qboLocationId = "0";
                    if (listItems[i].ClassRef != null && !string.IsNullOrEmpty(listItems[i].ClassRef.Value))
                    {
                        qboClassId = listItems[i].ClassRef.Value;
                    }
                    if (listItems[i].DepartmentRef != null && !string.IsNullOrEmpty(listItems[i].DepartmentRef.Value))
                    {
                        qboLocationId = listItems[i].DepartmentRef.Value;
                    }

                    if (_Note == null)
                    {
                        _Note = string.Empty;
                    }

                    string _Paid = GetPaidAmount(listItems[i].Id, listPayment);

                    if (listItems[i].CustomerMemo != null)
                    {
                        if (!string.IsNullOrEmpty(listItems[i].CustomerMemo.Value))
                        {
                            _Note = listItems[i].CustomerMemo.Value.ToString();
                        }
                    }
                    //cID = GetCustomerId(ilst.CustomerRef.Value, CompanyID, ConString, dbJobs);


                    cID = listItems[i].CustomerRef.Value; // GetProductId(LineProduct.ItemRef.Value, CompanyID, ConString);

                    foreach (DataRow dataRow in dt_Customers.Rows)
                    {
                        if (dataRow["QboId"].ToString() == listItems[i].CustomerRef.Value)
                        {
                            cID = dataRow["CustomerID"].ToString();
                        }
                    }


                    string TaxAmount = "0";
                    try
                    {
                        if (listItems[i].TxnTaxDetail != null)
                        {
                            if (listItems[i].TxnTaxDetail.TxnTaxCodeRef != null)
                            {
                                TaxTp = listItems[i].TxnTaxDetail.TxnTaxCodeRef.Value;

                                foreach (DataRow dr in dt_Taxes.Rows)
                                {
                                    if (TaxTp.Trim() == dr["QboId"].ToString().Trim())
                                    {

                                        TaxTp = dr["id"].ToString().Trim();
                                    }
                                }

                            }
                        }

                    }
                    catch { }
                    if (listItems[i].TxnTaxDetail != null)
                        TaxAmount = listItems[i].TxnTaxDetail.TotalTax.ToString();

                    string InvocieCraetedDate = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
                    if (listItems[i].MetaData != null)
                    {
                        if (listItems[i].MetaData.CreateTime != null)
                        {
                            InvocieCraetedDate = listItems[i].MetaData.CreateTime.ToString("yyyy-MM-dd h:mm:ss tt");
                        }
                    }

                    Line[] PrdLIne = listItems[i].Line;


                    foreach (Line pLn in PrdLIne)
                    {

                        // To Check Discount
                        if (pLn.DetailType == Intuit.Ipp.Data.LineDetailTypeEnum.DiscountLineDetail)
                        {
                            DiscountLineDetail _DiscountLineDetail = null;

                            Discount = pLn.Amount.ToString();
                            DiscountRate = pLn.Amount.ToString();
                            if (pLn.AnyIntuitObject.GetType() == typeof(DiscountLineDetail))
                            {
                                _DiscountLineDetail = (DiscountLineDetail)pLn.AnyIntuitObject;
                            }
                            if (_DiscountLineDetail != null)
                            {
                                if (_DiscountLineDetail.PercentBased)
                                {
                                    discountOpt = "1";
                                    DiscountRate = _DiscountLineDetail.DiscountPercent.ToString();

                                }
                            }

                        }

                        string TaxSt = "NON";
                        string LineNum = string.Empty;



                        if (pLn.LineNum != null)
                        {
                            LineNum = pLn.LineNum;
                        }

                        try
                        {
                            string pId = string.Empty;
                            string pName = string.Empty;
                            decimal pQty = 0;
                            decimal pRate = 0;
                            string ItemDescription = "";
                            DateTime ServiceDate = DateTime.Now;

                            Intuit.Ipp.Data.SubTotalLineDetail subTotalLineDetail = null;

                            if (pLn.AnyIntuitObject.GetType() == typeof(SubTotalLineDetail))
                            {
                                subTotalLineDetail = (SubTotalLineDetail)pLn.AnyIntuitObject;
                            }
                            if (subTotalLineDetail != null)
                            {

                            }
                            SalesItemLineDetail LineProduct = null;
                            if (pLn.AnyIntuitObject.GetType() == typeof(SalesItemLineDetail))
                            {
                                LineProduct = (SalesItemLineDetail)pLn.AnyIntuitObject;
                            }

                            if (LineProduct != null)
                            {
                                if (LineProduct.TaxCodeRef != null)
                                {
                                    if (LineProduct.TaxCodeRef.Value != null)
                                    {
                                        if (LineProduct.TaxCodeRef.Value == "TAX")
                                        {
                                            TaxSt = "TAX";
                                        }
                                    }
                                }

                                pId = "0"; // GetProductId(LineProduct.ItemRef.Value, CompanyID, ConString);

                                foreach (DataRow dataRow in dt_Items.Rows)
                                {
                                    if (dataRow["QboId"].ToString() == LineProduct.ItemRef.Value)
                                    {
                                        pId = dataRow["id"].ToString();
                                    }
                                }
                                if (LineProduct.ItemRef.name != null)
                                {
                                    pName = LineProduct.ItemRef.name.ToString().Replace("'", string.Empty).Replace(";", string.Empty).Replace("\"", string.Empty);
                                }


                                pQty = LineProduct.Qty;
                                try
                                {

                                    if (pLn.Description != null)
                                    {
                                        ItemDescription = pLn.Description.ToString().Replace(";", string.Empty).Replace("\"", string.Empty);
                                    }

                                    if (ItemDescription.Contains(","))
                                    {
                                        if (ItemDescription.Contains("code") &&
                                            ItemDescription.Contains("qty") &&
                                            ItemDescription.Contains("unit_type"))
                                        {
                                            string Item_Des = string.Empty;
                                            var reg = new Regex("\"([^\"]*)\"");
                                            var matches = reg.Matches(pLn.Description);
                                            foreach (var item in matches)
                                            {
                                                Item_Des += item.ToString();
                                            }

                                            ItemDescription = Item_Des.ToString().Replace(";", string.Empty).Replace("\"", string.Empty);
                                        }

                                    }

                                }
                                catch { }



                                try
                                {
                                    if (LineProduct.ServiceDate != null)
                                    {
                                        if (LineProduct.ServiceDate != DateTime.MinValue)
                                        {
                                            ServiceDate = LineProduct.ServiceDate;
                                        }
                                    }


                                }
                                catch { }


                                try
                                {
                                    if (LineProduct.ItemElementName == ItemChoiceType.UnitPrice)
                                    {
                                        pRate = (decimal)LineProduct.AnyIntuitObject;
                                    }


                                    pRate = (decimal)LineProduct.AnyIntuitObject;
                                }
                                catch { }

                                if (pRate == 0)
                                {
                                    if (pLn.Amount > 0)
                                    {
                                        if (LineProduct.Qty > 0)
                                        {
                                            pRate = pLn.Amount / LineProduct.Qty;
                                        }
                                        else
                                        {
                                            pRate = pLn.Amount;
                                        }

                                    }
                                }

                                if (pQty == 0)
                                {
                                    pQty = 1;
                                }
                                if (!dbJobs)

                                    itemInsertSql += @" insert into tbl_InvoiceDetails (LineNum,CompanyID,ServiceDate,Description,RefId, ItemId,ItemName, Quantity, uPrice , TotalPrice,  IsTaxable, ItemTyId,CreatedDate, CreatedBy) values
                                       ('" + LineNum + "','" + CompanyID + "','" + ServiceDate.ToString("yyyy-MM-dd") + "','" + ItemDescription.ToString().Replace("'", string.Empty) + "','" + InvoiceID + "', '" + pId + "', '" + pName.Replace("'", string.Empty) + "', '" + Convert.ToDecimal(pQty) + "', '" + Convert.ToDecimal(pRate) + "' , '" + pLn.Amount + "','"
                                            + TaxSt.Replace("'", string.Empty) + "', '" + "" + "', '" + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + "QBO" + "') ;";
                                else
                                {
                                    TaxSt = "0";
                                    itemInsertSql += @" insert into InvoiceItems (InvoiceNumber,ItemId, Quantity, UnitPrice, ItemName , ItemDescription,  IsTaxable, CreatedDate,ModifiedDate) values
                                        ('" + listItems[i].DocNumber + "', '" + pId + "', '" + pQty + "', '" + Convert.ToDecimal(pRate) + "' , '" + pName + "', '" + ItemDescription.Replace("'", "") + "'," + TaxSt + "," + @"
                                        SYSDATETIMEOFFSET(),SYSDATETIMEOFFSET() ) ; ";
                                }
                            }

                        }
                        catch (Exception ex) { }

                    }
                    decimal Subtotal = listItems[i].TotalAmt;
                    decimal TotalTax = 0;



                    try
                    {
                        if (listItems[i].TxnTaxDetail != null)
                        {
                            if (listItems[i].TxnTaxDetail.TotalTax != null)
                            {
                                TotalTax = listItems[i].TxnTaxDetail.TotalTax;
                            }
                        }
                        Subtotal = listItems[i].TotalAmt - (TotalTax - listItems[i].DiscountAmt);
                    }
                    catch { }

                    if (!dbJobs)
                    {


                        if (!isExist)
                        {

                            //if (listItems[i].status != EntityStatusEnum.Deleted)
                            //{
                            //getNextNumber(CompanyID);
                            totalRecordToInsert += 1;
                            //Sql += @" IF NOT  EXISTS (SELECT QboEstimateId FROM tbl_Invoice WHERE QboId = '" + listItems[i].Id + "' and CompnyID = '" + CompanyID + "' and CustomerId='" + cID + "') " +
                            //"BEGIN ";
                            //Sql += Environment.NewLine;

                            Sql += Environment.NewLine;
                            Sql += @" Insert into tbl_Invoice (id,SyncToken, CompnyID , TaxType, Number, CustomerId,
                                    Note, UserId, Subtotal,DiscountOption,DiscountRate,
                                    Discount, Tax, Total, Status, InvoiceDate,
                                    InvoiceType, CreatedDate, CreatedBy,AmountCollect, Type ,
                                    AppointmentId,QboId,QboClassId,QboLocationId) values 
                                    ('" + InvoiceID + "','" + listItems[i].SyncToken.Trim() + "','" + CompanyID + "' , '" + TaxTp + "', '" + listItems[i].DocNumber + "', '" + cID + "', '" +
                                    _Note.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "', '" +
                                    HttpContext.Current.Session["LoginUser"].ToString() + "', '"
                                   + Subtotal + "','" + discountOpt + "', '" + DiscountRate + "','" +
                                   Discount + "', '" + TaxAmount + "', '" + listItems[i].TotalAmt + "', '" + "1" + "', '" +
                                   listItems[i].TxnDate.ToString("MM/dd/yyyy") + "', '" + "" + "', '"
                                   + InvocieCraetedDate + "', 'QBO' ," + _Paid + ",'Invoice',''," + listItems[i].Id + "," +
                                   (string.IsNullOrEmpty(qboClassId) || qboClassId == "0" ? "NULL" : qboClassId) + "," +
                                   (string.IsNullOrEmpty(qboLocationId) || qboLocationId == "0" ? "NULL" : qboLocationId) + ") ;";
                            Sql += Environment.NewLine;
                            Sql += itemInsertSql;

                            Sql += Environment.NewLine;


                        }
                        else
                        {
                            //if (listItems[i].status == EntityStatusEnum.Deleted)
                            //{
                            //    Sql += "delete from tbl_Invoice where id='" + InvoiceID + "' and CompnyID='" + CompanyID + "';";
                            //    Sql += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";
                            //}

                            if (SyncToken != listItems[i].SyncToken.Trim() && SyncToken != "")
                            {


                                //if(listItems[i].status == EntityStatusEnum.Deleted)
                                //{
                                //    Sql += "delete from tbl_Invoice where id='" + InvoiceID + "' and CompnyID='" + CompanyID + "';";
                                //    Sql += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";
                                //}
                                //else
                                //{

                                // Update Aire Master Payment

                                if ((bool)HttpContext.Current.Session["IsAireMaster"])
                                {
                                    if (Convert.ToDecimal(_Paid) > AmountCollect)
                                    {
                                        string depositAmount = (Convert.ToDecimal(_Paid) - AmountCollect).ToString("0.00");

                                    }
                                }

                                string strTemp = " update tbl_Invoice set TaxType='" + TaxTp + "'";
                                strTemp += " , CustomerId='" + cID + "', Note='" + _Note.Replace(";", string.Empty).Replace("\"", string.Empty).Replace("'", string.Empty) + "',Subtotal='" + listItems[i].TotalAmt + "'";
                                strTemp += " , DiscountOption='" + discountOpt + "', DiscountRate='" + DiscountRate + "',Tax='" + TaxAmount + "'";
                                strTemp += " , Total='" + listItems[i].TotalAmt + "' ";
                                strTemp += " , AmountCollect='" + _Paid + "' ";
                                strTemp += " , Discount='" + Discount + "' ";
                                strTemp += " , Number='" + listItems[i].DocNumber.Trim() + "' ";
                                strTemp += " , Type='Invoice' ";
                                strTemp += " , SyncToken='" + listItems[i].SyncToken.Trim() + "' ";
                                strTemp += " , QboClassId=" + (string.IsNullOrEmpty(qboClassId) || qboClassId == "0" ? "NULL" : qboClassId);
                                strTemp += " , QboLocationId=" + (string.IsNullOrEmpty(qboLocationId) || qboLocationId == "0" ? "NULL" : qboLocationId);
                                strTemp += " where [QboId] ='" + listItems[i].Id + "' and CompnyID='" + CompanyID + "';";
                                Sql += strTemp;
                                Sql += "delete from tbl_InvoiceDetails where RefId='" + InvoiceID + "' and companyid='" + CompanyID + "';";

                                Sql += itemInsertSql;



                                //  }

                            }

                        }


                        if ((bool)HttpContext.Current.Session["IsAireMaster"])
                        {
                            ProcessPayment(CompanyID, InvoiceID, listItems[i].Id, Total_listPayment);
                        }
                    }
                    else
                    {
                        DateTime InvoiceDate = new DateTime(listItems[i].TxnDate.Year,
                                               listItems[i].TxnDate.Month,
                                                listItems[i].TxnDate.Day,
                                                23, 59, 59, 999);
                        string InvoiceUID = Guid.NewGuid().ToString("n").Substring(0, 8);



                        if (!isExist)
                        {
                            Sql += @"Insert into Invoices (InvoiceUID,CompanyId , Number, CustomerId, UserId, Subtotal, Discount, Tax, Total, Status, InvoiceDate,ModifiedDate, InvoiceType, CreatedDate) values 
                            ('" + InvoiceUID + "','" + CompanyID + "' , '" + listItems[i].DocNumber + "', '" + cID + "', " + GetJobsUsertId(HttpContext.Current.Session["LoginUser"].ToString()) + ", '"
                          + listItems[i].TotalAmt + "', '" + listItems[i].DiscountAmt.ToString() + "', '" + TaxAmount + "', '" + listItems[i].TotalAmt + "', '" + "0" + "', '" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + InvoiceDate.ToString("yyyy-MM-dd h:mm:ss tt") + "', '" + "" + "',SYSDATETIMEOFFSET())";
                            Sql += itemInsertSql;
                        }
                        else
                        {




                            if (SyncToken != listItems[i].SyncToken.Trim() && SyncToken != "")
                            {


                                string strTemp = " update Invoices set ";
                                strTemp += " CustomerId='" + cID + "', Subtotal='" + listItems[i].TotalAmt + "'";
                                strTemp += " ,Discount='" + listItems[i].DiscountAmt.ToString() + "',Tax='" + TaxAmount + "'";
                                strTemp += " , Total='" + listItems[i].TotalAmt + "' ";
                                strTemp += " , Number='" + listItems[i].DocNumber.Trim() + "' ";
                                if (Convert.ToDecimal(_Paid) >= listItems[i].TotalAmt)
                                {
                                    strTemp += " , Status='1' ";
                                }
                                strTemp += " where [QboId] ='" + listItems[i].Id + "' and CompnyID='" + CompanyID + "';";
                                // strTemp += " where Number='" + listItems[i].DocNumber + "' and CompanyId='" + CompanyID + "';";
                                Sql += strTemp;
                                Sql += "delete from InvoiceItems where InvoiceNumber='" + listItems[i].DocNumber + "';";
                                Sql += itemInsertSql;

                                if ((bool)HttpContext.Current.Session["IsAireMaster"])
                                {
                                    // ProcessPayment(CompanyID, InvoiceID, listItems[i].Id,Total_listPayment);
                                }

                            }

                        }


                    }
                    //}
                    //}
                }

                if (Sql != "")
                {
                    if (totalRecordToInsert > 0)
                    {
                        //UpdateInvoceiSeedNumber(CompanyID, totalRecordToInsert);
                    }


                    string Invoice = " begin " + CleanInput(Sql) + " return end;";
                    sb = Environment.NewLine + Invoice;
                    db.Execute(Invoice);
                }
                db.Close();
            }
            catch { }
        }
        public void ProcessPayment(string CompanyID, String InvocieID, string QBO_ID, List<Intuit.Ipp.Data.Payment> Total_listPayment)
        {

            Database _DB = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());

            try
            {



                foreach (Payment iPnt in Total_listPayment)
                {
                    foreach (Line ln in iPnt.Line)
                    {
                        foreach (LinkedTxn txln in ln.LinkedTxn)
                        {
                            if (txln.TxnId == QBO_ID)
                            {
                                string PaymentRefNum = "";
                                if (!string.IsNullOrEmpty(iPnt.PaymentRefNum))
                                {
                                    PaymentRefNum = iPnt.PaymentRefNum;
                                }
                                string SQL = "select *  from tbl_Payment where  CompanyID='" + CompanyID + "' and QboId='" + iPnt.Id +
                                    "'	and InvocieId='" + InvocieID + "' and  PaymentRefNum ='" + PaymentRefNum + "' ;";



                                DataTable dtJobs = new DataTable();
                                _DB.Execute(SQL, out dtJobs);

                                if (dtJobs.Rows.Count == 0)
                                {
                                    string query = @"INSERT INTO [dbo].[tbl_Payment]
                                       ([Companyid]
                                       ,[InvocieId]
                                       ,[Amount]
                                       ,[Type]
                                       ,[Source]
                                       ,CheckName
                                        ,IsDeposit
                                       ,CheckNumber
                                        ,PaymentRefNum
                                       ,[QboId])
                                 VALUES
                                       ('" + CompanyID +
                                            "','" + InvocieID +
                                            "','" + ln.Amount +
                                            "','" + "QBO" +
                                             "','" + "QBO" +
                                            "','" + "" +
                                              "','" + "0" +
                                             "','" + "" +
                                            "','" + PaymentRefNum + "'," + iPnt.Id + ");";
                                    _DB.Execute(query);
                                    string queryForRMCompany = "SELECT AM_CompanyID FROM [msSchedulerV3].[dbo].tbl_AMCompanyMapping " +
                                           " Where CompanyID ='" + CompanyID + "'";
                                    DataTable dt = new DataTable();
                                    _DB.Execute(queryForRMCompany, out dt);
                                    string RmCompanyId = dt.Rows[0]["AM_CompanyID"].ToString();
                                    if (dt.Rows.Count > 0)
                                    {
                                        
                                    }


                                }




                            }

                        }
                    }
                }
            }
            catch (Exception ex) { }

        }


        private string GetJobsUsertId(string _Id)
        {
            try
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
            catch { return ""; }
        }
        private string GetPaidAmount(string InvoiceId, List<Payment> listPayment)
        {
            double dlAmount = 0;
            try
            {
                foreach (Payment iPnt in listPayment)
                {
                    foreach (Line ln in iPnt.Line)
                    {
                        foreach (LinkedTxn txln in ln.LinkedTxn)
                        {
                            if (txln.TxnId == InvoiceId)
                                dlAmount += Convert.ToDouble(ln.Amount);
                        }
                    }
                }
            }
            catch { return "0"; }
            return dlAmount.ToString();
        }
        public string GetProductId(string _Id, string _CompanyID, string ConString)
        {
            try
            {
                Database db = new Database(ConString);
                //  db.Open();
                string Sql = "select Id from Items where QboId='" + _Id + "' and CompanyID='" + _CompanyID + "'";
                DataTable dt;
                db.Execute(Sql, out dt);
                db.Close();
                DataRow dr = dt.Rows[0];
                return dt.Rows[0]["Id"].ToString();
            }
            catch { return ""; }
        }
        public string GetCustomerId(string _Id, string _CompanyID, string ConString, bool dbJobs)
        {
            try
            {
                Database db = new Database(ConString);
                //  db.Open();
                string Sql = "select CustomerID from tbl_Customer where QboId='" + _Id + "' and CompanyID='" + _CompanyID + "'";
                if (dbJobs)
                    Sql = "select Id from Customers where QboId='" + _Id + "' and CompanyID='" + _CompanyID + "'";
                DataTable dt;
                db.Execute(Sql, out dt);
                db.Close();

                DataRow dr = dt.Rows[0];
                return dt.Rows[0][0].ToString();
            }
            catch { return ""; }
        }


        // Tax Synchronization
        public bool TaxSynchronization(QBOSettins qboSettings,
           string CompanyID,
           ServiceContext context,
           string QboLastUpdatedTime)
        {
            bool saveStat = false;
            try
            {
                string CompId = HttpContext.Current.Session["CompanyID"].ToString();
                Database db = new Database(ConfigurationManager.AppSettings["ConnStrSch"].ToString());
                //  db.Open();




                string qboQuery = "select * from TaxCode  Where MetaData.LastUpdatedTime >= '" + QboLastUpdatedTime + "' STARTPOSITION 1 MAXRESULTS 1000";

                QueryService<Intuit.Ipp.Data.TaxCode> qsItem = new QueryService<Intuit.Ipp.Data.TaxCode>(context);
                List<Intuit.Ipp.Data.TaxCode> listTaxCode = qsItem.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.TaxCode>();
                List<Intuit.Ipp.Data.TaxRate> listTaxRate = new List<Intuit.Ipp.Data.TaxRate>();


                string Sql = "Select QboId,ID from Taxes where CompanyId='" + CompId + "'";
                DataTable dt = new DataTable(); ;



                bool isExist = false;


                if (listTaxCode.Count > 0)
                {
                    string qboQueryRate = "select * from TaxRate STARTPOSITION 1 MAXRESULTS 1000";
                    QueryService<Intuit.Ipp.Data.TaxRate> qsItemRate = new QueryService<Intuit.Ipp.Data.TaxRate>(context);
                    listTaxRate = qsItemRate.ExecuteIdsQuery(qboQueryRate).ToList<Intuit.Ipp.Data.TaxRate>();


                    db.Execute(Sql, out dt);
                }
                Sql = string.Empty;
                foreach (Intuit.Ipp.Data.TaxCode ilst in listTaxCode)
                {
                    if (ilst.SalesTaxRateList != null && ilst.Active == true)
                    {


                        isExist = false;
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (ilst.Id == dr["QboId"].ToString().Trim())
                            {
                                isExist = true;
                            }
                        }


                        decimal txRate = 0;

                        foreach (Intuit.Ipp.Data.TaxRateDetail SLR in ilst.SalesTaxRateList.TaxRateDetail)
                        {
                            foreach (Intuit.Ipp.Data.TaxRate ilstRate in listTaxRate)
                            {
                                if (SLR.TaxRateRef.Value == ilstRate.Id && ilstRate.Active == true)
                                {
                                    txRate += ilstRate.RateValue;
                                }
                            }
                        }
                        if (!isExist)
                        {
                            Sql += "INSERT INTO Taxes (Name,Rate,CompanyId,QboId,IsDeleted,CreatedDate,CreatedBy)" +
                                                           " VALUES  ('" + ilst.Name + "'," + txRate.ToString() + ",'" +
                                                          CompId + "'," + ilst.Id + ",0,GETDATE(),'" + HttpContext.Current.Session["LoginUser"].ToString() + "'); ";


                        }

                    }

                }
                if (!string.IsNullOrEmpty(Sql))
                {
                    db.Execute(Sql);
                }
                return true;
            }
            catch (Intuit.Ipp.Exception.IdsException ex)
            {
                string errDetail = "";
                var innerException = ((Intuit.Ipp.Exception.ValidationException)(ex.InnerException)).InnerExceptions.FirstOrDefault();
                if (innerException != null)
                {
                    errDetail = innerException.Detail;
                    //throw new ApplicationException(innerException.Detail);

                }
                return false;
            }
        }

        public List<Intuit.Ipp.Data.Class> GetQboClasses(ServiceContext serviceContext, string CompanyId)
        {
            List<Intuit.Ipp.Data.Class> classes = new List<Intuit.Ipp.Data.Class>();
            try
            {
                string qboQuery = "select * from Class STARTPOSITION 1 MAXRESULTS 1000 ";
                QueryService<Intuit.Ipp.Data.Class> querySvc = new QueryService<Intuit.Ipp.Data.Class>(serviceContext);
                classes = querySvc.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Class>();
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["ConnStrSch"].ToString()))
                {
                    conn.Open();
                    foreach (var cls in classes)
                    {
                        if (cls == null || string.IsNullOrEmpty(cls.Name)) continue;

                        string checkQuery = "SELECT COUNT(1) FROM [msSchedulerV3].[dbo].Tbl_QboClass WHERE QboClassId = @QboClassId AND CompanyId = @CompanyId";

                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@QboClassId", Convert.ToInt32(cls.Id));
                            checkCmd.Parameters.AddWithValue("@CompanyId", CompanyId);

                            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (exists == 0)
                            {

                                string insertQuery = @"INSERT INTO [msSchedulerV3].[dbo].Tbl_QboClass (Name, QboClassId, CompanyId)
                                           VALUES (@Name, @QboClassId, @CompanyId)";
                                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                                {
                                    insertCmd.Parameters.AddWithValue("@Name", cls.Name ?? (object)DBNull.Value);
                                    insertCmd.Parameters.AddWithValue("@QboClassId", Convert.ToInt32(cls.Id));
                                    insertCmd.Parameters.AddWithValue("@CompanyId", CompanyId);

                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {

            }

            return classes;
        }
        public List<Intuit.Ipp.Data.Department> GetQboLocations(ServiceContext serviceContext, string companyId)
        {
            List<Intuit.Ipp.Data.Department> locations = new List<Intuit.Ipp.Data.Department>();
            try
            {
                string qboQuery = "select * from Department STARTPOSITION 1 MAXRESULTS 1000 ";
                QueryService<Intuit.Ipp.Data.Department> querySvc = new QueryService<Intuit.Ipp.Data.Department>(serviceContext);
                locations = querySvc.ExecuteIdsQuery(qboQuery).ToList<Intuit.Ipp.Data.Department>();
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["ConnStrSch"].ToString()))
                {
                    conn.Open();

                    foreach (var loc in locations)
                    {
                        if (loc == null || string.IsNullOrEmpty(loc.Name)) continue;


                        string checkQuery = "SELECT COUNT(1) FROM [msSchedulerV3].[dbo].Tbl_QboLocation WHERE QboLocationID = @QboLocationID AND CompanyId = @CompanyId";

                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@QboLocationID", Convert.ToInt32(loc.Id));
                            checkCmd.Parameters.AddWithValue("@CompanyId", companyId);

                            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (exists == 0)
                            {

                                string insertQuery = @"INSERT INTO [msSchedulerV3].[dbo].Tbl_QboLocation (Name, QboLocationID, CompanyId)
                                           VALUES (@Name, @QboLocationID, @CompanyId)";
                                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                                {
                                    insertCmd.Parameters.AddWithValue("@Name", loc.Name ?? (object)DBNull.Value);
                                    insertCmd.Parameters.AddWithValue("@QboLocationID", Convert.ToInt32(loc.Id));
                                    insertCmd.Parameters.AddWithValue("@CompanyId", companyId);

                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {

            }

            return locations;
        }

        /// <summary>
        /// QBO entity ids are positive numeric strings. Failed sync markers (-11, -1, 0) mean "create new".
        /// </summary>
        private static string NormalizeQboEntityId(string qboId)
        {
            qboId = (qboId ?? "").Trim();
            if (string.IsNullOrEmpty(qboId) || qboId == "0" || qboId == "-1" || qboId == "-11")
                return "0";
            return long.TryParse(qboId, out long parsed) && parsed > 0 ? qboId : "0";
        }

        private static bool QboDisplayNameExists(string displayName, List<Intuit.Ipp.Data.Customer> existing)
        {
            string normalized = NormalizeCustomerName(displayName);
            if (string.IsNullOrEmpty(normalized))
                return false;
            return existing.Any(c =>
                string.Equals(NormalizeCustomerName(c.DisplayName), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeCustomerName(c.CompanyName), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildUniqueQboDisplayName(string baseName, string email, List<Intuit.Ipp.Data.Customer> existing)
        {
            string candidate = NormalizeCustomerName(baseName);
            if (string.IsNullOrEmpty(candidate))
                candidate = "Customer";

            if (!QboDisplayNameExists(candidate, existing))
                return candidate;

            if (!string.IsNullOrEmpty(email))
            {
                string withEmail = candidate + " (" + email + ")";
                if (!QboDisplayNameExists(withEmail, existing))
                    return withEmail;
            }

            for (int i = 2; i < 100; i++)
            {
                string numbered = candidate + " (" + i + ")";
                if (!QboDisplayNameExists(numbered, existing))
                    return numbered;
            }

            return candidate + " (" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
        }

        private static bool TryLinkCustomerToQboCustomer(string companyId, string customerId, string qboCustomerId, string displayName, Database db)
        {
            if (IsQboIdLinkedToOtherCustomer(companyId, customerId, qboCustomerId))
                return false;

            string sql = "update tbl_Customer set QboId=" + qboCustomerId + " Where CompanyID='" + companyId + "' and CustomerID = '" + customerId + "'";
            db.Execute(sql);
            return true;
        }

        public static string GetEstimateQboSyncErrorMessage(string resultId)
        {
            resultId = (resultId ?? "").Trim();
            if (resultId == "-2")
                return "QuickBooks sync failed: could not resolve the customer in QuickBooks. If another customer has the same name, ensure this customer has a unique email or link them manually.";
            if (resultId == "-11")
                return "QuickBooks estimate sync failed. Please verify line items.";
            if (resultId == "0" || resultId == "-1")
                return "QuickBooks estimate sync failed. Please verify customer and line items.";
            return null;
        }

        /// <summary>Ensures one CEC customer is linked or created in QBO before invoice/estimate sync.</summary>
        public bool EnsureCustomerInQbo(ServiceContext context, QBOSettins qboSettings, string companyId, string customerId)
        {
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(customerId))
                return false;

            ServiceContext serviceContext = context ?? GetServiceContext(qboSettings, companyId);
            string qboId = "0";
            string qboName = "";
            CustomerCheckTEST(serviceContext, companyId, customerId, ref qboId, ref qboName);
            return !string.IsNullOrEmpty(qboId) && qboId != "0" && qboId != "-11";
        }
        private static bool IsQboIdLinkedToOtherCustomer(string companyId, string customerId, string qboId)
        {
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(qboId) || qboId == "0")
                return false;

            string sql = @"SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_Customer]
                           WHERE CompanyID = @CompanyId AND CAST(QboId AS varchar(50)) = @QboId AND CustomerID <> @CustomerId";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.AppSettings["ConnStrSch"].ToString()))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.Parameters.AddWithValue("@QboId", qboId);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }
        private static string NormalizeCustomerName(string name)
        {
            return (name ?? "").Replace("  ", " ").Trim();
        }
        public int VendorSynchronization(QBOSettins qboSettings, string CompanyID, ServiceContext context)
        {
            if (context == null)
                throw new Exception("QBO is not connected. Please connect to QuickBooks first.");

            int totalSynced = 0;
            int startPos = 1;
            const int maxResults = 500;

            string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();

            for (int page = 0; page < 10000; page++)
            {
                string qboQuery = "SELECT * FROM Vendor STARTPOSITION " + startPos + " MAXRESULTS " + maxResults;
                QueryService<Intuit.Ipp.Data.Vendor> qs = new QueryService<Intuit.Ipp.Data.Vendor>(context);
                List<Intuit.Ipp.Data.Vendor> vendors = qs.ExecuteIdsQuery(qboQuery).ToList();

                if (vendors.Count == 0) break;

                UpsertVendorsToLocal(CompanyID, connStr, vendors);
                totalSynced += vendors.Count;
                startPos += maxResults;

                if (vendors.Count < maxResults) break;
            }

            int pushed = PushLocalVendorsWithPOsToQBO(context, CompanyID, connStr);
            totalSynced += pushed;

            return totalSynced;
        }
        private void UpsertVendorsToLocal(string CompanyID, string connStr, List<Intuit.Ipp.Data.Vendor> vendors)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                DataTable existing;
                using (var cmd = new SqlCommand(
                    "SELECT VendorID, QboVendorID FROM [msSchedulerV3].[dbo].[tbl_Vendor] WHERE CompanyID=@cid", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", CompanyID);
                    var da = new SqlDataAdapter(cmd);
                    existing = new DataTable();
                    da.Fill(existing);
                }

                var qboIdMap = new Dictionary<string, int>();
                foreach (DataRow dr in existing.Rows)
                {
                    string qid = dr["QboVendorID"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(qid) && qid != "0")
                    {
                        int vid = Convert.ToInt32(dr["VendorID"]);
                        if (!qboIdMap.ContainsKey(qid))
                            qboIdMap[qid] = vid;
                    }
                }

                foreach (var v in vendors)
                {
                    string qboId = v.Id?.Trim() ?? "";
                    if (string.IsNullOrEmpty(qboId)) continue;

                    string displayName = (v.DisplayName ?? v.CompanyName ?? "").Trim().Replace("'", "''");
                    if (string.IsNullOrEmpty(displayName))
                        displayName = ((v.GivenName ?? "") + " " + (v.FamilyName ?? "")).Trim().Replace("'", "''");

                    string contactName = "";
                    if (!string.IsNullOrEmpty(v.GivenName) || !string.IsNullOrEmpty(v.FamilyName))
                        contactName = ((v.GivenName ?? "") + " " + (v.FamilyName ?? "")).Trim().Replace("'", "''");

                    string email = "";
                    if (v.PrimaryEmailAddr != null)
                        email = (v.PrimaryEmailAddr.Address ?? "").Trim().Replace("'", "''");

                    string phone = "";
                    if (v.PrimaryPhone != null)
                        phone = (v.PrimaryPhone.FreeFormNumber ?? "").Trim().Replace("'", "''");

                    string website = "";
                    if (v.WebAddr != null)
                        website = (v.WebAddr.URI ?? "").Trim().Replace("'", "''");

                    string addr = "", city = "", state = "", zip = "";
                    var billAddr = v.BillAddr;
                    if (billAddr != null)
                    {
                        addr = (billAddr.Line1 ?? "").Trim().Replace("'", "''");
                        city = (billAddr.City ?? "").Trim().Replace("'", "''");
                        state = (billAddr.CountrySubDivisionCode ?? "").Trim().Replace("'", "''");
                        zip = (billAddr.PostalCode ?? "").Trim().Replace("'", "''");
                    }

                    string terms = "";
                    if (v.TermRef != null)
                        terms = (v.TermRef.name ?? "").Trim().Replace("'", "''");

                    string taxId = (v.TaxIdentifier ?? "").Trim().Replace("'", "''");
                    string notes = (v.Notes ?? "").Trim().Replace("'", "''");
                    string dba = (v.CompanyName ?? "").Trim().Replace("'", "''");
                    string currency = "";
                    if (v.CurrencyRef != null)
                        currency = (v.CurrencyRef.Value ?? "").Trim().Replace("'", "''");

                    bool isActive = v.Active;

                    if (qboIdMap.ContainsKey(qboId))
                    {
                        int localId = qboIdMap[qboId];
                        string updateSql = @"
                            UPDATE [msSchedulerV3].[dbo].[tbl_Vendor] SET
                                Name=@Name, DBA=@DBA, TaxID=@TaxID, Website=@Website,
                                PaymentTerms=@Terms, Currency=@Currency,
                                ContactName=@Contact, Email=@Email, Phone=@Phone,
                                Address=@Addr, City=@City, State=@State, Zip=@Zip,
                                Notes=@Notes, IsActive=@Active, ModifiedDate=GETDATE()
                            WHERE VendorID=@VID AND CompanyID=@CID";
                        using (var cmd = new SqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", displayName);
                            cmd.Parameters.AddWithValue("@DBA", dba);
                            cmd.Parameters.AddWithValue("@TaxID", taxId);
                            cmd.Parameters.AddWithValue("@Website", website);
                            cmd.Parameters.AddWithValue("@Terms", terms);
                            cmd.Parameters.AddWithValue("@Currency", currency);
                            cmd.Parameters.AddWithValue("@Contact", contactName);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@Phone", phone);
                            cmd.Parameters.AddWithValue("@Addr", addr);
                            cmd.Parameters.AddWithValue("@City", city);
                            cmd.Parameters.AddWithValue("@State", state);
                            cmd.Parameters.AddWithValue("@Zip", zip);
                            cmd.Parameters.AddWithValue("@Notes", notes);
                            cmd.Parameters.AddWithValue("@Active", isActive ? 1 : 0);
                            cmd.Parameters.AddWithValue("@VID", localId);
                            cmd.Parameters.AddWithValue("@CID", CompanyID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertSql = @"
                            INSERT INTO [msSchedulerV3].[dbo].[tbl_Vendor]
                                (CompanyID, Name, DBA, TaxID, Website, Category, PaymentTerms, PreferredPayment,
                                 Currency, ContactName, ContactTitle, Email, Phone, Address, City, State, Zip,
                                 Notes, IsActive, QboVendorID, CreatedDate)
                            VALUES
                                (@CID, @Name, @DBA, @TaxID, @Website, '', @Terms, '',
                                 @Currency, @Contact, '', @Email, @Phone, @Addr, @City, @State, @Zip,
                                 @Notes, @Active, @QboId, GETDATE());
                            SELECT SCOPE_IDENTITY();";
                        using (var cmd = new SqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@CID", CompanyID);
                            cmd.Parameters.AddWithValue("@Name", displayName);
                            cmd.Parameters.AddWithValue("@DBA", dba);
                            cmd.Parameters.AddWithValue("@TaxID", taxId);
                            cmd.Parameters.AddWithValue("@Website", website);
                            cmd.Parameters.AddWithValue("@Terms", terms);
                            cmd.Parameters.AddWithValue("@Currency", currency);
                            cmd.Parameters.AddWithValue("@Contact", contactName);
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@Phone", phone);
                            cmd.Parameters.AddWithValue("@Addr", addr);
                            cmd.Parameters.AddWithValue("@City", city);
                            cmd.Parameters.AddWithValue("@State", state);
                            cmd.Parameters.AddWithValue("@Zip", zip);
                            cmd.Parameters.AddWithValue("@Notes", notes);
                            cmd.Parameters.AddWithValue("@Active", isActive ? 1 : 0);
                            cmd.Parameters.AddWithValue("@QboId", qboId);
                            cmd.ExecuteNonQuery();
                        }

                        qboIdMap[qboId] = -1;
                    }
                }
            }
        }
        public string SyncPurchaseOrderToQBO(ServiceContext context, string companyId, int poId)
        {
            if (context == null)
                throw new Exception("QBO is not connected.");

            string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
            string qboVendorId = null;
            string existingQboPOID = null;
            string poNumber = "", memo = "", localStatus = "";
            int vendorLocalId = 0;
            DateTime poDate = DateTime.Today;
            decimal subTotal = 0, tax = 0, freight = 0;

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT po.PONumber, po.PODate, po.SubTotal, po.Tax, po.Freight, po.Notes, po.QboPOID,
                           po.VendorID, po.Status,
                           v.QboVendorID, v.Name, v.ContactName, v.Email, v.Phone,
                           v.Address, v.City, v.State, v.Zip, v.Website, v.DBA
                    FROM [msSchedulerV3].[dbo].[tbl_PurchaseOrder] po
                    LEFT JOIN [msSchedulerV3].[dbo].[tbl_Vendor] v 
                        ON v.VendorID = po.VendorID AND v.CompanyID = po.CompanyID
                    WHERE po.POID = @pid AND po.CompanyID = @cid", conn))
                {
                    cmd.Parameters.AddWithValue("@pid", poId);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            throw new Exception("PO not found.");
                        poNumber = dr["PONumber"]?.ToString() ?? "";
                        qboVendorId = dr["QboVendorID"]?.ToString();
                        existingQboPOID = dr["QboPOID"]?.ToString();
                        memo = dr["Notes"]?.ToString() ?? "";
                        vendorLocalId = dr["VendorID"] != DBNull.Value ? Convert.ToInt32(dr["VendorID"]) : 0;
                        if (dr["PODate"] != DBNull.Value) poDate = Convert.ToDateTime(dr["PODate"]);
                        if (dr["SubTotal"] != DBNull.Value) subTotal = Convert.ToDecimal(dr["SubTotal"]);
                        if (dr["Tax"] != DBNull.Value) tax = Convert.ToDecimal(dr["Tax"]);
                        if (dr["Freight"] != DBNull.Value) freight = Convert.ToDecimal(dr["Freight"]);
                        localStatus = dr["Status"]?.ToString() ?? "";

                        if (string.IsNullOrEmpty(qboVendorId) || qboVendorId == "0" || qboVendorId == "-11")
                        {
                            string vendorName = dr["Name"]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(vendorName))
                                throw new Exception("Vendor has no name. Cannot create in QuickBooks.");

                            var qboV = new Intuit.Ipp.Data.Vendor();
                            qboV.DisplayName = vendorName;
                            string dba = dr["DBA"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(dba)) qboV.CompanyName = dba;

                            string contact = dr["ContactName"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(contact))
                            {
                                string[] np = contact.Split(new[] { ' ' }, 2);
                                qboV.GivenName = np[0];
                                if (np.Length > 1) qboV.FamilyName = np[1];
                            }
                            string vEmail = dr["Email"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(vEmail))
                                qboV.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress { Address = vEmail };
                            string vPhone = dr["Phone"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(vPhone))
                                qboV.PrimaryPhone = new TelephoneNumber { FreeFormNumber = vPhone };
                            string vWeb = dr["Website"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(vWeb))
                                qboV.WebAddr = new WebSiteAddress { URI = vWeb };

                            var vAddr = new PhysicalAddress();
                            vAddr.Line1 = dr["Address"]?.ToString()?.Trim() ?? "";
                            vAddr.City = dr["City"]?.ToString()?.Trim() ?? "";
                            vAddr.CountrySubDivisionCode = dr["State"]?.ToString()?.Trim() ?? "";
                            vAddr.PostalCode = dr["Zip"]?.ToString()?.Trim() ?? "";
                            qboV.BillAddr = vAddr;
                            qboV.Active = true;

                            DataService vendorDs = new DataService(context);
                            var addedV = vendorDs.Add(qboV);
                            qboVendorId = addedV.Id;
                        }
                    }
                }

                if (vendorLocalId > 0 && !string.IsNullOrEmpty(qboVendorId) && qboVendorId != "0")
                {
                    using (var upd = new SqlCommand(@"
                        UPDATE [msSchedulerV3].[dbo].[tbl_Vendor]
                        SET QboVendorID = @qid, ModifiedDate = GETDATE()
                        WHERE VendorID = @vid AND CompanyID = @cid", conn))
                    {
                        upd.Parameters.AddWithValue("@qid", qboVendorId);
                        upd.Parameters.AddWithValue("@vid", vendorLocalId);
                        upd.Parameters.AddWithValue("@cid", companyId);
                        upd.ExecuteNonQuery();
                    }
                }

                DataTable dtLines = new DataTable();
                using (var cmd = new SqlCommand(@"
                    SELECT LineNum, Description, Quantity, UnitPrice, Total
                    FROM [msSchedulerV3].[dbo].[tbl_PurchaseOrderLineItem]
                    WHERE POID = @pid AND CompanyID = @cid
                    ORDER BY LineNum", conn))
                {
                    cmd.Parameters.AddWithValue("@pid", poId);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    var da = new SqlDataAdapter(cmd);
                    da.Fill(dtLines);
                }

                ReferenceType expenseAcctRef = GetDefaultExpenseAccountRef(context);

                Intuit.Ipp.Data.PurchaseOrder qboPO = new Intuit.Ipp.Data.PurchaseOrder();

                qboPO.VendorRef = new ReferenceType { Value = qboVendorId };
                qboPO.DocNumber = poNumber;
                qboPO.TxnDate = poDate;
                qboPO.TxnDateSpecified = true;
                if (!string.IsNullOrEmpty(memo))
                    qboPO.Memo = memo;

                if (localStatus == "Closed" || localStatus == "Received")
                {
                    qboPO.POStatus = PurchaseOrderStatusEnum.Closed;
                    qboPO.POStatusSpecified = true;
                }
                else
                {
                    qboPO.POStatus = PurchaseOrderStatusEnum.Open;
                    qboPO.POStatusSpecified = true;
                }

                var qboLines = new System.Collections.Generic.List<Line>();

                foreach (DataRow lr in dtLines.Rows)
                {
                    decimal qty = lr["Quantity"] != DBNull.Value ? Convert.ToDecimal(lr["Quantity"]) : 1;
                    decimal unitPrice = lr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(lr["UnitPrice"]) : 0;
                    decimal lineTotal = lr["Total"] != DBNull.Value ? Convert.ToDecimal(lr["Total"]) : qty * unitPrice;
                    string desc = lr["Description"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(desc)) desc = "Line " + (lr["LineNum"]?.ToString() ?? "");

                    Line line = new Line();
                    line.DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail;
                    line.DetailTypeSpecified = true;
                    line.Amount = lineTotal;
                    line.AmountSpecified = true;
                    line.Description = desc;

                    var detail = new AccountBasedExpenseLineDetail();
                    detail.AccountRef = expenseAcctRef;
                    line.AnyIntuitObject = detail;
                    qboLines.Add(line);
                }

                if (qboLines.Count == 0)
                    throw new Exception("PO has no line items to sync.");

                qboPO.Line = qboLines.ToArray();

                DataService dataService = new DataService(context);
                string returnedQboId;

                bool isUpdate = !string.IsNullOrEmpty(existingQboPOID)
                                && !existingQboPOID.StartsWith("QBO-PO-");

                if (isUpdate)
                {
                    var qs = new QueryService<Intuit.Ipp.Data.PurchaseOrder>(context);
                    var found = qs.ExecuteIdsQuery(
                        "SELECT * FROM PurchaseOrder WHERE Id = '" + existingQboPOID + "'")
                        .FirstOrDefault();
                    if (found != null)
                    {
                        qboPO.Id = found.Id;
                        qboPO.SyncToken = found.SyncToken;
                        var updated = dataService.Update(qboPO);
                        returnedQboId = updated.Id;
                    }
                    else
                    {
                        var added = dataService.Add(qboPO);
                        returnedQboId = added.Id;
                    }
                }
                else
                {
                    var added = dataService.Add(qboPO);
                    returnedQboId = added.Id;
                }

                using (var upd = new SqlCommand(@"
                    UPDATE [msSchedulerV3].[dbo].[tbl_PurchaseOrder]
                    SET QboPOID = @qid, ModifiedDate = GETDATE()
                    WHERE POID = @pid AND CompanyID = @cid", conn))
                {
                    upd.Parameters.AddWithValue("@qid", returnedQboId);
                    upd.Parameters.AddWithValue("@pid", poId);
                    upd.Parameters.AddWithValue("@cid", companyId);
                    upd.ExecuteNonQuery();
                }

                return returnedQboId;
            }
        }
        private int PushLocalVendorsWithPOsToQBO(ServiceContext context, string CompanyID, string connStr)
        {
            int pushed = 0;
            try
            {
                DataTable dt = new DataTable();
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT v.VendorID, v.Name, v.ContactName, v.Email, v.Phone,
                               v.Address, v.City, v.State, v.Zip, v.Website, v.TaxID, v.DBA
                        FROM [msSchedulerV3].[dbo].[tbl_Vendor] v
                        WHERE v.CompanyID = @cid
                          AND (v.QboVendorID IS NULL OR v.QboVendorID = '' OR v.QboVendorID = '0')
                          AND v.IsActive = 1
                          AND EXISTS (
                              SELECT 1 FROM [msSchedulerV3].[dbo].[tbl_PurchaseOrder] po
                              WHERE po.VendorID = v.VendorID AND po.CompanyID = v.CompanyID
                          )", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", CompanyID);
                        var da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }

                if (dt.Rows.Count == 0) return 0;

                DataService dataService = new DataService(context);

                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    foreach (DataRow dr in dt.Rows)
                    {
                        try
                        {
                            string displayName = dr["Name"].ToString().Trim();
                            if (string.IsNullOrEmpty(displayName)) continue;

                            Intuit.Ipp.Data.Vendor qboVendor = new Intuit.Ipp.Data.Vendor();
                            qboVendor.DisplayName = displayName;

                            string companyName = dr["DBA"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(companyName))
                                qboVendor.CompanyName = companyName;

                            string contact = dr["ContactName"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(contact))
                            {
                                string[] parts = contact.Split(new[] { ' ' }, 2);
                                qboVendor.GivenName = parts[0];
                                if (parts.Length > 1)
                                    qboVendor.FamilyName = parts[1];
                            }

                            string email = dr["Email"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(email))
                                qboVendor.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress { Address = email };

                            string phone = dr["Phone"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(phone))
                                qboVendor.PrimaryPhone = new TelephoneNumber { FreeFormNumber = phone };

                            string website = dr["Website"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(website))
                                qboVendor.WebAddr = new WebSiteAddress { URI = website };

                            PhysicalAddress addr = new PhysicalAddress();
                            addr.Line1 = dr["Address"]?.ToString()?.Trim() ?? "";
                            addr.City = dr["City"]?.ToString()?.Trim() ?? "";
                            addr.CountrySubDivisionCode = dr["State"]?.ToString()?.Trim() ?? "";
                            addr.PostalCode = dr["Zip"]?.ToString()?.Trim() ?? "";
                            qboVendor.BillAddr = addr;

                            qboVendor.Active = true;

                            Intuit.Ipp.Data.Vendor added = dataService.Add(qboVendor);

                            using (var upd = new SqlCommand(@"
                                UPDATE [msSchedulerV3].[dbo].[tbl_Vendor]
                                SET QboVendorID = @qid, ModifiedDate = GETDATE()
                                WHERE VendorID = @vid AND CompanyID = @cid", conn))
                            {
                                upd.Parameters.AddWithValue("@qid", added.Id);
                                upd.Parameters.AddWithValue("@vid", Convert.ToInt32(dr["VendorID"]));
                                upd.Parameters.AddWithValue("@cid", CompanyID);
                                upd.ExecuteNonQuery();
                            }
                            pushed++;
                        }
                        catch
                        {
                            using (var upd = new SqlCommand(@"
                                UPDATE [msSchedulerV3].[dbo].[tbl_Vendor]
                                SET QboVendorID = '-11', ModifiedDate = GETDATE()
                                WHERE VendorID = @vid AND CompanyID = @cid", conn))
                            {
                                upd.Parameters.AddWithValue("@vid", Convert.ToInt32(dr["VendorID"]));
                                upd.Parameters.AddWithValue("@cid", CompanyID);
                                upd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch { }
            return pushed;
        }

        private ReferenceType GetDefaultExpenseAccountRef(ServiceContext context)
        {
            var qs = new QueryService<Account>(context);
            var accounts = qs.ExecuteIdsQuery(
                "SELECT * FROM Account WHERE AccountType = 'Expense' AND Active = true MAXRESULTS 1").ToList();

            if (accounts.Count == 0)
            {
                accounts = qs.ExecuteIdsQuery(
                    "SELECT * FROM Account WHERE AccountType = 'Cost of Goods Sold' AND Active = true MAXRESULTS 1").ToList();
            }

            if (accounts.Count == 0)
            {
                accounts = qs.ExecuteIdsQuery(
                    "SELECT * FROM Account WHERE Classification = 'Expense' AND Active = true MAXRESULTS 1").ToList();
            }

            if (accounts.Count == 0)
                throw new Exception("No expense account found in QuickBooks. Please create one first.");

            return new ReferenceType { Value = accounts[0].Id, name = accounts[0].Name };
        }
        public string SyncBillToQBO(ServiceContext context, string companyId, int billId)
        {
            if (context == null)
                throw new Exception("QBO is not connected.");

            string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
            string qboVendorId = null;
            string existingQboBillId = null;
            string billNumber = "";
            int vendorLocalId = 0;
            DateTime billDate = DateTime.Today;
            decimal total = 0;

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT b.BillNumber, b.BillDate, b.Total, b.QboBillID, b.VendorID,
                           v.QboVendorID, v.Name, v.ContactName, v.Email, v.Phone,
                           v.Address, v.City, v.State, v.Zip, v.Website, v.DBA
                    FROM [msSchedulerV3].[dbo].[tbl_Bill] b
                    LEFT JOIN [msSchedulerV3].[dbo].[tbl_Vendor] v 
                        ON v.VendorID = b.VendorID AND v.CompanyID = b.CompanyID
                    WHERE b.BillID = @bid AND b.CompanyID = @cid", conn))
                {
                    cmd.Parameters.AddWithValue("@bid", billId);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            throw new Exception("Bill not found.");
                        billNumber = dr["BillNumber"]?.ToString() ?? "";
                        qboVendorId = dr["QboVendorID"]?.ToString();
                        existingQboBillId = dr["QboBillID"]?.ToString();
                        vendorLocalId = dr["VendorID"] != DBNull.Value ? Convert.ToInt32(dr["VendorID"]) : 0;
                        if (dr["BillDate"] != DBNull.Value) billDate = Convert.ToDateTime(dr["BillDate"]);
                        if (dr["Total"] != DBNull.Value) total = Convert.ToDecimal(dr["Total"]);

                        if (string.IsNullOrEmpty(qboVendorId) || qboVendorId == "0" || qboVendorId == "-11")
                        {
                            string vendorName = dr["Name"]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(vendorName))
                                throw new Exception("Vendor has no name. Cannot create in QuickBooks.");

                            var qboV = new Intuit.Ipp.Data.Vendor();
                            qboV.DisplayName = vendorName;
                            string dba = dr["DBA"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(dba)) qboV.CompanyName = dba;
                            string contact = dr["ContactName"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(contact))
                            {
                                string[] np = contact.Split(new[] { ' ' }, 2);
                                qboV.GivenName = np[0];
                                if (np.Length > 1) qboV.FamilyName = np[1];
                            }
                            string vEmail = dr["Email"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(vEmail))
                                qboV.PrimaryEmailAddr = new Intuit.Ipp.Data.EmailAddress { Address = vEmail };
                            string vPhone = dr["Phone"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(vPhone))
                                qboV.PrimaryPhone = new TelephoneNumber { FreeFormNumber = vPhone };

                            var vAddr = new PhysicalAddress();
                            vAddr.Line1 = dr["Address"]?.ToString()?.Trim() ?? "";
                            vAddr.City = dr["City"]?.ToString()?.Trim() ?? "";
                            vAddr.CountrySubDivisionCode = dr["State"]?.ToString()?.Trim() ?? "";
                            vAddr.PostalCode = dr["Zip"]?.ToString()?.Trim() ?? "";
                            qboV.BillAddr = vAddr;
                            qboV.Active = true;

                            DataService vendorDs = new DataService(context);
                            var addedV = vendorDs.Add(qboV);
                            qboVendorId = addedV.Id;
                        }
                    }
                }

                if (vendorLocalId > 0 && !string.IsNullOrEmpty(qboVendorId) && qboVendorId != "0")
                {
                    using (var upd = new SqlCommand(@"
                        UPDATE [msSchedulerV3].[dbo].[tbl_Vendor]
                        SET QboVendorID = @qid, ModifiedDate = GETDATE()
                        WHERE VendorID = @vid AND CompanyID = @cid", conn))
                    {
                        upd.Parameters.AddWithValue("@qid", qboVendorId);
                        upd.Parameters.AddWithValue("@vid", vendorLocalId);
                        upd.Parameters.AddWithValue("@cid", companyId);
                        upd.ExecuteNonQuery();
                    }
                }

                DataTable dtLines = new DataTable();
                using (var cmd = new SqlCommand(@"
                    SELECT LineNum, Description, Quantity, UnitPrice, Total
                    FROM [msSchedulerV3].[dbo].[tbl_BillLineItem]
                    WHERE BillID = @bid
                    ORDER BY LineNum", conn))
                {
                    cmd.Parameters.AddWithValue("@bid", billId);
                    var da = new SqlDataAdapter(cmd);
                    da.Fill(dtLines);
                }

                ReferenceType expenseAcctRef = GetDefaultExpenseAccountRef(context);

                Intuit.Ipp.Data.Bill qboBill = new Intuit.Ipp.Data.Bill();
                qboBill.VendorRef = new ReferenceType { Value = qboVendorId };
                qboBill.DocNumber = billNumber;
                qboBill.TxnDate = billDate;
                qboBill.TxnDateSpecified = true;

                var qboLines = new System.Collections.Generic.List<Line>();

                foreach (DataRow lr in dtLines.Rows)
                {
                    decimal qty = lr["Quantity"] != DBNull.Value ? Convert.ToDecimal(lr["Quantity"]) : 1;
                    decimal unitPrice = lr["UnitPrice"] != DBNull.Value ? Convert.ToDecimal(lr["UnitPrice"]) : 0;
                    decimal lineTotal = lr["Total"] != DBNull.Value ? Convert.ToDecimal(lr["Total"]) : qty * unitPrice;
                    string desc = lr["Description"]?.ToString() ?? "";

                    Line line = new Line();
                    line.DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail;
                    line.DetailTypeSpecified = true;
                    line.Amount = lineTotal;
                    line.AmountSpecified = true;
                    line.Description = desc;

                    var detail = new AccountBasedExpenseLineDetail();
                    detail.AccountRef = expenseAcctRef;
                    line.AnyIntuitObject = detail;
                    qboLines.Add(line);
                }

                if (qboLines.Count == 0)
                {
                    Line singleLine = new Line();
                    singleLine.DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail;
                    singleLine.DetailTypeSpecified = true;
                    singleLine.Amount = total;
                    singleLine.AmountSpecified = true;
                    singleLine.Description = "Bill " + billNumber;
                    var fallbackDetail = new AccountBasedExpenseLineDetail();
                    fallbackDetail.AccountRef = expenseAcctRef;
                    singleLine.AnyIntuitObject = fallbackDetail;
                    qboLines.Add(singleLine);
                }

                qboBill.Line = qboLines.ToArray();

                DataService dataService = new DataService(context);
                string returnedQboId;

                bool isUpdate = !string.IsNullOrEmpty(existingQboBillId)
                                && !existingQboBillId.StartsWith("QBO-BILL-");

                if (isUpdate)
                {
                    var qs = new QueryService<Intuit.Ipp.Data.Bill>(context);
                    var found = qs.ExecuteIdsQuery(
                        "SELECT * FROM Bill WHERE Id = '" + existingQboBillId + "'")
                        .FirstOrDefault();
                    if (found != null)
                    {
                        qboBill.Id = found.Id;
                        qboBill.SyncToken = found.SyncToken;
                        var updated = dataService.Update(qboBill);
                        returnedQboId = updated.Id;
                    }
                    else
                    {
                        var added = dataService.Add(qboBill);
                        returnedQboId = added.Id;
                    }
                }
                else
                {
                    var added = dataService.Add(qboBill);
                    returnedQboId = added.Id;
                }

                using (var upd = new SqlCommand(@"
                    UPDATE [msSchedulerV3].[dbo].[tbl_Bill]
                    SET QboBillID = @qid, ModifiedDate = GETDATE()
                    WHERE BillID = @bid AND CompanyID = @cid", conn))
                {
                    upd.Parameters.AddWithValue("@qid", returnedQboId);
                    upd.Parameters.AddWithValue("@bid", billId);
                    upd.Parameters.AddWithValue("@cid", companyId);
                    upd.ExecuteNonQuery();
                }

                return returnedQboId;
            }
        }

    }
}
public class InsertQuery
{
    public string Invoice { get; set; }
    public string InvoiceDetails { get; set; }
}