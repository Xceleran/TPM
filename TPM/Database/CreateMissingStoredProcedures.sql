-- Missing Stored Procedures for Appointments Forms Integration
-- Execute this to add the missing stored procedures

USE [myServiceJobs]
GO

-- Create stored procedure to update attached forms for an appointment
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
        
        -- Update the appointment record to track attached forms
        -- Note: This assumes there's an AttachedForms column in Appointments table
        -- If not, this part can be removed or the table structure updated
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME = 'Appointments' AND COLUMN_NAME = 'AttachedForms')
        BEGIN
            UPDATE Appointments 
            SET AttachedForms = @FormIds,
                UpdatedBy = @UpdatedBy,
                UpdatedDateTime = GETDATE()
            WHERE AppoinmentId = @AppointmentId 
              AND CompanyID = @CompanyID
        END
        
        COMMIT TRANSACTION
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END
GO

-- Create stored procedure to get appointment details (enhanced version)
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
GO

-- Create stored procedure to get appointment forms with template names
CREATE OR ALTER PROCEDURE sp_Forms_GetAppointmentForms
    @AppointmentId VARCHAR(100),
    @CompanyID VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        fi.Id,
        fi.CompanyID,
        fi.TemplateId,
        fi.AppointmentId,
        fi.CustomerID,
        fi.FormData,
        fi.Status,
        fi.TechnicianSignature,
        fi.CustomerSignature,
        fi.TipAmount,
        fi.TipNotes,
        fi.FilledBy,
        fi.StartedDateTime,
        fi.CompletedDateTime,
        fi.SubmittedDateTime,
        fi.IsSynced,
        fi.StoredFilePath,
        fi.StoredFileName,
        -- Include template name from join
        ft.TemplateName,
        ft.RequireSignature,
        ft.RequireTip
    FROM FormInstances fi
    INNER JOIN FormTemplates ft ON fi.TemplateId = ft.Id
    WHERE fi.AppointmentId = @AppointmentId 
      AND fi.CompanyID = @CompanyID
    ORDER BY fi.StartedDateTime DESC
END
GO

-- Add AttachedForms column to Appointments table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Appointments' AND COLUMN_NAME = 'AttachedForms')
BEGIN
    ALTER TABLE Appointments 
    ADD AttachedForms VARCHAR(MAX) NULL,
        UpdatedBy VARCHAR(100) NULL,
        UpdatedDateTime DATETIME NULL
    
    PRINT 'Added AttachedForms, UpdatedBy, and UpdatedDateTime columns to Appointments table'
END
ELSE
BEGIN
    PRINT 'AttachedForms column already exists in Appointments table'
END
GO

PRINT '✅ Missing stored procedures created successfully!'
PRINT ''
PRINT 'Created procedures:'
PRINT '- sp_Appointments_UpdateAttachedForms'
PRINT '- sp_Appointments_GetDetails (enhanced)'
PRINT '- sp_Forms_GetAppointmentForms (enhanced with template names)'
PRINT ''
PRINT 'Also added AttachedForms column to Appointments table if needed'