#!/usr/bin/env pwsh

Write-Host "Test Export Gantt" -ForegroundColor Green
Write-Host "=================" -ForegroundColor Green

$ApiUrl = "http://localhost:5156"
$apiEndpoint = "$ApiUrl/api/pianificazione/esporta-su-programma"

Write-Host "`nTesting: $apiEndpoint" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $apiEndpoint -Method POST -TimeoutSec 10
    
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Green
    $body = $response.Content | ConvertFrom-Json
    
    Write-Host "Success: $($body.success)" -ForegroundColor Green
    Write-Host "Message: $($body.message)" -ForegroundColor Green
    
    if ($body.PSObject.Properties.Name -contains 'debugInfo') {
        $info = $body.debugInfo
        Write-Host "Debug Info:" -ForegroundColor Cyan
        Write-Host "  - Aggiornate: $($info.aggiornate)" 
        Write-Host "  - Totali: $($info.totali)"
        Write-Host "  - Rows Affected: $($info.rowsAffected)"
        Write-Host "  - Duration (ms): $($info.durationMs)"
    }
    
    if ($body.success -eq $true) {
        Write-Host "`nRESULT: PASS" -ForegroundColor Green
    } else {
        Write-Host "`nRESULT: FAIL (success=false)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "RESULT: FAIL" -ForegroundColor Red
    exit 1
}
