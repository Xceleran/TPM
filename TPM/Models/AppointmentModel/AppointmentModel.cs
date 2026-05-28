using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Models.AppoinmentModel
{
    public class AppointmentModel
    {
        
             public string AppoinmentUId { get; set; }
        public string AppoinmentId { get; set; }
        public string AppoinmentDate { get; set; }
        public string StatusColor { get; set; }
        public string SiteCity { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string RequestDate { get; set; }
        public string ServiceType { get; set; }
        public string ResourceName { get; set; }
        public string ResourceGroup { get; set; }
        public string TimeSlot { get; set; }
        public string TicketStatus { get; set; }
        public int TicketStatusID { get; set; }
        public int ResourceID { get; set; }
        public string CustomFieldsJson { get; set; }
        public string Country { get; set; }
        public string Note { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public int ServiceTypeID { get; set; }
        public string SiteAddress { get; set; }
        public string SiteState { get; set; }
        public string SiteZip { get; set; }
        public string SiteCountry { get; set; }
        public string SiteContact { get; set; }
        public string SiteEmail { get; set; }
        public string SitePhoneNumber { get; set; }
        public string SiteNote { get; set; }
        public bool SiteIsActive { get; set; }
        public string Duration { get; set; } = "0";
        public string AppoinmentStatus { get; set; }
        public int AppoinmentStatusID { get; set; }
        public string CompanyID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerGuid { get; set; }
        public string ServiceColor { get; set; }
        public string Contact { get; set; }
        public Int32 BusinessID { get; set; } = 0;
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string FirstName { get; set; }
        public string FirstName2 { get; set; } = "";
        public string LastName { get; set; } = "";
        public string LastName2 { get; set; } = "";
        public string Title { get; set; }
        public string JobTitle { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
        public string CompanyName { get; set; }
        public string BusinessName { get; set; }
        public string CustomerName { get; set; }
        public bool IsBusinessContact { get; set; } = false;
        public int timerequired_Hour { get; set; }
        public int timerequired_Minute { get; set; }
    }
 
    public class StatusHistoryViewModel
    {
        public string StatusName { get; set; }
        public string Timestamp { get; set; }
        public string ChangedBy { get; set; }
        public string Note { get; set; }
    }
}
