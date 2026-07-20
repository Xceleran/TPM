using FSM.Models.TPM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace FSM.Processors
{
    public class CoverageItemProcessor
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();

        public int SaveCoverageItem(CoverageItemEntity item, string userId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                if (item.Id > 0)
                {
                    var upd = new SqlCommand(
                        @"UPDATE [msSchedulerV3].[dbo].[tbl_WorkOrderCoverageItems]
                          SET ItemDescription = @Desc, CoverageStatus = @Status, EstimatedAmount = @Est,
                              ApprovedAmount = @Appr, TPAuthorizationNumber = @AuthNum, UpdatedDate = GETDATE(), UpdatedBy = @User
                          WHERE Id = @Id", con);
                    upd.Parameters.AddWithValue("@Desc", item.ItemDescription);
                    upd.Parameters.AddWithValue("@Status", item.CoverageStatus);
                    upd.Parameters.AddWithValue("@Est", (object)item.EstimatedAmount ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Appr", (object)item.ApprovedAmount ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@AuthNum", (object)item.TPAuthorizationNumber ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@User", userId ?? "System");
                    upd.Parameters.AddWithValue("@Id", item.Id);
                    upd.ExecuteNonQuery();
                    return item.Id;
                }

                var ins = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_WorkOrderCoverageItems]
                      (CompanyID, WorkOrderId, ItemDescription, CoverageStatus, EstimatedAmount, ApprovedAmount, TPAuthorizationNumber, CreatedDate, CreatedBy)
                      OUTPUT INSERTED.Id
                      VALUES (@CompanyID, @WorkOrderId, @Desc, @Status, @Est, @Appr, @AuthNum, GETDATE(), @User)", con);
                ins.Parameters.AddWithValue("@CompanyID", item.CompanyID);
                ins.Parameters.AddWithValue("@WorkOrderId", item.WorkOrderId);
                ins.Parameters.AddWithValue("@Desc", item.ItemDescription);
                ins.Parameters.AddWithValue("@Status", item.CoverageStatus ?? "Pending");
                ins.Parameters.AddWithValue("@Est", (object)item.EstimatedAmount ?? DBNull.Value);
                ins.Parameters.AddWithValue("@Appr", (object)item.ApprovedAmount ?? DBNull.Value);
                ins.Parameters.AddWithValue("@AuthNum", (object)item.TPAuthorizationNumber ?? DBNull.Value);
                ins.Parameters.AddWithValue("@User", userId ?? "System");
                return (int)ins.ExecuteScalar();
            }
        }

        public List<CoverageItemEntity> GetByWorkOrder(string companyId, int workOrderId)
        {
            var list = new List<CoverageItemEntity>();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT * FROM [msSchedulerV3].[dbo].[tbl_WorkOrderCoverageItems]
                      WHERE CompanyID = @CompanyID AND WorkOrderId = @WorkOrderId ORDER BY Id", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new CoverageItemEntity
                        {
                            Id = Convert.ToInt32(r["Id"]),
                            CompanyID = r["CompanyID"].ToString(),
                            WorkOrderId = Convert.ToInt32(r["WorkOrderId"]),
                            ItemDescription = r["ItemDescription"].ToString(),
                            CoverageStatus = r["CoverageStatus"].ToString(),
                            EstimatedAmount = r["EstimatedAmount"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["EstimatedAmount"]) : null,
                            ApprovedAmount = r["ApprovedAmount"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["ApprovedAmount"]) : null,
                            TPAuthorizationNumber = r["TPAuthorizationNumber"]?.ToString(),
                            FAProInvoiceId = r["FAProInvoiceId"]?.ToString(),
                            FAProCustomerId = r["FAProCustomerId"]?.ToString()
                        });
                    }
                }
            }
            return list;
        }

        public string CreateNonCoveredCustomer(string companyId, int workOrderId, int appointmentId, string firstName, string lastName, string email, string phone, string address)
        {
            string customerGuid = Guid.NewGuid().ToString().ToUpper();
            string customerId = "";
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var maxCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(CAST(CustomerID AS int)), 0) + 1 FROM [msSchedulerV3].[dbo].[tbl_Customer] WHERE CompanyID = @CompanyID", con);
                maxCmd.Parameters.AddWithValue("@CompanyID", companyId);
                customerId = maxCmd.ExecuteScalar().ToString();

                var ins = new SqlCommand(
                    @"INSERT INTO [msSchedulerV3].[dbo].[tbl_Customer]
                      (CompanyID, CreatedCompanyID, CustomerID, CustomerGuid, FirstName, LastName, Email, Phone, Address1, Notes, IsBusinessContact, WarrentyCompanyID)
                      VALUES (@CompanyID, @CompanyID, @CustomerID, @Guid, @FirstName, @LastName, @Email, @Phone, @Address, @Notes, 0, 0)", con);
                ins.Parameters.AddWithValue("@CompanyID", companyId);
                ins.Parameters.AddWithValue("@CustomerID", customerId);
                ins.Parameters.AddWithValue("@Guid", customerGuid);
                ins.Parameters.AddWithValue("@FirstName", firstName ?? "Homeowner");
                ins.Parameters.AddWithValue("@LastName", lastName ?? "");
                ins.Parameters.AddWithValue("@Email", email ?? "");
                ins.Parameters.AddWithValue("@Phone", phone ?? "");
                ins.Parameters.AddWithValue("@Address", address ?? "");
                ins.Parameters.AddWithValue("@Notes", "Non-covered items customer for WO " + workOrderId + " / Appt " + appointmentId);
                ins.ExecuteNonQuery();

                var linkCmd = new SqlCommand(
                    @"UPDATE [msSchedulerV3].[dbo].[tbl_WorkOrderCoverageItems]
                      SET FAProCustomerId = @CustomerId
                      WHERE CompanyID = @CompanyID AND WorkOrderId = @WorkOrderId AND CoverageStatus = 'NotCovered'", con);
                linkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                linkCmd.Parameters.AddWithValue("@CompanyID", companyId);
                linkCmd.Parameters.AddWithValue("@WorkOrderId", workOrderId);
                linkCmd.ExecuteNonQuery();
            }
            return customerGuid;
        }

        public bool DeleteCoverageItem(string companyId, int itemId)
        {
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    "DELETE FROM [msSchedulerV3].[dbo].[tbl_WorkOrderCoverageItems] WHERE Id = @Id AND CompanyID = @CompanyID", con);
                cmd.Parameters.AddWithValue("@Id", itemId);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
