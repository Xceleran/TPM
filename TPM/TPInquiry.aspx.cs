using FSM.Processors;
using System;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.UI;

namespace TPM
{
    public partial class TPInquiry : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string token = Request.QueryString["token"];
            string companyId = Request.QueryString["companyId"] ?? Session["CompanyID"]?.ToString();
            string workOrderId = Request.QueryString["woId"];
            string thirdPartyId = Request.QueryString["tpId"];

            if (Session["CompanyID"] == null && string.IsNullOrEmpty(token))
            {
                Response.Redirect("Dashboard.aspx");
                return;
            }

            if (!IsPostBack)
            {
                int wo = 0, tp = 0;
                int.TryParse(workOrderId, out wo);
                int.TryParse(thirdPartyId, out tp);

                string sessionKey = "TPM_InquiryToken_ThirdParty_" + (companyId ?? "");
                if (string.IsNullOrEmpty(token) && Session["CompanyID"] != null)
                    token = Session[sessionKey] as string;

                if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(companyId) && (wo > 0 || tp > 0))
                {
                    var chat = new InquiryChatService();
                    var thread = chat.CreateThread(companyId, "ThirdParty", wo > 0 ? wo : (int?)null, null, null, tp > 0 ? tp : (int?)null);
                    token = thread.AccessToken;
                    if (Session["CompanyID"] != null)
                        Session[sessionKey] = token;
                }
                hdnToken.Value = token ?? "";
                hdnHasContext.Value = (wo > 0 || tp > 0) ? "1" : "0";
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
            return chat.SendMessage(token, message, "ThirdParty");
        }
    }
}
