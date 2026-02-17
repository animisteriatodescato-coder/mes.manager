# Backup Database Pre-Fix Cataloghi
# Eseguito prima di applicare fix duplicati e seed operatori

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "C:\Dev\MESManager\backups"
$backupFile = "$backupDir\MESManager_Dev_PreFixCataloghi_$timestamp.bak"

# Crea directory backup se non esiste
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Write-Host "[OK] Directory backup creata: $backupDir" -ForegroundColor Green
}

Write-Host "`n=== BACKUP DATABASE ===" -ForegroundColor Cyan
Write-Host "Database: MESManager_Dev" -ForegroundColor Yellow
Write-Host "Destinazione: $backupFile" -ForegroundColor Yellow
Write-Host ""

$query = @"
BACKUP DATABASE [MESManager_Dev] 
TO DISK = N'$backupFile'
WITH NOFORMAT, INIT, 
NAME = N'MESManager_Dev-Full Database Backup PreFixCataloghi', 
SKIP, NOREWIND, NOUNLOAD, STATS = 10
"@

try {
    $connStr = "Server=localhost\SQLEXPRESS01;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    $cmd.CommandTimeout = 300  # 5 minuti timeout
    
    Write-Host "Backup in corso..." -ForegroundColor Yellow
    $cmd.ExecuteNonQuery() | Out-Null
    
    $conn.Close()
    
    if (Test-Path $backupFile) {
        $fileSize = (Get-Item $backupFile).Length / 1MB
        Write-Host "`n[OK] Backup completato!" -ForegroundColor Green
        Write-Host "File: $backupFile" -ForegroundColor Green
        Write-Host "Dimensione: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green
        
        # Salva info backup in file di testo
        $infoFile = "$backupDir\backup_info_$timestamp.txt"
        @"
BACKUP DATABASE MESManager_Dev
Data: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
File: $backupFile
Dimensione: $([math]::Round($fileSize, 2)) MB
Motivo: Pre-fix cataloghi (duplicati Macchine, operatori mancanti)
"@ | Out-File -FilePath $infoFile -Encoding UTF8
        
        Write-Host "`nPer ripristinare questo backup, eseguire:"
        Write-Host "RESTORE DATABASE [MESManager_Dev] FROM DISK = N'$backupFile' WITH REPLACE" -ForegroundColor Cyan
    }
    else {
        Write-Host "`n[ERRORE] File backup non trovato!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "`n[ERRORE] Backup fallito: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
