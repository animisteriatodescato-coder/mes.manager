-- Inserisce le configurazioni PLC per le macchine
-- Basato sui file JSON in MESManager.PlcSync/Configuration/machines/

DELETE FROM ConfigurazioniPLC;

-- M002
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000002', '192.168.17.26', 0, 1, 55, 0, 200, 1);

-- M003
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000003', '192.168.17.24', 0, 1, 55, 0, 200, 1);

-- M005
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000005', '192.168.17.27', 0, 1, 55, 0, 200, 1);

-- M006
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000006', '192.168.17.25', 0, 1, 55, 0, 200, 1);

-- M007
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000007', '192.168.17.23', 0, 1, 55, 0, 200, 1);

-- M008
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000008', '192.168.17.21', 0, 1, 55, 0, 200, 1);

-- M009
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000009', '192.168.17.20', 0, 1, 55, 0, 200, 1);

-- M010
INSERT INTO ConfigurazioniPLC (Id, MacchinaId, PlcIp, Rack, Slot, DbNumber, DbStart, DbLength, Enabled)
VALUES (NEWID(), '11111111-1111-1111-1111-000000000010', '192.168.17.22', 0, 1, 55, 0, 200, 1);

-- Visualizza le configurazioni inserite
SELECT M.Codice, M.Nome, C.PlcIp, C.Enabled
FROM ConfigurazioniPLC C
INNER JOIN Macchine M ON C.MacchinaId = M.Id
ORDER BY M.Codice;
