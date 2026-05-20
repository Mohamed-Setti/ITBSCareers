/*
    Full CarriereDB setup script for ITBSCareers.

    This script recreates the database schema used by the project and seeds the
    reference data that the application expects.

    Note:
    - It does not export live user-generated data from an existing database.
    - For an exact copy of an existing production database, use a SQL backup or BACPAC.
*/

IF DB_ID(N'CarriereDB') IS NULL
BEGIN
    CREATE DATABASE [CarriereDB];
END
GO

USE [CarriereDB];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* Core lookup tables */
IF OBJECT_ID('dbo.Degrees', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Degrees (
        DegreeID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Degrees_Name ON dbo.Degrees(Name);
END
GO

IF OBJECT_ID('dbo.Interests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Interests (
        InterestID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Interests_Name ON dbo.Interests(Name);
END
GO

IF OBJECT_ID('dbo.Skills', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Skills (
        SkillID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Skills_Name ON dbo.Skills(Name);
END
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles(Name);
END
GO

/* Users and profile tables */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        CreatedAt DATETIME NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
END
GO

IF OBJECT_ID('dbo.Students', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Students (
        StudentID INT NOT NULL PRIMARY KEY,
        DegreeID INT NOT NULL,
        Field NVARCHAR(100) NULL,
        Level NVARCHAR(50) NULL,
        CONSTRAINT FK_Students_User FOREIGN KEY (StudentID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_Students_Degree FOREIGN KEY (DegreeID) REFERENCES dbo.Degrees(DegreeID)
    );
END
GO

IF OBJECT_ID('dbo.Alumnis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alumnis (
        AlumniID INT NOT NULL PRIMARY KEY,
        CompanyName NVARCHAR(100) NULL,
        Position NVARCHAR(100) NULL,
        ExperienceYears INT NULL,
        IsContactPublic BIT NOT NULL CONSTRAINT DF_Alumnis_IsContactPublic DEFAULT (0),
        CONSTRAINT FK_Alumnis_User FOREIGN KEY (AlumniID) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.UserInterests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserInterests (
        UserID INT NOT NULL,
        InterestID INT NOT NULL,
        CONSTRAINT PK_UserInterests PRIMARY KEY (UserID, InterestID),
        CONSTRAINT FK_UserInterests_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_UserInterests_Interest FOREIGN KEY (InterestID) REFERENCES dbo.Interests(InterestID)
    );
END
GO

IF OBJECT_ID('dbo.UserSkills', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSkills (
        UserID INT NOT NULL,
        SkillID INT NOT NULL,
        CONSTRAINT PK_UserSkills PRIMARY KEY (UserID, SkillID),
        CONSTRAINT FK_UserSkills_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_UserSkills_Skill FOREIGN KEY (SkillID) REFERENCES dbo.Skills(SkillID)
    );
END
GO

IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserRoleID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NULL,
        RoleID INT NULL,
        CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
    );
END
GO

IF OBJECT_ID('dbo.Experiences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Experiences (
        ExperienceID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NULL,
        Title NVARCHAR(100) NULL,
        Company NVARCHAR(100) NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        Description NVARCHAR(MAX) NULL,
        CONSTRAINT FK_Experiences_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.CVs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CVs (
        CVID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NOT NULL,
        FilePath NVARCHAR(255) NOT NULL,
        UploadedAt DATETIME NULL CONSTRAINT DF_CVs_UploadedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_CVs_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );
END
GO

/* Hiring and applications */
IF OBJECT_ID('dbo.JobOffers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobOffers (
        JobID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AlumniID INT NOT NULL,
        Title NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Type NVARCHAR(50) NULL,
        Location NVARCHAR(100) NULL,
        CreatedAt DATETIME NULL CONSTRAINT DF_JobOffers_CreatedAt DEFAULT (GETDATE()),
        RequiredDegree NVARCHAR(100) NULL,
        RequiredLevel NVARCHAR(50) NULL,
        RequiredField NVARCHAR(100) NULL,
        RequiredSkillsCsv NVARCHAR(MAX) NULL,
        RequiredInterestsCsv NVARCHAR(MAX) NULL,
        CONSTRAINT FK_JobOffers_User FOREIGN KEY (AlumniID) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.Applications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Applications (
        ApplicationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        JobID INT NOT NULL,
        StudentID INT NOT NULL,
        CVID INT NULL,
        Status NVARCHAR(50) NULL CONSTRAINT DF_Applications_Status DEFAULT ('Pending'),
        AppliedAt DATETIME NULL CONSTRAINT DF_Applications_AppliedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_Applications_Job FOREIGN KEY (JobID) REFERENCES dbo.JobOffers(JobID),
        CONSTRAINT FK_Applications_Student FOREIGN KEY (StudentID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_Applications_CV FOREIGN KEY (CVID) REFERENCES dbo.CVs(CVID)
    );
END
GO

IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        NotificationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NOT NULL,
        Content NVARCHAR(255) NULL,
        Type NVARCHAR(50) NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAt DATETIME NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.EmailLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailLogs (
        EmailLogID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NULL,
        ToEmail NVARCHAR(100) NULL,
        Subject NVARCHAR(200) NULL,
        Body NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_EmailLogs_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_EmailLogs_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
    );
    CREATE INDEX IX_EmailLogs_UserID_CreatedAt ON dbo.EmailLogs(UserID, CreatedAt DESC);
END
GO

/* Alumni requests */
IF OBJECT_ID('dbo.AlumniRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlumniRequests (
        AlumniRequestID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NOT NULL,
        CompanyName NVARCHAR(100) NULL,
        Position NVARCHAR(100) NULL,
        ProofFilePath NVARCHAR(255) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_AlumniRequests_Status DEFAULT ('Pending'),
        ReviewedBy INT NULL,
        ReviewedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_AlumniRequests_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_AlumniRequests_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_AlumniRequests_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES dbo.Users(UserID)
    );
    CREATE INDEX IX_AlumniRequests_UserID_Status ON dbo.AlumniRequests(UserID, Status);
END
GO

/* Alumni contact requests / private messaging support */
IF OBJECT_ID('dbo.Conversations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Conversations (
        ConversationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt DATETIME NULL CONSTRAINT DF_Conversations_CreatedAt DEFAULT (GETDATE()),
        CreatedByUserId INT NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_Conversations_IsDeleted DEFAULT (0),
        DeletedAt DATETIME NULL,
        Subject NVARCHAR(200) NULL,
        CONSTRAINT FK_Conversations_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.ConversationParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationParticipants (
        ConversationParticipantId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConversationId INT NOT NULL,
        UserId INT NOT NULL,
        JoinedAt DATETIME NOT NULL CONSTRAINT DF_ConversationParticipants_JoinedAt DEFAULT (GETDATE()),
        LastReadAt DATETIME NULL,
        IsArchived BIT NOT NULL CONSTRAINT DF_ConversationParticipants_IsArchived DEFAULT (0),
        CONSTRAINT FK_ConversationParticipants_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(ConversationID) ON DELETE CASCADE,
        CONSTRAINT FK_ConversationParticipants_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_ConversationParticipants_Conversation_User ON dbo.ConversationParticipants(ConversationId, UserId);
END
GO

IF OBJECT_ID('dbo.Messages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Messages (
        MessageID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConversationID INT NOT NULL,
        SenderID INT NOT NULL,
        ReceiverID INT NOT NULL,
        Content NVARCHAR(MAX) NULL,
        AttachmentPath NVARCHAR(255) NULL,
        AttachmentName NVARCHAR(255) NULL,
        AttachmentContentType NVARCHAR(100) NULL,
        AttachmentSize BIGINT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT (0),
        ReadAt DATETIME NULL,
        SentAt DATETIME NULL CONSTRAINT DF_Messages_SentAt DEFAULT (GETDATE()),
        CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationID) REFERENCES dbo.Conversations(ConversationID),
        CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_Messages_Receiver FOREIGN KEY (ReceiverID) REFERENCES dbo.Users(UserID)
    );
END
GO

IF OBJECT_ID('dbo.MentorshipRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MentorshipRequests (
        MentorshipRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StudentId INT NOT NULL,
        AlumniId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_MentorshipRequests_Status DEFAULT ('Pending'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_MentorshipRequests_CreatedAt DEFAULT (GETDATE()),
        ReviewedAt DATETIME NULL,
        CONSTRAINT FK_MentorshipRequests_Student FOREIGN KEY (StudentId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_MentorshipRequests_Alumni FOREIGN KEY (AlumniId) REFERENCES dbo.Users(UserID)
    );
    CREATE UNIQUE INDEX UX_MentorshipRequests_Student_Alumni ON dbo.MentorshipRequests(StudentId, AlumniId);
END
GO

IF OBJECT_ID('dbo.PrivateUserBlocks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivateUserBlocks (
        PrivateUserBlockId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BlockerUserId INT NOT NULL,
        BlockedUserId INT NOT NULL,
        Reason NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PrivateUserBlocks_CreatedAt DEFAULT (GETDATE()),
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
        IsResolved BIT NOT NULL CONSTRAINT DF_PrivateUserReports_IsResolved DEFAULT (0),
        ResolvedByUserId INT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PrivateUserReports_CreatedAt DEFAULT (GETDATE()),
        ResolvedAt DATETIME NULL,
        CONSTRAINT FK_PrivateUserReports_Reporter FOREIGN KEY (ReporterUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_PrivateUserReports_Reported FOREIGN KEY (ReportedUserId) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_PrivateUserReports_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users(UserID)
    );
END
GO

/* Forum */
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

/* Utility table used by the project */
IF OBJECT_ID('dbo.Table', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Table] (
        Id INT NOT NULL PRIMARY KEY
    );
END
GO

/* Reference data */
IF NOT EXISTS (SELECT 1 FROM dbo.Degrees)
BEGIN
    INSERT INTO dbo.Degrees (Name)
    VALUES (N'BI'), (N'GL'), (N'Reseaux'), (N'IOT');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (Name)
    VALUES (N'Student'), (N'Alumni'), (N'Admin');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Interests)
BEGIN
    INSERT INTO dbo.Interests (Name)
    VALUES
    (N'Data Science'),
    (N'Machine Learning'),
    (N'Deep Learning'),
    (N'Big Data'),
    (N'Data Visualization'),
    (N'Web Development'),
    (N'Mobile Development'),
    (N'Game Development'),
    (N'Software Engineering'),
    (N'Cybersecurity'),
    (N'Cloud Computing'),
    (N'DevOps'),
    (N'Networking'),
    (N'Embedded Systems'),
    (N'Robotics'),
    (N'IoT Systems'),
    (N'Entrepreneurship'),
    (N'Project Management'),
    (N'UI/UX Design'),
    (N'Product Management');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Skills)
BEGIN
    INSERT INTO dbo.Skills (Name)
    VALUES
    (N'Python'), (N'Java'), (N'C'), (N'C++'), (N'C#'),
    (N'JavaScript'), (N'TypeScript'), (N'Go'), (N'Rust'), (N'PHP'),
    (N'HTML'), (N'CSS'), (N'React'), (N'Angular'), (N'Vue.js'),
    (N'ASP.NET'), (N'Node.js'), (N'Django'), (N'Flask'),
    (N'Machine Learning'), (N'Deep Learning'), (N'Data Analysis'), (N'Data Mining'),
    (N'Natural Language Processing'), (N'Computer Vision'), (N'TensorFlow'), (N'PyTorch'),
    (N'Pandas'), (N'NumPy'),
    (N'SQL'), (N'MySQL'), (N'SQL Server'), (N'PostgreSQL'), (N'MongoDB'), (N'NoSQL'),
    (N'Docker'), (N'Kubernetes'), (N'CI/CD'), (N'Git'), (N'GitHub'), (N'Azure'), (N'AWS'), (N'Google Cloud'),
    (N'Network Security'), (N'Penetration Testing'), (N'Cryptography'),
    (N'TCP/IP'), (N'Routing'), (N'Switching'), (N'Network Administration'),
    (N'Arduino'), (N'Raspberry Pi'), (N'Embedded C'), (N'Sensors Integration'),
    (N'Teamwork'), (N'Communication'), (N'Problem Solving'), (N'Leadership'), (N'Time Management');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ForumCategories)
BEGIN
    INSERT INTO dbo.ForumCategories (Name, Description, IsActive)
    VALUES
    (N'Orientation carri?re', N'Questions sur les parcours, stages et premiers emplois.', 1),
    (N'Comp?tences techniques', N'Discussions autour du d?veloppement, data, cloud et outils.', 1),
    (N'Entretiens', N'Astuces pour les entretiens et retours d''exp?rience.', 1),
    (N'Vie professionnelle', N'Culture d''entreprise, soft skills et ?volution de carri?re.', 1),
    (N'Responsabilit? num?rique', N'Veille sur les bonnes pratiques et la s?curit?.', 1);
END
GO

/* Admin bootstrap */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@itbs.local')
BEGIN
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, CreatedAt)
    VALUES (N'ITBS Admin', 'admin@itbs.local', 'Admin@123', GETDATE());
END
GO

DECLARE @AdminUserId INT = (SELECT TOP 1 UserID FROM dbo.Users WHERE Email = 'admin@itbs.local');
DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleID FROM dbo.Roles WHERE Name = 'Admin');

IF @AdminUserId IS NOT NULL AND @AdminRoleId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserID = @AdminUserId AND RoleID = @AdminRoleId)
BEGIN
    INSERT INTO dbo.UserRoles (UserID, RoleID)
    VALUES (@AdminUserId, @AdminRoleId);
END
GO

/* Optional demo CVs: keep separate if demo files exist in wwwroot/uploads/cvs/demo */
