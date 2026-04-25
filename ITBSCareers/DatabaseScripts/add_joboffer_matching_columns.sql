USE CarriereDB;
GO

IF COL_LENGTH('dbo.JobOffers', 'RequiredDegree') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredDegree NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.JobOffers', 'RequiredLevel') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredLevel NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.JobOffers', 'RequiredField') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredField NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.JobOffers', 'RequiredSkillsCsv') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredSkillsCsv NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.JobOffers', 'RequiredInterestsCsv') IS NULL
    ALTER TABLE dbo.JobOffers ADD RequiredInterestsCsv NVARCHAR(MAX) NULL;
GO
