/*
  Diagnostic procedure for New Work Orders (AppoinementList.aspx)
  Mirrors filters in Sp_GetAppointmnetData.

  Usage:
    EXEC dbo.Sp_CheckNewWorkOrders_Data @CompanyId = '14590';
    EXEC dbo.Sp_CheckNewWorkOrders_Data @CompanyId = '14590', @Status = 'pending';
*/

USE [msSchedulerV3];
GO

IF OBJECT_ID(N'dbo.Sp_CheckNewWorkOrders_Data', N'P') IS NOT NULL
    DROP PROCEDURE dbo.Sp_CheckNewWorkOrders_Data;
GO

CREATE PROCEDURE dbo.Sp_CheckNewWorkOrders_Data
    @CompanyId          NVARCHAR(50),
    @From               DATETIME = NULL,
    @To                 DATETIME = NULL,
    @Status             NVARCHAR(50) = 'all'   -- all | accept | pending
AS
BEGIN
    SET NOCOUNT ON;

    IF @From IS NULL SET @From = DATEADD(DAY, -60, GETDATE());
    IF @To   IS NULL SET @To   = DATEADD(DAY,  60, GETDATE());
    SET @Status = LOWER(LTRIM(RTRIM(ISNULL(@Status, 'all'))));

    PRINT '=== New Work Orders Data Check ===';
    PRINT 'CompanyID : ' + @CompanyId;
    PRINT 'Date From : ' + CONVERT(VARCHAR(30), @From, 120);
    PRINT 'Date To   : ' + CONVERT(VARCHAR(30), @To, 120);
    PRINT 'Status    : ' + @Status;
    PRINT '';

    /* Result set 1: Summary counts at each filter step */
    ;WITH Base AS (
        SELECT a.*
        FROM dbo.tbl_Appointment a
        WHERE a.CompanyID = @CompanyId
    ),
    StepCreatedBy AS (
        SELECT * FROM Base WHERE CreatedBy = 'TPM'
    ),
    StepDate AS (
        SELECT * FROM StepCreatedBy
        WHERE CreatedDateTime BETWEEN @From AND @To
    ),
    StepCustomer AS (
        SELECT a.*
        FROM StepDate a
        INNER JOIN dbo.tbl_Customer c
            ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
        WHERE TRY_CAST(c.WarrentyCompanyID AS BIGINT) > 0
    ),
    StepSite AS (
        SELECT a.*
        FROM StepCustomer a
        INNER JOIN dbo.tbl_CustomerSite cs
            ON cs.CompanyID = a.CompanyID
           AND cs.CustomerID = a.CustomerID
           AND cs.Id = a.SiteID
    ),
    StepStatus AS (
        SELECT *
        FROM StepSite
        WHERE (@Status = 'all')
           OR (@Status = 'accept'  AND IsApproved = 1)
           OR (@Status = 'pending' AND IsApproved = 0)
    )
    SELECT
        '1. All appointments for company' AS CheckStep,
        (SELECT COUNT(*) FROM Base) AS RowCount,
        'Any row in tbl_Appointment for this CompanyID' AS Notes
    UNION ALL
    SELECT
        '2. CreatedBy = TPM',
        (SELECT COUNT(*) FROM StepCreatedBy),
        'Jobs must be inserted with CreatedBy = ''TPM'''
    UNION ALL
    SELECT
        '3. CreatedDateTime in range',
        (SELECT COUNT(*) FROM StepDate),
        'Default page window is about -60 to +60 days'
    UNION ALL
    SELECT
        '4. Customer WarrentyCompanyID > 0',
        (SELECT COUNT(*) FROM StepCustomer),
        'Customer must be linked to a warranty provider (Assign on Supported TP Providers)'
    UNION ALL
    SELECT
        '5. Site join (Appointment.SiteID = CustomerSite.Id)',
        (SELECT COUNT(*) FROM StepSite),
        'Appointment SiteID must match tbl_CustomerSite.Id'
    UNION ALL
    SELECT
        '6. Final (same as Sp_GetAppointmnetData)',
        (SELECT COUNT(*) FROM StepStatus),
        'Rows that should appear on New Work Orders page';

    /* Result set 2: CreatedBy breakdown */
    SELECT
        ISNULL(NULLIF(LTRIM(RTRIM(CreatedBy)), ''), '(blank)') AS CreatedBy,
        COUNT(*) AS AppointmentCount,
        MIN(CreatedDateTime) AS OldestCreated,
        MAX(CreatedDateTime) AS NewestCreated
    FROM dbo.tbl_Appointment
    WHERE CompanyID = @CompanyId
    GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(CreatedBy)), ''), '(blank)')
    ORDER BY AppointmentCount DESC;

    /* Result set 3: TPM rows blocked and why */
    SELECT TOP 50
        a.ApptID,
        a.AppoinmentUId,
        a.CreatedDateTime,
        a.ApptDateTime,
        a.IsApproved,
        CASE WHEN a.IsApproved = 1 THEN 'Accept' ELSE 'Pending' END AS DisplayStatus,
        a.SiteID AS ApptSiteId,
        a.WarrentyCompanyID AS ApptWarrantyId,
        c.CustomerID,
        c.FirstName + ' ' + ISNULL(c.LastName, '') AS CustomerName,
        TRY_CAST(c.WarrentyCompanyID AS BIGINT) AS CustomerWarrantyId,
        cs.Id AS MatchedSiteId,
        cs.SiteName,
        CASE
            WHEN a.CreatedBy <> 'TPM' THEN 'CreatedBy is not TPM'
            WHEN a.CreatedDateTime < @From OR a.CreatedDateTime > @To THEN 'Outside date range'
            WHEN c.CustomerID IS NULL THEN 'No matching customer'
            WHEN TRY_CAST(c.WarrentyCompanyID AS BIGINT) IS NULL OR TRY_CAST(c.WarrentyCompanyID AS BIGINT) <= 0
                THEN 'Customer WarrentyCompanyID is 0 or NULL'
            WHEN cs.Id IS NULL THEN 'SiteID does not match tbl_CustomerSite.Id'
            WHEN @Status = 'accept'  AND a.IsApproved = 0 THEN 'Filtered out: status=accept but IsApproved=0'
            WHEN @Status = 'pending' AND a.IsApproved = 1 THEN 'Filtered out: status=pending but IsApproved=1'
            ELSE 'OK - should appear on page'
        END AS BlockReason
    FROM dbo.tbl_Appointment a
    LEFT JOIN dbo.tbl_Customer c
        ON c.CompanyID = a.CompanyID AND c.CustomerID = a.CustomerID
    LEFT JOIN dbo.tbl_CustomerSite cs
        ON cs.CompanyID = a.CompanyID
       AND cs.CustomerID = a.CustomerID
       AND cs.Id = a.SiteID
    WHERE a.CompanyID = @CompanyId
      AND (
            a.CreatedBy = 'TPM'
         OR a.CreatedDateTime >= DATEADD(DAY, -365, GETDATE())
      )
    ORDER BY
        CASE WHEN
            a.CreatedBy = 'TPM'
            AND a.CreatedDateTime BETWEEN @From AND @To
            AND TRY_CAST(c.WarrentyCompanyID AS BIGINT) > 0
            AND cs.Id IS NOT NULL
            AND (
                @Status = 'all'
                OR (@Status = 'accept'  AND a.IsApproved = 1)
                OR (@Status = 'pending' AND a.IsApproved = 0)
            )
        THEN 1 ELSE 0 END,
        a.CreatedDateTime DESC;

    /* Result set 4: Rows that WILL show (exact SP output preview) */
    IF @Status = 'accept'
    BEGIN
        SELECT TOP 50
            CONVERT(VARCHAR(10), a.ApptDateTime, 101) AS ApptDateTime,
            a.AppoinmentUId,
            a.ApptID,
            c.FirstName + ' ' + ISNULL(c.LastName, '') AS CustomerName,
            cs.SiteName,
            a.IsApproved,
            a.WarrentyCompanyID,
            a.CreatedDateTime
        FROM dbo.tbl_Customer c
        INNER JOIN dbo.tbl_CustomerSite cs
            ON c.CompanyID = cs.CompanyID AND c.CustomerID = cs.CustomerID
        INNER JOIN dbo.tbl_Appointment a
            ON cs.CustomerID = a.CustomerID AND cs.Id = a.SiteID AND c.CompanyID = a.CompanyID
        WHERE a.CreatedBy = 'TPM'
          AND TRY_CAST(c.WarrentyCompanyID AS BIGINT) > 0
          AND a.CreatedDateTime BETWEEN @From AND @To
          AND a.CompanyID = @CompanyId
          AND a.IsApproved = 1
        ORDER BY a.ApptDateTime DESC;
    END
    ELSE IF @Status = 'pending'
    BEGIN
        SELECT TOP 50
            CONVERT(VARCHAR(10), a.ApptDateTime, 101) AS ApptDateTime,
            a.AppoinmentUId,
            a.ApptID,
            c.FirstName + ' ' + ISNULL(c.LastName, '') AS CustomerName,
            cs.SiteName,
            a.IsApproved,
            a.WarrentyCompanyID,
            a.CreatedDateTime
        FROM dbo.tbl_Customer c
        INNER JOIN dbo.tbl_CustomerSite cs
            ON c.CompanyID = cs.CompanyID AND c.CustomerID = cs.CustomerID
        INNER JOIN dbo.tbl_Appointment a
            ON cs.CustomerID = a.CustomerID AND cs.Id = a.SiteID AND c.CompanyID = a.CompanyID
        WHERE a.CreatedBy = 'TPM'
          AND TRY_CAST(c.WarrentyCompanyID AS BIGINT) > 0
          AND a.CreatedDateTime BETWEEN @From AND @To
          AND a.CompanyID = @CompanyId
          AND a.IsApproved = 0
        ORDER BY a.ApptDateTime DESC;
    END
    ELSE
    BEGIN
        SELECT TOP 50
            CONVERT(VARCHAR(10), a.ApptDateTime, 101) AS ApptDateTime,
            a.AppoinmentUId,
            a.ApptID,
            c.FirstName + ' ' + ISNULL(c.LastName, '') AS CustomerName,
            cs.SiteName,
            a.IsApproved,
            a.WarrentyCompanyID,
            a.CreatedDateTime
        FROM dbo.tbl_Customer c
        INNER JOIN dbo.tbl_CustomerSite cs
            ON c.CompanyID = cs.CompanyID AND c.CustomerID = cs.CustomerID
        INNER JOIN dbo.tbl_Appointment a
            ON cs.CustomerID = a.CustomerID AND cs.Id = a.SiteID AND c.CompanyID = a.CompanyID
        WHERE a.CreatedBy = 'TPM'
          AND TRY_CAST(c.WarrentyCompanyID AS BIGINT) > 0
          AND a.CreatedDateTime BETWEEN @From AND @To
          AND a.CompanyID = @CompanyId
        ORDER BY a.ApptDateTime DESC;
    END
END
GO

PRINT 'Created dbo.Sp_CheckNewWorkOrders_Data';
PRINT '';
PRINT 'Run:';
PRINT '  EXEC dbo.Sp_CheckNewWorkOrders_Data @CompanyId = ''14590'';';
PRINT '  EXEC dbo.Sp_CheckNewWorkOrders_Data @CompanyId = ''14590'', @Status = ''pending'';';
