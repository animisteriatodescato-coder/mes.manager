# Script per reinstallare SQL Server Express
# ESEGUIRE COME AMMINISTRATORE

Write-Host "=== REINSTALLAZIONE SQL SERVER EXPRESS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Questo script guiderà la reinstallazione di SQL Server Express" -ForegroundColor Yellow
Write-Host ""

# 1. Verifica se SQL Server è in esecuzione
Write-Host "1. Verifica servizi SQL Server..." -ForegroundColor Yellow
$services = Get-Service -Name 'MSSQL$*' -ErrorAction SilentlyContinue
if ($services) {
    Write-Host "   Servizi SQL Server trovati:" -ForegroundColor Cyan
    $services | ForEach-Object {
        Write-Host "   - $($_.Name): $($_.Status)" -ForegroundColor White
    }
} else {
    Write-Host "   Nessun servizio SQL Server trovato" -ForegroundColor Green
}

# 2. Download SQL Server Express
Write-Host "`n2. Download SQL Server Express..." -ForegroundColor Yellow
$downloadUrl = "https://go.microsoft.com/fwlink/?linkid=866658"
$installerPath = "$env:TEMP\SQL2019-SSEI-Expr.exe"

Write-Host "   URL: $downloadUrl" -ForegroundColor Cyan
Write-Host "   Percorso download: $installerPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Avvio download..." -ForegroundColor Yellow

try {
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
    Write-Host "   ✓ Download completato!" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Errore nel download: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "   ALTERNATIVA: Scarica manualmente da:" -ForegroundColor Yellow
    Write-Host "   https://www.microsoft.com/it-it/sql-server/sql-server-downloads" -ForegroundColor Cyan
    Write-Host "   Scegli: SQL Server 2022 Express" -ForegroundColor Cyan
    exit 1
}

# 3. Istruzioni per l'installazione
Write-Host "`n3. ISTRUZIONI PER L'INSTALLAZIONE" -ForegroundColor Yellow
Write-Host "   ================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "   L'installer si aprirà automaticamente." -ForegroundColor White
Write-Host "   Segui questi passaggi:" -ForegroundColor White
Write-Host ""
Write-Host "   A. Nella prima schermata:" -ForegroundColor Cyan
Write-Host "      - Scegli: 'Download Media'" -ForegroundColor White
Write-Host "      - Seleziona: 'Express Core'" -ForegroundColor White
Write-Host "      - Lingua: Italian" -ForegroundColor White
Write-Host ""
Write-Host "   B. Dopo il download:" -ForegroundColor Cyan
Write-Host "      - Esegui il file scaricato (SQLEXPR_x64_ENU.exe)" -ForegroundColor White
Write-Host "      - Scegli: 'New SQL Server stand-alone installation'" -ForegroundColor White
Write-Host ""
Write-Host "   C. Configurazione:" -ForegroundColor Cyan
Write-Host "      - Instance Name: SQLEXPRESS" -ForegroundColor White
Write-Host "      - Named instance: SQLEXPRESS" -ForegroundColor White
Write-Host "      - Authentication: Windows Authentication Mode" -ForegroundColor White
Write-Host "      - Add Current User come amministratore" -ForegroundColor White
Write-Host ""
Write-Host "   D. Servizi:" -ForegroundColor Cyan
Write-Host "      - SQL Server Database Engine: Automatic" -ForegroundColor White
Write-Host "      - SQL Server Browser: Automatic (opzionale)" -ForegroundColor White
Write-Host ""

# 4. Avvia l'installer
Write-Host "`n4. Avvio installer..." -ForegroundColor Yellow
$response = Read-Host "   Vuoi avviare l'installer ora? (S/N)"

if ($response -eq 'S' -or $response -eq 's') {
    try {
        Start-Process -FilePath $installerPath -Wait
        Write-Host "   ✓ Installer completato" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ Errore: $_" -ForegroundColor Red
    }
} else {
    Write-Host "   Installer salvato in: $installerPath" -ForegroundColor Cyan
    Write-Host "   Eseguilo manualmente quando sei pronto" -ForegroundColor Yellow
}

# 5. Post-installazione
Write-Host "`n5. DOPO L'INSTALLAZIONE" -ForegroundColor Yellow
Write-Host "   ====================" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Una volta completata l'installazione:" -ForegroundColor White
Write-Host ""
Write-Host "   1. Verifica che il servizio sia in esecuzione:" -ForegroundColor Cyan
Write-Host "      Get-Service 'MSSQL`$SQLEXPRESS'" -ForegroundColor White
Write-Host ""
Write-Host "   2. Testa la connessione:" -ForegroundColor Cyan
Write-Host "      cd C:\Dev\MESManager" -ForegroundColor White
Write-Host "      .\test-sql-connection.ps1" -ForegroundColor White
Write-Host ""
Write-Host "   3. Crea il database MESManager:" -ForegroundColor Cyan
Write-Host "      cd C:\Dev\MESManager\MESManager.Infrastructure" -ForegroundColor White
Write-Host "      dotnet ef database update --startup-project ..\MESManager.Web" -ForegroundColor White
Write-Host ""
Write-Host "   4. Avvia l'applicazione:" -ForegroundColor Cyan
Write-Host "      cd C:\Dev\MESManager" -ForegroundColor White
Write-Host "      .\start-web-5156.cmd" -ForegroundColor White
Write-Host ""

Write-Host "`n=== FINE SCRIPT ===" -ForegroundColor Cyan
