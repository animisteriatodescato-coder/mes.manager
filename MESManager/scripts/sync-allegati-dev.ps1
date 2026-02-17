# ═══════════════════════════════════════════════════════════════════════════
# Script: sync-allegati-dev.ps1
# Scopo: Sincronizza tabella Allegati da PROD → DEV per testing locale
# Uso: .\scripts\sync-allegati-dev.ps1
# ═══════════════════════════════════════════════════════════════════════════

param(
    [switch]$SkipFiles  # Skip file copy (solo database)
)

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   SYNC ALLEGATI PROD → DEV (Solo Lettura)               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Configurazione
$prodServer = "192.168.1.230\SQLEXPRESS01"
$prodDb = "Gantt"
$prodUser = "FAB"  
$prodPass = "password.123"

$devServer = "localhost\SQLEXPRESS01"
$devDb = "MESManager_Dev"

$prodFilesPath = "\\192.168.1.230\Dati\Documenti"
$devFilesPath = "C:\DevData\Documenti"

# ═══════════════════════════════════════════════════════════════════════════
# STEP 1: Verifica connessione PROD
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "📡 [1/4] Connessione a database PROD..." -ForegroundColor Yellow

$prodConnString = "Server=$prodServer;Database=$prodDb;User Id=$prodUser;Password=$prodPass;TrustServerCertificate=True;"
$prodConn = New-Object System.Data.SqlClient.SqlConnection($prodConnString)

try {
    $prodConn.Open()
    
    # Conta record Allegati in PROD
    $cmd = $prodConn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[Allegati] WHERE Archivio = 'ARTICO'"
    $countProd = $cmd.ExecuteScalar()
    
    Write-Host "   ✅ Connesso a PROD - Record Allegati: $countProd" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ ERRORE connessione PROD: $_" -ForegroundColor Red
    Write-Host "`n⚠️  SOLUZIONE: Esegui questo script dal server PROD (192.168.1.230)" -ForegroundColor Yellow
    Write-Host "   oppure chiedi permessi READ per utente FAB sul database Gantt`n" -ForegroundColor Yellow
    exit 1
} finally {
    if ($prodConn.State -eq 'Open') { $prodConn.Close() }
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 2: Verifica connessione DEV
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "`n📡 [2/4] Connessione a database DEV..." -ForegroundColor Yellow

$devConnString = "Server=$devServer;Database=$devDb;Integrated Security=True;TrustServerCertificate=True;"
$devConn = New-Object System.Data.SqlClient.SqlConnection($devConnString)

try {
    $devConn.Open()
    Write-Host "   ✅ Connesso a DEV" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ ERRORE connessione DEV: $_" -ForegroundColor Red
    exit 1
} finally {
    if ($devConn.State -eq 'Open') { $devConn.Close() }
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 3: Crea tabella Allegati in DEV (se non esiste)
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "`n🔧 [3/4] Preparazione tabella Allegati in DEV..." -ForegroundColor Yellow

$devConn.Open()
try {
    $createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Allegati')
BEGIN
    CREATE TABLE [dbo].[Allegati] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Archivio] NVARCHAR(50) NOT NULL,
        [IdArchivio] INT NOT NULL,
        [Allegato] NVARCHAR(500) NOT NULL,
        [DescrizioneAllegato] NVARCHAR(255) NULL,
        [Priorita] INT NULL DEFAULT 0
    );
    
    CREATE INDEX IX_Allegati_Archivio ON [dbo].[Allegati](Archivio, IdArchivio);
    
    PRINT 'Tabella Allegati creata con successo';
END
ELSE
BEGIN
    -- Svuota tabella esistente
    TRUNCATE TABLE [dbo].[Allegati];
    PRINT 'Tabella Allegati pulita';
END
"@
    
    $cmd = $devConn.CreateCommand()
    $cmd.CommandText = $createTableSql
    $cmd.ExecuteNonQuery() | Out-Null
    
    Write-Host "   ✅ Tabella Allegati pronta in DEV" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ ERRORE creazione tabella: $_" -ForegroundColor Red
    exit 1
} finally {
    if ($devConn.State -eq 'Open') { $devConn.Close() }
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 4: Sync dati PROD → DEV
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "`n📋 [4/4] Sincronizzazione dati PROD → DEV..." -ForegroundColor Yellow

$prodConn.Open()
$devConn.Open()

try {
    # Leggi tutti gli allegati da PROD
    $selectCmd = $prodConn.CreateCommand()
    $selectCmd.CommandText = "SELECT Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita FROM [dbo].[Allegati] WHERE Archivio = 'ARTICO' ORDER BY IdArchivio, Priorita"
    $reader = $selectCmd.ExecuteReader()
    
    $insertedCount = 0
    
    # Prepara comando INSERT per DEV
    $insertCmd = $devConn.CreateCommand()
    $insertCmd.CommandText = @"
INSERT INTO [dbo].[Allegati] (Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita)
VALUES (@Archivio, @IdArchivio, @Allegato, @Descrizione, @Priorita)
"@
    $insertCmd.Parameters.Add("@Archivio", [System.Data.SqlDbType]::NVarChar, 50) | Out-Null
    $insertCmd.Parameters.Add("@IdArchivio", [System.Data.SqlDbType]::Int) | Out-Null
    $insertCmd.Parameters.Add("@Allegato", [System.Data.SqlDbType]::NVarChar, 500) | Out-Null
    $insertCmd.Parameters.Add("@Descrizione", [System.Data.SqlDbType]::NVarChar, 255) | Out-Null
    $insertCmd.Parameters.Add("@Priorita", [System.Data.SqlDbType]::Int) | Out-Null
    
    while ($reader.Read()) {
        $insertCmd.Parameters["@Archivio"].Value = $reader["Archivio"]
        $insertCmd.Parameters["@IdArchivio"].Value = $reader["IdArchivio"]
        $insertCmd.Parameters["@Allegato"].Value = $reader["Allegato"]
        $insertCmd.Parameters["@Descrizione"].Value = if ($reader.IsDBNull(3)) {[DBNull]::Value} else {$reader["DescrizioneAllegato"]}
        $insertCmd.Parameters["@Priorita"].Value = if ($reader.IsDBNull(4)) {0} else {$reader["Priorita"]}
        
        $insertCmd.ExecuteNonQuery() | Out-Null
        $insertedCount++
        
        if ($insertedCount % 100 -eq 0) {
            Write-Host "   Sincronizzati $insertedCount record..." -ForegroundColor Gray
        }
    }
    
    $reader.Close()
    
    Write-Host "   ✅ Sincronizzati $insertedCount allegati in DEV" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ ERRORE durante sync: $_" -ForegroundColor Red
    exit 1
} finally {
    if ($prodConn.State -eq 'Open') { $prodConn.Close() }
    if ($devConn.State -eq 'Open') { $devConn.Close() }
}

# ═══════════════════════════════════════════════════════════════════════════
# STEP 5: (OPZIONALE) Sync file fisici
# ═══════════════════════════════════════════════════════════════════════════

if (-not $SkipFiles) {
    Write-Host "`n📁 [OPZIONALE] Sincronizzazione file fisici..." -ForegroundColor Yellow
    Write-Host "   ⚠️  Questo può richiedere molto tempo!" -ForegroundColor Yellow
    Write-Host "   Path: $prodFilesPath → $devFilesPath" -ForegroundColor Gray
    
    $confirm = Read-Host "`n   Copiare file fisici? (s/N)"
    
    if ($confirm -eq 's' -or $confirm -eq 'S') {
        try {
            if (!(Test-Path $devFilesPath)) {
                New-Item -Path $devFilesPath -ItemType Directory -Force | Out-Null
            }
            
            Write-Host "   Avvio robocopy..." -ForegroundColor Gray
            robocopy "$prodFilesPath" "$devFilesPath" /MIR /R:2 /W:5 /NP /NDL /NFL /XO
            
            Write-Host "   ✅ File sincronizzati" -ForegroundColor Green
            
        } catch {
            Write-Host "   ❌ ERRORE copia file: $_" -ForegroundColor Red
            Write-Host "   Puoi continuare usando UNC path: \\192.168.1.230\Dati" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⏭️  Sync file saltata - Usa UNC path remoto" -ForegroundColor Yellow
    }
}

# ═══════════════════════════════════════════════════════════════════════════
# RIEPILOGO
# ═══════════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   ✅ SYNC COMPLETATA CON SUCCESSO                        ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n📋 Prossimi passi:" -ForegroundColor Cyan
Write-Host "   1. Modifica appsettings.Database.Development.json:" -ForegroundColor White
Write-Host '      "AllegatiDb": null  (rimuovi o commenta la riga)' -ForegroundColor Gray
Write-Host "   2. Riavvia il server MESManager.Web" -ForegroundColor White
Write-Host "   3. Test: http://localhost:5156/api/AllegatiAnima/1`n" -ForegroundColor White

Write-Host "💡 Suggerimento: Riesegui questo script periodicamente per aggiornare i dati.`n" -ForegroundColor Yellow
