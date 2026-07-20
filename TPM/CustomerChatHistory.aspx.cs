using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using FSM.Helper;
using System.Web.UI;
using FSM.SMSService;
using System.Web.Script.Services;
using System.Configuration;
using System.IO;
using Intuit.Ipp.Data;
using System.Reflection;

namespace FSM
{
    public partial class CustomerChatHistory : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string MobileNumber = Request.QueryString["mobile"];
            MobileNumber= Common.CleanInput(MobileNumber);
            MobileNumber = Server.UrlDecode(MobileNumber);
            string CustomerName = Request.QueryString["name"];
            CustomerName = Common.CleanInput(CustomerName);
            CustomerName = Server.UrlDecode(CustomerName);
            string customerId = Request.QueryString["customerId"];
            customerId = Common.CleanInput(customerId);
            customerId = Server.UrlDecode(customerId);

            if (Session["CompanyID"] == null)
            {
                Response.Redirect("logout.aspx");
            }
            if (!string.IsNullOrEmpty(MobileNumber))
                txtMobile.Value = MobileNumber;

            if (!string.IsNullOrEmpty(customerId))
                txtCustomerId.Value = customerId;

            if (!string.IsNullOrEmpty(CustomerName))
            {
                lblHeader.InnerHtml = $"<i class='fas fa-sms'></i> Text Message History of {CustomerName}";
                txtCustomerName.Value = CustomerName;
            }
                
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetMessages(string mobile, string customerId)
        {
            string companyId = HttpContext.Current.Session["CompanyID"]?.ToString();
            if (string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(mobile))
                return new { data = new List<object>() };

            try
            {
                TwilioSMSService smsService = new TwilioSMSService();
                var messages = smsService.GetSMSHistoryForNumber(mobile, companyId);
                var rows = new List<object>();
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        rows.Add(new
                        {
                            SendDateTime = m.SendDateTime.ToString("dd MMM yyyy hh:mm tt"),
                            Body = m.Body,
                            Status = m.Type == "outgoing" ? "Sent" : "Received",
                            Mobile = mobile,
                            CustomerId = customerId
                        });
                    }
                }
                return new { data = rows };
            }
            catch
            {
                return new { data = new List<object>() };
            }
        }

        protected void btnSendSMS_Click(object sender, EventArgs e)
        {
            string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
            string CustomerID = txtCustomerId.Value;
            string SmsBody = txtSMS.Value;
            string Mobile = txtMobile.Value;
            string customerName = txtCustomerName.Value;

            CustomerID = Common.CleanInput(CustomerID);
            SmsBody = Common.CleanInput(SmsBody);
            Mobile = Common.CleanInput(Mobile);
            customerName = Common.CleanInput(customerName);

            TwilioSMSService smsService = new TwilioSMSService();
            bool result = smsService.SendCustomerAdHocSMS(CompanyID, CustomerID, SmsBody, Mobile);

            string redirectUrl = "CustomerChatHistory.aspx?mobile=" + HttpUtility.UrlEncode(Mobile)
                        + "&name=" + HttpUtility.UrlEncode(customerName)
                        + "&customerId=" + HttpUtility.UrlEncode(CustomerID);

            string script = result
                ? "Swal.fire('SMS Sent Successfully', '', 'success').then(() => { window.location='" + redirectUrl + "'; });"
                : "Swal.fire('Something went wrong, Please try again.', '', 'warning').then(() => { window.location='" + redirectUrl + "'; });";

            ScriptManager.RegisterStartupScript(this, this.GetType(), "SendSMS", script, true);
        }

        protected void btnSendMMS_Click(object sender, EventArgs e)
        {
            string redirectUrl = "";
            try
            {
                string CompanyID = HttpContext.Current.Session["CompanyID"].ToString();
                string _CustomerID = txtCustomerId.Value;
                string customerName = txtCustomerName.Value;
                string mobile = txtMobile.Value;
                string mmsBody = txtMMSBody.Value;

                mmsBody = Common.CleanInput(mmsBody);
                _CustomerID = Common.CleanInput(_CustomerID);
                mobile = Common.CleanInput(mobile);
                customerName = Common.CleanInput(customerName);

                string _Path = "~/MMSFile/" + _CustomerID + "/";
                string strFolder = System.Web.HttpContext.Current.Server.MapPath(_Path);

                if (!Directory.Exists(strFolder))
                {
                    Directory.CreateDirectory(strFolder);
                }

                string uniqueFileName = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") + "_" + System.IO.Path.GetFileName(fuAttachment.FileName);
                string savedFilePath = System.IO.Path.Combine(strFolder, uniqueFileName);


                fuAttachment.SaveAs(savedFilePath);

                string baseUrl = ConfigurationManager.AppSettings["baseurl"];
                string mmsUrl = baseUrl + "MMSFile/" + _CustomerID + "/" + uniqueFileName;
                string filePath = "MMSFile/" + _CustomerID + "/" + uniqueFileName;

                // Send MMS
                TwilioSMSService twilio = new TwilioSMSService();
                bool result = twilio.SendCustomerMMS(CompanyID, _CustomerID, mmsBody, mobile, filePath, mmsUrl);

                 redirectUrl = "CustomerChatHistory.aspx?mobile=" + HttpUtility.UrlEncode(mobile)
                       + "&name=" + HttpUtility.UrlEncode(customerName)
                       + "&customerId=" + HttpUtility.UrlEncode(_CustomerID);

                string script = result
               ? "Swal.fire('MMS Sent Successfully', '', 'success').then(() => { window.location='" + redirectUrl + "'; });"
               : "Swal.fire('Something went wrong, Please try again.', '', 'warning').then(() => { window.location='" + redirectUrl + "'; });";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SendMMS", script, true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"Swal.fire('Error: {ex.Message}', '', 'warning'); window.location.replace('"+redirectUrl+"');", true);


            }
        }




    }
}
