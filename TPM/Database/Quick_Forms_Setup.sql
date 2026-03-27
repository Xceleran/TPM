-- QUICK SETUP: Execute this single script to create everything needed
-- This combines the essential parts of all setup scripts

USE [myServiceJobs]
GO

PRINT '=== QUICK FORMS SETUP START ==='
PRINT 'Database: myServiceJobs'
PRINT 'Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT ''

-- Create FormTemplates table
PRINT 'Creating FormTemplates table...'
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FormTemplates' AND xtype='U')
BEGIN
    CREATE TABLE FormTemplates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyID VARCHAR(100) NOT NULL,
        TemplateName VARCHAR(200) NOT NULL,
        Description VARCHAR(500),
        Category VARCHAR(100),
        IsAutoAssignEnabled BIT DEFAULT 0,
        AutoAssignServiceTypes NVARCHAR(MAX), -- JSON array
        FormStructure NVARCHAR(MAX), -- JSON structure
        RequireSignature BIT DEFAULT 0,
        RequireTip BIT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        CreatedBy VARCHAR(100),
        CreatedDateTime DATETIME DEFAULT GETDATE(),
        UpdatedBy VARCHAR(100),
        UpdatedDateTime DATETIME,
        INDEX IX_FormTemplates_CompanyID (CompanyID),
        INDEX IX_FormTemplates_Active (CompanyID, IsActive)
    )
    PRINT '✅ FormTemplates table created'
END
ELSE
    PRINT '✅ FormTemplates table already exists'

-- Create FormInstances table
PRINT 'Creating FormInstances table...'
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FormInstances' AND xtype='U')
BEGIN
    CREATE TABLE FormInstances (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyID VARCHAR(100) NOT NULL,
        TemplateId INT NOT NULL,
        AppointmentId VARCHAR(100) NOT NULL,
        CustomerID VARCHAR(100),
        FormData NVARCHAR(MAX), -- JSON form data
        Status VARCHAR(50) DEFAULT 'Pending', -- Pending, InProgress, Completed, Submitted
        TechnicianSignature VARBINARY(MAX),
        CustomerSignature VARBINARY(MAX),
        TipAmount DECIMAL(10,2),
        TipNotes VARCHAR(500),
        FilledBy VARCHAR(100),
        StartedDateTime DATETIME,
        CompletedDateTime DATETIME,
        SubmittedDateTime DATETIME,
        IsSynced BIT DEFAULT 0,
        SyncedDateTime DATETIME,
        StoredFilePath VARCHAR(500),
        StoredFileName VARCHAR(200),
        CreatedDateTime DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (TemplateId) REFERENCES FormTemplates(Id),
        INDEX IX_FormInstances_Appointment (CompanyID, AppointmentId),
        INDEX IX_FormInstances_Status (CompanyID, Status),
        INDEX IX_FormInstances_Template (TemplateId)
    )
    PRINT '✅ FormInstances table created'
END
ELSE
    PRINT '✅ FormInstances table already exists'

-- Create FormInstanceFields table
PRINT 'Creating FormInstanceFields table...'
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FormInstanceFields' AND xtype='U')
BEGIN
    CREATE TABLE FormInstanceFields (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FormInstanceId INT NOT NULL,
        FieldName VARCHAR(100) NOT NULL,
        FieldValue NVARCHAR(MAX),
        FieldType VARCHAR(50), -- text, number, date, dropdown, checkbox, radio, textarea, signature
        FieldLabel VARCHAR(200),
        CreatedDateTime DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (FormInstanceId) REFERENCES FormInstances(Id) ON DELETE CASCADE,
        INDEX IX_FormInstanceFields_Instance (FormInstanceId),
        INDEX IX_FormInstanceFields_Field (FormInstanceId, FieldName)
    )
    PRINT '✅ FormInstanceFields table created'
END
ELSE
    PRINT '✅ FormInstanceFields table already exists'

-- Create FormUsageLog table
PRINT 'Creating FormUsageLog table...'
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FormUsageLog' AND xtype='U')
BEGIN
    CREATE TABLE FormUsageLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyID VARCHAR(100) NOT NULL,
        FormInstanceId INT,
        TemplateId INT NOT NULL,
        AppointmentId VARCHAR(100),
        Action VARCHAR(50) NOT NULL, -- Created, Started, Completed, Submitted, Synced
        PerformedBy VARCHAR(100),
        ActionDateTime DATETIME DEFAULT GETDATE(),
        Notes VARCHAR(500),
        DeviceInfo VARCHAR(500),
        INDEX IX_FormUsageLog_Company (CompanyID, ActionDateTime),
        INDEX IX_FormUsageLog_Template (TemplateId, ActionDateTime),
        INDEX IX_FormUsageLog_Appointment (AppointmentId, ActionDateTime)
    )
    PRINT '✅ FormUsageLog table created'
END
ELSE
    PRINT '✅ FormUsageLog table already exists'

-- Create AppointmentFormMapping table
PRINT 'Creating AppointmentFormMapping table...'
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentFormMapping' AND xtype='U')
BEGIN
    CREATE TABLE AppointmentFormMapping (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyID VARCHAR(100) NOT NULL,
        TemplateId INT NOT NULL,
        ServiceType VARCHAR(200) NOT NULL,
        IsAutoAssign BIT DEFAULT 1,
        Priority INT DEFAULT 1,
        IsActive BIT DEFAULT 1,
        CreatedBy VARCHAR(100),
        CreatedDateTime DATETIME DEFAULT GETDATE(),
        FOREIGN KEY (TemplateId) REFERENCES FormTemplates(Id) ON DELETE CASCADE,
        INDEX IX_AppointmentFormMapping_Service (CompanyID, ServiceType, IsActive),
        INDEX IX_AppointmentFormMapping_Template (TemplateId, IsActive)
    )
    PRINT '✅ AppointmentFormMapping table created'
END
ELSE
    PRINT '✅ AppointmentFormMapping table already exists'

PRINT ''
PRINT 'Creating essential stored procedures...'

-- sp_Forms_GetServiceTypes
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
PRINT '✅ sp_Forms_GetServiceTypes created'

-- sp_Forms_GetAllTemplates
CREATE OR ALTER PROCEDURE sp_Forms_GetAllTemplates
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT Id, CompanyID, TemplateName, Description, Category,
           IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure,
           RequireSignature, RequireTip, IsActive,
           CreatedBy, CreatedDateTime, UpdatedBy, UpdatedDateTime
    FROM FormTemplates
    WHERE CompanyID = @CompanyID
    ORDER BY TemplateName
END
PRINT '✅ sp_Forms_GetAllTemplates created'

-- sp_Forms_GetUsageLog
CREATE OR ALTER PROCEDURE sp_Forms_GetUsageLog
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT TOP 50
        ul.Id, ul.CompanyID, ul.FormInstanceId, ul.TemplateId, ul.AppointmentId,
        ul.Action, ul.PerformedBy, ul.ActionDateTime, ul.Notes,
        ft.TemplateName
    FROM FormUsageLog ul
    LEFT JOIN FormTemplates ft ON ul.TemplateId = ft.Id
    WHERE ul.CompanyID = @CompanyID
    ORDER BY ul.ActionDateTime DESC
END
PRINT '✅ sp_Forms_GetUsageLog created'

-- sp_Forms_GetTemplate
CREATE OR ALTER PROCEDURE sp_Forms_GetTemplate
    @Id INT,
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT Id, CompanyID, TemplateName, Description, Category,
           IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure,
           RequireSignature, RequireTip, IsActive,
           CreatedBy, CreatedDateTime, UpdatedBy, UpdatedDateTime
    FROM FormTemplates
    WHERE Id = @Id AND CompanyID = @CompanyID
END
PRINT '✅ sp_Forms_GetTemplate created'

-- sp_Forms_SaveTemplate
CREATE OR ALTER PROCEDURE sp_Forms_SaveTemplate
    @Id INT,
    @CompanyID VARCHAR(100),
    @TemplateName VARCHAR(200),
    @Description VARCHAR(500),
    @Category VARCHAR(100),
    @IsAutoAssignEnabled BIT,
    @AutoAssignServiceTypes NVARCHAR(MAX),
    @FormStructure NVARCHAR(MAX),
    @RequireSignature BIT,
    @RequireTip BIT,
    @IsActive BIT,
    @CreatedBy VARCHAR(100),
    @UpdatedBy VARCHAR(100),
    @NewId INT OUTPUT
AS
BEGIN
    IF @Id = 0 OR @Id IS NULL
    BEGIN
        -- Insert new template
        INSERT INTO FormTemplates (
            CompanyID, TemplateName, Description, Category,
            IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure,
            RequireSignature, RequireTip, IsActive, CreatedBy, CreatedDateTime
        ) VALUES (
            @CompanyID, @TemplateName, @Description, @Category,
            @IsAutoAssignEnabled, @AutoAssignServiceTypes, @FormStructure,
            @RequireSignature, @RequireTip, @IsActive, @CreatedBy, GETDATE()
        )
        
        SET @NewId = SCOPE_IDENTITY()
    END
    ELSE
    BEGIN
        -- Update existing template
        UPDATE FormTemplates SET
            TemplateName = @TemplateName,
            Description = @Description,
            Category = @Category,
            IsAutoAssignEnabled = @IsAutoAssignEnabled,
            AutoAssignServiceTypes = @AutoAssignServiceTypes,
            FormStructure = @FormStructure,
            RequireSignature = @RequireSignature,
            RequireTip = @RequireTip,
            IsActive = @IsActive,
            UpdatedBy = @UpdatedBy,
            UpdatedDateTime = GETDATE()
        WHERE Id = @Id AND CompanyID = @CompanyID
        
        SET @NewId = @Id
    END
END
PRINT '✅ sp_Forms_SaveTemplate created'

PRINT ''
PRINT '=== QUICK SETUP COMPLETE! ==='
PRINT ''
PRINT 'You can now:'
PRINT '1. Test the Forms page - it should load without errors'
PRINT '2. Click "New Template" to create form templates'
PRINT '3. Set up auto-assignment rules'
PRINT ''
PRINT 'Optional: Execute Database/Forms_SampleData.sql for test data'