# Esegue lo script SQL di fix cataloghi

$scriptPath = "C:\Dev\MESManager\scripts\fix-cataloghi-complete.sql"
$connStr = "Server=localhost\SQLEXPRESS01;Database=MESManager_Dev;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "`n=== ESECUZIONE FIX CATALOGHI ===" -ForegroundColor Cyan
Write-Host "Script: $scriptPath" -ForegroundColor Yellow
Write-Host ""

if (-not (Test-Path $scriptPath)) {
    Write-Host "[ERRORE] Script SQL non trovato: $scriptPath" -ForegroundColor Red
    exit 1
}

try {
    # Leggi lo script SQL
    $sqlScript = Get-Content -Path $scriptPath -Raw
    
    # Rimuovi i comandi GO (non supportati in ExecuteNonQuery diretto)
    $sqlScript = $sqlScript -replace '(?m)^\s*GO\s*$', ''
    
    # Connetti al database
    Add-Type -AssemblyName System.Data
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    Write-Host "[OK] Connesso al database: $($conn.Database)" -ForegroundColor Green
    Write-Host ""
    
    # Esegui lo script
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlScript
    $cmd.CommandTimeout = 120  # 2 minuti timeout
    
    # Cattura i messaggi PRINT di SQL Server
    $conn.FireInfoMessageEventOnUserErrors = $true
    $handler = [System.Data.SqlClient.SqlInfoMessageEventHandler] {
        param($sender, $event)
        foreach ($err in $event.Errors) {
            if ($err.Message -like '*OK*' -or $err.Message -like '*completato*') {
                Write-Host $err.Message -ForegroundColor Green
            }
            elseif ($err.Message -like '*ATTENZIONE*' -or $err.Message -like '*duplicat*') {
                Write-Host $err.Message -ForegroundColor Yellow
            }
            else {
                Write-Host $err.Message
            }
        }
    }
    $conn.add_InfoMessage($handler)
    
    Write-Host "Esecuzione script in corso..." -ForegroundColor Yellow
    Write-Host ""
    
    $result = $cmd.ExecuteNonQuery()
    
    $conn.Close()
    
    Write-Host ""
    Write-Host "=== FIX COMPLETATO ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verifica risultato eseguendo:" -ForegroundColor Cyan
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts\diagnose-db.ps1" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "[ERRORE] Esecuzione fallita: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
