# Guida Testing E2E e Visual Regression

## 📋 Indice

- [Panoramica](#panoramica)
- [Stack Tecnologico](#stack-tecnologico)
- [Struttura Test](#struttura-test)
- [Esecuzione Locale](#esecuzione-locale)
- [Visual Regression](#visual-regression)
- [Page Object Model](#page-object-model)
- [CI/CD GitHub Actions](#cicd-github-actions)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## 🎯 Panoramica

MESManager ha un sistema completo di test E2E che include:

- **Test Funzionali**: Verifica comportamento UI (click button, navigazione, CRUD)
- **Visual Regression**: Screenshot baseline + diff detection pixel-by-pixel
- **GitHub Actions CI**: Esecuzione automatica su ogni push/PR
- **Artifacts dettagliati**: Screenshot, video, traces, diffs su failure

### Standard

- ✅ **data-testid**: Tutti i componenti critici hanno attributi `data-testid="nome-elemento"`
- ✅ **Page Object Model (POM)**: Test leggibili e manutenibili
- ✅ **Headless configurabile**: `PLAYWRIGHT_HEADED=1` per debug visivo
- ✅ **Auto-start server**: Server dotnet run avviato automaticamente
- ✅ **Isolamento**: Ogni test in browser context pulito
- ✅ **Seed dati E2E**: Popolamento automatico DB quando `E2E_SEED=1`
- ✅ **Server esterno**: Usa istanza già avviata con `E2E_USE_EXISTING_SERVER=1`

---

## 🛠 Stack Tecnologico

| Componente | Tecnologia | Versione |
|------------|-----------|----------|
| Test Framework | xUnit | 2.5.3 |
| Browser Automation | Playwright | 1.57.0 |
| Visual Regression | SixLabors.ImageSharp | 3.1.6 |
| Code Coverage | coverlet.collector | 6.0.0 |
| CI/CD | GitHub Actions | - |

---

## 📂 Struttura Test

```
tests/MESManager.E2E/
├── Pages/                          # Page Object Model
│   ├── BasePage.cs                 # Classe base comune
│   ├── HomePage.cs                 # Home + navigazione
│   ├── CommesseApertePage.cs       # Commesse aperte + auto-scheduler
│   ├── GanttMacchinePage.cs        # Gantt Syncfusion
│   └── ProgrammaMacchinePage.cs    # Programma read-only
│
├── Tests/                          # Test organizzati per feature
│   ├── HomeTests.cs                # Test navigazione
│   ├── CommesseAperteTests.cs      # Test commesse + v1.31
│   ├── GanttTests.cs               # Test Gantt + drag&drop
│   └── ProgrammaTests.cs           # Test programma macchine
│   ├── CataloghiTests.cs            # Cataloghi (anime, clienti, preventivi)
│   ├── ProduzioneTests.cs           # Produzione (dashboard, PLC)
│   └── ImpostazioniTests.cs         # Impostazioni (utenti, festivi, tabelle)
│
├── VisualBaselines/                # Baseline screenshot per visual regression
│   └── chromium/
│       ├── HomePage/
│       ├── CommesseApertePage/
│       ├── GanttMacchinePage/
│       └── ProgrammaMacchinePage/
│
├── playwright-results/             # Output test failures
│   ├── visual-diffs/               # Immagini diff per regression
│   ├── screenshots/
│   ├── videos/
│   └── traces/
│
├── PlaywrightTestBase.cs           # Setup browser + server
├── VisualRegressionHelper.cs       # Logic visual regression
└── MESManager.E2E.csproj
```

---

## 🚀 Esecuzione Locale

### Prerequisiti

```powershell
# .NET 8 SDK
dotnet --version  # Deve essere >= 8.0

# Installazione browser Playwright (FIRST TIME ONLY)
cd MESManager\tests\MESManager.E2E
pwsh bin\Debug\net8.0\playwright.ps1 install chromium
```

### Tutti i test

```powershell
cd MESManager\tests\MESManager.E2E
dotnet test
```

### Esecuzione per area (script automatico)

```powershell
# Programma (Commesse + Gantt + Programma Macchine)
.\tests\run-area.ps1 -Area Programma -UseExistingServer -Seed

# Cataloghi
.\tests\run-area.ps1 -Area Cataloghi -UseExistingServer -Seed

# Produzione
.\tests\run-area.ps1 -Area Produzione -UseExistingServer -Seed

# Impostazioni
.\tests\run-area.ps1 -Area Impostazioni -UseExistingServer -Seed
```

### Test specifici per categoria

```powershell
# Solo test funzionali
dotnet test --filter "Category=Functional"

# Solo visual regression
dotnet test --filter "Category=Visual"

# Solo feature CommesseAperte
dotnet test --filter "Feature=CommesseAperte"

# Solo feature Cataloghi / Produzione / Impostazioni
dotnet test --filter "Feature=Cataloghi"
dotnet test --filter "Feature=Produzione"
dotnet test --filter "Feature=Impostazioni"

# Solo v1.31 auto-scheduler
dotnet test --filter "Version=v1.31"
```

### Test singolo

```powershell
dotnet test --filter "DisplayName~AutoScheduler"
```

### Modalità debug (browser visibile)

```powershell
$env:PLAYWRIGHT_HEADED="1"
dotnet test --filter "CommesseAperte_AutoSchedulerButtonVisible"

### Uso server già avviato (consigliato se l'app è in esecuzione)

```powershell
$env:E2E_USE_EXISTING_SERVER="1"
$env:E2E_BASE_URL="http://localhost:5156"
dotnet test
```

### Seed automatico dati (DB E2E)

```powershell
$env:E2E_SEED="1"
dotnet test --filter "Category=Functional"
```
```

### Modalità slow-motion (debug timing issues)

```powershell
$env:PLAYWRIGHT_SLOWMO="500"   # 500ms delay per azione
$env:PLAYWRIGHT_HEADED="1"
dotnet test
```

---

## 🖼 Visual Regression

### Come funziona

1. **Primo run**: Test FALLISCE con `BASELINE MANCANTE` → crea screenshot in `playwright-results/`
2. **Aggiorna baseline**: `UPDATE_BASELINES=true dotnet test` → salva in `VisualBaselines/`
3. **Run successivi**: Confronto pixel-by-pixel con threshold 1% (configurabile)
4. **Failure**: Genera immagine diff con pixel diversi in ROSSO

### Comandi

```powershell
# Crea/Aggiorna tutte le baseline
$env:UPDATE_BASELINES="true"
dotnet test --filter "Category=Visual"

# Aggiorna baseline singola
$env:UPDATE_BASELINES="true"
dotnet test --filter "Home_LayoutVisualRegression"

# Test visual regression con threshold custom
$env:VISUAL_DIFF_THRESHOLD="0.02"  # 2% differenza tollerata
dotnet test --filter "Category=Visual"

# Run normale (confronto con baseline)
dotnet test --filter "Category=Visual"
```

### Flag locale (senza variabili ambiente)

In alternativa, crea il file:

```
tests/MESManager.E2E/UPDATE_BASELINES.flag
```

Quando il file esiste, i test visuali rigenerano le baseline. Ricorda di eliminarlo dopo l'uso.

### Analisi failures

Quando test visual fallisce:

```
❌ VISUAL REGRESSION DETECTED
Screenshot: commesse-aperte-grid
Diff: 3.45% (threshold: 1.00%)
Files:
    - Baseline: C:\Dev\MESManager\tests\MESManager.E2E\VisualBaselines\chromium\CommesseApertePage\commesse-aperte-grid.png
    - Actual: C:\Dev\MESManager\tests\MESManager.E2E\playwright-results\visual-diffs\CommesseApertePage\commesse-aperte-grid-actual.png
    - Diff: C:\Dev\MESManager\tests\MESManager.E2E\playwright-results\visual-diffs\CommesseApertePage\commesse-aperte-grid-diff.png
```

**Passi**:
1. Apri `*-diff.png` → vedi pixel diversi in ROSSO su background grigio
2. Confronta `baseline.png` vs `actual.png`
3. Se cambio intenzionale → `UPDATE_BASELINES=true dotnet test`
4. Se bug UI → fixa e re-run

### Configurazione

```csharp
// VisualRegressionHelper.cs
const int tolerance = 10;  // Tolleranza RGB anti-aliasing
_diffThreshold = 0.01f;    // 1% pixel diversi
```

---

## 📄 Page Object Model (POM)

### Naming Convention data-testid

```razor
<!-- Pagine -->
<div data-testid="page-nome-pagina">

<!-- Buttons -->
<MudButton data-testid="btn-azione-specifica">

<!-- Dialogs/Modals -->
<MudDialog data-testid="dlg-nome-modale">

<!-- Grids -->
<div data-testid="grid-nome-griglia">

<!-- Inputs -->
<MudNumericField data-testid="input-nome-campo">
```

### Esempio: Aggiungere nuovo test

**1. Aggiungi data-testid al componente**

```razor
<!-- CommesseAperte.razor -->
<MudButton Color="Color.Success" 
           data-testid="btn-esporta-excel"
           OnClick="EsportaExcel">
    📊 Esporta Excel
</MudButton>
```

**2. Aggiungi metodo al Page Object**

```csharp
// Pages/CommesseApertePage.cs
private ILocator BtnEsportaExcel => GetByTestId("btn-esporta-excel");

public async Task ClickEsportaExcel()
{
    await ClickAsync(BtnEsportaExcel);
}
```

**3. Scrivi test**

```csharp
// Tests/CommesseAperteTests.cs
[Fact(DisplayName = "CommesseAperte > Esporta Excel funziona")]
public async Task CommesseAperte_EsportaExcelWorks()
{
    var page = new CommesseApertePage(Page, BaseUrl);
    await page.NavigateAsync("commesse-aperte");
    
    await page.ClickEsportaExcel();
    
    // Verifica download...
    await AssertNoConsoleErrors();
}
```

### BasePage Utilities

```csharp
// Tutti i Page Object ereditano questi metodi:

await WaitForVisible(locator);              // Wait visibilità
await ClickAsync(locator);                  // Click safe
await FillAsync(locator, value);            // Input fill
await AssertVisuallyMatches("screenshot");  // Visual regression
AssertNoErrors(consoleErrors, pageErrors);  // Check errori
```

---

## ⚙️ CI/CD GitHub Actions

### Workflow: `.github/workflows/ci.yml`

#### Jobs

1. **build**: Compila solution + unit test
2. **e2e-tests**: Esegue test funzionali Playwright
3. **visual-regression**: Esegue visual regression con baseline cache
4. **report**: Genera summary markdown

#### Artifacts caricati su failure

- `playwright-screenshots`: Screenshot pagine failure
- `playwright-traces`: Playwright trace files (debug timing)
- `visual-diffs`: Immagini diff per visual regression
- `e2e-test-results`: File `.trx` risultati test

#### Badge Status

Aggiungi al README.md:

```markdown
[![CI Status](https://github.com/TUO-USERNAME/MESManager/workflows/CI%20-%20Build%20&%20E2E%20Tests/badge.svg)](https://github.com/TUO-USERNAME/MESManager/actions)
```

#### Aggiornare baseline da CI

```yaml
# In .github/workflows/ci.yml, job visual-regression
- name: Run visual regression tests
  env:
    UPDATE_BASELINES: 'true'  # ⚠️ Solo su branch dedicato!
  run: dotnet test --filter "Category=Visual"
```

**⚠️ ATTENZIONE**: Aggiornare baseline da CI richiede commit dei file `VisualBaselines/`. Usa workflow manuale:

1. Crea branch `update-visual-baselines`
2. Abilita `UPDATE_BASELINES: true` nel workflow
3. Run workflow manualmente
4. Download artifact baseline
5. Commit in branch → PR con review screenshot

---

## 🔧 Troubleshooting

### Test flaky (passano/falliscono random)

**Causa**: Timing issues, animazioni, network lento

**Fix**:
```csharp
// Aumenta timeout
await WaitForVisible(locator, timeoutMs: 15000);

// Disabilita animazioni (già fatto in visual regression)
await Page.ScreenshotAsync(new() {
    Animations = ScreenshotAnimations.Disabled
});

// Attende stabilità rete
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

### Server non si avvia

**Causa**: Porta 5156 occupata, build fallita

**Fix**:
```powershell
# Verifica porta libera
netstat -ano | findstr :5156

# Kill processo se presente
taskkill /PID <PID> /F

# Rebuild
dotnet build --no-incremental
```

### Browser non trovato

**Causa**: Playwright browser non installati

**Fix**:
```powershell
cd MESManager\tests\MESManager.E2E
pwsh bin\Debug\net8.0\playwright.ps1 install chromium --with-deps
```

### Visual regression sempre fallisce

**Causa**: Font diversi, rendering OS-specific, baseline mancante

**Fix**:
```powershell
# Verifica baseline esiste
ls VisualBaselines\chromium\

# Se mancante: crea baseline
$env:UPDATE_BASELINES="true"
dotnet test --filter "Nome_Test"

# Se persiste: aumenta threshold
$env:VISUAL_DIFF_THRESHOLD="0.05"  # 5%
```

### Errori console JS

**Causa**: AG-Grid warnings, Syncfusion logs

**Fix**:
```csharp
// PlaywrightTestBase.cs - filtra warning noti
Page.Console += (_, msg) =>
{
    if (msg.Type == "warning" && msg.Text.Contains("AG-Grid"))
        return; // Ignora
    _consoleErrors.Add(msg.Text);
};
```

---

## ✅ Best Practices

### 1. Test Atomici

```csharp
// ❌ MALE: Test fa troppe cose
[Fact]
public async Task MegaTest() {
    await NavigateHome();
    await NavigateToCommesse();
    await SelectCommessa();
    await CaricaSuGantt();
    await VerificaGantt();
    await VerificaProgramma();
}

// ✅ BENE: Test singola responsabilità
[Fact]
public async Task CommesseAperte_CaricaSuGantt_ShowsSuccessMessage() {
    var page = new CommesseApertePage(Page, BaseUrl);
    await page.NavigateAsync("commesse-aperte");
    await page.SelectCommessa("TEST123");
    await page.ClickCaricaSuGantt();
    
    var message = await page.GetStatusMessageAsync();
    Assert.Contains("successo", message.ToLower());
}
```

### 2. Wait Espliciti

```csharp
// ❌ MALE: Sleep fisso
await Task.Delay(3000);

// ✅ BENE: Wait condizionale
await locator.WaitForAsync(new() {
    State = WaitForSelectorState.Visible,
    Timeout = 5000
});
```

### 3. Selettori Stabili

```csharp
// ❌ MALE: Selettore fragile
var button = Page.Locator("div > div > button:nth-child(3)");

// ✅ BENE: data-testid
var button = Page.GetByTestId("btn-carica-su-gantt");
```

### 4. Cleanup

```csharp
// ✅ Test base fa cleanup automatico
public override async Task DisposeAsync()
{
    _webAppProcess?.Kill();  // Termina server
    await base.DisposeAsync();  // Chiude browser
}
```

### 5. Categorizzazione

```csharp
[Trait("Category", "Functional")]  // Funzionali
[Trait("Category", "Visual")]      // Visual regression
[Trait("Category", "Integration")] // Require DB
[Trait("Feature", "CommesseAperte")]
[Trait("Version", "v1.31")]
```

### 6. DisplayName leggibili

```csharp
[Fact(DisplayName = "CommesseAperte > Auto-Scheduler v1.31 > Click carica mostra messaggio successo")]
```

---

## 📊 Metriche

**Target Quality Gates** (da configurare in futuro):

- ✅ **Code Coverage**: >= 70% (unit + integration)
- ✅ **E2E Pass Rate**: >= 95%
- ✅ **Visual Regression**: 0 failures su stable branch
- ✅ **Build Time**: < 10 minuti
- ✅ **Test Execution**: < 5 minuti

---

## 🔗 Riferimenti

- [Playwright .NET Docs](https://playwright.dev/dotnet/)
- [xUnit Documentation](https://xunit.net/)
- [SixLabors.ImageSharp](https://docs.sixlabors.com/index.html)
- [GitHub Actions](https://docs.github.com/en/actions)

---

**Ultimo aggiornamento**: 2025-01-XX (v1.31)
**Autore**: MESManager Development Team
