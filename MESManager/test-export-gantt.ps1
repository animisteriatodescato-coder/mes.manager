#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test Framework per Export Gantt su Programma
    
.DESCRIPTION
    Script di test automatizzato per verificare:
    1. Commesse in DB con DataInizioPrevisione
    2. Endpoint esporta-su-programma
    3. Cambio stato StatoProgramma
    4. Risultati visibili in ProgrammaMacchine
    
.CRITICO
    NON dichiarare "funziona" finché TUTTI i test non passano
    
.LEZIONI
    - OrderBy randomico su Guid = codici casuali
    - Test script = prova di funzionamento
    - Log visibili = strumento di debug essenziale
#>

param(
    [string]$ApiUrl = "http://localhost:5156",
    [string]$DbServer = "localhost\SQLEXPRESS01",
    [string]$DbName = "MESManager",
    [int]$TimeoutSeconds = 30
)

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 TEST FRAMEWORK - Export Gantt su Programma" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ==============================================================================
# FASE 1: Verifica stato Database
# ==============================================================================

Write-Host "`n📋 FASE 1: Verifica Commesse in Database" -ForegroundColor Yellow

$sqlQuery = @"
SELECT 
    COUNT(*) as TotaleCommesse,
    SUM(CASE WHEN NumeroMacchina IS NOT NULL THEN 1 ELSE 0 END) as ConMacchina,
    SUM(CASE WHEN DataInizioPrevisione IS NOT NULL THEN 1 ELSE 0 END) as ConDataInizio,
    SUM(CASE WHEN StatoProgramma = 0 THEN 1 ELSE 0 END) as NonProgrammate,
    SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) as Programmate
FROM Commesse
WHERE DataInizioPrevisione IS NOT NULL
"@

try {
    $dbConnection = "Data Source=$DbServer;Initial Catalog=$DbName;Integrated Security=true;"
    $sqlCmd = New-Object System.Data.SqlClient.SqlCommand
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($dbConnection)
    $sqlConnection.Open()
    
    $sqlCmd.CommandText = $sqlQuery
    $sqlCmd.Connection = $sqlConnection
    
    $reader = $sqlCmd.ExecuteReader()
    $reader.Read()
    
    $totale = $reader["TotaleCommesse"]
    $conMacchina = $reader["ConMacchina"]
    $conDataInizio = $reader["ConDataInizio"]
    $nonProgrammate = $reader["NonProgrammate"]
    $programmate = $reader["Programmate"]
    
    $reader.Close()
    $sqlConnection.Close()
    
    Write-Host "✓ Stato Database:" -ForegroundColor Green
    Write-Host "  • Totale Commesse: $totale"
    Write-Host "  • Con Macchina: $conMacchina"
    Write-Host "  • Con DataInizioPrevisione: $conDataInizio ⚠️ CRITICO - Deve essere > 0"
    Write-Host "  • Non Programmate: $nonProgrammate"
    Write-Host "  • Già Programmate: $programmate"
    
    if ($conDataInizio -eq 0) {
        Write-Host "❌ ERRORE: Nessuna commessa con DataInizioPrevisione!" -ForegroundColor Red
        Write-Host "   → Verifica se il fix di normalizzazione è stato applicato" -ForegroundColor Red
        exit 1
    }
    
    # Dettagli commesse
    Write-Host "`n📊 Dettagli Commesse (prime 10 con date):" -ForegroundColor Cyan
    
    $sqlQuery2 = @"
SELECT TOP 10
    Codice,
    NumeroMacchina,
    DataInizioPrevisione,
    DataFinePrevisione,
    StatoProgramma,
    Stato
FROM Commesse
WHERE DataInizioPrevisione IS NOT NULL
ORDER BY Codice
"@
    
    $sqlCmd2 = New-Object System.Data.SqlClient.SqlCommand
    $sqlCmd2.CommandText = $sqlQuery2
    $sqlCmd2.Connection = New-Object System.Data.SqlClient.SqlConnection($dbConnection)
    $sqlCmd2.Connection.Open()
    
    $reader2 = $sqlCmd2.ExecuteReader()
    
    $testResults = @()
    while ($reader2.Read()) {
        $codice = $reader2["Codice"]
        $macchina = $reader2["NumeroMacchina"]
        $dataInizio = $reader2["DataInizioPrevisione"]
        $dataFine = $reader2["DataFinePrevisione"]
        $statoProg = @("NonProgrammata", "Programmata")[$reader2["StatoProgramma"]]
        $stato = $reader2["Stato"]
        
        Write-Host "  $codice | M$macchina | $dataInizio → $dataFine | $statoProg"
        
        $testResults += @{
            Codice = $codice
            Macchina = $macchina
            DataInizio = $dataInizio
            Stato = $stato
            StatoProgramma = $statoProg
        }
    }
    
    $reader2.Close()
    $sqlCmd2.Connection.Close()
    
} catch {
    Write-Host "❌ Errore DB: $_" -ForegroundColor Red
    exit 1
}

# ==============================================================================
# FASE 2: Test API - Endpoint Disponibile
# ==============================================================================

Write-Host "`n📡 FASE 2: Verifica Endpoint API" -ForegroundColor Yellow

$apiTestUrl = "$ApiUrl/api/pianificazione/esporta-su-programma"
Write-Host "URL: $apiTestUrl" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest `
        -Uri $apiTestUrl `
        -Method POST `
        -ContentType "application/json" `
        -TimeoutSec $TimeoutSeconds `
        -ErrorAction Stop
    
    Write-Host "✓ Endpoint disponibile (HTTP $($response.StatusCode))" -ForegroundColor Green
    
    $responseBody = $response.Content | ConvertFrom-Json
    Write-Host "✓ Risposta:" -ForegroundColor Green
    Write-Host "  • success: $($responseBody.success)"
    Write-Host "  • message: $($responseBody.message)"
    
    if ($responseBody.success -eq $true) {
        Write-Host "`n✅ EXPORT AVVENUTO CON SUCCESSO" -ForegroundColor Green
        Write-Host "   Messaggio: $($responseBody.message)" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️ Export ha restituito success=false" -ForegroundColor Yellow
        Write-Host "   Messaggio: $($responseBody.message)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Errore API: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Verifica che l'app sia in esecuzione su $ApiUrl" -ForegroundColor Red
    exit 1
}

# ==============================================================================
# FASE 3: Verifica Risultati Post-Export
# ==============================================================================

Write-Host "`n🔍 FASE 3: Verifica Cambio Stato Post-Export" -ForegroundColor Yellow

Start-Sleep -Seconds 2 # Dai tempo al DB di sincronizzarsi

try {
    $sqlCmd3 = New-Object System.Data.SqlClient.SqlCommand
    $sqlQuery3 = @"
SELECT 
    COUNT(*) as TotaleCommesse,
    SUM(CASE WHEN StatoProgramma = 0 THEN 1 ELSE 0 END) as NonProgrammate,
    SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) as Programmate
FROM Commesse
WHERE DataInizioPrevisione IS NOT NULL
"@
    
    $sqlCmd3.CommandText = $sqlQuery3
    $sqlCmd3.Connection = New-Object System.Data.SqlClient.SqlConnection($dbConnection)
    $sqlCmd3.Connection.Open()
    
    $reader3 = $sqlCmd3.ExecuteReader()
    $reader3.Read()
    
    $nuoveTotale = $reader3["TotaleCommesse"]
    $nuoveNonProgrammate = $reader3["NonProgrammate"]
    $nuoveProgrammate = $reader3["Programmate"]
    
    Write-Host "✓ Stato Post-Export:" -ForegroundColor Green
    Write-Host "  • Totale: $nuoveTotale"
    Write-Host "  • Non Programmate: $nuoveNonProgrammate (Prima: $nonProgrammate)"
    Write-Host "  • Programmate: $nuoveProgrammate (Prima: $programmate) ⚠️ CRITICO - Deve aumentare"
    
    if ($nuoveProgrammate -gt $programmate) {
        Write-Host "`n✅ SUCCESSO: Lo stato è cambiato!" -ForegroundColor Green
        Write-Host "   $($nuoveProgrammate - $programmate) commesse passate a Programmata" -ForegroundColor Green
    } else {
        Write-Host "`n❌ ERRORE: Lo stato NON è cambiato!" -ForegroundColor Red
        Write-Host "   Programmate prima: $programmate" -ForegroundColor Red
        Write-Host "   Programmate dopo: $nuoveProgrammate" -ForegroundColor Red
        exit 1
    }
    
    $reader3.Close()
    $sqlCmd3.Connection.Close()
    
} catch {
    Write-Host "❌ Errore verifica post-export: $_" -ForegroundColor Red
    exit 1
}

# ==============================================================================
# FASE 4: Test UI - Navigazione ProgrammaMacchine
# ==============================================================================

Write-Host "`n🌐 FASE 4: Test Navigazione UI ProgrammaMacchine" -ForegroundColor Yellow

$uiTestUrl = "$ApiUrl/programma/programma-macchine"
Write-Host "URL: $uiTestUrl" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest `
        -Uri $uiTestUrl `
        -Method GET `
        -TimeoutSec $TimeoutSeconds `
        -ErrorAction Stop
    
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Pagina ProgrammaMacchine accessibile (HTTP 200)" -ForegroundColor Green
        Write-Host "   → Verifica manualmente che i dati esportati siano visibili" -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "⚠️ Errore raggiungere pagina: $($_.Exception.Message)" -ForegroundColor Yellow
}

# ==============================================================================
# RIEPILOGO FINALE
# ==============================================================================

Write-Host "`n════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RIEPILOGO TEST" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`n✅ Test completati. Verifica manuale richiesta:" -ForegroundColor Green
Write-Host "  1. Vai a http://localhost:5156/programma/programma-macchine"
Write-Host "  2. Controlla che le commesse siano visibili con date corrette"
Write-Host "  3. Verifica che DataInizioPrevisione != NULL e != 00:00"
Write-Host "  4. Verifica che lo stato sia 'Programmata'" -ForegroundColor Green

Write-Host "`n📋 LEZIONI APPRESE:" -ForegroundColor Yellow
Write-Host "  • NON dichiarare 'funziona' senza test automatizzati visibili"
Write-Host "  • DataInizioPrevisione.HasValue = filtro critico"
Write-Host "  • StatoProgramma = 0 (NonProgrammata) / 1 (Programmata)"
Write-Host "  • OrderBy su Guid = ordine casuale, usare campo semantico"
Write-Host "  • Log terminal = strumento di debug essenziale pre-dichiarazione"
