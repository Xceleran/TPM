-- Forms Management System Database Schema
-- Execute these scripts in order to set up the forms system
-- Database: myServiceJobs (Server: 3.148.0.246)

USE [myServiceJobs]
GO

-- 1. Form Templates Table
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
END

-- 2. Form Instances Table
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
END

-- 3. Form Fields Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FormFields' AND xtype='U')
BEGIN
    CREATE TABLE FormFields (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TemplateId INT NOT NULL,
        FieldName VARCHAR(100) NOT NULL,
        FieldLabel VARCHAR(200) NOT NULL,
        FieldType VARCHAR(50) NOT NULL, -- text, number, date, dropdown, checkbox, radio, textarea, signature
        IsRequired BIT DEFAULT 0,
        DefaultValue VARCHAR(500),
        ValidationRules NVARCHAR(1000), -- JSON validation rules
        Options NVARCHAR(MAX), -- JSON options for dropdown/radio
        DisplayOrder INT DEFAULT 0,
        IsAutoFillEnabled BIT DEFAULT 0,
        AutoFillSource VARCHAR(200), -- customer.FirstName, appointment.ServiceType, etc.
        FOREIGN KEY (TemplateId) REFERENCES FormTemplates(Id) ON DELETE CASCADE,
        INDEX IX_FormFields_Template (TemplateId, DisplayOrder)
    )
END

-- 4. Form Instance Fields Table (stores individual field values for completed forms)
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
END

-- 5. Form Usage Log Table
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
END

-- 6. Appointment Form Mapping Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentFormMapping' AND xtype='U')
BEGIN
    CREATE TABLE AppointmentFormMapping (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyID VARCHAR(100) NOT NULL,
        ServiceType VARCHAR(200) NOT NULL,
        TemplateId INT NOT NULL,
        IsAutoAssign BIT DEFAULT 1,
        Priority INT DEFAULT 1, -- For multiple forms per service type
        IsActive BIT DEFAULT 1,
        CreatedDateTime DATETIME DEFAULT GETDATE(),
        CreatedBy VARCHAR(100),
        FOREIGN KEY (TemplateId) REFERENCES FormTemplates(Id),
        INDEX IX_AppointmentFormMapping_Service (CompanyID, ServiceType, IsActive),
        INDEX IX_AppointmentFormMapping_Template (TemplateId)
    )
END

-- Create stored procedures for Forms management

-- Get all form templates
CREATE OR ALTER PROCEDURE sp_Forms_GetAllTemplates
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        Id, CompanyID, TemplateName, Description, Category,
        IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure,
        RequireSignature, RequireTip, IsActive,
        CreatedBy, CreatedDateTime, UpdatedBy, UpdatedDateTime
    FROM FormTemplates 
    WHERE CompanyID = @CompanyID 
    ORDER BY TemplateName
END

-- Get specific form template
CREATE OR ALTER PROCEDURE sp_Forms_GetTemplate
    @TemplateId INT,
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        Id, CompanyID, TemplateName, Description, Category,
        IsAutoAssignEnabled, AutoAssignServiceTypes, FormStructure,
        RequireSignature, RequireTip, IsActive,
        CreatedBy, CreatedDateTime, UpdatedBy, UpdatedDateTime
    FROM FormTemplates 
    WHERE Id = @TemplateId AND CompanyID = @CompanyID
END

-- Save form template (Insert/Update)
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

-- Get auto-assigned forms for service type
CREATE OR ALTER PROCEDURE sp_Forms_GetAutoAssignedTemplates
    @ServiceType VARCHAR(200),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT DISTINCT t.Id, t.CompanyID, t.TemplateName, t.Description, t.Category,
           t.RequireSignature, t.RequireTip, t.FormStructure, m.Priority
    FROM FormTemplates t
    INNER JOIN AppointmentFormMapping m ON t.Id = m.TemplateId
    WHERE m.CompanyID = @CompanyID 
      AND m.ServiceType = @ServiceType 
      AND m.IsAutoAssign = 1 
      AND m.IsActive = 1
      AND t.IsActive = 1
    ORDER BY m.Priority, t.TemplateName
END

-- Create form instance
CREATE OR ALTER PROCEDURE sp_Forms_CreateInstance
    @CompanyID VARCHAR(100),
    @TemplateId INT,
    @AppointmentId VARCHAR(100),
    @CustomerID VARCHAR(100),
    @FormData NVARCHAR(MAX),
    @Status VARCHAR(50),
    @FilledBy VARCHAR(100),
    @NewId INT OUTPUT
AS
BEGIN
    INSERT INTO FormInstances (
        CompanyID, TemplateId, AppointmentId, CustomerID,
        FormData, Status, FilledBy, StartedDateTime
    ) VALUES (
        @CompanyID, @TemplateId, @AppointmentId, @CustomerID,
        @FormData, @Status, @FilledBy, GETDATE()
    )
    
    SET @NewId = SCOPE_IDENTITY()
END

-- Save form instance field data (for detailed field-level storage)
CREATE OR ALTER PROCEDURE sp_Forms_SaveInstanceField
    @FormInstanceId INT,
    @FieldName VARCHAR(100),
    @FieldValue NVARCHAR(MAX),
    @FieldType VARCHAR(50),
    @FieldLabel VARCHAR(200)
AS
BEGIN
    -- Check if field already exists, update or insert
    IF EXISTS (SELECT 1 FROM FormInstanceFields WHERE FormInstanceId = @FormInstanceId AND FieldName = @FieldName)
    BEGIN
        UPDATE FormInstanceFields 
        SET FieldValue = @FieldValue,
            FieldType = @FieldType,
            FieldLabel = @FieldLabel
        WHERE FormInstanceId = @FormInstanceId AND FieldName = @FieldName
    END
    ELSE
    BEGIN
        INSERT INTO FormInstanceFields (FormInstanceId, FieldName, FieldValue, FieldType, FieldLabel)
        VALUES (@FormInstanceId, @FieldName, @FieldValue, @FieldType, @FieldLabel)
    END
END

-- Get form instance fields
CREATE OR ALTER PROCEDURE sp_Forms_GetInstanceFields
    @FormInstanceId INT
AS
BEGIN
    SELECT Id, FormInstanceId, FieldName, FieldValue, FieldType, FieldLabel, CreatedDateTime
    FROM FormInstanceFields
    WHERE FormInstanceId = @FormInstanceId
    ORDER BY FieldName
END

-- Update form instance
CREATE OR ALTER PROCEDURE sp_Forms_UpdateInstance
    @Id INT,
    @FormData NVARCHAR(MAX),
    @Status VARCHAR(50),
    @TechnicianSignature VARBINARY(MAX),
    @CustomerSignature VARBINARY(MAX),
    @TipAmount DECIMAL(10,2),
    @TipNotes VARCHAR(500),
    @CompletedDateTime DATETIME,
    @SubmittedDateTime DATETIME,
    @StoredFilePath VARCHAR(500),
    @StoredFileName VARCHAR(200)
AS
BEGIN
    UPDATE FormInstances SET
        FormData = @FormData,
        Status = @Status,
        TechnicianSignature = @TechnicianSignature,
        CustomerSignature = @CustomerSignature,
        TipAmount = @TipAmount,
        TipNotes = @TipNotes,
        CompletedDateTime = @CompletedDateTime,
        SubmittedDateTime = @SubmittedDateTime,
        StoredFilePath = @StoredFilePath,
        StoredFileName = @StoredFileName
    WHERE Id = @Id
END

-- Get forms for appointment
CREATE OR ALTER PROCEDURE sp_Forms_GetAppointmentForms
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        fi.Id, fi.CompanyID, fi.TemplateId, fi.AppointmentId, fi.CustomerID,
        fi.FormData, fi.Status, fi.TipAmount, fi.TipNotes, fi.FilledBy,
        fi.StartedDateTime, fi.CompletedDateTime, fi.SubmittedDateTime,
        fi.IsSynced, fi.StoredFilePath, fi.StoredFileName,
        ft.TemplateName, ft.RequireSignature, ft.RequireTip
    FROM FormInstances fi
    INNER JOIN FormTemplates ft ON fi.TemplateId = ft.Id
    WHERE fi.AppointmentId = @AppointmentId AND fi.CompanyID = @CompanyID
    ORDER BY ft.TemplateName
END

-- Log form usage
CREATE OR ALTER PROCEDURE sp_Forms_LogUsage
    @FormInstanceId INT,
    @TemplateId INT,
    @AppointmentId VARCHAR(100),
    @Action VARCHAR(50),
    @PerformedBy VARCHAR(100),
    @CompanyID VARCHAR(100),
    @Notes VARCHAR(500),
    @DeviceInfo VARCHAR(500)
AS
BEGIN
    INSERT INTO FormUsageLog (
        CompanyID, FormInstanceId, TemplateId, AppointmentId,
        Action, PerformedBy, Notes, DeviceInfo, ActionDateTime
    ) VALUES (
        @CompanyID, @FormInstanceId, @TemplateId, @AppointmentId,
        @Action, @PerformedBy, @Notes, @DeviceInfo, GETDATE()
    )
END

-- Get usage log
CREATE OR ALTER PROCEDURE sp_Forms_GetUsageLog
    @CompanyID VARCHAR(100),
    @TemplateId INT = NULL,
    @AppointmentId VARCHAR(100) = NULL
AS
BEGIN
    SELECT 
        Id, CompanyID, FormInstanceId, TemplateId, AppointmentId,
        Action, PerformedBy, ActionDateTime, Notes, DeviceInfo
    FROM FormUsageLog
    WHERE CompanyID = @CompanyID
      AND (@TemplateId IS NULL OR TemplateId = @TemplateId)
      AND (@AppointmentId IS NULL OR AppointmentId = @AppointmentId)
    ORDER BY ActionDateTime DESC
END

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

-- Get appointment by ID (for auto-fill)
CREATE OR ALTER PROCEDURE sp_Appointments_GetById
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        AppoinmentId, CustomerID, ServiceType, RequestDate,
        TimeSlot, ResourceName, Status, Note
    FROM Appointments
    WHERE AppoinmentId = @AppointmentId AND CompanyID = @CompanyID
END

-- Update attached forms for an appointment
CREATE OR ALTER PROCEDURE sp_Appointments_UpdateAttachedForms
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100),
    @FormIds VARCHAR(MAX),
    @UpdatedBy VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION
        
        -- First, remove existing form instances for this appointment
        DELETE FROM FormInstances 
        WHERE AppointmentId = @AppointmentId 
          AND CompanyID = @CompanyID
        
        -- If FormIds is provided, create new form instances
        IF @FormIds IS NOT NULL AND @FormIds != ''
        BEGIN
            -- Split the comma-separated FormIds and create instances
            DECLARE @FormId INT
            DECLARE @Pos INT = 1
            DECLARE @NextPos INT
            DECLARE @Value VARCHAR(100)
            
            -- Add comma at end to make parsing easier
            SET @FormIds = @FormIds + ','
            
            WHILE @Pos <= LEN(@FormIds)
            BEGIN
                SET @NextPos = CHARINDEX(',', @FormIds, @Pos)
                SET @Value = LTRIM(RTRIM(SUBSTRING(@FormIds, @Pos, @NextPos - @Pos)))
                
                IF @Value != ''
                BEGIN
                    SET @FormId = CAST(@Value AS INT)
                    
                    -- Create form instance for this template
                    INSERT INTO FormInstances (
                        CompanyID, TemplateId, AppointmentId, 
                        Status, StartedDateTime
                    ) VALUES (
                        @CompanyID, @FormId, @AppointmentId,
                        'Pending', GETDATE()
                    )
                END
                
                SET @Pos = @NextPos + 1
            END
        END
        
        COMMIT TRANSACTION
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END

-- Get appointment details with customer info
CREATE OR ALTER PROCEDURE sp_Appointments_GetDetails
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        a.AppoinmentId,
        a.CustomerID,
        a.ServiceType,
        a.RequestDate,
        a.TimeSlot,
        a.ResourceName,
        a.Status,
        a.Note,
        -- Customer details
        c.FirstName + ' ' + c.LastName as CustomerName,
        c.Email as CustomerEmail,
        c.Phone as CustomerPhone,
        c.Mobile as CustomerMobile
    FROM Appointments a
    LEFT JOIN Customers c ON a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
    WHERE a.AppoinmentId = @AppointmentId 
      AND a.CompanyID = @CompanyID
END