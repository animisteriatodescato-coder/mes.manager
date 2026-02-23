# 03 - Configurazione

> **Scopo**: Configurazione database, sicurezza, secrets e PLC

---

## 🗄️ Database - Connection Strings

### File Unico Centralizzato

**TUTTI i progetti leggono da**: `appsettings.Database.json` (root)

**Prima** (❌ sbagliato):
- Connection string in 4 file diversi
- Ogni modifica = 4 file da aggiornare

**Ora** (✅ corretto):
- Un solo file: `appsettings.Database.json`
- Tutti i progetti lo leggono automaticamente

---

### Struttura File

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;",
    "MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;",
    "GanttDb": "Server=192.168.1.230\\SQLEXPRESS01;Database=Gantt;User Id=fab;Password=fabpwd;TrustServerCertificate=True;"
  }
}
```

---

### Connection Strings per Ambiente

#### Locale (Sviluppo)
```json
"MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### Server (Produzione)
```json
"MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Prod;User Id=fab;Password=password.123;TrustServerCertificate=True;"
```

**⚠️ NOTA**: Sul server si usa `localhost` perché SQL Server è sulla stessa macchina!

---

### Test Connection String

```powershell
# Test connessione locale
sqlcmd -S localhost\SQLEXPRESS01 -d MESManager -Q "SELECT DB_NAME()"

# Test connessione produzione (dal server)
sqlcmd -S localhost\SQLEXPRESS01 -d MESManager_Prod -U fab -P password.123 -Q "SELECT DB_NAME()"
```

---

## 🔒 Sicurezza - Secrets Management

### Ordine di Caricamento Configurazione

#### Web Application (MESManager.Web)

L'applicazione Web carica le configurazioni in questo ordine (ultimo vince):

1. `appsettings.json` - Config base
2. `appsettings.{Environment}.json` - Per ambiente (Development/Production)
3. `appsettings.Secrets.encrypted` - **Credenziali cifrate DPAPI** (preferito su Windows)
4. `appsettings.Secrets.json` - **Credenziali in chiaro** (fallback, non in git)
5. `appsettings.Database.json` - Legacy (deprecato)
6. Variabili d'ambiente - Per container/cloud

#### Worker Service (MESManager.Worker)

Il Worker carica le configurazioni in questo ordine:

1. `appsettings.json` - Config base Worker
2. `appsettings.Development.json` - Config Worker per ambiente
3. **`appsettings.Secrets.json` (root)** - **Credenziali condivise con Web** ⭐ PREFERITO
4. `appsettings.Database.json` (root) - Fallback legacy
5. `appsettings.Database.{Environment}.json` - Override ambiente (opzionale)

**⚠️ CRITICO**: Worker e Web **DEVONO** usare lo stesso database per ambiente!

**Configurazione corretta** (file `appsettings.Secrets.json` nella root):
```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Dev;Integrated Security=True;TrustServerCertificate=True;",
    "MagoDb": "Server=192.168.1.72\\SQLEXPRESS01;Database=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Connection Timeout=30;"
  }
}
```

**Errore comune**: Worker usa `appsettings.Database.json` (DB prod) mentre Web usa `appsettings.Secrets.json` (DB dev) → sincronizzazione manuale funziona, automatica fallisce!

**Soluzione**: Worker ora usa stessa logica del Web (vedi [DEPLOY-LESSONS-LEARNED.md - Problema 7](storico/DEPLOY-LESSONS-LEARNED.md#-problema-7-sync-automatica-fallisce-worker-vs-web-database-diversi))

---

### Setup File Secrets

#### 1. Crea File da Template

```powershell
# Dalla root del progetto
Copy-Item "appsettings.Secrets.json.template" "appsettings.Secrets.json"
```

#### 2. Modifica Credenziali

Apri `appsettings.Secrets.json` e inserisci credenziali reali:

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=TUO_SERVER;Database=TUO_DB;User Id=TUO_USER;Password=TUA_PASSWORD;TrustServerCertificate=True;"
  }
}
```

#### 3. Verifica Esclusione Git

```powershell
git status
# Il file appsettings.Secrets.json NON deve apparire
```

Se appare, aggiungi a `.gitignore`:
```
appsettings.Secrets.json
appsettings.Database.json
```

---

### Secrets Cifrati (Produzione)

Per produzione, usa `appsettings.Secrets.encrypted` con DPAPI:

```powershell
# Cifra file (Windows DPAPI)
# Automatico su server Windows
```

**Vantaggi**:
- File cifrato a livello macchina/utente
- Impossibile leggere da altra macchina
- Sicurezza extra per produzione

---

## 🔐 Best Practice Sicurezza

### ❌ NON Fare

1. ❌ Non committare `appsettings.Secrets.json` in Git
2. ❌ Non copiare file secrets dal dev al server
3. ❌ Non mettere password in plain text in file versionati
4. ❌ Non usare stesse credenziali dev/prod

### ✅ Fare

1. ✅ Usa template (`.template`) per esempi
2. ✅ File secrets diversi per dev/prod
3. ✅ Escludere secrets da Git (`.gitignore`)
4. ✅ Usa DPAPI per cifratura su Windows
5. ✅ Permessi SQL con "least privilege"

---

## 📦 Archivio Dati Allegati

### Strategia Implementata

**Principio**: Ambiente DEV connesso **direttamente** al database PROD per accesso ai dati reali.

**Approccio**:
- DEV legge direttamente da `MESManager_Prod` su server `192.168.1.230`
- Nessun database locale allegati
- Nessuno script di sincronizzazione
- Configurazione semplice: un solo connection string per ambiente

**Benefici**:
- ✅ Dati reali per test (901 articoli, 785 allegati)
- ✅ Nessuna duplicazione o desincronizzazione
- ✅ Zero complessità di sync
- ✅ Configurazione minimale

---

### Configurazione DEV

**File**: `appsettings.Database.Development.json` (root)

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=192.168.1.230\\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;"
  },
  "Files": {
    "AllegatiBasePath": "\\\\192.168.1.230\\Dati\\Documenti\\AA SCHEDE PRODUZIONE\\foto cel",
    "PathMappings": [
      "P:\\Documenti->\\\\192.168.1.230\\Dati\\Documenti",
      "P:\\->\\\\192.168.1.230\\Dati\\",
      "C:\\Dati\\->\\\\192.168.1.230\\Dati\\"
    ]
  }
}
```

**⚠️ IMPORTANTE**:
- La tabella allegati è `AllegatiArticoli` (non `Allegati`)
- Database: `MESManager_Prod` (non `Gantt`)
- Path file via UNC `\\192.168.1.230\Dati` (richiede rete attiva)
- Credenziali: `User Id=FAB; Password=password.123`

---

### Configurazione PROD

**File**: `appsettings.Database.Production.json` (sul server)

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Prod;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Files": {
    "AllegatiBasePath": "C:\\Dati\\Documenti\\AA SCHEDE PRODUZIONE\\foto cel",
    "PathMappings": [
      "P:\\Documenti->C:\\Dati\\Documenti",
      "P:\\->C:\\Dati\\"
    ]
  }
}
```

**Differenze**:
- Server locale (`localhost`) con autenticazione Windows
- Path fisici locali (`C:\Dati`)

---

### Struttura Database

**Tabella**: `[dbo].[AllegatiArticoli]` in `MESManager_Prod`

**Colonne principali**:
- `Id` - Primary key
- `Archivio` - Tipo archivio ('ARTICO' per articoli)
- `IdArchivio` - FK a `anime.Id`
- `CodiceArticolo` - Codice articolo per lookup rapido
- `PathFile` - Path completo del file
- `Descrizione` - Descrizione allegato
- `TipoFile` - 'Foto' o 'Documento'
- `Priorita` - Ordinamento

**Dati**:
- 901 articoli (tabella `anime`)
- 785 allegati (tabella `AllegatiArticoli`)
- 327 articoli hanno allegati

---

### Test Configurazione

```powershell
# Verifica connessione DEV → PROD
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=192.168.1.230\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) FROM [dbo].[AllegatiArticoli] WHERE Archivio = 'ARTICO'"
$count = $cmd.ExecuteScalar()
Write-Host "✅ Allegati disponibili: $count" -ForegroundColor Green
$conn.Close()

# Verifica path UNC
Test-Path "\\192.168.1.230\Dati\Documenti"

# Test API locale
Invoke-WebRequest -Uri "http://localhost:5156/api/anime" | ConvertFrom-Json | Select-Object -ExpandProperty Count
Invoke-WebRequest -Uri "http://localhost:5156/api/AllegatiAnima/codice/300014" | ConvertFrom-Json
```

---

## 🏭 Configurazione Macchine PLC

### Architettura Ibrida (Database + JSON)

| Dato | Fonte | Dove Modificarlo |
|------|-------|------------------|
| **Elenco macchine** | Database | Impostazioni → Gantt Macchine |
| **IP PLC** | Database | Impostazioni → Gantt Macchine |
| **Codice macchina** | Database | Impostazioni → Gantt Macchine |
| **Offset PLC** | File JSON | `PlcSync/Configuration/machines/*.json` |
| **Parametri connessione** | File JSON | `PlcSync/Configuration/machines/*.json` |

---

### Come Funziona

**PlcSync Worker** all'avvio:

1. Carica macchine dal database (con IP)
2. Carica file JSON per offset PLC
3. **Sovrascrive IP del JSON con quello del database**
4. Usa configurazione risultante per connettersi ai PLC

**Regola**: IP macchina sempre dal database, offset sempre dal JSON!

---

### Esempio File JSON

`PlcSync/Configuration/machines/macchina_003.json`:

```json
{
  "MachineId": "11111111-1111-1111-1111-111111111103",
  "MachineCode": "M003",
  "PlcIp": "192.168.17.24",  // ← Questo viene SOVRASCRITTO dal DB
  "Rack": 0,
  "Slot": 1,
  "DbNumber": 55,
  "Offsets": {
    "CicliFattiOffset": 0,
    "StatoOffset": 4,
    "BarcodeOffset": 8,
    "BarcodeLength": 20
  }
}
```

**⚠️ Importante**: Modifica IP solo nel database, non nel JSON!

---

### Modificare IP Macchina

1. Vai su **Impostazioni → Gantt Macchine**
2. Trova la macchina (es. M003)
3. Modifica campo **Indirizzo PLC**
4. Salva
5. Riavvia **PlcSync** (non serve modificare JSON!)

```powershell
# Riavvia PlcSync
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.PlcSync.exe /F
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESPlcSync"
```

---

## 🛠️ Configurazione Servizi

### Web (MESManager.Web)

File: `MESManager.Web/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5156"
      }
    }
  }
}
```

---

### Worker (Sync Mago)

File: `MESManager.Worker/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Mago": {
    "SyncIntervalMinutes": 5,
    "EnableAutoSync": true
  }
}
```

**⚠️ NON copiare** questo file sul server! Ha configurazioni specifiche.

---

### PlcSync

File: `MESManager.PlcSync/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MESManager.PlcSync": "Debug"
    }
  },
  "PlcSync": {
    "PollingIntervalMs": 1000,
    "ConnectionTimeoutMs": 5000,
    "RetryAttempts": 3,
    "MachineConfigPath": "Configuration/machines"
  }
}
```

**⚠️ NON copiare** questo file sul server! Ha configurazioni specifiche PLC.

---

## 📁 File da NON Copiare Mai

Durante il deploy, **ESCLUDERE SEMPRE**:

| File | Motivo |
|------|--------|
| `appsettings.Secrets.json` | Password produzione |
| `appsettings.Database.json` | Connection string produzione |
| `PlcSync/Configuration/machines/*.json` | Config IP macchine (se già configurate) |
| `PlcSync/appsettings.json` | Config polling PLC |
| `*.log` | File log inutili |
| `*.pdb` | Debug symbols |

---

## 🧪 Troubleshooting Configurazione

### ❌ "Connection string 'MESManagerDb' not found"

**Causa**: File `appsettings.Database.json` mancante

**Soluzione**:
```powershell
# Verifica esistenza
Test-Path "C:\Dev\MESManager\appsettings.Database.json"

# Se mancante, crea da esempio
Copy-Item "appsettings.Database.json.template" "appsettings.Database.json"
```

---

### ❌ "Could not find file appsettings.Database.json"

**Causa**: File non nella root del progetto

**Soluzione**:
```powershell
# Il file deve stare qui:
# C:\Dev\MESManager\appsettings.Database.json

# NON qui:
# C:\Dev\MESManager\MESManager.Web\appsettings.Database.json
```

---

### ❌ Connection string non letta dopo modifica

**Causa**: App in esecuzione, non ricaricato

**Soluzione**:
```powershell
# Riavvia applicazione
taskkill /IM dotnet.exe /F

# Oppure riavvia servizi su server
```

---

### ❌ PlcSync non si connette dopo cambio IP

**Causa**: IP modificato nel JSON invece che nel database

**Soluzione**:
1. Verifica IP nel database: `SELECT Codice, IndirizzoPLC FROM Macchine`
2. Aggiorna IP nel database (Impostazioni → Gantt Macchine)
3. Riavvia PlcSync

---

### ❌ Errore "Invalid connection string syntax"

**Causa**: Backslash non doppi in JSON

**Soluzione**:
```json
// ❌ Sbagliato
"Server=localhost\SQLEXPRESS01;..."

// ✅ Corretto
"Server=localhost\\SQLEXPRESS01;..."
```

In JSON, il carattere `\` deve essere `\\` (escaped).

---

## 🔄 Ambienti

### Locale (Sviluppo)

```
Database: localhost\SQLEXPRESS01 → MESManager
Auth:     Windows Authentication
File:     appsettings.Development.json + appsettings.Database.json
```

### Server (Produzione)

```
Database: localhost\SQLEXPRESS01 → MESManager_Prod
Auth:     SQL Authentication (fab / password.123)
File:     appsettings.Production.json + appsettings.Database.json
```

---

## 📝 Checklist Configurazione Iniziale

- [ ] File `appsettings.Database.json` presente in root
- [ ] File `appsettings.Secrets.json` creato (non committato)
- [ ] Connection string locale testata con sqlcmd
- [ ] IP macchine configurati nel database
- [ ] File JSON PLC con offset corretti
- [ ] Variabile ambiente `ASPNETCORE_ENVIRONMENT` impostata (Development/Production)

---

## 🆘 Supporto

Per deploy: [01-DEPLOY.md](01-DEPLOY.md)  
Per PLC sync: [07-PLC-SYNC.md](07-PLC-SYNC.md)  
Per architettura: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)
