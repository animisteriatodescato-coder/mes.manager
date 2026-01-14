-- Query per verificare le commesse con i dati dei clienti e articoli
SELECT TOP 10
    c.Codice AS CodiceCommessa,
    c.ClienteId,
    cli.RagioneSociale AS NomeCliente,
    cli.Codice AS CodiceCliente,
    c.ArticoloId,
    art.Descrizione AS DescrizioneArticolo,
    art.Codice AS CodiceArticolo,
    c.DataConsegna,
    c.Stato
FROM Commesse c
LEFT JOIN Clienti cli ON c.ClienteId = cli.Id
LEFT JOIN Articoli art ON c.ArticoloId = art.Id
ORDER BY c.UltimaModifica DESC;

-- Statistiche
SELECT 
    COUNT(*) AS TotaleCommesse,
    COUNT(c.ClienteId) AS CommesseConCliente,
    COUNT(c.ArticoloId) AS CommesseConArticolo,
    COUNT(*) - COUNT(c.ClienteId) AS CommesseSenzaCliente,
    COUNT(*) - COUNT(c.ArticoloId) AS CommesseSenzaArticolo
FROM Commesse c;
