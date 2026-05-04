BEGIN TRANSACTION;
GO

CREATE TABLE [NonConformita] (
    [Id] uniqueidentifier NOT NULL,
    [CodiceArticolo] nvarchar(100) NOT NULL,
    [DescrizioneArticolo] nvarchar(250) NULL,
    [Cliente] nvarchar(200) NULL,
    [DataSegnalazione] datetime2 NOT NULL,
    [Tipo] nvarchar(50) NOT NULL,
    [Gravita] nvarchar(50) NOT NULL,
    [Descrizione] nvarchar(max) NOT NULL,
    [AzioneCorrettiva] nvarchar(max) NULL,
    [Stato] nvarchar(50) NOT NULL,
    [CreatoDa] nvarchar(200) NULL,
    [CreatoIl] datetime2 NOT NULL,
    [ModificatoDa] nvarchar(200) NULL,
    [ModificatoIl] datetime2 NULL,
    [DataChiusura] datetime2 NULL,
    CONSTRAINT [PK_NonConformita] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504090704_AddNonConformita', N'8.0.26');
GO

COMMIT;
GO

