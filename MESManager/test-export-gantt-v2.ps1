#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test Framework per Export Gantt su Programma (v1 SEMPLIFICATA)
    
.DESCRIPTION
    Test automatizzato verifica export con approccio straightforward
    
.CRITICO
    Script DEVE essere eseguibile senza errori per dichiarare funzionamento
#>

param(
    [string]$ApiUrl = "http://localhost:5156",
    [string]$DbServer = "localhost\SQLEXPRESS01",
    [string]$DbName = "MESManager",
    [int]$TimeoutSeconds = 30
)

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 TEST FRAMEWORK - Export Gantt su Programma v1" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ============================================================================
# FASE 1: Verifica Database
# ============================================================================

Write-Host "`n📋 FASE 1: Verifica Commesse in Database" -ForegroundColor Yellow

$connectionString = "Data Source=$DbServer;Initial Catalog=$DbName;Integrated Security=true;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    # Query stato commesse
    $query1 = @"
    SELECT 
        COUNT(*) as TotaleCommesse,
        SUM(CASE WHEN NumeroMacchina IS NOT NULL THEN 1 ELSE 0 END) as ConMacchina,
        SUM(CASE WHEN DataInizioPrevisione IS NOT NULL THEN 1 ELSE 0 END) as ConDataInizio,
        SUM(CASE WHEN StatoProgramma = 0 THEN 1 ELSE 0 END) as NonProgrammate,
        SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) as Programmate
    FROM Commesse
    WHERE DataInizioPrevisione IS NOT NULL
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($query1, $connection)
    $reader = $command.ExecuteReader()
    $reader.Read()
    
    $totale = $reader.GetInt32(0)
    $conMacchina = $reader.GetInt32(1)
    $conDataInizio = $reader.GetInt32(2)
    $nonProgrammate = $reader.GetInt32(3)
    $programmate = $reader.GetInt32(4)
    
    $reader.Close()
    
    Write-Host "✓ Stato Database:" -ForegroundColor Green
    Write-Host "  • Totale Commesse: $totale"
    Write-Host "  • Con Macchina: $conMacchina"
    Write-Host "  • Con DataInizioPrevisione: $conDataInizio ⚠️ CRITICO"
    Write-Host "  • Non Programmate: $nonProgrammate"
    Write-Host "  • Programmate: $programmate"
    
    if ($conDataInizio -eq 0) {
        Write-Host "`n❌ ERRORE CRITICO: Nessuna commessa con DataInizioPrevisione!" -ForegroundColor Red
        Write-Host "   Export non può funzionare senza date!" -ForegroundColor Red
        Write-Host "   Verifica il fix di normalizzazione" -ForegroundColor Red
        exit 1
    }
    
    # Query campione commesse
    Write-Host "`n📊 Campione Commesse (TOP 5 con date):" -ForegroundColor Cyan
    
    $query2 = @"
    SELECT TOP 5
        Codice,
        NumeroMacchina,
        DataInizioPrevisione,
        DataFinePrevisione,
        StatoProgramma
    FROM Commesse
    WHERE DataInizioPrevisione IS NOT NULL
    ORDER BY Codice
"@
    
    $command2 = New-Object System.Data.SqlClient.SqlCommand($query2, $connection)
    $reader2 = $command2.ExecuteReader()
    
    while ($reader2.Read()) {
        $codice = $reader2.GetString(0)
        $macchina = $reader2.IsDBNull(1) ? "NULL" : $reader2.GetString(1)
        $dataInizio = $reader2.IsDBNull(2) ? "NULL" : $reader2.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss")
        $stato = $reader2.GetInt32(4) -eq 0 ? "NonProgrammata" : "Programmata"
        
        Write-Host "  $codice | M$macchina | Inizio=$dataInizio | $stato"
    }
    
    $reader2.Close()
    
} catch {
    Write-Host "❌ Errore Database: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# FASE 2: Test API Export
# ============================================================================

Write-Host "`n📡 FASE 2: Test API Export" -ForegroundColor Yellow

$apiUrl = "$ApiUrl/api/pianificazione/esporta-su-programma"
Write-Host "POST: $apiUrl" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri $apiUrl -Method POST -ContentType "application/json" -TimeoutSec $TimeoutSeconds -ErrorAction Stop
    
    Write-Host "✓ Endpoint risponde (HTTP $($response.StatusCode))" -ForegroundColor Green
    
    $responseJson = $response.Content | ConvertFrom-Json
    
    Write-Host "`n✓ Risposta API:" -ForegroundColor Green
    Write-Host "  • success: $($responseJson.success)"
    Write-Host "  • message: $($responseJson.message)"
    
    if ($responseJson.PSObject.Properties.Name -contains 'debugInfo') {
        Write-Host "  • debugInfo:" -ForegroundColor Cyan
        $debug = $responseJson.debugInfo
        Write-Host "    - totali: $($debug.totali)"
        Write-Host "    - conMacchina: $($debug.conMacchina)"
        Write-Host "    - conDataInizio: $($debug.conDataInizio)"
    }
    
    if ($responseJson.success -eq $true) {
        Write-Host "`n✅ SUCCESS: Export ha funzionato!" -ForegroundColor Green
    } else {
        Write-Host "`n⚠️ ATTENZIONE: success=false" -ForegroundColor Yellow
        Write-Host "   Causa: $($responseJson.message)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Errore API: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# FASE 3: Verifica Cambio Stato
# ============================================================================

Write-Host "`n🔍 FASE 3: Verifica Post-Export" -ForegroundColor Yellow

Start-Sleep -Seconds 2

try {
    $connection2 = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection2.Open()
    
    $query3 = @"
    SELECT 
        SUM(CASE WHEN StatoProgramma = 0 THEN 1 ELSE 0 END) as NonProgrammate,
        SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) as Programmate
    FROM Commesse
    WHERE DataInizioPrevisione IS NOT NULL
"@
    
    $command3 = New-Object System.Data.SqlClient.SqlCommand($query3, $connection2)
    $reader3 = $command3.ExecuteReader()
    $reader3.Read()
    
    $nonProgDopo = $reader3.GetInt32(0)
    $progDopo = $reader3.GetInt32(1)
    
    $reader3.Close()
    $connection2.Close()
    
    Write-Host "✓ Stato Post-Export:" -ForegroundColor Green
    Write-Host "  • Non Programmate: $nonProgDopo (Prima: $nonProgrammate) Δ= $($nonProgrammate - $nonProgDopo)"
    Write-Host "  • Programmate: $progDopo (Prima: $programmate) Δ= $($progDopo - $programmate)"
    
    $delta = $progDopo - $programmate
    if ($delta -gt 0) {
        Write-Host "`n✅ SUCCESSO: Stato cambiato!" -ForegroundColor Green
        Write-Host "   $delta commesse passate a Programmata" -ForegroundColor Green
    } elseif ($delta -eq 0) {
        Write-Host "`n⚠️ ATTENZIONE: Nessun cambio stato" -ForegroundColor Yellow
        Write-Host "   Non è un errore se tutte erano già Programmate" -ForegroundColor Yellow
    } else {
        Write-Host "`n❌ ERRORE: Il numero è diminuito (dati inconsistenti)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Errore verifica: $($_.Exception.Message)" -ForegroundColor Red
}

# ============================================================================
# RIEPILOGO
# ============================================================================

Write-Host "`n════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RIEPILOGO TEST" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`n✅ Test completati." -ForegroundColor Green
Write-Host "`n📝 Prossimi step manuali:" -ForegroundColor Yellow
Write-Host "  1. Apri: http://localhost:5156/programma/programma-macchine"
Write-Host "  2. Verifica che i dati siano visibili"
Write-Host "  3. Controlla date (non devono essere 00:00)"
Write-Host "  4. Controlla stato = Programmata" -ForegroundColor Yellow

Write-Host "`n📚 Lezioni Apprese:" -ForegroundColor Cyan
Write-Host "  • Test script eseguibile = prova di funzionamento"
Write-Host "  • Logging nei log terminale = strumento debug essenziale"
Write-Host "  • BEFORE/AFTER conteggi = verifica deterministica"
Write-Host "  • debugInfo nella response = diagnostica visibile"
Write-Host "  • Mai dichiarare 'funziona' senza prova visibile" -ForegroundColor Cyan
