# TEST: Verifica salvataggio ricette complete con 100 parametri
# ================================================================

$server = "192.168.1.230\SQLEXPRESS01"
$db = "MESManager_Prod"
$user = "FAB"
$pwd = "password.123"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   TEST RICETTE COMPLETE" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=$server;Database=$db;User Id=$user;Password=$pwd;TrustServerCertificate=True;")
$conn.Open()
$cmd = $conn.CreateCommand()

# 1. Statistiche attuali
Write-Host "📊 STATISTICHE PRE-MODIFICA:" -ForegroundColor Yellow
Write-Host "=" * 50

$cmd.CommandText = @"
SELECT 
    a.Codice,
    COUNT(pr.Id) as NumParametri,
    MIN(pr.Indirizzo) as IndirizzoMin,
    MAX(pr.Indirizzo) as IndirizzoMax,
    SUM(CASE WHEN pr.CodiceParametro IS NOT NULL THEN 1 ELSE 0 END) as ConCodice,
    SUM(CASE WHEN pr.Area IS NOT NULL THEN 1 ELSE 0 END) as ConArea
FROM Ricette r
INNER JOIN Articoli a ON r.ArticoloId = a.Id
INNER JOIN ParametriRicetta pr ON pr.RicettaId = r.Id
GROUP BY a.Codice
ORDER BY NumParametri DESC
"@

$reader = $cmd.ExecuteReader()
$ricetteStats = @()

Write-Host "`nRICETTE ESISTENTI:" -ForegroundColor Cyan
Write-Host ("-" * 90)
Write-Host ("{0,-15} {1,6} {2,8} {3,8} {4,10} {5,8}" -f "Codice", "Param", "Min", "Max", "ConCodice", "ConArea") -ForegroundColor Gray
Write-Host ("-" * 90)

while ($reader.Read()) {
    $stats = [PSCustomObject]@{
        Codice = $reader['Codice']
        NumParametri = $reader['NumParametri']
        IndirizzoMin = $reader['IndirizzoMin']
        IndirizzoMax = $reader['IndirizzoMax']
        ConCodice = $reader['ConCodice']
        ConArea = $reader['ConArea']
    }
    $ricetteStats += $stats
    
    $color = if ($stats.NumParametri -ge 80) { 'Green' } elseif ($stats.NumParametri -ge 50) { 'Yellow' } else { 'Red' }
    Write-Host ("{0,-15} {1,6} {2,8} {3,8} {4,10} {5,8}" -f `
        $stats.Codice, $stats.NumParametri, $stats.IndirizzoMin, $stats.IndirizzoMax, `
        $stats.ConCodice, $stats.ConArea) -ForegroundColor $color
}
$reader.Close()

Write-Host ("-" * 90)
Write-Host "`nTotale ricette: $($ricetteStats.Count)" -ForegroundColor White

# 2. Analisi distribuzione parametri
Write-Host "`n📈 DISTRIBUZIONE PARAMETRI:" -ForegroundColor Yellow
Write-Host "=" * 50

$cmd.CommandText = @"
SELECT 
    CASE 
        WHEN Indirizzo < 100 THEN 'Range 0-99 (stato PLC)'
        WHEN Indirizzo >= 100 AND Indirizzo < 200 THEN 'Range 100-199 (ricetta)'
        ELSE 'Range 200+ (altro)'
    END as RangeIndirizzo,
    COUNT(*) as TotaleParametri,
    COUNT(DISTINCT RicettaId) as NumRicette
FROM ParametriRicetta
GROUP BY 
    CASE 
        WHEN Indirizzo < 100 THEN 'Range 0-99 (stato PLC)'
        WHEN Indirizzo >= 100 AND Indirizzo < 200 THEN 'Range 100-199 (ricetta)'
        ELSE 'Range 200+ (altro)'
    END
ORDER BY MIN(Indirizzo)
"@

$reader = $cmd.ExecuteReader()
Write-Host ""
while ($reader.Read()) {
    $range = $reader['RangeIndirizzo']
    $tot = $reader['TotaleParametri']
    $numRic = $reader['NumRicette']
    
    Write-Host ("  {0,-30}: {1,5} parametri ({2,3} ricette)" -f $range, $tot, $numRic) -ForegroundColor Cyan
}
$reader.Close()

# 3. Ricetta di esempio completa
Write-Host "`n🔍 ESEMPIO RICETTA COMPLETA (0153-B):" -ForegroundColor Yellow
Write-Host "=" * 50

$cmd.CommandText = @"
SELECT TOP 20
    pr.CodiceParametro,
    pr.Indirizzo,
    pr.NomeParametro,
    pr.Valore,
    pr.Area,
    pr.Tipo
FROM ParametriRicetta pr
INNER JOIN Ricette r ON pr.RicettaId = r.Id
INNER JOIN Articoli a ON r.ArticoloId = a.Id
WHERE a.Codice = '0153-B'
ORDER BY pr.Indirizzo
"@

$reader = $cmd.ExecuteReader()
Write-Host "`nPrimi 20 parametri ordinati per Indirizzo:" -ForegroundColor Cyan
Write-Host ("{0,4} {1,5} {2,-35} {3,8} {4,-5} {5,-6}" -f "Cod", "Ind", "Nome", "Valore", "Area", "Tipo") -ForegroundColor Gray
Write-Host ("-" * 75)

while ($reader.Read()) {
    $cod = if ($reader['CodiceParametro'] -ne [DBNull]::Value) { $reader['CodiceParametro'] } else { "NULL" }
    $ind = if ($reader['Indirizzo'] -ne [DBNull]::Value) { $reader['Indirizzo'] } else { "NULL" }
    $nome = $reader['NomeParametro'].ToString().Substring(0, [Math]::Min(35, $reader['NomeParametro'].Length))
    $val = $reader['Valore']
    $area = if ($reader['Area'] -ne [DBNull]::Value) { $reader['Area'] } else { "-" }
    $tipo = if ($reader['Tipo'] -ne [DBNull]::Value) { $reader['Tipo'] } else { "-" }
    
    Write-Host ("{0,4} {1,5} {2,-35} {3,8} {4,-5} {5,-6}" -f $cod, $ind, $nome, $val, $area, $tipo) -ForegroundColor White
}
$reader.Close()

# 4. Ricerche problematiche
Write-Host "`n⚠️  PARAMETRI CON DATI MANCANTI:" -ForegroundColor Yellow
Write-Host "=" * 50

$cmd.CommandText = @"
SELECT 
    COUNT(*) as TotaleParametri,
    SUM(CASE WHEN CodiceParametro IS NULL THEN 1 ELSE 0 END) as SenzaCodice,
    SUM(CASE WHEN Indirizzo IS NULL THEN 1 ELSE 0 END) as SenzaIndirizzo,
    SUM(CASE WHEN Area IS NULL OR Area = '' THEN 1 ELSE 0 END) as SenzaArea,
    SUM(CASE WHEN Tipo IS NULL OR Tipo = '' THEN 1 ELSE 0 END) as SenzaTipo
FROM ParametriRicetta
"@

$reader = $cmd.ExecuteReader()
$reader.Read()

Write-Host "`nParametri totali: $($reader['TotaleParametri'])" -ForegroundColor White
Write-Host ("  - Senza CodiceParametro: {0,5} ({1:P1})" -f $reader['SenzaCodice'], ($reader['SenzaCodice'] / $reader['TotaleParametri'])) -ForegroundColor $(if ($reader['SenzaCodice'] -gt 0) { 'Red' } else { 'Green' })
Write-Host ("  - Senza Indirizzo:       {0,5} ({1:P1})" -f $reader['SenzaIndirizzo'], ($reader['SenzaIndirizzo'] / $reader['TotaleParametri'])) -ForegroundColor $(if ($reader['SenzaIndirizzo'] -gt 0) { 'Red' } else { 'Green' })
Write-Host ("  - Senza Area:            {0,5} ({1:P1})" -f $reader['SenzaArea'], ($reader['SenzaArea'] / $reader['TotaleParametri'])) -ForegroundColor $(if ($reader['SenzaArea'] -gt 0) { 'Yellow' } else { 'Green' })
Write-Host ("  - Senza Tipo:            {0,5} ({1:P1})" -f $reader['SenzaTipo'], ($reader['SenzaTipo'] / $reader['TotaleParametri'])) -ForegroundColor $(if ($reader['SenzaTipo'] -gt 0) { 'Yellow' } else { 'Green' })

$reader.Close()
$conn.Close()

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   RIEPILOGO MODIFICHE NECESSARIE" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "✅ MODIFICHE IMPLEMENTATE:" -ForegroundColor Green
Write-Host "  1. PlcController ora salva TUTTI i parametri (0-196)" -ForegroundColor White
Write-Host "  2. CodiceParametro generato sequenzialmente" -ForegroundColor White
Write-Host "  3. Area impostata automaticamente a 'DB'" -ForegroundColor White

Write-Host "`n📝 PROSSIMI PASSI:" -ForegroundColor Yellow
Write-Host "  1. Riavvia server (dotnet run)" -ForegroundColor White
Write-Host "  2. Salva una ricetta dal PLC usando il frontend" -ForegroundColor White
Write-Host "  3. Verifica che vengano salvati ~100 parametri (non più ~48)" -ForegroundColor White
Write-Host "  4. Controlla che CodiceParametro sia sequenziale 1,2,3..." -ForegroundColor White
Write-Host "  5. Verifica che Area sia 'DB' per tutti" -ForegroundColor White

Write-Host "`n🎯 ASPETTATA RICETTA COMPLETA:" -ForegroundColor Cyan
Write-Host "  - Parametri: ~100 (da offset 0 a 196)" -ForegroundColor White
Write-Host "  - Indirizzo min: 0 (era 102)" -ForegroundColor White
Write-Host "  - Indirizzo max: 196+" -ForegroundColor White
Write-Host "  - CodiceParametro: 1, 2, 3... 100" -ForegroundColor White
Write-Host "  - Area: 'DB' per tutti" -ForegroundColor White

Write-Host ""
