$eps = [System.IO.File]::ReadAllText("C:\Users\produ\Desktop\black_logo_transparent_background.eps")
$ic = [System.Globalization.CultureInfo]::InvariantCulture
$svgElements = [System.Collections.Generic.List[string]]::new()
$stack = [System.Collections.Generic.Stack[double]]::new()
$pathD = [System.Text.StringBuilder]::new()

# Estrai sezione disegno tra "0 g" e "Q Q"
$drawStart = $eps.IndexOf("0 g") + 3
$drawEnd = $eps.LastIndexOf("Q Q")
$drawing = $eps.Substring($drawStart, $drawEnd - $drawStart).Trim()
$tokens = ($drawing -split '\s+') | Where-Object { $_ -ne '' }
Write-Host "Tokens: $($tokens.Count)"

foreach ($t in $tokens) {
    $v = 0.0
    if ([double]::TryParse($t, [System.Globalization.NumberStyles]::Float, $ic, [ref]$v)) {
        $stack.Push($v)
    } else {
        switch ($t) {
            'm' {
                $y = $stack.Pop(); $x = $stack.Pop()
                if ($pathD.Length -gt 0) { [void]$pathD.Append(' ') }
                [void]$pathD.Append("M $($x.ToString('G5',$ic)),$($y.ToString('G5',$ic))")
            }
            'l' {
                $y = $stack.Pop(); $x = $stack.Pop()
                [void]$pathD.Append(" L $($x.ToString('G5',$ic)),$($y.ToString('G5',$ic))")
            }
            'c' {
                $y3=$stack.Pop();$x3=$stack.Pop()
                $y2=$stack.Pop();$x2=$stack.Pop()
                $y1=$stack.Pop();$x1=$stack.Pop()
                [void]$pathD.Append(" C $($x1.ToString('G5',$ic)),$($y1.ToString('G5',$ic)) $($x2.ToString('G5',$ic)),$($y2.ToString('G5',$ic)) $($x3.ToString('G5',$ic)),$($y3.ToString('G5',$ic))")
            }
            'h' { [void]$pathD.Append(" Z") }
            'f' {
                $d = $pathD.ToString()
                if ($d.Length -gt 0) { $svgElements.Add("<path d=`"$d`" fill=`"#000`"/>") }
                $pathD.Clear()
                $stack.Clear()
            }
            'f*' {
                $d = $pathD.ToString()
                if ($d.Length -gt 0) { $svgElements.Add("<path d=`"$d`" fill=`"#000`" fill-rule=`"evenodd`"/>") }
                $pathD.Clear()
                $stack.Clear()
            }
            're' {
                $h = $stack.Pop(); $w = $stack.Pop(); $yy = $stack.Pop(); $xx = $stack.Pop()
                $svgElements.Add("<rect x=`"$($xx.ToString('G5',$ic))`" y=`"$($yy.ToString('G5',$ic))`" width=`"$($w.ToString('G5',$ic))`" height=`"$($h.ToString('G5',$ic))`" fill=`"#000`"/>")
            }
        }
    }
}

Write-Host "SVG elements: $($svgElements.Count)"
$inner = $svgElements -join "`n  "
$svg = @"
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 315 83" width="100%" height="auto">
  $inner
</svg>
"@

$outPath = "C:\Dev\MESManager\MESManager.Web\wwwroot\images\logo-intestazione.svg"
[System.IO.File]::WriteAllText($outPath, $svg, [System.Text.Encoding]::UTF8)
Write-Host "Scritto: $((Get-Item $outPath).Length) bytes"
Write-Host "Ultima modifica: $((Get-Item $outPath).LastWriteTime)"
