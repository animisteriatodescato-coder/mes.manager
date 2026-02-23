# ======================================================================
# SCRIPT DEPLOY PRODUZIONE v1.47.0 → v1.49.0
# Server: 192.168.1.230
# Data: 23 Febbraio 2026
# ======================================================================

param(
    [switch]$SkipBackup = $false
)

$ErrorActionPreference = "Stop"

# Config
$serverName = "192.168.1.230"
$deployRoot = "\\$serverName\C$\MESManager"
$publishWeb = "C:\Dev\MESManager\publish\Web"
$publishWorker = "C:\Dev\MESManager\publish\Worker"
$publishPlcSync = "C:\Dev\MESManager\publish\PlcSync"

# File protetti (NON sovrascrivere MAI)
$protectedFiles = @(
    "appsettings.Secrets.json",
    "appsettings.Database.json"
)

Write-Host "`n══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   🚀 DEPLOY PRODUZIONE v1.49.0" -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Gray

# ======================================================================
# STEP 1: Verifica connessione server
# ======================================================================
Write-Host "📡 STEP 1: Verifica connessione server..." -ForegroundColor Cyan
if (!(Test-Path $deployRoot)) {
    Write-Host "❌ ERRORE: Server $serverName non raggiungibile!" -ForegroundColor Red
    Write-Host "   Verifica:" -ForegroundColor Yellow
    Write-Host "   - Connessione di rete" -ForegroundColor Gray
    Write-Host "   - Path: $deployRoot" -ForegroundColor Gray
    exit 1
}
Write-Host "✅ Server raggiungibile" -ForegroundColor Green
Write-Host "" -ForegroundColor Gray

# ======================================================================
# STEP 2: Backup (opzionale)
# ======================================================================
if (!$SkipBackup) {
    Write-Host "💾 STEP 2: Creazione backup..." -ForegroundColor Cyan
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "C:\Dev\MESManager\backups\prod_v147_$timestamp"
    
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    
    Write-Host "  Backup: MESManager.Web.dll..." -ForegroundColor White
    Copy-Item "$deployRoot\MESManager.Web.dll" -Destination "$backupPath\" -ErrorAction SilentlyContinue
    
    Write-Host "  Backup: wwwroot..." -ForegroundColor White
    Copy-Item "$deployRoot\wwwroot" -Destination "$backupPath\wwwroot" -Recurse -ErrorAction SilentlyContinue
    
    Write-Host "✅ Backup creato in: $backupPath" -ForegroundColor Green
    Write-Host "" -ForegroundColor Gray
} else {
    Write-Host "⏭️  STEP 2: Backup saltato (flag -SkipBackup)" -ForegroundColor Yellow
    Write-Host "" -ForegroundColor Gray
}

# ======================================================================
# STEP 3: Ferma servizi (MANUALE - richiede RDP)
# ======================================================================
Write-Host "⚠️  STEP 3: FERMARE SERVIZI SUL SERVER" -ForegroundColor Yellow
Write-Host "" -ForegroundColor Gray
Write-Host "   AZIONE MANUALE RICHIESTA:" -ForegroundColor Red
Write-Host "   1. Connettiti via RDP a $serverName" -ForegroundColor White
Write-Host "   2. Ferma i 3 servizi:" -ForegroundColor White
Write-Host "      - MESManager.Web.exe" -ForegroundColor Gray
Write-Host "      - MESManager.Worker.exe" -ForegroundColor Gray
Write-Host "      - MESManager.PlcSync.exe" -ForegroundColor Gray
Write-Host "   3. Verifica processi fermati (Task Manager)" -ForegroundColor White
Write-Host "" -ForegroundColor Gray

$continue = Read-Host "Servizi fermati? (Y/N)"
if ($continue -ne "Y" -and $continue -ne "y") {
    Write-Host "❌ Deploy annullato dall'utente" -ForegroundColor Red
    exit 1
}

# ======================================================================
# STEP 4: Deploy file
# ======================================================================
Write-Host "`n📦 STEP 4: Deploy file..." -ForegroundColor Cyan

# Deploy Web
Write-Host "  Copiando MESManager.Web..." -ForegroundColor White
Get-ChildItem $publishWeb | ForEach-Object {
    $fileName = $_.Name
    $destPath = Join-Path $deployRoot $fileName
    
    # Verifica file protetti
    if ($protectedFiles -contains $fileName) {
        Write-Host "    ⏭️  Saltato (protetto): $fileName" -ForegroundColor Yellow
    } else {
        Copy-Item $_.FullName -Destination $destPath -Recurse -Force
        # Write-Host "    ✓ $fileName" -ForegroundColor Green
    }
}
Write-Host "  ✅ Web deployato" -ForegroundColor Green

# Deploy Worker
Write-Host "  Copiando MESManager.Worker..." -ForegroundColor White
$workerDest = Join-Path $deployRoot "Worker"
if (!(Test-Path $workerDest)) {
    New-Item -ItemType Directory -Path $workerDest -Force | Out-Null
}
Get-ChildItem $publishWorker | ForEach-Object {
    $fileName = $_.Name
    if ($protectedFiles -contains $fileName) {
        Write-Host "    ⏭️  Saltato (protetto): $fileName" -ForegroundColor Yellow
    } else {
        Copy-Item $_.FullName -Destination (Join-Path $workerDest $fileName) -Recurse -Force
    }
}
Write-Host "  ✅ Worker deployato" -ForegroundColor Green

# Deploy PlcSync
Write-Host "  Copiando MESManager.PlcSync..." -ForegroundColor White
$plcDest = Join-Path $deployRoot "PlcSync"
if (!(Test-Path $plcDest)) {
    New-Item -ItemType Directory -Path $plcDest -Force | Out-Null
}
Get-ChildItem $publishPlcSync | ForEach-Object {
    $fileName = $_.Name
    if ($protectedFiles -contains $fileName) {
        Write-Host "    ⏭️  Saltato (protetto): $fileName" -ForegroundColor Yellow
    } else {
        Copy-Item $_.FullName -Destination (Join-Path $plcDest $fileName) -Recurse -Force
    }
}
Write-Host "  ✅ PlcSync deployato" -ForegroundColor Green
Write-Host "" -ForegroundColor Gray

# ======================================================================
# STEP 5: Riavvia servizi (MANUALE)
# ======================================================================
Write-Host "🔄 STEP 5: RIAVVIARE SERVIZI SUL SERVER" -ForegroundColor Yellow
Write-Host "" -ForegroundColor Gray
Write-Host "   AZIONE MANUALE RICHIESTA:" -ForegroundColor Red
Write-Host "   1. Sul server $serverName" -ForegroundColor White
Write-Host "   2. Avvia i 3 servizi:" -ForegroundColor White
Write-Host "      - C:\MESManager\MESManager.Web.exe" -ForegroundColor Gray
Write-Host "      - C:\MESManager\Worker\MESManager.Worker.exe" -ForegroundColor Gray
Write-Host "      - C:\MESManager\PlcSync\MESManager.PlcSync.exe" -ForegroundColor Gray
Write-Host "   3. Verifica processi avviati (Task Manager)" -ForegroundColor White
Write-Host "" -ForegroundColor Gray

# ======================================================================
# STEP 6: Verifica
# ======================================================================
Write-Host "✅ STEP 6: Verifica deploy" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Gray
Write-Host "   URL Produzione: http://192.168.1.230:5156" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Gray
Write-Host "   Controlli:" -ForegroundColor Yellow
Write-Host "   1. Versione visualizzata: v1.49.0" -ForegroundColor White
Write-Host "   2. Pagina Catalogo Commesse: clienti corretti (fonderie)" -ForegroundColor White
Write-Host "   3. Carica su Gantt: dialog selezione macchina" -ForegroundColor White
Write-Host "   4. Log server: nessun errore" -ForegroundColor White
Write-Host "" -ForegroundColor Gray

Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   ✅ DEPLOY COMPLETATO!" -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Gray
Write-Host "Modifiche deployate:" -ForegroundColor Yellow
Write-Host "  • v1.48.0 - Fix centralizzazione cliente" -ForegroundColor Green
Write-Host "  • v1.49.0 - Selezione macchina manuale Gantt" -ForegroundColor Green
Write-Host "" -ForegroundColor Gray
