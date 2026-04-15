# 🧪 11 — Testing Framework

> Strategia di test, script diagnostici e procedure di debugging per [NOME_PROGETTO].

---

## 🚨 REGOLA ASSOLUTA — MAI DICHIARARE "FATTO" SENZA TEST VERDI

> **Questa regola non ha eccezioni.**

L'AI **NON PUÒ** dire:
- ❌ "Ho finito"
- ❌ "Dovrebbe funzionare"
- ❌ "Ok, è apposto"
- ❌ "Il fix è completo"

…senza aver **eseguito** `scripts/test-smoke.ps1` e allegato l'output **VERDE** nella risposta.

**Sequenza obbligatoria dopo ogni modifica:**
```
1. dotnet build --nologo          → 0 errori
2. .\scripts\test-smoke.ps1       → tutti [OK]
3. dotnet run (background)        → server su
4. Riporta URL + output test      → utente verifica
```

Se uno step fallisce → **correggi e rilancia**, NON passare avanti.

---

## 🎯 Strategia Testing

```
Smoke Test     → scripts/test-smoke.ps1 (OBBLIGATORIO dopo ogni modifica)
Unit Tests     → logica business pura (Domain + Application)
Integration    → repository + EF Core + DB reale (test DB)
E2E Tests      → flussi utente completi (Playwright)
```

---

## 📁 Struttura Test

```
tests/
├── [NomeProgetto].Tests/
│   ├── Unit/
│   │   ├── Domain/        ← test entità e value objects
│   │   └── Application/   ← test servizi con mock repositories
│   └── Integration/
│       ├── Repositories/  ← test repository con DB test
│       └── Services/      ← test servizi con DB test
│
└── [NomeProgetto].E2E/
    ├── Tests/             ← test Playwright
    ├── Pages/             ← Page Object Model
    └── Fixtures/          ← seed dati, setup
```

---

## ⚙️ Configurazione Test

### appsettings.Tests.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=[DB_SERVER_DEV];Database=[DB_NAME_DEV]_Test;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Esecuzione
```powershell
# Tutti i test
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].Tests/ -v normal

# Solo unit test
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].Tests/ --filter "Category=Unit"

# Solo integration test
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].Tests/ --filter "Category=Integration"

# Coverage
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].Tests/ --collect:"XPlat Code Coverage"
```

---

## 🎭 E2E Playwright

### Setup
```powershell
cd tests/[NomeProgetto].E2E
dotnet playwright install chromium
```

### Esecuzione
```powershell
# Con server già in esecuzione
$env:E2E_USE_EXISTING_SERVER = "1"
$env:E2E_BASE_URL = "http://localhost:[PORTA_DEV]"
dotnet test tests/[NomeProgetto].E2E/

# Con seed dati automatico
$env:E2E_SEED = "1"
dotnet test tests/[NomeProgetto].E2E/

# Per area specifica
dotnet test tests/[NomeProgetto].E2E/ --filter "Feature=[NomeFeature]"
```

### Standard E2E
- `data-testid` su tutti gli elementi critici
- Page Object Model per ogni pagina
- Screenshot su fallimento
- Baseline per visual regression

---

## � Smoke Test Obbligatorio — scripts/test-smoke.ps1

> File: `scripts/test-smoke.ps1` — **Eseguire SEMPRE dopo ogni modifica.**
> Vedi il file completo in `scripts/test-smoke.ps1` (template già pronto).

```powershell
# Esecuzione rapida (server già in esecuzione)
cd [PATH_PROGETTO]; .\scripts\test-smoke.ps1 -UseExistingServer

# Esecuzione completa (avvia e ferma il server da solo)
cd [PATH_PROGETTO]; .\scripts\test-smoke.ps1
```

**Output atteso (VERDE = OK per dichiarare "fatto"):**
```
[START] Smoke Test [NOME_PROGETTO]
[OK]    Build: 0 errori, 0 warning
[OK]    HTTP 200 — App risponde su http://localhost:[PORTA_DEV]
[OK]    Health: Healthy
[OK]    Versione: v[X.Y.Z] nel HTML
[OK]    DB: connessione OK
[OK]    DB: [NomeTabella] — N record
[SUCCESS] Smoke Test PASSED — X/X checks OK
```

**Se ROSSO → correggi prima di proseguire. MAI ignorare.**

---

## 🔍 Script Diagnostici Aggiuntivi

```powershell
# Verifica porta occupata
Get-NetTCPConnection -LocalPort [PORTA_DEV] -State Listen

# Tail log applicazione
Get-Content "logs\app-*.log" -Tail 50

# Test connection string DB
$conn = New-Object System.Data.SqlClient.SqlConnection "[CONNECTION_STRING]"
try { $conn.Open(); Write-Host "DB OK" } catch { Write-Host "ERRORE: $_" }
```

---

## 🐛 Debugging Comune

### App non si avvia
```powershell
# Controlla porta occupata
Get-NetTCPConnection -LocalPort [PORTA_DEV] -State Listen

# Controlla log
Get-Content "logs\app-*.log" -Tail 50
```

### Errori DB
```powershell
# Test connection string
$conn = New-Object System.Data.SqlClient.SqlConnection "[CONNECTION_STRING]"
try { $conn.Open(); Write-Host "DB OK" } catch { Write-Host "ERRORE: $_" }
```

### Migration pending
```powershell
cd [PATH_PROGETTO]
dotnet ef migrations list --project [NomeProgetto].Infrastructure --startup-project [NomeProgetto].Web
```

---

## 📝 Log — Formato Standard

```
[START] [NomeOperazione] — [parametri input]
[SUCCESS] [NomeOperazione] — [risultato] in [ms]ms
[WARNING] [NomeOperazione] — [messaggio] (non bloccante)
[ERROR] [NomeOperazione] — [messaggio errore] | [stack trace]
```

---

*Versione: 1.0*
