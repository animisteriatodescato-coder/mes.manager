# Script per copiare i dati dal database di produzione MESManager a MESManager_Dev
# ATTENZIONE: Questo script copia TUTTI i dati dal database di produzione

$sourceSA = "sa"
$sourcePassword = "password.123"
$sourceServer = "192.168.1.230\SQLEXPRESS01"
$sourceDB = "MESManager"
$destDB = "MESManager_Dev"
$destUser = "FAB"
$destPassword = "password.123"

Write-Host "=== COPIA DATI DA PRODUZIONE A SVILUPPO ===" -ForegroundColor Cyan
Write-Host "Sorgente: $sourceServer\$sourceDB (con utente sa)" -ForegroundColor Yellow
Write-Host "Destinazione: $sourceServer\$destDB (con utente FAB)" -ForegroundColor Green
Write-Host ""

# Lista tabelle in ordine di dipendenza (prima le tabelle senza FK, poi quelle con FK)
$tabelle = @(
    "Operatori",
    "Clienti", 
    "Macchine",
    "TipiDocumento",
    "Commesse",
    "Articoli",
    "Anime",
    "PLCRealtime",
    "StatoCommesse",
    "ConfigurazioniPLC",
    "LogErrori"
)

try {
    # Connessione al database sorgente (produzione)
    $sourceConnString = "Server=$sourceServer;Database=$sourceDB;User Id=$sourceSA;Password=$sourcePassword;TrustServerCertificate=True;"
    $sourceConn = New-Object System.Data.SqlClient.SqlConnection($sourceConnString)
    $sourceConn.Open()
    Write-Host "✓ Connesso al database sorgente: $sourceDB" -ForegroundColor Green
    
    # Connessione al database destinazione (sviluppo)
    $destConnString = "Server=$sourceServer;Database=$destDB;User Id=$destUser;Password=$destPassword;TrustServerCertificate=True;"
    $destConn = New-Object System.Data.SqlClient.SqlConnection($destConnString)
    $destConn.Open()
    Write-Host "✓ Connesso al database destinazione: $destDB" -ForegroundColor Green
    Write-Host ""
    
    foreach ($tabella in $tabelle) {
        Write-Host "Copiando tabella: $tabella..." -ForegroundColor Cyan
        
        # Conta record nella sorgente
        $countCmd = $sourceConn.CreateCommand()
        $countCmd.CommandText = "SELECT COUNT(*) FROM [$tabella]"
        $recordCount = $countCmd.ExecuteScalar()
        
        if ($recordCount -eq 0) {
            Write-Host "  → Tabella vuota, skip" -ForegroundColor Gray
            continue
        }
        
        # Leggi i dati dalla sorgente
        $selectCmd = $sourceConn.CreateCommand()
        $selectCmd.CommandText = "SELECT * FROM [$tabella]"
        $reader = $selectCmd.ExecuteReader()
        
        # Prepara l'adapter e il DataTable
        $dataTable = New-Object System.Data.DataTable
        $dataTable.Load($reader)
        $reader.Close()
        
        if ($dataTable.Rows.Count -eq 0) {
            Write-Host "  → Nessun dato da copiare" -ForegroundColor Gray
            continue
        }
        
        # Disabilita IDENTITY_INSERT se necessario
        $setIdentityCmd = $destConn.CreateCommand()
        $setIdentityCmd.CommandText = "SET IDENTITY_INSERT [$tabella] ON"
        try {
            $setIdentityCmd.ExecuteNonQuery() | Out-Null
            $identityEnabled = $true
        } catch {
            $identityEnabled = $false
        }
        
        # Costruisci l'INSERT
        $columns = $dataTable.Columns | ForEach-Object { "[$($_.ColumnName)]" }
        $columnList = $columns -join ", "
        
        $copiedRows = 0
        foreach ($row in $dataTable.Rows) {
            $values = @()
            foreach ($col in $dataTable.Columns) {
                $value = $row[$col]
                if ($value -is [DBNull] -or $null -eq $value) {
                    $values += "NULL"
                }
                elseif ($value -is [string]) {
                    $escapedValue = $value.Replace("'", "''")
                    $values += "'$escapedValue'"
                }
                elseif ($value -is [DateTime]) {
                    $values += "'$($value.ToString("yyyy-MM-dd HH:mm:ss"))'"
                }
                elseif ($value -is [bool]) {
                    $values += if ($value) { "1" } else { "0" }
                }
                else {
                    $values += "$value"
                }
            }
            
            $valueList = $values -join ", "
            $insertSQL = "INSERT INTO [$tabella] ($columnList) VALUES ($valueList)"
            
            $insertCmd = $destConn.CreateCommand()
            $insertCmd.CommandText = $insertSQL
            
            try {
                $insertCmd.ExecuteNonQuery() | Out-Null
                $copiedRows++
            }
            catch {
                Write-Host "  ⚠ Errore inserimento: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
        
        # Disabilita IDENTITY_INSERT
        if ($identityEnabled) {
            $setIdentityCmd.CommandText = "SET IDENTITY_INSERT [$tabella] OFF"
            $setIdentityCmd.ExecuteNonQuery() | Out-Null
        }
        
        Write-Host "  ✓ Copiati $copiedRows/$($dataTable.Rows.Count) record" -ForegroundColor Green
    }
    
    $sourceConn.Close()
    $destConn.Close()
    
    Write-Host ""
    Write-Host "=== COPIA COMPLETATA ===" -ForegroundColor Green
    
    # Verifica finale
    Write-Host ""
    Write-Host "Verifica record copiati:" -ForegroundColor Cyan
    $verifyConn = New-Object System.Data.SqlClient.SqlConnection($destConnString)
    $verifyConn.Open()
    
    foreach ($tabella in $tabelle) {
        $cmd = $verifyConn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM [$tabella]"
        $count = $cmd.ExecuteScalar()
        if ($count -gt 0) {
            Write-Host "  $tabella : $count record" -ForegroundColor Green
        }
    }
    
    $verifyConn.Close()
}
catch {
    Write-Host ""
    Write-Host "ERRORE: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Red
}

Write-Host ""
Write-Host "Premi un tasto per continuare..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
