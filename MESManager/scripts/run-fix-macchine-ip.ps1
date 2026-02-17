# Esegue fix degli IP delle macchine per ripristinare visibilità dashboard

param(
    [string]$Server = "localhost\SQLEXPRESS01",
    [string]$Database = "MESManager_Dev"
)

$ErrorActionPreference = "Stop"

$connStr = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"

# Leggi script SQL
$sqlScript = Get-Content "C:\Dev\MESManager\scripts\fix-macchine-ip-dashboard.sql" -Raw

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    Write-Host "`n🔧 ESECUZIONE FIX INDIRIZZI IP MACCHINE`n" -ForegroundColor Cyan
    
    # Rimuovi istruzioni PRINT e parsing per output
    $commands = $sqlScript -split "PRINT '.*?'"
    
    # Stato PRE-fix
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Codice, Nome, IndirizzoPLC FROM Macchine ORDER BY Codice"
    
    Write-Host "=== STATO PRE-FIX ===" -ForegroundColor Yellow
    $reader = $cmd.ExecuteReader()
    $conIP = 0
    $senzaIP = 0
    while ($reader.Read()) {
        $codice = $reader["Codice"]
        $nome = $reader["Nome"]
        $ip = if ($reader.IsDBNull(2)) { $null } else { $reader["IndirizzoPLC"] }
        
        if ($ip) {
            Write-Host "  ✅ $codice - $nome | IP: $ip" -ForegroundColor Green
            $conIP++
        } else {
            Write-Host "  ❌ $codice - $nome | IP: NESSUNO (invisibile dashboard)" -ForegroundColor Red
            $senzaIP++
        }
    }
    $reader.Close()
    
    Write-Host "`nRiepilogo PRE-fix:" -ForegroundColor Yellow
    Write-Host "  Con IP: $conIP" -ForegroundColor Green
    Write-Host "  Senza IP (da fixare): $senzaIP" -ForegroundColor Red
    
    # Esegui UPDATE
    Write-Host "`n=== APPLICAZIONE FIX ===" -ForegroundColor Cyan
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
UPDATE Macchine
SET IndirizzoPLC = CASE Codice
    WHEN 'M002' THEN '192.168.17.2'
    WHEN 'M003' THEN '192.168.17.3'
    WHEN 'M004' THEN '192.168.17.4'
    WHEN 'M005' THEN '192.168.17.5'
    WHEN 'M006' THEN '192.168.17.6'
    WHEN 'M007' THEN '192.168.17.7'
    WHEN 'M008' THEN '192.168.17.8'
    WHEN 'M009' THEN '192.168.17.9'
    WHEN 'M010' THEN '192.168.17.10'
    ELSE IndirizzoPLC
END
WHERE (IndirizzoPLC IS NULL OR IndirizzoPLC = '')
  AND Codice IN ('M002', 'M003', 'M004', 'M005', 'M006', 'M007', 'M008', 'M009', 'M010')
"@
    
    $rowsAffected = $cmd.ExecuteNonQuery()
    Write-Host "✅ Macchine aggiornate: $rowsAffected" -ForegroundColor Green
    
    # Stato POST-fix
    Write-Host "`n=== STATO POST-FIX ===" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Codice, Nome, IndirizzoPLC, AttivaInGantt FROM Macchine ORDER BY Codice"
    $reader = $cmd.ExecuteReader()
    
    $visibiliDashboard = 0
    while ($reader.Read()) {
        $codice = $reader["Codice"]
        $nome = $reader["Nome"]
        $ip = if ($reader.IsDBNull(2)) { $null } else { $reader["IndirizzoPLC"] }
        $gantt = $reader["AttivaInGantt"]
        
        if ($ip) {
            Write-Host "  ✅ $codice - $nome | IP: $ip | Gantt: $gantt" -ForegroundColor Green
            $visibiliDashboard++
        } else {
            Write-Host "  ⚠️ $codice - $nome | IP: NESSUNO | Gantt: $gantt" -ForegroundColor Yellow
        }
    }
    $reader.Close()
    $conn.Close()
    
    Write-Host "`n=== RIEPILOGO FINALE ===" -ForegroundColor Cyan
    Write-Host "Macchine visibili in Dashboard Produzione/PLC: $visibiliDashboard" -ForegroundColor Green
    
    if ($visibiliDashboard -ge 6) {
        Write-Host "`n✅ FIX COMPLETATO CON SUCCESSO!" -ForegroundColor Green -BackgroundColor Black
        Write-Host "Le dashboard ora dovrebbero mostrare le macchine correttamente.`n" -ForegroundColor White
    } else {
        Write-Host "`n⚠️ ATTENZIONE: Solo $visibiliDashboard macchine visibili (atteso almeno 6)" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "`n❌ ERRORE: $_`n" -ForegroundColor Red
    exit 1
}
