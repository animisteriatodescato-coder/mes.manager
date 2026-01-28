# Publish executables for Windows (win-x64)
param(
    [string[]]$Projects = @(
        "MESManager.Web/MESManager.Web.csproj",
        "MESManager.Worker/MESManager.Worker.csproj",
        "MESManager.PlcSync/MESManager.PlcSync.csproj"
    ),
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [switch]$SingleFile = $true,
    [switch]$ReadyToRun = $false
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try {
    foreach ($proj in $Projects) {
        if (-not (Test-Path $proj)) {
            Write-Warning "Skipping missing project: $proj"
            continue
        }

        $projName = [IO.Path]::GetFileNameWithoutExtension($proj)
        $outDir = Join-Path "publish" (Join-Path $Runtime (Join-Path "Release" $projName))
        $args = @(
            'publish', $proj,
            '-c', 'Release',
            '-r', $Runtime,
            '/p:IncludeNativeLibrariesForSelfExtract=true'
        )

        if ($SelfContained) { $args += @('--self-contained', 'true') }
        if ($SingleFile)   { $args += @('/p:PublishSingleFile=true') }
        if ($ReadyToRun)   { $args += @('/p:PublishReadyToRun=true') }

        # Avoid trimming by default to reduce runtime surprises (reflection-heavy apps)
        # Add '/p:PublishTrimmed=true' manually if you have validated safety per project.

        $args += @('--output', $outDir)

        Write-Host "\n=== Publishing $projName -> $outDir ===" -ForegroundColor Cyan
        dotnet @args

        $exe = Get-ChildItem -Path $outDir -Filter "$projName*.exe" -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($exe) {
            Write-Host "Built: $($exe.FullName)" -ForegroundColor Green
        } else {
            Write-Warning "No .exe found in $outDir (project may target non-Windows or be library)."
        }
    }

    Write-Host "\nDone. Outputs under: $(Join-Path $PSScriptRoot 'publish')" -ForegroundColor Green
}
finally {
    Pop-Location
}
