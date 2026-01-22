# Script per migrare database MESManager da locale a server produzione
# Eseguire su macchina locale con accesso a entrambi i server

param(
    [string]$SourceServer = "localhost\SQLEXPRESS01",
    [string]$SourceDatabase = "MESManager",
    [string]$TargetServer = "192.168.1.230",
    [string]$TargetDatabase = "MESManager_Prod",
    [string]$TargetUser = "FAB",
    [string]$TargetPassword = "password.123",
    [string]$BackupPath = "C:\Temp"
)

Write-Host "=== Migrazione Database MESManager ===" -ForegroundColor Cyan
Write-Host ""

# Crea cartella backup se non esiste
if (!(Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Host "Creata cartella backup: $BackupPath" -ForegroundColor Green
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$BackupPath\MESManager_Backup_$timestamp.bak"

Write-Host "1. Creazione backup del database locale..." -ForegroundColor Yellow
Write-Host "   Server: $SourceServer"
Write-Host "   Database: $SourceDatabase"
Write-Host "   File: $backupFile"
Write-Host ""

# Backup locale (Windows Auth)
$backupQuery = @"
BACKUP DATABASE [$SourceDatabase] 
TO DISK = N'$backupFile' 
WITH NOFORMAT, NOINIT, 
NAME = N'$SourceDatabase-Full Backup', 
SKIP, NOREWIND, NOUNLOAD, STATS = 10
"@

try {
    Invoke-Sqlcmd -ServerInstance $SourceServer -Query $backupQuery -QueryTimeout 600
    Write-Host "Backup completato con successo!" -ForegroundColor Green
}
catch {
    Write-Host "ERRORE durante il backup: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Copia del file backup sul server di destinazione..." -ForegroundColor Yellow
Write-Host "   NOTA: Copiare manualmente il file su $TargetServer" -ForegroundColor Magenta
Write-Host "   File: $backupFile" -ForegroundColor Magenta
Write-Host ""

# Genera script per restore sul server
$restoreScript = @"

-- Script per restore su server produzione
-- Eseguire su $TargetServer con utente FAB o sa

-- 1. Se il database esiste, mettilo offline e cancellalo
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$TargetDatabase')
BEGIN
    ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$TargetDatabase];
END
GO

-- 2. Restore dal backup
-- NOTA: Modificare i percorsi di MOVE in base alla configurazione del server
RESTORE DATABASE [$TargetDatabase] 
FROM DISK = N'C:\Temp\MESManager_Backup_$timestamp.bak'
WITH 
    MOVE N'MESManager' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\$TargetDatabase.mdf',
    MOVE N'MESManager_log' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\${TargetDatabase}_log.ldf',
    REPLACE,
    STATS = 10
GO

-- 3. Verifica
SELECT name, state_desc FROM sys.databases WHERE name = '$TargetDatabase'
GO

PRINT 'Restore completato!'
"@

$restoreScriptPath = "$BackupPath\restore-on-server_$timestamp.sql"
$restoreScript | Out-File -FilePath $restoreScriptPath -Encoding UTF8
Write-Host "Script di restore generato: $restoreScriptPath" -ForegroundColor Green

Write-Host ""
Write-Host "=== ISTRUZIONI ===" -ForegroundColor Cyan
Write-Host "1. Copia il file backup sul server 192.168.1.230 in C:\Temp\"
Write-Host "2. Copia lo script SQL di restore sul server"
Write-Host "3. Esegui lo script SQL con SSMS o sqlcmd come sa o FAB"
Write-Host ""
Write-Host "Oppure esegui da remoto (se il server e' raggiungibile):"
Write-Host "sqlcmd -S $TargetServer -U $TargetUser -P $TargetPassword -i `"$restoreScriptPath`""
Write-Host ""
Write-Host "Backup file: $backupFile" -ForegroundColor Green
Write-Host "Restore script: $restoreScriptPath" -ForegroundColor Green
