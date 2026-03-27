-- Check what's currently in your myServiceJobs database
-- Execute this first to see what needs to be created

USE [myServiceJobs]
GO

PRINT '=== FORMS SYSTEM DATABASE STATUS CHECK ==='
PRINT '=========================================='
PRINT 'Database: myServiceJobs'
PRINT 'Server: 3.148.0.246'
PRINT 'Check Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT ''

-- Check 1: Forms Tables
PRINT '1. CHECKING FORMS TABLES:'
PRINT '-------------------------'
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='FormTemplates' AND xtype='U') THEN '✅ FormTemplates - EXISTS'
        ELSE '❌ FormTemplates - MISSING'
    END as TableStatus
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='FormInstances' AND xtype='U') THEN '✅ FormInstances - EXISTS'
        ELSE '❌ FormInstances - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='FormInstanceFields' AND xtype='U') THEN '✅ FormInstanceFields - EXISTS'
        ELSE '❌ FormInstanceFields - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentFormMapping' AND xtype='U') THEN '✅ AppointmentFormMapping - EXISTS'
        ELSE '❌ AppointmentFormMapping - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='FormUsageLog' AND xtype='U') THEN '✅ FormUsageLog - EXISTS'
        ELSE '❌ FormUsageLog - MISSING'
    END

PRINT ''

-- Check 2: Required Stored Procedures
PRINT '2. CHECKING STORED PROCEDURES:'
PRINT '------------------------------'
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_GetServiceTypes' AND xtype='P') THEN '✅ sp_Forms_GetServiceTypes - EXISTS'
        ELSE '❌ sp_Forms_GetServiceTypes - MISSING'
    END as ProcedureStatus
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_GetAllTemplates' AND xtype='P') THEN '✅ sp_Forms_GetAllTemplates - EXISTS'
        ELSE '❌ sp_Forms_GetAllTemplates - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_GetUsageLog' AND xtype='P') THEN '✅ sp_Forms_GetUsageLog - EXISTS'
        ELSE '❌ sp_Forms_GetUsageLog - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_GetTemplate' AND xtype='P') THEN '✅ sp_Forms_GetTemplate - EXISTS'
        ELSE '❌ sp_Forms_GetTemplate - MISSING'
    END
UNION ALL
SELECT 
    CASE 
        WHEN EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_SaveTemplate' AND xtype='P') THEN '✅ sp_Forms_SaveTemplate - EXISTS'
        ELSE '❌ sp_Forms_SaveTemplate - MISSING'
    END

PRINT ''

-- Check 3: Existing Appointments table (needed for service types)
PRINT '3. CHECKING APPOINTMENTS TABLE:'
PRINT '-------------------------------'
IF EXISTS (SELECT * FROM sysobjects WHERE name='Appointments' AND xtype='U')
BEGIN
    PRINT '✅ Appointments table found'
    
    -- Check for service types data
    DECLARE @ServiceTypeCount INT
    SELECT @ServiceTypeCount = COUNT(DISTINCT ServiceType) 
    FROM Appointments 
    WHERE ServiceType IS NOT NULL AND ServiceType != ''
    
    IF @ServiceTypeCount > 0
    BEGIN
        PRINT '✅ Service types found: ' + CAST(@ServiceTypeCount as VARCHAR) + ' unique types'
        PRINT 'Sample service types:'
        SELECT DISTINCT TOP 5 '  - ' + ServiceType as ServiceTypes
        FROM Appointments 
        WHERE ServiceType IS NOT NULL AND ServiceType != ''
        ORDER BY ServiceType
    END
    ELSE
    BEGIN
        PRINT '⚠️  No service types found (ServiceType column is empty)'
    END
END
ELSE
BEGIN
    PRINT '❌ Appointments table not found'
    PRINT '   Forms will work but service type auto-assignment may not work'
END

PRINT ''

-- Check 4: Summary and recommendations
PRINT '4. SETUP RECOMMENDATIONS:'
PRINT '-------------------------'

DECLARE @TablesExist BIT = 0
DECLARE @ProcsExist BIT = 0

IF EXISTS (SELECT * FROM sysobjects WHERE name='FormTemplates' AND xtype='U')
    SET @TablesExist = 1

IF EXISTS (SELECT * FROM sysobjects WHERE name='sp_Forms_GetAllTemplates' AND xtype='P')
    SET @ProcsExist = 1

IF @TablesExist = 0 AND @ProcsExist = 0
BEGIN
    PRINT '🔧 FULL SETUP NEEDED:'
    PRINT '   1. Execute: Database/Forms_Schema.sql'
    PRINT '   2. Execute: Database/CreateStoredProcedures.sql'
    PRINT '   3. Execute: Database/Forms_SampleData.sql (optional - for testing)'
END
ELSE IF @TablesExist = 1 AND @ProcsExist = 0
BEGIN
    PRINT '🔧 STORED PROCEDURES NEEDED:'
    PRINT '   Execute: Database/Forms_Schema.sql (contains missing procedures)'
END
ELSE IF @TablesExist = 0 AND @ProcsExist = 1
BEGIN
    PRINT '🔧 TABLES NEEDED:'
    PRINT '   Execute: Database/Forms_Schema.sql (contains table definitions)'
END
ELSE
BEGIN
    PRINT '✅ SETUP COMPLETE - Forms system should work!'
END

PRINT ''
PRINT '=== STATUS CHECK COMPLETE ==='