using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Entity.Appoinments
{
    public class Appointment
    {
        public string CompanyID { get; set; }
        public string CustomerID { get; set; }
        public string AppoinmentId { get; set; }
        public string AppoinmentDate { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string RequestDate { get; set; }
        public string ServiceType { get; set; }
        public string ResourceName { get; set; }
        public int ResourceID { get; set; }
        public string TimeSlot { get; set; }
        public int TimeSlotId { get; set; }  // Added to sync with Jobs-Scheduler tbl_TimeBlocks
        public int ServiceTypeID { get; set; }
        public string TicketStatus { get; set; }
        public string CustomTags { get; set; }
        public string Status { get; set; }
        public int SiteId { get; set; }
        public string Note { get; set; }
        public string CustomFieldsJson { get; set; }
        public string Duration { get; set; } = "0";
        public int StatusID { get; set; }
        public int TicketStatusID { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
}
