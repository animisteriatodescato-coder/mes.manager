# Script PowerShell per generare i file JSON corretti per PlcSync
# Esegui da: c:\Dev\MESManager\MESManager.PlcSync\Configuration\machines\

# Backup dei file esistenti
$backupDir = ".\backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
Copy-Item -Path ".\macchina_*.json" -Destination $backupDir

# Template offsets comuni (uguali per tutte le macchine)
$offsets = @{
    InizioSetup = 8
    FineSetup = 10
    NuovaProduzione = 12
    FineProduzione = 14
    QuantitaRaggiunta = 16
    CicliFatti = 18
    CicliScarti = 20
    NumeroOperatore = 22
    TempoMedioRil = 24
    StatoEmergenza = 34
    StatoManuale = 36
    StatoAutomatico = 38
    StatoCiclo = 40
    StatoPezziRagg = 42
    StatoAllarme = 44
    BarcodeLavorazione = 46
    QuantitaDaProd = 162
    TempoMedio = 164
    Figure = 170
}

# Mappatura corretta: Codice Macchina DB -> GUID -> IP
# NOTA: Questi dati vengono dal database MESManager_Prod
$macchine = @(
    # Codice, GUID,                                          Nome, IP,              Enabled
    @{Codice="M002"; Id="11111111-1111-1111-1111-000000000003"; Nome="02"; Ip="192.168.17.26"; Enabled=$true},
    @{Codice="M003"; Id="11111111-1111-1111-1111-000000000005"; Nome="03"; Ip="192.168.17.24"; Enabled=$true},
    @{Codice="M005"; Id="11111111-1111-1111-1111-000000000007"; Nome="05"; Ip="192.168.17.27"; Enabled=$true},
    @{Codice="M006"; Id="11111111-1111-1111-1111-000000000008"; Nome="06"; Ip="192.168.17.25"; Enabled=$true},
    @{Codice="M007"; Id="11111111-1111-1111-1111-000000000009"; Nome="07"; Ip="192.168.17.23"; Enabled=$true},
    @{Codice="M008"; Id="11111111-1111-1111-1111-000000000010"; Nome="08"; Ip="192.168.17.21"; Enabled=$true},
    @{Codice="M009"; Id="53A810FA-75D4-4D82-C583-08DE58C59F6F"; Nome="09"; Ip="192.168.17.29"; Enabled=$true},
    @{Codice="M010"; Id="57A8288D-3766-4C3B-C584-08DE58C59F6F"; Nome="10"; Ip="192.168.17.22"; Enabled=$true}
    # M001 e M004 e M011 non hanno IP configurato, quindi non vengono sincronizzati
)

foreach ($m in $macchine) {
    $config = [ordered]@{
        MachineId = $m.Id
        Numero = [int]($m.Nome)  # Numero dalla parte numerica del nome
        Nome = $m.Nome
        PlcIp = $m.Ip
        Enabled = $m.Enabled
        Offsets = $offsets
    }
    
    $fileName = "macchina_$($m.Codice).json"
    $config | ConvertTo-Json -Depth 3 | Set-Content -Path ".\$fileName" -Encoding UTF8
    
    Write-Host "Creato: $fileName (GUID: $($m.Id), IP: $($m.Ip))"
}

# Rimuovi i vecchi file con naming errato
Get-ChildItem -Path ".\macchina_0*.json" | ForEach-Object {
    Write-Host "Rimosso vecchio file: $($_.Name)" -ForegroundColor Yellow
    Remove-Item $_.FullName
}

Write-Host "`n✅ Generazione completata! File di backup in: $backupDir" -ForegroundColor Green
