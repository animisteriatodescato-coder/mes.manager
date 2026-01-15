# MESManager.E2E - Test End-to-End con Playwright

✅ **Test Status**: 3/3 passing (Totale test: 3, Superati: 3, Tempo: ~14s)

Questo progetto contiene test E2E automatici per MESManager usando Microsoft Playwright per .NET.

## Test Implementati

1. ✅ **Home non è bianca** - Verifica che la home page carichi elementi visibili (appbar, layout MudBlazor)
2. ✅ **Navbar non duplicata** - Verifica che esista solo una istanza dell'appbar principale
3. ✅ **Commesse carica UI** - Verifica che la pagina Commesse carichi titolo, grid e header ag-Grid

**Tutti i test includono verifica automatica di errori console JavaScript.**

## Prerequisiti

- .NET 8 SDK
- Browser Chromium (installato automaticamente da Playwright)
- **Porta 5156 disponibile** - i test avviano automaticamente l'app web su questa porta

## Setup Iniziale

### 1. Installazione Browser

Dopo il primo build, eseguire:

```powershell
cd MESManager.E2E\bin\Debug\net8.0
pwsh playwright.ps1 install chromium
```

**Nota**: Questo step viene fatto automaticamente durante il primo build. Se i browser non sono installati, il comando sopra li installerà.

## Esecuzione Test

### Esecuzione Standard (Headless)

```powershell
dotnet test
```

**IMPORTANTE**: I test avviano automaticamente l'applicazione web su `http://127.0.0.1:5156`. Assicurati che:
- La porta 5156 sia disponibile (non in uso da altri processi)
- L'app web non sia già in esecuzione manualmente sulla stessa porta

### Esecuzione con Browser Visibile (Headed Mode)

```powershell
$env:PLAYWRIGHT_HEADED="1"
dotnet test
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
  run: pwsh MESManager.E2E/bin/Debug/net8.0/playwright.ps1 install chromium
  
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
pwsh MESManager.E2E/bin/Debug/net8.0/playwright.ps1 install chromium --force
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
