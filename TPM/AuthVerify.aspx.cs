using FSM.Processors;
using System;
using System.Configuration;
using System.Data;
using System.Web;

namespace FSM
{
    public partial class AuthVerify : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string userID = "";
            string companyID = "";
            LoginProcessor loginProcessor = new LoginProcessor();
            if (Request.QueryString["id"] != null)
            {
                string sql = "SELECT * FROM  XinatorCentral.dbo.tbl_Login where SessionGuid='" + Request.QueryString["id"] + "'";
                try
                {
                    Database db = new Database();
                    DataSet dataSet = db.Get_DataSet(sql, "");
                    DataTable dt = dataSet.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        DataRow dr = dt.Rows[0];
                        string[] ssArr = dr["SessionString"].ToString().Split('|');
                        userID = ssArr[0];
                        companyID = ssArr[1];
                    }
                }
                catch (Exception ex)
                { }
            }
            else if (Request.QueryString["BYPass"] != null)
            {
                //To test LHG user, use this:
                ////companyID = "13185"; // LHG Company ID
                ////userID = "locationa@lhg.com";

                // To test mXP user, uncomment the following lines:
                companyID = "msProDemo1"; // mXP Company ID
                userID = "admin.prodemo@myserviceforce.com";

                userID = "xxacrescue@myserviceforce.com";
                companyID = "13202";

                //userID = "admin@mxptest.com";
                //companyID = "14590";

            }
            if (!string.IsNullOrEmpty(companyID) && !string.IsNullOrEmpty(userID))
            {
                var isVarified = loginProcessor.VerifyUser(userID, companyID);
                if (isVarified)
                {

                    if (companyID == "13185")
                    {
                        Session["IsLHG"] = true;
                        Session["mXP"] = false;
                    }
                    else if (companyID == "msProDemo1")
                    {
                        Session["mXP"] = true;
                        Session["IsLHG"] = false;
                    }

                    loginProcessor.LoadPrivilege(userID);
                    Response.Redirect("Dashboard.aspx");
                }
            }
            Response.Redirect("Dashboard.aspx");
        }
    }
}
