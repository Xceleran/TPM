using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Helper
{
    public class Common
    {
        public static string CleanInput(string sInput, int iLength = 0)
        {
            if (sInput == null)
            {
                return "";
            }
            if (iLength > 0)
            {
                if (sInput.Length > iLength)
                {
                    sInput = sInput.Substring(0, iLength);
                }
            }

            sInput = sInput.Replace("'", "");
            sInput = sInput.Replace(";", "");
            sInput = sInput.Replace("--", "");
            sInput = sInput.Replace("<", "");
            sInput = sInput.Replace(">", "");
            sInput = sInput.Replace("script", "");
            sInput = sInput.Replace("html", "");
            sInput = sInput.Replace("(", "");
            sInput = sInput.Replace(")", "");
            sInput = sInput.Replace("=", "");
            sInput = sInput.Replace("*", "");
            sInput = sInput.Replace("href", "");
            sInput = sInput.Replace("&lt", "");
            sInput = sInput.Replace("&gt", "");
            sInput = sInput.Replace("&quot", "");

            return sInput;

        }

        public enum Pages
        {
            BillableItems,
            QboConnection
        }
    }

    public class QboCommon
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Quantity { get; set; }
        public string Rate { get; set; }
        public string Amount { get; set; }
        public string Taxable { get; set; }
        public string TableData { get; set; }
        public string CustID { get; set; }
        public string CustName { get; set; }
        public string InvDate { get; set; }
        public string InvAmount { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string InvNo { get; set; }
        public string DisplayNumber { get; set; }
        public string PaidAmount { get; set; }
        public string DisType { get; set; }
        public string DisRate { get; set; }
        public string DisAmount { get; set; }
        public string TaxId { get; set; }
        public string TaxRate { get; set; }
        public string TotalTax { get; set; }
        public string Notes { get; set; }
        public string OptionData { get; set; }
        public string QboClassId { get; set; }
        public string QboLocationId { get; set; }
        
            
        public string TableRow { get; set; }
        public string ReqAmtType { get; set; }
        public string ReqDepoAmt { get; set; }
        public string ReqDepoPercent { get; set; }
    }
}
