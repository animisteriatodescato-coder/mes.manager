# Script per importare le Anime dal database Gantt
$ErrorActionPreference = "Stop"

Write-Host "Avvio importazione Anime dal database Gantt..." -ForegroundColor Yellow

# Usa curl invece di Invoke-RestMethod per evitare problemi di connessione
$result = curl.exe http://localhost:5156/api/anime/import -X POST -H "Content-Type: application/json" -s

if ($LASTEXITCODE -eq 0) {
    Write-Host "Importazione completata!" -ForegroundColor Green
    Write-Host $result
    
    # Verifica i dati importati
    Write-Host "`nVerifica dati importati:" -ForegroundColor Yellow
    sqlcmd -S "localhost\SQLEXPRESS01" -d MESManager -E -C -Q "SELECT COUNT(*) AS TotaleAnime FROM Anime" -W
} else {
    Write-Host "Errore durante l'importazione" -ForegroundColor Red
}
