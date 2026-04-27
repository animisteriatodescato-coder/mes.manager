$exe = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$tmp = $env:TEMP
$html = "<html><head><title>TEST PREVENTIVO</title></head><body><h1>Test</h1></body></html>"
$htmlFile = Join-Path $tmp "t_diag.html"
Set-Content $htmlFile $html -Encoding UTF8
$uri = "file:///" + $htmlFile.Replace('\','/')

# CON flag
$out1 = Join-Path $tmp "t_con_flag.pdf"
$prof1 = Join-Path $tmp "tprof1"
Start-Process -FilePath $exe -ArgumentList @("--headless=new","--print-to-pdf=$out1","--print-to-pdf-no-header-footer","--disable-gpu","--no-first-run","--user-data-dir=$prof1",$uri) -Wait -WindowStyle Hidden 2>$null
if(Test-Path $out1) {
    $b1 = [IO.File]::ReadAllBytes($out1)
    $txt1 = [Text.Encoding]::UTF8.GetString($b1)
    $hasUrl1 = $txt1.Contains("file:///")
    Write-Host "CON flag: $($b1.Length) byte, contiene 'file:///'=$hasUrl1"
} else { Write-Host "CON flag: PDF non creato" }

# SENZA flag  
$out2 = Join-Path $tmp "t_senza_flag.pdf"
$prof2 = Join-Path $tmp "tprof2"
Start-Process -FilePath $exe -ArgumentList @("--headless=new","--print-to-pdf=$out2","--disable-gpu","--no-first-run","--user-data-dir=$prof2",$uri) -Wait -WindowStyle Hidden 2>$null
if(Test-Path $out2) {
    $b2 = [IO.File]::ReadAllBytes($out2)
    $txt2 = [Text.Encoding]::UTF8.GetString($b2)
    $hasUrl2 = $txt2.Contains("file:///")
    Write-Host "SENZA flag: $($b2.Length) byte, contiene 'file:///'=$hasUrl2"
} else { Write-Host "SENZA flag: PDF non creato" }

# Cleanup
Remove-Item $htmlFile,$out1,$out2,$prof1,$prof2 -Recurse -Force -ErrorAction SilentlyContinue
