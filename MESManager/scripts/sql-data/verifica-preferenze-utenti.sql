-- =====================================================================
-- Script di verifica configurazione utenti e preferenze
-- =====================================================================

-- 1. Verifica utenti esistenti
PRINT '=== UTENTI CONFIGURATI ==='
SELECT 
    Nome,
    Colore,
    Attivo,
    Ordine,
    FORMAT(DataCreazione, 'dd/MM/yyyy HH:mm') AS DataCreazione
FROM UtentiApp
ORDER BY Ordine;

PRINT '';
PRINT '=== NUMERO PREFERENZE PER UTENTE ==='
SELECT 
    u.Nome AS Utente,
    u.Colore,
    COUNT(p.Id) AS NumeroPreferenze,
    MAX(p.UltimaModifica) AS UltimaModificaPreferenza
FROM UtentiApp u
LEFT JOIN PreferenzeUtente p ON u.Id = p.UtenteAppId
GROUP BY u.Nome, u.Colore
ORDER BY u.Ordine;

PRINT '';
PRINT '=== DETTAGLIO PREFERENZE (prime 5 per utente) ==='
SELECT TOP 20
    u.Nome AS Utente,
    p.Chiave,
    LEFT(p.ValoreJson, 50) + '...' AS ValoreJson_Preview,
    LEN(p.ValoreJson) AS Dimensione,
    FORMAT(p.UltimaModifica, 'dd/MM/yyyy HH:mm') AS UltimaModifica
FROM PreferenzeUtente p
INNER JOIN UtentiApp u ON p.UtenteAppId = u.Id
ORDER BY u.Ordine, p.Chiave;

PRINT '';
PRINT '=== CHIAVI PREFERENZE DISPONIBILI ==='
SELECT DISTINCT
    Chiave,
    COUNT(*) AS NumeroUtenti
FROM PreferenzeUtente
GROUP BY Chiave
ORDER BY Chiave;

PRINT '';
PRINT 'Verifica completata!';
