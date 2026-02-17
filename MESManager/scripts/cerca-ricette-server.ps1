# Script per cercare ricette sul server MESManager_Prod
# Cerca in tutte le tabelle possibili dove potrebbero essere salvate

$server = "192.168.1.230\SQLEXPRESS01"
$database = "MESManager_Prod"
$uid = "FAB"
$pwd = "password.123"

$connString = "Server=$server;Database=$database;User Id=$uid;Password=$pwd;TrustServerCertificate=True;"

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "   CERCA RICETTE SUL SERVER" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    
    # 1. Verifica tabelle standard
    Write-Host "📊 TABELLE STANDARD:" -ForegroundColor Yellow
    Write-Host "=" * 50
    
    $tables = @(
        @{Name="Ricette"; Query="SELECT COUNT(*) FROM Ricette"},
        @{Name="ParametriRicetta"; Query="SELECT COUNT(*) FROM ParametriRicetta"},
        @{Name="ArticoliRicetta"; Query="SELECT COUNT(*) FROM ArticoliRicetta"},
        @{Name="Articoli"; Query="SELECT COUNT(*) FROM Articoli WHERE Id IN (SELECT ArticoloId FROM Ricette)"}
    )
    
    foreach ($table in $tables) {
        try {
            $cmd.CommandText = $table.Query
            $count = $cmd.ExecuteScalar()
            Write-Host "  $($table.Name): " -NoNewline -ForegroundColor White
            if ($count -gt 0) {
                Write-Host "$count righe" -ForegroundColor Green
            } else {
                Write-Host "$count righe" -ForegroundColor Red
            }
        } catch {
            Write-Host "  $($table.Name): ERRORE - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # 2. Lista articoli con ricette
    Write-Host "`n📋 ARTICOLI CON RICETTE:" -ForegroundColor Yellow
    Write-Host "=" * 50
    
    $cmd.CommandText = @"
SELECT 
    a.Codice,
    LEFT(a.Descrizione, 50) as Descrizione,
    (SELECT COUNT(*) FROM ParametriRicetta pr WHERE pr.RicettaId = r.Id) as NumParametri
FROM Articoli a
INNER JOIN Ricette r ON a.Id = r.ArticoloId
ORDER BY a.Codice
"@
    
    $reader = $cmd.ExecuteReader()
    $hasRows = $false
    while ($reader.Read()) {
        $hasRows = $true
        Write-Host "  $($reader['Codice']) - $($reader['Descrizione']) ($($reader['NumParametri']) parametri)" -ForegroundColor White
    }
    $reader.Close()
    
    if (-not $hasRows) {
        Write-Host "  Nessun articolo con ricette trovato" -ForegroundColor Red
    }
    
    # 3. Cerca altre tabelle che potrebbero contenere ricette
    Write-Host "`n🔍 ALTRE TABELLE POSSIBILI:" -ForegroundColor Yellow
    Write-Host "=" * 50
    
    $cmd.CommandText = @"
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
AND (TABLE_NAME LIKE '%ricett%' 
     OR TABLE_NAME LIKE '%recipe%' 
     OR TABLE_NAME LIKE '%param%')
ORDER BY TABLE_NAME
"@
    
    $reader = $cmd.ExecuteReader()
    $foundTables = @()
    while ($reader.Read()) {
        $foundTables += $reader['TABLE_NAME']
    }
    $reader.Close()
    
    foreach ($tableName in $foundTables) {
        try {
            $cmd.CommandText = "SELECT COUNT(*) FROM [$tableName]"
            $count = $cmd.ExecuteScalar()
            Write-Host "  $tableName : $count righe" -ForegroundColor $(if ($count -gt 0) { "Green" } else { "Gray" })
        } catch {
            Write-Host "  $tableName : ERRORE" -ForegroundColor Red
        }
    }
    
    # 4. Statistiche parametri esistenti
    Write-Host "`n📈 STATISTICHE PARAMETRI:" -ForegroundColor Yellow
    Write-Host "=" * 50
    
    $cmd.CommandText = @"
SELECT 
    COUNT(*) as Totali,
    SUM(CASE WHEN CodiceParametro IS NOT NULL AND CodiceParametro > 0 THEN 1 ELSE 0 END) as ConCodice,
    SUM(CASE WHEN Indirizzo IS NOT NULL THEN 1 ELSE 0 END) as ConIndirizzo,
    SUM(CASE WHEN Area IS NOT NULL AND Area != '' THEN 1 ELSE 0 END) as ConArea
FROM ParametriRicetta
"@
    
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "  Parametri totali: $($reader['Totali'])" -ForegroundColor White
        Write-Host "  Con CodiceParametro: $($reader['ConCodice'])" -ForegroundColor White
        Write-Host "  Con Indirizzo: $($reader['ConIndirizzo'])" -ForegroundColor White
        Write-Host "  Con Area: $($reader['ConArea'])" -ForegroundColor White
    }
    $reader.Close()
    
    $conn.Close()
    
    Write-Host "`n✅ ANALISI COMPLETATA" -ForegroundColor Green
    Write-Host "================================================`n" -ForegroundColor Cyan
    
} catch {
    Write-Host "`n❌ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
    if ($conn.State -eq 'Open') {
        $conn.Close()
    }
}
