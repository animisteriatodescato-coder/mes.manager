# Script per pulire e normalizzare il campo numeroMacchina nelle commesse

$baseUrl = "http://localhost:5156"

Write-Host "Recupero tutte le commesse..." -ForegroundColor Cyan
$commesse = Invoke-RestMethod -Uri "$baseUrl/api/Commesse" -Method GET

$aggiornate = 0
$cancellate = 0
$errori = 0

foreach ($commessa in $commesse) {
    $numeroMacchina = $commessa.numeroMacchina
    $nuovoValore = $null
    $doUpdate = $false
    
    if ([string]::IsNullOrWhiteSpace($numeroMacchina)) {
        continue  # Skip se vuoto
    }
    
    # Caso 1: È un numero semplice (1, 2, 3, 9, 12, etc.) -> Converti in M001, M002, M003, M009, M012
    if ($numeroMacchina -match '^\d+$') {
        $num = [int]$numeroMacchina
        if ($num -ge 1 -and $num -le 11) {
            $nuovoValore = "M" + $num.ToString().PadLeft(3, '0')
            $doUpdate = $true
            Write-Host "Commessa $($commessa.codice): $numeroMacchina -> $nuovoValore" -ForegroundColor Yellow
        } else {
            # Numero fuori range -> Cancella
            $nuovoValore = ""
            $doUpdate = $true
            $cancellate++
            Write-Host "Commessa $($commessa.codice): Valore invalido '$numeroMacchina' -> cancellato" -ForegroundColor Red
        }
    }
    # Caso 2: Contiene punti e virgola o caratteri strani -> Cancella
    elseif ($numeroMacchina -match '[;,\s]' -or $numeroMacchina -notmatch '^M\d{3}$') {
        $nuovoValore = ""
        $doUpdate = $true
        $cancellate++
        Write-Host "Commessa $($commessa.codice): Formato invalido '$numeroMacchina' -> cancellato" -ForegroundColor Red
    }
    # Caso 3: È già nel formato corretto M001-M011 -> OK
    elseif ($numeroMacchina -match '^M\d{3}$') {
        $num = [int]$numeroMacchina.Substring(1)
        if ($num -lt 1 -or $num -gt 11) {
            $nuovoValore = ""
            $doUpdate = $true
            $cancellate++
            Write-Host "Commessa $($commessa.codice): Codice fuori range '$numeroMacchina' -> cancellato" -ForegroundColor Red
        }
    }
    
    if ($doUpdate) {
        try {
            $body = @{ numeroMacchina = $nuovoValore } | ConvertTo-Json
            $response = Invoke-RestMethod -Uri "$baseUrl/api/Commesse/$($commessa.id)/numero-macchina" `
                -Method PATCH `
                -Headers @{ "Content-Type" = "application/json" } `
                -Body $body
            
            $aggiornate++
            
            if ($aggiornate % 50 -eq 0) {
                Write-Host "Processate $aggiornate commesse..." -ForegroundColor Green
            }
        }
        catch {
            Write-Host "ERRORE aggiornando commessa $($commessa.codice): $_" -ForegroundColor Red
            $errori++
        }
    }
}

Write-Host "`n=== RIEPILOGO ===" -ForegroundColor Cyan
Write-Host "Commesse aggiornate: $aggiornate" -ForegroundColor Green
Write-Host "Valori cancellati: $cancellate" -ForegroundColor Yellow
Write-Host "Errori: $errori" -ForegroundColor Red
