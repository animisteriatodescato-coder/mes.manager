# Script diagnostica cataloghi database - Versione semplificata

$connStr = "Server=localhost\SQLEXPRESS01;Database=MESManager_Dev;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "`n=== DIAGNOSTICA CATALOGHI ===" -ForegroundColor Cyan

try {
    Add-Type -AssemblyName System.Data
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    Write-Host "[OK] Connesso: $($conn.Database)`n" -ForegroundColor Green
    
    # Conteggi tabelle
    $tabelle = @("Anime", "Operatori", "Macchine", "Articoli", "Ricette", "Clienti", "Commesse")
    
    Write-Host "CONTEGGI:" -ForegroundColor Yellow
    foreach ($t in $tabelle) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM [$t]"
        $count = [int]$cmd.ExecuteScalar()
        $status = if ($count -gt 0) { "[OK]" } else { "[!!]" }
        Write-Host "$status $t = $count"
    }
    
    Write-Host "`nDUPLICATI MACCHINE:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Codice, Nome, COUNT(*) as Cnt FROM Macchine GROUP BY Codice, Nome HAVING COUNT(*) > 1"
    $reader = $cmd.ExecuteReader()
    $found = $false
    while ($reader.Read()) {
        $found = $true
        Write-Host "[DUPLICATO] $($reader['Codice']) - $($reader['Nome']) (x$($reader['Cnt']))" -ForegroundColor Red
    }
    $reader.Close()
    if (-not $found) { Write-Host "[OK] Nessun duplicato" -ForegroundColor Green }
    
    Write-Host "`nPRIMI 5 ANIME:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 5 Id, CodiceArticolo, DescrizioneArticolo FROM Anime"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "  $($reader['Id']) | $($reader['CodiceArticolo']) | $($reader['DescrizioneArticolo'])"
    }
    $reader.Close()
    
    Write-Host "`nOPERATORI:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, NumeroOperatore, Nome, Cognome, Attivo FROM Operatori ORDER BY NumeroOperatore"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        $att = if ($reader['Attivo']) { "[A]" } else { "[-]" }
        Write-Host "  $att N.$($reader['NumeroOperatore']) | $($reader['Nome']) $($reader['Cognome'])"
    }
    $reader.Close()
    
    Write-Host "`nMACCHINE:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Codice, Nome, AttivaInGantt FROM Macchine ORDER BY Codice"
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        $att = if ($reader['AttivaInGantt']) { "[G]" } else { "[ ]" }
        Write-Host "  $att $($reader['Codice']) | $($reader['Nome'])"
    }
    $reader.Close()
    
    $conn.Close()
    Write-Host "`n=== DIAGNOSTICA COMPLETATA ===`n" -ForegroundColor Cyan
}
catch {
    Write-Host "`n[ERRORE] $_`n" -ForegroundColor Red
}
