using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Entity.Appoinments
{
    public class TimeSlot
    {
        public int ID { get; set; }
        public string CompanyID { get; set; }
        public string TimeBlock { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string TimeBlockSchedule { get; set; }
        public bool? IsFromCalender { get; set; }
        public int? Duration { get; set; }
    }
}
