-- Script per aggiornare forzatamente gli stati delle commesse in MESManager
-- basandosi sui dati effettivi del database Mago

-- Aggiorna gli stati delle commesse
UPDATE c
SET c.Stato = CASE 
    WHEN so.Invoiced = 1 THEN 3  -- Chiusa (Fatturata)
    WHEN so.Delivered = 1 THEN 2  -- Completata (Consegnata)
    ELSE 0  -- Aperta
END,
c.TimestampSync = GETDATE()
FROM [MESManager].dbo.Commesse c
INNER JOIN [TODESCATO_NET].dbo.MA_SaleOrd so ON so.InternalOrdNo = c.Codice
WHERE c.Stato != CASE 
    WHEN so.Invoiced = 1 THEN 3
    WHEN so.Delivered = 1 THEN 2
    ELSE 0
END;

-- Mostra il risultato
SELECT 
    'Aggiornamento completato' AS Messaggio,
    @@ROWCOUNT AS CommesseAggiornate;

-- Verifica gli stati dopo l'aggiornamento
SELECT 
    COUNT(*) AS TotaleCommesse,
    SUM(CASE WHEN Stato = 0 THEN 1 ELSE 0 END) AS Aperte,
    SUM(CASE WHEN Stato = 1 THEN 1 ELSE 0 END) AS InLavorazione,
    SUM(CASE WHEN Stato = 2 THEN 1 ELSE 0 END) AS Completate,
    SUM(CASE WHEN Stato = 3 THEN 1 ELSE 0 END) AS Chiuse
FROM [MESManager].dbo.Commesse;
