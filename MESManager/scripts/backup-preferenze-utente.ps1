# =========================================================================
# BACKUP PREFERENZE UTENTE - Pre-Deploy
# =========================================================================
# Salva tutte le preferenze utente dal database di produzione
# in un file JSON prima di un deploy che potrebbe invalidare le configurazioni
#
# Uso: .\backup-preferenze-utente.ps1
# =========================================================================

$ErrorActionPreference = "Stop"

$backupDir = "C:\Dev\MESManager\scripts\backup-preferenze"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$backupDir\preferenze_$timestamp.json"

# Crea directory backup se non esiste
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "BACKUP PREFERENZE UTENTE - Database Produzione" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Query per estrarre tutte le preferenze utente
$query = @"
SELECT 
    pu.Id,
    pu.UtenteId,
    u.Username,
    pu.ChiavePreferenza AS [Key],
    pu.ValoreJson AS [Value],
    pu.DataCreazione,
    pu.DataUltimaModifica
FROM PreferenzeUtente pu
INNER JOIN Utenti u ON u.Id = pu.UtenteId
ORDER BY u.Username, pu.ChiavePreferenza
FOR JSON PATH;
"@

try {
    Write-Host "[1/3] Connessione a 192.168.1.230\SQLEXPRESS01..." -ForegroundColor Yellow
    
    # Esegui query e salva in JSON
    $result = sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
        -U "FAB" -P "password.123" -C `
        -Q $query -h -1 -w 8000
    
    if ($LASTEXITCODE -ne 0) {
        throw "Errore sqlcmd (exit code: $LASTEXITCODE)"
    }
    
    # Rimuovi eventuali spazi/newline extra
    $jsonData = ($result -join "").Trim()
    
    Write-Host "[2/3] Salvataggio in: $backupFile" -ForegroundColor Yellow
    
    # Verifica che sia JSON valido
    if ($jsonData -match '^\[.*\]$') {
        $jsonData | Out-File -FilePath $backupFile -Encoding UTF8
        
        # Conta record salvati
        $parsed = $jsonData | ConvertFrom-Json
        $recordCount = $parsed.Count
        
        Write-Host "[3/3] ✅ Backup completato!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Record salvati: $recordCount" -ForegroundColor Cyan
        Write-Host "File: $backupFile" -ForegroundColor Cyan
        Write-Host "Dimensione: $((Get-Item $backupFile).Length / 1KB) KB" -ForegroundColor Cyan
        
        # Mostra riepilogo per utente
        Write-Host ""
        Write-Host "Preferenze per utente:" -ForegroundColor White
        $parsed | Group-Object Username | ForEach-Object {
            Write-Host "  - $($_.Name): $($_.Count) preferenze" -ForegroundColor Gray
        }
        
    } else {
        throw "Risultato query non è JSON valido"
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ ERRORE durante backup:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Backup salvato. Usa restore-preferenze-utente.ps1 per ripristinare" -ForegroundColor White
Write-Host "================================================" -ForegroundColor Cyan
