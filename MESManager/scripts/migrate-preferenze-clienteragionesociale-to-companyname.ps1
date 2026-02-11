# =========================================================================
# MIGRAZIONE PREFERENZE: ClienteRagioneSociale → CompanyName
# =========================================================================
# Aggiorna i JSON delle preferenze grid salvate sostituendo il vecchio campo
# "ClienteRagioneSociale" con il nuovo "CompanyName"
#
# IMPORTANTE: Questo script MODIFICA direttamente il database di produzione!
# Fare backup prima di eseguire.
#
# Uso: .\migrate-preferenze-clienteragionesociale-to-companyname.ps1
# =========================================================================

param(
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,  # Se specificato, mostra cosa farebbe SENZA modificare DB
    
    [Parameter(Mandatory=$false)]
    [switch]$BackupFirst  # Esegue backup automatico prima della migrazione
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "MIGRAZIONE PREFERENZE: ClienteRagioneSociale → CompanyName" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

if ($WhatIf) {
    Write-Host "⚠️  MODALITÀ WHAT-IF: Nessuna modifica verrà applicata al database" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Backup automatico se richiesto
if ($BackupFirst -and -not $WhatIf) {
    Write-Host "[BACKUP] Esecuzione backup preferenze prima della migrazione..." -ForegroundColor Yellow
    
    $backupScript = Join-Path $PSScriptRoot "backup-preferenze-utente.ps1"
    if (Test-Path $backupScript) {
        & $backupScript
        Write-Host ""
        Write-Host "✅ Backup completato. Procedendo con migrazione..." -ForegroundColor Green
        Write-Host ""
        Start-Sleep -Seconds 2
    } else {
        Write-Host "⚠️  Script backup non trovato: $backupScript" -ForegroundColor Yellow
        Write-Host "   Continuare senza backup? (S/N): " -NoNewline
        $confirm = Read-Host
        if ($confirm -ne 'S') {
            Write-Host "Migrazione annullata." -ForegroundColor Red
            exit 0
        }
    }
}

# Step 2: Carica preferenze dal database
Write-Host "[1/4] Lettura preferenze dal database produzione..." -ForegroundColor Yellow

$query = @"
SELECT 
    pu.Id,
    pu.UtenteAppId,
    u.Nome AS Username,
    pu.Chiave AS ChiavePreferenza,
    pu.ValoreJson,
    pu.DataCreazione
FROM PreferenzeUtente pu
INNER JOIN UtentiApp u ON u.Id = pu.UtenteAppId
WHERE pu.Chiave LIKE '%grid%'
  AND pu.ValoreJson LIKE '%ClienteRagioneSociale%'
ORDER BY u.Nome, pu.Chiave
FOR JSON PATH;
"@

try {
    $result = sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
        -U "FAB" -P "password.123" -C `
        -Q $query -h -1 -w 8000
    
    if ($LASTEXITCODE -ne 0) {
        throw "Errore sqlcmd (exit code: $LASTEXITCODE)"
    }
    
    $jsonData = ($result -join "").Trim()
    
    if ($jsonData -notmatch '^\[.*\]$') {
        Write-Host "❌ Nessuna preferenza trovata con 'ClienteRagioneSociale'" -ForegroundColor Yellow
        Write-Host "   Possibili motivi:" -ForegroundColor Gray
        Write-Host "   - Preferenze già migrate" -ForegroundColor Gray
        Write-Host "   - Utenti non hanno mai salvato stati grid" -ForegroundColor Gray
        exit 0
    }
    
    $preferenze = $jsonData | ConvertFrom-Json
    
    Write-Host "   Trovate $($preferenze.Count) preferenze da migrare" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host "❌ ERRORE durante lettura database:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Step 3: Analizza e mostra preview modifiche
Write-Host "[2/4] Analisi modifiche..." -ForegroundColor Yellow
Write-Host ""

$migrazioni = @()

foreach ($pref in $preferenze) {
    $oldJson = $pref.ValoreJson
    
    # Sostituisci tutte le occorrenze di "ClienteRagioneSociale" con "CompanyName"
    # Gestisce sia field names che column IDs
    $newJson = $oldJson -replace '"ClienteRagioneSociale"', '"CompanyName"'
    $newJson = $newJson -replace '"clienteRagioneSociale"', '"companyName"'
    $newJson = $newJson -replace "'ClienteRagioneSociale'", "'CompanyName'"
    $newJson = $newJson -replace "'clienteRagioneSociale'", "'companyName'"
    
    # Conta sostituzioni
    $occurrences = ([regex]::Matches($oldJson, 'ClienteRagioneSociale|clienteRagioneSociale')).Count
    
    if ($newJson -ne $oldJson) {
        $migrazioni += [PSCustomObject]@{
            Id = $pref.Id
            UtenteAppId = $pref.UtenteAppId
            Username = $pref.Username
            Chiave = $pref.ChiavePreferenza
            OldJson = $oldJson
            NewJson = $newJson
            Occurrences = $occurrences
        }
        
        Write-Host "  ✓ $($pref.Username) - $($pref.ChiavePreferenza)" -ForegroundColor Cyan
        Write-Host "    Sostituzioni: $occurrences occorrenze di 'ClienteRagioneSociale'" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Totale migrazioni da applicare: $($migrazioni.Count)" -ForegroundColor Cyan
Write-Host ""

if ($migrazioni.Count -eq 0) {
    Write-Host "✅ Nessuna migrazione necessaria!" -ForegroundColor Green
    exit 0
}

# Step 4: Mostra esempio prima/dopo
if ($migrazioni.Count -gt 0) {
    Write-Host "[3/4] Esempio trasformazione (prima migrazione):" -ForegroundColor Yellow
    Write-Host ""
    
    $esempio = $migrazioni[0]
    
    # Mostra solo una porzione del JSON per leggibilità
    $oldSnippet = $esempio.OldJson.Substring(0, [Math]::Min(200, $esempio.OldJson.Length))
    $newSnippet = $esempio.NewJson.Substring(0, [Math]::Min(200, $esempio.NewJson.Length))
    
    Write-Host "PRIMA:" -ForegroundColor Red
    Write-Host $oldSnippet -ForegroundColor Gray
    if ($esempio.OldJson.Length -gt 200) { Write-Host "..." -ForegroundColor Gray }
    Write-Host ""
    
    Write-Host "DOPO:" -ForegroundColor Green
    Write-Host $newSnippet -ForegroundColor Gray
    if ($esempio.NewJson.Length -gt 200) { Write-Host "..." -ForegroundColor Gray }
    Write-Host ""
}

# Step 5: Conferma (se non WhatIf)
if (-not $WhatIf) {
    Write-Host "⚠️  ATTENZIONE: Questa operazione modificherà $($migrazioni.Count) record nel database di PRODUZIONE!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Procedere con la migrazione? (S/N): " -NoNewline
    $confirm = Read-Host
    
    if ($confirm -ne 'S') {
        Write-Host ""
        Write-Host "Migrazione annullata dall'utente." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
}

# Step 6: Applica migrazioni
Write-Host "[4/4] Applicazione migrazioni..." -ForegroundColor Yellow
Write-Host ""

$migrated = 0
$errors = 0

foreach ($mig in $migrazioni) {
    try {
        if ($WhatIf) {
            Write-Host "  [WHAT-IF] $($mig.Username) - $($mig.Chiave)" -ForegroundColor Cyan
            $migrated++
        } else {
            # Escape singoli apici per SQL
            $jsonEscaped = $mig.NewJson -replace "'", "''"
            
            $updateQuery = @"
UPDATE PreferenzeUtente
SET ValoreJson = '$jsonEscaped'
WHERE Id = '$($mig.Id)'
"@
            
            $result = sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
                -U "FAB" -P "password.123" -C `
                -Q $updateQuery -h -1 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ $($mig.Username) - $($mig.Chiave)" -ForegroundColor Green
                $migrated++
            } else {
                Write-Host "  ✗ $($mig.Username) - $($mig.Chiave) (errore SQL)" -ForegroundColor Red
                $errors++
            }
        }
        
    } catch {
        Write-Host "  ✗ $($mig.Username) - $($mig.Chiave) (eccezione: $($_.Exception.Message))" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "✅ WHAT-IF completato!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Riepilogo simulazione:" -ForegroundColor Cyan
    Write-Host "  - Preferenze che SAREBBERO migrate: $migrated" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Per applicare realmente le modifiche, esegui senza -WhatIf:" -ForegroundColor White
    Write-Host "  .\migrate-preferenze-clienteragionesociale-to-companyname.ps1" -ForegroundColor Gray
} else {
    Write-Host "✅ Migrazione completata!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Riepilogo:" -ForegroundColor Cyan
    Write-Host "  - Migrate con successo: $migrated" -ForegroundColor Green
    if ($errors -gt 0) {
        Write-Host "  - Errori: $errors" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "📢 IMPORTANTE: Comunicare agli utenti di:" -ForegroundColor Yellow
    Write-Host "   1. Ricaricare il browser (Ctrl+Shift+R)" -ForegroundColor White
    Write-Host "   2. Le colonne dovrebbero tornare come prima del deploy!" -ForegroundColor White
}

Write-Host "========================================================================" -ForegroundColor Cyan
