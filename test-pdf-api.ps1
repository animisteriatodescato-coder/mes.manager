$ErrorActionPreference = 'Stop'

$sv = $null
$resp = Invoke-WebRequest 'http://localhost:5156/Account/Login' -SessionVariable sv -UseBasicParsing
$token = [regex]::Match($resp.Content, '__RequestVerificationToken[^>]+value="([^"]+)"').Groups[1].Value
Write-Host "Token ottenuto: $($token.Substring(0,15))..."

$body = "Input.Email=admin%40mesmanager.it&Input.Password=Admin123!&__RequestVerificationToken=$([Uri]::EscapeDataString($token))"
$login = Invoke-WebRequest 'http://localhost:5156/Account/Login' -Method Post -Body $body -ContentType 'application/x-www-form-urlencoded' -WebSession $sv -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
Write-Host "Login HTTP $($login.StatusCode)"

$sv.Cookies.GetCookies('http://localhost:5156') | ForEach-Object { Write-Host "  Cookie: $($_.Name)" }

# Chiama API con HTML semplice
$pdfJson = '{"Html":"<html><body style=\"margin:20px\"><h1>Test PDF MESManager</h1><p>Verifica generazione PDF da endpoint.</p></body></html>","FileName":"test.pdf"}'

Write-Host "Chiamata /api/preventivo/pdf..."
$pdfResp = Invoke-WebRequest 'http://localhost:5156/api/preventivo/pdf' -Method Post -Body $pdfJson -ContentType 'application/json' -WebSession $sv -UseBasicParsing
$outFile = "$env:TEMP\api_test_result.pdf"
[System.IO.File]::WriteAllBytes($outFile, $pdfResp.Content)
$header = [System.Text.Encoding]::ASCII.GetString($pdfResp.Content[0..4])
Write-Host "PDF: $($pdfResp.Content.Length) byte, header='$header'"
Write-Host "Salvato in: $outFile"
