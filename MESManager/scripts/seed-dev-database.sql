-- Script per popolare database di sviluppo locale
USE MESManager_Dev;

-- Inserisci macchine con IP localhost (per simulazione in locale)  
INSERT INTO Macchine (Id, Codice, Nome, Stato, AttivaInGantt, OrdineVisualizazione, IndirizzoPLC)
VALUES
  (NEWID(), 'M002', 'Macchina 02', 0, 1, 2, '127.0.0.1:102'),
  (NEWID(), 'M003', 'Macchina 03', 0, 1, 3, '127.0.0.1:102'),
  (NEWID(), 'M005', 'Macchina 05', 0, 1, 5, '127.0.0.1:102'),
  (NEWID(), 'M006', 'Macchina 06', 0, 1, 6, '127.0.0.1:102'),
  (NEWID(), 'M007', 'Macchina 07', 0, 1, 7, '127.0.0.1:102'),
  (NEWID(), 'M008', 'Macchina 08', 0, 1, 8, '127.0.0.1:102'),
  (NEWID(), 'M009', 'Macchina 09', 0, 1, 9, '127.0.0.1:102'),
  (NEWID(), 'M010', 'Macchina 10', 0, 1, 10, '127.0.0.1:102');

-- Inserisci articoli di test
DECLARE @ArticoloId1 UNIQUEIDENTIFIER = NEWID();
DECLARE @ArticoloId2 UNIQUEIDENTIFIER = NEWID();
DECLARE @ArticoloId3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Articoli (Id, Codice, Descrizione, Prezzo, Attivo, UltimaModifica, TimestampSync, TempoCiclo, NumeroFigure)
VALUES
  (@ArticoloId1, 'ART001', 'Articolo Test 1', 10.50, 1, GETDATE(), GETDATE(), 120, 4),
  (@ArticoloId2, 'ART002', 'Articolo Test 2', 15.00, 1, GETDATE(), GETDATE(), 180, 6),
  (@ArticoloId3, 'ART003', 'Articolo Test 3 (no ricetta)', 20.00, 1, GETDATE(), GETDATE(), 150, 5);

-- Inserisci ricetta per ART001 (con parametri)
DECLARE @RicettaId1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Ricette (Id, ArticoloId) VALUES (@RicettaId1, @ArticoloId1);

INSERT INTO ParametriRicetta (Id, RicettaId, NomeParametro, Valore, UnitaMisura)
VALUES
  (NEWID(), @RicettaId1, 'TempoNastro', '50', 'sec'),
  (NEWID(), @RicettaId1, 'TempoSparo', '30', 'sec'),
  (NEWID(), @RicettaId1, 'FrequenzaInvertitore', '45', 'Hz');

-- Inserisci ricetta per ART002 (senza parametri dettagliati - solo record)
INSERT INTO Ricette (Id, ArticoloId) VALUES (NEWID(), @ArticoloId2);

-- ART003 non ha ricetta (per testare warning ⚠️)

-- Inserisci commesse di test
DECLARE @CommessaId1 UNIQUEIDENTIFIER = NEWID();
DECLARE @CommessaId2 UNIQUEIDENTIFIER = NEWID();
DECLARE @CommessaId3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Commesse (Id, Codice, ArticoloId, QuantitaRichiesta, Stato, StatoProgramma, NumeroMacchina, OrdineSequenza, DataInizioPrevisione, DataFinePrevisione, UltimaModifica, TimestampSync)
VALUES
  (@CommessaId1, 'COM001', @ArticoloId1, 100, 1, 1, 'M002', 1, DATEADD(HOUR, 1, GETDATE()), DATEADD(HOUR, 5, GETDATE()), GETDATE(), GETDATE()),
  (@CommessaId2, 'COM002', @ArticoloId2, 150, 1, 1, 'M005', 1, DATEADD(HOUR, 2, GETDATE()), DATEADD(HOUR, 6, GETDATE()), GETDATE(), GETDATE()),
  (@CommessaId3, 'COM003', @ArticoloId3, 200, 1, 1, 'M007', 1, DATEADD(HOUR, 3, GETDATE()), DATEADD(HOUR, 7, GETDATE()), GETDATE(), GETDATE());

PRINT 'Database di sviluppo popolato con successo!';
PRINT '- 8 Macchine simulate (IP: 127.0.0.1)';
PRINT '- 3 Articoli (2 con ricetta, 1 senza)';
PRINT '- 3 Commesse programmate';
PRINT 'Test ✅ vs ⚠️ in colonna HasRicetta!';
