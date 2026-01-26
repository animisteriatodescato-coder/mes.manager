# Script per sincronizzare PreferenzeUtente da locale a produzione
# Mantiene le preferenze locali come master

$localServer = ".\SQLEXPRESS01"
$localDb = "MESManager"
$prodServer = "192.168.1.230\SQLEXPRESS01"
$prodDb = "MESManager_Prod"
$prodUser = "FAB"
$prodPassword = "password.123"

Write-Host "=== Sincronizzazione Preferenze Utenti ===" -ForegroundColor Cyan
Write-Host ""

# Primo: backup delle preferenze di produzione
Write-Host "1. Backup preferenze produzione..." -ForegroundColor Yellow
$backupQuery = @"
SELECT p.Id, u.Nome, p.Chiave, p.ValoreJson, p.DataCreazione, p.UltimaModifica 
FROM PreferenzeUtente p 
JOIN UtentiApp u ON p.UtenteAppId = u.Id 
ORDER BY u.Nome, p.Chiave
"@
sqlcmd -S $prodServer -d $prodDb -U $prodUser -P $prodPassword -C -Q $backupQuery -W

# Secondo: cancella preferenze esistenti in produzione
Write-Host "`n2. Pulizia preferenze produzione..." -ForegroundColor Yellow
$deleteQuery = "DELETE FROM PreferenzeUtente; SELECT @@ROWCOUNT as 'Preferenze eliminate';"
sqlcmd -S $prodServer -d $prodDb -U $prodUser -P $prodPassword -C -Q $deleteQuery -W

# Terzo: esporta preferenze locali con linked server o approccio diretto
Write-Host "`n3. Copia preferenze da locale a produzione..." -ForegroundColor Yellow

# Usa BCP per esportare e reimportare
$tempFile = "C:\Dev\MESManager\preferenze-temp.dat"

# Esporta da locale
Write-Host "   Esportazione da locale..." -ForegroundColor Gray
bcp "SELECT Id, UtenteAppId, Chiave, ValoreJson, DataCreazione, UltimaModifica FROM MESManager.dbo.PreferenzeUtente" queryout $tempFile -S $localServer -T -c -C 65001

# Importa in produzione
Write-Host "   Importazione in produzione..." -ForegroundColor Gray
bcp "MESManager_Prod.dbo.PreferenzeUtente" in $tempFile -S $prodServer -U $prodUser -P $prodPassword -c -C 65001

# Pulizia
Remove-Item $tempFile -ErrorAction SilentlyContinue

# Verifica finale
Write-Host "`n4. Verifica finale..." -ForegroundColor Yellow
$verifyQuery = @"
SELECT u.Nome, COUNT(*) as NumeroPreferenze 
FROM PreferenzeUtente p 
JOIN UtentiApp u ON p.UtenteAppId = u.Id 
GROUP BY u.Nome 
ORDER BY u.Nome
"@
sqlcmd -S $prodServer -d $prodDb -U $prodUser -P $prodPassword -C -Q $verifyQuery -W

Write-Host "`n=== Sincronizzazione completata ===" -ForegroundColor Green
