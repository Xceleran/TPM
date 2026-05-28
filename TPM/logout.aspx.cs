using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace TPM
{
    public partial class logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();
            Response.Cookies["User"].Expires = DateTime.Now.AddDays(-1);

            Response.Redirect(ConfigurationManager.AppSettings["SSOLogout"].ToString());
        }
    }
}