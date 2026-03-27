-- Additional stored procedures for forms file storage integration
-- Database: myServiceJobs (Server: 3.148.0.246)

USE [myServiceJobs]
GO

-- Update form instance with file storage information
CREATE OR ALTER PROCEDURE sp_Forms_UpdateFileStorage
    @FormInstanceId INT,
    @StoredFilePath VARCHAR(500),
    @StoredFileName VARCHAR(200),
    @IsSynced BIT = 0
AS
BEGIN
    UPDATE FormInstances 
    SET StoredFilePath = @StoredFilePath,
        StoredFileName = @StoredFileName,
        IsSynced = @IsSynced,
        SyncedDateTime = CASE WHEN @IsSynced = 1 THEN GETDATE() ELSE NULL END
    WHERE Id = @FormInstanceId
END

-- Get forms for appointment with file information
CREATE OR ALTER PROCEDURE sp_Forms_GetAppointmentFormsWithFiles
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        fi.Id, fi.CompanyID, fi.TemplateId, fi.AppointmentId, fi.CustomerID,
        fi.FormData, fi.Status, fi.TipAmount, fi.TipNotes, fi.FilledBy,
        fi.StartedDateTime, fi.CompletedDateTime, fi.SubmittedDateTime,
        fi.IsSynced, fi.SyncedDateTime, fi.StoredFilePath, fi.StoredFileName,
        ft.TemplateName, ft.RequireSignature, ft.RequireTip,
        ft.Description, ft.Category
    FROM FormInstances fi
    INNER JOIN FormTemplates ft ON fi.TemplateId = ft.Id
    WHERE fi.AppointmentId = @AppointmentId 
      AND fi.CompanyID = @CompanyID
      AND fi.Status IN ('Completed', 'Submitted')
    ORDER BY fi.SubmittedDateTime DESC, ft.TemplateName
END

-- Mark form as synced with external storage
CREATE OR ALTER PROCEDURE sp_Forms_MarkAsSynced
    @FormInstanceId INT
AS
BEGIN
    UPDATE FormInstances 
    SET IsSynced = 1,
        SyncedDateTime = GETDATE()
    WHERE Id = @FormInstanceId
END

-- Get unsync forms for batch processing
CREATE OR ALTER PROCEDURE sp_Forms_GetUnsyncedForms
    @CompanyID VARCHAR(100),
    @Limit INT = 50
AS
BEGIN
    SELECT TOP (@Limit)
        fi.Id, fi.AppointmentId, fi.FormData, fi.StoredFilePath, fi.StoredFileName,
        ft.TemplateName, ft.FormStructure
    FROM FormInstances fi
    INNER JOIN FormTemplates ft ON fi.TemplateId = ft.Id
    WHERE fi.CompanyID = @CompanyID
      AND fi.IsSynced = 0
      AND fi.Status = 'Submitted'
      AND fi.StoredFilePath IS NOT NULL
    ORDER BY fi.SubmittedDateTime ASC
END

-- Create appointment file record integration
CREATE OR ALTER PROCEDURE sp_AppointmentFiles_AddFormFile
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100),
    @FileName VARCHAR(200),
    @FilePath VARCHAR(500),
    @FileType VARCHAR(50) = 'Form',
    @FormInstanceId INT,
    @UploadedBy VARCHAR(100),
    @NewId INT OUTPUT
AS
BEGIN
    -- Check if AppointmentFiles table exists, if not create a basic version
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentFiles' AND xtype='U')
    BEGIN
        CREATE TABLE AppointmentFiles (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            AppointmentId VARCHAR(100) NOT NULL,
            CompanyID VARCHAR(100) NOT NULL,
            FileName VARCHAR(200) NOT NULL,
            FilePath VARCHAR(500) NOT NULL,
            FileType VARCHAR(50) DEFAULT 'Document',
            FormInstanceId INT NULL,
            FileSize BIGINT NULL,
            MimeType VARCHAR(100) NULL,
            UploadedBy VARCHAR(100) NULL,
            UploadedDateTime DATETIME DEFAULT GETDATE(),
            IsActive BIT DEFAULT 1,
            INDEX IX_AppointmentFiles_Appointment (CompanyID, AppointmentId),
            INDEX IX_AppointmentFiles_Form (FormInstanceId)
        )
    END
    
    -- Insert the file record
    INSERT INTO AppointmentFiles (
        AppointmentId, CompanyID, FileName, FilePath, FileType,
        FormInstanceId, UploadedBy, UploadedDateTime
    ) VALUES (
        @AppointmentId, @CompanyID, @FileName, @FilePath, @FileType,
        @FormInstanceId, @UploadedBy, GETDATE()
    )
    
    SET @NewId = SCOPE_IDENTITY()
END

-- Get appointment files including forms
CREATE OR ALTER PROCEDURE sp_AppointmentFiles_GetAll
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SELECT 
        af.Id, af.AppointmentId, af.FileName, af.FilePath, af.FileType,
        af.FormInstanceId, af.UploadedBy, af.UploadedDateTime,
        fi.Status as FormStatus, ft.TemplateName
    FROM AppointmentFiles af
    LEFT JOIN FormInstances fi ON af.FormInstanceId = fi.Id
    LEFT JOIN FormTemplates ft ON fi.TemplateId = ft.Id
    WHERE af.AppointmentId = @AppointmentId 
      AND af.CompanyID = @CompanyID
      AND af.IsActive = 1
    ORDER BY af.UploadedDateTime DESC
END"