
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Web;
using FSM.SMSService;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;

namespace TPM
{
    public class EmailCommunication
    {
        public string EmailTo { get; set; }
        public string StandardMailSubject { get; set; }
        public string StandardMailBody { get; set; }
        public string EmailBCC { get; set; }
        public string EmailCC { get; set; }
        public string ProposalMailSubject { get; set; }
        public string ProposalMailBody { get; set; }
        public string EmailConfirmText { get; set; }
        public string SMSConfirmText { get; set; }
        public string EmailAckText { get; set; }
        public string SMSAckText { get; set; }
        public string MobileNo { get; set; }
        public string InvoiceMailSubject { get; set; }
        public string InvoiceMailBody { get; set; }
        public string AttachmentsName { get; set; }
        public string EmailType { get; set; }
        public decimal ReqDepoAmt { get; set; }
        public string RescheduledMailSubject { get; set; }
        public string RescheduleMailBody { get; set; }

        public List<EmailContent> EmailContents { get; set; }
    }

    public class EmailContent
    {
        public byte[] FileContent { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileUrl { get; set; }
    }

    public class EmailProcessor
    {
        public DataTable dt_Customer = null;
        public DataTable dt_Company = null;
        private DataSet dataSet = null;
        private string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public EmailCommunication FillEmailPopUp(string CustomerID)
        {
            string EmailTo = string.Empty;

            //  db.ExecuteScalar("select Email from tbl_Customer where customerID=" + CustomerID + " and CompanyID='" + companyid + "' ");

            Database db = new Database(connStr);
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();

            string sql = "Select * from tbl_CustCommunication where  CompanyID=@CompanyID;";
            sql += "select Concat(FirstName,' ', LastName)as FullName,* from tbl_Customer where  CompanyID=@CompanyID and customerID='" + CustomerID + "';";
            sql += "select * from tbl_Company where CompanyID=@CompanyID;";
            DataTable dt_CustCommunication = new DataTable();

            DataSet dataSet = db.Get_DataSet(sql, CompanyID);
            dt_CustCommunication = dataSet.Tables[0];
            dt_Customer = dataSet.Tables[1];
            dt_Company = dataSet.Tables[2];
            EmailTo = dataSet.Tables[1].Rows.Count > 0 ? dataSet.Tables[1].Rows[0]["Email"].ToString() : "";

            EmailCommunication emailcomm = new EmailCommunication();

            emailcomm.EmailTo = EmailTo;
            if (dt_CustCommunication.Rows.Count > 0)
            {
                DataRow dr = dt_CustCommunication.Rows[0];
                emailcomm.StandardMailSubject = dr["StandardMailSubject"].ToString();
                emailcomm.StandardMailBody = dr["StandardMailBody"].ToString();
                emailcomm.ProposalMailSubject = dr["ProposalMailSubject"].ToString();
                emailcomm.ProposalMailBody = dr["ProposalMailBody"].ToString();
                emailcomm.EmailCC = dr["EmailCC"].ToString();
                emailcomm.EmailBCC = dr["EmailBCC"].ToString();
                if (Convert.ToBoolean(dr["SMSAck"].ToString()))
                {
                    emailcomm.SMSAckText = dr["SMSAckText"].ToString();
                }
                if (Convert.ToBoolean(dr["SMSConfirm"].ToString()))
                {
                    emailcomm.SMSConfirmText = dr["SMSConfirmText"].ToString();
                }
                if (Convert.ToBoolean(dr["EmaiAck"].ToString()))
                {
                    emailcomm.EmailAckText = dr["EmailAckText"].ToString();
                }
                if (Convert.ToBoolean(dr["EmailConfirm"].ToString()))
                {
                    emailcomm.EmailConfirmText = dr["EmailConfirmText"].ToString();
                }

                //replace email body
                string emailbody = emailcomm.StandardMailBody;

                emailcomm.StandardMailBody = stringRepalace(emailbody, CustomerID, CompanyID, "");
            }

            return emailcomm;
        }

        //public static string GetCSPaymentLink(string CompanyID, string CustomerID, string InvoiceNo, string CustomerName, string email, string amount)
        //{
        //    string firstName = "";
        //    string lastName = "";
        //    string UserID = HttpContext.Current.Session["LoginUser"].ToString();
        //    if (!string.IsNullOrEmpty(CustomerName))
        //    {
        //        string[] parts = CustomerName.Split(' ');
        //        firstName = string.Join(" ", parts.Take(parts.Length - 1));
        //        lastName = parts.Last();
        //    }
        //    string paymentLink = "";
        //    //PaymentServiceSoapClient ps = new PaymentServiceSoapClient();
        //    //try
        //    //{
        //    //    paymentLink = ps.GetCSLink(CompanyID, firstName, lastName, email, InvoiceNo, amount, UserID);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    return "";
        //    //}

        //    //if (paymentLink.Contains("Error"))
        //    //{
        //    //    return "";
        //    }

        //    return paymentLink;
        //}

        public EmailCommunication FillEmailPopUp(string CustomerID, string InvoiceNo, int FromInvDtl = 0)
        {
            Database db = new Database(connStr);
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();

            string mailinfo = "Select*from tbl_CustCommunication where  CompanyID='" + companyid + "'";
            DataTable dt = new DataTable();
            //  db.Open();
            db.Execute(mailinfo, out dt);
            db.Close();
            EmailCommunication emailcomm = new EmailCommunication();
            string EmailTo = db.ExecuteScalar("select Email from tbl_Customer where customerID=" + CustomerID + " and CompanyID='" + companyid + "' ");

            string MobileNo = db.ExecuteScalar("select Mobile from tbl_Customer where customerID=" + CustomerID + " and CompanyID='" + companyid + "' ");

            emailcomm.MobileNo = MobileNo;

            string reqDepoAmt = "0";
            string reqDepoAmtDollarAmt = "0";

            if (FromInvDtl > 0)
            {
                reqDepoAmt = db.ExecuteScalar("select ((isNULL(Total,0)-isNULL(AmountCollect,0)) * isNULL(ReqDepoPercent,0))/100 from tbl_invoice where id='" + InvoiceNo + "' and CompnyID='" + companyid + "'");
                reqDepoAmtDollarAmt = db.ExecuteScalar("select isNULL(RequestedDepoAmt,0)reqDep from tbl_invoice where  id='" + InvoiceNo + "' and CompnyID='" + companyid + "'");
                if (reqDepoAmt == "0" || reqDepoAmt == "0.000000") reqDepoAmt = reqDepoAmtDollarAmt;
            }
            else reqDepoAmt = db.ExecuteScalar("select (isNULL(Total,0)-isNULL(AmountCollect,0))  from tbl_invoice where id='" + InvoiceNo + "' and CompnyID='" + companyid + "'");


            emailcomm.EmailTo = EmailTo;
            emailcomm.ReqDepoAmt = Convert.ToDecimal(reqDepoAmt);
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                emailcomm.StandardMailSubject = dr["StandardMailSubject"].ToString();
                emailcomm.StandardMailBody = dr["StandardMailBody"].ToString();
                emailcomm.ProposalMailSubject = dr["ProposalMailSubject"].ToString();
                emailcomm.ProposalMailBody = dr["ProposalMailBody"].ToString();
                emailcomm.InvoiceMailSubject = dr["InvoiceMailSubject"].ToString();
                emailcomm.InvoiceMailBody = dr["InvoiceMailBody"].ToString();

                emailcomm.EmailCC = dr["EmailCC"].ToString();
                emailcomm.EmailBCC = dr["EmailBCC"].ToString();

                if (!string.IsNullOrEmpty(emailcomm.ProposalMailSubject)) emailcomm.ProposalMailSubject = dr["ProposalMailSubject"].ToString().Replace("[Invoice No]", InvoiceNo);
                if (!string.IsNullOrEmpty(emailcomm.InvoiceMailSubject)) emailcomm.InvoiceMailSubject = dr["InvoiceMailSubject"].ToString().Replace("[Invoice No]", InvoiceNo);

                //replace email body
                string emailbody = emailcomm.ProposalMailBody;
                string emailbodyInv = emailcomm.InvoiceMailBody;

                emailcomm.ProposalMailBody = stringRepalace(emailbody, CustomerID, companyid, InvoiceNo);
                emailcomm.InvoiceMailBody = stringRepalace(emailbodyInv, CustomerID, companyid, InvoiceNo);
            }

            return emailcomm;
        }
        public EmailCommunication FillEmailPopUpReschedule(string CustomerID, string ApptId)
        {
            Database db = new Database(connStr);
            string companyid = HttpContext.Current.Session["CompanyID"].ToString();

            string mailinfo = "Select*from tbl_CustCommunication where  CompanyID='" + companyid + "'";
            DataTable dt = new DataTable();
            //  db.Open();
            db.Execute(mailinfo, out dt);
            db.Close();
            EmailCommunication emailcomm = new EmailCommunication();
            string EmailTo = db.ExecuteScalar("select Email from tbl_Customer where customerID=" + CustomerID + " and CompanyID='" + companyid + "' ");

            emailcomm.EmailTo = EmailTo;

            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                emailcomm.StandardMailSubject = dr["StandardMailSubject"].ToString();
                emailcomm.StandardMailBody = dr["StandardMailBody"].ToString();
                emailcomm.ProposalMailSubject = dr["ProposalMailSubject"].ToString();
                emailcomm.ProposalMailBody = dr["ProposalMailBody"].ToString();
                emailcomm.InvoiceMailSubject = dr["InvoiceMailSubject"].ToString();
                emailcomm.InvoiceMailBody = dr["InvoiceMailBody"].ToString();

                emailcomm.RescheduledMailSubject = dr["AppointmentRescheduleSubject"].ToString();
                emailcomm.RescheduleMailBody = dr["AppointmentRescheduleBody"].ToString();

                emailcomm.EmailCC = dr["EmailCC"].ToString();
                emailcomm.EmailBCC = dr["EmailBCC"].ToString();


                //replace email body
                string emailbody = emailcomm.ProposalMailBody;
                string emailbodyInv = emailcomm.InvoiceMailBody;


                string CompanyGUID = "";
                string CustomerName = "";
                string ApptDate = "";
                string TimeSlotName = "";
                string Address = "";
                string Phone = "";
                string Mobile = "";
                string Email = "";
                string CompanyName = "";
                string CompanyEmail = "";
                string CompanyPhone = "";
                string CompanyWebsite = "";
                string Sql = "";


                Database dbReschedule = new Database(connStr);
                DataTable dtReschedule = new DataTable();


                Sql = "Select c.CustomerID,c.CustomerGuid,c.title,c.FirstName,c.LastName,c.Jobtitle,c.Address1,c.City,c.State,c.ZipCode,c.Phone,c.Mobile,c.Email,a.StartDateTime" +
               " From msSchedulerV3.dbo.tbl_Customer c inner join tbl_Appointment a on a.CustomerID=c.CustomerID and c.CompanyID=a.CompanyID and a.ApptId= " + ApptId + "" +
               " where c.CompanyID='" + companyid + "'" +
               " and c.CustomerID = '" + CustomerID + "'";
                // db.Open();
                db.Execute(Sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow drReSchedule = dt.Rows[0];

                    CustomerName = drReSchedule["title"].ToString() + " " + drReSchedule["FirstName"].ToString() + " " + drReSchedule["LastName"].ToString();
                    Address = drReSchedule["Address1"].ToString() + ", " + drReSchedule["City"].ToString() + ", " + drReSchedule["State"].ToString() + " " + drReSchedule["ZipCode"].ToString();

                    ApptDate = drReSchedule["StartDateTime"].ToString().Split(' ')[0];
                    TimeSlotName = drReSchedule["StartDateTime"].ToString().Split(' ')[1];

                    Mobile = drReSchedule["Mobile"].ToString();
                    Phone = drReSchedule["Phone"].ToString();
                    Email = drReSchedule["Email"].ToString();

                }


                Mobile = Mobile.Replace("-", "");
                Mobile = Mobile.Replace("(", "");
                Mobile = Mobile.Replace(")", "");


                Sql = "SELECT * From msSchedulerV3.dbo.tbl_Company Where CompanyID = '" + companyid + "'";
                dt = new DataTable();
                db.Execute(Sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow drCompany = dt.Rows[0];
                    CompanyName = drCompany["CompanyName"].ToString();
                    CompanyEmail = drCompany["Email"].ToString();
                    CompanyPhone = drCompany["Phone"].ToString();
                    CompanyWebsite = drCompany["Website"].ToString();
                    CompanyGUID = drCompany["CompanyGUID"].ToString();

                }

                // Replace placeholders
                string message = emailcomm.RescheduleMailBody.Replace("[First Name]", CustomerName)
                                         .Replace("[Date]", ApptDate)
                                         .Replace("[Time]", TimeSlotName)
                                         .Replace("[Company Name]", CompanyName);

                emailcomm.RescheduleMailBody = message;
            }

            return emailcomm;
        }
        public string GetPrequalLink(string companyID)
        {
            Database db = new Database();

            string PreqalLink = db.ExecuteScalar("select PrequalLink From Wiseteck.dbo.tbl_MerchantSettings Where CompanyID = '" + companyID + "'; ");

            if (PreqalLink != null || PreqalLink != "0")
            {
                return PreqalLink;
            }
            return "";
        }

        //will all the string  replace implemented later.
        public string stringRepalace(string replacedFrom, string CustomerID, string CompanyID, string InvoiceNo)
        {
            Database db = new Database(connStr);
            DataTable dt = new DataTable();
            string sql = string.Empty;
            if (dt_Customer == null)
            {
                sql = "select Concat(FirstName,' ', LastName)as FullName,* from tbl_Customer where customerID=" + CustomerID + " and CompanyID='" + CompanyID + "' ";
                //  db.Open();
                db.Execute(sql, out dt);
            }
            else
            {
                dt = dt_Customer;
            }

            if (string.IsNullOrEmpty(replacedFrom)) return "";

            string replacedstring = "";

            replacedstring = replacedFrom;
            replacedstring = replaceCompanyDetails(CompanyID, replacedstring);
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                replacedstring = replacedstring.Replace("[Title]", dr["title"].ToString());
                replacedstring = replacedstring.Replace("[Job Title]", dr["jobtitle"].ToString());
                replacedstring = replacedstring.Replace("[Full Name]", dr["FullName"].ToString());
                replacedstring = replacedstring.Replace("[First Name]", dr["FirstName"].ToString());
                replacedstring = replacedstring.Replace("[Last Name]", dr["LastName"].ToString());
            }

            //replaceing invoice details
            if (!string.IsNullOrEmpty(InvoiceNo))
            {
                sql = "select CONVERT(VARCHAR(10), InvoiceDate, 101)as InvoiceDate,* from tbl_Invoice where CustomerId=" + CustomerID + " and CompnyID='" + CompanyID + "' and id='" + InvoiceNo + "'";

                dt = new DataTable();
                db.Execute(sql, out dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow dr = dt.Rows[0];
                    replacedstring = replacedstring.Replace("[Invoice No]", dr["Number"].ToString());
                    replacedstring = replacedstring.Replace("[Invoice Total]", dr["Total"].ToString());
                    replacedstring = replacedstring.Replace("[Invoice Date]", dr["InvoiceDate"].ToString());
                    replacedstring = replacedstring.Replace("[Paid]", dr["AmountCollect"].ToString());
                }
            }

            replacedstring = replacedstring.Replace("at select", "");
            string prequalLink = GetPrequalLink(CompanyID);
            if (!string.IsNullOrEmpty(prequalLink))
            {
                replacedstring = replacedstring.Replace("[Prequal]", prequalLink);
            }
            else
            {
                replacedstring = replacedstring.Replace("[Prequal]", "");
            }
            db.Close();
            return replacedstring;
        }

        public string replaceCompanyDetails(string companyID, string replaceText)
        {
            string CompanyAddress = "";
            string CompanyCity = "";
            string CompanyState = "";
            string CompanyZipCode = "";
            string CompanyPhone = "";
            string CompanyEmail = "";
            string CompanyWebsite = "";
            string CompanyFacebook = "";
            string CompanyTwitter = "";
            string CompanyLogoFile = "";
            string CompanyFullName = "";
            string CompanyGUID = "";
            Database db = new Database(connStr);
            string sql = "select*from tbl_Company where CompanyID='" + companyID + "'";

            DataTable dt = new DataTable();
            //  db.Open();
            if (dt_Company == null)
            {
                db.Execute(sql, out dt);
            }
            else
            {
                dt = dt_Company;
            }

            if (dt.Rows.Count > 0)
            {
                DataRow Rs = dt.Rows[0];
                CompanyAddress = Rs["Address"].ToString();
                CompanyCity = Rs["City"].ToString();
                CompanyState = Rs["State"].ToString();
                CompanyZipCode = Rs["ZipCode"].ToString();
                CompanyPhone = Rs["Phone"].ToString();
                CompanyEmail = Rs["Email"].ToString();
                CompanyWebsite = Rs["Website"].ToString();
                CompanyFacebook = Rs["Facebook"].ToString();
                CompanyTwitter = Rs["Twitter"].ToString();
                CompanyLogoFile = Rs["LogoFile"].ToString();
                CompanyFullName = Rs["CompanyName"].ToString();
                CompanyGUID = Rs["CompanyGUID"].ToString();
            }
            string replacedText = replaceText;
            if (!string.IsNullOrEmpty(replacedText))
            {
                replacedText = replacedText.Replace("[Company Name]", CompanyFullName);
                replacedText = replacedText.Replace("[Address]", CompanyAddress + "," + CompanyCity + "," + CompanyState + " " + CompanyZipCode);
                replacedText = replacedText.Replace("[Phone]", CompanyPhone);
                replacedText = replacedText.Replace("[Email]", CompanyEmail);
                replacedText = replacedText.Replace("[Website]", CompanyWebsite);
                replacedText = replacedText.Replace("[Facebook]", CompanyFacebook);

                //StringBuilder builder = new StringBuilder(replacedText);
                //builder.Replace("\r\n", "<br />").Replace("\n", "<br />").Replace("\r", "<br />");
                //replacedText = builder.ToString();
            }
            return replacedText;
        }

        public EmialSetting GetEmailSetting()
        {
            EmialSetting emialSetting = new EmialSetting();
            emialSetting.Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]);
            emialSetting.SMTP = ConfigurationManager.AppSettings["SMTP"].ToString();
            emialSetting.SmtpUser = ConfigurationManager.AppSettings["SmtpUser"].ToString();
            emialSetting.SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"].ToString();

            try
            {
                string sql = "select * from [msSchedulerV3].[dbo].[tbl_EmailSetting] where CompanyID=@CompanyID;";

                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString().Trim();
                Database db = new Database(connStr);

                dataSet = db.Get_DataSet(sql, CompanyID);
                if (dataSet.Tables[0].Rows.Count > 0)
                {
                    emialSetting.Port = Convert.ToInt32(dataSet.Tables[0].Rows[0]["Port"]);
                    emialSetting.SMTP = dataSet.Tables[0].Rows[0]["SMTP"].ToString();
                    emialSetting.SmtpUser = dataSet.Tables[0].Rows[0]["SmtpUser"].ToString();
                    emialSetting.SmtpPassword = dataSet.Tables[0].Rows[0]["SmtpPassword"].ToString();
                }
            }
            catch
            {

            }
            return emialSetting;

        }
        public string SendHtmlFormattedEmail(string CompanyID, string CustomerID, string EmailType, string subject, string body,
                string recepientToEmail, string recepientCCEmail, string recepientBCCEmail, List<EmailContent> emailContents, List<AlternateView> alternateViews = default(List<AlternateView>))
        {
            string SendBy = "From Customer";

            if (HttpContext.Current.Session["LoginUser"] != null)
            {
                SendBy = HttpContext.Current.Session["LoginUser"].ToString();
            }
            string CompanyAddress = "";
            string CompanyCity = "";
            string CompanyState = "";
            string CompanyZipCode = "";
            string CompanyPhone = "";
            string CompanyEmail = "";
            string CompanyWebsite = "";
            string CompanyFacebook = "";
            string CompanyTwitter = "";
            string CompanyLogoFile = "";
            string CompanyFullName = "";
            string CompanyGUID = "";
            try
            {
                string wisetackFooter = "";

                Database db = new Database(connStr);

                string historyid = string.Empty;
                string EmailFrom = string.Empty;
                bool isSendPrequal = false;

                string prequalLink = string.Empty;

                string sql = "select * from msSchedulerV3.dbo.tbl_Company where CompanyID=@CompanyID;";

                sql += "Select [WisetackFooterMsg] from msSchedulerV3.dbo.tbl_CustCommunication where  CompanyID=@CompanyID;";

                sql += "Select ISNULL(Max(id), 0)+1 as newid from msSchedulerV3.dbo.tbl_EmailHistory;";

                sql += "Select EmailFrom from msSchedulerV3.dbo.tbl_CustCommunication where CompanyID = @CompanyID;";

                sql += "Select ISNULL(IsSendPrequal, 0) AS IsSendPrequal from msSchedulerV3.dbo.tbl_CustCommunication where  CompanyID=@CompanyID;";

                sql += "select ISNULL(PrequalLink, '') AS PrequalLink  From Wiseteck.dbo.tbl_MerchantSettings Where CompanyID =@CompanyID;";

                DataTable dt_Company = new DataTable();


                dataSet = db.Get_DataSet(sql, CompanyID);


                dt_Company = dataSet.Tables[0];

                wisetackFooter = dataSet.Tables[1].Rows.Count > 0 ? dataSet.Tables[1].Rows[0]["WisetackFooterMsg"].ToString() : "";
                historyid = dataSet.Tables[2].Rows.Count > 0 ? dataSet.Tables[2].Rows[0]["newid"].ToString() : "";
                EmailFrom = dataSet.Tables[3].Rows.Count > 0 ? dataSet.Tables[3].Rows[0]["EmailFrom"].ToString() : "";
                isSendPrequal = dataSet.Tables[4].Rows.Count > 0 ? Convert.ToBoolean(dataSet.Tables[4].Rows[0]["IsSendPrequal"].ToString()) : false;

                prequalLink = dataSet.Tables[5].Rows.Count > 0 ? dataSet.Tables[5].Rows[0]["PrequalLink"].ToString() : "";

                //db.Execute(sql, out dt_Company);

                if (dt_Company.Rows.Count > 0)
                {
                    DataRow Rs = dt_Company.Rows[0];
                    CompanyAddress = Rs["Address"].ToString();
                    CompanyCity = Rs["City"].ToString();
                    CompanyState = Rs["State"].ToString();
                    CompanyZipCode = Rs["ZipCode"].ToString();
                    CompanyPhone = Rs["Phone"].ToString();
                    CompanyEmail = Rs["Email"].ToString();
                    CompanyWebsite = Rs["Website"].ToString();
                    CompanyFacebook = Rs["Facebook"].ToString();
                    CompanyTwitter = Rs["Twitter"].ToString();
                    CompanyLogoFile = Rs["LogoFile"].ToString();
                    CompanyFullName = Rs["CompanyName"].ToString();
                    CompanyGUID = Rs["CompanyGUID"].ToString();
                }
                string Emailbody = body;

                if (!string.IsNullOrEmpty(body))
                {
                    StringBuilder builder = new StringBuilder(Emailbody);
                    builder.Replace("\r\n", "<br />").Replace("\n", "<br />").Replace("\r", "<br />");
                    Emailbody = builder.ToString();
                }

                string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + CompanyLogoFile);
                string header = "";

                string BodyText = "";

                //   wisetackFooter = db.ExecuteScalar("Select [WisetackFooterMsg] from msSchedulerV3.dbo.tbl_CustCommunication where  CompanyID='" + CompanyID + "'");

                //if (HttpContext.Current.Session["LoanInfo"] != null && (EmailType.Contains("Invoice") || (EmailType.Contains("Proposal"))))
                //{
                //    LoanInfo ln = (LoanInfo)HttpContext.Current.Session["LoanInfo"];

                //    using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/ProposalEmailTemplateBody.html")))
                //    {
                //        Emailbody = "<body><table style='max-width:800px; font-family:Arial;font-size:13px;'>" +
                //        "<tr><td align='left'><table width='100%'><tr><td>" +
                //        "<img src=\"cid:photo\"  id='img' alt='' style='max-height:60px;' height='60' /></td>" +
                //        "<td align='center' style='color:#2980b9; vertical-align:bottom;'>" +
                //        "</td></tr></table></td></tr>" +
                //        "<tr><td align='left' style='padding-left:5px;'>" +
                //        Emailbody +
                //               "</td></tr>";
                //        Emailbody += reader.ReadToEnd();

                //        string query = @"SELECT concat (c.FirstName,' ',c.LastName)as FullName,c.Address1,c.Email,c.Phone,CONVERT(VARCHAR(10), i.InvoiceDate, 101)as IssueDate,i.CompanyId, i.CustomerId, i.Subtotal, i.Number, i.UserId, i.Discount, i.Tax, i.Total, i.Status, i.InvoiceType, i.AmountCollect, i.AppointmentId, i.Note,
                //                         i.CreatedDate, i.CreatedBy, i.InvoiceDate, i.TaxType, i.Type, i.QboId, i.DiscountRate, i.DiscountOption, i.QboEstimateId, i.ExpirationDate, i.DepositAmount,
                //                         id.InvoiceNumber, id.RefId, id.InvoiceId, id.ItemId, id.Quantity, id.uPrice, id.ItemName, id.Description, id.TotalPrice,
                //                         id.ModifiedDate, id.ModifiedBy, id.ItemTyId, id.IsTaxable, id.ServiceDate
                //            FROM   tbl_Customer c INNER JOIN
                //                         tbl_Invoice i ON c.CompanyID = i.CompnyID AND c.CustomerID = i.CustomerId INNER JOIN
                //                         tbl_InvoiceDetails id ON i.id = id.RefId
                //            WHERE (i.CompnyID = N'" + HttpContext.Current.Session["CompanyID"].ToString() + "') AND (i.CustomerId = N'" + CustomerID + "') AND (i.id = N'" + ln.SalesID + "')";

                //        //  db.Open();
                //        DataTable dt_invocie = new DataTable();

                //        db.Execute(query, out dt_invocie);
                //        db.Close();
                //        if (dt_invocie.Rows.Count > 0)
                //        {
                //            DataRow dr = dt_invocie.Rows[0];

                //            Emailbody = Emailbody.Replace("{customerName}", dr["FullName"].ToString());
                //            Emailbody = Emailbody.Replace("{address}", dr["Address1"].ToString());
                //            Emailbody = Emailbody.Replace("{phone}", dr["Phone"].ToString());
                //            Emailbody = Emailbody.Replace("{email}", dr["Email"].ToString());
                //            Emailbody = Emailbody.Replace("{IssueDate}", dr["IssueDate"].ToString());
                //            Emailbody = Emailbody.Replace("{InvoiceNo}", dr["Number"].ToString());
                //            Emailbody = Emailbody.Replace("{InvoiceTotal}", dr["Total"].ToString());
                //            Emailbody = Emailbody.Replace("{SubTotal}", dr["Subtotal"].ToString());
                //            Emailbody = Emailbody.Replace("{Discount}", dr["Discount"].ToString());
                //            Emailbody = Emailbody.Replace("{Total}", dr["Total"].ToString());
                //            Emailbody = Emailbody.Replace("{Tax}", dr["Tax"].ToString());

                //            Emailbody = Emailbody.Replace("{DocType}", dr["Type"].ToString());
                //            Emailbody = Emailbody.Replace("[Discliemer]", "*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our <a href='https://www.wisetack.com/lenders' style='color:blue'>lending partners</a>. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at <a href='http://wisetack.com/faqs' style='color:blue'>http://wisetack.com/faqs</a>.");

                //            string itemDetails = "";
                //            foreach (DataRow d in dt_invocie.Rows)
                //            {
                //                itemDetails += "<tr style='border:1px solid #ddd; padding:.35em;'>" +
                //                   "<th colspan='3' style='padding: .625em; text-align: center; scope='row'>" + d["ItemName"] + "</th>" +
                //                   "<td colspan='4' style='padding: .625em; text-align: center;'>" + d["Description"] + "</td>" +
                //                   "<td colspan='2' style='padding: .625em; text-align: center;'>$ " + d["uPrice"] + "</td>" +
                //                   "<td colspan='1' style='padding: .625em; text-align: center;'>" + d["Quantity"] + "</td>" +
                //                   "<td colspan='2' style='padding: .625em; text-align: center;'>$ " + d["TotalPrice"] + "</td>" +
                //                   "</tr>";
                //            }
                //            Emailbody = Emailbody.Replace("{itemRow}", itemDetails);
                //        }
                //    }

                //    //Emailbody += body;
                //    Emailbody = Emailbody.Replace("{AsLowAsAmount}", ln.AsLowAsAmount);
                //    Emailbody = Emailbody.Replace("{paymentLink}", ln.PaymentLink);
                //    if (ln.Status == "LOAN CONFIRMED")
                //    {
                //        Emailbody = Emailbody.Replace("{isdisable}", "pointer-events: none;opacity: 0.5; ");
                //        Emailbody = Emailbody.Replace("{Status}", ln.Status);
                //    }
                //    else
                //    {
                //        Emailbody = Emailbody.Replace("{isdisable}", "");
                //        Emailbody = Emailbody.Replace("{Status}", "");
                //    }
                //    BodyText = "<br/><br/>" + Emailbody;

                //    // HttpContext.Current.Session["LoanInfo"] = null;
                //}
                //else
                //{
                    BodyText = "<body><table style='max-width:800px; font-family:Arial;font-size:13px;'>" +
                 "<tr><td align='left'><table width='100%'><tr><td>" +
                 "<img src=\"cid:photo\"  id='img' alt='' style='max-height:60px;' height='60' /></td>" +
                 "<td align='center' style='color:#2980b9; vertical-align:bottom;'>" +
                 "</td></tr></table></td></tr>" +
                 "<tr><td align='left' style='padding-left:5px;'>" +
                 Emailbody +
                        "</td></tr>";
                //}

                if (EmailType == "Survey")
                {
                    string surveyUrl = HttpContext.Current.Session["SurveyUrl"].ToString();
                    //string url = ConfigurationManager.AppSettings["SurveyResponse"].ToString() + CompanyGUID + "&CustomerID=" + CustomerID + "&CompanyID=" + CompanyID + "&Email=" + recepientEmail + "&SurveyID=" + SurveyID + "";
                    BodyText = BodyText + "<tr><td> <br/><br/>Would you like to participate in a quick rating ?<br/> <br/> <a href='" + surveyUrl + "'><button  onMouseOver=\"this.style.cursor = 'pointer'\" style='display: inline-block; padding: 6px 12px; margin-bottom: 0; font-size: 14px;  font-weight: normal; " +
                        "line-height: 1.42857143; text-align: center; white-space: nowrap;vertical-align: middle; cursor: pointer; background-color: #6c757d;color:white; border: 1px solid transparent;" +
                        " border-radius: 4px; padding: 10px 16px; '> Start Rating </button></a></td></tr>";

                    HttpContext.Current.Session["SurveyUrl"] = "";
                }
                if (!string.IsNullOrEmpty(wisetackFooter) && wisetackFooter != "0")
                {
                    BodyText = BodyText + "<tr><td> <br/><br/><hr/>" + wisetackFooter + "<br/> <br/></td></tr>";
                }

                BodyText = BodyText + "</table></td></tr></table></body>";
                string attachedfileNames = "";
                using (MailMessage mailMessage = new MailMessage())
                {

                    if (string.IsNullOrEmpty(EmailFrom) || EmailFrom == "0")
                    {
                        EmailFrom = "noreply@" + CompanyID + ".com";
                    }

                    if (!string.IsNullOrEmpty(recepientToEmail))
                    {
                        string[] recepientToList = recepientToEmail.Split(',');
                        foreach (string multiple_email in recepientToList)
                        {

                            mailMessage.To.Add(new MailAddress(multiple_email));
                        }
                    }
                    if (string.IsNullOrEmpty(recepientToEmail))
                    {
                        mailMessage.To.Add(new MailAddress(EmailFrom));
                    }
                    if (!string.IsNullOrEmpty(recepientCCEmail))
                    {
                        string[] recepientCCList = recepientCCEmail.Split(',');
                        foreach (string multiple_email in recepientCCList)
                        {
                            mailMessage.CC.Add(new MailAddress(multiple_email));

                        }
                    }

                    if (!string.IsNullOrEmpty(recepientBCCEmail))
                    {
                        string[] recepientBCCList = recepientBCCEmail.Split(',');
                        foreach (string multiple_email in recepientBCCList)
                        {
                            mailMessage.Bcc.Add(multiple_email);
                        }
                    }



                    if (!string.IsNullOrEmpty(prequalLink))
                    {
                        BodyText = BodyText.Replace("[Prequal]", prequalLink);
                    }
                    else
                    {
                        BodyText = BodyText.Replace("[Prequal]", "");
                    }
                    if (!string.IsNullOrEmpty(prequalLink))
                    {
                        BodyText = BodyText.Replace("[Prequal]", prequalLink);
                    }
                    if (isSendPrequal)
                    {
                        BodyText += "<br/><br/>Prequal Link: " + prequalLink;
                    }
                    mailMessage.From = new MailAddress(EmailFrom);

                    mailMessage.ReplyToList.Add(mailMessage.From);

                    mailMessage.Subject = subject;
                    mailMessage.Body = BodyText;

                    if (!string.IsNullOrEmpty(CompanyLogoFile))
                    {
                        if (File.Exists(LogoPath))
                        {
                            AlternateView htmlview = default(AlternateView);
                            htmlview = AlternateView.CreateAlternateViewFromString(BodyText, null, "text/html");
                            LinkedResource imageResourceEs = new LinkedResource(LogoPath, MediaTypeNames.Image.Jpeg)
                            {
                                ContentId = "photo"
                            };
                            //imageResourceEs.ContentId = "photo";
                            //imageResourceEs.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                            htmlview.LinkedResources.Add(imageResourceEs);
                            mailMessage.AlternateViews.Add(htmlview);
                        }
                        else
                        {
                            mailMessage.Body = BodyText;
                        }
                    }
                    else
                    {
                        mailMessage.Body = BodyText;
                    }
                    //Attaching uploded  document
                    // List<HttpPostedFile> tempAttachments = attachments;
                    if (emailContents != null)
                    {
                        if (emailContents.Count > 0)
                        {
                            foreach (EmailContent f in emailContents)
                            {
                                Attachment attachment = new Attachment(HttpContext.Current.Server.MapPath(f.FileUrl)); //create the attachment
                                mailMessage.Attachments.Add(attachment); //add the attachment
                            }
                        }
                    }

                    if (alternateViews != null && alternateViews.Count > 0)
                    {
                        AlternateView htmlview = default(AlternateView);
                        htmlview = AlternateView.CreateAlternateViewFromString(BodyText, null, "text/html");

                        mailMessage.AlternateViews.Add(htmlview);
                        foreach (AlternateView av in alternateViews)
                        {
                            mailMessage.AlternateViews.Add(av);
                        }
                    }

                    if (HttpContext.Current.Session["EmailHistoryID"] != null)
                    {
                        sql = "select*from tbl_EmailHistoryContent where HistoryID='" + HttpContext.Current.Session["EmailHistoryID"] + "' ;";
                        subject = "Re-send-" + subject;

                        DataTable dt_EmailHistoryContent = new DataTable();

                        db.Execute(sql, out dt_EmailHistoryContent);

                        if (dt_EmailHistoryContent.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt_EmailHistoryContent.Rows)
                            {
                                string FileUrl = "";
                                FileUrl = dr["FileUrl"].ToString();

                                //mailMessage.Attachments.Add(new Attachment(new MemoryStream(filesFromDB), dr["FileName"].ToString(), dr["FileType"].ToString()));
                                mailMessage.Attachments.Add(new Attachment(HttpContext.Current.Server.MapPath(dr["FileUrl"].ToString())));

                                attachedfileNames = attachedfileNames + "," + dr["FileName"].ToString();
                            }
                        }

                        HttpContext.Current.Session.Remove("EmailHistoryID");
                    }

                    mailMessage.IsBodyHtml = true;
                    string emailSentError = "";

                    SmtpClient smtp = new SmtpClient();
                    smtp.EnableSsl = true;
                    System.Net.NetworkCredential NetworkCred = new System.Net.NetworkCredential();

                    EmialSetting emialSetting = GetEmailSetting();
                    smtp.Host = emialSetting.SMTP;
                    NetworkCred.UserName = emialSetting.SmtpUser;
                    NetworkCred.Password = emialSetting.SmtpPassword;
                    smtp.Port = emialSetting.Port;

                    smtp.UseDefaultCredentials = true;
                    smtp.Credentials = NetworkCred;

                    smtp.Send(mailMessage);

                    string emailhistory = "INSERT INTO tbl_EmailHistory(id,CompanyID, CustomerID, Subject, EmailBody, EmailTo, EmailCC, EmailBCC, EmailType, SendBy)VALUES" +
                         "(" +
                         "'" + historyid + "'," +
                         "'" + CompanyID + "'," +
                         "'" + CustomerID + "'," +
                         "'" + subject + "'," +
                         "'" + body.Replace("'", string.Empty) + "'," +
                         "'" + recepientToEmail + "'," +
                         "'" + recepientCCEmail + "'," +
                         "'" + recepientBCCEmail + "'," +
                         "'" + EmailType + "'," +
                         "'" + SendBy + "'" +

                         ")";

                    db.Execute(emailhistory);
                    if (emailContents != null)
                    {
                        if (emailContents.Count > 0)
                        {
                            foreach (EmailContent e in emailContents)
                            {
                                SaveFileContent(historyid, CompanyID, e.FileName, e.FileUrl);
                            }
                        }
                    }
                    //if (attachments.Count > 0)
                    //{
                    //    string FileUrl = "";
                    //    string emailHistoryContent = "";
                    //    foreach (HttpPostedFile file in attachments)
                    //    {
                    //        string filename = Path.GetFileName(file.FileName);

                    //        string FileType = file.ContentType;

                    //        byte[] fileContent = ConvertToBytes(file);
                    //        SaveFileContent(historyid, CompanyID, filename, FileType, fileContent, FileUrl);

                    //    }
                    //    //db.Execute(emailHistoryContent);

                    //}
                }

                return "Sent";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //Nizam
        public string SendHtmlFormattedEmailPdf(string CompanyID, string CustomerID, string EmailType, string subject, string body,
                string recepientToEmail, string recepientCCEmail, string recepientBCCEmail, List<EmailContent> emailContents)
        {
            string SendBy = "From Customer";

            if (HttpContext.Current.Session["LoginUser"] != null)
            {
                SendBy = HttpContext.Current.Session["LoginUser"].ToString();
            }
            //string CompanyAddress = "";
            //string CompanyCity = "";
            //string CompanyState = "";
            //string CompanyZipCode = "";
            //string CompanyPhone = "";
            //string CompanyEmail = "";
            //string CompanyWebsite = "";
            //string CompanyFacebook = "";
            //string CompanyTwitter = "";
            string CompanyLogoFile = "";
            //string CompanyFullName = "";
            //string CompanyGUID = "";
            try
            {
                //string wisetackFooter = "";

                Database db = new Database(connStr);

                string historyid = string.Empty;
                string EmailFrom = string.Empty;
                bool isSendPrequal = false;

                string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + CompanyLogoFile);
                string header = "";

                string BodyText = body.ToString();

                //   wisetackFooter = db.ExecuteScalar("Select [WisetackFooterMsg] from msSchedulerV3.dbo.tbl_CustCommunication where  CompanyID='" + CompanyID + "'");

                string attachedfileNames = "";
                using (MailMessage mailMessage = new MailMessage())
                {
                    if (!string.IsNullOrEmpty(recepientToEmail))
                    {
                        string[] recepientToList = recepientToEmail.Split(',');
                        foreach (string multiple_email in recepientToList)
                        {
                            mailMessage.To.Add(new MailAddress(multiple_email));
                        }
                    }
                    if (!string.IsNullOrEmpty(recepientCCEmail))
                    {
                        string[] recepientCCList = recepientCCEmail.Split(',');
                        foreach (string multiple_email in recepientCCList)
                        {
                            mailMessage.CC.Add(new MailAddress(multiple_email));
                        }
                    }

                    if (!string.IsNullOrEmpty(recepientBCCEmail))
                    {
                        string[] recepientBCCList = recepientBCCEmail.Split(',');
                        foreach (string multiple_email in recepientBCCList)
                        {
                            mailMessage.Bcc.Add(multiple_email);
                        }
                    }

                    if (string.IsNullOrEmpty(EmailFrom) || EmailFrom == "0")
                    {
                        EmailFrom = "noreply@" + CompanyID + ".com";
                    }

                    mailMessage.From = new MailAddress(EmailFrom);
                    mailMessage.Subject = subject;
                    mailMessage.Body = BodyText;

                    if (!string.IsNullOrEmpty(CompanyLogoFile))
                    {
                        if (File.Exists(LogoPath))
                        {
                            AlternateView htmlview = default(AlternateView);
                            htmlview = AlternateView.CreateAlternateViewFromString(BodyText, null, "text/html");
                            LinkedResource imageResourceEs = new LinkedResource(LogoPath, MediaTypeNames.Image.Jpeg)
                            {
                                ContentId = "photo"
                            };
                            //imageResourceEs.ContentId = "photo";
                            //imageResourceEs.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                            htmlview.LinkedResources.Add(imageResourceEs);
                            mailMessage.AlternateViews.Add(htmlview);
                        }
                        else
                        {
                            mailMessage.Body = BodyText;
                        }
                    }
                    else
                    {
                        mailMessage.Body = BodyText;
                    }

                    //Attaching uploded  document
                    // List<HttpPostedFile> tempAttachments = attachments;
                    if (emailContents != null)
                    {
                        if (emailContents.Count > 0)
                        {
                            foreach (EmailContent f in emailContents)
                            {
                                Attachment attachment = new Attachment(HttpContext.Current.Server.MapPath(f.FileUrl)); //create the attachment
                                mailMessage.Attachments.Add(attachment); //add the attachment
                            }
                        }
                    }

                    mailMessage.IsBodyHtml = false;
                    string emailSentError = "";

                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = ConfigurationManager.AppSettings["SMTP"];
                    smtp.EnableSsl = true;
                    System.Net.NetworkCredential NetworkCred = new System.Net.NetworkCredential();

                    EmialSetting emialSetting = GetEmailSetting();
                    smtp.Host = emialSetting.SMTP;
                    NetworkCred.UserName = emialSetting.SmtpUser;
                    NetworkCred.Password = emialSetting.SmtpPassword;
                    smtp.Port = emialSetting.Port;

                    // NetworkCred.UserName = ConfigurationManager.AppSettings["SmtpUser"];
                    //  NetworkCred.Password = ConfigurationManager.AppSettings["SmtpPassword"];
                    smtp.UseDefaultCredentials = true;
                    smtp.Credentials = NetworkCred;
                    //  smtp.Port = int.Parse(ConfigurationManager.AppSettings["Port"]);
                    smtp.Send(mailMessage);

                    string emailhistory = "INSERT INTO tbl_EmailHistory(id,CompanyID, CustomerID, Subject, EmailBody, EmailTo, EmailCC, EmailBCC, EmailType, SendBy)VALUES" +
                         "(" +
                         "'" + historyid + "'," +
                         "'" + CompanyID + "'," +
                         "'" + CustomerID + "'," +
                         "'" + subject + "'," +
                         "'" + body.Replace("'", string.Empty) + "'," +
                         "'" + recepientToEmail + "'," +
                         "'" + recepientCCEmail + "'," +
                         "'" + recepientBCCEmail + "'," +
                         "'" + EmailType + "'," +
                         "'" + SendBy + "'" +

                         ")";

                    db.Execute(emailhistory);
                    if (emailContents != null)
                    {
                        if (emailContents.Count > 0)
                        {
                            foreach (EmailContent e in emailContents)
                            {
                                SaveFileContent(historyid, CompanyID, e.FileName, e.FileUrl);
                            }
                        }
                    }
                    //if (attachments.Count > 0)
                    //{
                    //    string FileUrl = "";
                    //    string emailHistoryContent = "";
                    //    foreach (HttpPostedFile file in attachments)
                    //    {
                    //        string filename = Path.GetFileName(file.FileName);

                    //        string FileType = file.ContentType;

                    //        byte[] fileContent = ConvertToBytes(file);
                    //        SaveFileContent(historyid, CompanyID, filename, FileType, fileContent, FileUrl);

                    //    }
                    //    //db.Execute(emailHistoryContent);

                    //}
                }

                return "Sent Successfully";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void SaveFileContent(string historyid, string CompanyID, string filename, string FileUrl)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "insert into tbl_EmailHistoryContent(HistoryID,CompanyID,FileName,FileUrl) values (@HistoryID,@CompanyID,@FileName,  @FileUrl)";
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.Parameters.AddWithValue("@HistoryID", historyid);
                    cmd.Parameters.AddWithValue("@CompanyID", CompanyID);
                    cmd.Parameters.AddWithValue("@FileName", filename);
                    cmd.Parameters.AddWithValue("@FileUrl", FileUrl);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public byte[] CreatePdfForResource(string ResourceID, string CompanyID, DateTime _StartDate, DateTime _EndDate)
        {
            string Host = "";

            string body = string.Empty;

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

            DateTime EndDate = Convert.ToDateTime(_EndDate).AddDays(-1);
            if (_StartDate > EndDate)
            {
                EndDate = _StartDate;
            }

            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("Get_DateForPdfResource", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@companyId", SqlDbType.VarChar).Value = HttpContext.Current.Session["CompanyID"].ToString();
                    cmd.Parameters.Add("@ResourceID", SqlDbType.VarChar).Value = ResourceID;
                    cmd.Parameters.Add("@StartDate", SqlDbType.VarChar).Value = _StartDate.ToString("yyyy-MM-dd");
                    cmd.Parameters.Add("@EndDate", SqlDbType.VarChar).Value = EndDate.ToString("yyyy-MM-dd");

                    using (SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd))
                    {
                        dataAdapter.Fill(ds);
                    }
                }
            }
            string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + "asdsa");
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }
            int count = 0;
            string info = string.Empty;
            List<ResourceData> resourceDataList = new List<ResourceData>();
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/ResourcePDF.html")))
            {
                body = reader.ReadToEnd();
            }

            body = body.Replace("{image}", LogoPath);
            try
            {
                if (ds != null)
                {
                    DataTable Resources = ds.Tables[0];
                    DataTable Appointment = ds.Tables[1];
                    DataTable ServiceType = ds.Tables[2];
                    DataTable TimeBlocks = ds.Tables[3];

                    int _i = 0;

                    // For Assiments

                    string TechnitianName = string.Empty;
                    foreach (DataRow dataRow in Resources.Rows)
                    {
                        if (dataRow["Id"].ToString().Trim() == ResourceID.Trim())
                        {
                            TechnitianName = dataRow["Name"].ToString().Trim();
                        }
                    }
                    body = body.Replace("{Resourcename}", TechnitianName);

                    foreach (DataRow _AppointmentRow in Appointment.Rows)
                    {
                        info = string.Empty;

                        _i++;

                        foreach (DataRow dataRow in Resources.Rows)
                        {
                            if (dataRow["Id"].ToString().Trim() == _AppointmentRow["ResourceID"].ToString().Trim())
                            {
                                TechnitianName = dataRow["Name"].ToString().Trim();
                            }
                        }
                        body = body.Replace("{Resourcename}", TechnitianName);

                        info += @"<h4 style='text-align:left;margin:0px;'>{DateRange}</h4>";

                        DateTime start = Convert.ToDateTime(_AppointmentRow["StartDateTime"]);
                        DateTime end = Convert.ToDateTime(_AppointmentRow["EndDateTime"]);

                        string ServiceTypeid = _AppointmentRow["ServiceType"].ToString();
                        // start = Convert.ToDateTime(_AppointmentRow["ApptDateTime"]);
                        foreach (DataRow _TimeBlocksRow in TimeBlocks.Rows)
                        {
                            if (_TimeBlocksRow["ID"].ToString().Trim() == _AppointmentRow["TimeSlotId"].ToString().Trim())
                            {
                                //string date = start.ToString("yyyy-MM-dd");
                                //string Time = _TimeBlocksRow["StartTime"].ToString();
                                //start = Convert.ToDateTime(date + " " + Time);
                            }
                        }
                        string ServiceTypeText = string.Empty;
                        foreach (DataRow _ServiceTypeRow in ServiceType.Rows)
                        {
                            if (_ServiceTypeRow["ServiceTypeID"].ToString().Trim() == _AppointmentRow["ServiceType"].ToString().Trim())
                            {
                                //end = start.AddHours(Convert.ToInt32(_ServiceTypeRow["Hour"]));
                                //end = end.AddMinutes(Convert.ToInt32(_ServiceTypeRow["Minute"]));
                                ServiceTypeText = _ServiceTypeRow["ServiceName"].ToString();
                            }
                        }

                        info = info.Replace("{DateRange}", start.ToString("MMMM dd") + " " + start.ToString("hh:mm tt") + " - " + end.ToString("hh:mm tt"));

                        info += @"  <table style='font-size:7px;margin-left:25px;margin-bottom:20px;' role='presentation' cellpadding='0' cellspacing='0' width='100%' border='0'>
                            <thead>
                                <tr>
                                    <th colspan='2' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>&nbsp;</th>
                                </tr>

                                <tr>
                                    <th colspan='2' style='font-size:9px;overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>APPOINTMENT DETAILS</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr>

                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Meeting Type
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {ServiceType}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Subject
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Subject}
                                    </td>

                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Location
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Location}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Objectives
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Objectives}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Staff Notes
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {StaffNotes}
                                    </td>
                                </tr>
                            </tbody>

                            <thead>
                                <tr>
                                    <th colspan='2' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>&nbsp;</th>
                                </tr>

                                <tr>

                                    <th colspan='2' style='font-size:9px;overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>CONTACT INFO</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Name
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Name}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Mobile Phone
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {MobilePhone}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Home
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Home}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Business
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Business}
                                    </td>
                                </tr>

                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Email
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Email}
                                    </td>
                                </tr>
                            </tbody>

                            <thead>
                                <tr>
                                    <th colspan='2' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>&nbsp;</th>
                                </tr>
                                <tr>

                                    <th colspan='2' style='font-size:9px;overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>OTHER DETAILS</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Mobile
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {Mobile}
                                    </td>
                                </tr>
                            </tbody>

                            <thead>
                                <tr>
                                    <th colspan='2' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>&nbsp;</th>
                                </tr>
                                <tr>

                                    <th colspan='2' style='font-size:9px;overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>Installation Details</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td width='40%' style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        Additional Notes:
                                    </td>

                                    <td style='overflow-wrap:break-word;word-break:break-word;vertical-align: top;' align='left'>
                                        {AdditionalNotes}
                                    </td>
                                </tr>
                            </tbody>
                        </table>";

                        string title = "" + _AppointmentRow["FirstName"].ToString() + " " + _AppointmentRow["LastName"].ToString();

                        string FullAddress = string.Empty;

                        if (!string.IsNullOrEmpty(_AppointmentRow["Address1"].ToString()))
                        {
                            FullAddress += _AppointmentRow["Address1"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(_AppointmentRow["City"].ToString()))
                        {
                            FullAddress += _AppointmentRow["City"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(_AppointmentRow["State"].ToString()))
                        {
                            FullAddress += _AppointmentRow["State"].ToString() + ", ";
                        }
                        if (!string.IsNullOrEmpty(_AppointmentRow["ZipCode"].ToString()))
                        {
                            FullAddress += _AppointmentRow["ZipCode"].ToString();
                        }

                        if (FullAddress.EndsWith(", "))
                        {
                            FullAddress = FullAddress.Substring(0, FullAddress.Length - 2);
                        }

                        info = info.Replace("{ServiceType}", ServiceTypeText);
                        info = info.Replace("{Subject}", string.Empty);
                        info = info.Replace("{Location}", FullAddress);
                        info = info.Replace("{Objectives}", string.Empty);
                        info = info.Replace("{StaffNotes}", _AppointmentRow["Note"].ToString().Trim());
                        info = info.Replace("{Name}", title);
                        info = info.Replace("{MobilePhone}", _AppointmentRow["Mobile"].ToString().Trim());
                        info = info.Replace("{Home}", _AppointmentRow["Phone"].ToString().Trim());
                        info = info.Replace("{Business}", string.Empty);
                        info = info.Replace("{Email}", _AppointmentRow["Email"].ToString().Trim());

                        info = info.Replace("{Mobile}", string.Empty);
                        info = info.Replace("{AdditionalNotes}", string.Empty);
                        info = info.Replace("{Business}", string.Empty);
                        info = info.Replace("{Business}", string.Empty);

                        ResourceData resourceData = new ResourceData();
                        resourceData._data = info;
                        resourceData.date = start;
                        resourceDataList.Add(resourceData);
                    }
                }
            }
            catch (Exception ex)
            { }

            string _DATAS = string.Empty;

            List<ResourceData> myList = resourceDataList.OrderBy(x => x.date).ToList();
            foreach (ResourceData resourceData in myList)
            {
                _DATAS += resourceData._data;
            }
            body = body.Replace("{DATA}", _DATAS);

            StringReader sr = new StringReader(body);

            Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);

            HTMLWorker htmlparser = new HTMLWorker(pdfDoc);

            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);

            pdfDoc.Open();

            htmlparser.Parse(sr);
            PdfContentByte cb = writer.DirectContent;
            BaseFont font = BaseFont.CreateFont();

            //int count = 3; // Set the count value to the actual count
            float buttonWidth = 150; // Width of the button
            float buttonHeight = 30; // Height of the button
            float verticalSpacing = 5; // Vertical spacing between buttons

            float initialYPosition = pdfDoc.PageSize.Height * 0.16f; // Initial position at 65% from the top
            float totalHeight = count * (buttonHeight + verticalSpacing); // Total height needed for all buttons
            int c = 0;

            if (c == 1)
            {
                totalHeight -= buttonHeight;
                initialYPosition = pdfDoc.PageSize.Height * 0.16f - totalHeight;
            }
            else
            {
                totalHeight -= (count - 2) * (buttonHeight + verticalSpacing);
                initialYPosition = pdfDoc.PageSize.Height * 0.16f - totalHeight;
            }

            pdfDoc.Close();
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return bytes;
        }

        public byte[] InvoicePdfForResource(string CustomerID, string InvoiceNo, string doctype)
        {
            CustomerID = Common.CleanInput(CustomerID);
            string Host = "";
            string LogoPath = "";
            string body = string.Empty;
            string MailTo = "";
            string ProposalMailSubject = "";
            string ProposalMailBody = "";
            string CompanyLogoFile = "";

            string CompanyAddress = "";
            string CompanyCity = "";
            string CompanyState = "";
            string CompanyZipCode = "";
            string CompanyPhone = "";
            string CompanyEmail = "";
            string CompanyWebsite = "";
            string CompanyFacebook = "";
            string CompanyTwitter = "";
            string CompanyFullName = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = @"SELECT concat (FirstName,' ',LastName)as FullName,CASE WHEN dbo.tbl_Customer.BusinessID = 0 THEN 1 ELSE 2 END as ctype,ISNULL(dbo.tbl_Customer.QboId, N'0') as qboCustID,* FROM[msSchedulerV3].[dbo].[tbl_Customer] Where CustomerId='" + CustomerID + "' and  CompanyID =@CompanyID  ;";

            sSQL += @"SELECT CONVERT(VARCHAR(10), InvoiceDate, 101)as IssueDate,ISNULL(dbo.tbl_Invoice.QboId, N'0') as qboInvID,ISNULL(dbo.tbl_Invoice.QboEstimateId, 0) as qboEstID,* FROM[msSchedulerV3].[dbo].[tbl_Invoice] Where  CustomerId='" + CustomerID + "' and id='" + InvoiceNo + "' and CompnyID =@CompanyID;";

            sSQL += "SELECT inv.*, inv.ItemId AS ID, i.Name AS ItemName FROM msSchedulerV3.dbo.Items as i INNER JOIN " +
                "             msSchedulerV3.dbo.tbl_InvoiceDetails as inv ON i.Id = inv.ItemId and i.CompanyID = inv.CompanyID Where inv.RefId='" + InvoiceNo + "' and  inv.CompanyID =@CompanyID ;";

            DataSet dataSet = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());

            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/ProposalEmailTemplate.html")))
            {
                body = reader.ReadToEnd();
            }

            DataTable customer = dataSet.Tables[0];
            int count = 0;
            if (customer.Rows.Count > 0)
            {
                DataRow dr = customer.Rows[0];

                body = body.Replace("{customerName}", dr["FullName"].ToString());
                body = body.Replace("{address}", dr["Address1"].ToString());
                body = body.Replace("{phone}", dr["Phone"].ToString());
                body = body.Replace("{email}", dr["Email"].ToString());
                DataTable inv = dataSet.Tables[1];

                if (inv.Rows.Count > 0)
                {
                    DataRow dr2 = inv.Rows[0];

                    body = body.Replace("{IssueDate}", dr2["IssueDate"].ToString());
                    body = body.Replace("{InvoiceNo}", dr2["Number"].ToString());
                    body = body.Replace("{InvoiceTotal}", "n/a");
                    body = body.Replace("{SubTotal}", "n/a");
                    body = body.Replace("{Discount}", "n/a");
                    body = body.Replace("{Total}", "n/a");
                    body = body.Replace("{Tax}", "n/a");

                    body = body.Replace("{DocType}", doctype);
                }

                MailTo = dr["Email"].ToString();
                string itemDetails = "";
                if (dataSet.Tables[2].Rows.Count > 0)
                {
                    count = dataSet.Tables[2].Rows.Count;

                    DataTable dt = dataSet.Tables[2];
                    foreach (DataRow d in dt.Rows)
                    {
                        itemDetails += "<tr style='border:1px solid #ddd; padding:.35em;'>" +
                           "<th colspan='3' style='padding: .625em; text-align: center; scope='row'>" + d["ItemName"] + "</th>" +
                           "<td colspan='4' style='padding: .625em; text-align: center;'>" + d["Description"] + "</td>" +
                           "<td colspan='2' style='padding: .625em; text-align: center;'>$ " + "n/a" + "</td>" +
                           "<td colspan='1' style='padding: .625em; text-align: center;'>" + "n/a" + "</td>" +
                           "<td colspan='2' style='padding: .625em; text-align: center;'>$ " + "n/a" + "</td>" +
                           "</tr>";
                    }
                }

                body = body.Replace("{itemRow}", itemDetails);
            }

            body = body.Replace("{CSclicktext}", "");

            StringReader sr = new StringReader(body);

            Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);

            HTMLWorker htmlparser = new HTMLWorker(pdfDoc);

            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);

            pdfDoc.Open();

            htmlparser.Parse(sr);
            PdfContentByte cb = writer.DirectContent;
            BaseFont font = BaseFont.CreateFont();

            //int count = 3; // Set the count value to the actual count
            float buttonWidth = 150; // Width of the button
            float buttonHeight = 30; // Height of the button
            float verticalSpacing = 5; // Vertical spacing between buttons

            float initialYPosition = pdfDoc.PageSize.Height * 0.16f; // Initial position at 65% from the top
            float totalHeight = count * (buttonHeight + verticalSpacing); // Total height needed for all buttons
            int c = 0;

            if (c == 1)
            {
                totalHeight -= buttonHeight;
                initialYPosition = pdfDoc.PageSize.Height * 0.16f - totalHeight;
            }
            else
            {
                totalHeight -= (count - 2) * (buttonHeight + verticalSpacing);
                initialYPosition = pdfDoc.PageSize.Height * 0.16f - totalHeight;
            }

            pdfDoc.Close();
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return bytes;
        }

        public class RoundedCellEvent : IPdfPCellEvent
        {
            private float radius;

            public RoundedCellEvent(float radius)
            {
                this.radius = radius;
            }

            public void CellLayout(PdfPCell cell, Rectangle position, PdfContentByte[] canvases)
            {
                PdfContentByte canvas = canvases[PdfPTable.BACKGROUNDCANVAS];
                canvas.SaveState();
                canvas.SetColorFill(cell.BackgroundColor);
                canvas.RoundRectangle(
                    position.GetLeft(0) + 2, // Adjust for padding
                    position.GetBottom(0) + 2,
                    position.Width - 4,
                    position.Height - 4,
                    radius
                );
                canvas.Fill();
                canvas.RestoreState();
            }
        }
        public byte[] InvoicePdfForASA(string CustomerID, string InvoiceNo, string doctype, string CSPaymentLink, decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);
            string Host = "";
            string LogoPath = "";
            string body = string.Empty;
            string MailTo = "";
            string ProposalMailSubject = "";
            string ProposalMailBody = "";
            string CompanyLogoFile = "";

            string CompanyAddress = "";
            string CompanyCity = "";
            string CompanyState = "";
            string CompanyZipCode = "";
            string CompanyPhone = "";
            string CompanyEmail = "";
            string CompanyWebsite = "";
            string CompanyFacebook = "";
            string CompanyTwitter = "";
            string companyFax = "";
            string CompanyFullName = "";


            if (HttpContext.Current.Session["CompanyType"].ToString() == "LHG")
            {
                var invLHG = InvoicePdfForLHGNew(CustomerID, InvoiceNo, doctype, CSPaymentLink, depoReqAmt);
                return invLHG;
            }
            if (HttpContext.Current.Session["CompanyID"].ToString() == "14557")
            {
                var pgEstimate = InvoicePG(CustomerID, InvoiceNo, doctype, CSPaymentLink, false, depoReqAmt);
                return pgEstimate;
            }
            if (HttpContext.Current.Session["CompanyID"].ToString() == "14570")
            {
                var wtEstimate = InvoiceWT(CustomerID, InvoiceNo, doctype, CSPaymentLink, false, depoReqAmt);
                return wtEstimate;
            }
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = @"SELECT concat (FirstName,' ',LastName)as FullName,CASE WHEN dbo.tbl_Customer.BusinessID = 0 THEN 1 ELSE 2 END as ctype,ISNULL(dbo.tbl_Customer.QboId, N'0') as qboCustID,* FROM[msSchedulerV3].[dbo].[tbl_Customer] Where CustomerId='" + CustomerID + "' and  CompanyID =@CompanyID  ;";

            sSQL += @"SELECT CONVERT(VARCHAR(10), InvoiceDate, 101)as IssueDate,convert(varchar, ExpirationDate, 107) as ExpDate,ISNULL(dbo.tbl_Invoice.QboId, N'0') as qboInvID,ISNULL(dbo.tbl_Invoice.QboEstimateId, 0) as qboEstID,isnull([AmountCollect],0.00) as DepositAmount,Discount,Tax,isNULL(RequestedDepoAmt,0)ReqDepo,(Total- (isnull(AmountCollect,0.00))) as Due,* FROM[msSchedulerV3].[dbo].[tbl_Invoice] Where  CustomerId='" + CustomerID + "' and id='" + InvoiceNo + "' and CompnyID =@CompanyID;";

            sSQL += "SELECT inv.Description,i.Location,inv.Quantity,inv.uPrice, inv.TotalPrice,inv.ItemId AS ID, i.Name AS ItemName FROM msSchedulerV3.dbo.Items as i INNER JOIN " +
                "             msSchedulerV3.dbo.tbl_InvoiceDetails as inv ON i.Id = inv.ItemId and i.CompanyID = inv.CompanyID Where inv.RefId='" + InvoiceNo + "' and  inv.CompanyID =@CompanyID order by CAST(NULLIF(inv.LineNum,'') AS INT) asc ;";

            sSQL += @"select * from [msSchedulerV3].[dbo].[tbl_Company] Where CompanyID =@CompanyID;";

            sSQL += @"SELECT [CompanyID]
                            ,[QuoteDisclaimer]
                          ,[InvoiceDisclaimer]
                          ,[CreatedDateTime]
                      FROM [msSchedulerV3].[dbo].[tbl_DisclaimerSettings] where CompanyID =@CompanyID";

            DataSet dataSet = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());

            string Disclaimer = "";
            DataTable dt_Disclaimer = dataSet.Tables[4];
            if (dt_Disclaimer.Rows.Count > 0)
            {
                DataRow Rs = dt_Disclaimer.Rows[0];
                if (doctype == "Invoice")
                {
                    Disclaimer = Rs["InvoiceDisclaimer"].ToString();
                }
                else
                {
                    Disclaimer = Rs["QuoteDisclaimer"].ToString();
                }
            }

            DataTable dt_Company = new DataTable();
            dt_Company = dataSet.Tables[3];
            string companyLogo = "";
            if (dt_Company.Rows.Count > 0)
            {
                DataRow Rs = dt_Company.Rows[0];
                CompanyAddress = Rs["Address"].ToString();
                CompanyCity = Rs["City"].ToString();
                CompanyState = Rs["State"].ToString();
                CompanyZipCode = Rs["ZipCode"].ToString();
                CompanyPhone = Rs["Phone"].ToString();
                CompanyEmail = Rs["Email"].ToString();
                CompanyWebsite = Rs["Website"].ToString();
                CompanyFacebook = Rs["Facebook"].ToString();
                CompanyTwitter = Rs["Twitter"].ToString();
                CompanyLogoFile = Rs["LogoFile"].ToString();
                CompanyFullName = Rs["CompanyName"].ToString();
                companyLogo = Rs["LogoFile"].ToString();
                companyFax = Rs["Fax"].ToString();
            }

            LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + companyLogo);
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }
            string SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            if (!File.Exists(SignaturePath))
            {
                SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            }

            if (doctype == "Invoice")
            {
                using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/AsaInvocie.html")))
                {
                    body = reader.ReadToEnd();
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/AsaQuote.html")))
                {
                    body = reader.ReadToEnd();
                }
            }

            body = body.Replace("{Disclaimer}", Disclaimer);
            // For Company
            body = body.Replace("{CompanyName}", CompanyFullName);
            body = body.Replace("{companyAddress}", CompanyAddress);
            body = body.Replace("{companyPhone}", CompanyPhone);
            body = body.Replace("{companyFax}", companyFax);
            body = body.Replace("{companyEmail}", CompanyEmail);
            body = body.Replace("{companyEmail}", CompanyEmail);
            body = body.Replace("{Signatureimage}", SignaturePath);

            DataTable customer = dataSet.Tables[0];
            int count = 0;
            if (customer.Rows.Count > 0)
            {
                DataRow dr = customer.Rows[0];

                body = body.Replace("{customerName}", dr["FullName"].ToString());
                body = body.Replace("{address}", dr["Address1"].ToString());
                body = body.Replace("{phone}", dr["Phone"].ToString());
                body = body.Replace("{email}", dr["Email"].ToString());

                body = body.Replace("{Fax}", "");

                body = body.Replace("{image}", LogoPath);

                DataTable inv = dataSet.Tables[1];

                if (inv.Rows.Count > 0)
                {
                    DataRow dr2 = inv.Rows[0];

                    body = body.Replace("{IssueDate}", dr2["IssueDate"].ToString());
                    body = body.Replace("{InvoiceNo}", dr2["Number"].ToString());
                    body = body.Replace("{InvoiceTotal}", dr2["Total"].ToString());
                    body = body.Replace("{SubTotal}", dr2["Subtotal"].ToString());
                    body = body.Replace("{Discount}", dr2["Discount"].ToString());
                    body = body.Replace("{Total}", dr2["Total"].ToString());
                    body = body.Replace("{Tax}", dr2["Tax"].ToString());
                    body = body.Replace("{Deposit}", dr2["DepositAmount"].ToString());
                    body = body.Replace("{BalanceDue}", dr2["Due"].ToString());

                    body = body.Replace("{DocType}", doctype);

                    body = body.Replace("{offerisvalid}", dr2["ExpDate"].ToString());
                    body = body.Replace("{ReqDepo}", depoReqAmt.ToString());
                }

                MailTo = dr["Email"].ToString();
                string itemDetails = "";
                Int32 rowcount = 0;
                if (dataSet.Tables[2].Rows.Count > 0)
                {
                    count = dataSet.Tables[2].Rows.Count;

                    DataTable dt = dataSet.Tables[2];
                    foreach (DataRow d in dt.Rows)
                    {
                        itemDetails += "<tr>" +
                         "<th colspan='3' style='padding: .625em; text-align: center; border-bottom: 1px solid #8a8a8a;'>" + d["ItemName"] + "</th>" +
                         "<td colspan='4' style='padding: .625em; text-align: center; border-bottom: 1px solid #8a8a8a;'>" + d["Description"] + "</td>" +
                         "<td colspan='2' style='padding: .625em; text-align: center; border-bottom: 1px solid #8a8a8a;'>$ " + d["uPrice"] + "</td>" +
                         "<td colspan='1' style='padding: .625em; text-align: center; border-bottom: 1px solid #8a8a8a;'>" + d["Quantity"] + "</td>" +
                         "<td colspan='2' style='padding: .625em; text-align: center; border-bottom: 1px solid #8a8a8a;'>$ " + d["TotalPrice"] + "</td>" +
                         "</tr>";
                    }
                }

                body = body.Replace("{itemRow}", itemDetails);

            }
            //add wisetack Loan Info  into pdf start
            string paylink = "";
            //if (HttpContext.Current.Session["LoanInfo"] != null)
            //{
            //    LoanInfo ln = (LoanInfo)HttpContext.Current.Session["LoanInfo"];
            //    if (!string.IsNullOrEmpty(ln.AsLowAsAmount))
            //    {
            //        body = body.Replace("{AsLowAsAmount}", ln.AsLowAsAmount);
            //    }
            //    else
            //    {
            //        body = body.Replace("{AsLowAsAmount}", "");
            //    }
            //    paylink = ln.PaymentLink;
            //    body = body.Replace("{PaymentLink}", ln.PaymentLink);

            //    body = body.Replace("{wisetackclicktext}", "Click this link to see your financing options");
            //}
            //else
            //{
                body = body.Replace("{AsLowAsAmount}", "");
                body = body.Replace("{wisetackclicktext}", "");
         //   }
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }

            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[WisetackDisclimer]", "	*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our lending partners. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at http://wisetack.com/faqs.");
            }
            else
            {
                body = body.Replace("[WisetackDisclimer]", "");
            }

            StringReader sr = new StringReader(body);

            Document pdfDoc = new Document(PageSize.A4, 20f, 20f, 20f, 10f);

            HTMLWorker htmlparser = new HTMLWorker(pdfDoc);

            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);

            pdfDoc.Open();

            htmlparser.Parse(sr);
            PdfContentByte cb = writer.DirectContent;
            BaseFont font = BaseFont.CreateFont();

            float buttonWidth = 150; // Width of the button
            float buttonHeight = 30; // Height of the button
            float verticalSpacing = 5; // Vertical spacing between buttons

            float initialYPosition = pdfDoc.PageSize.Height * 0.12f; // Initial position at 65% from the top
            float totalHeight = count * (buttonHeight + verticalSpacing); // Total height needed for all buttons
            int c = 0;
            if (!string.IsNullOrEmpty(paylink))
            {
                c += 1;
                Rectangle linkArea2 = new Rectangle(pdfDoc.PageSize.Width - buttonWidth - 30, initialYPosition - buttonHeight, pdfDoc.PageSize.Width - 30, initialYPosition);

                PdfAction action2 = new PdfAction(paylink);

                cb.SetColorFill(new BaseColor(7, 192, 202)); // Button color: #07c0ca
                cb.Rectangle(linkArea2.Left, linkArea2.Bottom, linkArea2.Width, linkArea2.Height);
                cb.Fill();
                cb.SetColorFill(BaseColor.WHITE); // Text color: white
                cb.BeginText();
                cb.SetFontAndSize(font, 12);
                cb.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea2.Left + linkArea2.Width / 2, linkArea2.Bottom + linkArea2.Height / 2, 0);
                cb.EndText();
                PdfAnnotation linkAnnotation1 = PdfAnnotation.CreateLink(writer, linkArea2, PdfAnnotation.HIGHLIGHT_INVERT, action2);
                writer.AddAnnotation(linkAnnotation1);
            }

            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                c += 1;
                //Rectangle linkArea = new Rectangle(pdfDoc.PageSize.Width - buttonWidth - 30, initialYPosition - totalHeight, pdfDoc.PageSize.Width - 30, initialYPosition - totalHeight + buttonHeight);

                //PdfAction action = new PdfAction(CSPaymentLink);

                //cb.SetColorFill(new BaseColor(0x50, 0x78, 0xB0));
                //cb.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                //cb.Fill();
                //cb.SetColorFill(BaseColor.BLACK);
                //cb.BeginText();
                //cb.SetFontAndSize(font, 12);
                //cb.ShowTextAligned(Element.ALIGN_CENTER, "Pay Now", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + linkArea.Height / 2, 0);
                //cb.EndText();

                //PdfAnnotation linkAnnotation2 = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, action);
                //writer.AddAnnotation(linkAnnotation2);

                // [1] create a Chunk with font and colors you want
                var anchor = new Chunk("Click this link to pay now")
                {
                    Font = new Font(
                        Font.FontFamily.HELVETICA, 15,
                        Font.NORMAL,
                        BaseColor.BLACK
                    )
                };

                // [2] set the anchor URL
                anchor.SetAnchor(CSPaymentLink);

                // [3] create a Paragraph with alignment, indentation, etc
                Paragraph p = new Paragraph()
                {
                    Alignment = Element.ALIGN_RIGHT,
                    IndentationLeft = 5
                };
                p.SetLeading(1.1f, 1.1f);

                // [4] add chunk to Paragraph
                p.Add(anchor);

                // [5] add Paragraph to Document
                pdfDoc.Add(p);
            }

            if (c == 1)
            {
                totalHeight -= buttonHeight;
                initialYPosition = pdfDoc.PageSize.Height * 0.18f - totalHeight;
            }
            else
            {
                totalHeight -= (count - 2) * (buttonHeight + verticalSpacing);
                initialYPosition = pdfDoc.PageSize.Height * 0.18f - totalHeight;
            }

            pdfDoc.Close();
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return bytes;
        }

        public byte[] InvoicePdf(string CustomerID, string InvoiceNo, string doctype, string CSPaymentLink, bool IsInvoiceWithoutDiscount, decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);
            string Host = "";
            string LogoPath = "";
            string body = string.Empty;
            string MailTo = "";
            string ProposalMailSubject = "";
            string ProposalMailBody = "";
            string CompanyLogoFile = "";

            string CompanyAddress = "";
            string CompanyCity = "";
            string CompanyState = "";
            string CompanyZipCode = "";
            string CompanyPhone = "";
            string CompanyEmail = "";
            string CompanyWebsite = "";
            string CompanyFacebook = "";
            string CompanyTwitter = "";
            string CompanyFullName = "";

            if (HttpContext.Current.Session["CompanyType"].ToString() == "LHG")
            {
                var lhgEstimate = EstimatePdfLHG(CustomerID, InvoiceNo, doctype, CSPaymentLink, false, depoReqAmt);
                return lhgEstimate;
            }
            if (HttpContext.Current.Session["CompanyID"].ToString() == "14557")
            {
                var pgEstimate = EstimatePG(CustomerID, InvoiceNo, doctype, CSPaymentLink, false, depoReqAmt);
                return pgEstimate;
            }
            if (HttpContext.Current.Session["CompanyID"].ToString() == "14570")
            {
                var pgEstimate = EstimateWT(CustomerID, InvoiceNo, doctype, CSPaymentLink, false, depoReqAmt);
                return pgEstimate;
            }
            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = @"SELECT concat (FirstName,' ',LastName)as FullName,CASE WHEN dbo.tbl_Customer.BusinessID = 0 THEN 1 ELSE 2 END as ctype,ISNULL(dbo.tbl_Customer.QboId, N'0') as qboCustID,* FROM[msSchedulerV3].[dbo].[tbl_Customer] Where CustomerId='" + CustomerID + "' and  CompanyID =@CompanyID  ;";

            sSQL += @"SELECT CONVERT(VARCHAR(10), InvoiceDate, 101)as IssueDate,convert(varchar, ExpirationDate, 107) as ExpDate,ISNULL(dbo.tbl_Invoice.QboId, N'0') as qboInvID,ISNULL(dbo.tbl_Invoice.QboEstimateId, 0) as qboEstID,isnull([AmountCollect],0.00) as DepositAmount,Discount,Tax,isNULL(RequestedDepoAmt,0)ReqDepo,(Total- (isnull(AmountCollect,0.00))) as Due,* FROM[msSchedulerV3].[dbo].[tbl_Invoice] Where  CustomerId='" + CustomerID + "' and id='" + InvoiceNo + "' and CompnyID =@CompanyID;";

            sSQL += "SELECT inv.TotalPrice,inv.Description,inv.Quantity,inv.uPrice, inv.TotalPrice,inv.ItemId AS ID, i.Name AS ItemName FROM msSchedulerV3.dbo.Items as i INNER JOIN " +
                 "             msSchedulerV3.dbo.tbl_InvoiceDetails as inv ON i.Id = inv.ItemId and i.CompanyID = inv.CompanyID Where inv.RefId='" + InvoiceNo + "' and  inv.CompanyID =@CompanyID order by CAST(NULLIF(inv.LineNum,'') AS INT) asc ;";

            sSQL += @"select * from [msSchedulerV3].[dbo].[tbl_Company] Where CompanyID =@CompanyID;";

            sSQL += @"SELECT [CompanyID]
                            ,[QuoteDisclaimer]
                          ,[InvoiceDisclaimer]
                          ,[CreatedDateTime]
                      FROM [msSchedulerV3].[dbo].[tbl_DisclaimerSettings] where CompanyID =@CompanyID";

            DataSet dataSet = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());

            string Disclaimer = "";
            DataTable dt_Disclaimer = dataSet.Tables[4];
            if (dt_Disclaimer.Rows.Count > 0)
            {
                DataRow Rs = dt_Disclaimer.Rows[0];
                if (doctype == "Invoice")
                {
                    Disclaimer = Rs["InvoiceDisclaimer"].ToString();
                }
                else
                {
                    Disclaimer = Rs["QuoteDisclaimer"].ToString();
                }
            }

            if (IsInvoiceWithoutDiscount)
            {
                using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/ProposalEmailTemplateWithoutDiscount.html")))
                {
                    body = reader.ReadToEnd();
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/ProposalEmailTemplate.html")))
                {
                    body = reader.ReadToEnd();
                }
            }

            DataTable dt_Company = new DataTable();
            dt_Company = dataSet.Tables[3];
            string companyLogo = "";
            if (dt_Company.Rows.Count > 0)
            {
                DataRow Rs = dt_Company.Rows[0];
                CompanyAddress = Rs["Address"].ToString();
                CompanyCity = Rs["City"].ToString();
                CompanyState = Rs["State"].ToString();
                CompanyZipCode = Rs["ZipCode"].ToString();
                CompanyPhone = Rs["Phone"].ToString();
                CompanyEmail = Rs["Email"].ToString();
                CompanyWebsite = Rs["Website"].ToString();
                CompanyFacebook = Rs["Facebook"].ToString();
                CompanyTwitter = Rs["Twitter"].ToString();
                CompanyLogoFile = Rs["LogoFile"].ToString();
                CompanyFullName = Rs["CompanyName"].ToString();
                companyLogo = Rs["LogoFile"].ToString();
            }
            LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + companyLogo);
            if (!File.Exists(LogoPath))
            {
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }
            string SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            if (!File.Exists(SignaturePath))
            {
                SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            }

            DataTable customer = dataSet.Tables[0];
            int count = 0;
            if (customer.Rows.Count > 0)
            {
                DataRow dr = customer.Rows[0];

                body = body.Replace("{customerName}", dr["FullName"].ToString());
                body = body.Replace("{address}", dr["Address1"].ToString());
                body = body.Replace("{phone}", dr["Phone"].ToString());
                body = body.Replace("{email}", dr["Email"].ToString());
                body = body.Replace("{image}", LogoPath);
                body = body.Replace("{Signatureimage}", SignaturePath);

                DataTable inv = dataSet.Tables[1];

                if (inv.Rows.Count > 0)
                {
                    DataRow dr2 = inv.Rows[0];

                    body = body.Replace("{IssueDate}", dr2["IssueDate"].ToString());
                    body = body.Replace("{InvoiceNo}", dr2["Number"].ToString());
                    body = body.Replace("{InvoiceTotal}", dr2["Total"].ToString());
                    body = body.Replace("{SubTotal}", dr2["Subtotal"].ToString());
                    body = body.Replace("{Discount}", dr2["Discount"].ToString());
                    body = body.Replace("{Total}", dr2["Total"].ToString());
                    body = body.Replace("{Tax}", dr2["Tax"].ToString());
                    body = body.Replace("{Deposit}", dr2["DepositAmount"].ToString());
                    body = body.Replace("{BalanceDue}", dr2["Due"].ToString());
                    body = body.Replace("{DocType}", doctype);
                    body = body.Replace("{ReqDepo}", depoReqAmt.ToString());
                }
                int rowcount = 0;
                MailTo = dr["Email"].ToString();
                string itemDetails = "";
                if (dataSet.Tables[2].Rows.Count > 0)
                {
                    count = dataSet.Tables[2].Rows.Count;

                    DataTable dt = dataSet.Tables[2];

                    foreach (DataRow d in dt.Rows)
                    {
                        rowcount += 1;
                        Int32 qty = 0;
                        try
                        {
                            qty = Convert.ToInt32(d["Quantity"]);
                        }
                        catch { }

                        itemDetails += "<tr>" +
                                "<td width='28%' style='border:none; min-width:28%;max-width:28%;text-align: left;margin-left:3px;'>" + d["ItemName"] + "</td>" +
                                "<td width='45%' style='border:none; min-width:45%;max-width:45%;text-align: left;margin-left:3px;'>" + d["Description"] + "</td>" +
                                  "<td width='7%' style='border:none; min-width:7%;max-width:7%;text-align: center;'>" + qty + "</td>" +
                                 "<td width='10%' style='border:none; min-width:10%;max-width:10%;text-align: center;'>$" + d["uPrice"] + "</td>" +
                                "<td width='10%' style='border:none; min-width:10%;max-width:10%;text-align: center;'>$" + d["TotalPrice"] + "</td>" +
                                "</tr>";
                    }
                }

                body = body.Replace("{itemRow}", itemDetails);
            }

            //add wisetack Loan Info  into pdf start
            string paylink = "";
          
                body = body.Replace("{AsLowAsAmount}", "");
                body = body.Replace("{wisetackclicktext}", "");
           
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }
            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[Disclimer]", "*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our <a href='https://www.wisetack.com/lenders' style='color:blue'>lending partners</a>. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at  <a href='http://wisetack.com/faqs' style='color:blue'>http://wisetack.com/faqs</a>.");

                body = body.Replace("{CSclicktext}", "Click this link to pay now");
            }
            else
            {
                body = body.Replace("[Disclimer]", Disclaimer);

                body = body.Replace("{CSclicktext}", "");
            }

            StringReader sr = new StringReader(body);

            Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);

            HTMLWorker htmlparser = new HTMLWorker(pdfDoc);

            MemoryStream memoryStream = new MemoryStream();

            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, memoryStream);

            pdfDoc.Open();

            htmlparser.Parse(sr);
            PdfContentByte cb = writer.DirectContent;
            BaseFont font = BaseFont.CreateFont();

            float buttonWidth = 150; // Width of the button
            float buttonHeight = 30; // Height of the button
            float verticalSpacing = 5; // Vertical spacing between buttons

            float initialYPosition = pdfDoc.PageSize.Height * 0.12f; // Initial position at 65% from the top
            float totalHeight = count * (buttonHeight + verticalSpacing); // Total height needed for all buttons
            int c = 0;
            if (!string.IsNullOrEmpty(paylink))
            {
                c += 1;
                Rectangle linkArea2 = new Rectangle(pdfDoc.PageSize.Width - buttonWidth - 30, initialYPosition - buttonHeight, pdfDoc.PageSize.Width - 30, initialYPosition);

                PdfAction action2 = new PdfAction(paylink);

                cb.SetColorFill(new BaseColor(7, 192, 202)); // Button color: #07c0ca
                cb.Rectangle(linkArea2.Left, linkArea2.Bottom, linkArea2.Width, linkArea2.Height);
                cb.Fill();
                cb.SetColorFill(BaseColor.WHITE); // Text color: white
                cb.BeginText();
                cb.SetFontAndSize(font, 12);
                cb.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea2.Left + linkArea2.Width / 2, linkArea2.Bottom + linkArea2.Height / 2, 0);
                cb.EndText();
                PdfAnnotation linkAnnotation1 = PdfAnnotation.CreateLink(writer, linkArea2, PdfAnnotation.HIGHLIGHT_INVERT, action2);
                writer.AddAnnotation(linkAnnotation1);
            }

            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                c += 1;
                //Rectangle linkArea = new Rectangle(pdfDoc.PageSize.Width - buttonWidth - 30, initialYPosition - totalHeight, pdfDoc.PageSize.Width - 30, initialYPosition - totalHeight + buttonHeight);

                //PdfAction action = new PdfAction(CSPaymentLink);

                //cb.SetColorFill(new BaseColor(0x50, 0x78, 0xB0));
                //cb.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                //cb.Fill();
                //cb.SetColorFill(BaseColor.BLACK);
                //cb.BeginText();
                //cb.SetFontAndSize(font, 12);
                //cb.ShowTextAligned(Element.ALIGN_CENTER, "Pay Now", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + linkArea.Height / 2, 0);
                //cb.EndText();

                //PdfAnnotation linkAnnotation2 = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, action);
                //writer.AddAnnotation(linkAnnotation2);

                // [1] create a Chunk with font and colors you want
                var anchor = new Chunk("Click this link to pay now")
                {
                    Font = new Font(
                        Font.FontFamily.HELVETICA, 15,
                        Font.NORMAL,
                        BaseColor.BLACK
                    )
                };

                // [2] set the anchor URL
                anchor.SetAnchor(CSPaymentLink);

                // [3] create a Paragraph with alignment, indentation, etc
                Paragraph p = new Paragraph()
                {
                    Alignment = Element.ALIGN_RIGHT,
                    IndentationLeft = 10
                };
                p.SetLeading(1.1f, 1.1f);

                // [4] add chunk to Paragraph
                p.Add(anchor);

                // [5] add Paragraph to Document
                pdfDoc.Add(p);
            }

            if (c == 1)
            {
                totalHeight -= buttonHeight;
                initialYPosition = pdfDoc.PageSize.Height * 0.18f - totalHeight;
            }
            else
            {
                totalHeight -= (count - 2) * (buttonHeight + verticalSpacing);
                initialYPosition = pdfDoc.PageSize.Height * 0.18f - totalHeight;
            }

            pdfDoc.Close();
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            return bytes;
        }

        public byte[] EstimatePdfLHG(string CustomerID, string InvoiceNo, string doctype, string CSPaymentLink, bool IsInvoiceWithoutDiscount, decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);
            string body = string.Empty;
            string LogoPath = "";
            string SignaturePath = "";
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";

            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, " +
                    "CASE WHEN BusinessID = 0 THEN 1 ELSE 2 END AS ctype, " +
                    "ISNULL(QboId, N'0') AS qboCustID, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "CONVERT(VARCHAR, ExpirationDate, 107) AS ExpDate, " +
                    "ISNULL(QboId, N'0') AS qboInvID, ISNULL(QboEstimateId, 0) AS qboEstID, " +
                    "ISNULL(AmountCollect, 0.00) AS DepositAmount, Discount, Tax, " +
                    "ISNULL(RequestedDepoAmt, 0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect, 0.00)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice, inv.ItemId AS ID, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID " +
                    "ORDER BY CAST(NULLIF(inv.LineNum, '') AS INT) ASC;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            System.Data.DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/LHG_Estimate.html")))
            {
                body = reader.ReadToEnd();
            }


            System.Data.DataRow company = ds.Tables[3].Rows[0];
            string CompanyName = company["CompanyName"].ToString();
            string CompanyAddress = company["Address"].ToString();
            string CompanyPhone = company["Phone"].ToString();
            string CompanyEmail = company["Email"].ToString();
            string companyLogoFile = company["LogoFile"].ToString();

            LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + companyLogoFile);
            if (!File.Exists(LogoPath)) LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");

            SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            if (!File.Exists(SignaturePath)) SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");

            //string base64Logo = Convert.ToBase64String(File.ReadAllBytes(LogoPath));
            body = body.Replace("{image}", LogoPath);
            string base64Signature = Convert.ToBase64String(File.ReadAllBytes(SignaturePath));

            System.Data.DataRow cust = ds.Tables[0].Rows[0];
            string phoneNumber = cust["Phone"].ToString();
            int digitCount = phoneNumber.Count(char.IsDigit);
            if (digitCount > 9)
            {
                phoneNumber = String.Format("({0}) {1}-{2}",
                  cust["Phone"].ToString().Substring(0, 3),
                  cust["Phone"].ToString().Substring(3, 3),
                  cust["Phone"].ToString().Substring(6, 4)
                 );
            }

            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${address}", cust["Address1"].ToString());
            body = body.Replace("${phone}", phoneNumber);
            body = body.Replace("${email}", cust["Email"].ToString());

            //body = body.Replace("${image}", $"data:image/png;base64,{base64Logo}");
            body = body.Replace("${Signatureimage}", $"data:image/png;base64,{base64Signature}");

            string phoneNumberCompany = CompanyPhone;
            int digitCountcom = phoneNumberCompany.Count(char.IsDigit);

            if (digitCountcom > 9)
            {
                phoneNumberCompany = String.Format("({0}) {1}-{2}",
                CompanyPhone.Substring(0, 3),
                CompanyPhone.Substring(3, 3),
                CompanyPhone.Substring(6, 4)
            );
            }

            body = body.Replace("${CompanyName}", CompanyName);
            body = body.Replace("${companyAddress}", CompanyAddress);
            body = body.Replace("${companyPhone}", phoneNumberCompany);
            body = body.Replace("${companyEmail}", CompanyEmail);
            // No icon file loading needed; SVGs are embedded in the HTML template
            //body = body.Replace("${locationIcon}", "");
            //body = body.Replace("${phoneIcon}", "");
            //body = body.Replace("${emailIcon}", "");
            string locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/locationicon.png");
            if (!File.Exists(locationIconPath)) locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/phoneicon.png");
            if (!File.Exists(phoneIconPath)) phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/emailicon.png");
            if (!File.Exists(emailIconPath)) emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");

            // Placeholders with icon file paths
            body = body.Replace("${locationIcon}", locationIconPath);
            body = body.Replace("${phoneIcon}", phoneIconPath);
            body = body.Replace("${emailIcon}", emailIconPath);


            System.Data.DataRow inv = ds.Tables[1].Rows[0];
            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Total}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            string itemDetails = "";
            foreach (System.Data.DataRow row in ds.Tables[2].Rows)
            {
                itemDetails += "<tr>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["ItemName"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["Description"] + "</td>" +
                  "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["Quantity"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>$ " + row["uPrice"] + "</td>" +

                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>$ " + row["TotalPrice"] + "</td>" +
                 "</tr>";
            }
            body = body.Replace("${itemRow}", itemDetails);

            if (HttpContext.Current.Session["LoanInfo"] != null)
            {
               
            }
            else
            {
                body = body.Replace("${AsLowAsAmount}", "");
                body = body.Replace("${wisetackclicktext}", "");
                body = body.Replace("${PaymentLink}", "");
            }

            body = body.Replace("${CSclicktext}", string.IsNullOrEmpty(CSPaymentLink) ? "" : "Click this link to pay now");
            body = body.Replace("{paymentLink}", CSPaymentLink ?? "");

            if (string.IsNullOrEmpty(paylink) && ds.Tables[4].Rows.Count > 0)
            {
                string disclaimer = (doctype == "Invoice") ? ds.Tables[4].Rows[0]["InvoiceDisclaimer"].ToString()
                                                           : ds.Tables[4].Rows[0]["QuoteDisclaimer"].ToString();
                body = body.Replace("[Disclimer]", disclaimer);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                // Add company logo (existing code)
                //if (File.Exists(LogoPath))
                //{
                //    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(LogoPath);
                //    logo.ScaleToFit(50f, 50f);
                //    logo.Alignment = Element.ALIGN_RIGHT;
                //    pdfDoc.Add(logo);
                //}

                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }

                // Add icons directly to the PDF at specific positions
                PdfContentByte cb = writer.DirectContent;
                //string locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/locationicon.png");
                //string phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/phoneicon.png");
                //string emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/emailicon.png");
                //string defaultIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");

                // Log paths for debugging
                System.Diagnostics.Debug.WriteLine("Location Icon Path: " + locationIconPath);
                System.Diagnostics.Debug.WriteLine("Phone Icon Path: " + phoneIconPath);
                System.Diagnostics.Debug.WriteLine("Email Icon Path: " + emailIconPath);
                //System.Diagnostics.Debug.WriteLine("Default Icon Path: " + defaultIconPath);

                // Define positions (adjust these based on your PDF layout)
                //float tableYPosition = 709f; for dev
                //float tableYPosition = 680f; // Vertical position (adjust up/down, e.g., 710f or 730f)  for live
                //float iconSize = 14f; // Icon height/width
                //float addressX = 91f; // Left column (adjust left/right, e.g., 40f or 60f)
                //float phoneX = 235f; // Middle column (adjust left/right, e.g., 200f or 240f)
                //float emailX = 380f; // Right column (adjust left/right, e.g., 360f or 400f)

                //// Add location icon
                //if (File.Exists(locationIconPath))
                //{
                //    iTextSharp.text.Image locationIcon = iTextSharp.text.Image.GetInstance(locationIconPath);
                //    locationIcon.ScaleToFit(iconSize, iconSize);
                //    locationIcon.SetAbsolutePosition(addressX, tableYPosition);
                //    cb.AddImage(locationIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(addressX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}

                //// Add phone icon
                //if (File.Exists(phoneIconPath))
                //{
                //    iTextSharp.text.Image phoneIcon = iTextSharp.text.Image.GetInstance(phoneIconPath);
                //    phoneIcon.ScaleToFit(iconSize, iconSize);
                //    phoneIcon.SetAbsolutePosition(phoneX, tableYPosition);
                //    cb.AddImage(phoneIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(phoneX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}

                //// Add email icon
                //if (File.Exists(emailIconPath))
                //{
                //    iTextSharp.text.Image emailIcon = iTextSharp.text.Image.GetInstance(emailIconPath);
                //    emailIcon.ScaleToFit(iconSize, iconSize);
                //    emailIcon.SetAbsolutePosition(emailX, tableYPosition);
                //    cb.AddImage(emailIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(emailX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}


                // Add payment link (existing code)
                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(7, 192, 202));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }

                // Add "Pay Now" button (existing code)
                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(138, 138, 138));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        public byte[] InvoicePdfForLHGNew(string CustomerID, string InvoiceNo, string doctype, string CSPaymentLink, decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);
            string body = string.Empty;
            string LogoPath = "";
            string SignaturePath = "";
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";

            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, " +
                    "CASE WHEN BusinessID = 0 THEN 1 ELSE 2 END AS ctype, " +
                    "ISNULL(QboId, N'0') AS qboCustID, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "CONVERT(VARCHAR, ExpirationDate, 107) AS ExpDate, " +
                    "ISNULL(QboId, N'0') AS qboInvID, ISNULL(QboEstimateId, 0) AS qboEstID, " +
                    "ISNULL(AmountCollect, 0.00) AS DepositAmount, Discount, Tax, " +
                    "ISNULL(RequestedDepoAmt, 0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect, 0.00)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice, inv.ItemId AS ID, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID " +
                    "ORDER BY CAST(NULLIF(inv.LineNum, '') AS INT) ASC;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            System.Data.DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/LHG_Invoice.html")))
            {
                body = reader.ReadToEnd();
            }

            System.Data.DataRow company = ds.Tables[3].Rows[0];
            string CompanyName = company["CompanyName"].ToString();
            string CompanyAddress = company["Address"].ToString();
            string CompanyPhone = company["Phone"].ToString();
            string CompanyEmail = company["Email"].ToString();
            string companyLogoFile = company["LogoFile"].ToString();

            LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + companyLogoFile);
            if (!File.Exists(LogoPath)) LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");

            SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");
            if (!File.Exists(SignaturePath)) SignaturePath = HttpContext.Current.Server.MapPath("~/images/Signature.PNG");

            //string base64Logo = Convert.ToBase64String(File.ReadAllBytes(LogoPath));
            string base64Signature = Convert.ToBase64String(File.ReadAllBytes(SignaturePath));


            System.Data.DataRow cust = ds.Tables[0].Rows[0];

            string phoneNumber = cust["Phone"].ToString();
            int digitCount = phoneNumber.Count(char.IsDigit);
            if (digitCount > 9)
            {
                phoneNumber = String.Format("({0}) {1}-{2}",
                  cust["Phone"].ToString().Substring(0, 3),
                  cust["Phone"].ToString().Substring(3, 3),
                  cust["Phone"].ToString().Substring(6, 4)
                 );
            }


            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${address}", cust["Address1"].ToString());
            body = body.Replace("${phone}", phoneNumber);
            body = body.Replace("${email}", cust["Email"].ToString());

            body = body.Replace("{image}", LogoPath);
            body = body.Replace("${Signatureimage}", $"data:image/png;base64,{base64Signature}");

            string phoneNumberCompany = CompanyPhone;
            int digitCountcom = phoneNumberCompany.Count(char.IsDigit);

            if (digitCountcom > 9)
            {
                phoneNumberCompany = String.Format("({0}) {1}-{2}",
                CompanyPhone.Substring(0, 3),
                CompanyPhone.Substring(3, 3),
                CompanyPhone.Substring(6, 4)
            );
            }

            body = body.Replace("${CompanyName}", CompanyName);
            body = body.Replace("${companyAddress}", CompanyAddress);
            body = body.Replace("${companyPhone}", phoneNumberCompany);
            body = body.Replace("${companyEmail}", CompanyEmail);
            //// No icon file loading needed; SVGs are embedded in the HTML template
            //body = body.Replace("${locationIcon}", "");
            //body = body.Replace("${phoneIcon}", "");
            //body = body.Replace("${emailIcon}", "");
            // Defining icon paths
            string locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/locationicon.png");
            if (!File.Exists(locationIconPath)) locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/phoneicon.png");
            if (!File.Exists(phoneIconPath)) phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");
            string emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/emailicon.png");
            if (!File.Exists(emailIconPath)) emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png");

            // Placeholders with icon file paths
            body = body.Replace("${locationIcon}", locationIconPath);
            body = body.Replace("${phoneIcon}", phoneIconPath);
            body = body.Replace("${emailIcon}", emailIconPath);


            System.Data.DataRow inv = ds.Tables[1].Rows[0];
            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Total}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            string itemDetails = "";
            foreach (System.Data.DataRow row in ds.Tables[2].Rows)
            {
                itemDetails += "<tr>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["ItemName"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["Description"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>" + row["Quantity"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>$ " + row["uPrice"] + "</td>" +
                 "<td style='text-align: left; border-bottom: 1px solid #8a8a8a;'>$ " + row["TotalPrice"] + "</td>" +
                 "</tr>";
            }
            body = body.Replace("${itemRow}", itemDetails);

            if (HttpContext.Current.Session["LoanInfo"] != null)
            {
               
            }
            else
            {
                body = body.Replace("${AsLowAsAmount}", "");
                body = body.Replace("${wisetackclicktext}", "");
                body = body.Replace("${PaymentLink}", "");
            }

            body = body.Replace("${CSclicktext}", string.IsNullOrEmpty(CSPaymentLink) ? "" : "Click this link to pay now");
            body = body.Replace("{paymentLink}", CSPaymentLink ?? "");

            if (string.IsNullOrEmpty(paylink) && ds.Tables[4].Rows.Count > 0)
            {
                string disclaimer = (doctype == "Invoice") ? ds.Tables[4].Rows[0]["InvoiceDisclaimer"].ToString()
                                                           : ds.Tables[4].Rows[0]["QuoteDisclaimer"].ToString();
                body = body.Replace("[Disclimer]", disclaimer);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                //// Add company logo (existing code)
                //if (File.Exists(LogoPath))
                //{
                //    iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(LogoPath);
                //    logo.ScaleToFit(50f, 50f);
                //    logo.Alignment = Element.ALIGN_RIGHT;
                //    pdfDoc.Add(logo);
                //}

                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }

                // Add icons directly to the PDF at specific positions
                PdfContentByte cb = writer.DirectContent;
                //string locationIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/locationicon.png");
                //string phoneIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/phoneicon.png");
                //string emailIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/emailicon.png");
                /*  string defaultIconPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/default_icon.png")*/
                ;

                // Log paths for debugging
                System.Diagnostics.Debug.WriteLine("Location Icon Path: " + locationIconPath);
                System.Diagnostics.Debug.WriteLine("Phone Icon Path: " + phoneIconPath);
                System.Diagnostics.Debug.WriteLine("Email Icon Path: " + emailIconPath);
                //System.Diagnostics.Debug.WriteLine("Default Icon Path: " + defaultIconPath);

                //// Define positions (adjust these based on your PDF layout)

                ////float tableYPosition = 709f;
                //float tableYPosition = 680f; // Vertical position (adjust up/down, e.g., 710f or 730f)
                //float iconSize = 14f; // Icon height/width
                //float addressX = 91f; // Left column (adjust left/right, e.g., 40f or 60f)
                //float phoneX = 235f; // Middle column (adjust left/right, e.g., 200f or 240f)
                //float emailX = 380f; // Right column (adjust left/right, e.g., 360f or 400f)

                //// Add location icon
                //if (File.Exists(locationIconPath))
                //{
                //    iTextSharp.text.Image locationIcon = iTextSharp.text.Image.GetInstance(locationIconPath);
                //    locationIcon.ScaleToFit(iconSize, iconSize);
                //    locationIcon.SetAbsolutePosition(addressX, tableYPosition);
                //    cb.AddImage(locationIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(addressX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}

                //// Add phone icon
                //if (File.Exists(phoneIconPath))
                //{
                //    iTextSharp.text.Image phoneIcon = iTextSharp.text.Image.GetInstance(phoneIconPath);
                //    phoneIcon.ScaleToFit(iconSize, iconSize);
                //    phoneIcon.SetAbsolutePosition(phoneX, tableYPosition);
                //    cb.AddImage(phoneIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(phoneX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}

                //// Add email icon
                //if (File.Exists(emailIconPath))
                //{
                //    iTextSharp.text.Image emailIcon = iTextSharp.text.Image.GetInstance(emailIconPath);
                //    emailIcon.ScaleToFit(iconSize, iconSize);
                //    emailIcon.SetAbsolutePosition(emailX, tableYPosition);
                //    cb.AddImage(emailIcon);
                //}
                //else if (File.Exists(defaultIconPath))
                //{
                //    iTextSharp.text.Image defaultIcon = iTextSharp.text.Image.GetInstance(defaultIconPath);
                //    defaultIcon.ScaleToFit(iconSize, iconSize);
                //    defaultIcon.SetAbsolutePosition(emailX, tableYPosition);
                //    cb.AddImage(defaultIcon);
                //}


                // Add payment link (existing code)
                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(7, 192, 202));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }

                // Add "Pay Now" button (existing code)
                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(138, 138, 138));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        public void SendText(string sMobile, string customerID, string sSMSText, string sSMSType, string CompanyID, Database db)
        {
            string response = "";
            string strSQL = "";

            try
            {
                TwilioSMSService twilioSMSService = new TwilioSMSService();
                string smsSid = twilioSMSService.SendSMS(sMobile, sSMSText, CompanyID);
               // twilioSMSService.LogSMS(CompanyID, customerID, "", "", sMobile, sSMSType, sSMSText, smsSid);
                // WriteLog("Sending call, PhoneNumber: " + PhoneNumber + " Company: " + CompanyID + " WO: " + WorkOrder);

                //ASCIIEncoding encoding = new ASCIIEncoding();

                //string JPUserID = ConfigurationManager.AppSettings["JPUser"].ToString();
                //string JPPassword = ConfigurationManager.AppSettings["JPPassword"].ToString();
                //string SMSID = "sms" + Guid.NewGuid().ToString().Substring(0, 8);

                //string postData = "USER=" + JPUserID;
                //postData += "&PASSWORD=" + JPPassword;
                //postData += "&PHONE=" + sMobile;
                //postData += "&MESSAGE=" + sSMSText;
                //postData += "&UniID=" + SMSID;

                //byte[] data = encoding.GetBytes(postData);

                //// Prepare web request...
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://cportal.justpressone.com/WebService/SendText/index.cfm");
                //request.Method = "POST";
                //request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = data.Length;

                //Stream newStream = request.GetRequestStream();
                //// Send the data.
                //newStream.Write(data, 0, data.Length);
                //newStream.Close();

                //HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
                //Stream responseStream = httpWebResponse.GetResponseStream();
                //StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                //response = reader.ReadToEnd();

                //responseStream.Close();
                //reader.Close();

                //strSQL = "INSERT INTO tbl_SMSLog " +
                //      "(CompanyID,SMSID,MobileNumber,SMSType,Message,Response) " +
                //      " VALUES (" +
                //     "'" + CompanyID + "'," +
                //     "'" + SMSID + "'," +
                //     "'" + sMobile + "'," +
                //     "'" + sSMSType + "'," +
                //     "'" + sSMSText + "'," +
                //     "'" + response + "')";

                //db.Execute(strSQL);
            }
            catch (Exception ex)
            {
                //Response.Write(ex.Message);
            }
        }

    
        private static DataSet GetInvoiceData(string InvoiceNo, string CustomerID)
        {
            // InvoiceNo = Common.CleanInput(InvoiceNo);
            CustomerID = Common.CleanInput(CustomerID);

            StringBuilder table2 = new StringBuilder();

            Database db = new Database(ConfigurationManager.AppSettings["ConnString"].ToString());

            string sSQL = @"SELECT concat (FirstName,' ',LastName)as FullName,CASE WHEN dbo.tbl_Customer.BusinessID = 0 THEN 1 ELSE 2 END as ctype,ISNULL(dbo.tbl_Customer.QboId, N'0') as qboCustID,* FROM[msSchedulerV3].[dbo].[tbl_Customer] Where CustomerId='" + CustomerID + "' and  CompanyID =@CompanyID  ;";

            sSQL += @"SELECT CONVERT(VARCHAR(10), InvoiceDate, 101)as IssueDate,ISNULL(dbo.tbl_Invoice.QboId, N'0') as qboInvID,ISNULL(dbo.tbl_Invoice.QboEstimateId, 0) as qboEstID,* FROM [msSchedulerV3].[dbo].[tbl_Invoice] Where  CustomerId='" + CustomerID + "' and id='" + InvoiceNo + "' and CompnyID =@CompanyID;";

            sSQL += "SELECT inv.*, inv.ItemId AS ID, i.Name AS ItemName FROM msSchedulerV3.dbo.Items as i INNER JOIN " +
                "             msSchedulerV3.dbo.tbl_InvoiceDetails as inv ON i.Id = inv.ItemId and i.CompanyID = inv.CompanyID Where inv.RefId='" + InvoiceNo + "' and  inv.CompanyID =@CompanyID order by CAST(NULLIF(inv.LineNum,'') AS INT) asc;";

            DataSet dataSet = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());

            return dataSet;
        }


        public byte[] EstimatePG(string CustomerID, string InvoiceNo, string doctype,
                         string CSPaymentLink, bool IsInvoiceWithoutDiscount,
                         decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);

            string body = string.Empty;
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";
            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "ISNULL(AmountCollect,0) AS DepositAmount, " +
                    "ISNULL(RequestedDepoAmt,0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect,0)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice,inv.servicedate, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/PGEstimateForm.html")))
            {
                body = reader.ReadToEnd();
            }

            #region Company Data

            DataRow company = ds.Tables[3].Rows[0];

            string companyName = company["CompanyName"].ToString();
            string companyAddress = company["Address"].ToString();
            string companyPhone = company["Phone"].ToString();
            string companyEmail = company["Email"].ToString();
            string logoFile = company["LogoFile"].ToString();
            string comapnyCitystate = company["City"].ToString() + ", " + company["State"].ToString() + ", " + company["Zipcode"].ToString();
            string companyWebsite = company["WebSite"].ToString();


            string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + logoFile);
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }


            body = body.Replace("{image}", LogoPath);


            body = body.Replace("${CompanyName}", companyName);
            body = body.Replace("${companyAddress}", companyAddress);
            body = body.Replace("${companyPhone}", companyPhone);
            body = body.Replace("${companyEmail}", companyEmail);
            body = body.Replace("${CompanyCityStateZip}", comapnyCitystate);
            body = body.Replace("${CompanyCountry}", "USA");
            body = body.Replace("${CompanyWebsite}", companyWebsite);

            #endregion

            #region Customer Data

            DataRow cust = ds.Tables[0].Rows[0];

            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${CustomerAddress}", cust["Address1"].ToString());
            body = body.Replace("${phone}", cust["Phone"].ToString());
            body = body.Replace("${email}", cust["Email"].ToString());
            body = body.Replace("${CustomerCityStateZip}", cust["City"].ToString() + ", " + cust["State"].ToString() + ", " + cust["Zipcode"].ToString());
            body = body.Replace("${CustomerCountry}", "USA");

            #endregion

            #region Invoice Data

            DataRow inv = ds.Tables[1].Rows[0];

            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            #endregion



            string itemRows = "";

            foreach (DataRow row in ds.Tables[2].Rows)
            {
                itemRows += "<tr>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["Description"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='center'>" + row["Quantity"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["uPrice"]).ToString("N2") + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + "</td>" +
                    "</tr>";
            }

            body = body.Replace("${itemRow}", itemRows);


           
                body = body.Replace("{AsLowAsAmount}", "");
                body = body.Replace("{wisetackclicktext}", "");
           
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }

            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[WisetackDisclimer]", "	*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our lending partners. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at http://wisetack.com/faqs.");
            }
            else
            {
                body = body.Replace("[WisetackDisclimer]", "");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();


                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }
                PdfContentByte cb = writer.DirectContent;

                ;



                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(255, 69, 0));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }


                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(0, 123, 255));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        public byte[] InvoicePG(string CustomerID, string InvoiceNo, string doctype,
                         string CSPaymentLink, bool IsInvoiceWithoutDiscount,
                         decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);

            string body = string.Empty;
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";
            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "ISNULL(AmountCollect,0) AS DepositAmount, " +
                    "ISNULL(RequestedDepoAmt,0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect,0)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice,inv.servicedate, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/PG_Form.html")))
            {
                body = reader.ReadToEnd();
            }

            #region Company Data

            DataRow company = ds.Tables[3].Rows[0];

            string companyName = company["CompanyName"].ToString();
            string companyAddress = company["Address"].ToString();
            string companyPhone = company["Phone"].ToString();
            string companyEmail = company["Email"].ToString();
            string logoFile = company["LogoFile"].ToString();
            string comapnyCitystate = company["City"].ToString() + ", " + company["State"].ToString() + ", " + company["Zipcode"].ToString();
            string companyWebsite = company["WebSite"].ToString();


            string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + logoFile);
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }


            body = body.Replace("{image}", LogoPath);


            body = body.Replace("${CompanyName}", companyName);
            body = body.Replace("${companyAddress}", companyAddress);
            body = body.Replace("${companyPhone}", companyPhone);
            body = body.Replace("${companyEmail}", companyEmail);
            body = body.Replace("${CompanyCityStateZip}", comapnyCitystate);
            body = body.Replace("${CompanyCountry}", "USA");
            body = body.Replace("${CompanyWebsite}", companyWebsite);

            #endregion

            #region Customer Data

            DataRow cust = ds.Tables[0].Rows[0];

            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${CustomerAddress}", cust["Address1"].ToString());
            body = body.Replace("${phone}", cust["Phone"].ToString());
            body = body.Replace("${email}", cust["Email"].ToString());
            body = body.Replace("${CustomerCityStateZip}", cust["City"].ToString() + ", " + cust["State"].ToString() + ", " + cust["Zipcode"].ToString());
            body = body.Replace("${CustomerCountry}", "USA");

            #endregion

            #region Invoice Data

            DataRow inv = ds.Tables[1].Rows[0];

            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            #endregion



            string itemRows = "";

            foreach (DataRow row in ds.Tables[2].Rows)
            {
                itemRows += "<tr>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["Description"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='center'>" + row["Quantity"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["uPrice"]).ToString("N2") + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + "</td>" +
                    "</tr>";
            }

            body = body.Replace("${itemRow}", itemRows);


        
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }

            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[WisetackDisclimer]", "	*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our lending partners. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at http://wisetack.com/faqs.");
            }
            else
            {
                body = body.Replace("[WisetackDisclimer]", "");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();


                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }
                PdfContentByte cb = writer.DirectContent;

                ;



                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(255, 69, 0));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }


                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(138, 138, 138));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }
        public byte[] InvoiceWT(string CustomerID, string InvoiceNo, string doctype,
                         string CSPaymentLink, bool IsInvoiceWithoutDiscount,
                         decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);

            string body = string.Empty;
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";
            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "ISNULL(AmountCollect,0) AS DepositAmount, " +
                    "ISNULL(RequestedDepoAmt,0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect,0)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice,inv.servicedate, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/WTInvForm.html")))
            {
                body = reader.ReadToEnd();
            }

            #region Company Data

            DataRow company = ds.Tables[3].Rows[0];

            string companyName = company["CompanyName"].ToString();
            string companyAddress = company["Address"].ToString();
            string companyPhone = company["Phone"].ToString();
            string companyEmail = company["Email"].ToString();
            string logoFile = company["LogoFile"].ToString();
            string comapnyCitystate = company["City"].ToString() + ", " + company["State"].ToString() + ", " + company["Zipcode"].ToString();
            string companyWebsite = company["WebSite"].ToString();


            string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + logoFile);
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }


            body = body.Replace("{image}", LogoPath);


            body = body.Replace("${CompanyName}", companyName);
            body = body.Replace("${companyAddress}", companyAddress);
            body = body.Replace("${companyPhone}", companyPhone);
            body = body.Replace("${companyEmail}", companyEmail);
            body = body.Replace("${CompanyCityStateZip}", comapnyCitystate);
            body = body.Replace("${CompanyCountry}", "USA");
            body = body.Replace("${CompanyWebsite}", companyWebsite);

            #endregion

            #region Customer Data

            DataRow cust = ds.Tables[0].Rows[0];

            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${CustomerAddress}", cust["Address1"].ToString());
            body = body.Replace("${phone}", cust["Phone"].ToString());
            body = body.Replace("${email}", cust["Email"].ToString());
            body = body.Replace("${CustomerCityStateZip}", cust["City"].ToString() + ", " + cust["State"].ToString() + ", " + cust["Zipcode"].ToString());
            body = body.Replace("${CustomerCountry}", "USA");

            #endregion

            #region Invoice Data

            DataRow inv = ds.Tables[1].Rows[0];

            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            #endregion



            string itemRows = "";

            foreach (DataRow row in ds.Tables[2].Rows)
            {
                itemRows += "<tr>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["ItemName"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["Description"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='center'>" + row["Quantity"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["uPrice"]).ToString("N2") + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + "</td>" +
                    "</tr>";
            }

            body = body.Replace("${itemRow}", itemRows);


          
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }

            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[WisetackDisclimer]", "	*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our lending partners. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at http://wisetack.com/faqs.");
            }
            else
            {
                body = body.Replace("[WisetackDisclimer]", "");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();


                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }
                PdfContentByte cb = writer.DirectContent;

                ;



                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(255, 69, 0));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }


                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(138, 138, 138));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }
        public byte[] EstimateWT(string CustomerID, string InvoiceNo, string doctype,
                       string CSPaymentLink, bool IsInvoiceWithoutDiscount,
                       decimal depoReqAmt = 0)
        {
            CustomerID = Common.CleanInput(CustomerID);

            string body = string.Empty;
            string paylink = "";

            string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
            Database db = new Database(connStr);

            string sSQL = "";
            sSQL += "SELECT CONCAT(FirstName, ' ', LastName) AS FullName, * " +
                    "FROM msSchedulerV3.dbo.tbl_Customer WHERE CustomerId='" + CustomerID + "' AND CompanyID = @CompanyID;";

            sSQL += "SELECT CONVERT(VARCHAR(10), InvoiceDate, 101) AS IssueDate, " +
                    "ISNULL(AmountCollect,0) AS DepositAmount, " +
                    "ISNULL(RequestedDepoAmt,0) AS ReqDepo, " +
                    "(Total - ISNULL(AmountCollect,0)) AS Due, * " +
                    "FROM msSchedulerV3.dbo.tbl_Invoice WHERE CustomerId='" + CustomerID + "' AND id='" + InvoiceNo + "' AND CompnyID = @CompanyID;";

            sSQL += "SELECT inv.TotalPrice, inv.Description, inv.Quantity, inv.uPrice,inv.servicedate, " +
                    "i.Name AS ItemName FROM msSchedulerV3.dbo.Items i " +
                    "INNER JOIN msSchedulerV3.dbo.tbl_InvoiceDetails inv ON i.Id = inv.ItemId AND i.CompanyID = inv.CompanyID " +
                    "WHERE inv.RefId='" + InvoiceNo + "' AND inv.CompanyID = @CompanyID;";

            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_Company WHERE CompanyID = @CompanyID;";
            sSQL += "SELECT * FROM msSchedulerV3.dbo.tbl_DisclaimerSettings WHERE CompanyID = @CompanyID;";

            DataSet ds = db.Get_DataSet(sSQL, HttpContext.Current.Session["CompanyID"].ToString());


            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/WTEstForm.html")))
            {
                body = reader.ReadToEnd();
            }

            #region Company Data

            DataRow company = ds.Tables[3].Rows[0];

            string companyName = company["CompanyName"].ToString();
            string companyAddress = company["Address"].ToString();
            string companyPhone = company["Phone"].ToString();
            string companyEmail = company["Email"].ToString();
            string logoFile = company["LogoFile"].ToString();
            string comapnyCitystate = company["City"].ToString() + ", " + company["State"].ToString() + ", " + company["Zipcode"].ToString();
            string companyWebsite = company["WebSite"].ToString();


            string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + logoFile);
            if (!File.Exists(LogoPath))
            {
                LogoPath = "crv_sched/img/logo/central.png";
                LogoPath = HttpContext.Current.Server.MapPath("~/crv_sched/img/logo/central.png");
            }


            body = body.Replace("{image}", LogoPath);


            body = body.Replace("${CompanyName}", companyName);
            body = body.Replace("${companyAddress}", companyAddress);
            body = body.Replace("${companyPhone}", companyPhone);
            body = body.Replace("${companyEmail}", companyEmail);
            body = body.Replace("${CompanyCityStateZip}", comapnyCitystate);
            body = body.Replace("${CompanyCountry}", "USA");
            body = body.Replace("${CompanyWebsite}", companyWebsite);

            #endregion

            #region Customer Data

            DataRow cust = ds.Tables[0].Rows[0];

            body = body.Replace("${customerName}", cust["FullName"].ToString());
            body = body.Replace("${CustomerAddress}", cust["Address1"].ToString());
            body = body.Replace("${phone}", cust["Phone"].ToString());
            body = body.Replace("${email}", cust["Email"].ToString());
            body = body.Replace("${CustomerCityStateZip}", cust["City"].ToString() + ", " + cust["State"].ToString() + ", " + cust["Zipcode"].ToString());
            body = body.Replace("${CustomerCountry}", "USA");

            #endregion

            #region Invoice Data

            DataRow inv = ds.Tables[1].Rows[0];

            body = body.Replace("${IssueDate}", inv["IssueDate"].ToString());
            body = body.Replace("${InvoiceNo}", inv["Number"].ToString());
            body = body.Replace("${InvoiceTotal}", Convert.ToDecimal(inv["Total"]).ToString("N2"));
            body = body.Replace("${SubTotal}", Convert.ToDecimal(inv["Subtotal"]).ToString("N2"));
            body = body.Replace("${Discount}", Convert.ToDecimal(inv["Discount"]).ToString("N2"));
            body = body.Replace("${Tax}", Convert.ToDecimal(inv["Tax"]).ToString("N2"));
            body = body.Replace("${Deposit}", Convert.ToDecimal(inv["DepositAmount"]).ToString("N2"));
            body = body.Replace("${BalanceDue}", Convert.ToDecimal(inv["Due"]).ToString("N2"));
            body = body.Replace("${ReqDepo}", depoReqAmt.ToString("N2"));

            #endregion



            string itemRows = "";

            foreach (DataRow row in ds.Tables[2].Rows)
            {
                itemRows += "<tr>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["ItemName"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;'>" + row["Description"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='center'>" + row["Quantity"] + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["uPrice"]).ToString("N2") + "</td>" +
                    "<td style='border-bottom:1px solid #ccc;' align='right'>$" + Convert.ToDecimal(row["TotalPrice"]).ToString("N2") + "</td>" +
                    "</tr>";
            }

            body = body.Replace("${itemRow}", itemRows);


          
            //add wisetack Loan Info  into pdf end
            if (!string.IsNullOrEmpty(CSPaymentLink))
            {
                body = body.Replace("{CSclicktext}", "");
            }
            else
            {
                body = body.Replace("{CSclicktext}", "");
            }

            if (!string.IsNullOrEmpty(paylink))
            {
                body = body.Replace("[WisetackDisclimer]", "	*All financing is subject to credit approval. Your terms may vary. Payment options through Wisetack are provided by our lending partners. For example, a $1,200 purchase could cost $104.89 a month for 12 months, based on an 8.9% APR, or $400 a month for 3 months, based on a 0% APR. Offers range from 0-35.9% APR based on creditworthiness. No other financing charges or participation fees. See additional terms at http://wisetack.com/faqs.");
            }
            else
            {
                body = body.Replace("[WisetackDisclimer]", "");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 10f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();


                // Parse the HTML content
                using (StringReader sr = new StringReader(body))
                {
                    iTextSharp.tool.xml.XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                }
                PdfContentByte cb = writer.DirectContent;

                ;



                if (!string.IsNullOrEmpty(paylink))
                {
                    PdfContentByte cbLink = writer.DirectContent;
                    BaseFont font = BaseFont.CreateFont();
                    iTextSharp.text.Rectangle linkArea = new iTextSharp.text.Rectangle(pdfDoc.PageSize.Width - 200, 720, pdfDoc.PageSize.Width - 40, 750);
                    cbLink.SetColorFill(new BaseColor(255, 69, 0));
                    cbLink.Rectangle(linkArea.Left, linkArea.Bottom, linkArea.Width, linkArea.Height);
                    cbLink.Fill();
                    cbLink.SetColorFill(BaseColor.WHITE);
                    cbLink.BeginText();
                    cbLink.SetFontAndSize(font, 12);
                    cbLink.ShowTextAligned(Element.ALIGN_CENTER, "See Financing Options", linkArea.Left + linkArea.Width / 2, linkArea.Bottom + 8, 0);
                    cbLink.EndText();
                    PdfAnnotation annotation = PdfAnnotation.CreateLink(writer, linkArea, PdfAnnotation.HIGHLIGHT_INVERT, new PdfAction(paylink));
                    writer.AddAnnotation(annotation);
                }


                if (!string.IsNullOrEmpty(CSPaymentLink))
                {
                    Font instructionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, new BaseColor(138, 138, 138));
                    Paragraph instruction = new Paragraph("To approve this invoice and pay the amount due, click the button below.", instructionFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 5f,
                        SpacingBefore = 15f
                    };
                    instruction.SetLeading(1.2f, 1.2f);
                    pdfDoc.Add(instruction);

                    PdfPTable table = new PdfPTable(1);
                    table.TotalWidth = 120f;
                    table.LockedWidth = true;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SpacingBefore = 5f;
                    table.SpacingAfter = 10f;

                    Font linkFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
                    Chunk linkChunk = new Chunk("Pay Now", linkFont);
                    linkChunk.SetAnchor(CSPaymentLink);

                    PdfPCell cell = new PdfPCell(new Phrase(linkChunk))
                    {
                        BackgroundColor = new BaseColor(255, 69, 0),
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8f,
                        FixedHeight = 30f
                    };

                    cell.CellEvent = new RoundedCellEvent(10f);
                    table.AddCell(cell);
                    pdfDoc.Add(table);
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }
    }
}

public class ResourceData
{
    public string _data { get; set; }
    public DateTime date { get; set; }
}
public class EmialSetting
{
    public string SMTP { get; set; }
    public Int32 Port { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPassword { get; set; }

}
