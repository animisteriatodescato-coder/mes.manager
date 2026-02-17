# Verifica macchine con/senza IP (problema dashboard)

param(
    [string]$Server = "localhost\SQLEXPRESS01",
    [string]$Database = "MESManager_Dev"
)

$ErrorActionPreference = "Stop"

$connStr = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    Write-Host "`n=== ANALISI MACCHINE E DASHBOARD ===" -ForegroundColor Cyan
    Write-Host "Database: $Database`n" -ForegroundColor Yellow
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT 
    Codice, 
    Nome, 
    AttivaInGantt,
    IndirizzoPLC,
    OrdineVisualizazione
FROM Macchine 
ORDER BY Codice
"@
    
    $reader = $cmd.ExecuteReader()
    
    $withIP = 0
    $withoutIP = 0
    $dettaglio = @()
    
    while ($reader.Read()) {
        $obj = [PSCustomObject]@{
            Codice = $reader["Codice"]
            Nome = $reader["Nome"]
            Gantt = $reader["AttivaInGantt"]
            IP = if ($reader.IsDBNull(3)) { $null } else { $reader["IndirizzoPLC"] }
            Ordine = $reader["OrdineVisualizazione"]
        }
        
        if ($obj.IP) {
            $withIP++
        } else {
            $withoutIP++
        }
        
        $dettaglio += $obj
    }
    
    $reader.Close()
    $conn.Close()
    
    # Mostra dettaglio
    Write-Host "MACCHINE CON IP (visibili in Dashboard Produzione/PLC):" -ForegroundColor Green
    if ($withIP -eq 0) {
        Write-Host "  ❌ NESSUNA MACCHINA CON IP CONFIGURATO!" -ForegroundColor Red -BackgroundColor Yellow
    } else {
        $dettaglio | Where-Object { $_.IP } | ForEach-Object {
            Write-Host "  ✅ $($_.Codice) - $($_.Nome) | IP: $($_.IP)" -ForegroundColor Green
        }
    }
    
    Write-Host "`nMACCHINE SENZA IP (NON visibili in dashboard):" -ForegroundColor Yellow
    if ($withoutIP -eq 0) {
        Write-Host "  Nessuna" -ForegroundColor Gray
    } else {
        $dettaglio | Where-Object { -not $_.IP } | ForEach-Object {
            Write-Host "  ❌ $($_.Codice) - $($_.Nome) | Gantt: $($_.Gantt)" -ForegroundColor Red
        }
    }
    
    Write-Host "`n=== RIEPILOGO ===" -ForegroundColor Cyan
    Write-Host "Totale macchine: $($dettaglio.Count)" -ForegroundColor White
    Write-Host "Con IP (dashboard OK): $withIP" -ForegroundColor $(if($withIP -gt 0){"Green"}else{"Red"})
    Write-Host "Senza IP (dashboard KO): $withoutIP" -ForegroundColor Yellow
    
    if ($withIP -eq 0) {
        Write-Host "`n⚠️⚠️⚠️ PROBLEMA CRITICO ⚠️⚠️⚠️" -ForegroundColor Red -BackgroundColor Yellow
        Write-Host "Nessuna macchina ha IndirizzoPLC configurato!" -ForegroundColor Red
        Write-Host "Le dashboard Produzione e PLC Realtime saranno VUOTE!" -ForegroundColor Red
        Write-Host "`nSOLUZIONE: Ripristinare gli indirizzi IP dalle macchine duplicate eliminate.`n" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "`n[ERRORE] $_`n" -ForegroundColor Red
    exit 1
}
