using FSM.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Models.LoginModels
{
    public class LoginObject
    {
        public string CompanyID { get; set; }
        public string ParentID { get; set; } = "0";
        public bool IsParent { get; set; } = false;
        public bool IsMMSAllowed { get; set; } = false;
        public bool IsInboundSMSAllowed { get; set; } = false;
        public string LoginUser { get; set; }
        public string CompanyName { get; set; }
        public AddressType Addresstype { get; set; }
        public string CompanyTag { get; set; }
        public string UserFirstName { get; set; }
        public string TimeZone { get; set; }
        public string DealerName { get; set; }
        public string DealerAddress { get; set; }

    }
}
