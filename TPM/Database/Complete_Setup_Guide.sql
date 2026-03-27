-- COMPLETE FORMS MANAGEMENT SYSTEM SETUP
-- Database: myServiceJobs
-- Server: 3.148.0.246
-- User: Mobilizedba
--
-- EXECUTE THESE SCRIPTS IN ORDER:

PRINT '=== Forms Management System Setup for myServiceJobs ==='
PRINT '======================================================='
PRINT 'Connection: 3.148.0.246 -> myServiceJobs'
PRINT 'User: Mobilizedba'
PRINT ''

USE [myServiceJobs]
GO

-- Step 1: Verify connection
PRINT 'Step 1: Verifying database connection...'
SELECT 
    'Connected to: ' + DB_NAME() as DatabaseName,
    'Server: ' + @@SERVERNAME as ServerName,
    'Current User: ' + SYSTEM_USER as CurrentUser,
    GETDATE() as ConnectionTime

-- Step 2: Check if tables already exist
PRINT ''
PRINT 'Step 2: Checking existing tables...'
SELECT 
    'Existing Forms Tables' as CheckType,
    COUNT(*) as TableCount
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('FormTemplates', 'FormInstances', 'FormInstanceFields', 'AppointmentFormMapping', 'FormUsageLog')

-- Step 3: Ready to execute setup
PRINT ''
PRINT 'Step 3: Ready for setup!'
PRINT ''
PRINT 'NEXT STEPS:'
PRINT '1. Execute Database/Forms_Schema.sql (creates tables and stored procedures)'
PRINT '2. Execute Database/CreateStoredProcedures.sql (additional procedures)'
PRINT '3. Execute Database/Forms_SampleData.sql (sample data for testing)'
PRINT ''
PRINT 'OR use the individual fix scripts:'
PRINT '- Database/FixServiceTypes.sql (if service types error)'
PRINT '- Database/FixFormInstanceFields.sql (if FormInstanceFields error)'
PRINT ''
PRINT 'All scripts are now configured for your database: myServiceJobs'

-- Quick check for Appointments table (needed for service types)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Appointments')
BEGIN
    PRINT ''
    PRINT '✅ Appointments table found - service types integration will work'
    
    -- Show sample service types if any exist
    IF EXISTS (SELECT TOP 1 * FROM Appointments WHERE ServiceType IS NOT NULL AND ServiceType != '')
    BEGIN
        PRINT 'Sample Service Types found:'
        SELECT DISTINCT TOP 5 ServiceType 
        FROM Appointments 
        WHERE ServiceType IS NOT NULL AND ServiceType != ''
        ORDER BY ServiceType
    END
    ELSE
    BEGIN
        PRINT '⚠️  No service types found in Appointments table yet'
        PRINT '   Auto-assignment will work once you have appointments with service types'
    END
END
ELSE
BEGIN
    PRINT ''
    PRINT '⚠️  Appointments table not found'
    PRINT '   You may need to create appointments first, or update the service types query'
END

PRINT ''
PRINT '=== Setup verification complete ==='