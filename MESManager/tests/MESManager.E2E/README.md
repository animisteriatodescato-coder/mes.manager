# MESManager.E2E - Test End-to-End con Playwright

✅ **Test Status**: Suite completa con test funzionali + visual regression

Suite E2E automatica per MESManager con:
- **Playwright** per browser automation
- **Page Object Model** per manutenibilità
- **Visual Regression** con screenshot baseline
- **GitHub Actions** CI/CD integration

## 🚀 Quick Start

```powershell
# Install browsers (first time only)
cd tests\MESManager.E2E
pwsh bin\Debug\net8.0\playwright.ps1 install chromium

# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Functional"
dotnet test --filter "Category=Visual"

# Debug mode (visible browser)
$env:PLAYWRIGHT_HEADED="1"
dotnet test
```

## 🖼 Visual Regression

```powershell
# Create/Update baselines
$env:UPDATE_BASELINES="true"
dotnet test --filter "Category=Visual"

# Run visual regression tests
dotnet test --filter "Category=Visual"
```

Diff images generated in `playwright-results/visual-diffs/` on failure.

### Flag locale (senza env var)

Se preferisci non usare variabili ambiente, crea il file:

```
UPDATE_BASELINES.flag
```

Nella cartella `tests/MESManager.E2E/`. Rimuovilo quando hai finito.

## 📚 Full Documentation

**Consulta [docs2/10-QA-UI-TESTING.md](../../docs2/10-QA-UI-TESTING.md) per:**
- Page Object Model pattern
- Aggiungere nuovi test
- Visual regression workflow
- CI/CD pipeline GitHub Actions
- Troubleshooting

## 🏗 Project Structure

```
tests/MESManager.E2E/
├── Pages/              # Page Object Model classes
│   ├── BasePage.cs
│   ├── HomePage.cs
│   ├── CommesseApertePage.cs
│   ├── GanttMacchinePage.cs
│   └── ProgrammaMacchinePage.cs
├── Tests/              # Test files
│   ├── HomeTests.cs
│   ├── CommesseAperteTests.cs
│   ├── GanttTests.cs
│   └── ProgrammaTests.cs
├── VisualBaselines/    # Screenshot baselines
└── playwright-results/ # Test output (screenshots, diffs)

```powershell
$env:PLAYWRIGHT_HEADED="1"
dotnet test --filter "Category=Modified"
```

### Solo test su pagina Pianificazione

```powershell
dotnet test --filter "Page=Pianificazione"
```

### Tutti i test (Core + Modified)

```powershell
dotnet test
```

**Per maggiori opzioni di filtro, consulta [TEST_FILTERS.md](TEST_FILTERS.md).**

**IMPORTANTE**: Se l'app è già avviata manualmente, usa:
```
$env:E2E_USE_EXISTING_SERVER="1"
$env:E2E_BASE_URL="http://localhost:5156"
```
Per seed automatico dati:
```
$env:E2E_SEED="1"
```
```

### Esecuzione con Slow Motion (debug)

```powershell
$env:PLAYWRIGHT_SLOWMO="200"  # Rallenta di 200ms ogni azione
dotnet test
```

### Combinazione (Headed + Slow Motion)

```powershell
$env:PLAYWRIGHT_HEADED="1"
$env:PLAYWRIGHT_SLOWMO="500"
dotnet test
```

## Struttura Test

Il progetto include 3 test chiave:

1. **Home_IsNotBlank**: Verifica che la home page non sia bianca e contenga elementi essenziali
2. **NavBar_IsNotDuplicated**: Verifica che la navbar/appbar non sia duplicata
3. **Commesse_LoadsMainUI**: Verifica che la pagina Commesse carichi correttamente grid e UI

## Gestione Errori

- **Console Errors**: Ogni errore JavaScript catturato nella console causa il fallimento del test
- **Page Errors**: Eccezioni JavaScript non gestite causano il fallimento del test
- **Screenshot**: In caso di fallimento, screenshot e log errori vengono salvati in `TestResults/Playwright/<nome_test>/`

## Output Test

Risultati salvati in:
- `TestResults/Playwright/<nome_test>/screenshot.png` - Screenshot della pagina al momento del fallimento
- `TestResults/Playwright/<nome_test>/errors.txt` - Log completo errori console e page

## Integrazione CI/CD

Per CI/CD, assicurarsi di:
1. Installare browser prima dei test: `pwsh bin/Debug/net8.0/playwright.ps1 install chromium`
2. Eseguire in headless mode (default)
3. Archiviare `TestResults/` come artifacts in caso di fallimento

Esempio GitHub Actions:

```yaml
- name: Install Playwright Browsers
  run: pwsh tests/MESManager.E2E/bin/Debug/net8.0/playwright.ps1 install chromium
  
- name: Run E2E Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
  
- name: Upload Test Results
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/
```

## Note Tecniche

- **Processo Separato**: L'app viene avviata tramite `dotnet run` in un processo separato (NON WebApplicationFactory/TestServer)
- **Porta Fissa**: Test usano porta 5156 per evitare problemi con porte dinamiche
- **Environment**: Test eseguiti in modalità Development per stacktrace dettagliati
- **Timeout**: Timeout default di Playwright (30s per azione)
- **Viewport**: 1920x1080 per consistenza cross-environment
- **WebSockets/SignalR**: Funzionano correttamente con server Kestrel reale

## Troubleshooting

### Test falliscono con "Connection refused"
- Verificare che nessun altro processo usi la porta 5156
- Chiudere eventuali istanze manuali di MESManager.Web
- Controllare firewall/antivirus che potrebbero bloccare la porta

### Browser non trovato
```powershell
pwsh tests/MESManager.E2E/bin/Debug/net8.0/playwright.ps1 install chromium --force
```

### Errori di compilazione Blazor
- Verificare che MESManager.Web compili correttamente
- `dotnet build MESManager.Web`

### Test rimangono appesi
- Il processo `dotnet.exe` (MESManager.Web) potrebbe non essere terminato correttamente
- Terminare manualmente: `Get-Process dotnet | Where-Object {$_.Path -like "*MESManager.Web*"} | Stop-Process`

## Estensione Test

Per aggiungere nuovi test, creare una nuova classe che eredita da `PlaywrightTestBase`:

```csharp
public class MyNewTests : PlaywrightTestBase
{
    [Fact]
    public async Task MyTest()
    {
        await Page.GotoAsync(BaseUrl + "/mypage");
        // ... test logic
        await AssertNoConsoleErrors();
    }
}
```

Gli hooks `data-testid` disponibili:
- `main-appbar` - AppBar principale
- `commesse-title` - Titolo pagina Commesse
- `commesse-grid` - Grid ag-Grid delle commesse
