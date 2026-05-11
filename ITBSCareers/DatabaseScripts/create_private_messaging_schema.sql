USE CarriereDB;
GO

IF COL_LENGTH('dbo.Conversations', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.Conversations ADD CreatedByUserId INT NOT NULL CONSTRAINT DF_Conversations_CreatedByUserId DEFAULT(1);
GO
IF COL_LENGTH('dbo.Conversations', 'IsDeleted') IS NULL
    ALTER TABLE dbo.Conversations ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Conversations_IsDeleted DEFAULT(0);
GO
IF COL_LENGTH('dbo.Conversations', 'DeletedAt') IS NULL
    ALTER TABLE dbo.Conversations ADD DeletedAt DATETIME NULL;
GO
IF COL_LENGTH('dbo.Conversations', 'Subject') IS NULL
    ALTER TABLE dbo.Conversations ADD Subject NVARCHAR(200) NULL;
GO

IF OBJECT_ID('dbo.ConversationParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationParticipants (
        ConversationParticipantId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConversationId INT NOT NULL,
        UserId INT NOT NULL,
        JoinedAt DATETIME NOT NULL CONSTRAINT DF_ConversationParticipants_JoinedAt DEFAULT(GETDATE()),
        LastReadAt DATETIME NULL,
        IsArchived BIT NOT NULL CONSTRAINT DF_ConversationParticipants_IsArchived DEFAULT(0),
        CONSTRAINT FK_ConversationParticipants_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(ConversationID) ON DELETE CASCADE,
        CONSTRAINT FK_ConversationParticipants_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_ConversationParticipants_Conversation_User ON dbo.ConversationParticipants(ConversationId, UserId);
END
GO

IF COL_LENGTH('dbo.Messages', 'AttachmentPath') IS NULL
    ALTER TABLE dbo.Messages ADD AttachmentPath NVARCHAR(255) NULL;
GO
IF COL_LENGTH('dbo.Messages', 'AttachmentName') IS NULL
    ALTER TABLE dbo.Messages ADD AttachmentName NVARCHAR(255) NULL;
GO
IF COL_LENGTH('dbo.Messages', 'AttachmentContentType') IS NULL
    ALTER TABLE dbo.Messages ADD AttachmentContentType NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.Messages', 'AttachmentSize') IS NULL
    ALTER TABLE dbo.Messages ADD AttachmentSize BIGINT NULL;
GO
IF COL_LENGTH('dbo.Messages', 'IsRead') IS NULL
    ALTER TABLE dbo.Messages ADD IsRead BIT NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT(0);
GO
IF COL_LENGTH('dbo.Messages', 'ReadAt') IS NULL
    ALTER TABLE dbo.Messages ADD ReadAt DATETIME NULL;
GO

IF OBJECT_ID('dbo.PrivateUserBlocks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivateUserBlocks (
        PrivateUserBlockId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BlockerUserId INT NOT NULL,
        BlockedUserId INT NOT NULL,
        Reason NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PrivateUserBlocks_CreatedAt DEFAULT(GETDATE()),
        CONSTRAINT FK_PrivateUserBlocks_Blocker FOREIGN KEY (BlockerUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_PrivateUserBlocks_Blocked FOREIGN KEY (BlockedUserId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_PrivateUserBlocks_Blocker_Blocked ON dbo.PrivateUserBlocks(BlockerUserId, BlockedUserId);
END
GO

IF OBJECT_ID('dbo.PrivateUserReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivateUserReports (
        PrivateUserReportId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReporterUserId INT NOT NULL,
        ReportedUserId INT NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        IsResolved BIT NOT NULL CONSTRAINT DF_PrivateUserReports_IsResolved DEFAULT(0),
        ResolvedByUserId INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PrivateUserReports_CreatedAt DEFAULT(GETDATE()),
        ResolvedAt DATETIME NULL,
        CONSTRAINT FK_PrivateUserReports_Reporter FOREIGN KEY (ReporterUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_PrivateUserReports_Reported FOREIGN KEY (ReportedUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_PrivateUserReports_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.MentorshipRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MentorshipRequests (
        MentorshipRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StudentId INT NOT NULL,
        AlumniId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_MentorshipRequests_Status DEFAULT('Pending'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MentorshipRequests_CreatedAt DEFAULT(GETDATE()),
        ReviewedAt DATETIME NULL,
        CONSTRAINT FK_MentorshipRequests_Student FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_MentorshipRequests_Alumni FOREIGN KEY (AlumniId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_MentorshipRequests_Student_Alumni ON dbo.MentorshipRequests(StudentId, AlumniId);
END
GO
