# Fix Missing Stored Procedure

## Issue
The application is throwing an error: 
```
Could not find stored procedure 'sp_Appointments_UpdateAttachedForms'
```

## Solution
Execute the SQL script to create the missing stored procedure.

### Quick Fix (Recommended)
1. Open SQL Server Management Studio (SSMS)
2. Connect to your database server: `3.148.0.246`
3. Open the file: `Database/FixMissingStoredProcedure.sql`
4. Make sure you're connected to the `myServiceJobs` database
5. Execute the script (F5)

### Alternative - Complete Setup
If you want to run the complete forms setup:
1. Execute: `Database/Forms_Schema.sql` (contains all form-related procedures)

## What the Script Does
1. Creates `sp_Appointments_UpdateAttachedForms` stored procedure
2. Creates `sp_Appointments_GetDetails` stored procedure (if missing)
3. Handles form instance creation and management for appointments

## After Running the Script
The appointment forms functionality should work:
- ✅ Update Forms button will work
- ✅ Email and SMS buttons will work
- ✅ Form instances will be properly created and managed

## Database Tables Used
- `FormInstances` - Stores form data for appointments
- `FormTemplates` - Stores form template definitions
- `Appointments` - Main appointments table
- `Customers` - Customer information for email/SMS

## Troubleshooting
If you still get errors after running the script:
1. Verify the script executed successfully (check for error messages)
2. Verify you're connected to the correct database (`myServiceJobs`)
3. Check that the `FormInstances` and `FormTemplates` tables exist
4. If tables are missing, run the complete `Forms_Schema.sql` script first