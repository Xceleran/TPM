using System;
using System.Collections.Generic;

namespace FSM.Models.TPM
{
    public class WorkOrderEntity
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public string WorkOrderNumber { get; set; }
        public int ThirdPartyId { get; set; }
        public int? PolicyHolderId { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string ServiceType { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public decimal? CoverageAmount { get; set; }
        public int? AppointmentId { get; set; }
        public string CreatedBy { get; set; }
    }

    public class WorkOrderContext
    {
        public WorkOrderEntity WorkOrder { get; set; }
        public int AppointmentId { get; set; }
        public string CustomerId { get; set; }
        public string SiteId { get; set; }
        public int ThirdPartyId { get; set; }
        public string WarrantyCompanyId { get; set; }
        public string CompanyId { get; set; }
        public string Notes { get; set; }
    }

    public class StatusUpdate
    {
        public string CanonicalStatus { get; set; }
        public string PreviousStatus { get; set; }
        public string Notes { get; set; }
        public string ChangedBy { get; set; }
    }

    public class ThirdPartyConfig
    {
        public int Id { get; set; }
        public int ThirdPartyId { get; set; }
        public string ThirdPartyName { get; set; }
        public string CompanyId { get; set; }
        public string PortalUrl { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string AuthToken { get; set; }
        public string SubmissionMethod { get; set; }
        public bool IsEnabled { get; set; }
        public string StatusServiceEndpoint { get; set; }
        public string StatusServiceUser { get; set; }
        public string StatusServicePassword { get; set; }
        public bool EnabledStatusReporting { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
    }

    public class PortalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string SubmissionId { get; set; }
        public string ManualUrl { get; set; }
        public bool RequiresManualAction { get; set; }
    }

    public class InvoiceContext
    {
        public int WorkOrderId { get; set; }
        public string CompanyId { get; set; }
        public int ThirdPartyId { get; set; }
        public string InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public string PdfPath { get; set; }
        public Dictionary<string, string> PortalFields { get; set; } = new Dictionary<string, string>();
    }

    public class PreAuthContext
    {
        public int WorkOrderId { get; set; }
        public string CompanyId { get; set; }
        public int ThirdPartyId { get; set; }
        public decimal EstimatedAmount { get; set; }
        public string Justification { get; set; }
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }

    public class CommunicationSettingsExtended
    {
        public string messageType { get; set; }
        public string triggerStatus { get; set; }
        public bool emailEnabled { get; set; }
        public bool smsEnabled { get; set; }
        public string emailContent { get; set; }
        public string emailSubject { get; set; }
        public string smsContent { get; set; }
        public bool autoSend { get; set; }
        public bool sendToCustomer { get; set; }
        public bool sendToResource { get; set; }
        public bool sendToThirdParty { get; set; }
    }

    public class CoverageItemEntity
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public int WorkOrderId { get; set; }
        public string ItemDescription { get; set; }
        public string CoverageStatus { get; set; }
        public decimal? EstimatedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string FAProInvoiceId { get; set; }
        public string FAProCustomerId { get; set; }
        public string TPAuthorizationNumber { get; set; }
    }

    public class InquiryThreadEntity
    {
        public int Id { get; set; }
        public string CompanyID { get; set; }
        public int? WorkOrderId { get; set; }
        public int? AppointmentId { get; set; }
        public string CustomerId { get; set; }
        public int? ThirdPartyId { get; set; }
        public string ChannelType { get; set; }
        public string AccessToken { get; set; }
        public string Status { get; set; }
    }

    public class InquiryMessageEntity
    {
        public int ThreadId { get; set; }
        public string Direction { get; set; }
        public string SenderType { get; set; }
        public string MessageText { get; set; }
        public decimal? AiConfidence { get; set; }
        public string SourceDataRefs { get; set; }
    }

    public class CslScopedData
    {
        public string Status { get; set; }
        public string AppointmentDate { get; set; }
        public string TechnicianName { get; set; }
        public string ServiceType { get; set; }
        public string CustomerName { get; set; }
        public string SiteAddress { get; set; }
        public List<string> PublicNotes { get; set; } = new List<string>();
        public List<CoverageItemEntity> CoverageItems { get; set; } = new List<CoverageItemEntity>();
    }

    public enum DataScopeChannel
    {
        Staff,
        PolicyHolder,
        ThirdParty
    }
}
