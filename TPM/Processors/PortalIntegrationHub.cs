using FSM.Models.TPM;
using FSM.Processors.PortalAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FSM.Processors
{
    public class PortalIntegrationHub
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private readonly List<IPortalAdapter> _adapters = new List<IPortalAdapter>
        {
            new ApiPortalAdapter(),
            new RpaPortalAdapter(),
            new GuidedManualAdapter()
        };

        public async Task<PortalResult> PushStatusAsync(WorkOrderContext ctx, StatusUpdate update)
        {
            if (ctx?.WorkOrder == null || ctx.WorkOrder.Id <= 0)
                return new PortalResult { Success = false, Message = "Invalid work order context" };

            var config = LoadThirdPartyConfig(ctx.CompanyId, ctx.ThirdPartyId);
            if (config == null || !config.IsEnabled)
                return new PortalResult { Success = true, Message = "Portal integration not configured", RequiresManualAction = true };

            string portalCode = GetPortalStatusCode(ctx.CompanyId, ctx.ThirdPartyId, update.CanonicalStatus);
            update.Notes = (update.Notes ?? "") + " [PortalCode:" + portalCode + "]";

            foreach (var adapter in _adapters)
            {
                if (adapter.CanHandle(config))
                    return await adapter.PushStatus(ctx, update, config);
            }

            return await new GuidedManualAdapter().PushStatus(ctx, update, config);
        }

        public async Task<PortalResult> SubmitInvoiceAsync(InvoiceContext invoiceCtx)
        {
            var config = LoadThirdPartyConfig(invoiceCtx.CompanyId, invoiceCtx.ThirdPartyId);
            if (config == null)
                return new PortalResult { Success = false, Message = "No TP config found" };

            foreach (var adapter in _adapters)
            {
                if (adapter.CanHandle(config))
                    return await adapter.SubmitInvoice(invoiceCtx, config);
            }
            return await new GuidedManualAdapter().SubmitInvoice(invoiceCtx, config);
        }

        public async Task<PortalResult> SubmitPreAuthorizationAsync(PreAuthContext preAuthCtx)
        {
            var config = LoadThirdPartyConfig(preAuthCtx.CompanyId, preAuthCtx.ThirdPartyId);
            if (config == null)
                return new PortalResult { Success = false, Message = "No TP config found" };

            foreach (var adapter in _adapters)
            {
                if (adapter.CanHandle(config))
                    return await adapter.SubmitPreAuthorization(preAuthCtx, config);
            }
            return await new GuidedManualAdapter().SubmitPreAuthorization(preAuthCtx, config);
        }

        public ThirdPartyConfig LoadThirdPartyConfig(string companyId, int thirdPartyId)
        {
            if (thirdPartyId <= 0) return null;
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT tp.Id, tp.CompanyID, tp.ThirdPartyName, tp.ContactEmail, tp.ContactPhone,
                             ac.PortalUrl, ac.ApiEndpoint, ac.ApiKey, ac.ApiSecret, ac.AuthToken,
                             ac.SubmissionMethod, ac.IsEnabled,
                             wc.StatusServiceEndpoint, wc.StatusServiceUser, wc.StatusServicePassword, wc.EnabledStatusReporting
                      FROM [msSchedulerV3].[dbo].[tbl_ThirdParties] tp
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_TPMApiConfig] ac ON ac.ThirdPartyId = tp.Id AND ac.CompanyID = tp.CompanyID
                      LEFT JOIN [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc ON wc.CompanyName = tp.ThirdPartyName
                      WHERE tp.Id = @Id AND tp.CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Id", thirdPartyId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new ThirdPartyConfig
                    {
                        Id = Convert.ToInt32(r["Id"]),
                        ThirdPartyId = Convert.ToInt32(r["Id"]),
                        ThirdPartyName = r["ThirdPartyName"]?.ToString(),
                        CompanyId = r["CompanyID"]?.ToString(),
                        PortalUrl = r["PortalUrl"]?.ToString(),
                        ApiEndpoint = r["ApiEndpoint"]?.ToString() ?? r["StatusServiceEndpoint"]?.ToString(),
                        ApiKey = r["ApiKey"]?.ToString(),
                        ApiSecret = r["ApiSecret"]?.ToString(),
                        AuthToken = r["AuthToken"]?.ToString(),
                        SubmissionMethod = r["SubmissionMethod"]?.ToString() ??
                            (r["EnabledStatusReporting"] != DBNull.Value && Convert.ToBoolean(r["EnabledStatusReporting"]) ? "API" : "Manual"),
                        IsEnabled = r["IsEnabled"] != DBNull.Value && Convert.ToBoolean(r["IsEnabled"]),
                        StatusServiceEndpoint = r["StatusServiceEndpoint"]?.ToString(),
                        StatusServiceUser = r["StatusServiceUser"]?.ToString(),
                        StatusServicePassword = r["StatusServicePassword"]?.ToString(),
                        ContactEmail = r["ContactEmail"]?.ToString(),
                        ContactPhone = r["ContactPhone"]?.ToString()
                    };
                }
            }
        }

        public bool SaveApiConfig(ThirdPartyConfig config, string updatedBy)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var del = new SqlCommand(
                    "DELETE FROM [msSchedulerV3].[dbo].[tbl_TPMApiConfig] WHERE CompanyID = @CompanyID AND ThirdPartyId = @ThirdPartyId", con);
                del.Parameters.AddWithValue("@CompanyID", config.CompanyId);
                del.Parameters.AddWithValue("@ThirdPartyId", config.ThirdPartyId);
                del.ExecuteNonQuery();

                var ins = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPMApiConfig]
                      (CompanyID, ThirdPartyId, PortalUrl, ApiEndpoint, ApiKey, ApiSecret, AuthToken, SubmissionMethod, IsEnabled, CreatedDate, CreatedBy)
                      VALUES (@CompanyID, @ThirdPartyId, @PortalUrl, @ApiEndpoint, @ApiKey, @ApiSecret, @AuthToken, @SubmissionMethod, @IsEnabled, GETDATE(), @CreatedBy)", con);
                ins.Parameters.AddWithValue("@CompanyID", config.CompanyId);
                ins.Parameters.AddWithValue("@ThirdPartyId", config.ThirdPartyId);
                ins.Parameters.AddWithValue("@PortalUrl", (object)config.PortalUrl ?? DBNull.Value);
                ins.Parameters.AddWithValue("@ApiEndpoint", (object)config.ApiEndpoint ?? DBNull.Value);
                ins.Parameters.AddWithValue("@ApiKey", (object)config.ApiKey ?? DBNull.Value);
                ins.Parameters.AddWithValue("@ApiSecret", (object)config.ApiSecret ?? DBNull.Value);
                ins.Parameters.AddWithValue("@AuthToken", (object)config.AuthToken ?? DBNull.Value);
                ins.Parameters.AddWithValue("@SubmissionMethod", (object)config.SubmissionMethod ?? "Manual");
                ins.Parameters.AddWithValue("@IsEnabled", config.IsEnabled);
                ins.Parameters.AddWithValue("@CreatedBy", updatedBy ?? "System");
                ins.ExecuteNonQuery();
            }
            return true;
        }

        public string GetPortalStatusCode(string companyId, int? thirdPartyId, string canonicalStatus)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 PortalStatusCode FROM [msSchedulerV3].[dbo].[tbl_TPStatusMapping]
                      WHERE (CompanyID = @CompanyID OR CompanyID = 'DEFAULT')
                        AND (@ThirdPartyId IS NULL OR ThirdPartyId = @ThirdPartyId OR ThirdPartyId IS NULL)
                        AND CanonicalStatus = @Status
                      ORDER BY CASE WHEN CompanyID = @CompanyID THEN 0 ELSE 1 END,
                               CASE WHEN ThirdPartyId = @ThirdPartyId THEN 0 ELSE 1 END", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@ThirdPartyId", (object)thirdPartyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", canonicalStatus);
                return cmd.ExecuteScalar()?.ToString() ?? canonicalStatus;
            }
        }
    }
}
