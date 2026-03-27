-- Test your ConnStrJobs connection string
-- Execute this to verify your connection is working

USE [myServiceJobs]
GO

PRINT '=== CONNECTION TEST ==='
PRINT 'Testing connection to myServiceJobs database'
PRINT 'Server: 3.148.0.246'
PRINT 'User: Mobilizedba'
PRINT 'Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT ''

-- Test 1: Basic connection
SELECT 
    'Connection Successful!' as Status,
    DB_NAME() as DatabaseName,
    @@SERVERNAME as ServerName,
    SYSTEM_USER as LoginUser,
    HOST_NAME() as ClientMachine

-- Test 2: Check if we can read from existing tables
PRINT ''
PRINT 'Checking existing tables...'

-- Check for Appointments table (needed for service types)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Appointments')
BEGIN
    PRINT '✅ Appointments table found'
    
    -- Count appointments
    DECLARE @AppointmentCount INT
    SELECT @AppointmentCount = COUNT(*) FROM Appointments
    PRINT 'Total appointments: ' + CAST(@AppointmentCount as VARCHAR)
    
    -- Check service types
    DECLARE @ServiceTypeCount INT
    SELECT @ServiceTypeCount = COUNT(DISTINCT ServiceType) 
    FROM Appointments 
    WHERE ServiceType IS NOT NULL AND ServiceType != ''
    
    IF @ServiceTypeCount > 0
        PRINT 'Service types available: ' + CAST(@ServiceTypeCount as VARCHAR)
    ELSE
        PRINT 'No service types found yet'
END
ELSE
BEGIN
    PRINT '❌ Appointments table not found'
    PRINT 'Forms will work but service types may be empty'
END

-- Test 3: Check current user permissions
PRINT ''
PRINT 'Checking permissions...'

-- Test CREATE TABLE permission
BEGIN TRY
    CREATE TABLE #TestTable (id INT)
    DROP TABLE #TestTable
    PRINT '✅ CREATE TABLE permission - OK'
END TRY
BEGIN CATCH
    PRINT '❌ CREATE TABLE permission - DENIED'
    PRINT 'Error: ' + ERROR_MESSAGE()
END CATCH

-- Test CREATE PROCEDURE permission  
BEGIN TRY
    EXEC('CREATE PROCEDURE #TestProc AS SELECT 1')
    EXEC('DROP PROCEDURE #TestProc')
    PRINT '✅ CREATE PROCEDURE permission - OK'
END TRY
BEGIN CATCH
    PRINT '❌ CREATE PROCEDURE permission - DENIED'
    PRINT 'Error: ' + ERROR_MESSAGE()
END CATCH

PRINT ''
PRINT '=== CONNECTION TEST COMPLETE ==='
PRINT 'If you see this message, your connection string is working!'
PRINT ''
PRINT 'Next steps:'
PRINT '1. If permissions are OK, run Database/Quick_Forms_Setup.sql'
PRINT '2. Test your Forms page after setup'