-- Script per verificare e aggiornare gli stati delle commesse dal database Mago
-- Questo script mostra lo stato attuale in MESManager e lo stato in Mago

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
    CASE 
        WHEN so.Invoiced = 1 THEN 'Chiusa (Fatturata)'
        WHEN so.Delivered = 1 THEN 'Completata (Consegnata)'
        ELSE 'Aperta'
    END AS StatoMago,
    so.Delivered,
    so.Invoiced,
    cli.RagioneSociale AS Cliente
FROM [MESManager].dbo.Commesse c
LEFT JOIN [MESManager].dbo.Clienti cli ON c.ClienteId = cli.Id
LEFT JOIN [TODESCATO_NET].dbo.MA_SaleOrd so ON so.InternalOrdNo = c.Codice
ORDER BY c.TimestampSync DESC;

-- Mostra statistiche
SELECT 
    'MESManager' AS Database,
    COUNT(*) AS TotaleCommesse,
    SUM(CASE WHEN c.Stato = 0 THEN 1 ELSE 0 END) AS Aperte,
    SUM(CASE WHEN c.Stato = 1 THEN 1 ELSE 0 END) AS InLavorazione,
    SUM(CASE WHEN c.Stato = 2 THEN 1 ELSE 0 END) AS Completate,
    SUM(CASE WHEN c.Stato = 3 THEN 1 ELSE 0 END) AS Chiuse
FROM [MESManager].dbo.Commesse c;

-- Mostra statistiche Mago
SELECT 
    'Mago' AS Database,
    COUNT(*) AS TotaleCommesse,
    SUM(CASE WHEN so.Delivered = 0 AND so.Invoiced = 0 THEN 1 ELSE 0 END) AS Aperte,
    SUM(CASE WHEN so.Delivered = 1 AND so.Invoiced = 0 THEN 1 ELSE 0 END) AS Consegnate,
    SUM(CASE WHEN so.Invoiced = 1 THEN 1 ELSE 0 END) AS Fatturate
FROM [TODESCATO_NET].dbo.MA_SaleOrd so;
