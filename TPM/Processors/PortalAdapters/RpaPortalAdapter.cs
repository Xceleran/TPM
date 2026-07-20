using FSM.Models.TPM;
using System;
using System.Threading.Tasks;

namespace FSM.Processors.PortalAdapters
{
    /// <summary>
    /// Queues RPA portal jobs for external Playwright worker processing.
    /// </summary>
    public class RpaPortalAdapter : IPortalAdapter
    {
        private readonly AhsStatusQueueProcessor _queue = new AhsStatusQueueProcessor();

        public bool CanHandle(ThirdPartyConfig config)
        {
            return config != null && config.IsEnabled &&
                   string.Equals(config.SubmissionMethod, "RPA", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrEmpty(config.PortalUrl);
        }

        public Task<PortalResult> PushStatus(WorkOrderContext ctx, StatusUpdate update, ThirdPartyConfig config)
        {
            int queueId = _queue.EnqueueStatus(ctx.CompanyId, ctx.WorkOrder.Id, ctx.ThirdPartyId,
                update.CanonicalStatus, "RPA", "{\"portalUrl\":\"" + config.PortalUrl + "\",\"action\":\"PushStatus\"}");

            return Task.FromResult(new PortalResult
            {
                Success = queueId > 0,
                Message = queueId > 0 ? "Queued for RPA portal update" : "Failed to queue RPA job",
                SubmissionId = queueId.ToString(),
                RequiresManualAction = false
            });
        }

        public Task<PortalResult> SubmitInvoice(InvoiceContext ctx, ThirdPartyConfig config)
        {
            int queueId = _queue.EnqueueStatus(ctx.CompanyId, ctx.WorkOrderId, ctx.ThirdPartyId,
                "InvoiceSubmitted", "RPA", "{\"portalUrl\":\"" + config.PortalUrl + "\",\"action\":\"SubmitInvoice\",\"invoiceNumber\":\"" + ctx.InvoiceNumber + "\"}");

            return Task.FromResult(new PortalResult
            {
                Success = queueId > 0,
                Message = "Invoice queued for RPA portal submission",
                SubmissionId = queueId.ToString()
            });
        }

        public Task<PortalResult> SubmitPreAuthorization(PreAuthContext ctx, ThirdPartyConfig config)
        {
            int queueId = _queue.EnqueueStatus(ctx.CompanyId, ctx.WorkOrderId, ctx.ThirdPartyId,
                "PendingAuthorization", "RPA", "{\"portalUrl\":\"" + config.PortalUrl + "\",\"action\":\"PreAuth\",\"amount\":" + ctx.EstimatedAmount + "}");

            return Task.FromResult(new PortalResult
            {
                Success = queueId > 0,
                Message = "Pre-authorization queued for RPA",
                SubmissionId = queueId.ToString()
            });
        }
    }
}
