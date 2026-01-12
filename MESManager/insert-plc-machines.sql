-- Elimina completamente le macchine di test e inserisce quelle reali da PlcMultiMachine
-- Prima elimina i dati correlati, poi le macchine, infine inserisce quelle nuove

-- 1. Elimina dati PLC correlati
DELETE FROM EventiPLC;
DELETE FROM PLCStorico;
DELETE FROM PLCRealtime;

-- 2. Elimina tutte le macchine esistenti
DELETE FROM Macchine;

-- 3. Inserisci le macchine reali con GUID predefiniti
-- Stato: 0=Sconosciuto, 1=Ferma, 2=InFunzione, 3=Manutenzione, 4=Allarme
INSERT INTO Macchine (Id, Codice, Nome, Stato) VALUES 
('11111111-1111-1111-1111-000000000002', 'M002', 'Tornio CNC 2', 0),
('11111111-1111-1111-1111-000000000003', 'M003', 'Fresatrice 3', 0),
('11111111-1111-1111-1111-000000000005', 'M005', 'Saldatrice 5', 0),
('11111111-1111-1111-1111-000000000006', 'M006', 'Piegatrice 6', 0),
('11111111-1111-1111-1111-000000000007', 'M007', 'Taglio Laser 7', 0),
('11111111-1111-1111-1111-000000000008', 'M008', 'Pressa Piegatrice 8', 0),
('11111111-1111-1111-1111-000000000009', 'M009', 'Centro Lavoro 9', 0),
('11111111-1111-1111-1111-000000000010', 'M010', 'Tornio Automatico 10', 0);

-- Visualizza tutte le macchine inserite
SELECT Id, Codice, Nome, Stato FROM Macchine ORDER BY Codice;

