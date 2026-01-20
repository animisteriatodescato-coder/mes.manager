# Script per testare le connessioni SQL Server

# Carica l'assembly System.Data.SqlClient
Add-Type -AssemblyName "System.Data"

Write-Host "`n=== TEST CONNESSIONE SQL SERVER ===" -ForegroundColor Cyan

# Test 1: SQL Server locale (MESManager)
Write-Host "`n1. Testing localhost\SQLEXPRESS (MESManager)..." -ForegroundColor Yellow
$localConnString = "Server=localhost\SQLEXPRESS;Database=MESManager;Integrated Security=True;TrustServerCertificate=True;"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($localConnString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "  ✓ Connesso a: $($reader['ServerName'])" -ForegroundColor Green
        Write-Host "  ✓ Database: $($reader['DatabaseName'])" -ForegroundColor Green
    }
    $reader.Close()
    $conn.Close()
    Write-Host "  ✓ Test SUPERATO" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: SQL Server remoto (Mago)
Write-Host "`n2. Testing 192.168.1.72\SQLEXPRESS (Mago)..." -ForegroundColor Yellow
$magoConnString = "Data Source=192.168.1.72\SQLEXPRESS;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Connection Timeout=30;"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($magoConnString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, COUNT(*) AS CommesseCount FROM MA_SaleOrd"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "  ✓ Connesso a: $($reader['ServerName'])" -ForegroundColor Green
        Write-Host "  ✓ Database: $($reader['DatabaseName'])" -ForegroundColor Green
        Write-Host "  ✓ Numero commesse: $($reader['CommesseCount'])" -ForegroundColor Green
    }
    $reader.Close()
    $conn.Close()
    Write-Host "  ✓ Test SUPERATO" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: SQL Server remoto (Gantt)
Write-Host "`n3. Testing 192.168.1.230\SQLEXPRESS (Gantt)..." -ForegroundColor Yellow
$ganttConnString = "Server=192.168.1.230\SQLEXPRESS;Database=Gantt;User Id=sa;Password=password.123;TrustServerCertificate=True;"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($ganttConnString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "  ✓ Connesso a: $($reader['ServerName'])" -ForegroundColor Green
        Write-Host "  ✓ Database: $($reader['DatabaseName'])" -ForegroundColor Green
    }
    $reader.Close()
    $conn.Close()
    Write-Host "  ✓ Test SUPERATO" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== FINE TEST ===" -ForegroundColor Cyan
