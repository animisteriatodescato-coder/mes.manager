<#
.SYNOPSIS
    Esporta le preferenze delle griglie dal localStorage del browser e genera SQL per utenti predefiniti.

.DESCRIPTION
    Questo script:
    1. Legge le preferenze salvate nel localStorage del browser (Chrome DevTools)
    2. Genera SQL INSERT per creare preferenze default per IRENE, FABIO e GIULIA
    3. Salva il risultato in un file SQL pronto per l'esecuzione

.NOTES
    ISTRUZIONI:
    1. Apri il browser e vai su http://localhost:5156 (o l'URL dell'app)
    2. Premi F12 per aprire DevTools
    3. Vai alla tab "Console"
    4. Esegui questo comando JavaScript per esportare le preferenze:
    
    JSON.stringify(Object.keys(localStorage).filter(k => k.includes('grid')).reduce((obj, key) => { obj[key] = localStorage.getItem(key); return obj; }, {}), null, 2)
    
    5. Copia l'output JSON
    6. Incollalo quando richiesto da questo script
    7. Lo script genererà il file SQL con gli INSERT
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Esportazione Preferenze Griglie dal localStorage ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "ISTRUZIONI:" -ForegroundColor Yellow
Write-Host "1. Apri Chrome DevTools (F12) su http://localhost:5156" -ForegroundColor White
Write-Host "2. Vai alla tab 'Console'" -ForegroundColor White
Write-Host "3. Esegui questo comando JavaScript:" -ForegroundColor White
Write-Host ""
Write-Host "JSON.stringify(Object.keys(localStorage).filter(k => k.includes('grid')).reduce((obj, key) => { obj[key] = localStorage.getItem(key); return obj; }, {}), null, 2)" -ForegroundColor Green
Write-Host ""
Write-Host "4. Copia l'output JSON e incollalo qui sotto" -ForegroundColor White
Write-Host "5. Premi CTRL+Z seguito da ENTER quando hai finito di incollare" -ForegroundColor White
Write-Host ""
Write-Host "Incolla il JSON delle preferenze:" -ForegroundColor Cyan

# Leggi input multi-linea
$lines = @()
while ($true) {
    $line = Read-Host
    if ([string]::IsNullOrEmpty($line)) {
        break
    }
    $lines += $line
}

$jsonInput = $lines -join "`n"

if ([string]::IsNullOrWhiteSpace($jsonInput)) {
    Write-Host "Errore: Nessun input fornito" -ForegroundColor Red
    exit 1
}

try {
    $preferences = ConvertFrom-Json $jsonInput
} catch {
    Write-Host "Errore nel parsing JSON: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "JSON valido! Trovate $($preferences.PSObject.Properties.Count) preferenze" -ForegroundColor Green
Write-Host ""

# Definisci gli utenti predefiniti
$utenti = @(
    @{ Nome = "IRENE"; Colore = "#E91E63" }, # Pink
    @{ Nome = "FABIO"; Colore = "#2196F3" }, # Blue
    @{ Nome = "GIULIA"; Colore = "#4CAF50" }  # Green
)

# Genera SQL
$sqlOutput = @"
-- =====================================================================
-- Script di inizializzazione preferenze utenti
-- Generato il: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")
-- =====================================================================

-- Aggiorna colori utenti se già esistono
UPDATE UtentiApp SET Colore = '#E91E63' WHERE Nome = 'IRENE';
UPDATE UtentiApp SET Colore = '#2196F3' WHERE Nome = 'FABIO';
UPDATE UtentiApp SET Colore = '#4CAF50' WHERE Nome = 'GIULIA';

-- Inserisci utenti se non esistono
IF NOT EXISTS (SELECT 1 FROM UtentiApp WHERE Nome = 'IRENE')
    INSERT INTO UtentiApp (Id, Nome, Colore, Attivo, Ordine, DataCreazione, UltimaModifica)
    VALUES (NEWID(), 'IRENE', '#E91E63', 1, 1, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM UtentiApp WHERE Nome = 'FABIO')
    INSERT INTO UtentiApp (Id, Nome, Colore, Attivo, Ordine, DataCreazione, UltimaModifica)
    VALUES (NEWID(), 'FABIO', '#2196F3', 1, 2, GETDATE(), GETDATE());

IF NOT EXISTS (SELECT 1 FROM UtentiApp WHERE Nome = 'GIULIA')
    INSERT INTO UtentiApp (Id, Nome, Colore, Attivo, Ordine, DataCreazione, UltimaModifica)
    VALUES (NEWID(), 'GIULIA', '#4CAF50', 1, 3, GETDATE(), GETDATE());

-- =====================================================================
-- Inserimento preferenze griglie per ogni utente
-- =====================================================================

"@

foreach ($utente in $utenti) {
    $sqlOutput += "`n-- Preferenze per utente: $($utente.Nome)`n"
    $sqlOutput += "DECLARE @UtenteId$($utente.Nome) UNIQUEIDENTIFIER = (SELECT Id FROM UtentiApp WHERE Nome = '$($utente.Nome)');"
    $sqlOutput += "`n`n"
    
    foreach ($prop in $preferences.PSObject.Properties) {
        $chiave = $prop.Name
        $valore = $prop.Value
        
        # Escapa apici singoli per SQL
        $valoreEscaped = $valore -replace "'", "''"
        
        $sqlOutput += @"
-- Preferenza: $chiave
IF NOT EXISTS (SELECT 1 FROM PreferenzeUtente WHERE UtenteAppId = @UtenteId$($utente.Nome) AND Chiave = '$chiave')
BEGIN
    INSERT INTO PreferenzeUtente (Id, UtenteAppId, Chiave, ValoreJson, DataCreazione, UltimaModifica)
    VALUES (
        NEWID(),
        @UtenteId$($utente.Nome),
        '$chiave',
        '$valoreEscaped',
        GETDATE(),
        GETDATE()
    );
END
ELSE
BEGIN
    UPDATE PreferenzeUtente
    SET ValoreJson = '$valoreEscaped',
        UltimaModifica = GETDATE()
    WHERE UtenteAppId = @UtenteId$($utente.Nome) AND Chiave = '$chiave';
END

"@
    }
    
    $sqlOutput += "`n"
}

$sqlOutput += @"

-- =====================================================================
-- Verifica
-- =====================================================================
SELECT 
    u.Nome AS Utente,
    u.Colore,
    COUNT(p.Id) AS NumeroPreferenze
FROM UtentiApp u
LEFT JOIN PreferenzeUtente p ON u.Id = p.UtenteAppId
WHERE u.Nome IN ('IRENE', 'FABIO', 'GIULIA')
GROUP BY u.Nome, u.Colore
ORDER BY u.Ordine;

PRINT 'Preferenze inizializzate con successo!';
"@

# Salva il file SQL
$outputFile = Join-Path $PSScriptRoot "init-preferenze-utenti.sql"
$sqlOutput | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "✓ File SQL generato con successo!" -ForegroundColor Green
Write-Host "  Percorso: $outputFile" -ForegroundColor White
Write-Host ""
Write-Host "Per applicare le preferenze al database:" -ForegroundColor Yellow
Write-Host "  sqlcmd -S localhost -d MesManager -i `"$outputFile`"" -ForegroundColor White
Write-Host ""
Write-Host "Oppure esegui il file direttamente in SQL Server Management Studio" -ForegroundColor White
Write-Host ""

# Mostra statistiche
Write-Host "Riepilogo:" -ForegroundColor Cyan
Write-Host "  - Utenti configurati: $($utenti.Count)" -ForegroundColor White
Write-Host "  - Preferenze per utente: $($preferences.PSObject.Properties.Count)" -ForegroundColor White
Write-Host "  - Totale INSERT: $($utenti.Count * $preferences.PSObject.Properties.Count)" -ForegroundColor White
Write-Host ""
