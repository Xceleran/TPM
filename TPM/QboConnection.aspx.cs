using FSM.Helper;
using FSM.Processors;
using Intuit.Ipp.Core;
using Intuit.Ipp.OAuth2PlatformClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Services;
using System.Web.UI;

namespace FSM
{
    public partial class QboConnection : System.Web.UI.Page
    {
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string appEnvironment = ConfigurationManager.AppSettings["appEnvironment"];
        static OAuth2Client oauthClient = new OAuth2Client(clientID, clientSecret, redirectURI, appEnvironment);
        static string authCode;
        string CompanyID = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null)
            {
                Response.Redirect("logout.aspx");
            }
            CompanyID = Session["CompanyID"].ToString();

            if (Session["CanAccessQuickBooks"] != null)
            {
                if (!Convert.ToBoolean(Session["CanAccessQuickBooks"]))
                {
                    Response.Redirect("Home.aspx", false);
                }
            }
            else
            {
                Response.Redirect("Home.aspx", false);
            }
            QBOSettins qBoStng = new QBOSettins();
            QBOManager qBOManager = new QBOManager();

            if (qBOManager.VerifyCompanySetting(CompanyID, ref qBoStng))
            {
                ServiceContext context = qBOManager.GetServiceContext(qBoStng, HttpContext.Current.Session["CompanyID"].ToString());

                if (string.IsNullOrEmpty(qBoStng.ConnectedCompanyName))
                {
                    Intuit.Ipp.Data.CompanyInfo companyInfo = qBOManager.GetQboCompanyInfo(context);
                    if (companyInfo != null)
                    {
                        Database db = new Database();
                        string Sql = "";
                        try
                        {
                            Sql = "Update [myServiceJobs].[dbo].[QBOCompanySettings] Set ConnectedCompanyName =@ConnectedCompanyName Where CompanyID =@CompanyID;";
                            db.Command.CommandText = Sql;
                            db.Command.Parameters.Clear();
                            db.Command.Parameters.AddWithValue("@ConnectedCompanyName", companyInfo.CompanyName);
                            db.Command.Parameters.AddWithValue("@CompanyID", CompanyID);
                            db.ExecuteCommand();
                        }
                        catch { }
                    }
                }
                else
                {
                    h_FileName.InnerHtml = "QuickBooks File : " + qBoStng.ConnectedCompanyName;
                }

                if (context == null)
                {
                    try
                    {
                        Database db = new Database();
                        //  db.Open();
                        string Sql = "Delete From [myServiceJobs].[dbo].[QBOCompanySettings] Where CompanyID = '" + CompanyID + "'";
                    }
                    catch { }

                    dvAfterConnect.Visible = false;
                    dvBeforeConnect.Visible = true;
                    Session["accessToken"] = null;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", "alert('Your session timeout. please reconnect.');", true);
                }
                else
                {
                    dvBeforeConnect.Visible = false;
                    dvAfterConnect.Visible = true;
                    Session["accessToken"] = qBoStng.AccessToken;
                }
            }
            else
            {
                dvAfterConnect.Visible = false;
                dvBeforeConnect.Visible = true;
                //Session["accessToken"] = null;
            }

            //--------- QuicBooks Conn
            AsyncMode = true;

            if (Request.QueryString.Count > 0)
            {
                var response = new AuthorizeResponse(Request.QueryString.ToString());
                if (response.State != null)
                {
                    if (oauthClient.CSRFToken == response.State)
                    {
                        if (response.RealmId != null)
                        {
                            Session["realmId"] = response.RealmId;
                        }
                        if (response.Code != null)
                        {

                            authCode = response.Code;
                            output("Authorization code obtained.");
                            PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                            Page.RegisterAsyncTask(t);
                            Page.ExecuteRegisteredAsyncTasks();
                        }
                    }
                    else
                    {
                        output("Invalid State");

                        Session.Remove("realmId");
                        Session.Remove("accessToken");
                        Session.Remove("refreshToken");
                    }
                }
            }
            //-----------------------
        }

        protected void btnConnect_Click(object sender, EventArgs e)
        {
            output("Intiating OAuth2 call.");

            Session["ConnectClick"] = "true";
            try
            {
                if (Session["accessToken"] == null)
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Accounting);
                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }
        public async System.Threading.Tasks.Task performCodeExchange()
        {
            output("Exchanging code for tokens.");
            QBOManager qBOManager = new QBOManager();

            try
            {
                var tokenResp = await oauthClient.GetBearerTokenAsync(authCode);

                Session["accessToken"] = tokenResp.AccessToken;
                Session["refreshToken"] = tokenResp.RefreshToken;
                string QBOFileID = Session["realmId"].ToString();
                string QBOApp = ConfigurationManager.AppSettings["QBOApp"];

                qBOManager.SaveCompanySetting(CompanyID, tokenResp.AccessToken, tokenResp.RefreshToken, QBOFileID);

                QBOSettins qs = new QBOSettins();
                qs.AccessToken = tokenResp.AccessToken;
                qs.RefreshToken = tokenResp.RefreshToken;
                qs.FileID = QBOFileID;

                Session["QBOSettins"] = qs;
                if (Request.Url.Query == "")
                {
                    Response.Redirect(Request.RawUrl);
                }
                else
                {
                    Response.Redirect(Request.RawUrl.Replace(Request.Url.Query, ""));
                }
            }
            catch (Exception ex)
            {
                output("Problem while getting bearer tokens.");
            }
        }
        public void output(string logMsg)
        {}

        [WebMethod(EnableSession = false)]
        public static string QBODisconnect()
        {
            try
            {
                HttpContext.Current.Session["accessToken"] = null;

                QBOManager qBOManager = new QBOManager();
                qBOManager.DeleteCompanySetting(HttpContext.Current.Session["CompanyID"].ToString());
                return "QuickBooks Online disconnected.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
    public static class ResponseHelper
    {
        public static void Redirect(this HttpResponse response, string url, string target, string windowFeatures)
        {
            if ((String.IsNullOrEmpty(target) || target.Equals("_self", StringComparison.OrdinalIgnoreCase)) && String.IsNullOrEmpty(windowFeatures))
            {
                response.Redirect(url);
            }
            else
            {
                Page page = (Page)HttpContext.Current.Handler;
                if (page == null)
                {
                    throw new InvalidOperationException("Cannot redirect to new window outside Page context.");
                }
                url = page.ResolveClientUrl(url);
                string script;
                if (!String.IsNullOrEmpty(windowFeatures))
                {
                    script = @"window.open(""{0}"", ""{1}"", ""{2}"");";
                }
                else
                {
                    script = @"window.open(""{0}"", ""{1}"");";
                }
                script = String.Format(script, url, target, windowFeatures);
                ScriptManager.RegisterStartupScript(page, typeof(Page), "Redirect", script, true);
            }
        }
    }
}