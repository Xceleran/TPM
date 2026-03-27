using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FSM.SMSService
{
    public class Message
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Body { get; set; }
        public string twilioMediaUrls { get; set; }
        public string FilePath { get; set; }
        public DateTime SendDateTime { get; set; }
        public int NumMedia { get; set; }

        public List<string> MediaUrls { get; set; } = new List<string>();
        public int Id { get; set; }
        public string Mobile { get; set; }
        public string FullName { get; set; }
        public int CustomerId { get; set; }
        public string CompanyId { get; set; }
        public DateTime ReceivedDate { get; set; }
    }
}