param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Programma", "Cataloghi", "Produzione", "Impostazioni")]
    [string]$Area,
    [string]$BaseUrl = "http://localhost:5156",
    [switch]$UseExistingServer = $true,
    [switch]$Seed
)

$filters = @{
    "Programma"     = "Feature=CommesseAperte|Feature=Gantt|Feature=ProgrammaMacchine"
    "Cataloghi"     = "Feature=Cataloghi"
    "Produzione"    = "Feature=Produzione"
    "Impostazioni"  = "Feature=Impostazioni"
}

$filter = $filters[$Area]
if (-not $filter) {
    Write-Error "Area non valida: $Area"
    exit 1
}

if ($UseExistingServer) {
    $env:E2E_USE_EXISTING_SERVER = "1"
    $env:E2E_BASE_URL = $BaseUrl
}

if ($Seed) {
    $env:E2E_SEED = "1"
}

Push-Location "C:\Dev\MESManager\tests\MESManager.E2E"
try {
    dotnet test --filter "$filter"
}
finally {
    Pop-Location
}
