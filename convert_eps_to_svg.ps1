# Legge il file EPS del SOLO ICONA (spirale) - BoundingBox 104x83
$eps = [System.IO.File]::ReadAllText("C:\Users\produ\Desktop\black_logo_transparent_background.eps")
$ic = [System.Globalization.CultureInfo]::InvariantCulture

$stack = [System.Collections.Generic.Stack[double]]::new()
$pathD = [System.Text.StringBuilder]::new()
$spiralPaths = [System.Collections.Generic.List[string]]::new()

# Estrai sezione disegno tra "0 g" e "Q Q"
$drawStart = $eps.IndexOf("0 g") + 3
$drawEnd   = $eps.LastIndexOf("Q Q")
$drawing   = $eps.Substring($drawStart, $drawEnd - $drawStart).Trim()
$tokens    = ($drawing -split '\s+') | Where-Object { $_ -ne '' }
Write-Host "Tokens totali: $($tokens.Count)"

foreach ($t in $tokens) {
    $v = 0.0
    if ([double]::TryParse($t, [System.Globalization.NumberStyles]::Float, $ic, [ref]$v)) {
        $stack.Push($v)
    } else {
        switch ($t) {
            'm' {
                $y = $stack.Pop(); $x = $stack.Pop()
                if ($pathD.Length -gt 0) { [void]$pathD.Append(' ') }
                [void]$pathD.Append("M $($x.ToString('G6',$ic)),$($y.ToString('G6',$ic))")
            }
            'l' {
                $y = $stack.Pop(); $x = $stack.Pop()
                [void]$pathD.Append(" L $($x.ToString('G6',$ic)),$($y.ToString('G6',$ic))")
            }
            'c' {
                $y3=$stack.Pop();$x3=$stack.Pop()
                $y2=$stack.Pop();$x2=$stack.Pop()
                $y1=$stack.Pop();$x1=$stack.Pop()
                [void]$pathD.Append(" C $($x1.ToString('G6',$ic)),$($y1.ToString('G6',$ic)) $($x2.ToString('G6',$ic)),$($y2.ToString('G6',$ic)) $($x3.ToString('G6',$ic)),$($y3.ToString('G6',$ic))")
            }
            'h' { [void]$pathD.Append(" Z") }
            'f' {
                # f = testo/lettere vettoriali → ignoriamo
                $pathD.Clear(); $stack.Clear()
            }
            'f*' {
                # f* = evenodd fill = SPIRALE
                $d = $pathD.ToString()
                if ($d.Length -gt 0) { $spiralPaths.Add($d) }
                $pathD.Clear(); $stack.Clear()
            }
            're' {
                # rettangoli divisori → ignoriamo
                $h=$stack.Pop();$w=$stack.Pop();$yy=$stack.Pop();$xx=$stack.Pop()
                $stack.Clear()
            }
        }
    }
}

Write-Host "Spiral paths trovati: $($spiralPaths.Count)"

# Scala uniforme per riempire 148x115 dal BBox 104x83
$scale = [Math]::Min([Math]::Round(148.0 / 104.0, 4), [Math]::Round(115.0 / 83.0, 4))

$spiralG = $spiralPaths | ForEach-Object { "    <path d=`"$_`" fill=`"#000`" fill-rule=`"evenodd`"/>" }
$spiralBlock = $spiralG -join "`n"

$svg = @"
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 840 115" width="100%" height="auto">
  <rect width="840" height="115" fill="white"/>
  <!-- Spirale dall'EPS (BBox 104x83), Y-flipped, scalata x$scale per riempire 148x115 -->
  <g transform="scale($scale)">
$spiralBlock
  </g>
  <!-- Separatore verticale dx spirale -->
  <line x1="148" y1="5" x2="148" y2="110" stroke="#ccc" stroke-width="1"/>
  <!-- ANIMISTERIA TODESCATO bold -->
  <text x="162" y="50" font-family="'Arial Black',Arial,sans-serif" font-size="32" font-weight="900" fill="#000" letter-spacing="-0.5">ANIMISTERIA</text>
  <text x="162" y="88" font-family="'Arial Black',Arial,sans-serif" font-size="32" font-weight="900" fill="#000" letter-spacing="-0.5">TODESCATO</text>
  <!-- Separatore verticale dx nome -->
  <line x1="480" y1="10" x2="480" y2="105" stroke="#ddd" stroke-width="1"/>
  <!-- Blocco dati aziendali -->
  <text x="494" y="30" font-family="Arial,sans-serif" font-size="9.5" font-weight="bold" letter-spacing="2.5" fill="#000">ANIME IN SHELL MOULDING</text>
  <line x1="494" y1="36" x2="836" y2="36" stroke="#555" stroke-width="1"/>
  <text x="494" y="52" font-family="Arial,sans-serif" font-size="9" fill="#333">Via Luigi Galvani 44/46 - 36066 SANDRIGO (VI)</text>
  <text x="494" y="65" font-family="Arial,sans-serif" font-size="9" fill="#333">Tel: 0444 658208   Email: info@animisteriatodescato.it</text>
  <text x="494" y="78" font-family="Arial,sans-serif" font-size="9" fill="#333">PEC: animisteriatodescatosas@cgn.legalmail.it</text>
  <text x="494" y="91" font-family="Arial,sans-serif" font-size="9" fill="#333">P.I.: 03200610248   SDI: SUBM70N</text>
</svg>
"@

$outPath = "C:\Dev\MESManager\MESManager.Web\wwwroot\images\logo-intestazione.svg"
[System.IO.File]::WriteAllText($outPath, $svg, [System.Text.Encoding]::UTF8)
$info = Get-Item $outPath
Write-Host "Scritto: $($info.Length) bytes, $($info.LastWriteTime)"
