# Test Automatici MESManager - Guida Rapida

## 🎯 Problema Risolto

**PRIMA**: "Testa manualmente e dimmi se vedi errori console"  
**DOPO**: `.\test-plc-realtime.ps1` → Risposta automatica in 30 secondi

---

## 🚀 Setup Iniziale (Una Volta Sola)

### 1. Installa Browser Playwright

```powershell
cd C:\Dev\MESManager\tests\MESManager.E2E
dotnet build --nologo
pwsh bin\Debug\net8.0\playwright.ps1 install chromium
```

**Tempo**: ~2 minuti (download browser Chromium ~100MB)

---

## 🧪 Esecuzione Test Automatici

### Opzione A: Script Dedicato (Consigliato)

```powershell
cd C:\Dev\MESManager

# Con server GIÀ in esecuzione (fast - 10 secondi)
.\test-plc-realtime.ps1 -UseExistingServer

# Server auto-start (slow - 30 secondi)
.\test-plc-realtime.ps1

# Debug visuale (mostra browser)  
.\test-plc-realtime.ps1 -UseExistingServer -Headed
```

### Opzione B: Comando Diretto dotnet test

```powershell
# Server esistente
$env:E2E_USE_EXISTING_SERVER="1"
dotnet test tests\MESManager.E2E\MESManager.E2E.csproj --filter "Feature=Produzione" --nologo

# Tutti i test E2E (slow - ~5 minuti)
dotnet test tests\MESManager.E2E\MESManager.E2E.csproj --nologo
```

---

## ✅ Risultati Test

### Test Superati ✅

```
╔════════════════════════════════════════════════════════════════╗
║                    ✅ TEST SUPERATI ✅                         ║
╠════════════════════════════════════════════════════════════════╣
║  Nessun errore console rilevato                                ║
║  Lifecycle Blazor funziona correttamente                       ║
║  Navigazione tra pagine OK                                     ║
╚════════════════════════════════════════════════════════════════╝
```

**Significa**: PLC Realtime funziona senza errori JavaScript/console.

### Test Falliti ❌

```
╔════════════════════════════════════════════════════════════════╗
║                    ❌ TEST FALLITI ❌                          ║
╠════════════════════════════════════════════════════════════════╣
║  Errori console rilevati o eccezioni JavaScript               ║
╚════════════════════════════════════════════════════════════════╝

📁 Artifacts salvati in: TestResults/Playwright/
```

**Artifacts disponibili**:
- `screenshot.png`: Screenshot pagina al momento del fallimento
- `errors.txt`: Lista errori console catturati
- `video.webm`: Video sessione browser (se abilitato)

---

## 🔬 Test Disponibili

| Test | Descrizione | Durata |
|------|-------------|--------|
| `PlcRealtime_PageLoads` | Carica pagina PLC Realtime | 2s |
| `PlcRealtime_LifecycleComplete` | Load → Navigate Away → Reload → Navigate Away | 5s |
| `PlcRealtime_NavigazioneProduzione` | Naviga tutte pagine Produzione (Dashboard, Realtime, Storico, Incollaggio) | 4s |

**Totale test Produzione**: 6 test (~15 secondi con server esistente)

---

## 🐛 Debug Test Falliti

### 1. Visualizza Browser Durante Test

```powershell
.\test-plc-realtime.ps1 -UseExistingServer -Headed -SlowMo 500
```

- **Headed**: Mostra browser Chrome
- **SlowMo**: Rallenta azioni di 500ms (osserva cosa succede)

### 2. Leggi Errori Dettagliati

```powershell
cd tests\MESManager.E2E\TestResults\Playwright
Get-ChildItem -Recurse errors.txt | ForEach-Object { Get-Content $_.FullName }
```

### 3. Screenshot Fallimento

```powershell
cd tests\MESManager.E2E\TestResults\Playwright
Get-ChildItem -Recurse screenshot.png | ForEach-Object { Invoke-Item $_.FullName }
```

---

## 📋 Workflow Consigliato

### Dopo Ogni Modifica a PlcRealtime.razor

```powershell
# 1. Build
cd C:\Dev\MESManager
dotnet build MESManager.sln --nologo

# 2. (Se server non running) Start server background
cd C:\Dev
dotnet run --project MESManager/MESManager.Web/MESManager.Web.csproj --environment Development &

# 3. Test automatici (SENZA bisogno di aprire browser manualmente)
.\test-plc-realtime.ps1 -UseExistingServer
```

**Tempo totale**: Build (10s) + Test (10s) = **20 secondi** vs **2+ minuti manuali**

---

## 🎯 Vantaggi Test Automatici

| Aspetto | Test Manuale | Test Automatico |
|---------|--------------|-----------------|
| **Tempo** | 2-5 minuti | 10-30 secondi |
| **Console errors** | "Vedo errori rossi" (soggettivo) | Cattura TUTTI console.error() |
| **Lifecycle** | Solo caricamento pagina | Load + Navigate Away + Reload |
| **Riproducibilità** | Varia a seconda utente | Identico sempre |
| **CI/CD** | Impossibile | GitHub Actions ready |
| **Screenshot** | Manuale | Automatico se fallisce |
| **Multi-browser** | Uno alla volta | Chromium/Firefox/Safari paralleli |

---

## ⚙️ Configurazione Avanzata

### Variabili Ambiente

```powershell
# URL server (default: http://localhost:5156)
$env:E2E_BASE_URL = "http://localhost:8080"

# Usa server già running (default: 0 = auto-start)
$env:E2E_USE_EXISTING_SERVER = "1"

# Mostra browser (default: 0 = headless)
$env:PLAYWRIGHT_HEADED = "1"

# SlowMo ms tra azioni (default: 0)
$env:PLAYWRIGHT_SLOWMO = "500"

# Timeout startup server (default: 90 secondi)
$env:WEBAPP_STARTUP_TIMEOUT_SECONDS = "120"
```

### Filtraggio Test

```powershell
# Solo test PLC Realtime
dotnet test --filter "FullyQualifiedName~PlcRealtime" --nologo

# Solo test Produzione
dotnet test --filter "Feature=Produzione" --nologo

# Solo test Cataloghi
dotnet test --filter "Feature=Cataloghi" --nologo

# Escludi test lenti
dotnet test --filter "Category!=Slow" --nologo
```

---

## 📊 Integrazione CI/CD

I test sono compatibili con GitHub Actions. Esempio workflow:

```yaml
- name: Run E2E Tests
  run: |
    dotnet test tests/MESManager.E2E/MESManager.E2E.csproj --nologo
  env:
    E2E_USE_EXISTING_SERVER: "0"  # Auto-start
    PLAYWRIGHT_HEADED: "0"        # Headless
```

---

## 🆘 Troubleshooting

### Errore: "Executable doesn't exist"

**Causa**: Browser Chromium non installato

**Fix**:
```powershell
cd tests\MESManager.E2E
pwsh bin\Debug\net8.0\playwright.ps1 install chromium
```

### Errore: "Server failed to start"

**Causa**: Server ASP.NET non partito entro timeout

**Fix**:
```powershell
# Aumenta timeout
$env:WEBAPP_STARTUP_TIMEOUT_SECONDS = "180"

# OPPURE usa server esistente
$env:E2E_USE_EXISTING_SERVER = "1"
```

### Test lenti (>2 minuti)

**Causa**: Auto-start server + build

**Fix**: Usa server esistente
```powershell
.\test-plc-realtime.ps1 -UseExistingServer
```

---

## 📚 Riferimenti

- [10-QA-UI-TESTING.md](docs/10-QA-UI-TESTING.md): Documentazione completa E2E
- [09-TESTING-FRAMEWORK.md](docs/09-TESTING-FRAMEWORK.md): Testing patterns & best practices
- [Playwright .NET Docs](https://playwright.dev/dotnet/): API reference ufficiale

---

**Versione**: v1.46.1  
**Ultima modifica**: 23 Feb 2026
