-- ============================================================================
-- FIX COMPLETO: PLCRealtime + Verifica Dashboard
-- ============================================================================
-- PROBLEMA: Dashboard scollegate perché PLCRealtime vuota o incompleta
-- SOLUZIONE: Inserire record di test in PLCRealtime per tutte le macchine
-- ⚠️ Solo per ambiente DEV! In PROD i dati vengono da PlcSync service
-- ============================================================================

SET NOCOUNT ON;

PRINT '=== STEP 1: Analisi PLCRealtime ===' 
PRINT ''

-- Verifica conteggi
DECLARE @MacchineConIP INT
DECLARE @PLCRealtimeCount INT

SELECT @MacchineConIP = COUNT(*) FROM Macchine WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != ''
SELECT @PLCRealtimeCount = COUNT(*) FROM PLCRealtime

PRINT 'Macchine con IndirizzoPLC: ' + CAST(@MacchineConIP AS VARCHAR(10))
PRINT 'Record in PLCRealtime: ' + CAST(@PLCRealtimeCount AS VARCHAR(10))

IF @PLCRealtimeCount = 0
BEGIN
    PRINT ''
    PRINT '⚠️⚠️⚠️ PROBLEMA TROVATO! ⚠️⚠️⚠️'
    PRINT 'PLCRealtime è VUOTA - le dashboard risulteranno vuote!'
    PRINT ''
END
ELSE IF @PLCRealtimeCount < @MacchineConIP
BEGIN
    PRINT ''
    PRINT '⚠️ ATTENZIONE: Mancano ' + CAST(@MacchineConIP - @PLCRealtimeCount AS VARCHAR(10)) + ' record in PLCRealtime'
    PRINT ''
END

-- Mostra dettaglio JOIN
PRINT 'Dettaglio Macchine vs PLCRealtime:'
PRINT ''

SELECT 
    m.Codice,
    m.Nome,
    m.IndirizzoPLC,
    CASE 
        WHEN p.Id IS NULL THEN '❌ MANCA IN PLCRealtime'
        WHEN DATEDIFF(MINUTE, p.DataUltimoAggiornamento, GETDATE()) > 2 THEN '⚠️ DATI VECCHI (>2 min)'
        ELSE '✅ OK'
    END AS Stato,
    p.CicliFatti,
    p.DataUltimoAggiornamento
FROM Macchine m
LEFT JOIN PLCRealtime p ON p.MacchinaId = m.Id
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != ''
ORDER BY m.Codice

PRINT ''
PRINT '=== STEP 2: Fix PLCRealtime ===' 
PRINT ''

BEGIN TRANSACTION

-- Elimina record vecchi (se esistono)
DELETE FROM PLCRealtime WHERE MacchinaId IN (
    SELECT Id FROM Macchine WHERE IndirizzoPLC IS NOT NULL
)

PRINT 'Record vecchi eliminati: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

-- Inserisci record di test per TUTTE le macchine con IP
INSERT INTO PLCRealtime (
    Id,
    MacchinaId,
    DataUltimoAggiornamento,
    CicliFatti,
    QuantitaDaProdurre,
    CicliScarti,
    BarcodeLavorazione,
    OperatoreId,
    NumeroOperatore,
    TempoMedioRilevato,
    TempoMedio,
    Figure,
    StatoMacchina,
    QuantitaRaggiunta
)
SELECT 
    NEWID(),
    m.Id,
    GETDATE(),                      -- DataUltimoAggiornamento: NOW (IMPORTANTE per visibility!)
    0,                              -- CicliFatti: 0 (TEST)
    0,                              -- QuantitaDaProdurre: 0
    0,                              -- CicliScarti: 0
    0,                              -- BarcodeLavorazione: 0
    NULL,                           -- OperatoreId: NULL (nullable)
    0,                              -- NumeroOperatore: 0 (NOT NULL)
    0,                              -- TempoMedioRilevato: 0 (NOT NULL)
    0,                              -- TempoMedio: 0 (NOT NULL)
    0,                              -- Figure: 0 (NOT NULL)
    'FERMO',                        -- StatoMacchina: FERMO (test)
    0                               -- QuantitaRaggiunta: 0
FROM Macchine m
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != ''
  AND NOT EXISTS (
      SELECT 1 FROM PLCRealtime p WHERE p.MacchinaId = m.Id
  )

PRINT 'Record PLCRealtime inseriti: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

COMMIT TRANSACTION

PRINT ''
PRINT '=== STEP 3: Verifica POST-FIX ===' 
PRINT ''

-- Verifica finale
SELECT 
    m.Codice,
    m.Nome,
    m.IndirizzoPLC,
    p.CicliFatti,
    p.StatoMacchina,
    p.DataUltimoAggiornamento,
    DATEDIFF(SECOND, p.DataUltimoAggiornamento, GETDATE()) AS SecondiDaAggiornamento,
    CASE 
        WHEN DATEDIFF(MINUTE, p.DataUltimoAggiornamento, GETDATE()) <= 2 THEN '✅ VISIBILE'
        ELSE '❌ TIMEOUT'
    END AS VisibilitaDashboard
FROM Macchine m
INNER JOIN PLCRealtime p ON p.MacchinaId = m.Id
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != ''
ORDER BY m.Codice

PRINT ''

SELECT 
    'PLCRealtime totali' AS Descrizione,
    COUNT(*) AS Conteggio
FROM PLCRealtime

PRINT ''
PRINT '=== FIX COMPLETATO ==='
PRINT '✅ Tutte le macchine ora dovrebbero essere visibili nelle dashboard!'
PRINT ''
PRINT '⚠️ NOTA AMBIENTE DEV:'
PRINT '   - Questi sono dati di TEST (StatoMacchina=FERMO, CicliFatti=0)'
PRINT '   - In PROD i dati vengono aggiornati da PlcSync service'
PRINT '   - Se PlcSync è attivo in DEV, sovrascriverà questi dati'
PRINT ''
