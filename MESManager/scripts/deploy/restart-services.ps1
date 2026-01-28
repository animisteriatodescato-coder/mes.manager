<#
.SYNOPSIS
    Riavvia i servizi MESManager in modo sicuro con graceful shutdown.
.DESCRIPTION
    Questo script:
    1. Ferma i servizi in ordine (prima PLC, poi Worker, poi Web)
    2. Attende il graceful shutdown
    3. Riavvia i servizi in ordine inverso
    
    Usare questo script invece di riavviare manualmente per evitare
    problemi di sincronizzazione con Mago e PLC.
.EXAMPLE
    .\restart-services.ps1
    .\restart-services.ps1 -Service PlcSync
    .\restart-services.ps1 -WaitTime 15
#>

param(
    [ValidateSet("All", "Web", "Worker", "PlcSync")]
    [string]$Service = "All",
    
    [int]$WaitTime = 30,
    
    [switch]$Force
)

$ErrorActionPreference = 'Continue'

$Services = @{
    "PlcSync" = @{
        Name = "MESManager.PlcSync"
        DisplayName = "MESManager PLC Sync"
        Order = 1
    }
    "Worker" = @{
        Name = "MESManager.Worker"
        DisplayName = "MESManager Worker (Mago)"
        Order = 2
    }
    "Web" = @{
        Name = "MESManagerWeb"  # O IIS Application Pool
        DisplayName = "MESManager Web"
        Order = 3
        IsIIS = $true
    }
}

function Stop-ServiceGraceful {
    param(
        [string]$ServiceName,
        [string]$DisplayName,
        [int]$TimeoutSeconds = 30,
        [switch]$Force
    )
    
    Write-Host "🛑 Fermando $DisplayName..." -ForegroundColor Yellow
    
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "  ⚠️ Servizio $ServiceName non trovato" -ForegroundColor DarkYellow
        return $true
    }
    
    if ($svc.Status -ne 'Running') {
        Write-Host "  ℹ️ Servizio già fermo" -ForegroundColor Gray
        return $true
    }
    
    # Invia segnale di stop
    Stop-Service -Name $ServiceName -Force:$Force -ErrorAction SilentlyContinue
    
    # Attendi graceful shutdown
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        Start-Sleep -Seconds 2
        $elapsed += 2
        
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($svc.Status -eq 'Stopped') {
            Write-Host "  ✅ $DisplayName fermato dopo $elapsed secondi" -ForegroundColor Green
            return $true
        }
        
        Write-Host "  ⏳ Attesa shutdown... ($elapsed/$TimeoutSeconds sec)" -ForegroundColor Gray
    }
    
    # Timeout - forza kill
    Write-Host "  ⚠️ Timeout - forzo terminazione..." -ForegroundColor Red
    $processName = $ServiceName -replace '\.' , ''
    Get-Process -Name "*$processName*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    return $false
}

function Start-ServiceSafe {
    param(
        [string]$ServiceName,
        [string]$DisplayName,
        [int]$TimeoutSeconds = 20
    )
    
    Write-Host "🚀 Avviando $DisplayName..." -ForegroundColor Cyan
    
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "  ⚠️ Servizio $ServiceName non trovato" -ForegroundColor DarkYellow
        return $false
    }
    
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    # Attendi avvio
    $elapsed = 0
    while ($elapsed -lt $TimeoutSeconds) {
        Start-Sleep -Seconds 2
        $elapsed += 2
        
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($svc.Status -eq 'Running') {
            Write-Host "  ✅ $DisplayName avviato!" -ForegroundColor Green
            return $true
        }
        
        Write-Host "  ⏳ Attesa avvio... ($elapsed/$TimeoutSeconds sec)" -ForegroundColor Gray
    }
    
    Write-Host "  ❌ $DisplayName non avviato correttamente" -ForegroundColor Red
    return $false
}

# Main
Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║           MESManager Services Restart Script                      ║
╠═══════════════════════════════════════════════════════════════════╣
║  Questo script riavvia i servizi con graceful shutdown per        ║
║  evitare problemi di sincronizzazione con Mago e PLC.             ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Servizio: $Service" -ForegroundColor White
Write-Host "Timeout: $WaitTime secondi" -ForegroundColor White
Write-Host ""

# Determina quali servizi gestire
$targetServices = if ($Service -eq "All") {
    $Services.GetEnumerator() | Sort-Object { $_.Value.Order }
} else {
    @{ $Service = $Services[$Service] }.GetEnumerator()
}

# FASE 1: Stop servizi (in ordine)
Write-Host "═══ FASE 1: STOP SERVIZI ═══" -ForegroundColor Magenta
foreach ($svc in $targetServices) {
    if (-not $svc.Value.IsIIS) {
        Stop-ServiceGraceful -ServiceName $svc.Value.Name -DisplayName $svc.Value.DisplayName -TimeoutSeconds $WaitTime -Force:$Force
    }
}

# Pausa tra stop e start
Write-Host ""
Write-Host "⏸️ Pausa di 5 secondi prima del riavvio..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# FASE 2: Start servizi (in ordine inverso)
Write-Host ""
Write-Host "═══ FASE 2: START SERVIZI ═══" -ForegroundColor Magenta
$reverseServices = $targetServices | Sort-Object { $_.Value.Order } -Descending
foreach ($svc in $reverseServices) {
    if (-not $svc.Value.IsIIS) {
        Start-ServiceSafe -ServiceName $svc.Value.Name -DisplayName $svc.Value.DisplayName
    }
}

Write-Host ""
Write-Host "═══ STATO FINALE ═══" -ForegroundColor Magenta
foreach ($svc in $targetServices) {
    if (-not $svc.Value.IsIIS) {
        $status = (Get-Service -Name $svc.Value.Name -ErrorAction SilentlyContinue).Status
        $color = if ($status -eq 'Running') { 'Green' } else { 'Red' }
        Write-Host "  $($svc.Value.DisplayName): $status" -ForegroundColor $color
    }
}

Write-Host ""
Write-Host "✅ Operazione completata!" -ForegroundColor Green
