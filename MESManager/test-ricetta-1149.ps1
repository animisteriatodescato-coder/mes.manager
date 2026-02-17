# Test ricetta articolo 1149/1-01
$connectionString = "Server=localhost\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"

$query = @"
-- 1. Cerchiamo l'anime
SELECT TOP 5
    a.Id as AnimeId,
    a.CodiceArticolo,
    a.DescrizioneArticolo
FROM Anime a
WHERE a.CodiceArticolo LIKE '%1149%'
ORDER BY a.CodiceArticolo;

-- 2. Cerchiamo l'articolo corrispondente
SELECT TOP 5
    art.Id as ArticoloId,
    art.Codice,
    art.Descrizione,
    CASE WHEN r.Id IS NOT NULL THEN 'SI' ELSE 'NO' END as HaRicetta,
    (SELECT COUNT(*) FROM ParametriRicetta pr WHERE pr.RicettaId = r.Id) as NumParametri
FROM Articoli art
LEFT JOIN Ricette r ON r.ArticoloId = art.Id
WHERE art.Codice LIKE '%1149%'
ORDER BY art.Codice;

-- 3. Verifica match esatto per '1149/1-01'
SELECT
    art.Codice as Codice_DB,
    LEN(art.Codice) as Len_DB,
    '1149/1-01' as Codice_Cercato,
    LEN('1149/1-01') as Len_Cercato,
    CASE WHEN art.Codice = '1149/1-01' THEN 'MATCH ESATTO' ELSE 'NO MATCH' END as Risultato,
    CASE WHEN r.Id IS NOT NULL THEN 'SI' ELSE 'NO' END as HaRicetta,
    (SELECT COUNT(*) FROM ParametriRicetta pr WHERE pr.RicettaId = r.Id) as NumParametri
FROM Articoli art
LEFT JOIN Ricette r ON r.ArticoloId = art.Id
WHERE art.Codice LIKE '%1149%';
"@

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds) | Out-Null
    
    Write-Host "`n=== RISULTATI TEST RICETTA 1149/1-01 ===" -ForegroundColor Cyan
    
    for ($i = 0; $i -lt $ds.Tables.Count; $i++) {
        $table = $ds.Tables[$i]
        Write-Host "`n--- Tabella $($i + 1) ---" -ForegroundColor Yellow
        $table | Format-Table -AutoSize
    }
    
    $conn.Close()
    Write-Host "`n✅ Test completato" -ForegroundColor Green
}
catch {
    Write-Host "❌ Errore: $_" -ForegroundColor Red
}
