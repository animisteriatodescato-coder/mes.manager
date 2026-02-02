# Test sincronizzazione stati commesse

Write-Host "Avvio Worker per sincronizzazione..." -ForegroundColor Yellow

# Avvia il worker in background
$job = Start-Job -ScriptBlock {
    Set-Location "c:\Dev\MESManager"
    dotnet run --project MESManager.Worker\MESManager.Worker.csproj 2>&1
}

# Aspetta 30 secondi per la sincronizzazione
Write-Host "Attesa 30 secondi per completamento sincronizzazione..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Ferma il worker
Stop-Job -Job $job
Remove-Job -Job $job

# Server di produzione
$server = "192.168.1.230\SQLEXPRESS01"
$database = "MESManager_Prod"
$user = "FAB"
$password = "password.123"

# Verifica i log
Write-Host "`n=== LOG SINCRONIZZAZIONE ===" -ForegroundColor Cyan
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT TOP 1 Modulo, CONVERT(VARCHAR(20), DataOra, 120) AS DataOra, Nuovi, Aggiornati, Ignorati, Errori, MessaggioErrore FROM LogSync WHERE Modulo = 'Commesse' ORDER BY DataOra DESC" -W -s "|"

# Verifica distribuzione stati
Write-Host "`n=== DISTRIBUZIONE STATI ===" -ForegroundColor Cyan
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT Stato, COUNT(*) AS NumeroCommesse FROM Commesse GROUP BY Stato ORDER BY Stato" -W -s "|"

# Verifica alcune commesse specifiche
Write-Host "`n=== CAMPIONE COMMESSE APERTE (Stato 1) ===" -ForegroundColor Cyan
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT TOP 5 Codice, Stato FROM Commesse WHERE Stato = 1" -W -s "|"

Write-Host "`n=== CAMPIONE COMMESSE CHIUSE (Stato 4) ===" -ForegroundColor Cyan
sqlcmd -S $server -d $database -U $user -P $password -C -Q "SELECT TOP 5 Codice, Stato FROM Commesse WHERE Stato = 4" -W -s "|"

Write-Host "`nSincronizzazione completata!" -ForegroundColor Green
