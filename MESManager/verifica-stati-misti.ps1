# Verifica commesse con più righe e stati diversi

Write-Host "=== COMMESSE CON RIGHE IN STATI DIVERSI ===" -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q @"
SELECT Codice, COUNT(DISTINCT Stato) AS StatiDiversi, COUNT(*) AS TotaleRighe, 
       MIN(Stato) AS StatoMin, MAX(Stato) AS StatoMax
FROM Commesse 
GROUP BY Codice 
HAVING COUNT(DISTINCT Stato) > 1
ORDER BY TotaleRighe DESC
"@ -W -s "|"

Write-Host "`n=== ESEMPIO COMMESSA CON STATI MISTI ===" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q @"
SELECT TOP 1 @CodiceTest = Codice FROM Commesse GROUP BY Codice HAVING COUNT(DISTINCT Stato) > 1;
SELECT Codice, Stato, CONVERT(VARCHAR(20), TimestampSync, 120) AS UltimoSync 
FROM Commesse 
WHERE Codice = (SELECT TOP 1 Codice FROM Commesse GROUP BY Codice HAVING COUNT(DISTINCT Stato) > 1)
ORDER BY Codice
"@ -W -s "|"

Write-Host "`n=== DISTRIBUZIONE STATI PER COMMESSE UNICHE ===" -ForegroundColor Green
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q @"
WITH StatoCommessa AS (
    SELECT Codice, MAX(Stato) AS StatoMax
    FROM Commesse
    GROUP BY Codice
)
SELECT StatoMax AS Stato, COUNT(*) AS NumeroCommesse
FROM StatoCommessa
GROUP BY StatoMax
ORDER BY StatoMax
"@ -W -s "|"
