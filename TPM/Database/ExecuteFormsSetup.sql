-- Complete Forms Management System Setup Script
-- Execute this script to set up the entire forms system with sample data
-- 
-- Instructions:
-- 1. Update the database name on line 6
-- 2. Execute this entire script in SQL Server Management Studio
-- 3. Verify all tables and data were created successfully

USE [myServiceJobs]
GO

PRINT '=== Starting Forms Management System Setup ==='
PRINT '================================================'

-- Step 1: Create all tables and stored procedures from schema
PRINT 'Step 1/3: Creating database schema...'
EXEC('
-- Execute the Forms_Schema.sql content here
-- Copy and paste the entire content of Forms_Schema.sql here
-- OR execute Forms_Schema.sql separately first
')

-- Step 2: Create additional stored procedures
PRINT 'Step 2/3: Creating additional stored procedures...'
EXEC('
-- Execute the CreateStoredProcedures.sql content here  
-- Copy and paste the entire content of CreateStoredProcedures.sql here
-- OR execute CreateStoredProcedures.sql separately first
')

-- Step 3: Insert sample data
PRINT 'Step 3/3: Inserting sample data...'

-- Clear existing sample data (optional)
DELETE FROM FormUsageLog WHERE CompanyID = 'COMP001';
DELETE FROM FormInstanceFields WHERE FormInstanceId IN (SELECT Id FROM FormInstances WHERE CompanyID = 'COMP001');
DELETE FROM FormInstances WHERE CompanyID = 'COMP001';
DELETE FROM AppointmentFormMapping WHERE CompanyID = 'COMP001';
DELETE FROM FormTemplates WHERE CompanyID = 'COMP001';

-- Insert sample data (from Forms_SampleData.sql)
EXEC('
-- Execute the Forms_SampleData.sql content here
-- Copy and paste the entire content of Forms_SampleData.sql here
-- OR execute Forms_SampleData.sql separately after the schema
')

-- Verification queries
PRINT ''
PRINT '=== Verification ==='
PRINT 'Checking created objects...'

-- Check tables
SELECT 
    'Tables Created' as ObjectType,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('FormTemplates', 'FormInstances', 'FormInstanceFields', 'AppointmentFormMapping', 'FormUsageLog')

-- Check stored procedures
SELECT 
    'Stored Procedures Created' as ObjectType,
    COUNT(*) as Count
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_NAME LIKE 'sp_Forms_%' OR ROUTINE_NAME LIKE 'sp_AppointmentFiles_%' OR ROUTINE_NAME LIKE 'sp_Customers_GetDetails' OR ROUTINE_NAME LIKE 'sp_Appointments_GetById'

-- Check sample data
SELECT 'Sample Data Summary' as Section, '' as Details
UNION ALL
SELECT 'Form Templates', CAST(COUNT(*) as VARCHAR) + ' records' FROM FormTemplates WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Auto-Assignment Rules', CAST(COUNT(*) as VARCHAR) + ' records' FROM AppointmentFormMapping WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Form Instances', CAST(COUNT(*) as VARCHAR) + ' records' FROM FormInstances WHERE CompanyID = 'COMP001'
UNION ALL
SELECT 'Usage Log Entries', CAST(COUNT(*) as VARCHAR) + ' records' FROM FormUsageLog WHERE CompanyID = 'COMP001'

PRINT ''
PRINT '=== Setup Complete! ==='
PRINT 'The Forms Management System is now ready to use.'
PRINT ''
PRINT 'Next Steps:'
PRINT '1. Update your application connection string'
PRINT '2. Test the Forms page functionality'
PRINT '3. Configure auto-assignment rules as needed'
PRINT '4. Customize form templates for your business'