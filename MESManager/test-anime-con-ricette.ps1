# Test: quali anime hanno ricette
$connectionString = "Server=localhost\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"

$query = @"
-- Anime con ricette (mostra badge verde)
SELECT TOP 10
    a.CodiceArticolo,
    a.DescrizioneArticolo,
    art.Codice as ArticoloCodicE_DB,
    CASE WHEN r.Id IS NOT NULL THEN 'SI' ELSE 'NO' END as HasRicetta,
    (SELECT COUNT(*) FROM ParametriRicetta pr WHERE pr.RicettaId = r.Id) as NumParametri
FROM Anime a
LEFT JOIN Articoli art ON art.Codice = a.CodiceArticolo
LEFT JOIN Ricette r ON r.ArticoloId = art.Id
WHERE r.Id IS NOT NULL
ORDER BY art.Codice;
"@

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`n=== ANIME CON RICETTE (Badge Verde) ===" -ForegroundColor Cyan
    Write-Host ""
    
    $count = 0
    while ($reader.Read()) {
        $count++
        Write-Host "$($count). " -NoNewline -ForegroundColor Yellow
        Write-Host "CodiceArticolo: " -NoNewline
        Write-Host "$($reader['CodiceArticolo'])" -ForegroundColor Green -NoNewline
        Write-Host " | NumParametri: " -NoNewline
        Write-Host "$($reader['NumParametri'])" -ForegroundColor Cyan
    }
    
    $reader.Close()
    $conn.Close()
    
    Write-Host "`n✅ Trovati $count anime con ricette" -ForegroundColor Green
}
catch {
    Write-Host "❌ Errore: $_" -ForegroundColor Red
}
