# Script PowerShell per creare utente FAB su SQL Server
Write-Host "=== CREAZIONE UTENTE FAB ===" -ForegroundColor Cyan
Write-Host ""

try {
    Add-Type -AssemblyName "System.Data"
    
    # Connessione come sa
    Write-Host "1. Connessione al server 192.168.1.230\SQLEXPRESS come sa..." -ForegroundColor Yellow
    $connString = "Server=192.168.1.230\SQLEXPRESS;Database=master;User Id=sa;Password=password.123;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    Write-Host "   OK Connesso al server" -ForegroundColor Green
    
    # Crea il login FAB
    Write-Host ""
    Write-Host "2. Creazione login FAB..." -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'FAB')
BEGIN
    CREATE LOGIN [FAB] WITH PASSWORD = 'password.123', CHECK_POLICY = OFF
    SELECT 'Login FAB creato con successo' AS Risultato
END
ELSE
BEGIN
    SELECT 'Login FAB gia esistente' AS Risultato
END
"@
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "   $($reader['Risultato'])" -ForegroundColor Green
    }
    $reader.Close()
    
    # Cambia database a MESManager_Dev
    Write-Host ""
    Write-Host "3. Configurazione utente nel database MESManager_Dev..." -ForegroundColor Yellow
    $cmd.CommandText = "USE [MESManager_Dev]"
    $cmd.ExecuteNonQuery() | Out-Null
    
    # Crea utente nel database
    $cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'FAB')
BEGIN
    CREATE USER [FAB] FOR LOGIN [FAB]
    SELECT 'Utente FAB creato nel database' AS Risultato
END
ELSE
BEGIN
    SELECT 'Utente FAB gia esistente nel database' AS Risultato
END
"@
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "   $($reader['Risultato'])" -ForegroundColor Green
    }
    $reader.Close()
    
    # Assegna ruolo db_owner
    Write-Host ""
    Write-Host "4. Assegnazione permessi db_owner..." -ForegroundColor Yellow
    $cmd.CommandText = "ALTER ROLE [db_owner] ADD MEMBER [FAB]"
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "   OK Permessi assegnati" -ForegroundColor Green
    
    # Verifica permessi
    Write-Host ""
    Write-Host "5. Verifica configurazione..." -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT 
    dp.name AS UserName,
    r.name AS RoleName
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.name = 'FAB'
"@
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "   Utente: $($reader['UserName']) - Ruolo: $($reader['RoleName'])" -ForegroundColor Cyan
    }
    $reader.Close()
    
    $conn.Close()
    
    Write-Host ""
    Write-Host "=== UTENTE FAB CREATO CON SUCCESSO ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "CONFIGURAZIONE:" -ForegroundColor Yellow
    Write-Host "  Server: 192.168.1.230\SQLEXPRESS" -ForegroundColor White
    Write-Host "  Database: MESManager_Dev" -ForegroundColor White
    Write-Host "  User Id: FAB" -ForegroundColor White
    Write-Host "  Password: password.123" -ForegroundColor White
    Write-Host "  Permessi: db_owner (solo su MESManager_Dev)" -ForegroundColor White
    Write-Host ""
    
    # Test connessione con nuovo utente
    Write-Host "6. Test connessione con utente FAB..." -ForegroundColor Yellow
    $testConnString = "Server=192.168.1.230\SQLEXPRESS;Database=MESManager_Dev;User Id=FAB;Password=password.123;TrustServerCertificate=True;"
    $testConn = New-Object System.Data.SqlClient.SqlConnection($testConnString)
    $testConn.Open()
    
    $testCmd = $testConn.CreateCommand()
    $testCmd.CommandText = "SELECT DB_NAME() AS CurrentDB, COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $testReader = $testCmd.ExecuteReader()
    
    if ($testReader.Read()) {
        Write-Host "   OK Connesso come FAB al database: $($testReader['CurrentDB'])" -ForegroundColor Green
        Write-Host "   Numero tabelle accessibili: $($testReader['TableCount'])" -ForegroundColor Cyan
    }
    $testReader.Close()
    $testConn.Close()
    
    Write-Host ""
    Write-Host "OK Tutto configurato correttamente!" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "ERRORE: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.Exception.StackTrace -ForegroundColor Gray
}
