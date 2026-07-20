using FSM.Models.TPM;
using System.Threading.Tasks;

namespace FSM.Processors.PortalAdapters
{
    public class GuidedManualAdapter : IPortalAdapter
    {
        public bool CanHandle(ThirdPartyConfig config)
        {
            return true;
        }

        public Task<PortalResult> PushStatus(WorkOrderContext ctx, StatusUpdate update, ThirdPartyConfig config)
        {
            string url = config?.PortalUrl;
            if (string.IsNullOrEmpty(url))
                url = "#";

            string manualUrl = url +
                (url.Contains("?") ? "&" : "?") +
                "wo=" + ctx.WorkOrder.WorkOrderNumber +
                "&status=" + update.CanonicalStatus +
                "&appt=" + ctx.AppointmentId;

            return Task.FromResult(new PortalResult
            {
                Success = true,
                RequiresManualAction = true,
                ManualUrl = manualUrl,
                Message = "Open portal to complete status update manually",
                SubmissionId = "MANUAL-" + ctx.WorkOrder.Id
            });
        }

        public Task<PortalResult> SubmitInvoice(InvoiceContext ctx, ThirdPartyConfig config)
        {
            string url = config?.PortalUrl ?? "#";
            return Task.FromResult(new PortalResult
            {
                Success = true,
                RequiresManualAction = true,
                ManualUrl = url + "?invoice=" + ctx.InvoiceNumber,
                Message = "Open portal to submit invoice manually"
            });
        }

        public Task<PortalResult> SubmitPreAuthorization(PreAuthContext ctx, ThirdPartyConfig config)
        {
            string url = config?.PortalUrl ?? "#";
            return Task.FromResult(new PortalResult
            {
                Success = true,
                RequiresManualAction = true,
                ManualUrl = url + "?preauth=" + ctx.WorkOrderId,
                Message = "Open portal to submit pre-authorization manually"
            });
        }
    }
}
