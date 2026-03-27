################################################################################
# backup-preferenze.ps1
# Esporta tutte le righe di PreferenzeUtente + AspNetUsers.UserName in un JSON
# ripristinabile. Usare restore-preferenze.ps1 per reimportare.
#
# Uso:
#   .\backup-preferenze.ps1                         # crea backup con timestamp
#   .\backup-preferenze.ps1 -Nome "backup_base"     # nome personalizzato
################################################################################
param(
    [string]$Nome = ("backup_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
)

$connStr = "Server=192.168.1.230\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;"
$outDir  = Join-Path $PSScriptRoot "..\backups"
$outFile = Join-Path $outDir "$Nome.json"

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory $outDir | Out-Null }

# Carica assembly SQL Server
Add-Type -AssemblyName "System.Data"

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()

    $sql = @"
SELECT
    p.Id,
    p.UserId,
    u.UserName,
    u.Nome        AS NomeUtente,
    p.Chiave,
    p.ValoreJson,
    p.DataCreazione,
    p.UltimaModifica
FROM PreferenzeUtente p
JOIN AspNetUsers u ON u.Id = p.UserId
ORDER BY u.UserName, p.Chiave
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $reader = $cmd.ExecuteReader()

    $rows = [System.Collections.Generic.List[object]]::new()
    while ($reader.Read()) {
        $rows.Add([pscustomobject]@{
            Id            = $reader["Id"].ToString()
            UserId        = $reader["UserId"].ToString()
            UserName      = $reader["UserName"].ToString()
            NomeUtente    = if ($reader["NomeUtente"] -is [DBNull]) { "" } else { $reader["NomeUtente"].ToString() }
            Chiave        = $reader["Chiave"].ToString()
            ValoreJson    = $reader["ValoreJson"].ToString()
            DataCreazione = $reader["DataCreazione"].ToString()
            UltimaModifica = $reader["UltimaModifica"].ToString()
        })
    }
    $reader.Close()

    $export = [pscustomobject]@{
        NomeBackup  = $Nome
        DataBackup  = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        TotaleRighe = $rows.Count
        Preferenze  = $rows
    }

    $json = $export | ConvertTo-Json -Depth 5
    $json | Out-File -FilePath $outFile -Encoding UTF8

    Write-Host ""
    Write-Host "✅ Backup completato: $outFile" -ForegroundColor Green
    Write-Host "   Righe salvate : $($rows.Count)" -ForegroundColor Cyan
    Write-Host "   Utenti trovati: $(($rows | Select-Object -Unique UserName).Count)" -ForegroundColor Cyan

    # Riepilogo per utente
    Write-Host ""
    Write-Host "Riepilogo per utente:" -ForegroundColor Yellow
    $rows | Group-Object UserName | ForEach-Object {
        Write-Host ("  {0,-20} {1,3} chiavi" -f $_.Name, $_.Count)
    }

} finally {
    $conn.Close()
}
