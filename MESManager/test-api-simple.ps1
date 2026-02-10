#!/usr/bin/env pwsh
# Simple export test without markdown formatting

$ApiUrl = "http://localhost:5156"
$apiEndpoint = "$ApiUrl/api/pianificazione/esporta-su-programma"

Write-Host "Starting export test..." -ForegroundColor Cyan
Write-Host "Endpoint: $apiEndpoint" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $apiEndpoint -Method POST -TimeoutSec 10 -UseBasicParsing
    
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    
    $body = $response.Content | ConvertFrom-Json
    
    Write-Host "`nResponse:" -ForegroundColor Cyan
    Write-Host "  Success: $($body.success)" 
    Write-Host "  Message: $($body.message)"
    
    if ($body.debugInfo) {
        Write-Host "`nDebug Info:" -ForegroundColor Cyan
        Write-Host "  Aggiornate: $($body.debugInfo.aggiornate)" 
        Write-Host "  Totali: $($body.debugInfo.totali)"
        Write-Host "  Rows Affected: $($body.debugInfo.rowsAffected)"
        Write-Host "  Duration: $($body.debugInfo.durationMs) ms"
    }
    
    if ($body.success -eq $true -and $body.debugInfo.aggiornate -gt 0) {
        Write-Host "`n*** TEST PASSED ***" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "`n*** TEST FAILED ***" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "Connection Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Is app running on localhost:5156?" -ForegroundColor Yellow
    exit 1
}
