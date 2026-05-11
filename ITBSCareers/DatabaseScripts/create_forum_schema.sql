USE CarriereDB;
GO

IF OBJECT_ID('dbo.ForumCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumCategories (
        ForumCategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ForumCategories_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumCategories_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID('dbo.ForumTopics', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumTopics (
        ForumTopicId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ForumCategoryId INT NOT NULL,
        CreatedByUserId INT NOT NULL,
        IsLocked BIT NOT NULL CONSTRAINT DF_ForumTopics_IsLocked DEFAULT (0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_ForumTopics_IsDeleted DEFAULT (0),
        UpvotesCount INT NOT NULL CONSTRAINT DF_ForumTopics_UpvotesCount DEFAULT (0),
        DownvotesCount INT NOT NULL CONSTRAINT DF_ForumTopics_DownvotesCount DEFAULT (0),
        CommentsCount INT NOT NULL CONSTRAINT DF_ForumTopics_CommentsCount DEFAULT (0),
        ReportsCount INT NOT NULL CONSTRAINT DF_ForumTopics_ReportsCount DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumTopics_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_ForumTopics_UpdatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ForumTopics_Categories FOREIGN KEY (ForumCategoryId) REFERENCES dbo.ForumCategories(ForumCategoryId),
        CONSTRAINT FK_ForumTopics_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ForumComments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumComments (
        ForumCommentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ForumTopicId INT NOT NULL,
        CreatedByUserId INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_ForumComments_IsDeleted DEFAULT (0),
        UpvotesCount INT NOT NULL CONSTRAINT DF_ForumComments_UpvotesCount DEFAULT (0),
        DownvotesCount INT NOT NULL CONSTRAINT DF_ForumComments_DownvotesCount DEFAULT (0),
        ReportsCount INT NOT NULL CONSTRAINT DF_ForumComments_ReportsCount DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumComments_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_ForumComments_UpdatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ForumComments_Topics FOREIGN KEY (ForumTopicId) REFERENCES dbo.ForumTopics(ForumTopicId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumComments_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ForumVotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumVotes (
        ForumVoteId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ForumTopicId INT NOT NULL,
        ForumCommentId INT NULL,
        UserId INT NOT NULL,
        IsUpvote BIT NOT NULL CONSTRAINT DF_ForumVotes_IsUpvote DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumVotes_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ForumVotes_Topics FOREIGN KEY (ForumTopicId) REFERENCES dbo.ForumTopics(ForumTopicId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumVotes_Comments FOREIGN KEY (ForumCommentId) REFERENCES dbo.ForumComments(ForumCommentId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumVotes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_ForumVotes_Topic_User ON dbo.ForumVotes(ForumTopicId, UserId);
    CREATE UNIQUE INDEX UX_ForumVotes_Comment_User ON dbo.ForumVotes(ForumCommentId, UserId) WHERE ForumCommentId IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.ForumReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumReports (
        ForumReportId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ForumTopicId INT NOT NULL,
        ForumCommentId INT NULL,
        ReportedByUserId INT NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        IsResolved BIT NOT NULL CONSTRAINT DF_ForumReports_IsResolved DEFAULT (0),
        ResolvedByUserId INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumReports_CreatedAt DEFAULT (GETDATE()),
        ResolvedAt DATETIME NULL,
        CONSTRAINT FK_ForumReports_Topics FOREIGN KEY (ForumTopicId) REFERENCES dbo.ForumTopics(ForumTopicId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumReports_Comments FOREIGN KEY (ForumCommentId) REFERENCES dbo.ForumComments(ForumCommentId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumReports_ReportedBy FOREIGN KEY (ReportedByUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_ForumReports_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ForumTopicHistories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumTopicHistories (
        ForumTopicHistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ForumTopicId INT NOT NULL,
        ChangedByUserId INT NOT NULL,
        TitleSnapshot NVARCHAR(200) NOT NULL,
        ContentSnapshot NVARCHAR(MAX) NOT NULL,
        ChangedAt DATETIME NOT NULL CONSTRAINT DF_ForumTopicHistories_ChangedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ForumTopicHistories_Topics FOREIGN KEY (ForumTopicId) REFERENCES dbo.ForumTopics(ForumTopicId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumTopicHistories_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ForumCommentHistories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumCommentHistories (
        ForumCommentHistoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ForumCommentId INT NOT NULL,
        ChangedByUserId INT NOT NULL,
        ContentSnapshot NVARCHAR(MAX) NOT NULL,
        ChangedAt DATETIME NOT NULL CONSTRAINT DF_ForumCommentHistories_ChangedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_ForumCommentHistories_Comments FOREIGN KEY (ForumCommentId) REFERENCES dbo.ForumComments(ForumCommentId) ON DELETE CASCADE,
        CONSTRAINT FK_ForumCommentHistories_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ForumUserBans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ForumUserBans (
        ForumUserBanId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        BannedByUserId INT NOT NULL,
        Reason NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ForumUserBans_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ForumUserBans_CreatedAt DEFAULT (GETDATE()),
        EndsAt DATETIME NULL,
        CONSTRAINT FK_ForumUserBans_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_ForumUserBans_BannedBy FOREIGN KEY (BannedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO
