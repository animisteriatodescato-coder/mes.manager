# Script per impostare tutte le macchine (1-11) su tutte le anime
Write-Host "Recupero tutte le anime dal database..." -ForegroundColor Cyan

try {
    # Recupera tutte le anime
    $anime = Invoke-RestMethod -Uri "http://localhost:5156/api/Anime" -Method GET
    Write-Host "Totale anime recuperate: $($anime.Count)" -ForegroundColor Green
    
    # Prepara il valore standard con tutte le macchine
    $tutteLeMacchine = "1;2;3;4;5;6;7;8;9;10;11"
    
    $aggiornate = 0
    $errori = 0
    
    foreach ($a in $anime) {
        try {
            # Aggiorna solo se le macchine non sono già impostate a tutte
            if ($a.macchineSuDisponibili -ne $tutteLeMacchine) {
                $a.macchineSuDisponibili = $tutteLeMacchine
                $a.modificatoLocalmente = $true
                $a.dataUltimaModificaLocale = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
                
                $response = Invoke-RestMethod -Uri "http://localhost:5156/api/Anime/$($a.id)" -Method PUT -Body ($a | ConvertTo-Json) -ContentType "application/json"
                $aggiornate++
                
                if ($aggiornate % 10 -eq 0) {
                    Write-Host "Aggiornate $aggiornate anime..." -ForegroundColor Yellow
                }
            }
        }
        catch {
            Write-Host "Errore aggiornando anima $($a.codiceArticolo): $($_.Exception.Message)" -ForegroundColor Red
            $errori++
        }
    }
    
    Write-Host "`n=== COMPLETATO ===" -ForegroundColor Green
    Write-Host "Anime aggiornate: $aggiornate" -ForegroundColor Cyan
    Write-Host "Errori: $errori" -ForegroundColor $(if ($errori -eq 0) { "Green" } else { "Red" })
}
catch {
    Write-Host "Errore: $($_.Exception.Message)" -ForegroundColor Red
}
