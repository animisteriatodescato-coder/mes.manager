# Script per pulire e reimpostare correttamente tutte le macchine
Write-Host "=== PULIZIA E REIMPOSTAZIONE MACCHINE ===" -ForegroundColor Cyan

try {
    # Recupera tutte le anime
    $anime = Invoke-RestMethod -Uri "http://localhost:5156/api/Anime" -Method GET
    Write-Host "Totale anime recuperate: $($anime.Count)" -ForegroundColor Green
    
    # Codici corretti delle macchine
    $tutteLeMacchine = "M001;M002;M003;M004;M005;M006;M007;M008;M009;M010;M011"
    
    $aggiornate = 0
    $errori = 0
    
    foreach ($a in $anime) {
        try {
            # Imposta tutte le macchine con i codici corretti
            $a.macchineSuDisponibili = $tutteLeMacchine
            $a.modificatoLocalmente = $true
            $a.dataUltimaModificaLocale = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
            
            $response = Invoke-RestMethod -Uri "http://localhost:5156/api/Anime/$($a.id)" -Method PUT -Body ($a | ConvertTo-Json) -ContentType "application/json"
            $aggiornate++
            
            if ($aggiornate % 50 -eq 0) {
                Write-Host "Aggiornate $aggiornate anime..." -ForegroundColor Yellow
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
    Write-Host "Codici macchine impostati: $tutteLeMacchine" -ForegroundColor White
}
catch {
    Write-Host "Errore: $($_.Exception.Message)" -ForegroundColor Red
}
