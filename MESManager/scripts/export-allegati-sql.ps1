<#
.SYNOPSIS
    Esporta dati Allegati da PROD in formato SQL INSERT

.DESCRIPTION
    Script da eseguire SUL SERVER per estrarre dati dalla tabella Allegati (Gantt DB)
    e generare un file SQL importabile in DEV

.PARAMETER OutputFile
    Path del file SQL di output (default: allegati-export.sql)

.EXAMPLE
    .\export-allegati-sql.ps1
    .\export-allegati-sql.ps1 -OutputFile "C:\backup\allegati.sql"
#>

param(
    [string]$OutputFile = "allegati-export.sql",
    [int]$Limit = 0  # 0 = tutti i record, >0 = limita per testing
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   EXPORT ALLEGATI → SQL FILE                             ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Configurazione
$prodServer = "localhost\SQLEXPRESS01"  # Da eseguire sul server, quindi localhost
$prodDb = "Gantt"

try {
    Write-Host "📡 [1/3] Connessione a $prodDb..." -ForegroundColor Yellow
    
    $connString = "Server=$prodServer;Database=$prodDb;Integrated Security=True;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    
    Write-Host "   ✅ Connesso a $prodDb" -ForegroundColor Green
    
    # Query estrazione dati
    Write-Host "`n📊 [2/3] Estrazione dati Allegati (Archivio='ARTICO')..." -ForegroundColor Yellow
    
    $query = @"
SELECT 
    [Archivio],
    [IdArchivio],
    [Allegato],
    [DescrizioneAllegato],
    [Priorita]
FROM [dbo].[Allegati]
WHERE [Archivio] = 'ARTICO'
ORDER BY [IdArchivio], [Priorita]
"@

    if ($Limit -gt 0) {
        $query = "SELECT TOP $Limit " + $query.Substring(7)
    }

    $cmd = New-Object System.Data.SqlClient.SqlCommand($query, $conn)
    $reader = $cmd.ExecuteReader()
    
    # Genera SQL
    $sqlContent = @"
-- =====================================================
-- EXPORT ALLEGATI da Gantt DB (PROD)
-- Data export: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- =====================================================

-- Pulisci dati esistenti (opzionale - decommenta se necessario)
-- DELETE FROM [dbo].[Allegati] WHERE [Archivio] = 'ARTICO';

-- Disabilita controllo identità per permettere INSERT espliciti di ID (se necessario)
-- SET IDENTITY_INSERT [dbo].[Allegati] ON;

-- Inserimenti
"@

    $count = 0
    $sqlInserts = @()
    
    while ($reader.Read()) {
        $count++
        
        # Escape singole quote in stringhe SQL
        $archivio = $reader["Archivio"].ToString().Replace("'", "''")
        $idArchivio = $reader["IdArchivio"]
        $allegato = $reader["Allegato"].ToString().Replace("'", "''")
        $descrizione = if ($reader["DescrizioneAllegato"] -is [DBNull]) { "NULL" } else { "'" + $reader["DescrizioneAllegato"].ToString().Replace("'", "''") + "'" }
        $priorita = if ($reader["Priorita"] -is [DBNull]) { "NULL" } else { $reader["Priorita"].ToString() }
        
        $insert = "INSERT INTO [dbo].[Allegati] ([Archivio], [IdArchivio], [Allegato], [DescrizioneAllegato], [Priorita]) VALUES ('$archivio', $idArchivio, '$allegato', $descrizione, $priorita);"
        $sqlInserts += $insert
        
        if ($count % 100 -eq 0) {
            Write-Host "   Processati $count record..." -ForegroundColor Gray
        }
    }
    
    $reader.Close()
    $conn.Close()
    
    Write-Host "   ✅ Estratti $count record" -ForegroundColor Green
    
    # Scrivi file SQL
    Write-Host "`n💾 [3/3] Scrittura file SQL..." -ForegroundColor Yellow
    
    $sqlContent += "`n" + ($sqlInserts -join "`n")
    $sqlContent += @"


-- Riabilita controllo identità (se disabilitato sopra)
-- SET IDENTITY_INSERT [dbo].[Allegati] OFF;

-- =====================================================
-- RIEPILOGO: $count record esportati
-- =====================================================
SELECT COUNT(*) as TotaleInserito FROM [dbo].[Allegati] WHERE [Archivio] = 'ARTICO';
"@
    
    $sqlContent | Set-Content -Path $OutputFile -Encoding UTF8
    
    $fullPath = (Resolve-Path $OutputFile).Path
    Write-Host "   ✅ File creato: $fullPath" -ForegroundColor Green
    Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ EXPORT COMPLETATO" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan
    
    Write-Host "📋 PROSSIMI PASSI:" -ForegroundColor Yellow
    Write-Host "   1. Copia file su macchina DEV" -ForegroundColor White
    Write-Host "   2. Esegui: sqlcmd -S localhost\SQLEXPRESS01 -d MESManager_Dev -i `"$OutputFile`"" -ForegroundColor White
    Write-Host "      OPPURE esegui il contenuto del file in SQL Server Management Studio`n" -ForegroundColor White
    
    Write-Host "📊 Record totali: $count" -ForegroundColor Cyan
    
    exit 0
    
} catch {
    Write-Host "`n❌ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`n⚠️  NOTA: Questo script deve essere eseguito SUL SERVER (192.168.1.230)" -ForegroundColor Yellow
    Write-Host "   dove ha accesso locale al database Gantt`n" -ForegroundColor Yellow
    exit 1
}
