# 05 - Replica Sistema

> **Scopo**: Setup completo nuovo ambiente MESManager da zero

---

## 📋 Indice Rapido

1. [Requisiti Sistema](#requisiti-sistema)
2. [Installazione Software](#installazione-software)
3. [Setup Database](#setup-database)
4. [Clona Progetto](#clona-progetto)
5. [Configurazione](#configurazione)
6. [Build e Test](#build-e-test)
7. [Deployment Produzione](#deployment-produzione)

---

## 📦 Requisiti Sistema

### Hardware Minimo
| Componente | Requisito |
|------------|-----------|
| CPU | Intel Core i5 o equivalente |
| RAM | 8 GB (16 GB consigliati) |
| Storage | 50 GB SSD |
| Rete | Gigabit Ethernet |

### Software Richiesto
| Software | Versione | Link |
|----------|----------|------|
| Windows | 10/11 o Server 2019+ | - |
| .NET SDK | 8.0.x | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| SQL Server | Express 2019+ | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) |
| Git | 2.x | [Download](https://git-scm.com/) |

### Software Opzionale
| Software | Uso |
|----------|-----|
| SQL Server Management Studio | Gestione database |
| Visual Studio 2022 | Sviluppo (o VS Code) |
| TIA Portal | Programmazione PLC Siemens |

---

## 🛠️ Installazione Software

### 1. Installa .NET 8 SDK

```powershell
# Verifica installazione
dotnet --version
# Output: 8.0.x
```

Se non installato: [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)

---

### 2. Installa SQL Server Express

```powershell
# Download SQL Server 2022 Express
# https://www.microsoft.com/sql-server/sql-server-downloads

# Durante installazione:
# - Seleziona "Basic" o "Custom"
# - Nome istanza: SQLEXPRESS01
# - Abilita "Mixed Mode Authentication"
# - Imposta password SA
```

**Verifica installazione**:
```powershell
sqlcmd -S localhost\SQLEXPRESS01 -Q "SELECT @@VERSION"
```

---

### 3. Installa Git

```powershell
# Verifica installazione
git --version
# Output: git version 2.x.x
```

---

## 🗄️ Setup Database

### 1. Crea Database MES

```sql
-- Apri SQL Server Management Studio o usa sqlcmd

CREATE DATABASE MESManager;
GO

USE MESManager;
GO
```

---

### 2. Crea Utente Applicazione

```sql
-- Crea login SQL
CREATE LOGIN FAB WITH PASSWORD = 'password.123';
GO

-- Crea user nel database
USE MESManager;
CREATE USER FAB FOR LOGIN FAB;
GO

-- Assegna permessi
ALTER ROLE db_owner ADD MEMBER FAB;
GO
```

---

### 3. Verifica Connection String

```powershell
# Test connessione
sqlcmd -S localhost\SQLEXPRESS01 -U FAB -P password.123 -d MESManager -Q "SELECT DB_NAME()"

# Output:
# MESManager
```

---

## 📥 Clona Progetto

### 1. Clona Repository

```powershell
cd C:\Dev

# Clona (se hai accesso Git)
git clone <URL_REPOSITORY> MESManager

# Oppure copia file da backup/USB
```

---

### 2. Verifica Struttura

```powershell
cd C:\Dev\MESManager
dir

# Dovresti vedere:
# - MESManager.sln
# - MESManager.Domain/
# - MESManager.Application/
# - MESManager.Infrastructure/
# - MESManager.Web/
# - MESManager.Worker/
# - MESManager.PlcSync/
# - docs/
```

---

## ⚙️ Configurazione

### 1. Crea File Database Config

```powershell
cd C:\Dev\MESManager

# Copia template
Copy-Item "appsettings.Database.json.template" "appsettings.Database.json"
```

Modifica `appsettings.Database.json`:
```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager;User Id=FAB;Password=password.123;TrustServerCertificate=True;"
  }
}
```

---

### 2. Crea File Secrets (Opzionale)

```powershell
# Copia template
Copy-Item "appsettings.Secrets.json.template" "appsettings.Secrets.json"
```

Modifica `appsettings.Secrets.json` (solo se hai credenziali aggiuntive).

---

### 3. Restore Pacchetti NuGet

```powershell
cd C:\Dev\MESManager

# Restore tutti i pacchetti
dotnet restore MESManager.sln
```

---

## 🏗️ Build e Test

### 1. Build Progetto

```powershell
cd C:\Dev\MESManager

# Build Release
dotnet build MESManager.sln -c Release --nologo

# Verifica: 0 Error(s)
```

---

### 2. Applica Migrazioni Database

```powershell
cd C:\Dev\MESManager\MESManager.Infrastructure

# Applica tutte le migrazioni
dotnet ef database update --startup-project ../MESManager.Web
```

**Verifica tabelle create**:
```sql
USE MESManager;
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
```

---

### 3. Test Applicazione Locale

```powershell
cd C:\Dev\MESManager\MESManager.Web

# Avvia applicazione
dotnet run --environment Development
```

**Output atteso**:
```
Now listening on: http://localhost:5156
Application started. Press Ctrl+C to shut down.
```

**Test browser**: http://localhost:5156

---

### 4. Crea Utente Admin (Primo Accesso)

```sql
-- Inserisci utente admin di default (password: Admin123!)
USE MESManager;

-- Script da creare (o usa interfaccia web dopo login)
```

---

## 🚀 Deployment Produzione

### 1. Publish Applicazione

```powershell
cd C:\Dev\MESManager

# Publish Web
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Publish Worker
dotnet publish MESManager.Worker/MESManager.Worker.csproj -c Release -o publish/Worker --nologo

# Publish PlcSync
dotnet publish MESManager.PlcSync/MESManager.PlcSync.csproj -c Release -o publish/PlcSync --nologo
```

---

### 2. Copia File su Server

**Path server**: `C:\MESManager\`

```powershell
# Copia Web
robocopy "C:\Dev\MESManager\publish\Web" "C:\MESManager" /E

# Copia Worker
robocopy "C:\Dev\MESManager\publish\Worker" "C:\MESManager\Worker" /E

# Copia PlcSync
robocopy "C:\Dev\MESManager\publish\PlcSync" "C:\MESManager\PlcSync" /E
```

---

### 3. Configura Connection String Produzione

**File**: `C:\MESManager\appsettings.Database.json`

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True;",
    "MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;",
    "GanttDb": "Server=localhost\\SQLEXPRESS01;Database=Gantt;User Id=fab;Password=fabpwd;TrustServerCertificate=True;"
  }
}
```

---

### 4. Crea Task Scheduler (Avvio Automatico)

#### Task Web

```powershell
# Crea task "StartMESWeb"
schtasks /Create /TN "StartMESWeb" /TR "C:\MESManager\MESManager.Web.exe" /SC ONSTART /RU SYSTEM /RL HIGHEST
```

#### Task Worker

```powershell
# Crea task "StartMESWorker"
schtasks /Create /TN "StartMESWorker" /TR "C:\MESManager\Worker\MESManager.Worker.exe" /SC ONSTART /RU SYSTEM /RL HIGHEST
```

#### Task PlcSync

```powershell
# Crea task "StartMESPlcSync"
schtasks /Create /TN "StartMESPlcSync" /TR "C:\MESManager\PlcSync\MESManager.PlcSync.exe" /SC ONSTART /RU SYSTEM /RL HIGHEST
```

---

### 5. Avvia Servizi (Ordine Importante!)

```powershell
# 1. Web
schtasks /Run /TN "StartMESWeb"
Start-Sleep 5

# 2. Worker
schtasks /Run /TN "StartMESWorker"
Start-Sleep 3

# 3. PlcSync
schtasks /Run /TN "StartMESPlcSync"
```

---

### 6. Verifica Servizi Attivi

```powershell
# Lista processi
tasklist | findstr MESManager

# Output atteso:
# MESManager.Web.exe
# MESManager.Worker.exe
# MESManager.PlcSync.exe
```

---

## 🔧 Integrazione PLC

### 1. Configura Macchine nel Database

```sql
-- Inserisci macchine
INSERT INTO Macchine (Id, Codice, Nome, IndirizzoPLC, AttivaInGantt, OrdineVisualizazione)
VALUES
    (NEWID(), 'M001', '01', '192.168.17.21', 1, 1),
    (NEWID(), 'M002', '02', '192.168.17.26', 1, 2),
    (NEWID(), 'M003', '03', '192.168.17.24', 1, 3);
```

---

### 2. Crea File Configurazione JSON

**Path**: `C:\MESManager\PlcSync\Configuration\machines\macchina_002.json`

```json
{
  "MachineId": "11111111-1111-1111-1111-111111111102",
  "MachineCode": "M002",
  "PlcIp": "192.168.17.26",
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

**Ripeti per ogni macchina** (M003, M005, etc.).

---

### 3. Test Connessione PLC

```powershell
# Ping PLC
ping 192.168.17.26

# Se risponde, PlcSync dovrebbe connettersi automaticamente
```

**Verifica log**: `C:\MESManager\PlcSync\logs\`

---

## 🔌 Integrazione ERP Mago

### 1. Configura Connection String Mago

In `appsettings.Database.json`:
```json
"MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;"
```

---

### 2. Test Connessione Mago

```powershell
sqlcmd -S 192.168.1.72\SQLEXPRESS01 -U Gantt -P Gantt2019 -d TODESCATO_NET -Q "SELECT TOP 1 * FROM MA_SaleOrd"
```

---

### 3. Avvia Worker Sync

```powershell
# Verifica che Worker sia in esecuzione
tasklist | findstr MESManager.Worker
```

Il Worker sincronizzerà automaticamente ogni 5 minuti.

---

## ✅ Checklist Setup Completo

- [ ] .NET 8 SDK installato
- [ ] SQL Server Express installato (istanza SQLEXPRESS01)
- [ ] Database MESManager creato
- [ ] User FAB creato con permessi
- [ ] Progetto clonato in `C:\Dev\MESManager`
- [ ] File `appsettings.Database.json` configurato
- [ ] Build completato senza errori
- [ ] Migrazioni database applicate
- [ ] Test locale funzionante (http://localhost:5156)
- [ ] Publish eseguito per Web/Worker/PlcSync
- [ ] File copiati su server in `C:\MESManager`
- [ ] Connection string produzione configurata
- [ ] Task Scheduler creati (Web, Worker, PlcSync)
- [ ] Servizi avviati e funzionanti
- [ ] Macchine configurate nel database
- [ ] File JSON PLC creati
- [ ] Connessione PLC testata
- [ ] Connection string Mago configurata
- [ ] Sync Mago funzionante

---

## 🐛 Troubleshooting Setup

### ❌ Build fallisce con "SDK not found"

**Soluzione**:
```powershell
# Verifica SDK installato
dotnet --list-sdks

# Se mancante, scarica .NET 8 SDK
```

---

### ❌ "Cannot connect to SQL Server"

**Soluzione**:
```powershell
# Verifica servizio SQL Server attivo
Get-Service -Name "MSSQL$SQLEXPRESS01"

# Se stopped, avvia
Start-Service -Name "MSSQL$SQLEXPRESS01"
```

---

### ❌ Migration fallisce con errori

**Soluzione**:
```powershell
# Verifica connection string in appsettings.Database.json
# Testa connessione manualmente con sqlcmd

# Rimuovi migration se necessario
dotnet ef migrations remove --startup-project ../MESManager.Web

# Ricrea
dotnet ef migrations add Initial --startup-project ../MESManager.Web
dotnet ef database update --startup-project ../MESManager.Web
```

---

### ❌ Applicazione web non risponde

**Soluzione**:
```powershell
# Verifica porta 5156 libera
netstat -ano | findstr :5156

# Verifica log in C:\MESManager\logs\

# Riavvia servizio
taskkill /IM MESManager.Web.exe /F
schtasks /Run /TN "StartMESWeb"
```

---

## 📚 Riferimenti

- Deploy: [01-DEPLOY.md](01-DEPLOY.md)
- Configurazione: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)
- Architettura: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)
- PLC Sync: [07-PLC-SYNC.md](07-PLC-SYNC.md)
