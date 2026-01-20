# Script per riparare SQL Server Express
# ESEGUIRE COME AMMINISTRATORE

Write-Host "=== RIPARAZIONE SQL SERVER EXPRESS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Crea directory mancanti
Write-Host "1. Creazione directory DATA e LOG..." -ForegroundColor Yellow
$sqlPath = "C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL"
$dataPath = "$sqlPath\DATA"
$logPath = "$sqlPath\LOG"

try {
    if (-not (Test-Path $dataPath)) {
        New-Item -Path $dataPath -ItemType Directory -Force | Out-Null
        Write-Host "   OK Directory DATA creata" -ForegroundColor Green
    } else {
        Write-Host "   OK Directory DATA gia esistente" -ForegroundColor Green
    }
    
    if (-not (Test-Path $logPath)) {
        New-Item -Path $logPath -ItemType Directory -Force | Out-Null
        Write-Host "   OK Directory LOG creata" -ForegroundColor Green
    } else {
        Write-Host "   OK Directory LOG gia esistente" -ForegroundColor Green
    }
} catch {
    Write-Host "   ERRORE: $_" -ForegroundColor Red
    Write-Host "   Assicurati di eseguire questo script come AMMINISTRATORE" -ForegroundColor Yellow
    exit 1
}

# 2. Verifica account del servizio
Write-Host ""
Write-Host "2. Verifica account servizio SQL Server..." -ForegroundColor Yellow
$service = Get-WmiObject win32_service | Where-Object {$_.Name -eq 'MSSQL$SQLEXPRESS'}
if ($service) {
    Write-Host "   Account: $($service.StartName)" -ForegroundColor Cyan
    Write-Host "   Stato: $($service.State)" -ForegroundColor Cyan
    Write-Host "   StartMode: $($service.StartMode)" -ForegroundColor Cyan
} else {
    Write-Host "   ERRORE Servizio non trovato!" -ForegroundColor Red
}

# 3. Imposta permessi sulle directory
Write-Host ""
Write-Host "3. Impostazione permessi directory..." -ForegroundColor Yellow
try {
    $sqlServiceAccount = "NT Service\MSSQL`$SQLEXPRESS"
    
    $acl = Get-Acl $dataPath
    $permission = $sqlServiceAccount, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $dataPath $acl
    
    $acl = Get-Acl $logPath
    $acl.SetAccessRule($accessRule)
    Set-Acl $logPath $acl
    
    Write-Host "   OK Permessi impostati correttamente" -ForegroundColor Green
} catch {
    Write-Host "   Avviso permessi: $_" -ForegroundColor Yellow
}

# 4. Ferma il servizio (se in esecuzione)
Write-Host ""
Write-Host "4. Arresto servizio SQL Server..." -ForegroundColor Yellow
try {
    $svc = Get-Service 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq 'Running') {
        Stop-Service 'MSSQL$SQLEXPRESS' -Force
        Write-Host "   OK Servizio fermato" -ForegroundColor Green
    } else {
        Write-Host "   OK Servizio gia fermo" -ForegroundColor Green
    }
} catch {
    Write-Host "   Avviso: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 5. Avvia il servizio
Write-Host ""
Write-Host "5. Avvio servizio SQL Server..." -ForegroundColor Yellow
try {
    Start-Service 'MSSQL$SQLEXPRESS'
    Write-Host "   OK Servizio avviato con successo!" -ForegroundColor Green
    
    Start-Sleep -Seconds 3
    
    $svc = Get-Service 'MSSQL$SQLEXPRESS'
    Write-Host "   Stato finale: $($svc.Status)" -ForegroundColor Cyan
} catch {
    Write-Host "   ERRORE nell'avvio: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Il servizio potrebbe non avviarsi se i database di sistema sono mancanti." -ForegroundColor Yellow
    Write-Host "   In questo caso, sara necessaria una reinstallazione completa." -ForegroundColor Yellow
}

# 6. Test connessione
Write-Host ""
Write-Host "6. Test connessione al database..." -ForegroundColor Yellow
try {
    Add-Type -AssemblyName "System.Data"
    $connString = "Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=10;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT @@VERSION as Version"
    $reader = $cmd.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "   OK Connessione riuscita!" -ForegroundColor Green
        Write-Host "   Versione: $($reader['Version'])" -ForegroundColor Cyan
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host "   ERRORE connessione: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Potrebbe essere necessaria una reinstallazione." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== FINE RIPARAZIONE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "PROSSIMI PASSI:" -ForegroundColor Yellow
Write-Host "1. Se il servizio si avvia ma la connessione fallisce" -ForegroundColor White
Write-Host "   i database di sistema potrebbero essere corrotti" -ForegroundColor White
Write-Host "2. In tal caso, esegui: C:\Dev\MESManager\reinstall-sqlserver.ps1" -ForegroundColor White
Write-Host ""
