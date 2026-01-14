# Script per testare la sincronizzazione delle commesse
$ErrorActionPreference = "Stop"

Write-Host "Resetting sync state..." -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "UPDATE SyncStates SET UltimaSyncRiuscita = '2020-01-01' WHERE Modulo = 'Commesse'" -b

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error resetting sync state" -ForegroundColor Red
    exit 1
}

Write-Host "`nCurrent commesse status in MESManager:" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT Stato, COUNT(*) AS Conteggio FROM Commesse GROUP BY Stato" -W -s "|"

Write-Host "`nExpected commesse status in Mago:" -ForegroundColor Yellow
sqlcmd -S "192.168.1.72\SQLEXPRESS" -d TODESCATO_NET -U Gantt -P Gantt2019 -C -Q "SELECT 'Aperte' AS Stato, COUNT(*) AS Conteggio FROM MA_SaleOrd WHERE Delivered = 0 AND Invoiced = 0 UNION ALL SELECT 'Consegnate', COUNT(*) FROM MA_SaleOrd WHERE Delivered = 1 AND Invoiced = 0 UNION ALL SELECT 'Fatturate', COUNT(*) FROM MA_SaleOrd WHERE Invoiced = 1" -W -s "|"

Write-Host "`nStarting Worker for sync..." -ForegroundColor Yellow
$workerJob = Start-Job -ScriptBlock {
    param($path)
    cd $path
    dotnet run --project MESManager.Worker\MESManager.Worker.csproj --no-build 2>&1
} -ArgumentList "C:\Dev\MESManager"

Write-Host "Waiting 45 seconds for sync to complete..." -ForegroundColor Yellow
Start-Sleep -Seconds 45

Stop-Job -Job $workerJob
Remove-Job -Job $workerJob

Write-Host "`nSync logs:" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT TOP 10 Modulo, CONVERT(VARCHAR(20), DataOra, 120) AS DataOra, Nuovi, Aggiornati, Ignorati, Errori, LEFT(MessaggioErrore, 100) AS Errore FROM LogSync ORDER BY DataOra DESC" -W -s "|"

Write-Host "`nCommesse status after sync:" -ForegroundColor Yellow
sqlcmd -S "localhost\SQLEXPRESS" -d MESManager -E -C -Q "SELECT Stato, COUNT(*) AS Conteggio FROM Commesse GROUP BY Stato" -W -s "|"

Write-Host "`nDone!" -ForegroundColor Green
