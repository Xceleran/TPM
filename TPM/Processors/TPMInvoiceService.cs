using FSM.Models.TPM;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace FSM.Processors
{
    public class TPMInvoiceService
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private readonly PortalIntegrationHub _portalHub = new PortalIntegrationHub();
        private readonly QBOManager _qboManager = new QBOManager();

        public int CreateTpmInvoiceRecord(string companyId, int workOrderId, string invoiceId, string invoiceNumber, decimal subtotal, decimal tax, decimal total, string createdBy)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMInvoices]
                      (CompanyID, WorkOrderId, InvoiceId, InvoiceNumber, InvoiceDate, Subtotal, Tax, Total, AmountDue, PaymentStatus, CreatedDate, CreatedBy, SubmissionStatus)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @WorkOrderId, @InvoiceId, @InvoiceNumber, GETDATE(), @Subtotal, @Tax, @Total, @Total, 'Draft', GETDATE(), @CreatedBy, 'Draft')", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                cmd.Parameters.AddWithValue("@Subtotal", subtotal);
                cmd.Parameters.AddWithValue("@Tax", tax);
                cmd.Parameters.AddWithValue("@Total", total);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy ?? "System");
                return (int)cmd.ExecuteScalar();
            }
        }

        public PortalResult SubmitToPortal(string companyId, int tpmInvoiceId, int thirdPartyId)
        {
            InvoiceContext ctx = null;
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_TPMInvoices] WHERE Id = @Id AND CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Id", tpmInvoiceId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return new PortalResult { Success = false, Message = "Invoice not found" };
                    ctx = new InvoiceContext
                    {
                        CompanyId = companyId,
                        WorkOrderId = Convert.ToInt32(r["WorkOrderId"]),
                        ThirdPartyId = thirdPartyId,
                        InvoiceId = r["InvoiceId"].ToString(),
                        InvoiceNumber = r["InvoiceNumber"]?.ToString(),
                        Total = Convert.ToDecimal(r["Total"])
                    };
                }
            }

            var result = _portalHub.SubmitInvoiceAsync(ctx).GetAwaiter().GetResult();
            UpdateSubmissionStatus(companyId, tpmInvoiceId, result);
            if (result.Success)
            {
                var woProc = new WorkOrderProcessor();
                woProc.TransitionStatus(companyId, ctx.WorkOrderId, "InvoiceSubmitted", "Invoice submitted to TP portal", "System");
            }
            return result;
        }

        public bool SyncToQbo(string companyId, string invoiceId, string customerQboId, decimal total, string invoiceNumber)
        {
            QBOSettins settings = new QBOSettins();
            if (!_qboManager.VerifyCompanySetting(companyId, ref settings)) return false;
            var ctx = _qboManager.GetServiceContext(settings, companyId);
            string qboId = _qboManager.CreateInvoiceQbo(ctx, companyId, customerQboId, invoiceNumber, total, "TPM Invoice " + invoiceNumber);
            if (qboId != "0")
            {
                using (var con = new SqlConnection(_connStr))
                {
                    con.Open();
                    var cmd = new SqlCommand(
                        "UPDATE [msSchedulerV3].[dbo].[tbl_Invoice] SET QboId = @QboId WHERE ID = @Id AND CompnyID = @CompanyID", con);
                    cmd.Parameters.AddWithValue("@QboId", qboId);
                    cmd.Parameters.AddWithValue("@Id", invoiceId);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            return false;
        }

        public bool ReconcilePayment(string companyId, int tpmInvoiceId, decimal amount, string paymentReference, string paymentMethod)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                int workOrderId = 0;
                string invoiceId = "";
                decimal total = 0;

                var get = new SqlCommand(
                    "SELECT WorkOrderId, InvoiceId, Total, AmountPaid FROM [msSchedulerV3].[dbo].[tbl_TPMInvoices] WHERE Id = @Id", con);
                get.Parameters.AddWithValue("@Id", tpmInvoiceId);
                using (var r = get.ExecuteReader())
                {
                    if (!r.Read()) return false;
                    workOrderId = Convert.ToInt32(r["WorkOrderId"]);
                    invoiceId = r["InvoiceId"].ToString();
                    total = Convert.ToDecimal(r["Total"]);
                }

                decimal paid = amount;
                var payCmd = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMPayments]
                      (CompanyID, TPMInvoiceId, WorkOrderId, PaymentAmount, PaymentDate, PaymentMethod, PaymentReference, CreatedDate, CreatedBy)
                      VALUES (@CompanyID, @TPMInvoiceId, @WorkOrderId, @Amount, GETDATE(), @Method, @Ref, GETDATE(), @CreatedBy)", con);
                payCmd.Parameters.AddWithValue("@CompanyID", companyId);
                payCmd.Parameters.AddWithValue("@TPMInvoiceId", tpmInvoiceId);
                payCmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
                payCmd.Parameters.AddWithValue("@Amount", amount);
                payCmd.Parameters.AddWithValue("@Method", paymentMethod ?? "Check");
                payCmd.Parameters.AddWithValue("@Ref", paymentReference ?? "");
                payCmd.Parameters.AddWithValue("@CreatedBy", "System");
                payCmd.ExecuteNonQuery();

                string paymentStatus = paid >= total ? "Reconciled" : "PaymentPending";
                var upd = new SqlCommand(
                    @"UPDATE [msSchedulerV3].[dbo].[tbl_TPMInvoices]
                      SET AmountPaid = ISNULL(AmountPaid,0) + @Amount, AmountDue = Total - ISNULL(AmountPaid,0) - @Amount,
                          PaymentStatus = @Status, PaymentDate = GETDATE(), PaymentReference = @Ref
                      WHERE Id = @Id", con);
                upd.Parameters.AddWithValue("@Amount", amount);
                upd.Parameters.AddWithValue("@Status", paymentStatus);
                upd.Parameters.AddWithValue("@Ref", paymentReference ?? "");
                upd.Parameters.AddWithValue("@Id", tpmInvoiceId);
                upd.ExecuteNonQuery();

                var invUpd = new SqlCommand(
                    "UPDATE [msSchedulerV3].[dbo].[tbl_Invoice] SET AmountCollect = ISNULL(AmountCollect,0) + @Amount WHERE ID = @InvoiceId AND CompnyID = @CompanyID", con);
                invUpd.Parameters.AddWithValue("@Amount", amount);
                invUpd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                invUpd.Parameters.AddWithValue("@CompanyID", companyId);
                invUpd.ExecuteNonQuery();

                if (paymentStatus == "Reconciled")
                {
                    var woProc = new WorkOrderProcessor();
                    woProc.TransitionStatus(companyId, workOrderId, "Reconciled", "Payment reconciled: " + paymentReference, "System");
                }
            }
            return true;
        }

        private void UpdateSubmissionStatus(string companyId, int tpmInvoiceId, PortalResult result)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"UPDATE [msSchedulerV3].[dbo].[tbl_TPMInvoices]
                      SET SubmissionStatus = @Status, SubmissionId = @SubmissionId, SubmissionDate = GETDATE(), SubmissionNotes = @Notes
                      WHERE Id = @Id AND CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Status", result.Success ? "Submitted" : "Failed");
                cmd.Parameters.AddWithValue("@SubmissionId", (object)result.SubmissionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Notes", result.Message ?? "");
                cmd.Parameters.AddWithValue("@Id", tpmInvoiceId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
