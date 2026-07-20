using FSM.Models.TPM;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Processors.PortalAdapters
{
    public class ApiPortalAdapter : IPortalAdapter
    {
        public bool CanHandle(ThirdPartyConfig config)
        {
            return config != null && config.IsEnabled &&
                   string.Equals(config.SubmissionMethod, "API", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrEmpty(config.ApiEndpoint);
        }

        public Task<PortalResult> PushStatus(WorkOrderContext ctx, StatusUpdate update, ThirdPartyConfig config)
        {
            try
            {
                string endpoint = config.ApiEndpoint.TrimEnd('/');
                string payload = "{\"workOrderNumber\":\"" + ctx.WorkOrder.WorkOrderNumber +
                                 "\",\"status\":\"" + update.CanonicalStatus +
                                 "\",\"appointmentId\":" + ctx.AppointmentId + "}";

                var request = (HttpWebRequest)WebRequest.Create(endpoint + "/status");
                request.Method = "POST";
                request.ContentType = "application/json";
                if (!string.IsNullOrEmpty(config.AuthToken))
                    request.Headers.Add("Authorization", "Bearer " + config.AuthToken);
                else if (!string.IsNullOrEmpty(config.ApiKey))
                    request.Headers.Add("X-Api-Key", config.ApiKey);

                byte[] data = Encoding.UTF8.GetBytes(payload);
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return Task.FromResult(new PortalResult
                    {
                        Success = response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created,
                        Message = "Status pushed via API",
                        SubmissionId = ctx.WorkOrder.WorkOrderNumber + "-" + update.CanonicalStatus
                    });
                }
            }
            catch (Exception ex)
            {
                var queue = new AhsStatusQueueProcessor();
                int qid = queue.EnqueueStatus(ctx.CompanyId, ctx.WorkOrder.Id, ctx.ThirdPartyId,
                    update.CanonicalStatus, "API",
                    "{\"workOrderNumber\":\"" + ctx.WorkOrder.WorkOrderNumber + "\",\"status\":\"" + update.CanonicalStatus + "\"}");
                return Task.FromResult(new PortalResult
                {
                    Success = true,
                    Message = "API unreachable — queued for retry (queue #" + qid + "). Use Process Status Queue.",
                    SubmissionId = qid.ToString()
                });
            }
        }

        public Task<PortalResult> SubmitInvoice(InvoiceContext ctx, ThirdPartyConfig config)
        {
            try
            {
                string endpoint = config.ApiEndpoint.TrimEnd('/');
                string payload = "{\"invoiceNumber\":\"" + ctx.InvoiceNumber + "\",\"total\":" + ctx.Total + ",\"workOrderId\":" + ctx.WorkOrderId + "}";

                var request = (HttpWebRequest)WebRequest.Create(endpoint + "/invoice");
                request.Method = "POST";
                request.ContentType = "application/json";
                if (!string.IsNullOrEmpty(config.AuthToken))
                    request.Headers.Add("Authorization", "Bearer " + config.AuthToken);

                byte[] data = Encoding.UTF8.GetBytes(payload);
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(data, 0, data.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return Task.FromResult(new PortalResult
                    {
                        Success = true,
                        Message = "Invoice submitted via API",
                        SubmissionId = ctx.InvoiceNumber
                    });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new PortalResult { Success = false, Message = ex.Message });
            }
        }

        public Task<PortalResult> SubmitPreAuthorization(PreAuthContext ctx, ThirdPartyConfig config)
        {
            return PushStatus(new WorkOrderContext { WorkOrder = new WorkOrderEntity { Id = ctx.WorkOrderId, WorkOrderNumber = ctx.WorkOrderId.ToString() }, CompanyId = ctx.CompanyId },
                new StatusUpdate { CanonicalStatus = "PendingAuthorization", Notes = ctx.Justification }, config);
        }
    }
}
