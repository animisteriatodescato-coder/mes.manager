################################################################################
# restore-preferenze.ps1
# Ripristina le preferenze da un file JSON creato da backup-preferenze.ps1
#
# Uso:
#   .\restore-preferenze.ps1 -File "..\backups\backup_base.json"
#   .\restore-preferenze.ps1 -File "..\backups\backup_base.json" -SoloUtente "NAION"
#   .\restore-preferenze.ps1 -File "..\backups\backup_base.json" -DryRun
################################################################################
param(
    [Parameter(Mandatory)][string]$File,
    [string]$SoloUtente = "",
    [switch]$DryRun
)

$connStr = "Server=192.168.1.230\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;"

if (-not (Test-Path $File)) { Write-Host "File non trovato: $File" -ForegroundColor Red; exit 1 }

$data = Get-Content $File -Encoding UTF8 | ConvertFrom-Json
$prefs = $data.Preferenze

if ($SoloUtente) {
    $prefs = $prefs | Where-Object { $_.UserName -eq $SoloUtente }
}

Write-Host "Backup: $($data.NomeBackup)  ($($data.DataBackup))" -ForegroundColor Cyan
Write-Host "Righe da ripristinare: $($prefs.Count)" -ForegroundColor Cyan
if ($DryRun) { Write-Host "[DRY-RUN] Nessuna modifica al DB" -ForegroundColor Yellow }

if (-not $DryRun) {
    $confirm = Read-Host "Confermi il ripristino? (s/N)"
    if ($confirm -notmatch "^[sSyY]$") { Write-Host "Annullato."; exit 0 }
}

Add-Type -AssemblyName "System.Data"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    $ok = 0; $skip = 0

    foreach ($p in $prefs) {
        if ($DryRun) {
            Write-Host "  [DRY] $($p.UserName) / $($p.Chiave)"
            continue
        }
        $sql = @"
MERGE PreferenzeUtente AS target
USING (SELECT @UserId AS UserId, @Chiave AS Chiave) AS src
ON target.UserId = src.UserId AND target.Chiave = src.Chiave
WHEN MATCHED THEN
    UPDATE SET ValoreJson = @Valore, UltimaModifica = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, Chiave, ValoreJson, DataCreazione, UltimaModifica)
    VALUES (NEWID(), @UserId, @Chiave, @Valore, GETDATE(), GETDATE());
"@
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.Parameters.AddWithValue("@UserId", $p.UserId) | Out-Null
        $cmd.Parameters.AddWithValue("@Chiave", $p.Chiave) | Out-Null
        $cmd.Parameters.AddWithValue("@Valore", $p.ValoreJson) | Out-Null
        $cmd.ExecuteNonQuery() | Out-Null
        $ok++
    }
    if (-not $DryRun) {
        Write-Host "✅ Ripristino completato: $ok righe aggiornate/inserite" -ForegroundColor Green
    }
} finally {
    $conn.Close()
}
