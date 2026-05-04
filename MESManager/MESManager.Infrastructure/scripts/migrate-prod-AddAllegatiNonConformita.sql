BEGIN TRANSACTION;
GO

CREATE TABLE [AllegatiNonConformita] (
    [Id] int NOT NULL IDENTITY,
    [NonConformitaId] uniqueidentifier NOT NULL,
    [NomeFile] nvarchar(255) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [Dati] varbinary(max) NOT NULL,
    [DimensioneBytes] bigint NOT NULL,
    [DataCaricamento] datetime2 NOT NULL,
    CONSTRAINT [PK_AllegatiNonConformita] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AllegatiNonConformita_NonConformita_NonConformitaId] FOREIGN KEY ([NonConformitaId]) REFERENCES [NonConformita] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AllegatiNonConformita_NonConformitaId] ON [AllegatiNonConformita] ([NonConformitaId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504123845_AddAllegatiNonConformita', N'8.0.26');
GO

COMMIT;
GO

