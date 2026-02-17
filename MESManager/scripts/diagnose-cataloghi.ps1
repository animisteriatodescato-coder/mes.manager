# Script PowerShell per diagnostica cataloghi database

$connectionString = "Server=localhost\SQLEXPRESS01;Database=MESManager_Dev;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  DIAGNOSTICA CATALOGHI - MESManager" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

try {
    Add-Type -AssemblyName "System.Data"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    Write-Host "[OK] Connesso a: $($conn.Database) su $($conn.DataSource)" -ForegroundColor Green
    Write-Host ""
    
    # Query conteggi
    $queries = @{
        "Anime" = "SELECT COUNT(*) FROM Anime"
        "Operatori" = "SELECT COUNT(*) FROM Operatori"
        "Macchine" = "SELECT COUNT(*) FROM Macchine"
        "Articoli" = "SELECT COUNT(*) FROM Articoli"
        "Ricette" = "SELECT COUNT(*) FROM Ricette"
        "Clienti" = "SELECT COUNT(*) FROM Clienti"
        "Commesse" = "SELECT COUNT(*) FROM Commesse"
    }
    
    Write-Host "📊 CONTEGGI TABELLE:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    foreach ($tabella in $queries.Keys | Sort-Object) {
        $cmd = New-Object System.Data.SqlClient.SqlCommand($queries[$tabella], $conn)
        $count = [int]$cmd.ExecuteScalar()
        $status = if ($count -gt 0) { "✅" } else { "❌" }
        Write-Host ("{0} {1,-15} : {2,6} record" -f $status, $tabella, $count)
    }
    
    Write-Host ""
    Write-Host "🔍 VERIFICA DUPLICATI MACCHINE:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $checkDuplicates = @"
        SELECT Codice, Nome, COUNT(*) as Conteggio 
        FROM Macchine 
        GROUP BY Codice, Nome 
        HAVING COUNT(*) > 1
"@
    
    $cmd = New-Object System.Data.SqlClient.SqlCommand($checkDuplicates, $conn)
    $reader = $cmd.ExecuteReader()
    $hasDuplicates = $false
    
    while ($reader.Read()) {
        $hasDuplicates = $true
        Write-Host ("⚠️  Duplicato: {0} - {1} ({2} volte)" -f $reader["Codice"], $reader["Nome"], $reader["Conteggio"]) -ForegroundColor Red
    }
    $reader.Close()
    
    if (-not $hasDuplicates) {
        Write-Host "✅ Nessun duplicato trovato" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🔍 SAMPLE ANIME (primi 5):" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $sampleAnime = "SELECT TOP 5 Id, CodiceArticolo, DescrizioneArticolo FROM Anime ORDER BY Id"
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sampleAnime, $conn)
    $reader = $cmd.ExecuteReader()
    
    if ($reader.HasRows) {
        while ($reader.Read()) {
            Write-Host ("  {0,5} | {1,-15} | {2}" -f $reader["Id"], $reader["CodiceArticolo"], $reader["DescrizioneArticolo"])
        }
    } else {
        Write-Host "  ⚠️  Nessuna anima presente!" -ForegroundColor Red
    }
    $reader.Close()
    
    Write-Host ""
    Write-Host "🔍 SAMPLE OPERATORI:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $sampleOp = "SELECT Id, NumeroOperatore, Nome, Cognome, Attivo FROM Operatori ORDER BY NumeroOperatore"
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sampleOp, $conn)
    $reader = $cmd.ExecuteReader()
    
    if ($reader.HasRows) {
        while ($reader.Read()) {
            $attivo = if ($reader["Attivo"]) { "✅" } else { "❌" }
            Write-Host ("  {0} Num: {1,3} | {2} {3}" -f $attivo, $reader["NumeroOperatore"], $reader["Nome"], $reader["Cognome"])
        }
    } else {
        Write-Host "  ⚠️  Nessun operatore presente!" -ForegroundColor Red
    }
    $reader.Close()
    
    Write-Host ""
    Write-Host "🔍 MACCHINE:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    $sampleMac = "SELECT Id, Codice, Nome, AttivaInGantt FROM Macchine ORDER BY Codice"
    $cmd = New-Object System.Data.SqlClient.SqlCommand($sampleMac, $conn)
    $reader = $cmd.ExecuteReader()
    
    if ($reader.HasRows) {
        while ($reader.Read()) {
            $attiva = if ($reader["AttivaInGantt"]) { "✅" } else { "❌" }
            Write-Host ("  {0} {1,-10} | {2}" -f $attiva, $reader["Codice"], $reader["Nome"])
        }
    } else {
        Write-Host "  ⚠️  Nessuna macchina presente!" -ForegroundColor Red
    }
    $reader.Close()
    
    $conn.Close()
    
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  DIAGNOSTICA COMPLETATA                                  ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "❌ ERRORE: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.StackTrace -ForegroundColor DarkRed
}
