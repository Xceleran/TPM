using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using FSM.Helper;

namespace FSM
{
    public partial class SMSSettings : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.AppSettings["ConnString"].ToString();
        protected void Page_Load(object sender, EventArgs e)
        {
           
            string CompanyID = Session["CompanyID"].ToString();
            hdCompanyID.Value = CompanyID;

            if (Page.IsPostBack == false)
            {

                LoadSettings();
                
            }

        }

        private void LoadSettings()
        {
            

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT * FROM tbl_FSMSMSSettings WHERE CompanyId = @CompanyId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        PendingYN.Checked = dr["PendingYN"] != DBNull.Value && Convert.ToBoolean(dr["PendingYN"]);
                        txtPending.Value = dr["PendingText"] != DBNull.Value ? dr["PendingText"].ToString() : "";

                        CancelledYN.Checked = dr["CancelledYN"] != DBNull.Value && Convert.ToBoolean(dr["CancelledYN"]);
                        txtCancelled.Value = dr["CancelledText"] != DBNull.Value ? dr["CancelledText"].ToString() : "";

                        ProgressYN.Checked = dr["ProgressYN"] != DBNull.Value && Convert.ToBoolean(dr["ProgressYN"]);
                        txtProgress.Value = dr["ProgressText"] != DBNull.Value ? dr["ProgressText"].ToString() : "";

                        ScheduledYN.Checked = dr["ScheduledYN"] != DBNull.Value && Convert.ToBoolean(dr["ScheduledYN"]);
                        txtScheduled.Value = dr["ScheduledText"] != DBNull.Value ? dr["ScheduledText"].ToString() : "";

                        ClosedYN.Checked = dr["ClosedYN"] != DBNull.Value && Convert.ToBoolean(dr["ClosedYN"]);
                        txtClosed.Value = dr["ClosedText"] != DBNull.Value ? dr["ClosedText"].ToString() : "";

                        CompletedYN.Checked = dr["CompletedYN"] != DBNull.Value && Convert.ToBoolean(dr["CompletedYN"]);
                        txtCompleted.Value = dr["CompletedText"] != DBNull.Value ? dr["CompletedText"].ToString() : "";
                    }
                }
            }
        }

        protected void SubmitData_Click(object sender, EventArgs e)
        {

            try
            {

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Delete old row
                    string deleteQuery = "DELETE FROM tbl_FSMSMSSettings WHERE CompanyId = @CompanyId";
                    SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);
                    deleteCmd.ExecuteNonQuery();

                    // Insert new row
                    string insertQuery = @"
                INSERT INTO tbl_FSMSMSSettings
                (CompanyId, PendingYN, PendingText, CancelledYN, CancelledText, ProgressYN, ProgressText, 
                 ScheduledYN, ScheduledText, ClosedYN, ClosedText, CompletedYN, CompletedText, 
                  CreateDate)
                VALUES
                (@CompanyId, @PendingYN, @PendingText, @CancelledYN, @CancelledText, @ProgressYN, @ProgressText, 
                 @ScheduledYN, @ScheduledText, @ClosedYN, @ClosedText, @CompletedYN, @CompletedText, 
                  GETDATE()) ";

                    SqlCommand cmd = new SqlCommand(insertQuery, conn);

                    cmd.Parameters.AddWithValue("@CompanyId", hdCompanyID.Value);

                    // Pending
                    cmd.Parameters.AddWithValue("@PendingYN", PendingYN.Checked);
                    cmd.Parameters.AddWithValue("@PendingText", string.IsNullOrEmpty(txtPending.Value) ? (object)DBNull.Value : txtPending.Value);

                    // Cancelled
                    cmd.Parameters.AddWithValue("@CancelledYN", CancelledYN.Checked);
                    cmd.Parameters.AddWithValue("@CancelledText", string.IsNullOrEmpty(txtCancelled.Value) ? (object)DBNull.Value : txtCancelled.Value);

                    // Progress
                    cmd.Parameters.AddWithValue("@ProgressYN", ProgressYN.Checked);
                    cmd.Parameters.AddWithValue("@ProgressText", string.IsNullOrEmpty(txtProgress.Value) ? (object)DBNull.Value : txtProgress.Value);

                    // Scheduled
                    cmd.Parameters.AddWithValue("@ScheduledYN", ScheduledYN.Checked);
                    cmd.Parameters.AddWithValue("@ScheduledText", string.IsNullOrEmpty(txtScheduled.Value) ? (object)DBNull.Value : txtScheduled.Value);

                    // Closed
                    cmd.Parameters.AddWithValue("@ClosedYN", ClosedYN.Checked);
                    cmd.Parameters.AddWithValue("@ClosedText", string.IsNullOrEmpty(txtClosed.Value) ? (object)DBNull.Value : txtClosed.Value);

                    // Completed
                    cmd.Parameters.AddWithValue("@CompletedYN", CompletedYN.Checked);
                    cmd.Parameters.AddWithValue("@CompletedText", string.IsNullOrEmpty(txtCompleted.Value) ? (object)DBNull.Value : txtCompleted.Value);


                    cmd.ExecuteNonQuery();
                }

                string success = "Swal.fire('Saved!', 'Your SMS settings have been Saved.', 'success')" +
                                 ".then(function(){ window.location.replace('SMSSettings.aspx'); });";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SuccessAlertScript", success, true);
            }
            catch (Exception ex)
            {
                string error = $"Swal.fire('Error!', '{ex.Message.Replace("'", "\\'")}', 'error');";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ErrorAlertScript", error, true);
            }
        }


    }
}