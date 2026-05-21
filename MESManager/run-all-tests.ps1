param(
    [ValidateSet("", "Unit", "Integration")]
    [string]$Category = "",
    [switch]$Coverage
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$UnitProj  = Join-Path $ScriptDir "tests\MESManager.UnitTests\MESManager.UnitTests.csproj"
$IntegProj = Join-Path $ScriptDir "tests\MESManager.IntegrationTests\MESManager.IntegrationTests.csproj"

$TotalFailed = 0
$Results     = @()
$StartTime   = Get-Date

Write-Host "`n???  Build in corso..." -ForegroundColor Yellow
dotnet build $UnitProj  --nologo -c Debug | Out-Null
dotnet build $IntegProj --nologo -c Debug | Out-Null
Write-Host "OK Build`n" -ForegroundColor Green

if ($Category -eq "" -or $Category -eq "Unit") {
    Write-Host "=== UNIT TESTS ===" -ForegroundColor Cyan
    dotnet test $UnitProj --nologo --no-build --logger "console;verbosity=normal"
    $unitExit = $LASTEXITCODE
    if ($unitExit -ne 0) { $TotalFailed++ }
    $Results += [PSCustomObject]@{ Suite = "Unit Tests"; ExitCode = $unitExit }
}

if ($Category -eq "" -or $Category -eq "Integration") {
    Write-Host "`n=== INTEGRATION TESTS ===" -ForegroundColor Cyan
    dotnet test $IntegProj --nologo --no-build --logger "console;verbosity=normal"
    $integExit = $LASTEXITCODE
    if ($integExit -ne 0) { $TotalFailed++ }
    $Results += [PSCustomObject]@{ Suite = "Integration Tests"; ExitCode = $integExit }
}

$Elapsed = (Get-Date) - $StartTime
Write-Host "`n=== RIEPILOGO | Durata: $($Elapsed.ToString('mm\:ss')) ===" -ForegroundColor White

foreach ($r in $Results) {
    $icon  = if ($r.ExitCode -eq 0) { "OK" } else { "FAIL" }
    $color = if ($r.ExitCode -eq 0) { "Green" } else { "Red" }
    Write-Host "  [$icon] $($r.Suite)" -ForegroundColor $color
}

Write-Host ""

if ($TotalFailed -gt 0) {
    Write-Host "DEPLOY BLOCCATO - $TotalFailed suite fallita/e" -ForegroundColor Red
    exit 1
}

Write-Host "Tutti i test VERDI - OK per deploy" -ForegroundColor Green
exit 0
