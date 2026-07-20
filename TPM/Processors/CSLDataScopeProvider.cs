using FSM.Models.TPM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace FSM.Processors
{
    public class CSLDataScopeProvider
    {
        private readonly string _connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        private readonly CoverageItemProcessor _coverageProcessor = new CoverageItemProcessor();

        public CslScopedData GetScopedData(string companyId, int appointmentId, int? workOrderId, DataScopeChannel channel)
        {
            var data = new CslScopedData();
            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                SqlCommand cmd;
                if (appointmentId > 0)
                {
                    cmd = new SqlCommand(
                        @"SELECT a.Status, a.ApptDateTime, a.ServiceType, a.Note,
                                 c.FirstName, c.LastName, c.Address1, c.City, c.State, c.ZipCode,
                                 r.Name AS ResourceName, wo.Status AS WoStatus, wo.Id AS WorkOrderId
                          FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r ON r.CompanyID = a.CompanyID AND r.Id = a.ResourceID
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_WorkOrders] wo ON wo.CompanyID = a.CompanyID AND wo.AppointmentId = a.ApptID
                          WHERE a.CompanyID = @CompanyID AND a.ApptID = @ApptID", con);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    cmd.Parameters.AddWithValue("@ApptID", appointmentId);
                }
                else if (workOrderId.HasValue && workOrderId.Value > 0)
                {
                    cmd = new SqlCommand(
                        @"SELECT a.Status, a.ApptDateTime, a.ServiceType, a.Note,
                                 c.FirstName, c.LastName, c.Address1, c.City, c.State, c.ZipCode,
                                 r.Name AS ResourceName, wo.Status AS WoStatus, wo.Id AS WorkOrderId, a.ApptID
                          FROM [msSchedulerV3].[dbo].[tbl_WorkOrders] wo
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_Appointment] a ON a.CompanyID = wo.CompanyID AND a.ApptID = wo.AppointmentId
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
                          LEFT JOIN [msSchedulerV3].[dbo].[tbl_Resources] r ON r.CompanyID = a.CompanyID AND r.Id = a.ResourceID
                          WHERE wo.CompanyID = @CompanyID AND wo.Id = @WorkOrderId", con);
                    cmd.Parameters.AddWithValue("@CompanyID", companyId);
                    cmd.Parameters.AddWithValue("@WorkOrderId", workOrderId.Value);
                }
                else
                {
                    return data;
                }

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        data.Status = r["WoStatus"]?.ToString() ?? r["Status"]?.ToString();
                        data.AppointmentDate = r["ApptDateTime"] != DBNull.Value ? Convert.ToDateTime(r["ApptDateTime"]).ToString("g") : "";
                        data.ServiceType = r["ServiceType"]?.ToString();
                        data.TechnicianName = r["ResourceName"]?.ToString();
                        data.CustomerName = (r["FirstName"]?.ToString() + " " + r["LastName"]?.ToString()).Trim();
                        data.SiteAddress = string.Join(", ", new[] { r["Address1"]?.ToString(), r["City"]?.ToString(), r["State"]?.ToString(), r["ZipCode"]?.ToString() });
                        if (!workOrderId.HasValue && r["WorkOrderId"] != DBNull.Value)
                            workOrderId = Convert.ToInt32(r["WorkOrderId"]);
                        if (appointmentId <= 0 && HasColumn(r, "ApptID") && r["ApptID"] != DBNull.Value)
                            appointmentId = Convert.ToInt32(r["ApptID"]);
                    }
                }
            }

            if (appointmentId > 0)
                data.PublicNotes = GetNotesForChannel(companyId, appointmentId, channel);

            if (channel == DataScopeChannel.ThirdParty && workOrderId.HasValue)
                data.CoverageItems = _coverageProcessor.GetByWorkOrder(companyId, workOrderId.Value);

            if (channel == DataScopeChannel.PolicyHolder)
            {
                data.CoverageItems = new List<CoverageItemEntity>();
            }

            return data;
        }

        public List<string> GetNotesForChannel(string companyId, int appointmentId, DataScopeChannel channel)
        {
            var notes = new List<string>();
            string visibilityFilter = "Internal";
            switch (channel)
            {
                case DataScopeChannel.PolicyHolder:
                    visibilityFilter = "PolicyHolder";
                    break;
                case DataScopeChannel.ThirdParty:
                    visibilityFilter = "ThirdParty";
                    break;
                case DataScopeChannel.Staff:
                    return notes;
            }

            using (var con = new SqlConnection(_connStr))
            {
                con.Open();
                var cmd = new SqlCommand(
                    @"SELECT Description FROM [msSchedulerV3].[dbo].[tbl_Note]
                      WHERE CompanyID = @CompanyID AND AppointmentId = @ApptID
                        AND (VisibilityScope = @Scope OR VisibilityScope = 'Public')
                        AND ISNULL(IsAiAccessible, 0) = 1", con);
                cmd.Parameters.AddWithValue("@CompanyID", companyId);
                cmd.Parameters.AddWithValue("@ApptID", appointmentId.ToString());
                cmd.Parameters.AddWithValue("@Scope", visibilityFilter);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        notes.Add(r["Description"]?.ToString());
                }
            }
            return notes;
        }

        public string BuildAiContext(CslScopedData data, DataScopeChannel channel)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Status: " + (data.Status ?? "Unknown"));
            sb.AppendLine("Appointment: " + (data.AppointmentDate ?? "TBD"));
            if (!string.IsNullOrEmpty(data.TechnicianName))
                sb.AppendLine("Technician: " + data.TechnicianName);
            if (!string.IsNullOrEmpty(data.ServiceType))
                sb.AppendLine("Service: " + data.ServiceType);
            if (channel != DataScopeChannel.PolicyHolder && data.CoverageItems != null)
            {
                foreach (var item in data.CoverageItems)
                    sb.AppendLine("Coverage item: " + item.ItemDescription + " - " + item.CoverageStatus);
            }
            foreach (var note in data.PublicNotes)
                sb.AppendLine("Note: " + note);
            return sb.ToString();
        }

        private static bool HasColumn(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
