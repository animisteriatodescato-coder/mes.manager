# 📘 GUIDA DEPLOY MESManager - Versione Definitiva

> **Documento unico di riferimento per deploy senza errori**  
> Versione: 1.0 | Data: 29 Gennaio 2026  
> **LEGGI QUESTA PRIMA DI FARE QUALSIASI DEPLOY**

---

## 🎯 Indice Rapido

- [Credenziali](#credenziali)
- [Architettura Server](#architettura-server)
- [Deploy Completo (Consigliato)](#deploy-completo)
- [Deploy Solo JavaScript](#deploy-solo-javascript)
- [Errori Comuni e Soluzioni](#errori-comuni)

---

## 🔐 Credenziali

### Admin Server (Windows)
```
Hostname: 192.168.1.230
Username: Administrator
Password: A123456!
```

### Database SQL (Produzione)
```
Server:   192.168.1.230\SQLEXPRESS01
Database: MESManager_Prod
Username: FAB
Password: password.123
```

### Database Locale (Sviluppo)
```
Server:   localhost\SQLEXPRESS01
Database: MESManager
Auth:     Windows Authentication
```

---

## 🏗️ Architettura Server - CRITICA!

**⚠️ ERRORE COMUNE: Copiare file nella cartella sbagliata!**

```
C:\MESManager\                          ← ROOT
├── MESManager.Web.exe                 ← ✅ APP IN ESECUZIONE (Copia qui!)
├── wwwroot\                           ← ✅ FILE STATICI (JS/CSS - Copia qui!)
│   ├── lib\ag-grid\                   ← File JS griglie
│   ├── css\                           ← Stylesheet
│   └── ...
├── appsettings.json                   ← ⚠️ NON COPIARE
├── appsettings.Production.json        ← ⚠️ NON COPIARE
├── appsettings.Database.json          ← 🔒 CRITICO - NON COPIARE MAI
├── appsettings.Secrets.json           ← 🔒 CRITICO - NON COPIARE MAI
├── Worker\                            ← Servizio Mago (appsettings.Database.json protetto)
├── PlcSync\                           ← Servizio PLC (appsettings.Database.json protetto)
└── Web\                               ← ❌ BACKUP VECCHIO - NON USATO
    └── wwwroot\                       ← ❌ NON COPIARE QUI!
```

**REGOLA D'ORO:**
- ✅ Copia in: `C:\MESManager\wwwroot\` (ROOT)
- ❌ NON copiare in: `C:\MESManager\Web\wwwroot\`
- ❌ NON copiare MAIL: `appsettings.Secrets.json`, `appsettings.Database.json`

---

## 🚀 Deploy Completo (Passo Dopo Passo)

### PREREQUISITO: Prima di iniziare

1. **Incrementa versione** in `MESManager.Web/Components/Layout/MainLayout.razor` (linea ~126):
```html
<div style="position: fixed; bottom: 10px; right: 15px; font-size: 11px; color: #999; font-family: monospace;">
    v1.12  <!-- Cambia v1.11 → v1.12, v1.12 → v1.13, ecc. -->
</div>
```

2. **Aggiorna CHANGELOG.md** con cosa è cambiato

3. **Salva tutto in Git** (se usi versionamento)

### STEP 1: Build Locale

Eseguire da: `C:\Dev\MESManager`

```powershell
# Compila tutto in Release
dotnet build MESManager.sln -c Release --nologo

# Se ci sono errori, STOP! Non continuare finché non sono risolti.
```

### STEP 2: Publish Applicazione

```powershell
# Pubblica solo il progetto Web (i servizi non cambiano di solito)
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Verifica che il file exe sia stato creato
Get-Item "C:\Dev\MESManager\publish\Web\MESManager.Web.exe"
```

### STEP 3: Ferma Applicazione su Server

```powershell
# Ferma il processo Web
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Attendi che il processo termini completamente
Start-Sleep 2

# Verifica che sia fermo
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager.Web
# Output: (nulla) = OK
```

### STEP 4: Copia File (ESCLUSIONI CRITICHE!)

```powershell
# Robocopy copia ricorsivamente escludendo file critici
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups

# IMPORTANTE: robocopy restituisce exit code 0-7 = OK, >7 = errore
echo "Exit Code: $LASTEXITCODE"
```

**File esclusi (per motivo):**
- `appsettings.Secrets.json` → Contiene password di produzione
- `appsettings.Database.json` → Connection string con credenziali
- `*.log` → Log di debug
- `*.pdb` → Debug symbols (inutili)
- `logs\` → Directory log (non serve copiarla)
- `SyncBackups\` → Backup locali

### STEP 5: Riavvia Applicazione

```powershell
# Esegui lo script di start dal task scheduler
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"

# Attendi avvio
Start-Sleep 6

# Verifica che sia partita
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager.Web
# Output: MESManager.Web.exe     8360                            2    173.768 K = OK
```

### STEP 6: Verifica Connettività

```powershell
# Test porta
Test-NetConnection -ComputerName 192.168.1.230 -Port 5156 -WarningAction SilentlyContinue | Select-Object TcpTestSucceeded
# Output: True = OK

# Test URL
Invoke-WebRequest -Uri "http://192.168.1.230:5156" -UseBasicParsing -TimeoutSec 10
# Se vedi HTML, OK
```

### STEP 7: Comunica agli Utenti

```
Versione aggiornata: v1.X.X
Azioni necessarie per gli utenti:
1. Ctrl+Shift+R nel browser per svuotare cache
2. Se ancora non funziona: Cancella cookie e localStorage
```

---

## 🔧 Deploy Solo JavaScript (Senza Recompile)

**Quando usare:** Solo file JS/CSS modificati, niente C# o database

```powershell
# Copia il singolo file
Copy-Item "C:\Dev\MESManager\MESManager.Web\wwwroot\lib\ag-grid\commesse-aperte-grid.js" "\\192.168.1.230\c$\MESManager\wwwroot\lib\ag-grid\" -Force

# Oppure copia tutti i JS delle griglie
Copy-Item "C:\Dev\MESManager\MESManager.Web\wwwroot\lib\ag-grid\*.js" "\\192.168.1.230\c$\MESManager\wwwroot\lib\ag-grid\" -Force

# Comunica agli utenti: "Fai Ctrl+Shift+R"
```

**NON serve riavviare l'app per JS/CSS!**

---

## 🔄 Deploy SQL (Database Changes)

Se hai uno script SQL (es. nuove colonne, migrazioni):

```powershell
# Esegui script su database di produzione
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" -U "FAB" -P "password.123" -i "C:\Dev\MESManager\scripts\nome-script.sql" -C

# Se l'output è vuoto, significa è andato bene
```

**Fai SEMPRE il backup prima:**
```powershell
# Backup manuale (opzionale ma consigliato)
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -U "FAB" -P "password.123" -Q "BACKUP DATABASE MESManager_Prod TO DISK = 'C:\Backup\MESManager_Prod_$(Get-Date -Format yyyyMMdd_HHmmss).bak'"
```

---

## 📌 Versionamento

Ogni deploy ha una versione numerica per tracciare cosa è aggiornato.

### Formato: `vX.Y`
- `X` = Major (cambio architettura/database schema raro)
- `Y` = Minor (incrementa ad ogni deploy)

### Esempio progressione:
- v1.11 → v1.12 → v1.13 → v1.20 → v2.0

### Dove appare:
- Angolo basso-destra di ogni pagina
- Nel CHANGELOG.md
- Negli alert ai clienti

### Come aggiornarla:
1. Modifica `MESManager.Web/Components/Layout/MainLayout.razor`
2. Compila
3. Deploya
4. Aggiorna CHANGELOG.md

---

## ❌ Errori Comuni e Soluzioni

### ❌ Errore 1: "File non trovato in publish"

**Sintomo:** `Exception: Cannot find file publish\Web\MESManager.Web.exe`

**Causa:** Publish fallito

**Soluzione:**
```powershell
# Pulisci e republica
Remove-Item "C:\Dev\MESManager\publish\Web" -Recurse -Force
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo
# Verificare l'output per errori di build
```

---

### ❌ Errore 2: "App non si connette al database"

**Sintomo:** `Connection string error` nel browser

**Causa:** Sovrascritto `appsettings.Database.json`

**Soluzione:**
```powershell
# NON copiare in futuro! Usa /XF appsettings.Database.json in robocopy

# Per recuperare: ripristina dal backup o ricrea manualmente
# Crea file manualmente con credenziali corrette
```

**Credenziali corrette per server:**
```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Prod;User Id=FAB;Password=password.123;TrustServerCertificate=True"
  }
}
```

---

### ❌ Errore 3: "Bad Request - Invalid Hostname"

**Sintomo:** HTTP Error 400 quando accedi a http://192.168.1.230:5156

**Causa:** App non configurata per accettare connessioni via IP

**Soluzione:** Verifica che start-all-services.cmd abbia:
```cmd
set ASPNETCORE_URLS=http://0.0.0.0:5156
```

---

### ❌ Errore 4: "File JS non si aggiorna"

**Sintomo:** Cambi il JS ma il browser mostra la versione vecchia

**Cause possibili:**
1. Copiato nella cartella sbagliata (`Web\wwwroot` invece di `wwwroot`)
2. Cache del browser non svuotata
3. App non ha servito il file nuovo

**Soluzione (in ordine):**
1. **Verifica il percorso:**
```powershell
Get-ChildItem "\\192.168.1.230\c$\MESManager\wwwroot\lib\ag-grid\commesse-aperte-grid.js"
# Deve esistere in C:\MESManager\wwwroot\ (ROOT), non in Web\wwwroot\
```

2. **Svuota cache browser:**
   - Ctrl+Shift+R (hard refresh)
   - F12 → Applicazione → Cache → Svuota tutto
   - Cancella localStorage

3. **Se ancora non funziona: riavvia app**
```powershell
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
```

---

### ❌ Errore 5: "Processi non si fermano"

**Sintomo:** `taskkill` non termina il processo

**Causa:** Processo bloccato in shutdown

**Soluzione:**
```powershell
# Forza kill più aggressivo
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F /T

# Se ancora non muore, last resort:
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.* /F /T
```

---

### ❌ Errore 6: "Database standardizzazione fallita"

**Sintomo:** Numeri macchina ancora nel formato vecchio "01" invece di "M001"

**Causa:** Script SQL non eseguito

**Soluzione:**
```powershell
# Esegui il script di standardizzazione
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" -U "FAB" -P "password.123" -i "C:\Dev\MESManager\scripts\utilities\fix-numero-macchina-standard.sql" -C

# Verifica:
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" -U "FAB" -P "password.123" -Q "SELECT TOP 5 NumeroMacchina FROM Commesse WHERE NumeroMacchina IS NOT NULL"
# Output deve mostrare: M001, M002, ecc.
```

---

## 📋 Checklist Pre-Deploy

- [ ] **Versione incrementata** in MainLayout.razor
- [ ] **CHANGELOG.md aggiornato** con modifiche
- [ ] **Build locale completato** (`dotnet build ...` con exit code 0)
- [ ] **Publish completato** e file exe verificato
- [ ] **Credenziali corrette** (Administrator / A123456!)
- [ ] **Backup eventualmente fatto** se modifiche database critiche
- [ ] **File sensibili esclusi** da copia (Secrets.json, Database.json)
- [ ] **Percorso copia corretto** (`C:\MESManager\wwwroot\`, NON Web\wwwroot\)
- [ ] **Utenti avvisati** che vedranno versione nuova in basso a destra

---

## 📞 Comando Rapido Deploy Completo

Copia e incolla da `C:\Dev\MESManager`:

```powershell
$versionNew = "1.12"  # Cambia il numero
Write-Host "🔨 Building..." -ForegroundColor Cyan
dotnet build MESManager.sln -c Release --nologo > $null 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Build failed"; exit }

Write-Host "📦 Publishing..." -ForegroundColor Cyan
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo > $null 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Publish failed"; exit }

Write-Host "🛑 Stopping..." -ForegroundColor Yellow
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F 2>$null
Start-Sleep 2

Write-Host "📋 Copying..." -ForegroundColor Cyan
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups > $null 2>&1

Write-Host "🚀 Starting..." -ForegroundColor Green
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb" 2>$null
Start-Sleep 6

$ok = tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager.Web
if ($ok) {
    Write-Host "✅ DEPLOY COMPLETATO v$versionNew!" -ForegroundColor Green
    Write-Host "   Visita: http://192.168.1.230:5156" -ForegroundColor Green
    Write-Host "   Utenti: Ctrl+Shift+R per aggiornare" -ForegroundColor Green
} else {
    Write-Host "❌ ERRORE: App non avviata" -ForegroundColor Red
}
```

---

## 📚 Documentazione Correlata

- `CHANGELOG.md` - Storico versioni
- `.credentials.local` - Credenziali di backup (non versionato)
- `scripts/utilities/fix-numero-macchina-standard.sql` - Standardizzazione database
- `MESManager.Web/Components/Layout/MainLayout.razor` - Versione app
