#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Check SQL Logs - Analizza log applicazione e database per errori export
    
.DESCRIPTION
    Estrae informazioni diagnostiche da:
    1. Application log (ILogger)
    2. Event Viewer (Application)
    3. SQL queries recenti
    4. File log app (.NET)
    
.NOTA
    Esegui come Admin per accedere a Event Viewer
#>

param(
    [string]$DbServer = "localhost\SQLEXPRESS01",
    [string]$DbName = "MESManager",
    [int]$MinutesBack = 5
)

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📋 ANALISI LOG - Export Gantt Diagnostica" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ==============================================================================
# Sezione 1: SQL Query Log
# ==============================================================================

Write-Host "`n🔍 SQL QUERIES RECENTI (ultimi $MinutesBack minuti)" -ForegroundColor Yellow

$sqlLogsQuery = @"
SELECT TOP 20
    CONVERT(VARCHAR(20), create_time, 120) as [Ora],
    text as [Query],
    execution_count as [Esecuzioni],
    total_elapsed_time as [Tempo (µs)]
FROM sys.dm_exec_query_stats
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
WHERE create_time > DATEADD(MINUTE, -$MinutesBack, GETDATE())
  AND (text LIKE '%esporta%' 
       OR text LIKE '%Programmata%' 
       OR text LIKE '%StatoProgramma%'
       OR text LIKE '%ExportSuProgramma%')
ORDER BY create_time DESC
"@

try {
    $dbConnection = "Data Source=$DbServer;Initial Catalog=$DbName;Integrated Security=true;"
    $sqlCmd = New-Object System.Data.SqlClient.SqlCommand
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($dbConnection)
    $sqlConnection.Open()
    
    $sqlCmd.CommandText = $sqlLogsQuery
    $sqlCmd.Connection = $sqlConnection
    $reader = $sqlCmd.ExecuteReader()
    
    if ($reader.HasRows) {
        Write-Host "✓ Query trovate:" -ForegroundColor Green
        while ($reader.Read()) {
            $ora = $reader["Ora"]
            $query = $reader["Query"].ToString().Substring(0, [Math]::Min(80, $reader["Query"].Length))
            Write-Host "  [$ora] $query..."
        }
    } else {
        Write-Host "⚠️ Nessuna query di export trovata negli ultimi $MinutesBack minuti" -ForegroundColor Yellow
        Write-Host "   → Aumenta MinutesBack o esegui export manualmente" -ForegroundColor Gray
    }
    
    $reader.Close()
    $sqlConnection.Close()
} catch {
    Write-Host "❌ Errore SQL: $_" -ForegroundColor Red
}

# ==============================================================================
# Sezione 2: Stato Tabella Commesse
# ==============================================================================

Write-Host "`n📊 STATO TABELLA COMMESSE" -ForegroundColor Yellow

$statusQuery = @"
SELECT 
    COUNT(*) as TotalRows,
    SUM(CASE WHEN NumeroMacchina IS NULL THEN 1 ELSE 0 END) as [SenzaMacchina],
    SUM(CASE WHEN DataInizioPrevisione IS NULL THEN 1 ELSE 0 END) as [SenzaDataInizio],
    SUM(CASE WHEN StatoProgramma = 0 THEN 1 ELSE 0 END) as [StatoNonProgrammata],
    SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) as [StatoProgrammata]
FROM Commesse
"@

try {
    $sqlCmd2 = New-Object System.Data.SqlClient.SqlCommand
    $sqlCmd2.CommandText = $statusQuery
    $sqlCmd2.Connection = New-Object System.Data.SqlClient.SqlConnection($dbConnection)
    $sqlCmd2.Connection.Open()
    
    $reader2 = $sqlCmd2.ExecuteReader()
    if ($reader2.Read()) {
        Write-Host "✓ Riepilogo:" -ForegroundColor Green
        Write-Host "  • Totale: $($reader2['TotalRows'])"
        Write-Host "  • Senza Macchina: $($reader2['SenzaMacchina'])"
        Write-Host "  • Senza DataInizio: $($reader2['SenzaDataInizio'])"
        Write-Host "  • Stato NonProgrammata: $($reader2['StatoNonProgrammata'])"
        Write-Host "  • Stato Programmata: $($reader2['StatoProgrammata'])"
    }
    $reader2.Close()
    $sqlCmd2.Connection.Close()
} catch {
    Write-Host "❌ Errore: $_" -ForegroundColor Red
}

# ==============================================================================
# Sezione 3: Application Event Log
# ==============================================================================

Write-Host "`n⚠️ APPLICATION EVENTS (Event Viewer)" -ForegroundColor Yellow

try {
    $events = Get-EventLog -LogName Application `
        -After (Get-Date).AddMinutes(-$MinutesBack) `
        -Source "*MESManager*" `
        -ErrorAction SilentlyContinue | Select-Object -First 10
    
    if ($events) {
        Write-Host "✓ Eventi trovati:" -ForegroundColor Green
        foreach ($event in $events) {
            Write-Host "  [$($event.TimeGenerated)] $($event.EntryType): $($event.Message.Substring(0, 100))"
        }
    } else {
        Write-Host "ℹ️ Nessun evento MESManager trovato" -ForegroundColor Gray
        Write-Host "   (Richiede di eseguire con privilegi Admin)" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️ Non puoi accedere a Event Viewer (esegui come Admin)" -ForegroundColor Yellow
}

# ==============================================================================
# Sezione 4: Raccomandazioni
# ==============================================================================

Write-Host "`n💡 RACCOMANDAZIONI DEBUG" -ForegroundColor Cyan
Write-Host "  1. Aggiungi logging in PianificazioneController.EsportaSuProgramma()" -ForegroundColor Gray
Write-Host "  2. Verifica DataCambioStatoProgramma in tabella Commesse" -ForegroundColor Gray
Write-Host "  3. Controlla che Cosmos/SignalR notifichino correttamente" -ForegroundColor Gray
Write-Host "  4. Usa SQL Profiler per tracciare query esatte" -ForegroundColor Gray
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
