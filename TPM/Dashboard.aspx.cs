using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using FSM.Processors;

namespace FSM
{
    public partial class Dashboard : System.Web.UI.Page
    {
        static string AccountsUrl = ConfigurationManager.AppSettings["Accounts_Xinator_Url"].ToString();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["CompanyID"] == null || Request.QueryString["logout"] == "true")
            {
                Session.Clear();
                Session.Abandon();
                if (!AccountsUrl.EndsWith("/"))
                {
                    AccountsUrl += "/";
                }
                string url = AccountsUrl + "Login.aspx";
                Response.Redirect(url);
                return;
            }

            if (!IsPostBack)
            {
                LoadDashboard();
            }
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            SetHeader();
            string companyID = Session["CompanyID"] as string ?? string.Empty;

            lblTotalProviders.Text = GetCount(@"
                SELECT COUNT(1)
                FROM [msSchedulerV3].[dbo].[tbl_Customer]
                WHERE CompanyID = @CompanyID
                  AND IsBusinessContact = 1
                  AND TRY_CAST(WarrentyCompanyID AS BIGINT) > 0",
                companyID).ToString();

            // Pending TPM work-order requests. Deliberately runs the same stored
            // procedure the New Work Orders page uses (Sp_GetAppointmnetData with
            // @Status='pending') with the same default +/-60 day window, so the
            // tile can never disagree with the grid it links to.
            lblNewWorkOrders.Text = GetPendingWorkOrderCount(companyID).ToString();

            // Approved, not closed/cancelled/deleted (mirrors Appointments.aspx + Customer.aspx filters).
            lblActiveAppointments.Text = GetCount(@"
                SELECT COUNT(1)
                FROM [msSchedulerV3].[dbo].[tbl_Appointment]
                WHERE CompanyID = @CompanyID
                  AND IsApproved = 1
                  AND Status NOT IN ('Deleted','Closed','Cancelled','Canceled')",
                companyID).ToString();

            // tbl_Assignment has no status column, so "active" = assignment whose
            // appointment is still open. No prebuilt query exists; this is the definition.
            lblActiveAssignments.Text = GetCount(@"
                SELECT COUNT(1)
                FROM [msSchedulerV3].[dbo].[tbl_Assignment] a
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Appointment] ap
                    ON ap.CompanyID = a.CompanyID AND ap.ApptID = a.ApptID
                WHERE a.CompanyID = @CompanyID
                  AND ap.Status NOT IN ('Deleted','Closed','Cancelled','Canceled')",
                companyID).ToString();

            LoadAnnouncements(companyID);
            LoadRecentActivity(companyID);
        }

        private void SetHeader()
        {
            int h = DateTime.Now.Hour;
            lblGreeting.Text = h < 12 ? "Good morning" : (h < 17 ? "Good afternoon" : "Good evening");
            string name = Convert.ToString(Session["UserFirstName"] ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                name = Convert.ToString(Session["LoginUser"] ?? "Demo");
            lblUserName.Text = name;
            lblDateLine.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy");
            lblUpdated.Text = "Updated " + DateTime.Now.ToString("h:mm tt");
        }

        private void LoadAnnouncements(string companyID)
        {
            // Shared board: tbl_DashAnnouncement scoped to AppSource = 'TPM'
            // (same table as JobScheduler 'CEC' and FSM-OLD 'FSM').
            DataTable dt = new DataTable();
            dt.Columns.Add("Title", typeof(string));
            dt.Columns.Add("Announcement", typeof(string));
            dt.Columns.Add("WhenText", typeof(string));
            try
            {
                var list = new DashAnnouncementProcessor().GetActiveAnnouncements();
                foreach (var a in list.Take(5))
                {
                    DataRow r = dt.NewRow();
                    r["Title"] = string.IsNullOrWhiteSpace(a.Title) ? "Announcement" : a.Title;
                    r["Announcement"] = a.Description ?? string.Empty;
                    r["WhenText"] = (a.StartDate ?? a.CreatedOn).ToString("MMM d, yyyy");
                    dt.Rows.Add(r);
                }
            }
            catch { dt.Rows.Clear(); }

            rptAnnouncements.DataSource = dt;
            rptAnnouncements.DataBind();
            pnlNoAnnouncements.Visible = dt.Rows.Count == 0;
        }

        private void LoadRecentActivity(string companyID)
        {
            DataTable dt = null;
            try
            {
                dt = Query(@"
                    SELECT TOP 5 a.ApptID, a.Status, a.CreatedDateTime,
                        ISNULL(NULLIF(c.BusinessName, ''), ISNULL(c.FirstName + ' ' + c.LastName, 'Customer')) AS CustomerName
                    FROM [msSchedulerV3].[dbo].[tbl_Appointment] a
                    LEFT JOIN [msSchedulerV3].[dbo].[tbl_Customer] c
                        ON c.CustomerID = a.CustomerID AND c.CompanyID = a.CompanyID
                    WHERE a.CompanyID = @CompanyID
                    ORDER BY a.CreatedDateTime DESC",
                    companyID);
                dt.Columns.Add("Headline", typeof(string));
                dt.Columns.Add("WhenText", typeof(string));
                foreach (DataRow r in dt.Rows)
                {
                    r["Headline"] = "Work order #" + Convert.ToString(r["ApptID"])
                        + " — " + Convert.ToString(r["CustomerName"])
                        + " (" + Convert.ToString(r["Status"]) + ")";
                    r["WhenText"] = Convert.ToDateTime(r["CreatedDateTime"]).ToString("MMM d, yyyy h:mm tt");
                }
            }
            catch { dt = null; }

            rptRecent.DataSource = dt;
            rptRecent.DataBind();
            pnlNoRecent.Visible = dt == null || dt.Rows.Count == 0;
        }

        private DataTable Query(string sql, string companyID)
        {
            Database db = new Database();
            db.AddParameter("@CompanyID", companyID ?? string.Empty, SqlDbType.NVarChar);
            DataTable dt = new DataTable();
            db.ExecuteParam(sql, out dt);
            return dt;
        }

        /// <summary>
        /// Counts pending TPM work orders by running the exact same stored procedure
        /// and parameters as AppoinementList.aspx (New Work Orders), then counting rows.
        /// </summary>
        private int GetPendingWorkOrderCount(string companyID)
        {
            try
            {
                string connStr = ConfigurationManager.AppSettings["ConnString"];
                using (var con = new SqlConnection(connStr))
                using (var cmd = new SqlCommand("Sp_GetAppointmnetData", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 900;
                    cmd.Parameters.Add("@CompanyId", SqlDbType.NVarChar, 200).Value = companyID ?? string.Empty;
                    cmd.Parameters.Add("@SearchBy", SqlDbType.NVarChar, 200).Value = string.Empty;
                    cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar, 500).Value = string.Empty;
                    cmd.Parameters.Add("@From", SqlDbType.DateTime).Value = DateTime.Now.AddDays(-60);
                    cmd.Parameters.Add("@To", SqlDbType.DateTime).Value = DateTime.Now.AddDays(60);
                    cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 500).Value = "pending";
                    cmd.Parameters.Add("@IsWarrantyCompany", SqlDbType.NVarChar, 500).Value = DBNull.Value;
                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        int n = 0;
                        while (rdr.Read()) n++;
                        return n;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetPendingWorkOrderCount: " + ex);
                return 0;
            }
        }

        private int GetCount(string sql, string companyID)
        {
            try
            {
                DataTable dt = Query(sql, companyID);
                if (dt.Rows.Count > 0 && dt.Columns.Count > 0
                    && int.TryParse(Convert.ToString(dt.Rows[0][0]), out int n))
                {
                    return n;
                }
            }
            catch
            {
                // Offline / unreachable DB -> show 0 like the rest of the app's swallowed errors.
            }
            return 0;
        }
    }
}
