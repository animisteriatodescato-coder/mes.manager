-- Script per verificare gli stati delle commesse confrontandoli con Mago
-- Da eseguire sul database locale MESManager (localhost\SQLEXPRESS)

-- Prima mostra il confronto
SELECT TOP 20
    c.Codice AS CodiceCommessa,
    CASE c.Stato 
        WHEN 0 THEN 'Aperta'
        WHEN 1 THEN 'InLavorazione'
        WHEN 2 THEN 'Completata'
        WHEN 3 THEN 'Chiusa'
        ELSE 'Sconosciuto'
    END AS StatoMESManager,
    cli.RagioneSociale AS Cliente,
    c.DataConsegna,
    c.TimestampSync
FROM Commesse c
LEFT JOIN Clienti cli ON c.ClienteId = cli.Id
ORDER BY c.TimestampSync DESC;

-- Mostra statistiche
SELECT 
    COUNT(*) AS TotaleCommesse,
    SUM(CASE WHEN Stato = 0 THEN 1 ELSE 0 END) AS Aperte,
    SUM(CASE WHEN Stato = 1 THEN 1 ELSE 0 END) AS InLavorazione,
    SUM(CASE WHEN Stato = 2 THEN 1 ELSE 0 END) AS Completate,
    SUM(CASE WHEN Stato = 3 THEN 1 ELSE 0 END) AS Chiuse,
    SUM(CASE WHEN Stato NOT IN (0,1,2,3) THEN 1 ELSE 0 END) AS Sconosciuto
FROM Commesse;

-- Mostra log ultima sincronizzazione
SELECT TOP 5
    Modulo,
    DataOra,
    Nuovi,
    Aggiornati,
    Ignorati,
    Errori,
    MessaggioErrore
FROM LogSync
WHERE Modulo = 'Commesse'
ORDER BY DataOra DESC;
