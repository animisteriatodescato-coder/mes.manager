Param(
    [string]$OutputPath = "C:\MESManager\Web\appsettings.Secrets.json",
    [switch]$Encrypt
)

# ATTENZIONE: questo script crea il file dei segreti in chiaro.
# Non trasferire l'encrypted dalla macchina di sviluppo al server.

$dir = Split-Path $OutputPath -Parent
if (-not (Test-Path $dir)) {
    New-Item -Path $dir -ItemType Directory -Force | Out-Null
}

$secrets = @{
    DatabaseConfiguration = @{
        MESManagerDb = "Server=192.168.1.72;Database=MESManager;User Id=FAB;Password=password.123;TrustServerCertificate=True;"
        MagoDb       = "Server=192.168.1.230;Database=Mago;User Id=sa;Password=password.123;TrustServerCertificate=True;"
        GanttDb      = "Server=192.168.1.72;Database=Gantt2019;User Id=sa;Password=Gantt2019;TrustServerCertificate=True;"
    }
}

$json = $secrets | ConvertTo-Json -Depth 10

# Scrive il file in UTF8
$json | Out-File -FilePath $OutputPath -Encoding utf8
Write-Host "✅ File creato: $OutputPath"

if ($Encrypt) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $protectScript = Join-Path $scriptDir "protect-secrets.ps1"
    if (-not (Test-Path $protectScript)) {
        Write-Warning "protect-secrets.ps1 non trovato in $scriptDir. Copia lo script nello stesso folder oppure esegui la criptazione manuale sul server."
    } else {
        Write-Host "→ Chiamando $protectScript -Encrypt -FilePath $OutputPath"
        & $protectScript -Encrypt -FilePath $OutputPath
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ File criptato correttamente (e file originale eliminato dal protector script)."
        } else {
            Write-Warning "Errore nella criptazione. Controlla l'output di protect-secrets.ps1"
        }
    }
}
