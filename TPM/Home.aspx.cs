using System;
using System.Web.UI;

namespace TPM
{
    public partial class Home : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("Dashboard.aspx", true);
        }
    }
}
