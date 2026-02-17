-- =====================================================
-- QUERY PER ESTRARRE DATI ALLEGATI DA MESManager_Prod
-- Esegui questa query in SSMS connesso a 192.168.1.230
-- =====================================================

-- 1. VERIFICA DATABASE E TABELLA
USE [MESManager_Prod];
GO

SELECT 
    DB_NAME() as DatabaseCorrente,
    OBJECT_NAME(OBJECT_ID('dbo.Allegati')) as TabellaEsiste;
GO

-- 2. STATISTICHE DATI
SELECT 
    COUNT(*) as TotaleRecord,
    COUNT(DISTINCT Archivio) as TipiArchivio,
    COUNT(DISTINCT IdArchivio) as ArticoliDistinti
FROM [dbo].[Allegati]
WHERE Archivio = 'ARTICO';
GO

-- 3. PRIMI 10 RECORD (ANTEPRIMA)
SELECT TOP 10
    Id,
    Archivio,
    IdArchivio,
    Allegato,
    DescrizioneAllegato,
    Priorita
FROM [dbo].[Allegati]
WHERE Archivio = 'ARTICO'
ORDER BY IdArchivio, Priorita;
GO

-- =====================================================
-- 4. EXPORT COMPLETO PER IMPORT IN DEV
-- =====================================================
-- Copia il risultato di questa query e salvalo come file .sql

DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql = @sql + 
    'INSERT INTO [dbo].[Allegati] ([Archivio], [IdArchivio], [Allegato], [DescrizioneAllegato], [Priorita]) VALUES (' +
    '''' + Archivio + ''', ' +
    CAST(IdArchivio AS VARCHAR(10)) + ', ' +
    '''' + REPLACE(Allegato, '''', '''''') + ''', ' +
    CASE 
        WHEN DescrizioneAllegato IS NULL THEN 'NULL'
        ELSE '''' + REPLACE(DescrizioneAllegato, '''', '''''') + ''''
    END + ', ' +
    CASE 
        WHEN Priorita IS NULL THEN 'NULL'
        ELSE CAST(Priorita AS VARCHAR(10))
    END + 
    ');' + CHAR(13) + CHAR(10)
FROM [dbo].[Allegati]
WHERE Archivio = 'ARTICO'
ORDER BY IdArchivio, Priorita;

-- Header del file SQL
PRINT '-- =====================================================';
PRINT '-- EXPORT ALLEGATI DA GANTT DB';
PRINT '-- Data export: ' + CONVERT(VARCHAR(20), GETDATE(), 120);
PRINT '-- =====================================================';
PRINT '';
PRINT '-- Pulisci dati test esistenti';
PRINT 'DELETE FROM [dbo].[Allegati] WHERE Archivio = ''ARTICO'';';
PRINT 'GO';
PRINT '';

-- Output degli INSERT
PRINT @sql;

-- Footer
PRINT '';
PRINT '-- Verifica import';
PRINT 'SELECT COUNT(*) as RecordImportati FROM [dbo].[Allegati] WHERE Archivio = ''ARTICO'';';
PRINT 'GO';
