# Verifica distribuzione stati considerando tutti i fattori

Write-Host "=== VERIFICA TOTALE COMMESSE IN MESMANAGER ===" -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT COUNT(DISTINCT Codice) AS TotaleCommesse FROM Commesse" -W

Write-Host "`n=== DISTRIBUZIONE STATI ATTUALI ===" -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT Stato, COUNT(*) AS Numero FROM Commesse GROUP BY Stato ORDER BY Stato" -W -s "|"

Write-Host "`n=== COMMESSE PER ANNO (ultime modifiche) ===" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT YEAR(UltimaModifica) AS Anno, COUNT(*) AS Numero FROM Commesse GROUP BY YEAR(UltimaModifica) ORDER BY Anno DESC" -W -s "|"

Write-Host "`n=== COMMESSE RECENTI (ultimi 12 mesi) ===" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT Stato, COUNT(*) AS Numero FROM Commesse WHERE UltimaModifica >= DATEADD(MONTH, -12, GETDATE()) GROUP BY Stato ORDER BY Stato" -W -s "|"
