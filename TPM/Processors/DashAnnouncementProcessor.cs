using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using FSM.Entity;

namespace FSM.Processors
{
    public class DashAnnouncementProcessor
    {
        public const string AppSource = "TPM";

        private static string ConnStr
        {
            get { return ConfigurationManager.AppSettings["ConnString"].ToString(); }
        }

        private static void EnsureColumns(SqlConnection con)
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[msSchedulerV3].[dbo].[tbl_DashAnnouncement]') AND name = 'CreatedBy')
    ALTER TABLE [msSchedulerV3].[dbo].[tbl_DashAnnouncement] ADD CreatedBy NVARCHAR(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[msSchedulerV3].[dbo].[tbl_DashAnnouncement]') AND name = 'AppSource')
    ALTER TABLE [msSchedulerV3].[dbo].[tbl_DashAnnouncement] ADD AppSource NVARCHAR(20) NULL;";
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        private static DashAnnouncementEntity MapRow(DataRow dr)
        {
            return new DashAnnouncementEntity
            {
                AnnouncementID = Convert.ToInt32(dr["AnnouncementID"]),
                Title = dr["Title"].ToString(),
                Description = dr["Description"].ToString(),
                IsActive = Convert.ToBoolean(dr["IsActive"]),
                DurationPreset = dr["DurationPreset"] == DBNull.Value ? null : dr["DurationPreset"].ToString(),
                StartDate = dr["StartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["StartDate"]),
                EndDate = dr["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["EndDate"]),
                CreatedOn = dr["CreatedOn"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(dr["CreatedOn"]),
                CreatedBy = dr.Table.Columns.Contains("CreatedBy") && dr["CreatedBy"] != DBNull.Value ? dr["CreatedBy"].ToString() : "",
                AppSource = dr.Table.Columns.Contains("AppSource") && dr["AppSource"] != DBNull.Value ? dr["AppSource"].ToString() : null
            };
        }

        public List<DashAnnouncementEntity> GetAllAnnouncements()
        {
            var list = new List<DashAnnouncementEntity>();
            const string sql = @"
SELECT AnnouncementID, Title, Description, IsActive, DurationPreset,
       StartDate, EndDate, CreatedOn,
       ISNULL(CreatedBy,'') AS CreatedBy, AppSource
FROM [msSchedulerV3].[dbo].[tbl_DashAnnouncement]
WHERE (AppSource IS NULL OR AppSource = @AppSource)
ORDER BY CreatedOn DESC";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AppSource", AppSource);
                con.Open();
                try { EnsureColumns(con); } catch { }
                using (var dr = cmd.ExecuteReader())
                {
                    var dt = new DataTable();
                    dt.Load(dr);
                    foreach (DataRow row in dt.Rows) list.Add(MapRow(row));
                }
            }
            return list;
        }

        public List<DashAnnouncementEntity> GetActiveAnnouncements()
        {
            var list = new List<DashAnnouncementEntity>();
            const string sql = @"
SELECT AnnouncementID, Title, Description, IsActive, DurationPreset,
       StartDate, EndDate, CreatedOn,
       ISNULL(CreatedBy,'') AS CreatedBy, AppSource
FROM [msSchedulerV3].[dbo].[tbl_DashAnnouncement]
WHERE IsActive = 1
  AND (AppSource IS NULL OR AppSource = @AppSource)
  AND (StartDate IS NULL OR CAST(StartDate AS DATE) <= @Today)
  AND (EndDate IS NULL OR CAST(EndDate AS DATE) >= @Today)
ORDER BY AnnouncementID DESC";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AppSource", AppSource);
                cmd.Parameters.Add("@Today", SqlDbType.Date).Value = DateTime.Today;
                con.Open();
                try { EnsureColumns(con); } catch { }
                using (var dr = cmd.ExecuteReader())
                {
                    var dt = new DataTable();
                    dt.Load(dr);
                    foreach (DataRow row in dt.Rows) list.Add(MapRow(row));
                }
            }
            return list;
        }

        public DashAnnouncementEntity GetAnnouncementByID(int id)
        {
            const string sql = @"
SELECT AnnouncementID, Title, Description, IsActive, DurationPreset,
       StartDate, EndDate, CreatedOn,
       ISNULL(CreatedBy,'') AS CreatedBy, AppSource
FROM [msSchedulerV3].[dbo].[tbl_DashAnnouncement]
WHERE AnnouncementID = @AnnouncementID";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AnnouncementID", id);
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    var dt = new DataTable();
                    dt.Load(dr);
                    if (dt.Rows.Count > 0) return MapRow(dt.Rows[0]);
                }
            }
            return new DashAnnouncementEntity();
        }

        public int AddAnnouncement(DashAnnouncementEntity entity)
        {
            const string sql = @"
INSERT INTO [msSchedulerV3].[dbo].[tbl_DashAnnouncement]
    (Title, Description, IsActive, DurationPreset, StartDate, EndDate, CreatedBy, AppSource, CreatedOn)
VALUES
    (@Title, @Description, @IsActive, @DurationPreset, @StartDate, @EndDate, @CreatedBy, @AppSource, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Title", (object)entity.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                cmd.Parameters.AddWithValue("@DurationPreset", (object)entity.DurationPreset ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", entity.StartDate.HasValue ? (object)entity.StartDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", entity.EndDate.HasValue ? (object)entity.EndDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedBy", (object)entity.CreatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AppSource", AppSource);
                con.Open();
                try { EnsureColumns(con); } catch { }
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public bool UpdateAnnouncement(DashAnnouncementEntity entity)
        {
            const string sql = @"
UPDATE [msSchedulerV3].[dbo].[tbl_DashAnnouncement]
SET Title = @Title, Description = @Description, IsActive = @IsActive,
    DurationPreset = @DurationPreset, StartDate = @StartDate, EndDate = @EndDate
WHERE AnnouncementID = @AnnouncementID";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AnnouncementID", entity.AnnouncementID);
                cmd.Parameters.AddWithValue("@Title", (object)entity.Title ?? "");
                cmd.Parameters.AddWithValue("@Description", (object)entity.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", entity.IsActive);
                cmd.Parameters.AddWithValue("@DurationPreset", (object)entity.DurationPreset ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", entity.StartDate.HasValue ? (object)entity.StartDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", entity.EndDate.HasValue ? (object)entity.EndDate.Value : DBNull.Value);
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteAnnouncement(int id)
        {
            const string sql = @"DELETE FROM [msSchedulerV3].[dbo].[tbl_DashAnnouncement] WHERE AnnouncementID = @AnnouncementID";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AnnouncementID", id);
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ToggleAnnouncementStatus(int id, bool isActive)
        {
            const string sql = @"UPDATE [msSchedulerV3].[dbo].[tbl_DashAnnouncement] SET IsActive = @IsActive WHERE AnnouncementID = @AnnouncementID";
            using (var con = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@AnnouncementID", id);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
