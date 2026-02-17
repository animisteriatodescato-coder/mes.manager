# Verifica problema dashboard scollegate - Analisi PLCRealtime

$connStr = "Server=localhost\SQLEXPRESS01;Database=MESManager_Dev;Trusted_Connection=True;TrustServerCertificate=True;"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  DIAGNOSI PROBLEMA DASHBOARD - Analisi PLCRealtime          ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    # 1. Conteggio Macchine
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM Macchine WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != ''"
    $macchineConIP = [int]$cmd.ExecuteScalar()
    Write-Host "📊 Macchine con IndirizzoPLC configurato: $macchineConIP" -ForegroundColor Yellow
    
    # 2. Conteggio PLCRealtime
    $cmd.CommandText = "SELECT COUNT(*) FROM PLCRealtime"
    $plcRealtimeCount = [int]$cmd.ExecuteScalar()
    Write-Host "📊 Record in PLCRealtime: $plcRealtimeCount" -ForegroundColor Yellow
    
    if ($plcRealtimeCount -eq 0) {
        Write-Host "`n⚠️⚠️⚠️ PROBLEMA TROVATO! ⚠️⚠️⚠️" -ForegroundColor Red -BackgroundColor Yellow
        Write-Host "La tabella PLCRealtime è VUOTA!" -ForegroundColor Red
        Write-Host "Le dashboard Produzione e PLC Realtime non possono mostrare dati." -ForegroundColor Red
        Write-Host "`nCAUSA PROBABILE:" -ForegroundColor Yellow
        Write-Host "  - PlcSync service non è in esecuzione" -ForegroundColor White
        Write-Host "  - PlcSync service non riesce a connettersi ai PLC" -ForegroundColor White
        Write-Host "  - PlcSync service non ha mai eseguito il polling`n" -ForegroundColor White
        $conn.Close()
        exit
    }
    
    # 3. Dettaglio PLCRealtime con Macchine
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  DETTAGLIO PLCRealtime con JOIN Macchine                    ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    $cmd.CommandText = @"
SELECT 
    m.Codice,
    m.Nome,
    m.IndirizzoPLC,
    p.CicliFatti,
    p.StatoMacchina,
    p.DataUltimoAggiornamento,
    DATEDIFF(MINUTE, p.DataUltimoAggiornamento, GETDATE()) AS MinutiDaAggiornamento,
    CASE 
        WHEN DATEDIFF(MINUTE, p.DataUltimoAggiornamento, GETDATE()) > 2 THEN 'NON CONNESSA'
        ELSE 'CONNESSA'
    END AS StatoConnessione
FROM Macchine m
LEFT JOIN PLCRealtime p ON p.MacchinaId = m.Id
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != ''
ORDER BY m.Codice
"@
    
    $reader = $cmd.ExecuteReader()
    
    $connesse = 0
    $nonConnesse = 0
    $senzaDati = 0
    
    while ($reader.Read()) {
        $codice = $reader["Codice"]
        $nome = $reader["Nome"]
        $ip = $reader["IndirizzoPLC"]
        
        if ($reader.IsDBNull(3)) {
            # Nessun record in PLCRealtime per questa macchina
            Write-Host "❌ $codice - $nome | IP: $ip | " -NoNewline
            Write-Host "NESSUN DATO IN PLCRealtime" -ForegroundColor Red -BackgroundColor Yellow
            $senzaDati++
        } else {
            $cicli = $reader["CicliFatti"]
            $stato = $reader["StatoMacchina"]
            $ultimoAgg = $reader["DataUltimoAggiornamento"]
            $minuti = [int]$reader["MinutiDaAggiornamento"]
            $statoConn = $reader["StatoConnessione"]
            
            if ($statoConn -eq "CONNESSA") {
                Write-Host "✅ $codice - $nome | IP: $ip | Cicli: $cicli |Stato: $stato" -NoNewline  -ForegroundColor Green
                Write-Host " | Agg: $minuti min fa" -ForegroundColor Gray
                $connesse++
            } else {
                Write-Host "⚠️ $codice - $nome | IP: $ip | Cicli: $cicli | Stato: $stato" -NoNewline -ForegroundColor Yellow
                Write-Host " | Agg: $minuti min fa (TIMEOUT!)" -ForegroundColor Red
                $nonConnesse++
            }
        }
    }
    
    $reader.Close()
    
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  RIEPILOGO PROBLEMA                                          ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    Write-Host "Macchine con IP configurato: $macchineConIP" -ForegroundColor White
    Write-Host "Macchine CONNESSE (dati < 2 min): " -NoNewline
    Write-Host "$connesse" -ForegroundColor $(if($connesse -gt 0){"Green"}else{"Red"})
    Write-Host "Macchine NON CONNESSE (dati > 2 min): " -NoNewline
    Write-Host "$nonConnesse" -ForegroundColor $(if($nonConnesse -gt 0){"Yellow"}else{"Green"})
    Write-Host "Macchine SENZA DATI (no record PLCRealtime): " -NoNewline
    Write-Host "$senzaDati" -ForegroundColor $(if($senzaDati -gt 0){"Red"}else{"Green"})
    
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  SOLUZIONE                                                   ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    if ($senzaDati -gt 0) {
        Write-Host "⚠️ PROBLEMA: $senzaDati macchine non hanno record in PLCRealtime" -ForegroundColor Red
        Write-Host "`nPossibili cause:" -ForegroundColor Yellow
        Write-Host "  1. PlcSync service non è mai stato avviato" -ForegroundColor White
        Write-Host "  2. PlcSync service non sta facendo il polling di queste macchine" -ForegroundColor White
        Write-Host "  3. I file JSON di configurazione PlcSync sono incompleti`n" -ForegroundColor White
        
        Write-Host "Azioni consigliate:" -ForegroundColor Green
        Write-Host "  1. Verificare stato PlcSync: SELECT * FROM PlcServiceStatus" -ForegroundColor White
        Write-Host "  2. Controllare log PlcSync: SELECT TOP 20 * FROM PlcSyncLogs ORDER BY Id DESC" -ForegroundColor White
        Write-Host "  3. Avviare PlcSync se non attivo" -ForegroundColor White
        Write-Host "  4. In ambiente DEV, creare record PLCRealtime di test con:" -ForegroundColor White
        Write-Host "     INSERT INTO PLCRealtime (MacchinaId, ...) VALUES (...)`n" -ForegroundColor Gray
    } elseif ($nonConnesse -gt 0) {
        Write-Host "⚠️ PROBLEMA: $nonConnesse macchine hanno dati vecchi (>2 min)" -ForegroundColor Yellow
        Write-Host "`nCAUSA: PlcSync non sta aggiornando i dati o PLC non risponde`n" -ForegroundColor White
    } else {
        Write-Host "✅ Tutte le macchine sono CONNESSE e aggiornate!" -ForegroundColor Green -BackgroundColor Black
        Write-Host "   Se le dashboard risultano vuote, il problema è nel frontend.`n" -ForegroundColor White
    }
    
    # 4. Check PlcServiceStatus
    Write-Host "Controllo PlcServiceStatus..." -ForegroundColor Cyan
    $cmd.CommandText = "SELECT TOP 1 * FROM PlcServiceStatus ORDER BY LastHeartbeat DESC"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        $heartbeat = $reader["LastHeartbeat"]
        $running = $reader["IsRunning"]
        $minuti = [int]((Get-Date) - $heartbeat).TotalMinutes
        
        Write-Host "Status PlcSync:" -ForegroundColor Yellow
        Write-Host "  Running: $running" -ForegroundColor $(if($running){"Green"}else{"Red"})
        Write-Host "  Last Heartbeat: $heartbeat ($minuti minuti fa)" -ForegroundColor White
        
        if (-not $running -or $minuti -gt 5) {
            Write-Host "`n❌ PlcSync NON ATTIVO o non aggiorna da $minuti minuti!" -ForegroundColor Red
            Write-Host "   Avviarlo con: sc start MESManager.PlcSync`n" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ Nessun record in PlcServiceStatus (PlcSync mai avviato)`n" -ForegroundColor Red
    }
    $reader.Close()
    
    $conn.Close()
    
} catch {
    Write-Host "`n❌ ERRORE: $_`n" -ForegroundColor Red
    exit 1
}
