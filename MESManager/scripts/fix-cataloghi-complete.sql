-- ===================================================================
-- FIX CATALOGHI - MESManager Database
-- Data: 2026-02-16
-- Descrizione: Rimuove duplicati Macchine e ripristina Operatori
-- ===================================================================

USE [MESManager_Dev]
GO

PRINT '=== INIZIO FIX CATALOGHI ==='
PRINT ''

-- ===================================================================
-- STEP 1: ANALISI SITUAZIONE ATTUALE
-- ===================================================================
PRINT '--- STEP 1: Analisi situazione attuale ---'

DECLARE @MacchineCount INT, @MacchineAttiveCount INT, @MacchineInattiveCount INT
DECLARE @OperatoriCount INT, @AnimeCount INT

SELECT @MacchineCount = COUNT(*) FROM Macchine
SELECT @MacchineAttiveCount = COUNT(*) FROM Macchine WHERE AttivaInGantt = 1
SELECT @MacchineInattiveCount = COUNT(*) FROM Macchine WHERE AttivaInGantt = 0
SELECT @OperatoriCount = COUNT(*) FROM Operatori
SELECT @AnimeCount = COUNT(*) FROM Anime

PRINT 'Macchine totali: ' + CAST(@MacchineCount AS VARCHAR(10))
PRINT 'Macchine attive: ' + CAST(@MacchineAttiveCount AS VARCHAR(10))
PRINT 'Macchine inattive: ' + CAST(@MacchineInattiveCount AS VARCHAR(10))
PRINT 'Operatori: ' + CAST(@OperatoriCount AS VARCHAR(10))
PRINT 'Anime: ' + CAST(@AnimeCount AS VARCHAR(10))
PRINT ''

-- ===================================================================
-- STEP 2: IDENTIFICAZIONE DUPLICATI MACCHINE
-- ===================================================================
PRINT '--- STEP 2: Identificazione duplicati Macchine ---'

SELECT Codice, Nome, COUNT(*) as Conteggio
INTO #DuplicatiMacchine
FROM Macchine
GROUP BY Codice, Nome
HAVING COUNT(*) > 1

IF EXISTS (SELECT 1 FROM #DuplicatiMacchine)
BEGIN
    PRINT 'Duplicati trovati:'
    SELECT Codice, Nome, Conteggio FROM #DuplicatiMacchine
END
ELSE
BEGIN
    PRINT 'Nessun duplicato trovato'
END
PRINT ''

-- ===================================================================
-- STEP 3: RIMOZIONE DUPLICATI (mantieni solo AttivaInGantt = 1)
-- ===================================================================
PRINT '--- STEP 3: Rimozione duplicati Macchine ---'

BEGIN TRANSACTION

-- Crea tabella temporanea con gli ID da mantenere (quelli con AttivaInGantt = 1)
SELECT MIN(M.Id) as IdDaMantenere, M.Codice, M.Nome
INTO #MacchineValide
FROM Macchine M
WHERE M.AttivaInGantt = 1
GROUP BY M.Codice, M.Nome

-- Identifica gli ID da eliminare (duplicati inattivi)
SELECT M.Id as IdDaEliminare
INTO #MacchineEliminate
FROM Macchine M
WHERE EXISTS (
    SELECT 1 FROM #MacchineValide MV 
    WHERE MV.Codice = M.Codice AND MV.Nome = M.Nome
)
AND M.Id NOT IN (SELECT IdDaMantenere FROM #MacchineValide)

-- Verifica quante macchine verranno eliminate
DECLARE @MacchineEliminate INT
SELECT @MacchineEliminate = COUNT(*) FROM #MacchineEliminate
PRINT 'Macchine da eliminare: ' + CAST(@MacchineEliminate AS VARCHAR(10))

IF @MacchineEliminate > 0
BEGIN
    -- Mostra quali macchine saranno eliminate
    PRINT 'Elenco macchine che saranno eliminate:'
    SELECT M.Id, M.Codice, M.Nome, M.AttivaInGantt
    FROM Macchine M
    INNER JOIN #MacchineEliminate ME ON M.Id = ME.IdDaEliminare
    
    -- Elimina i duplicati
    DELETE FROM Macchine WHERE Id IN (SELECT IdDaEliminare FROM #MacchineEliminate)
    PRINT 'Duplicati eliminati: ' + CAST(@@ROWCOUNT AS VARCHAR(10))
END

COMMIT TRANSACTION
PRINT ''

-- ===================================================================
-- STEP 4: RIPRISTINO OPERATORI (seed dati di test)
-- ===================================================================
PRINT '--- STEP 4: Ripristino Operatori ---'

BEGIN TRANSACTION

IF @OperatoriCount = 0
BEGIN
    PRINT 'Inserimento operatori di test...'
    
    INSERT INTO Operatori (Id, NumeroOperatore, Matricola, Nome, Cognome, Attivo, DataAssunzione)
    VALUES
        (NEWID(), 1, 'MAT001', 'Mario', 'Rossi', 1, '2020-01-01'),
        (NEWID(), 2, 'MAT002', 'Giuseppe', 'Verdi', 1, '2020-03-15'),
        (NEWID(), 3, 'MAT003', 'Luigi', 'Bianchi', 1, '2021-06-01'),
        (NEWID(), 4, 'MAT004', 'Paolo', 'Neri', 1, '2021-09-10'),
        (NEWID(), 5, 'MAT005', 'Andrea', 'Gialli', 1, '2022-01-20'),
        (NEWID(), 6, 'MAT006', 'Marco', 'Blu', 1, '2022-04-05'),
        (NEWID(), 7, 'MAT007', 'Luca', 'Viola', 1, '2022-07-15'),
        (NEWID(), 8, 'MAT008', 'Stefano', 'Arancio', 1, '2023-01-10'),
        (NEWID(), 9, 'MAT009', 'Fabio', 'Rosa', 1, '2023-05-20'),
        (NEWID(), 10, 'MAT010', 'Davide', 'Marrone', 1, '2023-09-01')
    
    PRINT 'Operatori inseriti: ' + CAST(@@ROWCOUNT AS VARCHAR(10))
END
ELSE
BEGIN
    PRINT 'Operatori già presenti, skip inserimento'
END

COMMIT TRANSACTION
PRINT ''

-- ===================================================================
-- STEP 5: VERIFICA FINALE
-- ===================================================================
PRINT '--- STEP 5: Verifica finale ---'

SELECT @MacchineCount = COUNT(*) FROM Macchine
SELECT @OperatoriCount = COUNT(*) FROM Operatori

PRINT 'Macchine totali (dopo fix): ' + CAST(@MacchineCount AS VARCHAR(10))
PRINT 'Operatori totali (dopo fix): ' + CAST(@OperatoriCount AS VARCHAR(10))
PRINT ''

-- Verifica duplicati residui
IF EXISTS (SELECT 1 FROM Macchine GROUP BY Codice, Nome HAVING COUNT(*) > 1)
BEGIN
    PRINT '[ATTENZIONE] Duplicati ancora presenti!'
    SELECT Codice, Nome, COUNT(*) as Conteggio
    FROM Macchine
    GROUP BY Codice, Nome
    HAVING COUNT(*) > 1
END
ELSE
BEGIN
    PRINT '[OK] Nessun duplicato residuo'
END

PRINT ''
PRINT '=== FIX COMPLETATO CON SUCCESSO ==='

-- Cleanup tabelle temporanee
DROP TABLE IF EXISTS #DuplicatiMacchine
DROP TABLE IF EXISTS #MacchineValide
DROP TABLE IF EXISTS #MacchineEliminate

GO
