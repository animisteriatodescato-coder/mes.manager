# Script per eseguire la migrazione e verificare il risultato
$ErrorActionPreference = "Stop"

$server = "192.168.1.230\SQLEXPRESS01"
$database = "MESManager_Prod"
$user = "FAB"
$password = "password.123"
$scriptPath = "C:\Dev\MESManager\scripts\migrations\migrate-gantt-to-mesmanager.sql"
$logFile = "C:\Dev\MESManager\migration-log.txt"

Write-Host "=== MIGRAZIONE DATABASE ===" -ForegroundColor Cyan
Write-Host "Server: $server"
Write-Host "Database: $database"
Write-Host ""

# Esegui lo script di migrazione
Write-Host "Esecuzione script di migrazione..." -ForegroundColor Yellow
$output = sqlcmd -S $server -U $user -P $password -d $database -i $scriptPath -C 2>&1
$output | Out-File $logFile

# Verifica le tabelle create
Write-Host ""
Write-Host "=== VERIFICA TABELLE ===" -ForegroundColor Cyan

$checkQuery = @"
SELECT 'ArticoliRicetta' AS Tabella, COUNT(*) AS Righe FROM dbo.ArticoliRicetta
UNION ALL
SELECT 'AllegatiGantt', COUNT(*) FROM dbo.AllegatiGantt  
UNION ALL
SELECT 'tbArticoliGantt', COUNT(*) FROM dbo.tbArticoliGantt
"@

$result = sqlcmd -S $server -U $user -P $password -d $database -Q $checkQuery -W -C 2>&1
Write-Host $result

Write-Host ""
Write-Host "Log completo salvato in: $logFile" -ForegroundColor Green
