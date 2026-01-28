-- Script per copiare dati da MESManager a MESManager_Dev
USE MESManager_Dev;
GO

-- Copia Clienti
INSERT INTO MESManager_Dev.dbo.Clienti
SELECT * FROM MESManager.dbo.Clienti;
GO

-- Copia Operatori  
INSERT INTO MESManager_Dev.dbo.Operatori
SELECT * FROM MESManager.dbo.Operatori;
GO

-- Copia Macchine
INSERT INTO MESManager_Dev.dbo.Macchine
SELECT * FROM MESManager.dbo.Macchine;
GO

-- Copia Commesse
INSERT INTO MESManager_Dev.dbo.Commesse
SELECT * FROM MESManager.dbo.Commesse;
GO

-- Copia Articoli
INSERT INTO MESManager_Dev.dbo.Articoli
SELECT * FROM MESManager.dbo.Articoli;
GO

-- Copia Anime  
INSERT INTO MESManager_Dev.dbo.Anime
SELECT * FROM MESManager.dbo.Anime;
GO

-- Copia PLCRealtime
INSERT INTO MESManager_Dev.dbo.PLCRealtime
SELECT * FROM MESManager.dbo.PLCRealtime;
GO

-- Verifica dati copiati
SELECT 'Clienti' AS Tabella, COUNT(*) AS Record FROM Clienti
UNION ALL
SELECT 'Operatori', COUNT(*) FROM Operatori
UNION ALL
SELECT 'Macchine', COUNT(*) FROM Macchine
UNION ALL
SELECT 'Commesse', COUNT(*) FROM Commesse
UNION ALL
SELECT 'Articoli', COUNT(*) FROM Articoli
UNION ALL
SELECT 'Anime', COUNT(*) FROM Anime
UNION ALL
SELECT 'PLCRealtime', COUNT(*) FROM PLCRealtime;
GO
