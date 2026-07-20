using FSM.Entity.Enums;
using FSM.Models.TPM;
using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;

namespace FSM.Processors
{
    public class StatusTransitionEngine
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private readonly WorkOrderProcessor _workOrderProcessor = new WorkOrderProcessor();
        private readonly CommunicationOrchestrator _communicationOrchestrator = new CommunicationOrchestrator();
        private readonly PortalIntegrationHub _portalHub = new PortalIntegrationHub();

        private static readonly Dictionary<string, string> AppointmentStatusMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Accept", TpmWorkOrderStatus.Acknowledged.ToString() },
            { "Confirm", TpmWorkOrderStatus.Scheduled.ToString() },
            { "Pending", TpmWorkOrderStatus.New.ToString() },
            { "Cancel", TpmWorkOrderStatus.Closed.ToString() },
            { "Scheduled", TpmWorkOrderStatus.Scheduled.ToString() },
            { "Dispatched", TpmWorkOrderStatus.Scheduled.ToString() },
            { "In-Route", TpmWorkOrderStatus.InProgress.ToString() },
            { "Arrived", TpmWorkOrderStatus.InProgress.ToString() },
            { "Completed", TpmWorkOrderStatus.Approved.ToString() },
            { "Closed", TpmWorkOrderStatus.Closed.ToString() },
            { "Cancelled", TpmWorkOrderStatus.Closed.ToString() }
        };

        public bool ProcessAppointmentStatusChange(string companyId, int appointmentId, string customerId, string siteId, string appointmentStatus, string changedBy)
        {
            string canonicalStatus;
            if (!AppointmentStatusMap.TryGetValue(appointmentStatus ?? "", out canonicalStatus))
                canonicalStatus = appointmentStatus;

            var ctx = _workOrderProcessor.BuildContext(companyId, appointmentId, customerId, siteId);
            if (ctx.WorkOrder.Id <= 0) return false;

            string previousStatus = ctx.WorkOrder.Status;
            if (string.Equals(previousStatus, canonicalStatus, StringComparison.OrdinalIgnoreCase))
                return true;

            _workOrderProcessor.TransitionStatus(companyId, ctx.WorkOrder.Id, canonicalStatus, "Status changed via appointment", changedBy);

            var update = new StatusUpdate
            {
                CanonicalStatus = canonicalStatus,
                PreviousStatus = previousStatus,
                Notes = "Appointment status: " + appointmentStatus,
                ChangedBy = changedBy
            };

            ctx.WorkOrder.Status = canonicalStatus;
            _communicationOrchestrator.OnStatusTransition(ctx, update, MapMessageType(canonicalStatus));
            _portalHub.PushStatusAsync(ctx, update).GetAwaiter().GetResult();
            return true;
        }

        public bool ProcessManualTransition(string companyId, int workOrderId, TpmWorkOrderStatus newStatus, string notes, string changedBy)
        {
            var wo = _workOrderProcessor.GetById(companyId, workOrderId);
            if (wo.Id <= 0) return false;

            string previous = wo.Status;
            _workOrderProcessor.TransitionStatus(companyId, workOrderId, newStatus.ToString(), notes, changedBy);

            var ctx = new WorkOrderContext
            {
                WorkOrder = wo,
                AppointmentId = wo.AppointmentId ?? 0,
                ThirdPartyId = wo.ThirdPartyId,
                CompanyId = companyId
            };
            wo.Status = newStatus.ToString();

            var update = new StatusUpdate
            {
                CanonicalStatus = newStatus.ToString(),
                PreviousStatus = previous,
                Notes = notes,
                ChangedBy = changedBy
            };

            _communicationOrchestrator.OnStatusTransition(ctx, update, MapMessageType(newStatus.ToString()));
            _portalHub.PushStatusAsync(ctx, update).GetAwaiter().GetResult();
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

        public bool SaveStatusMapping(string companyId, int? thirdPartyId, string canonicalStatus, string portalCode, string portalLabel)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var del = new SqlCommand(
                    @"DELETE FROM [msSchedulerV3].[dbo].[tbl_TPStatusMapping]
                      WHERE CompanyID = @CompanyID AND ISNULL(ThirdPartyId,0) = ISNULL(@ThirdPartyId,0) AND CanonicalStatus = @Status", con);
                del.Parameters.AddWithValue("@CompanyID", companyId);
                del.Parameters.AddWithValue("@ThirdPartyId", (object)thirdPartyId ?? DBNull.Value);
                del.Parameters.AddWithValue("@Status", canonicalStatus);
                del.ExecuteNonQuery();

                var ins = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_TPStatusMapping]
                      (CompanyID, ThirdPartyId, CanonicalStatus, PortalStatusCode, PortalStatusLabel)
                      VALUES (@CompanyID, @ThirdPartyId, @Status, @PortalCode, @PortalLabel)", con);
                ins.Parameters.AddWithValue("@CompanyID", companyId);
                ins.Parameters.AddWithValue("@ThirdPartyId", (object)thirdPartyId ?? DBNull.Value);
                ins.Parameters.AddWithValue("@Status", canonicalStatus);
                ins.Parameters.AddWithValue("@PortalCode", portalCode ?? "");
                ins.Parameters.AddWithValue("@PortalLabel", portalLabel ?? "");
                ins.ExecuteNonQuery();
            }
            return true;
        }

        public List<object> GetStatusMappings(string companyId, int? thirdPartyId)
        {
            var list = new List<object>();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT Id, CanonicalStatus, PortalStatusCode, PortalStatusLabel, ThirdPartyId
                      FROM [msSchedulerV3].[dbo].[tbl_TPStatusMapping]
                      WHERE CompanyID = @CompanyID OR CompanyID = 'DEFAULT'
                      ORDER BY CanonicalStatus", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new
                        {
                            id = r["Id"],
                            canonicalStatus = r["CanonicalStatus"].ToString(),
                            portalStatusCode = r["PortalStatusCode"]?.ToString(),
                            portalStatusLabel = r["PortalStatusLabel"]?.ToString(),
                            thirdPartyId = r["ThirdPartyId"] != DBNull.Value ? r["ThirdPartyId"] : null
                        });
                    }
                }
            }
            return list;
        }

        private string MapMessageType(string canonicalStatus)
        {
            switch (canonicalStatus)
            {
                case "Acknowledged": return "AcceptTPWorkOrder";
                case "Scheduled": return "AppointmentConfirmation";
                case "PendingAuthorization": return "PreAuthorizationRequest";
                case "PendingInfo": return "RequestAdditionalInfo";
                case "InvoiceSubmitted": return "InvoiceNotification";
                case "Escalated": return "EscalationNotice";
                case "Closed": return "ClosureConfirmation";
                default: return "StatusUpdate";
            }
        }
    }
}
