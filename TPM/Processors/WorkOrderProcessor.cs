using FSM.Entity.Enums;
using FSM.Models.TPM;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace FSM.Processors
{
    public class WorkOrderProcessor
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public WorkOrderEntity GetByAppointmentId(string companyId, int appointmentId)
        {
            var wo = new WorkOrderEntity();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT TOP 1 * FROM [msSchedulerV3].[dbo].[tbl_WorkOrders]
                      WHERE CompanyID = @CompanyID AND AppointmentId = @AppointmentId
                      ORDER BY Id DESC", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        wo = MapWorkOrder(r);
                }
            }
            return wo;
        }

        public WorkOrderEntity GetById(string companyId, int workOrderId)
        {
            var wo = new WorkOrderEntity();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_WorkOrders]
                      WHERE CompanyID = @CompanyID AND Id = @Id", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@Id", workOrderId);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        wo = MapWorkOrder(r);
                }
            }
            return wo;
        }

        public WorkOrderEntity UpsertFromAppointment(string companyId, int appointmentId, string customerId, string siteId, string changedBy)
        {
            int thirdPartyId = ResolveThirdPartyId(companyId, appointmentId);
            var existing = GetByAppointmentId(companyId, appointmentId);
            if (existing.Id > 0)
                return existing;

            string woNumber = "WO-" + companyId + "-" + appointmentId;
            string serviceType = "";
            string description = "";
            string warrantyId = "";

            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var apptCmd = new SqlCommand(
                    @"SELECT ServiceType, Note, WarrentyCompanyID FROM [msSchedulerV3].[dbo].[tbl_Appointment]
                      WHERE CompanyID = @CompanyID AND ApptID = @ApptID", con);
                apptCmd.Parameters.AddWithValue("@CompanyID", companyId);
                apptCmd.Parameters.AddWithValue("@ApptID", appointmentId);
                using (var r = apptCmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        serviceType = r["ServiceType"]?.ToString() ?? "";
                        description = r["Note"]?.ToString() ?? "";
                        warrantyId = r["WarrentyCompanyID"]?.ToString() ?? "";
                    }
                }

                if (thirdPartyId == 0 && !string.IsNullOrEmpty(warrantyId))
                    thirdPartyId = EnsureThirdPartyFromWarranty(con, companyId, warrantyId);

                var insert = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_WorkOrders]
                      (CompanyID, WorkOrderNumber, ThirdPartyId, Status, RequestDate, ServiceType, Description, AppointmentId, CreatedDate, CreatedBy)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @WorkOrderNumber, @ThirdPartyId, @Status, GETDATE(), @ServiceType, @Description, @AppointmentId, GETDATE(), @CreatedBy)", con);
                insert.Parameters.AddWithValue("@CompanyID", companyId);
                insert.Parameters.AddWithValue("@WorkOrderNumber", woNumber);
                insert.Parameters.AddWithValue("@ThirdPartyId", thirdPartyId > 0 ? thirdPartyId : 0);
                insert.Parameters.AddWithValue("@Status", TpmWorkOrderStatus.New.ToString());
                insert.Parameters.AddWithValue("@ServiceType", serviceType);
                insert.Parameters.AddWithValue("@Description", description);
                insert.Parameters.AddWithValue("@AppointmentId", appointmentId);
                insert.Parameters.AddWithValue("@CreatedBy", changedBy ?? "System");
                int newId = (int)insert.ExecuteScalar();

                LogStatusHistory(con, newId, companyId, TpmWorkOrderStatus.New.ToString(), "Work order created from appointment", changedBy);
                return GetById(companyId, newId);
            }
        }

        public bool TransitionStatus(string companyId, int workOrderId, string newStatus, string notes, string changedBy)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"UPDATE [msSchedulerV3].[dbo].[tbl_WorkOrders]
                      SET Status = @Status, UpdatedDate = GETDATE(), UpdatedBy = @UpdatedBy
                      WHERE CompanyID = @CompanyID AND Id = @Id", con);
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@UpdatedBy", changedBy ?? "System");
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@Id", workOrderId);
                if (cmd.ExecuteNonQuery() <= 0) return false;
                LogStatusHistory(con, workOrderId, companyId, newStatus, notes, changedBy);
            }
            return true;
        }

        public WorkOrderContext BuildContext(string companyId, int appointmentId, string customerId, string siteId)
        {
            var wo = UpsertFromAppointment(companyId, appointmentId, customerId, siteId,
                HttpContext.Current?.Session?["LoginUser"]?.ToString() ?? "System");

            string warrantyId = "";
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT WarrentyCompanyID FROM [msSchedulerV3].[dbo].[tbl_Appointment]
                      WHERE CompanyID = @CompanyID AND ApptID = @ApptID", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@ApptID", appointmentId);
                var val = cmd.ExecuteScalar();
                warrantyId = val?.ToString() ?? "";
            }

            return new WorkOrderContext
            {
                WorkOrder = wo,
                AppointmentId = appointmentId,
                CustomerId = customerId,
                SiteId = siteId,
                ThirdPartyId = wo.ThirdPartyId,
                WarrantyCompanyId = warrantyId,
                CompanyId = companyId
            };
        }

        private int ResolveThirdPartyId(string companyId, int appointmentId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT tp.Id FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc ON wc.WarrantyCompanyUID = TRY_CAST(a.WarrentyCompanyID AS bigint)
                      INNER JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.ThirdPartyName = wc.CompanyName AND tp.CompanyID = a.CompanyID
                      WHERE a.CompanyID = @CompanyID AND a.ApptID = @ApptID", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@ApptID", appointmentId);
                var val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                    return Convert.ToInt32(val);
            }
            return 0;
        }

        private int EnsureThirdPartyFromWarranty(SqlConnection con, string companyId, string warrantyCompanyId)
        {
            var check = new SqlCommand(
                @"SELECT tp.Id FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] wc
                  INNER JOIN [msSchedulerV3].[dbo].[tbl_ThirdParties] tp ON tp.ThirdPartyName = wc.CompanyName AND tp.CompanyID = @CompanyID
                  WHERE wc.WarrantyCompanyUID = @WarrantyId", con);
            check.Parameters.AddWithValue("@CompanyID", companyId);
            check.Parameters.AddWithValue("@WarrantyId", warrantyCompanyId);
            var existing = check.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            string companyName = "";
            var nameCmd = new SqlCommand(
                "SELECT CompanyName FROM [msSchedulerV3].[dbo].[tbl_WarrantyCompany] WHERE WarrantyCompanyUID = @WarrantyId", con);
            nameCmd.Parameters.AddWithValue("@WarrantyId", warrantyCompanyId);
            companyName = nameCmd.ExecuteScalar()?.ToString() ?? "Unknown TP";

            var insert = new SqlCommand(
                @"INSERT INTO [msSchedulerV3].[dbo].[tbl_ThirdParties]
                  (CompanyID, ThirdPartyName, ThirdPartyType, IsActive, CreatedDate, CreatedBy)
                  OUTPUT INSERTED.Id VALUES (@CompanyID, @Name, 'Warranty', 1, GETDATE(), 'System')", con);
            insert.Parameters.AddWithValue("@CompanyID", companyId);
            insert.Parameters.AddWithValue("@Name", companyName);
            return (int)insert.ExecuteScalar();
        }

        private void LogStatusHistory(SqlConnection con, int workOrderId, string companyId, string status, string notes, string changedBy)
        {
            var cmd = new SqlCommand(
                @"INSERT INTO [msSchedulerV3].[dbo].[tbl_WorkOrderStatusHistory]
                  (WorkOrderId, Status, Notes, ChangedBy, ChangedDate, CompanyID)
                  VALUES (@WorkOrderId, @Status, @Notes, @ChangedBy, GETDATE(), @CompanyID)", con);
            cmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Notes", notes ?? "");
            cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? "System");
            cmd.Parameters.AddWithValue("@CompanyID", companyId);
            cmd.ExecuteNonQuery();
        }

        private WorkOrderEntity MapWorkOrder(IDataReader r)
        {
            return new WorkOrderEntity
            {
                Id = Convert.ToInt32(r["Id"]),
                CompanyID = r["CompanyID"].ToString(),
                WorkOrderNumber = r["WorkOrderNumber"].ToString(),
                ThirdPartyId = Convert.ToInt32(r["ThirdPartyId"]),
                Status = r["Status"].ToString(),
                RequestDate = Convert.ToDateTime(r["RequestDate"]),
                ServiceType = r["ServiceType"]?.ToString(),
                Description = r["Description"]?.ToString(),
                AppointmentId = r["AppointmentId"] != DBNull.Value ? (int?)Convert.ToInt32(r["AppointmentId"]) : null
            };
        }
    }
}
