# Script per verificare che la configurazione centralizzata funzioni
Write-Host "=== VERIFICA CONFIGURAZIONE DATABASE CENTRALIZZATA ===" -ForegroundColor Cyan
Write-Host ""

$databaseConfigFile = "c:\Dev\MESManager\appsettings.Database.json"

# 1. Verifica esistenza file
Write-Host "1. Verifica file di configurazione..." -ForegroundColor Yellow
if (Test-Path $databaseConfigFile) {
    Write-Host "   ✓ File trovato: appsettings.Database.json" -ForegroundColor Green
    
    # Legge il contenuto
    $config = Get-Content $databaseConfigFile | ConvertFrom-Json
    
    Write-Host ""
    Write-Host "   Connection Strings configurate:" -ForegroundColor Cyan
    $config.ConnectionStrings.PSObject.Properties | ForEach-Object {
        Write-Host "   - $($_.Name)" -ForegroundColor White
    }
} else {
    Write-Host "   ✗ ERRORE: File non trovato!" -ForegroundColor Red
    exit 1
}

# 2. Verifica sintassi JSON
Write-Host ""
Write-Host "2. Verifica sintassi JSON..." -ForegroundColor Yellow
try {
    $json = Get-Content $databaseConfigFile -Raw | ConvertFrom-Json
    Write-Host "   ✓ JSON valido" -ForegroundColor Green
} catch {
    Write-Host "   ✗ ERRORE: JSON non valido!" -ForegroundColor Red
    Write-Host "   $_" -ForegroundColor Red
    exit 1
}

# 3. Compila i progetti
Write-Host ""
Write-Host "3. Compilazione progetti..." -ForegroundColor Yellow

$projects = @(
    "MESManager.Web\MESManager.Web.csproj",
    "MESManager.Worker\MESManager.Worker.csproj",
    "MESManager.PlcSync\MESManager.PlcSync.csproj"
)

$allSuccess = $true
foreach ($project in $projects) {
    $projectName = Split-Path $project -Leaf
    Write-Host "   Compilando $projectName..." -ForegroundColor Cyan
    
    $result = dotnet build $project --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ $projectName OK" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $projectName ERRORE" -ForegroundColor Red
        $allSuccess = $false
    }
}

# 4. Test Entity Framework
Write-Host ""
Write-Host "4. Test Entity Framework..." -ForegroundColor Yellow
Push-Location "MESManager.Infrastructure"
$efResult = dotnet ef database update --startup-project ..\MESManager.Web\MESManager.Web.csproj --verbosity quiet 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Entity Framework legge correttamente il file" -ForegroundColor Green
} else {
    Write-Host "   ✗ Entity Framework non riesce a leggere la configurazione" -ForegroundColor Red
    Write-Host "   $efResult" -ForegroundColor Red
    $allSuccess = $false
}
Pop-Location

# 5. Test connessione database
Write-Host ""
Write-Host "5. Test connessione database..." -ForegroundColor Yellow

$connectionString = $config.ConnectionStrings.MESManagerDb
if ($connectionString -match "Server=([^;]+);") {
    $server = $matches[1]
    Write-Host "   Server configurato: $server" -ForegroundColor Cyan
    
    try {
        Add-Type -AssemblyName "System.Data"
        $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $conn.Open()
        
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT DB_NAME() as DatabaseName"
        $reader = $cmd.ExecuteReader()
        
        if ($reader.Read()) {
            Write-Host "   ✓ Connesso al database: $($reader['DatabaseName'])" -ForegroundColor Green
        }
        $reader.Close()
        $conn.Close()
    } catch {
        Write-Host "   ✗ ERRORE connessione: $_" -ForegroundColor Red
        $allSuccess = $false
    }
}

# Riepilogo
Write-Host ""
Write-Host "=== RIEPILOGO ===" -ForegroundColor Cyan
if ($allSuccess) {
    Write-Host "✓ Configurazione centralizzata funzionante!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Per cambiare server SQL, modifica SOLO:" -ForegroundColor Yellow
    Write-Host "  appsettings.Database.json" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "✗ Alcuni test sono falliti" -ForegroundColor Red
    Write-Host "Controlla gli errori sopra riportati" -ForegroundColor Yellow
}
