USE CarriereDB;
GO

IF COL_LENGTH('dbo.Alumnis', 'IsContactPublic') IS NULL
    ALTER TABLE dbo.Alumnis ADD IsContactPublic BIT NOT NULL CONSTRAINT DF_Alumnis_IsContactPublic DEFAULT(0);
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
