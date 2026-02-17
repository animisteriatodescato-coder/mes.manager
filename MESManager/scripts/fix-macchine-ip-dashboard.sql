-- ===========================================================================
-- FIX MACCHINE: Ripristino IndirizzoPLC per Dashboard
-- ===========================================================================
-- PROBLEMA: Quando abbiamo eliminato i duplicati Macchine, abbiamo tenuto
--           le righe con AttivaInGantt=true, ma probabilmente erano quelle
--           SENZA IndirizzoPLC. Quelle eliminate (AttivaInGantt=false) avevano
--           gli IP configurati.
-- 
-- SOLUZIONE: Assegnare indirizzi IP alle macchine esistenti basandoci sui
--            codici standard (M002-M010 corrispondo agli IP 192.168.17.2-10)
-- 
-- RIFERIMENTO: docs2/storico/docs/CHANGELOG.md#L381-383
--              "Dashboard mostrano solo macchine con IndirizzoPLC configurato"
-- ===========================================================================

SET NOCOUNT ON;

PRINT '=== ANALISI PRE-FIX ==='
PRINT ''

-- Mostra stato attuale
SELECT 
    'Macchine totali' AS Descrizione,
    COUNT(*) AS Conteggio
FROM Macchine

UNION ALL

SELECT 
    'Con IndirizzoPLC',
    COUNT(*)
FROM Macchine
WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != ''

UNION ALL

SELECT 
    'Senza IndirizzoPLC (invisibili in dashboard)',
    COUNT(*)
FROM Macchine
WHERE IndirizzoPLC IS NULL OR IndirizzoPLC = ''

PRINT ''
PRINT 'Dettaglio macchine attuali:'
SELECT 
    Codice,
    Nome,
    AttivaInGantt,
    IndirizzoPLC,
    CASE 
        WHEN IndirizzoPLC IS NULL OR IndirizzoPLC = '' THEN 'NON VISIBILE IN DASHBOARD'
        ELSE 'OK - Visibile'
    END AS StatoDashboard
FROM Macchine
ORDER BY Codice

PRINT ''
PRINT '=== APPLICAZIONE FIX ==='
PRINT ''

BEGIN TRANSACTION

-- Assegna IP standard basandosi sul codice macchina
-- Subnet PLC: 192.168.17.x (da documentazione)
-- M002 = 192.168.17.2, M003 = 192.168.17.3, etc.

UPDATE Macchine
SET IndirizzoPLC = CASE Codice
    WHEN 'M002' THEN '192.168.17.2'
    WHEN 'M003' THEN '192.168.17.3'
    WHEN 'M004' THEN '192.168.17.4'
    WHEN 'M005' THEN '192.168.17.5'
    WHEN 'M006' THEN '192.168.17.6'
    WHEN 'M007' THEN '192.168.17.7'
    WHEN 'M008' THEN '192.168.17.8'
    WHEN 'M009' THEN '192.168.17.9'
    WHEN 'M010' THEN '192.168.17.10'
    ELSE IndirizzoPLC  -- Mantieni IP esistente se non match
END
WHERE (IndirizzoPLC IS NULL OR IndirizzoPLC = '')
  AND Codice IN ('M002', 'M003', 'M004', 'M005', 'M006', 'M007', 'M008', 'M009', 'M010')

PRINT 'Macchine aggiornate con IndirizzoPLC: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

COMMIT TRANSACTION

PRINT ''
PRINT '=== VERIFICA POST-FIX ==='
PRINT ''

SELECT 
    Codice,
    Nome,
    AttivaInGantt,
    IndirizzoPLC,
    CASE 
        WHEN IndirizzoPLC IS NULL OR IndirizzoPLC = '' THEN '❌ NON VISIBILE'
        ELSE '✅ OK Dashboard'
    END AS StatoDashboard,
    OrdineVisualizazione
FROM Macchine
ORDER BY Codice

PRINT ''
SELECT 
    'Macchine con IP (visibili dashboard)' AS Descrizione,
    COUNT(*) AS Conteggio
FROM Macchine
WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != ''

PRINT ''
PRINT '=== FIX COMPLETATO ==='
PRINT '✅ Le macchine ora dovrebbero essere visibili nelle dashboard Produzione e PLC Realtime'
PRINT ''
