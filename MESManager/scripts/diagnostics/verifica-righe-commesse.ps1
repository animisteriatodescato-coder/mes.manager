# Verifica commesse con più righe e campo Delivered
# Server di produzione
$server = "192.168.1.230\SQLEXPRESS01"
$database = "MESManager_Prod"
$user = "FAB"
$password = "password.123"

Write-Host "=== COMMESSE CON PIU' RIGHE ===" -ForegroundColor Cyan
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT Codice, COUNT(*) AS NumeroRighe FROM Commesse GROUP BY Codice HAVING COUNT(*) > 1 ORDER BY NumeroRighe DESC" -W -s "|"

Write-Host "`n=== TOTALE COMMESSE PER NUMERO DI RIGHE ===" -ForegroundColor Yellow
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT NumeroRighe, COUNT(*) AS NumeroCommesse FROM (SELECT Codice, COUNT(*) AS NumeroRighe FROM Commesse GROUP BY Codice) AS T GROUP BY NumeroRighe ORDER BY NumeroRighe" -W -s "|"

Write-Host "`n=== ESEMPIO COMMESSA CON PIU' RIGHE ===" -ForegroundColor Green
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT TOP 10 Codice, Stato, ArticoloId FROM Commesse WHERE Codice IN (SELECT TOP 1 Codice FROM Commesse GROUP BY Codice HAVING COUNT(*) > 1) ORDER BY Codice" -W -s "|"

Write-Host "`n=== VERIFICA SE Delivered È MEMORIZZATO ===" -ForegroundColor Magenta
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Commesse' ORDER BY ORDINAL_POSITION" -W -s "|"
