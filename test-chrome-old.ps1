$exe = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$tmp = $env:TEMP
$html = "<html><head><title>TEST PREVENTIVO</title></head><body><h1>Test headless=old su Chrome 147</h1><p>Test header/footer suppression.</p></body></html>"
$htmlFile = Join-Path $tmp "t_diag2.html"
Set-Content $htmlFile $html -Encoding UTF8
$uri = "file:///" + $htmlFile.Replace('\','/')

# headless=old + no-header-footer
$out = Join-Path $tmp "t_old2.pdf"; $prof = Join-Path $tmp "tprof_old2"
Start-Process -FilePath $exe -ArgumentList @("--headless=old","--print-to-pdf=$out","--print-to-pdf-no-header-footer","--disable-gpu","--no-first-run","--user-data-dir=$prof",$uri) -Wait -WindowStyle Hidden 2>$null
if(Test-Path $out) {
    $b = [IO.File]::ReadAllBytes($out)
    $txt = [Text.Encoding]::UTF8.GetString($b)
    $hasUrl = $txt.Contains("file:///")
    Write-Host "headless=old Chrome 147: $($b.Length) byte, header/footer URL=$hasUrl"
    $header = [Text.Encoding]::ASCII.GetString($b[0..4])
    Write-Host "PDF header: $header"
} else { Write-Host "headless=old: PDF NON creato - flag non supportato" }

Remove-Item $htmlFile,$out -Force -ErrorAction SilentlyContinue
Remove-Item $prof -Recurse -Force -ErrorAction SilentlyContinue
