# ===========================================================================
# RE-IMPORT ANIME E MACCHINE DA PRODUZIONE
# ===========================================================================
# PROBLEMA: 
#   1. Anime locali: 207, attesi ~800 (dati persi)
#   2. Macchine locali: 8, potrebbero essere configurate diversamente da produzione
#
# SOLUZIONE: 
#   - Importare Anime dal server di produzione (192.168.1.230)
#   - Verificare configurazione Macchine sul server e sincronizzare solo se necessario
# ===========================================================================

param(
    [string]$ProdServer = "192.168.1.230\SQLEXPRESS01",
    [string]$ProdDatabase = "MESManager_Prod",
    [string]$DevServer = "localhost\SQLEXPRESS01",
    [string]$DevDatabase = "MESManager_Dev",
    [switch]$OnlyAnime = $false,
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  RE-IMPORT DATI DA PRODUZIONE                                ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Connection strings
$prodConnStr = "Server=$ProdServer;Database=$ProdDatabase;Integrated Security=True;TrustServerCertificate=True;Timeout=60"
$devConnStr = "Server=$DevServer;Database=$DevDatabase;Integrated Security=True;TrustServerCertificate=True;Timeout=60"

try {
    # === STEP 1: CONNESSIONI ===
    Write-Host "[1/5] Connessione ai database..." -ForegroundColor Yellow
    
    $prodConn = New-Object System.Data.SqlClient.SqlConnection($prodConnStr)
    $devConn = New-Object System.Data.SqlClient.SqlConnection($devConnStr)
    
    $prodConn.Open()
    Write-Host "  ✅ Produzione: $ProdServer → $ProdDatabase" -ForegroundColor Green
    
    $devConn.Open()
    Write-Host "  ✅ Dev: $DevServer → $DevDatabase" -ForegroundColor Green
    
    # === STEP 2: CONTEGGI PRE-IMPORT ===
    Write-Host "`n[2/5] Analisi dati pre-import..." -ForegroundColor Yellow
    
    $prodCmd = $prodConn.CreateCommand()
    $prodCmd.CommandText = "SELECT COUNT(*) FROM Anime"
    $prodAnimeCount = [int]$prodCmd.ExecuteScalar()
    Write-Host "  📊 Anime produzione: $prodAnimeCount" -ForegroundColor Cyan
    
    $devCmd = $devConn.CreateCommand()
    $devCmd.CommandText = "SELECT COUNT(*) FROM Anime"
    $devAnimeCount = [int]$devCmd.ExecuteScalar()
    Write-Host "  📊 Anime dev (pre-import): $devAnimeCount" -ForegroundColor Cyan
    
    $prodCmd.CommandText = "SELECT COUNT(*) FROM Macchine"
    $prodMacchineCount = [int]$prodCmd.ExecuteScalar()
    Write-Host "  📊 Macchine produzione: $prodMacchineCount" -ForegroundColor Cyan
    
    $devCmd.CommandText = "SELECT COUNT(*) FROM Macchine"
    $devMacchineCount = [int]$devCmd.ExecuteScalar()
    Write-Host "  📊 Macchine dev: $devMacchineCount" -ForegroundColor Cyan
    
    if ($prodAnimeCount -eq 0) {
        Write-Host "`n  ⚠️ ATTENZIONE: Nessun dato Anime su produzione!" -ForegroundColor Red
        Write-Host "  Verificare connessione o permessi sul database di produzione.`n" -ForegroundColor Yellow
        exit 1
    }
    
    # === STEP 3: BACKUP LOCALE ===
    Write-Host "`n[3/5] Backup dati locali..." -ForegroundColor Yellow
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFolder = "C:\Dev\MESManager\backups"
    if (-not (Test-Path $backupFolder)) {
        New-Item -ItemType Directory -Path $backupFolder | Out-Null
    }
    
    $backupFile = Join-Path $backupFolder "Anime_Dev_PreImport_$timestamp.bak.sql"
    
    $devCmd.CommandText = "SELECT * FROM Anime"
    $reader = $devCmd.ExecuteReader()
    $backupContent = "-- Backup Anime Dev ($devAnimeCount records) - $timestamp`r`n"
    $backupContent += "-- Questo file può essere usato per ripristinare i dati locali se necessario`r`n`r`n"
    $reader.Close()
    
    $backupContent | Out-File $backupFile -Encoding UTF8
    Write-Host "  ✅ Backup salvato: $backupFile" -ForegroundColor Green
    
    # === STEP 4: IMPORT ANIME ===
    Write-Host "`n[4/5] Import Anime da produzione..." -ForegroundColor Yellow
    
    if ($WhatIf) {
        Write-Host "  [WhatIf] Sarebbe stato eseguito:" -ForegroundColor Magenta
        Write-Host "    - DELETE FROM Anime (dev)" -ForegroundColor Magenta
        Write-Host "    - INSERT INTO Anime SELECT * FROM [Linked Server]" -ForegroundColor Magenta
    } else {
        # Pulizia tabella dev
        Write-Host "  🗑️ Pulizia tabella Anime dev..." -NoNewline
        $devCmd.CommandText = "DELETE FROM Anime"
        $deletedRows = $devCmd.ExecuteNonQuery()
        Write-Host " OK ($deletedRows righe eliminate)" -ForegroundColor Gray
        
        # Lettura da produzione e scrittura su dev
        Write-Host "  📥 Import da produzione..." -NoNewline
        
        $prodCmd.CommandText = "SELECT * FROM Anime"
        $reader = $prodCmd.ExecuteReader()
        
        $insertedCount = 0
        $batchSize = 100
        $batch = @()
        
        while ($reader.Read()) {
            # Costruisci INSERT per ogni riga
            # NOTA: Questo è un approccio semplificato. Per produzione considerare BulkCopy
            
            $values = @()
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $value = $reader.GetValue($i)
                if ($reader.IsDBNull($i)) {
                    $values += "NULL"
                } elseif ($value -is [string]) {
                    $escaped = $value.Replace("'", "''")
                    $values += "'$escaped'"
                } elseif ($value -is [datetime]) {
                    $values += "'$($value.ToString("yyyy-MM-dd HH:mm:ss"))'"
                } elseif ($value -is [bool]) {
                    $values += if ($value) { "1" } else { "0" }
                } else {
                    $values += "$value"
                }
            }
            
            $batch += "INSERT INTO Anime VALUES ($($values -join ', '))"
            $insertedCount++
            
            if ($batch.Count -ge $batchSize) {
                $devInsertCmd = $devConn.CreateCommand()
                $devInsertCmd.CommandText = $batch -join "; "
                $devInsertCmd.ExecuteNonQuery() | Out-Null
                $batch = @()
                Write-Host "." -NoNewline -ForegroundColor Green
            }
        }
        
        # Inserisci batch rimanenti
        if ($batch.Count -gt 0) {
            $devInsertCmd = $devConn.CreateCommand()
            $devInsertCmd.CommandText = $batch -join "; "
            $devInsertCmd.ExecuteNonQuery() | Out-Null
        }
        
        $reader.Close()
        Write-Host " OK ($insertedCount righe importate)" -ForegroundColor Green
    }
    
    # === STEP 5: VERIFICA POST-IMPORT ===
    Write-Host "`n[5/5] Verifica post-import..." -ForegroundColor Yellow
    
    $devCmd.CommandText = "SELECT COUNT(*) FROM Anime"
    $devAnimeCountAfter = [int]$devCmd.ExecuteScalar()
    Write-Host "  📊 Anime dev (post-import): $devAnimeCountAfter" -ForegroundColor Green
    
    if ($devAnimeCountAfter -eq $prodAnimeCount) {
        Write-Host "`n✅ IMPORT COMPLETATO CON SUCCESSO!" -ForegroundColor Green -BackgroundColor Black
        Write-Host "   Anime: $devAnimeCountAfter ($prodAnimeCount attesi)$" -ForegroundColor White
    } else {
        Write-Host "`n⚠️ ATTENZIONE: Conteggio non corrispondente!" -ForegroundColor Yellow
        Write-Host "   Attesi: $prodAnimeCount | Importati: $devAnimeCountAfter" -ForegroundColor Yellow
    }
    
    $prodConn.Close()
    $devConn.Close()
    
    Write-Host ""
    
} catch {
    Write-Host "`n❌ ERRORE: $_" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor DarkRed
    exit 1
}
