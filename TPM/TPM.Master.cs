using FSM.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FSM
{
    public partial class TPMMaster : MasterPage
    {
        public string SidebarLogoUrl { get; private set; }
        public string SidebarLogoUrlLight { get; private set; }
        public string LogoContainerClass { get; private set; } 

        protected void Page_Load(object sender, EventArgs e)
        {
            SetLogoPathsAndStyle(); 
        }

        private void SetLogoPathsAndStyle() 
        {
            SidebarLogoUrl = "images/LHLogowhite.png";     // Default for dark theme
            SidebarLogoUrlLight = "images/lhlogo.png";     // Default for light theme
            LogoContainerClass = "logo-lhg";               // Default class

            if (Session["mXP"] != null && (bool)Session["mXP"])
            {
                SidebarLogoUrl = "images/xinatorwhite.png"; // mXP logo for dark theme
                SidebarLogoUrlLight = "images/xinator.png"; // mXP logo for light theme
                LogoContainerClass = "logo-mxp";            // mXP-specific class
            }
            else if (Session["IsLHG"] != null && (bool)Session["IsLHG"])
            {
                SidebarLogoUrl = "images/LHLogowhite.png";
                SidebarLogoUrlLight = "images/lhlogo.png";
                LogoContainerClass = "logo-lhg";
            }
        }
    }
}
