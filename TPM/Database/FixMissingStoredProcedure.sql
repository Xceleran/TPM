-- Quick fix for missing sp_Appointments_UpdateAttachedForms stored procedure
-- Execute this script to add the missing stored procedure

USE [myServiceJobs]
GO

-- Create the missing stored procedure
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
GO

-- Also create the enhanced sp_Appointments_GetDetails if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Appointments_GetDetails')
BEGIN
    EXEC('
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
            ISNULL(c.FirstName + '' '' + c.LastName, ''Customer'') as CustomerName,
            c.Email as CustomerEmail,
            c.Phone as CustomerPhone,
            c.Mobile as CustomerMobile
        FROM Appointments a
        LEFT JOIN Customers c ON a.CustomerID = c.CustomerID AND a.CompanyID = c.CompanyID
        WHERE a.AppoinmentId = @AppointmentId 
          AND a.CompanyID = @CompanyID
    END')
    
    PRINT 'Created sp_Appointments_GetDetails'
END

PRINT '✅ Fixed missing stored procedure sp_Appointments_UpdateAttachedForms'
PRINT 'The appointment forms update functionality should now work correctly.'