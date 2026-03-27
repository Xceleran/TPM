-- Find the correct appointments table in your myServiceJobs database
-- Execute this to discover your table structure

USE [myServiceJobs]
GO

PRINT '=== FINDING APPOINTMENTS TABLE ==='
PRINT 'Searching in database: myServiceJobs'
PRINT 'Server: 3.148.0.246'
PRINT ''

-- Step 1: Find all tables that might be appointments
PRINT '1. SEARCHING FOR APPOINTMENT-RELATED TABLES:'
PRINT '--------------------------------------------'
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME LIKE '%appointment%' 
   OR TABLE_NAME LIKE '%appt%'
   OR TABLE_NAME LIKE '%job%'
   OR TABLE_NAME LIKE '%service%'
   OR TABLE_NAME LIKE '%schedule%'
ORDER BY TABLE_NAME

PRINT ''

-- Step 2: Show all tables to help identify the right one
PRINT '2. ALL TABLES IN DATABASE:'
PRINT '--------------------------'
SELECT 
    TABLE_SCHEMA + '.' + TABLE_NAME as FullTableName,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME

PRINT ''

-- Step 3: Look for tables with ServiceType column
PRINT '3. TABLES WITH ServiceType COLUMN:'
PRINT '----------------------------------'
SELECT 
    TABLE_SCHEMA + '.' + TABLE_NAME as TableWithServiceType,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE COLUMN_NAME LIKE '%service%type%'
   OR COLUMN_NAME LIKE '%servicetype%'
   OR COLUMN_NAME = 'ServiceType'
ORDER BY TABLE_NAME

PRINT ''

-- Step 4: Look for tables with CompanyID column (common in this app)
PRINT '4. TABLES WITH CompanyID COLUMN:'
PRINT '--------------------------------'
SELECT DISTINCT
    TABLE_SCHEMA + '.' + TABLE_NAME as TableWithCompanyID
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE COLUMN_NAME = 'CompanyID'
ORDER BY TABLE_NAME

PRINT ''
PRINT '=== SEARCH COMPLETE ==='
PRINT ''
PRINT 'INSTRUCTIONS:'
PRINT '1. Look at the results above to find your appointments table'
PRINT '2. It might be named something like:'
PRINT '   - tbl_Appointments'
PRINT '   - Jobs'
PRINT '   - ServiceJobs' 
PRINT '   - WorkOrders'
PRINT '   - Appointments (in a different schema)'
PRINT '3. Once you find it, let me know the exact table name'
PRINT '4. I will update the stored procedure accordingly'