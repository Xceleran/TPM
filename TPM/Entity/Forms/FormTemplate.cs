using System;
using System.Collections.Generic;

namespace FSM.Entity.Forms
{
    public class FormTemplate
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        
        // Auto-assignment rules
        public bool IsAutoAssignEnabled { get; set; }
        public string AutoAssignServiceTypes { get; set; } // JSON array of service types
        
        // Template configuration
        public string FormStructure { get; set; } // JSON configuration of form fields
        public bool RequireSignature { get; set; }
        public bool RequireTip { get; set; }
        public bool IsActive { get; set; }
        
        // Audit fields
        public string CreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }

    public class FormInstance
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public int TemplateId { get; set; }
        public string AppointmentId { get; set; }
        public string CustomerID { get; set; }
        
        // Form data
        public string FormData { get; set; } // JSON data of filled form
        public string Status { get; set; } // Pending, InProgress, Completed, Submitted
        
        // Signatures and tips
        public byte[] TechnicianSignature { get; set; }
        public byte[] CustomerSignature { get; set; }
        public decimal? TipAmount { get; set; }
        public string TipNotes { get; set; }
        
        // Audit and tracking
        public string FilledBy { get; set; } // Technician ID or name
        public DateTime? StartedDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public DateTime? SubmittedDateTime { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? SyncedDateTime { get; set; }
        
        // File storage
        public string StoredFilePath { get; set; }
        public string StoredFileName { get; set; }
        
        // Helper property for display purposes (not stored in database)
        public string TemplateName { get; set; }
    }

    public class FormField
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string FieldName { get; set; }
        public string FieldLabel { get; set; }
        public string FieldType { get; set; } // text, number, date, dropdown, checkbox, radio, textarea, signature
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
        public string ValidationRules { get; set; } // JSON validation rules
        public string Options { get; set; } // JSON options for dropdown/radio
        public int DisplayOrder { get; set; }
        
        // Auto-fill configuration
        public bool IsAutoFillEnabled { get; set; }
        public string AutoFillSource { get; set; } // customer.FirstName, appointment.ServiceType, etc.
    }

    public class FormUsageLog
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public int FormInstanceId { get; set; }
        public int TemplateId { get; set; }
        public string AppointmentId { get; set; }
        public string Action { get; set; } // Created, Started, Completed, Submitted, Synced
        public string PerformedBy { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string Notes { get; set; }
        public string DeviceInfo { get; set; }
    }

    public class AppointmentFormMapping
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public string ServiceType { get; set; }
        public int TemplateId { get; set; }
        public bool IsAutoAssign { get; set; }
        public int Priority { get; set; } // For multiple forms per service type
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string CreatedBy { get; set; }
    }
}