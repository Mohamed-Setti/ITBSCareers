/*
    Create EmailLogs table for existing CarriereDB database.
    Run this script once if dbo.EmailLogs is missing.
*/

USE [CarriereDB];
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
