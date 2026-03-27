using FSM.Processors;
using System;
using System.ComponentModel.Design;
using System.Configuration;
using System.Security.Policy;
using System.Web;

namespace FSM
{
    public partial class Dashboard : System.Web.UI.Page
    {
        static string AccountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null || Request.QueryString["logout"] == "true")
            {
                Session.Clear();
                Session.Abandon();
                if (!AccountsUrl.EndsWith("/"))
                {
                    AccountsUrl += "/";
                }
                string url = AccountsUrl + "Login.aspx";
                Response.Redirect(url);
            }
        }
    }
}