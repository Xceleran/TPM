-- Temporary fix for service types until we find the correct table
-- This creates a basic stored procedure that won't crash

USE [myServiceJobs]
GO

PRINT 'Creating temporary service types procedure...'

-- Replace the problematic stored procedure with a safe version
CREATE OR ALTER PROCEDURE sp_Forms_GetServiceTypes
    @CompanyID VARCHAR(100)
AS
BEGIN
    -- For now, return some default service types until we find the correct table
    -- This prevents the "Invalid object name" error
    
    -- Try to find the correct appointments table dynamically
    DECLARE @TableName NVARCHAR(255)
    DECLARE @SQL NVARCHAR(MAX)
    
    -- Look for a table that might contain appointments/services
    SET @TableName = NULL
    
    -- Check for common appointment table names
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Appointments')
        SET @TableName = 'Appointments'
    ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tbl_Appointments')
        SET @TableName = 'tbl_Appointments'
    ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Jobs')
        SET @TableName = 'Jobs'
    ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ServiceJobs')
        SET @TableName = 'ServiceJobs'
    ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkOrders')
        SET @TableName = 'WorkOrders'
    ELSE IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tbl_Jobs')
        SET @TableName = 'tbl_Jobs'
        
    IF @TableName IS NOT NULL
    BEGIN
        -- Check if the table has ServiceType column
        IF EXISTS (
            SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @TableName 
            AND COLUMN_NAME = 'ServiceType'
        )
        BEGIN
            SET @SQL = 'SELECT DISTINCT ServiceType FROM ' + @TableName + 
                      ' WHERE CompanyID = @CompanyID AND ServiceType IS NOT NULL AND ServiceType != '''' ORDER BY ServiceType'
            EXEC sp_executesql @SQL, N'@CompanyID VARCHAR(100)', @CompanyID
            RETURN
        END
    END
    
    -- If no suitable table found, return default service types
    SELECT 'HVAC Maintenance' as ServiceType
    UNION ALL SELECT 'Plumbing'
    UNION ALL SELECT 'Electrical'
    UNION ALL SELECT 'Installation'
    UNION ALL SELECT 'Repair'
    UNION ALL SELECT 'Inspection'
    ORDER BY ServiceType
END
GO

PRINT '✅ Temporary service types procedure created'
PRINT ''
PRINT 'This will prevent crashes, but you should:'
PRINT '1. Execute Database/Find_Appointments_Table.sql to find your real table'
PRINT '2. Let me know the correct table name'
PRINT '3. I will create the proper stored procedure'