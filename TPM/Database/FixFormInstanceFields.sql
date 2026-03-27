-- Quick fix script to add missing FormInstanceFields table
-- Execute this if you've already run the main schema but got the FormInstanceFields error

USE [myServiceJobs]
GO

PRINT 'Adding missing FormInstanceFields table...'

-- Create FormInstanceFields table if it doesn't exist
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
    PRINT 'FormInstanceFields table created successfully!'
END
ELSE
BEGIN
    PRINT 'FormInstanceFields table already exists.'
END

-- Add supporting stored procedures
GO

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
GO

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
GO

PRINT 'FormInstanceFields setup completed!'
PRINT 'You can now re-run your sample data script.'