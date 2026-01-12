-- Inserisce dati di test in PLCRealtime per le 8 macchine
DELETE FROM PLCRealtime;

INSERT INTO PLCRealtime (Id, MacchinaId, CicliFatti, QuantitaDaProdurre, CicliScarti, BarcodeLavorazione, 
                         TempoMedioRilevato, TempoMedio, Figure, StatoMacchina, QuantitaRaggiunta, DataUltimoAggiornamento)
VALUES 
-- M002 - Tornio CNC 2 (In Produzione)
(NEWID(), '11111111-1111-1111-1111-000000000002', 150, 200, 5, 20240001, 
 45.5, 50.0, 8, 2, 0, GETDATE()),

-- M003 - Fresatrice 3 (Completato)
(NEWID(), '11111111-1111-1111-1111-000000000003', 100, 100, 2, 20240002, 
 38.2, 40.0, 12, 2, 1, GETDATE()),

-- M005 - Saldatrice 5 (Allarme)
(NEWID(), '11111111-1111-1111-1111-000000000005', 75, 150, 10, 20240003, 
 60.0, 55.0, 6, 4, 0, GETDATE()),

-- M006 - Piegatrice 6 (In Setup)
(NEWID(), '11111111-1111-1111-1111-000000000006', 0, 80, 0, 20240004, 
 0.0, 42.0, 10, 1, 0, GETDATE()),

-- M007 - Taglio Laser 7 (In Produzione)
(NEWID(), '11111111-1111-1111-1111-000000000007', 200, 250, 8, 20240005, 
 25.5, 28.0, 15, 2, 0, GETDATE()),

-- M008 - Pressa Piegatrice 8 (Ferma)
(NEWID(), '11111111-1111-1111-1111-000000000008', 50, 120, 3, 20240006, 
 52.0, 50.0, 9, 1, 0, GETDATE()),

-- M009 - Centro Lavoro 9 (In Produzione)
(NEWID(), '11111111-1111-1111-1111-000000000009', 180, 200, 4, 20240007, 
 65.3, 70.0, 11, 2, 0, GETDATE()),

-- M010 - Tornio Automatico 10 (Completato)
(NEWID(), '11111111-1111-1111-1111-000000000010', 300, 300, 12, 20240008, 
 32.8, 35.0, 14, 2, 1, GETDATE());

SELECT 'Inserite ' + CAST(COUNT(*) AS VARCHAR) + ' righe di test in PLCRealtime' AS Risultato
FROM PLCRealtime;

-- Mostra dati inseriti
SELECT m.Codice AS Macchina, m.Nome, p.CicliFatti, p.QuantitaDaProdurre, p.CicliScarti, 
       CAST((p.CicliFatti * 100.0 / NULLIF(p.QuantitaDaProdurre,0)) AS DECIMAL(5,1)) AS [% Completamento],
       p.BarcodeLavorazione, p.StatoMacchina
FROM PLCRealtime p
INNER JOIN Macchine m ON p.MacchinaId = m.Id
ORDER BY m.Codice;
