USE CarriereDB;

/*
  Demo CV seed
  Prerequisite: demo files exist in wwwroot/uploads/cvs/demo/
  - cv_amine_demo.pdf
  - cv_lina_demo.pdf
  - cv_sana_demo.pdf
*/

DECLARE @amineId INT = (SELECT TOP 1 UserID FROM Users WHERE Email = 'amine@student.itbs');
DECLARE @linaId  INT = (SELECT TOP 1 UserID FROM Users WHERE Email = 'lina@student.itbs');
DECLARE @sanaId  INT = (SELECT TOP 1 UserID FROM Users WHERE Email = 'sana@alumni.itbs');

DELETE FROM CVs WHERE FilePath LIKE '/uploads/cvs/demo/%';

IF @amineId IS NOT NULL
BEGIN
    INSERT INTO CVs (UserID, FilePath, UploadedAt)
    VALUES (@amineId, '/uploads/cvs/demo/cv_amine_demo.pdf', GETDATE());
END

IF @linaId IS NOT NULL
BEGIN
    INSERT INTO CVs (UserID, FilePath, UploadedAt)
    VALUES (@linaId, '/uploads/cvs/demo/cv_lina_demo.pdf', DATEADD(MINUTE, -15, GETDATE()));
END

IF @sanaId IS NOT NULL
BEGIN
    INSERT INTO CVs (UserID, FilePath, UploadedAt)
    VALUES (@sanaId, '/uploads/cvs/demo/cv_sana_demo.pdf', DATEADD(MINUTE, -30, GETDATE()));
END

SELECT TOP 50 *
FROM CVs
ORDER BY UploadedAt DESC;
