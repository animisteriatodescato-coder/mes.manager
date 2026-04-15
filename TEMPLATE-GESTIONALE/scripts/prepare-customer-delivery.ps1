<#
.SYNOPSIS
    Prepara il pacchetto di consegna al cliente rimuovendo tutta la proprietà intellettuale privata.
    Eseguire PRIMA di ogni consegna al cliente.

.DESCRIPTION
    Questo script:
    1. Crea una copia pulita del progetto nella cartella di output
    2. Rimuove tutti i file privati (BIBBIA developer, storico, business, credenziali)
    3. Sostituisce copilot-instructions.md con la versione sicura per il cliente
    4. Verifica che nessun file privato sia rimasto nel pacchetto

.PARAMETER ProjectPath
    Path del progetto sorgente (dove lavora il developer). Default: directory corrente.

.PARAMETER OutputPath
    Path dove creare il pacchetto cliente. Default: .\customer-delivery\[NomeProgetto]-[data]

.PARAMETER CustomerName
    Nome del cliente (usato per personalizzare la BIBBIA cliente).

.PARAMETER SupplierName
    Nome del fornitore (te) da inserire nella BIBBIA cliente come riferimento supporto.

.PARAMETER SupplierContact
    Contatto del fornitore da inserire nella BIBBIA cliente.

.EXAMPLE
    .\scripts\prepare-customer-delivery.ps1 -CustomerName "Azienda Srl" -SupplierName "TuaNome Dev" -SupplierContact "info@tuonome.it"

.NOTES
    ⚠️  VERIFICA SEMPRE l'output prima di consegnare al cliente.
    ⚠️  Controlla la sezione "FILE PRIVATI RIMOSSI" nel report finale.
#>
param(
    [string]$ProjectPath     = (Get-Location).Path,
    [string]$OutputPath      = "",
    [string]$CustomerName    = "[NOME_CLIENTE]",
    [string]$SupplierName    = "[NOME_FORNITORE]",
    [string]$SupplierContact = "[CONTATTO_FORNITORE]"
)

# ─── Configurazione ─────────────────────────────────────────────────────────
$ProjectName = Split-Path $ProjectPath -Leaf
$DateStamp   = Get-Date -Format "yyyyMMdd"

if ($OutputPath -eq "") {
    $OutputPath = Join-Path (Split-Path $ProjectPath -Parent) "customer-delivery\$ProjectName-$DateStamp"
}

# File e cartelle da ESCLUDERE SEMPRE dalla consegna cliente
$PrivateFiles = @(
    # BIBBIA developer (contiene tutta la metodologia e lessons learned)
    "docs\BIBBIA-AI-*.md",

    # Storico tecnico (decisions, fix, lessons learned del developer)
    "docs\storico\*",

    # Business e commerciale
    "docs\10-BUSINESS.md",

    # Credenziali e deploy del developer
    "docs\01-DEPLOY.md",

    # Script deploy con credenziali
    "scripts\deploy-*.ps1",
    "deploy-*.ps1",
    "publish_v*.txt",

    # File template/template developer
    "COME-USARE-QUESTO-TEMPLATE.md",
    "STRUTTURA-PROGETTO.md",
    "BIBBIA-AI-TEMPLATE.md",
    "BIBBIA-AI-CLIENTE-TEMPLATE.md",

    # Secrets (non devono MAI uscire)
    "appsettings.Secrets.json",
    "appsettings.Database.json",
    "*.Secrets.json",
    "*.encrypted",

    # File di lavoro interni
    "backups\*",
    "*.bak",
    "rag\*",

    # Note private
    "DEPLOY-CHECKLIST-*.md",
    "RIEPILOGO-*.txt",
    "fix-result.txt",
    "final-fix.txt",
    "fk-check.txt",
    "build_*.txt",
    "server_*.txt"
)

# Cartelle da escludere completamente
$PrivateFolders = @(
    "docs\storico",
    "backups",
    "rag",
    ".git"
)

$RemovedFiles = @()
$WarningFiles = @()

# ─── Helpers ────────────────────────────────────────────────────────────────
function Write-Step  { param($msg) Write-Host "`n[$msg]" -ForegroundColor Cyan }
function Write-OK    { param($msg) Write-Host "  [OK]   $msg" -ForegroundColor Green }
function Write-WARN  { param($msg) Write-Host "  [WARN] $msg" -ForegroundColor Yellow; $script:WarningFiles += $msg }
function Write-REMOVED { param($msg) Write-Host "  [DEL]  $msg" -ForegroundColor DarkYellow; $script:RemovedFiles += $msg }

# ─── START ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "  PREPARAZIONE PACCHETTO CLIENTE — $ProjectName" -ForegroundColor Magenta
Write-Host "  Cliente: $CustomerName" -ForegroundColor Magenta
Write-Host "  Output:  $OutputPath" -ForegroundColor Magenta
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Magenta

# ─── STEP 1: Copia progetto ──────────────────────────────────────────────────
Write-Step "STEP 1 — Copia progetto in $OutputPath"

if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
    Write-OK "Cartella output precedente rimossa"
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Copia tutto escludendo bin, obj, .git già dall'inizio
$robocopyArgs = @(
    $ProjectPath, $OutputPath,
    "/MIR", "/Z",
    "/XD", "bin", "obj", ".git", "backups", "rag", "publish", "customer-delivery",
    "/XF", "*.bak", "*.log", "*.user", "*.suo",
    "/NJH", "/NJS"
)
& robocopy @robocopyArgs | Out-Null
Write-OK "Progetto copiato in $OutputPath"

# ─── STEP 2: Rimozione file privati ──────────────────────────────────────────
Write-Step "STEP 2 — Rimozione file privati (IP developer)"

foreach ($pattern in $PrivateFiles) {
    $fullPattern = Join-Path $OutputPath $pattern
    $matches = Get-ChildItem -Path $fullPattern -ErrorAction SilentlyContinue -Recurse
    foreach ($f in $matches) {
        Remove-Item -Force -Recurse $f.FullName -ErrorAction SilentlyContinue
        Write-REMOVED $f.FullName.Replace($OutputPath, "").TrimStart("\")
    }
}

foreach ($folder in $PrivateFolders) {
    $fullFolder = Join-Path $OutputPath $folder
    if (Test-Path $fullFolder) {
        Remove-Item -Recurse -Force $fullFolder
        Write-REMOVED "$folder\" 
    }
}
Write-OK "File privati rimossi: $($RemovedFiles.Count) elementi"

# ─── STEP 3: Copia storico vuoto con solo le regole generali ────────────────
Write-Step "STEP 3 — Creazione storico cliente (solo regole generali)"

$storicoPath = Join-Path $OutputPath "docs\storico"
New-Item -ItemType Directory -Path $storicoPath -Force | Out-Null

$storicoContent = @"
# storico/ — Note Tecniche

> Questa cartella raccoglie le note tecniche importanti scoperte durante lo sviluppo.
> Quando trovi un bug ricorrente o una soluzione non ovvia, aggiungila qui.

## Formato voce

``````
## [DD/MM/YYYY] — v[X.Y.Z] — [Titolo breve]

**Problema**: [cosa è successo]
**Causa**: [causa tecnica]
**Soluzione**: [come è stato risolto]
**Prevenzione futura**: [cosa fare per evitarlo]
``````

---

*Cartella vuota al momento della consegna — da popolare durante il mantenimento*
"@
Set-Content -Path (Join-Path $storicoPath "README.md") -Value $storicoContent
Write-OK "Cartella storico ricreata (vuota)"

# ─── STEP 4: Sostituisci copilot-instructions.md con versione cliente ────────
Write-Step "STEP 4 — Configurazione copilot-instructions.md per il cliente"

$githubFolder = Join-Path $OutputPath ".github"
New-Item -ItemType Directory -Path $githubFolder -Force | Out-Null

# Leggi la BIBBIA cliente template e sostituisci placeholder
$bibbiaClientePath = Join-Path $OutputPath "docs\BIBBIA-AI-$ProjectName-CLIENTE.md"

# Se esiste già una BIBBIA cliente specifica usa quella, altrimenti crea da template
if (-not (Test-Path $bibbiaClientePath)) {
    Write-WARN "BIBBIA cliente non trovata: $bibbiaClientePath"
    Write-WARN "Creata BIBBIA cliente generica — personalizzare prima della consegna!"
    $bibbiaClientePath = Join-Path $OutputPath "docs\GUIDA-AI.md"
    # Crea una guida minima
    $bibbiaContent = "# Guida AI — $ProjectName`n> Configurare la BIBBIA cliente prima della consegna.`n"
    Set-Content -Path $bibbiaClientePath -Value $bibbiaContent
}

# Crea copilot-instructions.md che punta alla BIBBIA cliente
$copilotContent = @"
# $ProjectName — Istruzioni per GitHub Copilot

> **REGOLA ASSOLUTA**: Leggi la guida completa prima di qualsiasi risposta:
> ``[PATH_LOCALE]\docs\BIBBIA-AI-$ProjectName-CLIENTE.md``
>
> **⛔ LETTURA INTEGRALE OBBLIGATORIA — PRIMA di qualsiasi risposta:**
> Usa ``read_file`` con ``startLine: 1`` e ``endLine: 400`` sulla guida AI.
> **MAI** leggere solo parzialmente.
> Ogni risposta basata su lettura parziale è INVALIDA.

---

## Identità e Ruolo

Agisci come **assistente tecnico senior** per il progetto $ProjectName di $CustomerName.

Stack: .NET 8, Blazor Server, MudBlazor, SQL Server, EF Core 8

- Versione corrente: vedi ``AppVersion.cs``
- Docs: ``[PATH_LOCALE]\docs\``

---

## Workflow Obbligatorio

``````
1. Incrementa AppVersion.cs
2. dotnet build --nologo           → 0 errori
3. .\scripts\test-smoke.ps1        → tutti [OK]
4. dotnet run (background Dev)
5. Comunica URL + output test
6. Attendi conferma utente
``````

**MAI** dire "fatto" senza test verdi allegati.
**MAI** lasciare comandi all'utente.
**MAI** modificare appsettings.Secrets.json o appsettings.Database.json.

---

## Supporto Fornitore

Per deploy, integrazioni nuove, modifiche architettura:
**$SupplierName** — $SupplierContact
"@

Set-Content -Path (Join-Path $githubFolder "copilot-instructions.md") -Value $copilotContent
Write-OK "copilot-instructions.md cliente creato"

# ─── STEP 5: Rimozione BIBBIA developer se rimasta ───────────────────────────
Write-Step "STEP 5 — Verifica finale: nessuna IP privata rimasta"

$sensitiveKeywords = @(
    "BIBBIA-AI-.*\.md",            # qualsiasi BIBBIA developer
    "lessons.learned",
    "A123456",                     # esempio password nota
    "storico.*FIX-",               # file fix storici
    "DEPLOY-LESSONS"
)

$leakedFiles = @()
Get-ChildItem -Path $OutputPath -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Replace($OutputPath, "").TrimStart("\")
    foreach ($kw in $sensitiveKeywords) {
        if ($relativePath -match $kw) {
            $leakedFiles += $relativePath
        }
    }
    # Controlla anche il contenuto dei .md per keyword sensibili
    if ($_.Extension -eq ".md" -or $_.Extension -eq ".json") {
        $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
        if ($content -match "LESSON.LEARNED|storico/FIX-|deploy.*credenziali|password.*admin" -and
            $relativePath -notmatch "storico.README") {
            $leakedFiles += "[CONTENUTO] $relativePath"
        }
    }
}

if ($leakedFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "  ⚠️  ATTENZIONE — FILE POTENZIALMENTE PRIVATI RILEVATI:" -ForegroundColor Red
    $leakedFiles | ForEach-Object { Write-Host "  ⚠️  $_" -ForegroundColor Red }
    Write-Host "  Verifica manualmente prima di consegnare!" -ForegroundColor Red
} else {
    Write-OK "Nessun file privato rilevato nel pacchetto"
}

# ─── REPORT FINALE ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "  PACCHETTO CLIENTE PRONTO" -ForegroundColor Green
Write-Host "  Output: $OutputPath" -ForegroundColor Cyan
Write-Host "  File privati rimossi: $($RemovedFiles.Count)" -ForegroundColor Cyan
if ($WarningFiles.Count -gt 0) {
    Write-Host "  ⚠️  Warning: $($WarningFiles.Count) — verifica prima di consegnare" -ForegroundColor Yellow
}
Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host ""
Write-Host "CHECKLIST FINALE prima di consegnare:" -ForegroundColor White
Write-Host "  [ ] Aprire $OutputPath e verificare manualmente" -ForegroundColor Gray
Write-Host "  [ ] Controllare che docs/storico/ sia vuoto (solo README)" -ForegroundColor Gray
Write-Host "  [ ] Verificare che .github/copilot-instructions.md sia quello cliente" -ForegroundColor Gray
Write-Host "  [ ] Controllare che appsettings.Secrets.json NON esista" -ForegroundColor Gray
Write-Host "  [ ] Fare un test build nella cartella output prima di consegnare" -ForegroundColor Gray
Write-Host "  [ ] ZIP della cartella output e consegna" -ForegroundColor Gray
Write-Host ""
