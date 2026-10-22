
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using TPM;
using static Common;

namespace CECPro.Processor
{
    public class Emailer
    {
        static string connStr = ConfigurationManager.AppSettings["ConnStrSch"].ToString();
        string EmailFrom = string.Empty;
        public string FullAddress = string.Empty;
        public string ResourcEmail = string.Empty;
        public string ResourcMobile = string.Empty;
        string EmailTemplate = string.Empty;
        string CompanyFullAddress = string.Empty;
        string CompanyFullName = string.Empty;
       public string AppointmentRescheduleSubject = string.Empty;
        string AppointmentRescheduleBody = string.Empty;

        Database db = new Database(connStr);
        string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
        string LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/") ;
        string baseUrl = ConfigurationManager.AppSettings["baseurl"].ToString();
        public DataTable dt_Company = new DataTable();
        public DataTable dt_Customer = new DataTable();
        public DataTable dt_Appointment = new DataTable();
        public DataTable dt_CustCommunication = new DataTable();
        public DataTable dt_Resources = new DataTable();
        public DataTable dt_Invocie = new DataTable();


        public bool GetEmailBody(EmailType EmailType,
            string CustomerID,
            string AppoinmnetID,
            string ResourceID,
            string InvoiceID)
        {

           

            string Sql = @"SELECT [CompanyName],[CompanyTag],[Address],[City],[State],[ZipCode],[Phone],[Fax],[Email]
                                  ,[Website],[Facebook],[Twitter],[LogoFile],[CompanyGUID] 
                                  FROM [msSchedulerV3].[dbo].[tbl_Company] where CompanyID = @CompanyID;";

            Sql += @"SELECT  Concat(FirstName,' ', LastName) as FullName, [FirstName],[LastName],[Title],[JobTitle],[Address1],[Address2],[City]
                              ,[State],[ZipCode],[Phone],[Mobile],[Email],[Notes]
                          FROM [msSchedulerV3].[dbo].[tbl_Customer] where CompanyID = @CompanyID AND CustomerID='" + CustomerID + "';";

            Sql += @"SELECT [TimeSlotId],[TimeSlot],[ServiceType],CONVERT(VARCHAR(10), ApptDateTime, 101) as AppointmentDate,[Note],[Status],[CreatedDateTime]
                      FROM [msSchedulerV3].[dbo].[tbl_Appointment] where CompanyID = @CompanyID AND [ApptID] = '" + AppoinmnetID + "';";


             Sql += @"select [SMSAck]
                          ,[SMSAckText]
                          ,[SMSConfirm]
                          ,[SMSConfirmText]
                          ,[EmaiAck]
                          ,[EmailAckText]
                          ,[EmailConfirm]
                          ,[EmailConfirmText]
                          ,[EmailFrom]
                          ,[EmailCC]
                          ,[EmailBCC]
                          ,[ProposalMailSubject]
                          ,[ProposalMailBody]
                          ,[StandardMailSubject]
                          ,[StandardMailBody]
                          ,[InvoiceMailSubject]
                          ,[InvoiceMailBody]
                          ,[AppointmentRescheduleSubject]
                          ,[AppointmentRescheduleBody] FROM [msSchedulerV3].[dbo].[tbl_CustCommunication] where CompanyID = @CompanyID;";

            Sql += @"SELECT [Mobile],[email]
                    FROM [msSchedulerV3].[dbo].[tbl_Resources] where CompanyID =@CompanyID AND ID='" + ResourceID + "';";

           Sql += @"select CONVERT(VARCHAR(10), InvoiceDate, 101) as InvoiceDate,* from tbl_Invoice where CompnyID = @CompanyID and  id ='" + InvoiceID + "'";


            DataTable _ServiceType = new DataTable();
            DataSet dataSet = db.Get_DataSet(Sql, CompanyID);

            dt_Company = dataSet.Tables[0];
             dt_Customer = dataSet.Tables[1];
             dt_Appointment = dataSet.Tables[2];
             dt_CustCommunication = dataSet.Tables[3];
            dt_Resources = dataSet.Tables[4];
            dt_Invocie = dataSet.Tables[5];
            
            if (dt_Resources.Rows.Count > 0)
            {
                ResourcEmail = dt_Resources.Rows[0]["email"].ToString();
                ResourcMobile = dt_Resources.Rows[0]["Mobile"].ToString();
            }
            if (dt_CustCommunication.Rows.Count > 0)
            {
               
                AppointmentRescheduleSubject = dt_CustCommunication.Rows[0]["AppointmentRescheduleSubject"].ToString();
                AppointmentRescheduleBody = dt_CustCommunication.Rows[0]["AppointmentRescheduleBody"].ToString();
            }

            if (string.IsNullOrEmpty(dt_CustCommunication.Rows[0]["EmailFrom"].ToString()) || dt_CustCommunication.Rows[0]["EmailFrom"].ToString() == "0")
            {
                EmailFrom = "noreply@" + CompanyID + ".com";

            }
            else
            {
                EmailFrom = dt_CustCommunication.Rows[0]["EmailFrom"].ToString();
            }

            if (!string.IsNullOrEmpty(dt_Customer.Rows[0]["Address1"].ToString()))
            {
                FullAddress += dt_Customer.Rows[0]["Address1"].ToString() + ", ";
            }
            if (!string.IsNullOrEmpty(dt_Customer.Rows[0]["City"].ToString()))
            {
                FullAddress += dt_Customer.Rows[0]["City"].ToString() + ", ";
            }
            if (!string.IsNullOrEmpty(dt_Customer.Rows[0]["State"].ToString()))
            {
                FullAddress += dt_Customer.Rows[0]["State"].ToString() + ", ";
            }
            if (!string.IsNullOrEmpty(dt_Customer.Rows[0]["ZipCode"].ToString()))
            {
                FullAddress += dt_Customer.Rows[0]["ZipCode"].ToString();
            }

            if (FullAddress.EndsWith(", "))
            {
                FullAddress = FullAddress.Substring(0, FullAddress.Length - 2);
            }
            string LogoUrl = string.Empty;

            if(!string.IsNullOrEmpty(dt_Company.Rows[0]["LogoFile"].ToString()))
            {
                LogoUrl = baseUrl + "/CompanyLogo/" + dt_Company.Rows[0]["LogoFile"].ToString();
                LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + dt_Company.Rows[0]["LogoFile"].ToString());
            }
            else
            {
                LogoUrl = baseUrl + "/CompanyLogo/xceleran.jpg";
                 LogoPath = HttpContext.Current.Server.MapPath("~/CompanyLogo/" + dt_Company.Rows[0]["LogoFile"].ToString());
            }

            string twitterUrl = "#";
            if (!string.IsNullOrEmpty(dt_Company.Rows[0]["Twitter"].ToString()))
            {
                twitterUrl = dt_Company.Rows[0]["Twitter"].ToString();
            }

            string facebookUrl = "#";
            if (!string.IsNullOrEmpty(dt_Company.Rows[0]["Facebook"].ToString()))
            {
                facebookUrl = dt_Company.Rows[0]["Facebook"].ToString();
            }
            string WebsiteUrl = "#";
            if (!string.IsNullOrEmpty(dt_Company.Rows[0]["Website"].ToString()))
            {
                WebsiteUrl = dt_Company.Rows[0]["Website"].ToString();
            }

            
           
            //using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("~/EmailTemplate.html")))
            //{
            //    EmailTemplate = reader.ReadToEnd();
            //}
           
              



           // EmailTemplate.Replace("cid:photo", LogoUrl);
            EmailTemplate.Replace("{twitterUrl}", twitterUrl);
            EmailTemplate.Replace("{facebookUrl}", facebookUrl);
            EmailTemplate.Replace("{WebsiteUrl}", WebsiteUrl);



            
            switch (EmailType)
            {
                case EmailType.ResourceEmail:
                    
                    break;



                default:
                    break;
                  
            }
            return true;
        }
        public void SentCustomerAcknowledgeMail()
        {
            bool EmaiAck = false;
           
            string EmailAckText = string.Empty;
            string EmailTO = dt_Customer.Rows[0]["Email"].ToString();
            string EmailCC = string.Empty;
            string EmailBCC = string.Empty;
          

            if (dt_CustCommunication.Rows.Count > 0)
            {

                if(dt_CustCommunication.Rows[0]["EmaiAck"] != null)
                {
                    EmaiAck = Convert.ToBoolean(dt_CustCommunication.Rows[0]["EmaiAck"].ToString());
                    EmailAckText = dt_CustCommunication.Rows[0]["EmailAckText"].ToString();
                     EmailCC = dt_CustCommunication.Rows[0]["EmailCC"].ToString();
                     EmailBCC = dt_CustCommunication.Rows[0]["EmailBCC"].ToString();
                   

                }
              
            }



            if (EmaiAck && !string.IsNullOrEmpty(EmailAckText) && !string.IsNullOrEmpty(EmailTO))
            {
               List<EmailContent> emailContents = new List<EmailContent>();
                SentEmail("Your Appointment Request",
                 EmailAckText,
                 EmailTO,
                 EmailCC,
                 EmailBCC,
                 emailContents);
            }
        }
        public string GetRescheduleEmailBody()
        {
            string str = AppointmentRescheduleBody;
            DataRow dataRow = dt_Customer.Rows[0];
            //str = "Customer Name: " + dataRow["FirstName"].ToString() + dataRow["LastName"].ToString();
            //str += "\r\n";
            //str += "Address: " + FullAddress;
            //str += "\r\n";
            //str += "Phone : " + dataRow["Phone"].ToString();
            //str += "\r\n";
            //str += "Mobile : " + dataRow["Mobile"].ToString();
            //str += "\r\n";
            //str += "Appointment Date: " + dt_Appointment.Rows[0]["AppointmentDate"].ToString();
            //str += "\r\n";
            //str += "Time Slot: " + dt_Appointment.Rows[0]["TimeSlot"].ToString();
            //str += "\r\n";
            //str += "\r\n";
            //str += "You have a new appointment in myServiceJobsPlus. Open the app to see the details.";
            return ReplaceEmailText(str,true);

        }
        public string GetResourceEmailBody()
        {
            string str = string.Empty;
            DataRow dataRow = dt_Customer.Rows[0];
            str = "Customer Name: " + dataRow["FirstName"].ToString() + dataRow["LastName"].ToString();
            str += "\r\n";
            str += "Address: " + FullAddress;
            str += "\r\n";
            str += "Phone : " + dataRow["Phone"].ToString();
            str += "\r\n";
            str += "Mobile : " + dataRow["Mobile"].ToString();
            str += "\r\n";
            str += "Appointment Date: " + dt_Appointment.Rows[0]["AppointmentDate"].ToString();
            str += "\r\n";
            str += "Time Slot: " + dt_Appointment.Rows[0]["TimeSlot"].ToString();
            str += "\r\n";
            str += "\r\n";
            str += "You have a new appointment in myServiceJobsPlus. Open the app to see the details.";
            return str;

        }
       
        public string ReplaceEmailText(string emailbody, bool ForPreview)
        {
            string ReplaceEmailText = emailbody;
            if(!ForPreview)
            {
                ReplaceEmailText = ReplaceEmailText.Replace("\r\n", "<br />").Replace("\n", "<br />").Replace("\r", "<br />");

            }

            // For Customer

            if (dt_Customer.Rows.Count > 0)
            {
                DataRow dr = dt_Customer.Rows[0];
                ReplaceEmailText = ReplaceEmailText.Replace("[Title]", dr["title"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Job Title]", dr["jobtitle"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Full Name]", dr["FullName"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[First Name]", dr["FirstName"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Last Name]", dr["LastName"].ToString());
            }

            // For Appoinmnet

            if (dt_Appointment.Rows.Count > 0)
            {
                DataRow dr = dt_Appointment.Rows[0];
                ReplaceEmailText = ReplaceEmailText.Replace("[Date]", dr["AppointmentDate"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Time]", dr["TimeSlot"].ToString());
            
            }

            // For Invocie

            if (dt_Invocie.Rows.Count > 0)
            {
                DataRow dr_Invocie = dt_Invocie.Rows[0];
                ReplaceEmailText = ReplaceEmailText.Replace("[Invoice No]", dr_Invocie["Number"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Invoice Total]", dr_Invocie["Total"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Invoice Date]", dr_Invocie["InvoiceDate"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Paid]", dr_Invocie["AmountCollect"].ToString());
            }

            // For Company
            if (dt_Company.Rows.Count > 0)
            {
                DataRow dr_Company = dt_Company.Rows[0];
                ReplaceEmailText = ReplaceEmailText.Replace("[Company Name]", dr_Company["CompanyName"].ToString());

                CompanyFullName = dr_Company["CompanyName"].ToString();



                if (!string.IsNullOrEmpty(dr_Company["Address"].ToString()))
                {
                    FullAddress += dr_Company["Address"].ToString() + ", ";
                }
                if (!string.IsNullOrEmpty(dr_Company["City"].ToString()))
                {
                    
                       FullAddress += dr_Company["City"].ToString() + ", ";
                }
                if (!string.IsNullOrEmpty(dr_Company["State"].ToString()))
                {
                    FullAddress += dr_Company["State"].ToString() + ", ";
                }
                if (!string.IsNullOrEmpty(dr_Company["ZipCode"].ToString()))
                {
                    FullAddress += dr_Company["ZipCode"].ToString();
                }

                if (FullAddress.EndsWith(", "))
                {
                    FullAddress = FullAddress.Substring(0, FullAddress.Length - 2);
                }
              

                ReplaceEmailText = ReplaceEmailText.Replace("[Address]", FullAddress);
                ReplaceEmailText = ReplaceEmailText.Replace("[Phone]", dr_Company["Phone"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Email]", dr_Company["Email"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Website]", dr_Company["Website"].ToString());
                ReplaceEmailText = ReplaceEmailText.Replace("[Facebook]", dr_Company["Facebook"].ToString());
            }
           
            return ReplaceEmailText;
        }
        public bool SentEmail (string Subject,
           string BodyText,
           string recepientToEmail,
           string recepientCCEmail,
           string recepientBCCEmail,
           List<EmailContent> emailContents)
        {


            EmailTemplate = "<body><table style='max-width:800px; font-family:Arial;font-size:13px;'>" +
                "<tr><td align='left'><table width='100%'><tr><td>" +
                "<img src=\"cid:photo\"  id='img' alt='' style='max-height:60px;' height='60' /></td>" +
                "<td align='center' style='color:#2980b9; vertical-align:bottom;'>" +
                "</td></tr></table></td></tr>" +
                "<tr><td align='left' style='padding-left:5px;'>";

            //this.EmailTemplate = EmailTemplate + "</td></tr><tr><td><br /><br /><table style='font-family:Arial;font-size:13px;'>" +
            //    "<tr><td>" + CompanyFullName + "</td></tr>" +
            //    "<tr><td>" + CompanyFullAddress + "</td></tr>";

            this.EmailTemplate = EmailTemplate + ReplaceEmailText(BodyText,false);

            // Company email signature image (uploaded on Company.aspx), embedded as cid:signature.
            string signaturePath = EmailSignature.GetFilePath(CompanyID);

            EmailTemplate = EmailTemplate + "</td></tr>"
                + (signaturePath != "" ? EmailSignature.HtmlRow() : "")
                + "</table></body>";


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
                mailMessage.Subject = Subject;
                mailMessage.Body = this.EmailTemplate;

                // The logo (cid:photo) and the signature (cid:signature) are embedded in one HTML view.
                bool hasLogo = !string.IsNullOrEmpty(dt_Company.Rows[0]["LogoFile"].ToString()) && File.Exists(LogoPath);
                if (hasLogo || signaturePath != "")
                {
                    AlternateView htmlview = AlternateView.CreateAlternateViewFromString(this.EmailTemplate, null, "text/html");
                    if (hasLogo)
                    {
                        htmlview.LinkedResources.Add(new LinkedResource(LogoPath, MediaTypeNames.Image.Jpeg) { ContentId = "photo" });
                    }
                    if (signaturePath != "")
                    {
                        htmlview.LinkedResources.Add(EmailSignature.CreateResource(signaturePath));
                    }
                    mailMessage.AlternateViews.Add(htmlview);
                }
               
            
               
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


                mailMessage.IsBodyHtml = true;


                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["SMTP"];
                smtp.EnableSsl = true;
                System.Net.NetworkCredential NetworkCred = new System.Net.NetworkCredential();

                EmailProcessor emailProcessor = new EmailProcessor();
                EmialSetting emialSetting = emailProcessor.GetEmailSetting();
                smtp.Host = emialSetting.SMTP;
                NetworkCred.UserName = emialSetting.SmtpUser;
                NetworkCred.Password = emialSetting.SmtpPassword;
                smtp.Port = emialSetting.Port;

                //  NetworkCred.UserName = ConfigurationManager.AppSettings["SmtpUser"];
                //  NetworkCred.Password = ConfigurationManager.AppSettings["SmtpPassword"];
                smtp.UseDefaultCredentials = true;
                smtp.Credentials = NetworkCred;
                //  smtp.Port = int.Parse(ConfigurationManager.AppSettings["Port"]);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                try
                {
                    smtp.Send(mailMessage);
                }
                catch { return false; }
                
            }

            return true;
        }
    }
}