using FSM.Models.TPM;
using System.Threading.Tasks;

namespace FSM.Processors.PortalAdapters
{
    public interface IPortalAdapter
    {
        bool CanHandle(ThirdPartyConfig config);
        Task<PortalResult> PushStatus(WorkOrderContext ctx, StatusUpdate update, ThirdPartyConfig config);
        Task<PortalResult> SubmitInvoice(InvoiceContext ctx, ThirdPartyConfig config);
        Task<PortalResult> SubmitPreAuthorization(PreAuthContext ctx, ThirdPartyConfig config);
    }
}
