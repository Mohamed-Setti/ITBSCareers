USE CarriereDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ForumCategories)
BEGIN
    INSERT INTO dbo.ForumCategories (Name, Description, IsActive)
    VALUES
    (N'Orientation carrière', N'Questions sur les parcours, stages et premiers emplois.', 1),
    (N'Compétences techniques', N'Discussions autour du développement, data, cloud et outils.', 1),
    (N'Entretiens', N'Astuces pour les entretiens et retours d'expérience.', 1),
    (N'Vie professionnelle', N'Culture d'entreprise, soft skills et évolution de carrière.', 1),
    (N'Responsabilité numérique', N'Veille sur les bonnes pratiques et la sécurité.', 1);
END
GO
