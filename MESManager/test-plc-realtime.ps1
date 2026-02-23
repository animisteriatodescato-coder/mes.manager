#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test automatico PLC Realtime - Console Errors & Lifecycle
.DESCRIPTION
    Esegue test E2E Playwright specifici per PLC Realtime:
    - Caricamento pagina senza errori console
    - Lifecycle completo (load + navigate away)
    - Navigazione tra pagine Produzione
    
    Fallisce se trova errori console o eccezioni JavaScript.
.PARAMETER UseExistingServer
    Se specificato, usa server già in esecuzione su porta 5156
.PARAMETER Headed
    Se specificato, mostra il browser durante i test (utile per debug)
.PARAMETER SlowMo
    Millisecondi di ritardo tra operazioni (default: 0)
.EXAMPLE
    .\test-plc-realtime.ps1
    Test con server auto-start (headless)
.EXAMPLE
    .\test-plc-realtime.ps1 -UseExistingServer -Headed
    Test con server esistente, browser visibile
#>

param(
    [switch]$UseExistingServer,
    [switch]$Headed,
    [int]$SlowMo = 0
)

$ErrorActionPreference = "Stop"

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  🧪 TEST AUTOMATICO PLC REALTIME - Console Errors Lifecycle   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Configurazione ambiente test
$env:E2E_BASE_URL = "http://localhost:5156"
if ($UseExistingServer) {
    $env:E2E_USE_EXISTING_SERVER = "1"
    Write-Host "✓ Modalità: Server esistente" -ForegroundColor Yellow
} else {
    $env:E2E_USE_EXISTING_SERVER = "0"
    Write-Host "✓ Modalità: Auto-start server" -ForegroundColor Yellow
}

if ($Headed) {
    $env:PLAYWRIGHT_HEADED = "1"
    Write-Host "✓ Browser: Visibile (Headed)" -ForegroundColor Yellow
} else {
    $env:PLAYWRIGHT_HEADED = "0"
    Write-Host "✓ Browser: Headless" -ForegroundColor Yellow
}

if ($SlowMo -gt 0) {
    $env:PLAYWRIGHT_SLOWMO = $SlowMo.ToString()
    Write-Host "✓ SlowMo: $SlowMo ms" -ForegroundColor Yellow
}

Write-Host ""

# Naviga alla cartella test
$testProjectPath = Join-Path $PSScriptRoot "tests\MESManager.E2E"
if (-not (Test-Path $testProjectPath)) {
    Write-Host "❌ Errore: Directory test non trovata: $testProjectPath" -ForegroundColor Red
    exit 1
}

Push-Location $testProjectPath

try {
    Write-Host "📂 Directory test: $testProjectPath`n" -ForegroundColor Gray

    # Verifica se Playwright è installato
    $playwrightBinaryCheck = Join-Path "bin" "Debug" "net8.0" "playwright.ps1"
    if (-not (Test-Path $playwrightBinaryCheck)) {
        Write-Host "⚠️  Playwright non trovato, eseguo build..." -ForegroundColor Yellow
        dotnet build --nologo
    }

    Write-Host "🎯 Esecuzione test PLC Realtime:`n" -ForegroundColor Cyan

    # Esegue solo i test della feature Produzione
    $testFilter = "Feature=Produzione"
    
    Write-Host "   Filter: $testFilter`n" -ForegroundColor Gray

    # Esegue test con output dettagliato
    $testOutput = dotnet test `
        --filter $testFilter `
        --logger "console;verbosity=detailed" `
        --nologo `
        -- RunConfiguration.CollectSourceInformation=true
    
    $exitCode = $LASTEXITCODE

    Write-Host "`n" -NoNewline

    if ($exitCode -eq 0) {
        Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║                    ✅ TEST SUPERATI ✅                         ║" -ForegroundColor Green
        Write-Host "╠════════════════════════════════════════════════════════════════╣" -ForegroundColor Green
        Write-Host "║  Nessun errore console rilevato                                ║" -ForegroundColor Green
        Write-Host "║  Lifecycle Blazor funziona correttamente                       ║" -ForegroundColor Green
        Write-Host "║  Navigazione tra pagine OK                                     ║" -ForegroundColor Green
        Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
        Write-Host "║                    ❌ TEST FALLITI ❌                          ║" -ForegroundColor Red
        Write-Host "╠════════════════════════════════════════════════════════════════╣" -ForegroundColor Red
        Write-Host "║  Errori console rilevati o eccezioni JavaScript               ║" -ForegroundColor Red
        Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
        Write-Host ""
        Write-Host "📁 Artifacts salvati in: TestResults/Playwright/" -ForegroundColor Yellow
        Write-Host ""
        
        # Mostra ultimi errori se disponibili
        $errorLogPath = Get-ChildItem -Path "TestResults\Playwright" -Filter "errors.txt" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($errorLogPath) {
            Write-Host "🔍 Errori rilevati:" -ForegroundColor Red
            Write-Host (Get-Content $errorLogPath.FullName -Raw) -ForegroundColor Gray
        }
    }

    exit $exitCode

} finally {
    Pop-Location
}
