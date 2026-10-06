using System;

namespace FSM.Entity
{
    public class DashAnnouncementEntity
    {
        public int AnnouncementID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string DurationPreset { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string AppSource { get; set; }
    }
}
