# ===========================================================================
# FIX COMPLETO Dashboard + Re-import Anime
# ===========================================================================
# Esegue in sequenza:
#   1. Fix PLCRealtime - popola tabella per rendere visibili le macchine
#   2. Verifica accessibilità server produzione
#   3. Re-import Anime dal server (se accessibile), altrimenti salta-
# ===========================================================================

param(
    [string]$ProdServer = "192.168.1.230\SQLEXPRESS01",
    [string]$ProdDatabase = "MESManager_Prod",
    [switch]$SkipAnimeImport = $false,
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Continue"  # Continua anche se alcuni step falliscono

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  FIX COMPLETO: Dashboard + Anime                            ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$devConnStr = "Server=localhost\SQLEXPRESS01;Database=MESManager_Dev;Integrated Security=True;TrustServerCertificate=True;"

# ======================================================================
# STEP 1: Fix PLCRealtime
# ======================================================================

Write-Host "[1/3] Fix PLCRealtime per dashboard..." -ForegroundColor Yellow

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($devConnStr)
    $conn.Open()
    
    # Leggi e esegui SQL script
    $sqlScript = Get-Content "C:\Dev\MESManager\scripts\fix-plcrealtime-dashboard.sql" -Raw
    
    # Rimuovi comandi PRINT (non supportati da ADO.NET)
    $sqlCommands = $sqlScript -split '\r?\nGO\r?\n' | Where-Object { $_.Trim() -ne '' }
    
    # Esegui statement a statement (evitando PRINT)
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 60
    
    # Estrai solo i comandi SQL effettivi (DELETE, INSERT, SELECT)
    $cmd.CommandText = @"
-- Fix PLCRealtime
DELETE FROM PLCRealtime WHERE MacchinaId IN (
    SELECT Id FROM Macchine WHERE IndirizzoPLC IS NOT NULL
);

INSERT INTO PLCRealtime (
    Id, MacchinaId, CicliFatti, QuantitaDaProdurre, CicliScarti,
    BarcodeLavorazione, NumeroOperatore, TempoMedioRilevato, TempoMedio,
    Figure, StatoMacchina, QuantitaRaggiunta, DataUltimoAggiornamento
)
SELECT 
    NEWID(), m.Id, 0, 0, 0, 0, NULL, NULL, NULL, NULL,
    'FERMO', 0, GETDATE()
FROM Macchine m
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != '';
"@
    
    $rowsAffected = $cmd.ExecuteNonQuery()
    Write-Host "  ✅ PLCRealtime popolato: $rowsAffected macchine" -ForegroundColor Green
    
    $conn.Close()
    
} catch {
    Write-Host "  ❌ Errore fix PLCRealtime: $_" -ForegroundColor Red
}

# ======================================================================
# STEP 2: Verifica server produzione
# ======================================================================

Write-Host "`n[2/3] Verifica accessibilità server produzione..." -ForegroundColor Yellow

$serverAccessibile = $false

if (-not $SkipAnimeImport) {
    try {
        $prodConnStr = "Server=$ProdServer;Database=$ProdDatabase;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=5"
        $prodConn = New-Object System.Data.SqlClient.SqlConnection($prodConnStr)
        $prodConn.Open()
        
        $cmd = $prodConn.CreateCommand()
        $cmd.CommandText = "SELECT COUNT(*) FROM Anime"
        $prodAnimeCount = [int]$cmd.ExecuteScalar()
        
        $prodConn.Close()
        
        Write-Host "  ✅ Server produzione accessibile" -ForegroundColor Green
        Write-Host "  📊 Anime produzione: $prodAnimeCount" -ForegroundColor Cyan
        $serverAccessibile = $true
        
    } catch {
        Write-Host "  ⚠️ Server produzione NON accessibile: $_" -ForegroundColor Yellow
        Write-Host "  ℹ️  Puoi importare Anime manualmente più tardi" -ForegroundColor Gray
    }
}

# ======================================================================
# STEP 3: Re-import Anime (se server accessibile)
# ======================================================================

if ($serverAccessibile -and -not $SkipAnimeImport) {
    Write-Host "`n[3/3] Re-import Anime da produzione..." -ForegroundColor Yellow
    Write-Host "  ⚠️ ATTENZIONE: Questo può richiedere diversi secondi..." -ForegroundColor Yellow
    
    if ($WhatIf) {
        Write-Host "  [WhatIf] Avrei eseguito:" -ForegroundColor Magenta
Write-Host "    - TRUNCATE TABLE Anime (dev)" -ForegroundColor Magenta
        Write-Host "    - INSERT INTO Anime SELECT * FROM [$ProdServer].[$ProdDatabase].dbo.Anime" -ForegroundColor Magenta
    } else {
        try {
            # Usa BulkCopy per performance
            $prodConn = New-Object System.Data.SqlClient.SqlConnection($prodConnStr)
            $devConn = New-Object System.Data.SqlClient.SqlConnection($devConnStr)
            
            $prodConn.Open()
            $devConn.Open()
            
            # 1. Truncate Anime dev
            $devCmd = $devConn.CreateCommand()
            $devCmd.CommandText = "TRUNCATE TABLE Anime"
            $devCmd.ExecuteNonQuery() | Out-Null
            Write-Host "  📝 Tabella Anime svuotata" -ForegroundColor Gray
            
            # 2. BulkCopy da produzione
            $prodCmd = $prodConn.CreateCommand()
            $prodCmd.CommandText = "SELECT * FROM Anime"
            $prodCmd.CommandTimeout = 120
            $reader = $prodCmd.ExecuteReader()
            
            $bulkCopy = New-Object System.Data.SqlClient.SqlBulkCopy($devConn)
            $bulkCopy.DestinationTableName = "Anime"
            $bulkCopy.BatchSize = 500
            $bulkCopy.BulkCopyTimeout = 120
            
            $bulkCopy.WriteToServer($reader)
            $rowsCopied = $bulkCopy.RowsCopiedCount
            
            $reader.Close()
            $prodConn.Close()
            
            # 3. Verifica
            $devCmd.CommandText = "SELECT COUNT(*) FROM Anime"
            $newCount = [int]$devCmd.ExecuteScalar()
            
            $devConn.Close()
            
            Write-Host "  ✅ Anime importate: $newCount" -ForegroundColor Green
            
        } catch {
            Write-Host "  ❌ Errore import Anime: $_" -ForegroundColor Red
            Write-Host "  ℹ️  Possibile alternativa: esportare/importare manualmente via SSMS" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "`n[3/3] Re-import Anime SALTATO" -ForegroundColor Yellow
    if ($SkipAnimeImport) {
        Write-Host "  ℹ️  Parametro -SkipAnimeImport specificato" -ForegroundColor Gray
    } else {
        Write-Host "  ℹ️  Server produzione non accessibile" -ForegroundColor Gray
    }
}

# ======================================================================
# RIEPILOGO FINALE
# ======================================================================

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  RIEPILOGO FIX                                               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($devConnStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT 
    'Anime' AS Tabella,
    COUNT(*) AS Record
FROM Anime
UNION ALL
SELECT 
    'PLCRealtime',
    COUNT(*)
FROM PLCRealtime
UNION ALL
SELECT 
    'Macchine con IP',
    COUNT(*)
FROM Macchine
WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != ''
"@
    
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        $tabella = $reader[0]
        $count = $reader[1]
        
        $color = "White"
        if ($tabella -eq "Anime" -and $count -lt 700) { $color = "Yellow" }
        if ($tabella -eq "PLCRealtime" -and $count -lt 6) { $color = "Red" }
        
        Write-Host "  $tabella : $count" -ForegroundColor $color
    }
    $reader.Close()
    $conn.Close()
    
} catch {
    Write-Host "  ❌ Errore verifica finale: $_" -ForegroundColor Red
}

Write-Host "`n✅ FIX COMPLETATO!" -ForegroundColor Green -BackgroundColor Black
Write-Host "   Riavviare il server e testare le dashboard:`n" -ForegroundColor White
Write-Host "   http://localhost:5156/produzione/dashboard" -ForegroundColor Cyan
Write-Host "   http://localhost:5156/produzione/plc-realtime`n" -ForegroundColor Cyan
