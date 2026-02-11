-- BACKUP della situazione attuale (per sicurezza)  
SELECT CAST(GETDATE() AS VARCHAR) AS 'BackupTimestamp', * 
INTO #BackupPreferenze
FROM PreferenzeUtente
WHERE Chiave LIKE '%grid%' AND ValoreJson LIKE '%ClienteRagioneSociale%';

-- UPDATE: Sostituisci ClienteRagioneSociale con CompanyName
UPDATE PreferenzeUtente
SET ValoreJson = REPLACE(REPLACE(REPLACE(REPLACE(
    ValoreJson,
    '\"ClienteRagioneSociale\"', '\"CompanyName\"'),
    '\"clienteRagioneSociale\"', '\"companyName\"'),
    '''ClienteRagioneSociale''', '''CompanyName'''),
    '''clienteRagioneSociale''', '''companyName''')
WHERE Chiave LIKE '%grid%'
  AND ValoreJson LIKE '%ClienteRagioneSociale%';

-- Mostra cosa è stato modificato
SELECT 
    u.Nome,
    pu.Chiave,
    'MIGRATO' AS Stato,
    LEN(pu.ValoreJson) AS Size
FROM PreferenzeUtente pu
INNER JOIN UtentiApp u ON u.Id = pu.UtenteAppId
WHERE pu.Id IN (SELECT Id FROM #BackupPreferenze);

-- Cleanup temp table
DROP TABLE #BackupPreferenze;
