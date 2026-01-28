<#
.SYNOPSIS
    Cripta/decripta il file appsettings.Secrets.json
.DESCRIPTION
    Usa DPAPI (Data Protection API) di Windows per proteggere le credenziali.
    Il file criptato può essere decriptato solo sulla stessa macchina e dallo stesso utente.
.EXAMPLE
    .\protect-secrets.ps1 -Encrypt
    .\protect-secrets.ps1 -Decrypt
#>

param(
    [switch]$Encrypt,
    [switch]$Decrypt
)

$SecretsPath = Join-Path $PSScriptRoot "appsettings.Secrets.json"
$EncryptedPath = Join-Path $PSScriptRoot "appsettings.Secrets.encrypted"

Add-Type -AssemblyName System.Security

function Protect-File {
    param([string]$InputPath, [string]$OutputPath)
    
    if (-not (Test-Path $InputPath)) {
        Write-Error "File non trovato: $InputPath"
        return $false
    }
    
    $content = [System.IO.File]::ReadAllBytes($InputPath)
    $encrypted = [System.Security.Cryptography.ProtectedData]::Protect(
        $content, 
        $null, 
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser
    )
    
    [System.IO.File]::WriteAllBytes($OutputPath, $encrypted)
    
    # Elimina il file originale in chiaro
    Remove-Item $InputPath -Force
    
    Write-Host "✅ File criptato con successo!" -ForegroundColor Green
    Write-Host "   File originale eliminato: $InputPath" -ForegroundColor Yellow
    Write-Host "   File criptato creato: $OutputPath" -ForegroundColor Cyan
    return $true
}

function Unprotect-File {
    param([string]$InputPath, [string]$OutputPath)
    
    if (-not (Test-Path $InputPath)) {
        Write-Error "File criptato non trovato: $InputPath"
        return $false
    }
    
    try {
        $encrypted = [System.IO.File]::ReadAllBytes($InputPath)
        $decrypted = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $encrypted, 
            $null, 
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser
        )
        
        [System.IO.File]::WriteAllBytes($OutputPath, $decrypted)
        
        Write-Host "✅ File decriptato con successo!" -ForegroundColor Green
        Write-Host "   File decriptato: $OutputPath" -ForegroundColor Cyan
        Write-Host "⚠️  ATTENZIONE: Ricordati di ri-criptare dopo l'uso!" -ForegroundColor Yellow
        return $true
    }
    catch {
        Write-Error "Errore nella decriptazione. Il file può essere decriptato solo dallo stesso utente Windows che lo ha criptato."
        return $false
    }
}

# Main
if ($Encrypt) {
    Protect-File -InputPath $SecretsPath -OutputPath $EncryptedPath
}
elseif ($Decrypt) {
    Unprotect-File -InputPath $EncryptedPath -OutputPath $SecretsPath
}
else {
    Write-Host "Uso: .\protect-secrets.ps1 -Encrypt | -Decrypt" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  -Encrypt  Cripta appsettings.Secrets.json e lo elimina" -ForegroundColor Gray
    Write-Host "  -Decrypt  Decripta il file (solo per modifiche)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Il file criptato può essere decriptato SOLO:" -ForegroundColor Yellow
    Write-Host "  - Su questa macchina" -ForegroundColor Yellow
    Write-Host "  - Dal tuo utente Windows ($env:USERNAME)" -ForegroundColor Yellow
}
