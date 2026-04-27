$exe = "C:\Program Files\Google\Chrome\Application\chrome.exe"
$tmp = $env:TEMP

# Input HTML
$html = "<html><head><title>TEST PREVENTIVO</title></head><body style='font-family:sans-serif'><h1>Test headless</h1><p>Contenuto di test.</p></body></html>"
Set-Content "$tmp\t.html" $html -Encoding UTF8
$uri = "file:///$($tmp.Replace('\','/'))/t.html"

# Test 1: headless=old
$out1 = "$tmp\t_old.pdf"; $prof1 = "$tmp\tprof_old"
Start-Process -FilePath $exe -ArgumentList @("--headless=old", "--print-to-pdf=$out1", "--print-to-pdf-no-header-footer", "--disable-gpu", "--no-first-run", "--user-data-dir=$prof1", $uri) -Wait -WindowStyle Hidden 2>$null
$r1 = if(Test-Path $out1) { "$([IO.File]::ReadAllBytes($out1).Length) byte" } else { "FALLITO" }
Write-Host "headless=old: $r1"

# Test 2: headless=new + print-to-pdf-no-header-footer
$out2 = "$tmp\t_new.pdf"; $prof2 = "$tmp\tprof_new"
Start-Process -FilePath $exe -ArgumentList @("--headless=new", "--print-to-pdf=$out2", "--print-to-pdf-no-header-footer", "--disable-gpu", "--no-first-run", "--user-data-dir=$prof2", $uri) -Wait -WindowStyle Hidden 2>$null
$r2 = if(Test-Path $out2) { "$([IO.File]::ReadAllBytes($out2).Length) byte" } else { "FALLITO" }
Write-Host "headless=new + no-header-footer: $r2"

# Cleanup
@($out1, $out2, $prof1, $prof2) | ForEach-Object { if(Test-Path $_) { Remove-Item $_ -Recurse -Force -ErrorAction SilentlyContinue } }
