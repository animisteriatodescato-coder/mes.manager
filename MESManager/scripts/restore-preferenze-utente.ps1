# =========================================================================
# RESTORE PREFERENZE UTENTE - Post-Deploy
# =========================================================================
# Ripristina le preferenze utente dal file di backup
# ATTENZIONE: Salta le preferenze di grid se sono state modificate colonne
#
# Uso: .\restore-preferenze-utente.ps1 -BackupFile "backup-preferenze\preferenze_20260211_085500.json"
# =========================================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupFile,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipGridStates  # Salta gli stati delle grid (usa questo se hai cambiato le colonne)
)

$ErrorActionPreference = "Stop"

$backupDir = "C:\Dev\MESManager\scripts\backup-preferenze"

# Se BackupFile non specificato, prendi il più recente
if ([string]::IsNullOrEmpty($BackupFile)) {
    $latestBackup = Get-ChildItem -Path $backupDir -Filter "preferenze_*.json" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
    
    if ($null -eq $latestBackup) {
        Write-Host "❌ Nessun backup trovato in $backupDir" -ForegroundColor Red
        exit 1
    }
    
    $BackupFile = $latestBackup.FullName
    Write-Host "📁 Usando backup più recente: $($latestBackup.Name)" -ForegroundColor Cyan
}

if (-not (Test-Path $BackupFile)) {
    Write-Host "❌ File backup non trovato: $BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "RESTORE PREFERENZE UTENTE - Database Produzione" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Chiavi da SALTARE se SkipGridStates è attivo (stati colonne che potrebbero essere incompatibili)
$gridStateKeys = @(
    'commesse-aperte-grid-fixed-state',
    'commesse-aperte-grid-settings',
    'commesse-grid-fixed-state',
    'commesse-grid-settings',
    'programma-macchine-grid-fixed-state',
    'programma-macchine-grid-settings',
    'anime-grid-fixed-state',
    'anime-grid-settings',
    'articoli-grid-fixed-state',
    'articoli-grid-settings',
    'clienti-grid-fixed-state',
    'clienti-grid-settings'
)

try {
    Write-Host "[1/4] Lettura backup: $BackupFile" -ForegroundColor Yellow
    
    $jsonContent = Get-Content -Path $BackupFile -Raw
    $preferenze = $jsonContent | ConvertFrom-Json
    
    Write-Host "      Record totali nel backup: $($preferenze.Count)" -ForegroundColor Gray
    
    # Filtra se richiesto
    if ($SkipGridStates) {
        Write-Host ""
        Write-Host "⚠️  SKIP GRID STATES attivo - stati colonne NON verranno ripristinati" -ForegroundColor Yellow
        $preferenceDaRipristinare = $preferenze | Where-Object { $gridStateKeys -notcontains $_.Key }
        Write-Host "      Record da ripristinare: $($preferenceDaRipristinare.Count)" -ForegroundColor Gray
    } else {
        $preferenceDaRipristinare = $preferenze
    }
    
    Write-Host ""
    Write-Host "[2/4] Connessione a database produzione..." -ForegroundColor Yellow
    
    $restored = 0
    $skipped = 0
    $errors = 0
    
    Write-Host "[3/4] Ripristino preferenze..." -ForegroundColor Yellow
    
    foreach ($pref in $preferenceDaRipristinare) {
        try {
            $valueEscaped = $pref.Value -replace "'", "''"
            
            $query = @"
IF EXISTS (SELECT 1 FROM PreferenzeUtente WHERE UtenteId = '$($pref.UtenteId)' AND ChiavePreferenza = '$($pref.Key)')
BEGIN
    UPDATE PreferenzeUtente 
    SET ValoreJson = '$valueEscaped',
        DataUltimaModifica = GETDATE()
    WHERE UtenteId = '$($pref.UtenteId)' AND ChiavePreferenza = '$($pref.Key)'
END
ELSE
BEGIN
    INSERT INTO PreferenzeUtente (Id, UtenteId, ChiavePreferenza, ValoreJson, DataCreazione, DataUltimaModifica)
    VALUES (NEWID(), '$($pref.UtenteId)', '$($pref.Key)', '$valueEscaped', GETDATE(), GETDATE())
END
"@
            
            $result = sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
                -U "FAB" -P "password.123" -C `
                -Q $query -h -1 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                $restored++
                Write-Host "  ✓ $($pref.Username) - $($pref.Key)" -ForegroundColor Green
            } else {
                $errors++
                Write-Host "  ✗ $($pref.Username) - $($pref.Key) (errore sqlcmd)" -ForegroundColor Red
            }
            
        } catch {
            $errors++
            Write-Host "  ✗ $($pref.Username) - $($pref.Key) (eccezione: $($_.Exception.Message))" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "[4/4] ✅ Restore completato!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Riepilogo:" -ForegroundColor Cyan
    Write-Host "  - Ripristinate: $restored" -ForegroundColor Green
    if ($SkipGridStates) {
        $skipped = $preferenze.Count - $preferenceDaRipristinare.Count
        Write-Host "  - Saltate (grid states): $skipped" -ForegroundColor Yellow
    }
    if ($errors -gt 0) {
        Write-Host "  - Errori: $errors" -ForegroundColor Red
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ ERRORE durante restore:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
if ($SkipGridStates) {
    Write-Host "⚠️  Gli utenti dovranno riconfigurare le colonne delle grid!" -ForegroundColor Yellow
} else {
    Write-Host "Preferenze ripristinate. Aggiorna il browser (Ctrl+F5)." -ForegroundColor White
}
Write-Host "================================================" -ForegroundColor Cyan
