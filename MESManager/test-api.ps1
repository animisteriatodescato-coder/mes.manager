# Script di test automatico per verificare le API
# Uso: .\test-api.ps1
# NOTA: Avvia prima l'app con .\start-web.ps1

$baseUrl = "http://localhost:5156"
$ErrorActionPreference = "Continue"  # Non fermare tutto script su errore

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    Write-Host "`n=== TEST: $Name ===" -ForegroundColor Cyan
    Write-Host "URL: $Method $Url" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            ContentType = "application/json"
            TimeoutSec = 10
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json)
            Write-Host "Body: $($params.Body)" -ForegroundColor Gray
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✓ SUCCESS" -ForegroundColor Green
        
        # Formatta output in modo più leggibile
        if ($response -is [System.Collections.IEnumerable] -and $response -isnot [string]) {
            Write-Host ($response | ConvertTo-Json -Depth 3 -Compress:$false) -ForegroundColor White
        } else {
            Write-Host ($response | ConvertTo-Json -Depth 3 -Compress:$false) -ForegroundColor White
        }
        
        return $response
    }
    catch [System.Net.WebException] {
        Write-Host "✗ FAILED - Connessione" -ForegroundColor Red
        Write-Host "Errore: L'app non è raggiungibile su $baseUrl" -ForegroundColor Red
        Write-Host "Soluzione: Avvia prima l'app con .\start-web.ps1" -ForegroundColor Yellow
        return $null
    }
    catch {
        Write-Host "✗ FAILED" -ForegroundColor Red
        Write-Host "Errore: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "Response: $responseBody" -ForegroundColor Yellow
            }
            catch {
                Write-Host "Impossibile leggere il corpo della risposta" -ForegroundColor Gray
            }
        }
        return $null
    }
}

# Test 1: Verifica stato commesse
Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  TEST SUITE: Verifica Sistema Programma Macchine        ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

$debug = Test-Endpoint `
    -Name "Debug Commesse - Conteggi" `
    -Url "$baseUrl/api/pianificazione/debug-commesse"

if ($debug) {
    Write-Host "`nRISULTATI DEBUG:" -ForegroundColor Yellow
    Write-Host "  • Totale commesse: $($debug.totaleCommesse)" -ForegroundColor White
    Write-Host "  • Con macchina: $($debug.conMacchina)" -ForegroundColor White
    Write-Host "  • Con date: $($debug.conDate)" -ForegroundColor White
    Write-Host "  • Con macchina E date (esportabili): $($debug.conMacchinaEDate)" -ForegroundColor Cyan
    Write-Host "  • Stato Programmata: $($debug.statoProgrammataProgrammata)" -ForegroundColor Cyan
    Write-Host "  • Stato Aperta: $($debug.statoAperta)" -ForegroundColor White
    Write-Host "  • Aperte con macchina: $($debug.aperteConMacchina)" -ForegroundColor White
    
    if ($debug.conMacchinaEDate -eq 0) {
        Write-Host "`n⚠ PROBLEMA: Non ci sono commesse con macchina E date!" -ForegroundColor Red
        Write-Host "Devi prima assegnare commesse nel Gantt prima di esportare." -ForegroundColor Yellow
    }
}

# Test 2: Lista commesse
$commesse = Test-Endpoint `
    -Name "GET /api/Commesse" `
    -Url "$baseUrl/api/Commesse"

if ($commesse) {
    Write-Host "`nPrime 3 commesse:" -ForegroundColor Yellow
    $commesse | Select-Object -First 3 | ForEach-Object {
        Write-Host "  - $($_.codice): Macchina=$($_.numeroMacchina), StatoProgramma=$($_.statoProgramma), Stato=$($_.stato)" -ForegroundColor White
    }
}

# Test 3: Verifica filtro Programma Macchine
Write-Host "`n--- Verifica Commesse Programmate ---" -ForegroundColor Yellow
if ($commesse) {
    $programmate = $commesse | Where-Object {
        $_.stato -eq "Aperta" -and
        $_.numeroMacchina -ne $null -and
        $_.statoProgramma -ne "Archiviata"
    }
    
    Write-Host "COMMESSE IN PROGRAMMA MACCHINE: $($programmate.Count)" -ForegroundColor Cyan
    if ($programmate.Count -gt 0) {
        Write-Host "Prime 3 commesse programmate:" -ForegroundColor Cyan
        $programmate | Select-Object -First 3 | ForEach-Object {
            Write-Host "  - $($_.codice): Macchina=$($_.numeroMacchina), StatoProgramma=$($_.statoProgramma), DataInizio=$($_.dataInizioPrevisione)" -ForegroundColor White
        }
    } else {
        Write-Host "⚠ Nessuna commessa trovata in Programma Macchine!" -ForegroundColor Red
        Write-Host "Filtro usato: Stato == 'Aperta' AND NumeroMacchina != null AND StatoProgramma != 'Archiviata'" -ForegroundColor Gray
        
        # Diagnostica perché è vuoto
        $aperteConMacchina = ($commesse | Where-Object { $_.stato -eq "Aperta" -and $_.numeroMacchina -ne $null }).Count
        $aperteConMacchinaArchiviate = ($commesse | Where-Object { $_.stato -eq "Aperta" -and $_.numeroMacchina -ne $null -and $_.statoProgramma -eq "Archiviata" }).Count
        
        Write-Host "Diagnostica:" -ForegroundColor Yellow
        Write-Host "  - Aperte con macchina: $aperteConMacchina" -ForegroundColor White
        Write-Host "  - Di cui Archiviate: $aperteConMacchinaArchiviate" -ForegroundColor White
        
        if ($aperteConMacchinaArchiviate -gt 0) {
            Write-Host "  ⚠ Ci sono commesse Archiviate che vengono filtrate!" -ForegroundColor Red
        }
    }
}

# Test 4: Export (solo se ci sono commesse da esportare)
if ($debug -and $debug.conMacchinaEDate -gt 0) {
    Write-Host "`n" -NoNewline
    Read-Host "Premi INVIO per testare l'esportazione (o CTRL+C per uscire)"
    
    $export = Test-Endpoint `
        -Name "POST /api/pianificazione/esporta-su-programma" `
        -Url "$baseUrl/api/pianificazione/esporta-su-programma" `
        -Method "POST"
    
    if ($export -and $export.success) {
        Write-Host "`n✓ Export completato: $($export.message)" -ForegroundColor Green
        
        # Riverifica i conteggi dopo export
        Start-Sleep -Seconds 1
        $debugPost = Test-Endpoint `
            -Name "Debug Commesse DOPO export" `
            -Url "$baseUrl/api/pianificazione/debug-commesse"
        
        if ($debugPost) {
            Write-Host "`nCONFRONTO PRIMA/DOPO:" -ForegroundColor Yellow
            Write-Host "  StatoProgrammata: $($debug.statoProgrammataProgrammata) → $($debugPost.statoProgrammataProgrammata)" -ForegroundColor Cyan
            
            if ($debugPost.statoProgrammataProgrammata -gt $debug.statoProgrammataProgrammata) {
                Write-Host "  ✓ StatoProgramma aggiornato correttamente!" -ForegroundColor Green
            } else {
                Write-Host "  ✗ StatoProgramma NON è cambiato!" -ForegroundColor Red
                Write-Host "  Causa: Tutte le commesse esportabili sono già in stato Programmata" -ForegroundColor Yellow
            }
        }
        
        # Test 5: Riverifica Programma Macchine dopo export
        if ($commesse) {
            Start-Sleep -Seconds 1
            $commessePost = Test-Endpoint `
                -Name "GET /api/Commesse DOPO export" `
                -Url "$baseUrl/api/Commesse"
            
            if ($commessePost) {
                $programmataPost = $commessePost | Where-Object {
                    $_.stato -eq "Aperta" -and
                    $_.numeroMacchina -ne $null -and
                    $_.statoProgramma -ne "Archiviata"
                }
                
                Write-Host "`nCOMMESSE IN PROGRAMMA MACCHINE DOPO EXPORT: $($programmataPost.Count)" -ForegroundColor Cyan
                if ($programmate -and $programmataPost) {
                    $diff = $programmataPost.Count - $programmate.Count
                    if ($diff -gt 0) {
                        Write-Host "  ✓ Aggiunte $diff nuove commesse!" -ForegroundColor Green
                    } elseif ($diff -eq 0) {
                        Write-Host "  ⚠ Nessuna nuova commessa aggiunta (già presenti)" -ForegroundColor Yellow
                    }
                }
            }
        }
    }
} else {
    Write-Host "`n⊘ Skip export test (nessuna commessa esportabile)" -ForegroundColor Gray
}

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║  TEST COMPLETATI                                         ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

# Riepilogo finale
Write-Host "`n📊 RIEPILOGO FINALE:" -ForegroundColor Cyan
if ($debug) {
    Write-Host "  • Commesse esportabili: $($debug.conMacchinaEDate)" -ForegroundColor White
    Write-Host "  • Commesse programmate: $($debug.statoProgrammataProgrammata)" -ForegroundColor White
}
if ($programmate) {
    Write-Host "  • Commesse in Programma Macchine: $($programmate.Count)" -ForegroundColor White
}

if ($debug -and $programmate -and $debug.statoProgrammataProgrammata -gt 0 -and $programmate.Count -eq 0) {
    Write-Host "`n❌ PROBLEMA CRITICO IDENTIFICATO:" -ForegroundColor Red
    Write-Host "  Ci sono $($debug.statoProgrammataProgrammata) commesse con StatoProgramma=Programmata" -ForegroundColor Yellow
    Write-Host "  MA 0 commesse vengono mostrate in Programma Macchine!" -ForegroundColor Yellow
    Write-Host "  Possibile causa: NumeroMacchina non impostato o commesse Archiviate" -ForegroundColor Gray
}
