using FSM.Processors;
using System;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.UI;

namespace TPM
{
    public partial class PolicyHolderInquiry : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string token = Request.QueryString["token"];
            string companyId = Request.QueryString["companyId"] ?? Session["CompanyID"]?.ToString();
            string apptId = Request.QueryString["apptId"];

            if (Session["CompanyID"] == null && string.IsNullOrEmpty(token))
            {
                Response.Redirect("Dashboard.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int appt = 0;
                int.TryParse(apptId, out appt);
                string woIdParam = Request.QueryString["woId"];
                int wo = 0;
                int.TryParse(woIdParam, out wo);

                string sessionKey = "TPM_InquiryToken_PolicyHolder_" + (companyId ?? "");
                if (string.IsNullOrEmpty(token) && Session["CompanyID"] != null)
                    token = Session[sessionKey] as string;

                if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(companyId) && (appt > 0 || wo > 0))
                {
                    var chat = new InquiryChatService();
                    var thread = chat.CreateThread(companyId, "PolicyHolder", wo > 0 ? wo : (int?)null, appt > 0 ? appt : (int?)null, null, null);
                    token = thread.AccessToken;
                    if (Session["CompanyID"] != null)
                        Session[sessionKey] = token;
                }
                hdnToken.Value = token ?? "";
                hdnHasContext.Value = (appt > 0 || wo > 0) ? "1" : "0";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetMessages(string token)
        {
            var chat = new InquiryChatService();
            return new { messages = chat.GetThreadMessagesByToken(token) };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SendMessage(string token, string message)
        {
            var chat = new InquiryChatService();
            return chat.SendMessage(token, message, "PolicyHolder");
        }
    }
}
