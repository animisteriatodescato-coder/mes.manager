BEGIN TRANSACTION;
GO

CREATE TABLE [PlcServiceStatus] (
    [Id] int NOT NULL IDENTITY,
    [IsRunning] bit NOT NULL,
    [ServiceStartTime] datetime2 NULL,
    [LastHeartbeat] datetime2 NULL,
    [ServiceVersion] nvarchar(max) NOT NULL,
    [PollingIntervalSeconds] int NOT NULL,
    [EnableRealtime] bit NOT NULL,
    [EnableStorico] bit NOT NULL,
    [EnableEvents] bit NOT NULL,
    [TotalSyncCount] int NOT NULL,
    [TotalErrorCount] int NOT NULL,
    [LastSyncTime] datetime2 NULL,
    [LastErrorTime] datetime2 NULL,
    [LastErrorMessage] nvarchar(max) NULL,
    [MachinesConfigured] int NOT NULL,
    [MachinesConnected] int NOT NULL,
    CONSTRAINT [PK_PlcServiceStatus] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PlcSyncLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [Timestamp] datetime2 NOT NULL,
    [MacchinaId] uniqueidentifier NULL,
    [MacchinaNumero] nvarchar(max) NULL,
    [Level] nvarchar(450) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NULL,
    CONSTRAINT [PK_PlcSyncLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PlcSyncLogs_Macchine_MacchinaId] FOREIGN KEY ([MacchinaId]) REFERENCES [Macchine] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_PlcSyncLogs_Level] ON [PlcSyncLogs] ([Level]);
GO

CREATE INDEX [IX_PlcSyncLogs_MacchinaId] ON [PlcSyncLogs] ([MacchinaId]);
GO

CREATE INDEX [IX_PlcSyncLogs_Timestamp] ON [PlcSyncLogs] ([Timestamp]);
GO

-- Inserisci record iniziale per PlcServiceStatus
INSERT INTO [PlcServiceStatus] (IsRunning, ServiceVersion, PollingIntervalSeconds, EnableRealtime, EnableStorico, EnableEvents, TotalSyncCount, TotalErrorCount, MachinesConfigured, MachinesConnected)
VALUES (0, '1.0.0', 30, 1, 1, 1, 0, 0, 0, 0);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260127075814_AddPlcServiceStatusAndLogs', N'8.0.23');
GO

COMMIT;
GO

