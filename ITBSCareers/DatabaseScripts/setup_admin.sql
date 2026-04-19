/*
Run this script on CarriereDB to create:
1) AlumniRequests table (if missing)
2) Admin role (if missing)
3) Admin user and role assignment
*/

USE [CarriereDB];
GO

IF OBJECT_ID('dbo.AlumniRequests', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AlumniRequests] (
        [AlumniRequestID] INT IDENTITY(1,1) PRIMARY KEY,
        [UserID] INT NOT NULL,
        [CompanyName] NVARCHAR(100) NULL,
        [Position] NVARCHAR(100) NULL,
        [ProofFilePath] NVARCHAR(255) NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_AlumniRequests_Status] DEFAULT ('Pending'),
        [ReviewedBy] INT NULL,
        [ReviewedAt] DATETIME NULL,
        [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_AlumniRequests_CreatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [FK_AlumniRequests_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [FK_AlumniRequests_ReviewedBy] FOREIGN KEY ([ReviewedBy]) REFERENCES [dbo].[Users]([UserID])
    );

    CREATE INDEX IX_AlumniRequests_UserID_Status ON [dbo].[AlumniRequests]([UserID], [Status]);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'Admin')
BEGIN
    INSERT INTO dbo.Roles (Name) VALUES ('Admin');
END
GO

DECLARE @AdminEmail NVARCHAR(100) = 'admin@itbs.local';
DECLARE @AdminPassword NVARCHAR(255) = 'Admin@123';
DECLARE @AdminFullName NVARCHAR(100) = 'ITBS Admin';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @AdminEmail)
BEGIN
    /*
      Password inserted in plain text intentionally.
      App will convert it to secure hash automatically after first successful login.
    */
    INSERT INTO dbo.Users (FullName, Email, PasswordHash, CreatedAt)
    VALUES (@AdminFullName, @AdminEmail, @AdminPassword, GETDATE());
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
