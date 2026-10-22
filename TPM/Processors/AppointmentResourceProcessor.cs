using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;


namespace CECPro
{
    /// <summary>
    /// One assigned resource (technician) on an appointment.
    /// </summary>
    public class AssignedResource
    {
        public int ResourceID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Reads and writes the crew of an appointment - the one-to-many
    /// msSchedulerV3.dbo.tbl_AppointmentResource (see SQL\tbl_AppointmentResource.sql).
    ///
    /// THE INVARIANT, and the reason this class exists rather than pages writing the table
    /// directly: tbl_Appointment.ResourceID still holds the PRIMARY (lead) resource, and the
    /// crew table holds EVERY assigned resource INCLUDING that primary, IsPrimary = 1 on
    /// exactly one row. The two sides must move together, so <see cref="SaveAssignments"/> is
    /// the only supported way to change a crew.
    ///
    /// Keeping the primary in tbl_Appointment is what lets the ~45 files that read
    /// ResourceID keep working untouched (commission, reports, FSM sync and the technician
    /// mobile API are all deliberately left reading the primary).
    ///
    /// ApptID IS PER COMPANY, not globally unique - Processor\AppoinmentProcessorcs.cs
    /// GetApptID allocates MAX(ApptID)+1 within a CompanyID - so every statement here filters
    /// on CompanyID as well. Dropping it would leak one tenant's crew onto another's job.
    ///
    /// Every read falls back to tbl_Appointment.ResourceID when an appointment has no crew
    /// rows yet. That is what makes the backfill in the SQL script a tidy-up rather than a
    /// cutover: callers behave correctly before it is run, during, and after.
    /// </summary>
    public class AppointmentResourceProcessor
    {
        private readonly string connStr =
            ConfigurationManager.AppSettings["ConnStrSch"].ToString();

        #region Read

        /// <summary>
        /// The crew of one appointment, primary first then by name. Resources that have since
        /// been deleted drop out (the join is inner, and tbl_Resources rows are hard-deleted).
        ///
        /// Falls back to the single tbl_Appointment.ResourceID, reported as the primary, when
        /// the appointment has no crew rows.
        /// </summary>
        public List<AssignedResource> GetAssignedResources(string companyID, string apptID)
        {
            List<AssignedResource> assigned = new List<AssignedResource>();

            int apptId;
            if (string.IsNullOrWhiteSpace(companyID) || !int.TryParse(apptID, out apptId))
            {
                return assigned;
            }

            const string sql = @"
                SELECT  r.[Id]        AS ResourceID,
                        r.[Name]      AS Name,
                        r.[email]     AS Email,
                        r.[Mobile]    AS Mobile,
                        m.[IsPrimary] AS IsPrimary
                FROM    [msSchedulerV3].[dbo].[tbl_AppointmentResource] m
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Resources] r
                        ON  r.[Id]        = m.[ResourceID]
                        AND r.[CompanyID] = m.[CompanyID]
                WHERE   m.[CompanyID] = @CompanyID
                  AND   m.[ApptID]    = @ApptID
                  AND   m.[IsActive]  = 1
                ORDER BY m.[IsPrimary] DESC, r.[Name];";

            // A crew table that cannot be read is treated exactly like an appointment with no
            // crew rows: fall through to the lead on tbl_Appointment below. That is what lets
            // every page keep working before SQL\tbl_AppointmentResource.sql has been run -
            // the alternative is that a read throws into whatever the caller's catch is, which
            // on the dispatch board meant a completely blank board.
            try
            {
                DataTable dt = new DataTable();
                Database db = new Database(connStr);
                db.Command = new SqlCommand();
                db.Command.CommandText = sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", companyID);
                db.Command.Parameters.AddWithValue("@ApptID", apptId);
                db.Execute(out dt);

                foreach (DataRow row in dt.Rows)
                {
                    assigned.Add(new AssignedResource
                    {
                        ResourceID = Convert.ToInt32(row["ResourceID"]),
                        Name = row["Name"].ToString(),
                        Email = row["Email"].ToString(),
                        Mobile = row["Mobile"].ToString(),
                        IsPrimary = row["IsPrimary"] != DBNull.Value && Convert.ToBoolean(row["IsPrimary"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Crew read failed for appointment " + apptID + ", falling back to the lead - "
                    + ex.Message);
                assigned.Clear();
            }

            if (assigned.Count == 0)
            {
                AssignedResource legacy = GetLegacyResource(companyID, apptId);
                if (legacy != null) assigned.Add(legacy);
            }

            return assigned;
        }

        /// <summary>
        /// Just the ids, primary first. Convenience for the UI preselect and for callers that
        /// only need to know who is on the job.
        /// </summary>
        public List<int> GetAssignedResourceIds(string companyID, string apptID)
        {
            return GetAssignedResources(companyID, apptID)
                   .Select(a => a.ResourceID)
                   .ToList();
        }

        /// <summary>
        /// Crews for a whole date range in ONE query, keyed by ApptID.
        ///
        /// The dispatch and resource-wise views render a range at a time; calling
        /// <see cref="GetAssignedResources"/> per appointment would be an N+1 against the hot
        /// path. Appointments with no crew rows are absent from the dictionary - callers must
        /// fall back to the row's own ResourceID, which is what keeps pre-backfill data
        /// rendering correctly.
        /// </summary>
        /// <param name="startDate">
        /// Half-open range start, or null for "every appointment this company has". The list
        /// pages let the user clear the date filter, and their own query is unbounded when
        /// they do - the crew read has to be able to match that or the rows it cannot see
        /// would silently fall back to showing the lead alone.
        /// </param>
        public Dictionary<int, List<AssignedResource>> GetAssignedResourcesForRange(
            string companyID, DateTime? startDate = null, DateTime? endDate = null)
        {
            Dictionary<int, List<AssignedResource>> byAppt =
                new Dictionary<int, List<AssignedResource>>();

            if (string.IsNullOrWhiteSpace(companyID)) return byAppt;

            // DISTINCT is load-bearing, not defensive tidiness. tbl_Appointment has no unique
            // constraint on (CompanyID, ApptID) and really does hold duplicate rows for one
            // appointment - GetApptID allocates MAX(ApptID)+1 read-then-insert, with a known
            // double-insert path on the create page. Joining to it therefore multiplies each
            // crew row by the number of copies, which would render the same job several times
            // in a resource's lane. The selected columns are identical across those copies, so
            // collapsing them is exactly right. (SQL\tbl_AppointmentResource.sql has the
            // diagnostic that lists the duplicates.)
            const string sql = @"
                SELECT DISTINCT
                        m.[ApptID]    AS ApptID,
                        r.[Id]        AS ResourceID,
                        r.[Name]      AS Name,
                        r.[email]     AS Email,
                        r.[Mobile]    AS Mobile,
                        m.[IsPrimary] AS IsPrimary
                FROM    [msSchedulerV3].[dbo].[tbl_AppointmentResource] m
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Resources] r
                        ON  r.[Id]        = m.[ResourceID]
                        AND r.[CompanyID] = m.[CompanyID]
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Appointment] a
                        ON  a.[ApptID]    = m.[ApptID]
                        AND a.[CompanyID] = m.[CompanyID]
                WHERE   m.[CompanyID] = @CompanyID
                  AND   m.[IsActive]  = 1
                  AND   (@StartDate IS NULL OR a.[StartDateTime] >= @StartDate)
                  AND   (@EndDate   IS NULL OR a.[StartDateTime] <  @EndDate)
                ORDER BY m.[ApptID], m.[IsPrimary] DESC, r.[Name];";

            DataTable dt = new DataTable();

            // As in GetAssignedResources: an unreadable crew table is "no crew rows". Every
            // caller of this already falls back to the appointment's own ResourceID for the
            // ApptIDs the dictionary does not mention, so an empty dictionary renders the whole
            // range lead-only instead of throwing and rendering nothing at all.
            try
            {
                Database db = new Database(connStr);
                db.Command = new SqlCommand();
                db.Command.CommandText = sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", companyID);
                db.Command.Parameters.AddWithValue("@StartDate", (object)startDate ?? DBNull.Value);
                db.Command.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);
                db.Execute(out dt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Crew range read failed, falling back to leads - " + ex.Message);
                return byAppt;
            }

            foreach (DataRow row in dt.Rows)
            {
                int apptId = Convert.ToInt32(row["ApptID"]);
                if (!byAppt.ContainsKey(apptId))
                {
                    byAppt[apptId] = new List<AssignedResource>();
                }

                byAppt[apptId].Add(new AssignedResource
                {
                    ResourceID = Convert.ToInt32(row["ResourceID"]),
                    Name = row["Name"].ToString(),
                    Email = row["Email"].ToString(),
                    Mobile = row["Mobile"].ToString(),
                    IsPrimary = row["IsPrimary"] != DBNull.Value && Convert.ToBoolean(row["IsPrimary"])
                });
            }

            return byAppt;
        }

        /// <summary>
        /// Every ApptID in a date range that <paramref name="resourceId"/> is on the crew of,
        /// as lead OR as helper.
        ///
        /// This exists for the pages that filter by resource inside a hand-built query string
        /// (calendar.aspx, Appointmentlist.aspx). Those say "and ResourceID = x", which is the
        /// lead only, so filtering to a person hid every job where they were additional crew -
        /// the opposite of what someone filtering to a person wants. Widening the condition in
        /// SQL would mean naming tbl_AppointmentResource from those strings, and the table may
        /// not exist yet, so the page would 500 instead of degrading. Pulling the ids here and
        /// widening the condition to "ResourceID = x OR ApptID IN (...)" leaves every other
        /// filter, every result set in the batch and the pre-backfill fallback intact.
        /// </summary>
        /// <returns>
        /// Empty when the resource is on no crew, when the id is not a positive number, or
        /// when the crew table is not there yet. In all three the caller is right to fall back
        /// to the plain lead-only condition, which is why this swallows rather than throws.
        /// </returns>
        public List<int> GetApptIdsForResource(
            string companyID, string resourceId,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            List<int> apptIds = new List<int>();

            int resId;
            if (string.IsNullOrWhiteSpace(companyID) ||
                !int.TryParse((resourceId ?? "").Trim(), out resId) || resId <= 0)
            {
                return apptIds;
            }

            const string sql = @"
                SELECT DISTINCT m.[ApptID]
                FROM    [msSchedulerV3].[dbo].[tbl_AppointmentResource] m
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Appointment] a
                        ON  a.[ApptID]    = m.[ApptID]
                        AND a.[CompanyID] = m.[CompanyID]
                WHERE   m.[CompanyID]  = @CompanyID
                  AND   m.[ResourceID] = @ResourceID
                  AND   m.[IsActive]   = 1
                  AND   (@StartDate IS NULL OR a.[StartDateTime] >= @StartDate)
                  AND   (@EndDate   IS NULL OR a.[StartDateTime] <  @EndDate);";

            try
            {
                DataTable dt = new DataTable();
                Database db = new Database(connStr);
                db.Command = new SqlCommand();
                db.Command.CommandText = sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", companyID);
                db.Command.Parameters.AddWithValue("@ResourceID", resId);
                db.Command.Parameters.AddWithValue("@StartDate", (object)startDate ?? DBNull.Value);
                db.Command.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);
                db.Execute(out dt);

                foreach (DataRow row in dt.Rows)
                {
                    int apptId;
                    if (int.TryParse(row["ApptID"].ToString(), out apptId))
                    {
                        apptIds.Add(apptId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "GetApptIdsForResource fell back to lead-only: " + ex.Message);
                apptIds.Clear();
            }

            return apptIds;
        }

        /// <summary>
        /// Every ApptID in a date range that the resources behind ONE LOGIN are on the crew
        /// of, as lead or as helper.
        ///
        /// The technician mobile API is the caller. It identifies the signed-in technician by
        /// tbl_Resources.UserID rather than by resource id, and that is not a unique column -
        /// one login can map to more than one resource row - so resolving the login to ids
        /// first and then calling <see cref="GetApptIdsForResource"/> per id would be a round
        /// trip each. The join does it in one.
        ///
        /// CompanyID is applied to the resource join as well as the crew rows. The API's own
        /// list queries match the login on UserID alone, which only holds because their outer
        /// joins are company-scoped; this read does not rely on that.
        /// </summary>
        /// <returns>
        /// Empty when the login is on no crew, when no login was supplied, or when the crew
        /// table is not there yet - see <see cref="GetApptIdsForResource"/> for why this
        /// swallows rather than throws.
        /// </returns>
        public List<int> GetApptIdsForUser(
            string companyID, string userId,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            List<int> apptIds = new List<int>();

            if (string.IsNullOrWhiteSpace(companyID) || string.IsNullOrWhiteSpace(userId))
            {
                return apptIds;
            }

            const string sql = @"
                SELECT DISTINCT m.[ApptID]
                FROM    [msSchedulerV3].[dbo].[tbl_AppointmentResource] m
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Resources] r
                        ON  r.[Id]        = m.[ResourceID]
                        AND r.[CompanyID] = m.[CompanyID]
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Appointment] a
                        ON  a.[ApptID]    = m.[ApptID]
                        AND a.[CompanyID] = m.[CompanyID]
                WHERE   m.[CompanyID] = @CompanyID
                  AND   m.[IsActive]  = 1
                  AND   r.[UserID]    = @UserID
                  AND   (@StartDate IS NULL OR a.[StartDateTime] >= @StartDate)
                  AND   (@EndDate   IS NULL OR a.[StartDateTime] <  @EndDate);";

            try
            {
                DataTable dt = new DataTable();
                Database db = new Database(connStr);
                db.Command = new SqlCommand();
                db.Command.CommandText = sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", companyID);
                db.Command.Parameters.AddWithValue("@UserID", userId);
                db.Command.Parameters.AddWithValue("@StartDate", (object)startDate ?? DBNull.Value);
                db.Command.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);
                db.Execute(out dt);

                foreach (DataRow row in dt.Rows)
                {
                    int apptId;
                    if (int.TryParse(row["ApptID"].ToString(), out apptId))
                    {
                        apptIds.Add(apptId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "GetApptIdsForUser fell back to lead-only: " + ex.Message);
                apptIds.Clear();
            }

            return apptIds;
        }

        /// <summary>
        /// The pre-crew-table answer: the single resource on tbl_Appointment, reported as the
        /// primary. Returns null when the appointment has no usable resource - ResourceID is
        /// loosely typed and legitimately holds '', '0' and junk.
        /// </summary>
        private AssignedResource GetLegacyResource(string companyID, int apptId)
        {
            // TOP 1 with an explicit ORDER BY, because (CompanyID, ApptID) can match several
            // tbl_Appointment rows (see the DISTINCT note in GetAssignedResourcesForRange).
            // Ordering by resource id picks the SAME row the backfill in
            // SQL\tbl_AppointmentResource.sql picks, so an appointment shows the same lead
            // before and after that script is run.
            const string sql = @"
                SELECT TOP 1
                        r.[Id]     AS ResourceID,
                        r.[Name]   AS Name,
                        r.[email]  AS Email,
                        r.[Mobile] AS Mobile
                FROM    [msSchedulerV3].[dbo].[tbl_Appointment] a
                INNER JOIN [msSchedulerV3].[dbo].[tbl_Resources] r
                        ON  r.[Id]        = TRY_CAST(a.[ResourceID] AS INT)
                        AND r.[CompanyID] = a.[CompanyID]
                WHERE   a.[CompanyID] = @CompanyID
                  AND   a.[ApptID]    = @ApptID
                ORDER BY r.[Id];";

            DataTable dt = new DataTable();
            Database db = new Database(connStr);
            db.Command = new SqlCommand();
            db.Command.CommandText = sql;
            db.Command.Parameters.Clear();
            db.Command.Parameters.AddWithValue("@CompanyID", companyID);
            db.Command.Parameters.AddWithValue("@ApptID", apptId);
            db.Execute(out dt);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new AssignedResource
            {
                ResourceID = Convert.ToInt32(row["ResourceID"]),
                Name = row["Name"].ToString(),
                Email = row["Email"].ToString(),
                Mobile = row["Mobile"].ToString(),
                IsPrimary = true
            };
        }

        /// <summary>
        /// Whether this appointment has real crew rows, as opposed to the lead-only answer the
        /// reads synthesise from tbl_Appointment.ResourceID.
        ///
        /// Every other read here deliberately blurs that distinction - a caller rendering a
        /// crew wants the lead back either way. A caller about to WRITE cannot afford the blur:
        /// <see cref="GetAssignedResources"/> returns a one-member list in both cases, so
        /// "keep the existing crew and move the primary" would, on an appointment with no crew
        /// rows, save the OLD lead as a helper alongside the new one and silently invent a
        /// two-man crew on a legacy update.
        /// </summary>
        /// <returns>False when there are none, and when the table cannot be read - both mean
        /// "there is no crew here to preserve".</returns>
        public bool HasCrewRows(string companyID, string apptID)
        {
            int apptId;
            if (string.IsNullOrWhiteSpace(companyID) || !int.TryParse(apptID, out apptId))
            {
                return false;
            }

            const string sql = @"
                SELECT TOP 1 1
                FROM   [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                WHERE  [CompanyID] = @CompanyID
                  AND  [ApptID]    = @ApptID
                  AND  [IsActive]  = 1;";

            try
            {
                DataTable dt = new DataTable();
                Database db = new Database(connStr);
                db.Command = new SqlCommand();
                db.Command.CommandText = sql;
                db.Command.Parameters.Clear();
                db.Command.Parameters.AddWithValue("@CompanyID", companyID);
                db.Command.Parameters.AddWithValue("@ApptID", apptId);
                db.Execute(out dt);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "HasCrewRows could not read the crew table: " + ex.Message);
                return false;
            }
        }

        #endregion Read

        #region Write

        /// <summary>
        /// Replaces the crew of an appointment.
        ///
        /// Deactivates every existing row for the appointment and then upserts the supplied
        /// ones, so a row per (appointment, resource) is reused forever instead of churning
        /// identities - the unique index makes that upsert a seek. The whole thing runs in one
        /// transaction: a crew is never half-written.
        ///
        /// The caller is responsible for writing <paramref name="primaryResourceId"/> into
        /// tbl_Appointment.ResourceID (Update_Appointment / Add_Appointment already do, from
        /// the same value) - this method only owns the crew table.
        /// </summary>
        /// <param name="resourceIds">
        /// Every assigned resource INCLUDING the primary. Duplicates and non-positive ids are
        /// ignored. The primary is added if the caller left it out.
        /// </param>
        public void SaveAssignments(string companyID, string apptID, string primaryResourceId,
                                    IEnumerable<string> resourceIds, string createdBy)
        {
            int apptId;
            if (string.IsNullOrWhiteSpace(companyID) || !int.TryParse(apptID, out apptId))
            {
                return;
            }

            int primaryId;
            if (!int.TryParse(primaryResourceId, out primaryId) || primaryId <= 0)
            {
                primaryId = 0;
            }

            // Normalise: positive ints only, no duplicates, primary guaranteed present.
            List<int> ids = new List<int>();
            if (resourceIds != null)
            {
                foreach (string raw in resourceIds)
                {
                    int id;
                    if (int.TryParse(raw, out id) && id > 0 && !ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }
            if (primaryId > 0 && !ids.Contains(primaryId))
            {
                ids.Add(primaryId);
            }

            // Nobody selected the lead explicitly (or it was cleared): the first pick leads, so
            // there is always exactly one IsPrimary row for a non-empty crew.
            if (primaryId <= 0 && ids.Count > 0)
            {
                primaryId = ids[0];
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        const string deactivateSql = @"
                            UPDATE [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                            SET    [IsActive] = 0, [IsPrimary] = 0
                            WHERE  [CompanyID] = @CompanyID AND [ApptID] = @ApptID;";

                        using (SqlCommand cmd = new SqlCommand(deactivateSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@CompanyID", companyID);
                            cmd.Parameters.AddWithValue("@ApptID", apptId);
                            cmd.ExecuteNonQuery();
                        }

                        // Reuse the existing row when this pairing has been assigned before,
                        // otherwise insert. The unique index on
                        // (CompanyID, ApptID, ResourceID) keeps both branches a seek.
                        const string upsertSql = @"
                            UPDATE [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                            SET    [IsActive] = 1, [IsPrimary] = @IsPrimary
                            WHERE  [CompanyID] = @CompanyID
                              AND  [ApptID] = @ApptID
                              AND  [ResourceID] = @ResourceID;

                            IF @@ROWCOUNT = 0
                            INSERT INTO [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                                   ([CompanyID], [ApptID], [ResourceID], [IsPrimary], [IsActive], [CreatedBy])
                            VALUES (@CompanyID, @ApptID, @ResourceID, @IsPrimary, 1, @CreatedBy);";

                        foreach (int id in ids)
                        {
                            using (SqlCommand cmd = new SqlCommand(upsertSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                                cmd.Parameters.AddWithValue("@ApptID", apptId);
                                cmd.Parameters.AddWithValue("@ResourceID", id);
                                cmd.Parameters.AddWithValue("@IsPrimary", id == primaryId);
                                cmd.Parameters.AddWithValue("@CreatedBy",
                                    (object)createdBy ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        try { tran.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Swaps one member of a crew for another, leaving the rest of the crew alone. This is
        /// what dragging a single lane copy on the dispatch board means.
        ///
        /// Returns false without changing anything when the appointment is already assigned to
        /// <paramref name="newResourceId"/> - dropping a job onto someone already on it would
        /// otherwise silently shrink the crew.
        ///
        /// Does NOT touch tbl_Appointment.ResourceID; the caller updates that only when the
        /// resource being replaced was the primary (<see cref="IsPrimaryResource"/>).
        /// </summary>
        public bool ReplaceResource(string companyID, string apptID,
                                    string oldResourceId, string newResourceId)
        {
            int apptId, oldId, newId;
            if (string.IsNullOrWhiteSpace(companyID)
                || !int.TryParse(apptID, out apptId)
                || !int.TryParse(oldResourceId, out oldId)
                || !int.TryParse(newResourceId, out newId)
                || newId <= 0)
            {
                return false;
            }

            if (oldId == newId) return true;

            List<int> current = GetAssignedResourceIds(companyID, apptID);
            if (current.Contains(newId)) return false;

            bool wasPrimary = IsPrimaryResource(companyID, apptID, oldId);

            // Same upsert shape as SaveAssignments: the pairing may have existed and been
            // deactivated by an earlier reassignment, in which case the row is reused.
            const string sql = @"
                UPDATE [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                SET    [IsActive] = 0, [IsPrimary] = 0
                WHERE  [CompanyID] = @CompanyID AND [ApptID] = @ApptID
                  AND  [ResourceID] = @OldResourceID;

                UPDATE [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                SET    [IsActive] = 1, [IsPrimary] = @IsPrimary
                WHERE  [CompanyID] = @CompanyID AND [ApptID] = @ApptID
                  AND  [ResourceID] = @NewResourceID;

                IF @@ROWCOUNT = 0
                INSERT INTO [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                       ([CompanyID], [ApptID], [ResourceID], [IsPrimary], [IsActive], [CreatedBy])
                VALUES (@CompanyID, @ApptID, @NewResourceID, @IsPrimary, 1, 'dispatch');";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                cmd.Parameters.AddWithValue("@ApptID", apptId);
                cmd.Parameters.AddWithValue("@OldResourceID", oldId);
                cmd.Parameters.AddWithValue("@NewResourceID", newId);
                cmd.Parameters.AddWithValue("@IsPrimary", wasPrimary);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        /// <summary>
        /// Is this resource the lead on the appointment? Used by the dispatch drop handler to
        /// decide whether tbl_Appointment.ResourceID has to move too.
        ///
        /// Pre-backfill appointments have no crew rows, so the answer falls back to "is it the
        /// one on tbl_Appointment", which is the same question.
        /// </summary>
        public bool IsPrimaryResource(string companyID, string apptID, int resourceId)
        {
            AssignedResource primary = GetAssignedResources(companyID, apptID)
                                       .FirstOrDefault(a => a.IsPrimary);

            return primary != null && primary.ResourceID == resourceId;
        }

        /// <summary>
        /// Drops the whole crew of an appointment. For appointment deletion; the crew table has
        /// no FK to tbl_Appointment (ApptID is per company, so a single-column FK is impossible)
        /// and would otherwise leave orphans behind.
        /// </summary>
        public void DeleteAssignments(string companyID, string apptID)
        {
            int apptId;
            if (string.IsNullOrWhiteSpace(companyID) || !int.TryParse(apptID, out apptId))
            {
                return;
            }

            const string sql = @"
                DELETE FROM [msSchedulerV3].[dbo].[tbl_AppointmentResource]
                WHERE  [CompanyID] = @CompanyID AND [ApptID] = @ApptID;";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CompanyID", companyID);
                cmd.Parameters.AddWithValue("@ApptID", apptId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        #endregion Write
    }
}
