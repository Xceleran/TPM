-- Quick fix to add missing service types stored procedure
-- Execute this if you need just this stored procedure

USE [myServiceJobs]
GO

PRINT 'Adding sp_Forms_GetServiceTypes stored procedure...'

-- Get unique service types for forms auto-assignment
CREATE OR ALTER PROCEDURE sp_Forms_GetServiceTypes
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT DISTINCT ServiceType
    FROM Appointments 
    WHERE CompanyID = @CompanyID 
      AND ServiceType IS NOT NULL 
      AND ServiceType != ''
    ORDER BY ServiceType
END
GO

PRINT 'sp_Forms_GetServiceTypes created successfully!'
PRINT 'You can now test the Forms page - GetServiceTypes should work.'

-- Test the procedure (optional)
-- EXEC sp_Forms_GetServiceTypes @CompanyID = 'YOUR_COMPANY_ID'