# ANALISI APPROFONDITA - Cerca 120 ricette mancanti

$server = "192.168.1.230\SQLEXPRESS01"
$uid = "FAB"
$pwd = "password.123"

Write-Host "`n======================================================" -ForegroundColor Cyan
Write-Host "   ANALISI APPROFONDITA RICETTE - CERCA 120 RICETTE" -ForegroundColor Cyan
Write-Host "======================================================`n" -ForegroundColor Cyan

# Lista tutti i database
Write-Host "📁 SCANSIONE TUTTI I DATABASE..." -ForegroundColor Yellow
$connMaster = New-Object System.Data.SqlClient.SqlConnection("Server=$server;Database=master;User Id=$uid;Password=$pwd;TrustServerCertificate=True;")
$connMaster.Open()
$cmdMaster = $connMaster.CreateCommand()

$cmdMaster.CommandText = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb') ORDER BY name"
$readerDbs = $cmdMaster.ExecuteReader()
$databases = @()
while ($readerDbs.Read()) {
    $databases += $readerDbs['name']
}
$readerDbs.Close()
$connMaster.Close()

Write-Host "  Database trovati: $($databases -join ', ')`n" -ForegroundColor White

foreach ($dbName in $databases) {
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "📊 DATABASE: $dbName" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection("Server=$server;Database=$dbName;User Id=$uid;Password=$pwd;TrustServerCertificate=True;")
        $conn.Open()
        $cmd = $conn.CreateCommand()
        
        # Cerca tutte le tabelle con "ricett", "recipe", "param"
        $cmd.CommandText = @"
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME
"@
        
        $readerTables = $cmd.ExecuteReader()
        $allTables = @()
        while ($readerTables.Read()) {
            $allTables += $readerTables['TABLE_NAME']
        }
        $readerTables.Close()
        
        # Filtra tabelle interessanti
        $ricetteTables = $allTables | Where-Object { 
            $_ -like '*ricett*' -or 
            $_ -like '*recipe*' -or 
            $_ -like '*param*' -or
            $_ -like '*Articol*'
        }
        
        if ($ricetteTables.Count -gt 0) {
            Write-Host "`n  📋 Tabelle rilevanti trovate:" -ForegroundColor Yellow
            foreach ($tableName in $ricetteTables) {
                try {
                    $cmd.CommandText = "SELECT COUNT(*) FROM [$tableName]"
                    $count = $cmd.ExecuteScalar()
                    
                    $color = "Gray"
                    if ($count -gt 0) { $color = "Green" }
                    if ($count -gt 50) { $color = "Cyan" }
                    
                    Write-Host "     [$tableName] = " -NoNewline -ForegroundColor White
                    Write-Host "$count righe" -ForegroundColor $color
                    
                    # Se ha molte righe, mostra struttura
                    if ($count -gt 50) {
                        $cmd.CommandText = "SELECT TOP 1 * FROM [$tableName]"
                        $readerSample = $cmd.ExecuteReader()
                        if ($readerSample.Read()) {
                            Write-Host "        Colonne: " -NoNewline -ForegroundColor Gray
                            $columns = @()
                            for ($i = 0; $i -lt $readerSample.FieldCount; $i++) {
                                $columns += $readerSample.GetName($i)
                            }
                            Write-Host ($columns -join ", ") -ForegroundColor Gray
                        }
                        $readerSample.Close()
                    }
                } catch {
                    Write-Host "     [$tableName] = ERRORE" -ForegroundColor Red
                }
            }
        }
        
        # Cerca specificamente tabelle Ricette e ParametriRicetta
        if ($allTables -contains "Ricette") {
            Write-Host "`n  🔍 DETTAGLIO RICETTE:" -ForegroundColor Yellow
            
            $cmd.CommandText = @"
SELECT 
    COUNT(DISTINCT r.Id) as NumRicette,
    COUNT(DISTINCT r.ArticoloId) as NumArticoli,
    (SELECT COUNT(*) FROM ParametriRicetta) as NumParametri
FROM Ricette r
"@
            try {
                $readerStats = $cmd.ExecuteReader()
                if ($readerStats.Read()) {
                    Write-Host "     Ricette: $($readerStats['NumRicette'])" -ForegroundColor White
                    Write-Host "     Articoli distinti: $($readerStats['NumArticoli'])" -ForegroundColor White
                    Write-Host "     Parametri totali: $($readerStats['NumParametri'])" -ForegroundColor White
                }
                $readerStats.Close()
            } catch {
                Write-Host "     Errore lettura stats: $($_.Exception.Message)" -ForegroundColor Red
            }
            
            # Lista articoli
            $cmd.CommandText = @"
SELECT TOP 20
    a.Codice as CodiceArticolo,
    LEFT(a.Descrizione, 40) as Descrizione,
    (SELECT COUNT(*) FROM ParametriRicetta WHERE RicettaId = r.Id) as NumParam
FROM Ricette r
LEFT JOIN Articoli a ON r.ArticoloId = a.Id
ORDER BY a.Codice
"@
            try {
                $readerArticoli = $cmd.ExecuteReader()
                $count = 0
                Write-Host "`n     Articoli con ricette:" -ForegroundColor Gray
                while ($readerArticoli.Read()) {
                    $count++
                    $codice = if ($readerArticoli['CodiceArticolo'] -ne [DBNull]::Value) { $readerArticoli['CodiceArticolo'] } else { "NULL" }
                    $desc = if ($readerArticoli['Descrizione'] -ne [DBNull]::Value) { $readerArticoli['Descrizione'] } else { "" }
                    Write-Host "       $count. $codice - $desc ($($readerArticoli['NumParam']) param)" -ForegroundColor White
                }
                $readerArticoli.Close()
                
                if ($count -eq 0) {
                    Write-Host "       Nessun articolo trovato (possibile problema FK)" -ForegroundColor Red
                }
            } catch {
                Write-Host "       Errore: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        
        # Cerca ArticoliRicetta (vecchia struttura)
        if ($allTables -contains "ArticoliRicetta") {
            Write-Host "`n  🔍 DETTAGLIO ArticoliRicetta (VECCHIA STRUTTURA):" -ForegroundColor Yellow
            
            $cmd.CommandText = "SELECT COUNT(*), COUNT(DISTINCT CodiceArticolo) as NumArticoli FROM ArticoliRicetta"
            try {
                $readerAR = $cmd.ExecuteReader()
                if ($readerAR.Read()) {
                    $totRighe = $readerAR.GetInt32(0)
                    $numArt = $readerAR['NumArticoli']
                    Write-Host "     Righe totali: $totRighe" -ForegroundColor White
                    Write-Host "     Articoli distinti: $numArt" -ForegroundColor White
                    
                    if ($totRighe -gt 0) {
                        Write-Host "     ⚠️ POSSIBILE FONTE DATI DA IMPORTARE!" -ForegroundColor Yellow
                    }
                }
                $readerAR.Close()
                
                if ($totRighe -gt 0) {
                    # Mostra campione
                    $cmd.CommandText = "SELECT DISTINCT TOP 10 CodiceArticolo FROM ArticoliRicetta ORDER BY CodiceArticolo"
                    $readerSample = $cmd.ExecuteReader()
                    Write-Host "`n     Campione articoli:" -ForegroundColor Gray
                    while ($readerSample.Read()) {
                        Write-Host "       - $($readerSample['CodiceArticolo'])" -ForegroundColor White
                    }
                    $readerSample.Close()
                }
            } catch {
                Write-Host "     Errore: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        
        $conn.Close()
        
    } catch {
        Write-Host "  ❌ Impossibile accedere a [$dbName]: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n======================================================" -ForegroundColor Cyan
Write-Host "   ANALISI COMPLETATA" -ForegroundColor Cyan
Write-Host "======================================================`n" -ForegroundColor Cyan
