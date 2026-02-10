-- =====================================================
-- SCRIPT MANUALE PRODUZIONE
-- Migration: AddRobustPlanningFeatures
-- Data: 2026-02-04
-- Database: MESManager_Prod su 192.168.1.230\SQLEXPRESS01
-- =====================================================
-- ATTENZIONE: Eseguire in una finestra di manutenzione
-- Backup obbligatorio prima dell'esecuzione!
-- =====================================================

USE MESManager_Prod;
GO

BEGIN TRANSACTION;

-- 1. Optimistic Concurrency Control
ALTER TABLE Commesse ADD RowVersion ROWVERSION NOT NULL;
GO

-- 2. Scheduling Constraints
ALTER TABLE Commesse ADD Priorita INT NOT NULL DEFAULT 100;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Priorità scheduling: più basso = più urgente. Default 100',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'Priorita';
GO

ALTER TABLE Commesse ADD Bloccata BIT NOT NULL DEFAULT 0;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Se true, la commessa non viene spostata dai ricalcoli automatici',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'Bloccata';
GO

ALTER TABLE Commesse ADD VincoloDataInizio DATETIME2 NULL;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'La commessa non può iniziare prima di questa data',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'VincoloDataInizio';
GO

ALTER TABLE Commesse ADD VincoloDataFine DATETIME2 NULL;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'La commessa deve finire entro questa data (warning se impossibile)',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'VincoloDataFine';
GO

-- 3. Dynamic Setup Time
ALTER TABLE Commesse ADD SetupStimatoMinuti INT NULL;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Override setup time per questa commessa. Se null, usa default da ImpostazioniProduzione',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'SetupStimatoMinuti';
GO

-- 4. Classe Lavorazione
ALTER TABLE Commesse ADD ClasseLavorazione NVARCHAR(50) NULL;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Classe di lavorazione per riduzione setup se consecutiva',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Commesse',
    @level2type = N'Column', @level2name = 'ClasseLavorazione';
GO

ALTER TABLE Articoli ADD ClasseLavorazione NVARCHAR(50) NULL;
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Classe di lavorazione default per articolo',
    @level0type = N'Schema', @level0name = 'dbo',
    @level1type = N'Table',  @level1name = 'Articoli',
    @level2type = N'Column', @level2name = 'ClasseLavorazione';
GO

-- 5. Indici per performance e integrità

-- Indice composito per query frequenti (macchina + ordine)
CREATE NONCLUSTERED INDEX IX_Commesse_NumeroMacchina_OrdineSequenza
ON Commesse (NumeroMacchina, OrdineSequenza)
INCLUDE (DataInizioPrevisione, DataFinePrevisione, Bloccata, Priorita);
GO

-- Indice per filtrare commesse bloccate/priorità
CREATE NONCLUSTERED INDEX IX_Commesse_NumeroMacchina_Bloccata_Priorita
ON Commesse (NumeroMacchina, Bloccata, Priorita)
WHERE NumeroMacchina IS NOT NULL;
GO

-- Indice per vincoli temporali
CREATE NONCLUSTERED INDEX IX_Commesse_VincoloDataInizio_VincoloDataFine
ON Commesse (VincoloDataInizio, VincoloDataFine)
WHERE VincoloDataInizio IS NOT NULL OR VincoloDataFine IS NOT NULL;
GO

-- 6. Verifica integrità dati
PRINT 'Verifica commesse con OrdineSequenza duplicato sulla stessa macchina...';

SELECT NumeroMacchina, OrdineSequenza, COUNT(*) AS Conteggio
FROM Commesse
WHERE NumeroMacchina IS NOT NULL
GROUP BY NumeroMacchina, OrdineSequenza
HAVING COUNT(*) > 1;

-- Se ci sono duplicati, vanno risolti manualmente prima di procedere
-- Esempio di fix (eseguire solo se necessario):
-- UPDATE Commesse SET OrdineSequenza = ROW_NUMBER() OVER (PARTITION BY NumeroMacchina ORDER BY DataInizioPrevisione)
-- WHERE NumeroMacchina IS NOT NULL;

COMMIT TRANSACTION;
GO

PRINT 'Migration AddRobustPlanningFeatures completata con successo!';
PRINT 'Verificare che:';
PRINT '  1. Tutti gli indici siano stati creati';
PRINT '  2. Non ci siano duplicati OrdineSequenza';
PRINT '  3. Tutte le colonne abbiano i valori default corretti';
GO
