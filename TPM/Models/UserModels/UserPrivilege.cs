using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Models.UserModels
{
    public class UserPrivilege
    {
        public bool CanAccessQuickBooks { get; set; }
        public bool CanEditPayment { get; set; }
        public bool CanAccessVtPayment { get; set; }
        public bool CanEditBooking { get; set; }
        public bool CanAccessUserInfo { get; set; }
        public bool CanAccessInvoice { get; set; }
        public bool CanEditCustomer { get; set; }
        public bool CanDeleteCustomer { get; set; }
        public bool CanAddCustomerBooking { get; set; }
        public bool CanAccessSetting { get; set; }
    }

}
