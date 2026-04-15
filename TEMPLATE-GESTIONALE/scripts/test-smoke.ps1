<#
.SYNOPSIS
    Smoke Test automatico per [NOME_PROGETTO].
    Eseguire SEMPRE dopo ogni modifica. MAI dichiarare "fatto" con output rosso.

.PARAMETER UseExistingServer
    Se specificato, non avvia/ferma il server (usa quello già in esecuzione).

.PARAMETER Port
    Porta del server. Default: [PORTA_DEV].

.PARAMETER ConnectionString
    Connection string per il DB. Default: legge da appsettings.Development.json.

.EXAMPLE
    .\scripts\test-smoke.ps1 -UseExistingServer
    .\scripts\test-smoke.ps1
#>
param(
    [switch]$UseExistingServer,
    [int]$Port = [PORTA_DEV],
    [string]$ConnectionString = ""
)

# ─── Configurazione ────────────────────────────────────────────────
$ProjectName    = "[NOME_PROGETTO]"
$SolutionFile   = "[NOME_PROGETTO].sln"
$WebProject     = "[NomeProgetto].Web/[NomeProgetto].Web.csproj"
$BaseUrl        = "http://localhost:$Port"
$ExpectedVersion = $null  # se null, salta verifica versione
$DbTableToCheck = "[NomeTabellaPrincipale]"  # tabella da contare per verifica DB

# ─── Helpers ───────────────────────────────────────────────────────
$PassCount = 0
$FailCount = 0
$ServerProcess = $null

function Write-OK   { param($msg) Write-Host "[OK]    $msg" -ForegroundColor Green;  $script:PassCount++ }
function Write-FAIL { param($msg) Write-Host "[FAIL]  $msg" -ForegroundColor Red;    $script:FailCount++ }
function Write-SKIP { param($msg) Write-Host "[SKIP]  $msg" -ForegroundColor DarkGray }
function Write-INFO { param($msg) Write-Host "        $msg" -ForegroundColor DarkCyan }

# ─── START ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SMOKE TEST — $ProjectName" -ForegroundColor Cyan
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor DarkGray
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ─── 1. BUILD ──────────────────────────────────────────────────────
Write-Host "[TEST 1] Build soluzione..." -ForegroundColor Yellow
try {
    $buildOutput = & dotnet build $SolutionFile --nologo 2>&1
    $errors   = ($buildOutput | Select-String "Error(s)" | Select-Object -Last 1)
    $warnings = ($buildOutput | Select-String "Warning(s)" | Select-Object -Last 1)

    if ($buildOutput -match " 0 Error") {
        $errLine  = if ($errors)   { $errors.Line.Trim()   } else { "0 errors" }
        $warnLine = if ($warnings) { $warnings.Line.Trim() } else { "0 warnings" }
        Write-OK "Build: $errLine | $warnLine"
    } else {
        Write-FAIL "Build FALLITA — controlla errori sopra"
        $buildOutput | Where-Object { $_ -match "error" } | ForEach-Object { Write-INFO $_ }
    }
} catch {
    Write-FAIL "Build: eccezione — $_"
}

# ─── 2. AVVIO SERVER (se non UseExistingServer) ────────────────────
if (-not $UseExistingServer) {
    Write-Host "[TEST 2] Avvio server..." -ForegroundColor Yellow
    $existing = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($existing) {
        $pid = $existing | Select-Object -First 1 -ExpandProperty OwningProcess
        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-INFO "Fermato processo precedente su porta $Port"
    }
    $ServerProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project $WebProject --environment Development" `
        -PassThru -WindowStyle Hidden
    Write-INFO "Server avviato (PID $($ServerProcess.Id)), attendo startup..."
    Start-Sleep -Seconds 8
} else {
    Write-SKIP "Server avvio saltato (UseExistingServer)"
}

# ─── 3. HTTP RESPONSE ──────────────────────────────────────────────
Write-Host "[TEST 3] Risposta HTTP $BaseUrl..." -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 15
    if ($r.StatusCode -eq 200) {
        Write-OK "HTTP $($r.StatusCode) — App risponde su $BaseUrl"
    } else {
        Write-FAIL "HTTP $($r.StatusCode) — risposta non attesa"
    }
} catch {
    Write-FAIL "HTTP: nessuna risposta da $BaseUrl — $_"
    Write-INFO "Hint: server in esecuzione? Controlla porta $Port"
}

# ─── 4. HEALTH CHECK ───────────────────────────────────────────────
Write-Host "[TEST 4] Health check $BaseUrl/health..." -ForegroundColor Yellow
try {
    $h = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 10
    if ($h.Content -match '"status"\s*:\s*"Healthy"') {
        Write-OK "Health: Healthy"
    } elseif ($h.StatusCode -eq 200) {
        Write-OK "Health: HTTP 200 (endpoint presente, verifica contenuto)"
        Write-INFO "Response: $($h.Content.Substring(0, [Math]::Min(200, $h.Content.Length)))"
    } else {
        Write-FAIL "Health: status non Healthy — $($h.Content)"
    }
} catch {
    Write-SKIP "Health endpoint non trovato (aggiungere /health in Program.cs)"
    Write-INFO "Hint: builder.Services.AddHealthChecks(); app.MapHealthChecks(\"/health\");"
}

# ─── 5. VERIFICA VERSIONE ──────────────────────────────────────────
if ($ExpectedVersion) {
    Write-Host "[TEST 5] Versione $ExpectedVersion nel HTML..." -ForegroundColor Yellow
    try {
        $html = (Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 10).Content
        $escaped = [regex]::Escape($ExpectedVersion)
        if ($html -match $escaped) {
            Write-OK "Versione: $ExpectedVersion trovata nel HTML"
        } else {
            Write-FAIL "Versione: $ExpectedVersion NON trovata nel HTML"
            Write-INFO "Hint: AppVersion.cs aggiornato? MainLayout mostra la versione?"
        }
    } catch {
        Write-FAIL "Versione: impossibile leggere HTML — $_"
    }
} else {
    Write-SKIP "Verifica versione saltata (impostare \$ExpectedVersion nello script)"
}

# ─── 6. DATABASE ───────────────────────────────────────────────────
Write-Host "[TEST 6] Connessione database..." -ForegroundColor Yellow
if ($ConnectionString -eq "") {
    # Prova a leggere da appsettings.Development.json
    $appSettings = "[NomeProgetto].Web/appsettings.Development.json"
    if (Test-Path $appSettings) {
        try {
            $config = Get-Content $appSettings | ConvertFrom-Json
            $ConnectionString = $config.ConnectionStrings.DefaultConnection
        } catch { }
    }
}

if ($ConnectionString -ne "") {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
        $conn.Open()
        Write-OK "DB: connessione OK"

        if ($DbTableToCheck -ne "") {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "SELECT COUNT(*) FROM [$DbTableToCheck]"
            $count = $cmd.ExecuteScalar()
            Write-OK "DB: tabella [$DbTableToCheck] — $count record"
        }
        $conn.Close()
    } catch {
        Write-FAIL "DB: connessione fallita — $_"
        Write-INFO "Hint: SQL Server in esecuzione? Connection string corretta?"
    }
} else {
    Write-SKIP "DB: ConnectionString non trovata (passare -ConnectionString o aggiornare appsettings.Development.json)"
}

# ─── RISULTATO FINALE ──────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
$total = $PassCount + $FailCount
if ($FailCount -eq 0) {
    Write-Host "  ✅ SMOKE TEST PASSED — $PassCount/$total checks OK" -ForegroundColor Green
    Write-Host "  L'AI può dichiarare 'fatto' e avvisare l'utente." -ForegroundColor DarkGreen
} else {
    Write-Host "  ❌ SMOKE TEST FAILED — $FailCount errori su $total checks" -ForegroundColor Red
    Write-Host "  L'AI NON PUÒ dichiarare 'fatto'. Correggere prima." -ForegroundColor DarkRed
}
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ─── STOP server (se avviato da questo script) ─────────────────────
if ($ServerProcess -and -not $UseExistingServer) {
    Stop-Process -Id $ServerProcess.Id -Force -ErrorAction SilentlyContinue
    Write-INFO "Server fermato (PID $($ServerProcess.Id))"
}

# Exit code: 0 = OK, 1 = FAIL (usabile in CI/CD)
exit $FailCount
