-- TPM Efficiency Platform migration
-- Run against msSchedulerV3

USE [msSchedulerV3]
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_TPStatusMapping')
BEGIN
    CREATE TABLE [dbo].[tbl_TPStatusMapping](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyID] [nvarchar](50) NOT NULL,
        [ThirdPartyId] [int] NULL,
        [CanonicalStatus] [nvarchar](50) NOT NULL,
        [PortalStatusCode] [nvarchar](100) NULL,
        [PortalStatusLabel] [nvarchar](200) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_tbl_TPStatusMapping] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_TPStatusMapping_Company] ON [dbo].[tbl_TPStatusMapping]([CompanyID], [ThirdPartyId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_TPMStatusQueue')
BEGIN
    CREATE TABLE [dbo].[tbl_TPMStatusQueue](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyID] [nvarchar](50) NOT NULL,
        [WorkOrderId] [int] NOT NULL,
        [ThirdPartyId] [int] NULL,
        [StatusCode] [nvarchar](50) NOT NULL,
        [PortalStatusCode] [nvarchar](100) NULL,
        [PayloadJson] [nvarchar](max) NULL,
        [SubmissionMethod] [nvarchar](50) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending',
        [RetryCount] [int] NOT NULL DEFAULT 0,
        [ErrorMessage] [nvarchar](max) NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [ProcessedDate] [datetime] NULL,
        CONSTRAINT [PK_tbl_TPMStatusQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_TPMStatusQueue_Pending] ON [dbo].[tbl_TPMStatusQueue]([Status], [CreatedDate]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_TPMInquiryThreads')
BEGIN
    CREATE TABLE [dbo].[tbl_TPMInquiryThreads](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CompanyID] [nvarchar](50) NOT NULL,
        [WorkOrderId] [int] NULL,
        [AppointmentId] [int] NULL,
        [CustomerId] [nvarchar](50) NULL,
        [ThirdPartyId] [int] NULL,
        [ChannelType] [nvarchar](50) NOT NULL,
        [AccessToken] [nvarchar](200) NULL,
        [Subject] [nvarchar](500) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Open',
        [EscalatedToStaff] [bit] NOT NULL DEFAULT 0,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] [datetime] NULL,
        CONSTRAINT [PK_tbl_TPMInquiryThreads] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_TPMInquiryThreads_Token] ON [dbo].[tbl_TPMInquiryThreads]([AccessToken]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_TPMInquiryMessages')
BEGIN
    CREATE TABLE [dbo].[tbl_TPMInquiryMessages](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ThreadId] [int] NOT NULL,
        [Direction] [nvarchar](20) NOT NULL,
        [SenderType] [nvarchar](50) NOT NULL,
        [MessageText] [nvarchar](max) NOT NULL,
        [AiConfidence] [decimal](5, 2) NULL,
        [SourceDataRefs] [nvarchar](max) NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_tbl_TPMInquiryMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_TPMInquiryMessages_Thread] FOREIGN KEY([ThreadId]) REFERENCES [dbo].[tbl_TPMInquiryThreads]([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Note') AND name = 'VisibilityScope')
BEGIN
    ALTER TABLE [dbo].[tbl_Note] ADD [VisibilityScope] [nvarchar](20) NULL DEFAULT 'Internal';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Note') AND name = 'IsAiAccessible')
BEGIN
    ALTER TABLE [dbo].[tbl_Note] ADD [IsAiAccessible] [bit] NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_TPMSettings') AND name = 'AiChatEnabled')
BEGIN
    ALTER TABLE [dbo].[tbl_TPMSettings] ADD [AiChatEnabled] [bit] NULL DEFAULT 0;
    ALTER TABLE [dbo].[tbl_TPMSettings] ADD [AiAutoReply] [bit] NULL DEFAULT 0;
    ALTER TABLE [dbo].[tbl_TPMSettings] ADD [PolicyHolderDataScope] [nvarchar](max) NULL;
    ALTER TABLE [dbo].[tbl_TPMSettings] ADD [ThirdPartyDataScope] [nvarchar](max) NULL;
END
GO

-- Seed default canonical status mappings (global defaults, ThirdPartyId NULL)
IF NOT EXISTS (SELECT 1 FROM tbl_TPStatusMapping WHERE CompanyID = 'DEFAULT')
BEGIN
    INSERT INTO tbl_TPStatusMapping (CompanyID, ThirdPartyId, CanonicalStatus, PortalStatusCode, PortalStatusLabel) VALUES
    ('DEFAULT', NULL, 'New', 'NEW', 'New / Received'),
    ('DEFAULT', NULL, 'Acknowledged', 'ACK', 'Acknowledged'),
    ('DEFAULT', NULL, 'PendingAuthorization', 'PENDAUTH', 'Pending Authorization'),
    ('DEFAULT', NULL, 'Scheduled', 'SCHED', 'Scheduled'),
    ('DEFAULT', NULL, 'InProgress', 'INPROG', 'In Progress / On Site'),
    ('DEFAULT', NULL, 'AwaitingParts', 'PARTS', 'Awaiting Parts'),
    ('DEFAULT', NULL, 'PendingInfo', 'PENDINFO', 'Pending Info'),
    ('DEFAULT', NULL, 'Approved', 'APPROVED', 'Approved'),
    ('DEFAULT', NULL, 'Denied', 'DENIED', 'Denied'),
    ('DEFAULT', NULL, 'InvoiceSubmitted', 'INVSUB', 'Invoice Submitted'),
    ('DEFAULT', NULL, 'PaymentPending', 'PAYPEND', 'Payment Pending'),
    ('DEFAULT', NULL, 'Reconciled', 'RECON', 'Reconciled / Paid'),
    ('DEFAULT', NULL, 'Closed', 'CLOSED', 'Closed'),
    ('DEFAULT', NULL, 'Escalated', 'ESC', 'Escalated');
END
GO

PRINT 'TPM Efficiency migration completed.'
GO
