# ============================================================
# Script Diagnostico Connettività PLC
# Eseguire direttamente sul server 192.168.1.230
# ============================================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DIAGNOSTICA CONNETTIVITA' PLC" -ForegroundColor Cyan
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Lista IP PLC da testare
$plcList = @(
    @{ Numero = 2;  IP = "192.168.17.26" },
    @{ Numero = 3;  IP = "192.168.17.24" },
    @{ Numero = 5;  IP = "192.168.17.27" },
    @{ Numero = 6;  IP = "192.168.17.25" },
    @{ Numero = 7;  IP = "192.168.17.23" },
    @{ Numero = 8;  IP = "192.168.17.22" },
    @{ Numero = 9;  IP = "192.168.17.21" },
    @{ Numero = 10; IP = "192.168.17.20" }
)

# 1. Informazioni schede di rete
Write-Host "1. SCHEDE DI RETE ATTIVE" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" } | 
    Select-Object InterfaceAlias, IPAddress, PrefixLength | 
    Format-Table -AutoSize
Write-Host ""

# 2. Route di rete
Write-Host "2. ROUTE DI RETE (verso 192.168.17.x)" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
$routes = Get-NetRoute -AddressFamily IPv4 | Where-Object { 
    $_.DestinationPrefix -like "192.168.17.*" -or 
    $_.DestinationPrefix -eq "0.0.0.0/0" -or
    $_.DestinationPrefix -like "192.168.*"
} | Select-Object DestinationPrefix, NextHop, InterfaceAlias, RouteMetric
if ($routes) {
    $routes | Format-Table -AutoSize
} else {
    Write-Host "  ATTENZIONE: Nessuna route specifica per 192.168.17.x!" -ForegroundColor Red
    Write-Host "  Verificare se esiste un gateway o una seconda scheda di rete" -ForegroundColor Red
}
Write-Host ""

# 3. Test Ping a ogni PLC
Write-Host "3. TEST PING AI PLC" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
foreach ($plc in $plcList) {
    $result = Test-Connection -ComputerName $plc.IP -Count 1 -Quiet -ErrorAction SilentlyContinue
    if ($result) {
        Write-Host "  PLC $($plc.Numero) ($($plc.IP)): " -NoNewline
        Write-Host "OK - Raggiungibile" -ForegroundColor Green
    } else {
        Write-Host "  PLC $($plc.Numero) ($($plc.IP)): " -NoNewline
        Write-Host "ERRORE - Non raggiungibile" -ForegroundColor Red
    }
}
Write-Host ""

# 4. Test porta 102 (S7 Siemens)
Write-Host "4. TEST PORTA 102 (S7 Protocol)" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
foreach ($plc in $plcList) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $connect = $tcp.BeginConnect($plc.IP, 102, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(2000, $false)
        
        if ($wait -and $tcp.Connected) {
            Write-Host "  PLC $($plc.Numero) ($($plc.IP)):102 - " -NoNewline
            Write-Host "APERTA" -ForegroundColor Green
            $tcp.Close()
        } else {
            Write-Host "  PLC $($plc.Numero) ($($plc.IP)):102 - " -NoNewline
            Write-Host "TIMEOUT" -ForegroundColor Red
        }
    } catch {
        Write-Host "  PLC $($plc.Numero) ($($plc.IP)):102 - " -NoNewline
        Write-Host "RIFIUTATA" -ForegroundColor Red
    }
}
Write-Host ""

# 5. Firewall Windows
Write-Host "5. REGOLE FIREWALL PORTA 102" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
$rules = Get-NetFirewallRule -Direction Outbound -Enabled True -ErrorAction SilentlyContinue | 
    Get-NetFirewallPortFilter -ErrorAction SilentlyContinue | 
    Where-Object { $_.LocalPort -eq 102 -or $_.RemotePort -eq 102 }

if ($rules) {
    Write-Host "  Trovate regole firewall per porta 102" -ForegroundColor Green
} else {
    Write-Host "  Nessuna regola specifica - verificare se firewall blocca" -ForegroundColor Yellow
}

# Controlla se firewall è attivo
$fwProfiles = Get-NetFirewallProfile
foreach ($profile in $fwProfiles) {
    $status = if ($profile.Enabled) { "ATTIVO" } else { "DISATTIVO" }
    $color = if ($profile.Enabled) { "Yellow" } else { "Green" }
    Write-Host "  Profilo $($profile.Name): $status" -ForegroundColor $color
}
Write-Host ""

# 6. Traceroute al primo PLC
Write-Host "6. TRACEROUTE A 192.168.17.26 (PLC 2)" -ForegroundColor Yellow
Write-Host "-----------------------------------------"
try {
    $trace = Test-NetConnection -ComputerName "192.168.17.26" -TraceRoute -WarningAction SilentlyContinue
    if ($trace.TraceRoute) {
        foreach ($hop in $trace.TraceRoute) {
            Write-Host "  -> $hop"
        }
    } else {
        Write-Host "  Traceroute fallito - nessun percorso" -ForegroundColor Red
    }
} catch {
    Write-Host "  Errore durante traceroute" -ForegroundColor Red
}
Write-Host ""

# 7. Riepilogo
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RIEPILOGO" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

$hasRoute = (Get-NetRoute -AddressFamily IPv4 | Where-Object { $_.DestinationPrefix -like "192.168.17.*" }).Count -gt 0
$hasNic = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -like "192.168.17.*" }).Count -gt 0

if (-not $hasRoute -and -not $hasNic) {
    Write-Host ""
    Write-Host "  PROBLEMA IDENTIFICATO:" -ForegroundColor Red
    Write-Host "  Il server non ha accesso alla rete 192.168.17.x" -ForegroundColor Red
    Write-Host ""
    Write-Host "  SOLUZIONI POSSIBILI:" -ForegroundColor Yellow
    Write-Host "  1. Aggiungere una seconda scheda di rete con IP 192.168.17.x"
    Write-Host "  2. Configurare una route statica verso 192.168.17.0/24"
    Write-Host "     Esempio: route add 192.168.17.0 mask 255.255.255.0 <gateway>"
    Write-Host "  3. Verificare che il router/switch supporti il routing tra le subnet"
    Write-Host ""
} elseif ($hasNic) {
    Write-Host ""
    Write-Host "  Il server ha una scheda di rete sulla subnet 192.168.17.x" -ForegroundColor Green
    Write-Host "  Se i PLC non rispondono, verificare:" -ForegroundColor Yellow
    Write-Host "  - I PLC sono accesi e connessi alla rete"
    Write-Host "  - Gli IP configurati nei JSON corrispondono ai PLC reali"
    Write-Host "  - Non ci sono conflitti IP sulla rete"
    Write-Host ""
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Fine diagnostica" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
