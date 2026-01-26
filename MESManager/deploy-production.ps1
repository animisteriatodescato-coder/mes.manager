<#
.SYNOPSIS
    Deploy sicuro di MESManager sui server di produzione.
.DESCRIPTION
    Questo script:
    1. Compila l'applicazione
    2. Copia i file sul server (usa le credenziali Windows integrate)
    3. Gestisce le credenziali in modo sicuro (NON copia file secrets locali)
    
    Le credenziali del server devono essere configurate DIRETTAMENTE sul server,
    non vengono copiate dalla macchina di sviluppo.
.EXAMPLE
    .\deploy-production.ps1 -Target Web
    .\deploy-production.ps1 -Target Worker
    .\deploy-production.ps1 -Target All
#>

param(
    [ValidateSet("Web", "Worker", "PlcSync", "All")]
    [string]$Target = "Web",
    
    [string]$Server = "192.168.1.230",
    
    [switch]$SkipBuild,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Configurazione percorsi
$SolutionRoot = $PSScriptRoot
$PublishRoot = Join-Path $SolutionRoot "publish\win-x64\Release"

$Targets = @{
    "Web" = @{
        Project = "MESManager.Web/MESManager.Web.csproj"
        LocalPath = "MESManager.Web"
        RemotePath = "\\$Server\c$\MESManager\Web"
        ServiceName = $null  # IIS o processo standalone
    }
    "Worker" = @{
        Project = "MESManager.Worker/MESManager.Worker.csproj"
        LocalPath = "MESManager.Worker"
        RemotePath = "\\$Server\c$\MESManager\Worker"
        ServiceName = "MESManager.Worker"
    }
    "PlcSync" = @{
        Project = "MESManager.PlcSync/MESManager.PlcSync.csproj"
        LocalPath = "MESManager.PlcSync"
        RemotePath = "\\$Server\c$\MESManager\PlcSync"
        ServiceName = "MESManager.PlcSync"
    }
}

# File da NON copiare (sicurezza)
$ExcludeFiles = @(
    "appsettings.Secrets.json",
    "appsettings.Secrets.encrypted",
    "appsettings.Database.json",
    "*.log"
)

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Deploy-Target {
    param([string]$TargetName)
    
    $config = $Targets[$TargetName]
    $publishPath = Join-Path $PublishRoot $config.LocalPath
    
    Write-Step "Deploying $TargetName"
    
    # 1. Build
    if (-not $SkipBuild) {
        Write-Host "Building $TargetName..." -ForegroundColor Yellow
        $buildArgs = @(
            'publish', $config.Project,
            '-c', 'Release',
            '-r', 'win-x64',
            '--self-contained', 'true',
            '/p:PublishSingleFile=true',
            '/p:IncludeNativeLibrariesForSelfExtract=true',
            '--output', $publishPath
        )
        
        if ($WhatIf) {
            Write-Host "[WhatIf] dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
        } else {
            Push-Location $SolutionRoot
            dotnet @buildArgs
            Pop-Location
        }
    }
    
    # 2. Verifica che esista il path di publish
    if (-not (Test-Path $publishPath) -and -not $WhatIf) {
        throw "Publish path not found: $publishPath. Run without -SkipBuild."
    }
    
    # 3. Stop servizio remoto (se Windows Service)
    if ($config.ServiceName) {
        Write-Host "Stopping service $($config.ServiceName) on $Server..." -ForegroundColor Yellow
        if ($WhatIf) {
            Write-Host "[WhatIf] Stop-Service -Name $($config.ServiceName)" -ForegroundColor Gray
        } else {
            try {
                Invoke-Command -ComputerName $Server -ScriptBlock {
                    param($svc)
                    Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Seconds 2
                } -ArgumentList $config.ServiceName -ErrorAction SilentlyContinue
            } catch {
                Write-Warning "Could not stop service: $_"
            }
        }
    }
    
    # 4. Copia file (ESCLUDENDO file sensibili)
    Write-Host "Copying files to $($config.RemotePath)..." -ForegroundColor Yellow
    Write-Host "  (Excluding: $($ExcludeFiles -join ', '))" -ForegroundColor Gray
    
    if ($WhatIf) {
        Write-Host "[WhatIf] Copy-Item $publishPath\* -> $($config.RemotePath)" -ForegroundColor Gray
    } else {
        # Crea directory se non esiste
        if (-not (Test-Path $config.RemotePath)) {
            New-Item -ItemType Directory -Path $config.RemotePath -Force | Out-Null
        }
        
        # Copia file escludendo quelli sensibili
        Get-ChildItem -Path $publishPath -Recurse | Where-Object {
            $file = $_
            $exclude = $false
            foreach ($pattern in $ExcludeFiles) {
                if ($file.Name -like $pattern) {
                    $exclude = $true
                    break
                }
            }
            -not $exclude
        } | ForEach-Object {
            $relativePath = $_.FullName.Substring($publishPath.Length)
            $destPath = Join-Path $config.RemotePath $relativePath
            
            if ($_.PSIsContainer) {
                if (-not (Test-Path $destPath)) {
                    New-Item -ItemType Directory -Path $destPath -Force | Out-Null
                }
            } else {
                $destDir = Split-Path $destPath -Parent
                if (-not (Test-Path $destDir)) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                }
                Copy-Item -Path $_.FullName -Destination $destPath -Force
            }
        }
    }
    
    # 5. Restart servizio
    if ($config.ServiceName) {
        Write-Host "Starting service $($config.ServiceName)..." -ForegroundColor Yellow
        if ($WhatIf) {
            Write-Host "[WhatIf] Start-Service -Name $($config.ServiceName)" -ForegroundColor Gray
        } else {
            try {
                Invoke-Command -ComputerName $Server -ScriptBlock {
                    param($svc)
                    Start-Service -Name $svc -ErrorAction SilentlyContinue
                } -ArgumentList $config.ServiceName -ErrorAction SilentlyContinue
            } catch {
                Write-Warning "Could not start service: $_"
            }
        }
    }
    
    Write-Host "✅ $TargetName deployed successfully!" -ForegroundColor Green
}

# Main
Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║           MESManager Secure Deployment Script                     ║
╠═══════════════════════════════════════════════════════════════════╣
║  ⚠️  NOTA SICUREZZA:                                               ║
║  Le credenziali NON vengono copiate da questo script.             ║
║  Il file appsettings.Secrets.json deve essere configurato         ║
║  DIRETTAMENTE sul server di produzione.                           ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Target: $Target" -ForegroundColor White
Write-Host "Server: $Server" -ForegroundColor White
Write-Host "WhatIf: $WhatIf" -ForegroundColor White

if ($Target -eq "All") {
    foreach ($t in @("Web", "Worker", "PlcSync")) {
        Deploy-Target -TargetName $t
    }
} else {
    Deploy-Target -TargetName $Target
}

Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║  ✅ Deployment completato!                                        ║
║                                                                   ║
║  Ricorda: Se è la prima installazione, configura le credenziali  ║
║  sul server creando il file appsettings.Secrets.json              ║
║  nella cartella dell'applicazione.                                ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green
