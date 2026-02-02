-- ============================================================================
-- MIGRAZIONE DATABASE: Gantt -> MESManager_Prod
-- ============================================================================
-- Questo script copia le tabelle dal database Gantt a MESManager_Prod
-- Eseguire su SQL Server: 192.168.1.230\SQLEXPRESS01
-- Utente: FAB
-- ============================================================================

USE MESManager_Prod;
GO

-- ============================================================================
-- 1. TABELLA ArticoliRicetta (Parametri PLC per ricette)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ArticoliRicetta')
BEGIN
    CREATE TABLE [dbo].[ArticoliRicetta] (
        [IdRigaRicetta] INT IDENTITY(1,1) PRIMARY KEY,
        [CodiceArticolo] NVARCHAR(50) NOT NULL,
        [CodiceParametro] INT NULL,
        [DescrizioneParametro] NVARCHAR(255) NULL,
        [Indirizzo] INT NULL,
        [Area] NVARCHAR(50) NULL,
        [Tipo] NVARCHAR(50) NULL,
        [UM] NVARCHAR(20) NULL,
        [Valore] INT NULL
    );
    
    CREATE NONCLUSTERED INDEX IX_ArticoliRicetta_CodiceArticolo 
        ON [dbo].[ArticoliRicetta]([CodiceArticolo]);
    
    PRINT 'Tabella ArticoliRicetta creata';
END
ELSE
BEGIN
    PRINT 'Tabella ArticoliRicetta già esistente';
END
GO

-- Copia dati da Gantt (se la tabella è vuota)
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[ArticoliRicetta])
BEGIN
    SET IDENTITY_INSERT [dbo].[ArticoliRicetta] ON;
    
    INSERT INTO [dbo].[ArticoliRicetta] 
        ([IdRigaRicetta], [CodiceArticolo], [CodiceParametro], [DescrizioneParametro], 
         [Indirizzo], [Area], [Tipo], [UM], [Valore])
    SELECT 
        [IdRigaRicetta], [CodiceArticolo], [CodiceParametro], [DescrizioneParametro], 
        [Indirizzo], [Area], [Tipo], [UM], [Valore]
    FROM [Gantt].[dbo].[ArticoliRicetta];
    
    SET IDENTITY_INSERT [dbo].[ArticoliRicetta] OFF;
    
    PRINT 'Dati ArticoliRicetta copiati da Gantt';
END
GO

-- ============================================================================
-- 2. TABELLA Allegati (Foto e documenti)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AllegatiGantt')
BEGIN
    CREATE TABLE [dbo].[AllegatiGantt] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Archivio] NVARCHAR(50) NOT NULL,
        [IdArchivio] INT NOT NULL,
        [Allegato] NVARCHAR(500) NOT NULL,  -- Path completo del file
        [DescrizioneAllegato] NVARCHAR(255) NULL,
        [Priorita] INT NULL DEFAULT 0
    );
    
    CREATE NONCLUSTERED INDEX IX_AllegatiGantt_Archivio_IdArchivio 
        ON [dbo].[AllegatiGantt]([Archivio], [IdArchivio]);
    
    PRINT 'Tabella AllegatiGantt creata';
END
ELSE
BEGIN
    PRINT 'Tabella AllegatiGantt già esistente';
END
GO

-- Copia dati da Gantt (se la tabella è vuota)
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[AllegatiGantt])
BEGIN
    SET IDENTITY_INSERT [dbo].[AllegatiGantt] ON;
    
    INSERT INTO [dbo].[AllegatiGantt] 
        ([Id], [Archivio], [IdArchivio], [Allegato], [DescrizioneAllegato], [Priorita])
    SELECT 
        [Id], [Archivio], [IdArchivio], [Allegato], [DescrizioneAllegato], [Priorita]
    FROM [Gantt].[dbo].[Allegati];
    
    SET IDENTITY_INSERT [dbo].[AllegatiGantt] OFF;
    
    PRINT 'Dati AllegatiGantt copiati da Gantt';
END
GO

-- ============================================================================
-- 3. TABELLA tbArticoli (Descrizioni articoli - solo lettura)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tbArticoliGantt')
BEGIN
    CREATE TABLE [dbo].[tbArticoliGantt] (
        [CodiceArticolo] NVARCHAR(50) PRIMARY KEY,
        [DescrizioneArticolo] NVARCHAR(500) NULL
    );
    
    PRINT 'Tabella tbArticoliGantt creata';
END
ELSE
BEGIN
    PRINT 'Tabella tbArticoliGantt già esistente';
END
GO

-- Copia dati da Gantt (se la tabella è vuota)
IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[tbArticoliGantt])
BEGIN
    INSERT INTO [dbo].[tbArticoliGantt] 
        ([CodiceArticolo], [DescrizioneArticolo])
    SELECT 
        [CodiceArticolo], [DescrizioneArticolo]
    FROM [Gantt].[dbo].[tbArticoli];
    
    PRINT 'Dati tbArticoliGantt copiati da Gantt';
END
GO

-- ============================================================================
-- VERIFICA MIGRAZIONE
-- ============================================================================
PRINT '=== VERIFICA MIGRAZIONE ===';
SELECT 'ArticoliRicetta' AS Tabella, COUNT(*) AS Righe FROM [dbo].[ArticoliRicetta]
UNION ALL
SELECT 'AllegatiGantt', COUNT(*) FROM [dbo].[AllegatiGantt]
UNION ALL
SELECT 'tbArticoliGantt', COUNT(*) FROM [dbo].[tbArticoliGantt];
GO

PRINT 'Migrazione completata!';
PRINT 'Ora puoi aggiornare appsettings.Database.json rimuovendo GanttDb';
