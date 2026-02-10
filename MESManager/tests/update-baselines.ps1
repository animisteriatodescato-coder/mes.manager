param(
    [string]$BaseUrl = "http://localhost:5156",
    [switch]$UseExistingServer = $true,
    [switch]$Seed
)

if ($UseExistingServer) {
    $env:E2E_USE_EXISTING_SERVER = "1"
    $env:E2E_BASE_URL = $BaseUrl
}

if ($Seed) {
    $env:E2E_SEED = "1"
}

$flagPath = "C:\Dev\MESManager\tests\MESManager.E2E\UPDATE_BASELINES.flag"
New-Item -ItemType File -Path $flagPath -Force | Out-Null

Push-Location "C:\Dev\MESManager\tests\MESManager.E2E"
try {
    dotnet test --filter "Category=Visual"
}
finally {
    Remove-Item -Path $flagPath -Force -ErrorAction SilentlyContinue
    Pop-Location
}
