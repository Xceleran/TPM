// FSM.Entity/Notes/Note.cs
using Newtonsoft.Json; // Add this using statement
using System;

namespace FSM.Entity.Notes
{
    public class Note
    {
        [JsonProperty("Id")] // Maps the "Id" field from the JSON
        public int Id { get; set; }

        [JsonProperty("Description")] // Maps the "Description" field
        public string Description { get; set; }

        [JsonProperty("CreatedAt")] // Maps the "CreatedAt" field
        public string CreatedAt { get; set; }

        [JsonProperty("CSLId")]
        public int CSLId { get; set; }

        [JsonProperty("CustomerId")]
        public int CustomerId { get; set; }

        [JsonProperty("AppointmentId")]
        public int AppointmentId { get; set; }

        [JsonProperty("CompanyId")]
        public string CompanyId { get; set; }
    }
}
