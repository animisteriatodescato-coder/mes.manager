# Script per avviare MESManager.Web in modo isolato
# Uso: .\start-web.ps1

$projectPath = "MESManager.Web\MESManager.Web.csproj"
$workingDir = $PSScriptRoot

Write-Host "=== Avvio MESManager Web ===" -ForegroundColor Cyan
Write-Host "Directory: $workingDir" -ForegroundColor Gray
Write-Host "Progetto: $projectPath" -ForegroundColor Gray
Write-Host ""

# Ferma eventuali istanze precedenti
$processes = Get-Process -Name dotnet -ErrorAction SilentlyContinue | 
    Where-Object { $_.Path -like "*MESManager*" }

if ($processes) {
    Write-Host "⚠ Trovate $($processes.Count) istanze in esecuzione. Arresto..." -ForegroundColor Yellow
    $processes | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Avvia in un nuovo processo separato
Write-Host "🚀 Avvio applicazione..." -ForegroundColor Green
Write-Host ""

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --project `"$projectPath`" --environment Development"
$psi.WorkingDirectory = $workingDir
$psi.UseShellExecute = $true  # Importante: processo separato
$psi.CreateNoWindow = $false   # Mostra la finestra

try {
    $process = [System.Diagnostics.Process]::Start($psi)
    
    Write-Host "✓ Applicazione avviata (PID: $($process.Id))" -ForegroundColor Green
    Write-Host ""
    Write-Host "Attendi 10 secondi che l'app sia completamente avviata..." -ForegroundColor Cyan
    Write-Host "Poi puoi accedere a: http://localhost:5156" -ForegroundColor White
    Write-Host ""
    Write-Host "Per testare le API: .\test-api.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Per fermare l'app, chiudi la finestra del terminale o:" -ForegroundColor Gray
    Write-Host "  Stop-Process -Id $($process.Id)" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Errore durante l'avvio: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
