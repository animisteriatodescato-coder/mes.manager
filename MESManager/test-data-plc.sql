-- Script per inserire dati di test PLC Realtime
USE MESManager;
GO

-- Inserisci macchine di test se non esistono
IF NOT EXISTS (SELECT 1 FROM Macchine WHERE Codice = 'M001')
BEGIN
    INSERT INTO Macchine (Id, Codice, Nome, Stato)
    VALUES (NEWID(), 'M001', 'Pressa Idraulica 1', 0);
END

IF NOT EXISTS (SELECT 1 FROM Macchine WHERE Codice = 'M002')
BEGIN
    INSERT INTO Macchine (Id, Codice, Nome, Stato)
    VALUES (NEWID(), 'M002', 'Tornio CNC 2', 0);
END

IF NOT EXISTS (SELECT 1 FROM Macchine WHERE Codice = 'M003')
BEGIN
    INSERT INTO Macchine (Id, Codice, Nome, Stato)
    VALUES (NEWID(), 'M003', 'Fresatrice 3', 0);
END

-- Inserisci operatori di test
IF NOT EXISTS (SELECT 1 FROM Operatori WHERE NumeroOperatore = 101)
BEGIN
    INSERT INTO Operatori (Id, Nome, Cognome, NumeroOperatore)
    VALUES (NEWID(), 'Mario', 'Rossi', 101);
END

IF NOT EXISTS (SELECT 1 FROM Operatori WHERE NumeroOperatore = 102)
BEGIN
    INSERT INTO Operatori (Id, Nome, Cognome, NumeroOperatore)
    VALUES (NEWID(), 'Luigi', 'Bianchi', 102);
END

-- Inserisci dati realtime di test
DECLARE @Macchina1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Macchine WHERE Codice = 'M001');
DECLARE @Macchina2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Macchine WHERE Codice = 'M002');
DECLARE @Macchina3Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Macchine WHERE Codice = 'M003');
DECLARE @Operatore1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Operatori WHERE NumeroOperatore = 101);
DECLARE @Operatore2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Operatori WHERE NumeroOperatore = 102);

-- Cancella dati esistenti
DELETE FROM PLCRealtime WHERE MacchinaId IN (@Macchina1Id, @Macchina2Id, @Macchina3Id);

-- Macchina 1: AUTOMATICO - CICLO (in produzione)
INSERT INTO PLCRealtime (Id, MacchinaId, DataUltimoAggiornamento, CicliFatti, QuantitaDaProdurre, 
    CicliScarti, BarcodeLavorazione, OperatoreId, TempoMedioRilevato, TempoMedio, Figure, 
    StatoMacchina, QuantitaRaggiunta)
VALUES (NEWID(), @Macchina1Id, GETDATE(), 750, 1000, 12, 12345, @Operatore1Id, 
    45, 42, 8, 'AUTOMATICO - CICLO', 0);

-- Macchina 2: ALLARME
INSERT INTO PLCRealtime (Id, MacchinaId, DataUltimoAggiornamento, CicliFatti, QuantitaDaProdurre, 
    CicliScarti, BarcodeLavorazione, OperatoreId, TempoMedioRilevato, TempoMedio, Figure, 
    StatoMacchina, QuantitaRaggiunta)
VALUES (NEWID(), @Macchina2Id, GETDATE(), 320, 500, 8, 12346, @Operatore2Id, 
    38, 40, 6, 'ALLARME', 0);

-- Macchina 3: COMPLETATA
INSERT INTO PLCRealtime (Id, MacchinaId, DataUltimoAggiornamento, CicliFatti, QuantitaDaProdurre, 
    CicliScarti, BarcodeLavorazione, OperatoreId, TempoMedioRilevato, TempoMedio, Figure, 
    StatoMacchina, QuantitaRaggiunta)
VALUES (NEWID(), @Macchina3Id, GETDATE(), 200, 200, 3, 12347, @Operatore1Id, 
    52, 50, 4, 'NUMERO PEZZI RAGGIUNTI', 1);

SELECT 'Dati test inseriti con successo!' AS Risultato;

-- Verifica
SELECT M.Codice, M.Nome, P.CicliFatti, P.QuantitaDaProdurre, P.StatoMacchina, 
       O.Nome + ' ' + O.Cognome AS Operatore
FROM PLCRealtime P
INNER JOIN Macchine M ON P.MacchinaId = M.Id
LEFT JOIN Operatori O ON P.OperatoreId = O.Id;
