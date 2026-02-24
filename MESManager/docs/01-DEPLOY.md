# 01 - Deploy MESManager

> **Scopo**: Guida unica per deploy senza errori su server di produzione

---

## 🔐 Credenziali

### Server Windows
```
Host:     192.168.1.230
User:     Administrator
Password: A123456!
```

### Database SQL (Produzione)
```
Server:   192.168.1.230\SQLEXPRESS01
Database: MESManager_Prod
User:     FAB
Password: password.123
```

---

## 🏗️ Architettura Server - CRITICA!

```
C:\MESManager\                          ← ROOT
├── MESManager.Web.exe                 ← ✅ APP PRINCIPALE
├── wwwroot\                           ← ✅ FILE STATICI (JS/CSS)
│   ├── lib\ag-grid\
│   ├── css\
│   └── js\
├── appsettings.json                   ← Config base
├── appsettings.Production.json        ← Config produzione
├── appsettings.Database.json          ← 🔒 NON COPIARE MAI
├── appsettings.Secrets.json           ← 🔒 NON COPIARE MAI
├── Worker\                            ← Servizio Sync Mago
└── PlcSync\                           ← Servizio PLC
```

**⚠️ ERRORE COMUNE**: Copiare in `C:\MESManager\Web\wwwroot\` invece di `C:\MESManager\wwwroot\`

**REGOLA D'ORO**:
- ✅ Copia in: `C:\MESManager\wwwroot\` (ROOT)
- ❌ NON copiare: `appsettings.Secrets.json`, `appsettings.Database.json`

---

## 🚀 Deploy Completo (Step-by-Step)

### STEP 0: Pre-requisiti

1. **Incrementa versione** in `MESManager.Web/Components/Layout/MainLayout.razor`:
```razor
<div style="position: fixed; bottom: 10px; right: 15px;">
    v1.24  <!-- Incrementa: v1.23 → v1.24 -->
</div>
```

2. **Aggiorna CHANGELOG.md** con le modifiche

3. **Commit tutto in Git**

---

### STEP 1: Build Locale

```powershell
cd C:\Dev\MESManager

# Compila in Release
dotnet build MESManager.sln -c Release --nologo

# Verifica: 0 errori
```

---

### STEP 2: Publish Applicazione

```powershell
# Pubblica Web (principale)
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Pubblica Worker (solo se modificato)
dotnet publish MESManager.Worker/MESManager.Worker.csproj -c Release -o publish/Worker --nologo

# Pubblica PlcSync (solo se modificato)
dotnet publish MESManager.PlcSync/MESManager.PlcSync.csproj -c Release -o publish/PlcSync --nologo
```

---

### STEP 2.5: Backup Preferenze Utente (⭐ CRITICO)

**⚠️ OBBLIGATORIO** se il deploy modifica:
- Nomi campi nelle grid (es: `ClienteRagioneSociale` → `CompanyName`)
- Struttura colonne (nuove colonne, rimosse, riordinate)
- DTO che cambiano property usate nelle grid

```powershell
cd C:\Dev\MESManager\scripts

# Backup automatico delle preferenze utente
.\backup-preferenze-utente.ps1

# Output: backup-preferenze\preferenze_YYYYMMDD_HHmmss.json
```

**Risultato atteso**:
```
✅ Backup completato!
Record salvati: 45
File: C:\Dev\MESManager\scripts\backup-preferenze\preferenze_20260211_085500.json

Preferenze per utente:
  - admin: 12 preferenze
  - operatore1: 8 preferenze
  ...
```

**Quando NON serve**:
- Deploy solo fix logica backend (senza cambi UI)
- Deploy solo nuove pagine (senza modificare grid esistenti)

---

### STEP 3: Ferma Servizi su Server

**Ordine CRITICO**: PlcSync → Worker → Web

```powershell
# 1. Ferma PlcSync (per liberare connessioni PLC)
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.PlcSync.exe /F

# 2. Ferma Worker
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Worker.exe /F

# 3. Ferma Web
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Attendi 3 secondi
Start-Sleep 3
```

---

### STEP 4: Copia File

```powershell
# Copia Web ESCLUDENDO file critici e sottocartelle Worker/PlcSync
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" `
    /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb `
    /XD logs SyncBackups Worker PlcSync

# Copia Worker (solo se modificato in questa versione)
robocopy "C:\Dev\MESManager\publish\Worker" "\\192.168.1.230\c$\MESManager\Worker" `
    /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb `
    /XD logs SyncBackups

# Copia PlcSync (solo se modificato in questa versione)
robocopy "C:\Dev\MESManager\publish\PlcSync" "\\192.168.1.230\c$\MESManager\PlcSync" `
    /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb `
    /XD logs

# Verifica exit code
# 0-7 = OK, >7 = errore
echo "Exit Code: $LASTEXITCODE (0-7 = OK)"
```

**File esclusi**:
- `appsettings.Secrets.json` → Password produzione
- `appsettings.Database.json` → Connection string produzione
- `*.log`, `*.pdb` → Debug e log inutili

**Cartelle escluse da Web**:
- `Worker\`, `PlcSync\` → Copiate separatamente per non sovrascrivere

---

### STEP 5: Riavvia Servizi

**⚠️ NOTA**: Sul server attuale, Worker e PlcSync si avviano automaticamente (servizi/task schedulati al boot).
È sufficiente avviare Web Application.

```powershell
# 1. Avvia Web Application
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
Start-Sleep 5

# 2. Verifica che tutti i servizi siano attivi
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager

# Output atteso:
# MESManager.Web.exe
# MESManager.Worker.exe  
# MESManager.PlcSync.exe
```

**Se Worker o PlcSync NON sono attivi** (caso raro):
```powershell
# Avvia manualmente da percorso remoto
Invoke-Command -ComputerName 192.168.1.230 -Credential (Get-Credential) -ScriptBlock {
    Start-Process "C:\MESManager\Worker\MESManager.Worker.exe" -WindowStyle Hidden
    Start-Sleep 2
    Start-Process "C:\MESManager\PlcSync\MESManager.PlcSync.exe" -WindowStyle Hidden
}
```

---

### STEP 6: Verifica

1. **Browser**: http://192.168.1.230:5156
2. **Versione**: Controlla numero versione in basso a destra
3. **Test login** con utente
4. **Log**: Verifica `C:\MESManager\logs\` per errori

---

### STEP 7: Restore Preferenze Utente (se necessario)

**Usa SOLO se**:
- Gli utenti segnalano che le colonne delle grid sono resettate
- Vuoi ripristinare configurazioni pre-deploy

#### Caso A: Deploy NON ha cambiato colonne (ripristino COMPLETO)

```powershell
cd C:\Dev\MESManager\scripts

# Restore completo (include stati grid)
.\restore-preferenze-utente.ps1
```

#### Caso B: Deploy HA CAMBIATO colonne (ripristino PARZIALE)

Se hai modificato nomi campi o struttura colonne (es: `ClienteRagioneSociale` → `CompanyName`):

```powershell
cd C:\Dev\MESManager\scripts

# Restore parziale - SALTA stati grid incompatibili
.\restore-preferenze-utente.ps1 -SkipGridStates
```

**Risultato con -SkipGridStates**:
- ✅ Ripristina: preferenze UI (font size, density, zebra stripes, ecc.)
- ❌ **NON** ripristina: ordine colonne, larghezze, visibilità (incompatibili)
- ⚠️ Gli utenti dovranno:
  1. Riordinare le colonne come preferivano
  2. Cliccare "Fix" per salvare il nuovo layout

**Verifica**:
```powershell
# Output atteso:
Ripristinate: 22
Saltate (grid states): 12
```

**Alternative**:
- NON fare restore → utenti riconfigurano tutto da zero
- Fare restore completo → rischio errori JS se JSON incompatibile

---

## ⚡ Deploy Solo JavaScript (Rapido)

Per modifiche solo JS/CSS **senza** recompile C#:

```powershell
# 1. Ferma solo Web
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# 2. Copia solo wwwroot
robocopy "C:\Dev\MESManager\MESManager.Web\wwwroot" "\\192.168.1.230\c$\MESManager\wwwroot" /E

# 3. Riavvia Web
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"

# 4. IMPORTANTE: Cache browser
# Comunicare agli utenti: Ctrl+Shift+R per ricaricare
```

---

## 🐛 Errori Comuni e Soluzioni

### 1. "File wwwroot non trovati" dopo deploy

**Causa**: Copiato nella cartella sbagliata

**Soluzione**:
```powershell
# Verifica path corretto
dir \\192.168.1.230\c$\MESManager\wwwroot\

# Se vuoto, ri-copia
robocopy "C:\Dev\MESManager\publish\Web\wwwroot" "\\192.168.1.230\c$\MESManager\wwwroot" /E
```

---

### 2. "Connection string non valida"

**Causa**: Copiato `appsettings.Database.json` locale sul server

**Soluzione**:
```powershell
# Ripristina file originale dal backup
# Oppure modifica manualmente:
# Server=localhost\SQLEXPRESS01 (non 192.168.1.230)
# Database=MESManager_Prod (non MESManager)
```

---

### 3. "PlcSync non si connette ai PLC"

**Causa**: Terminazione brusca ha lasciato connessioni aperte

**Soluzione**:
```powershell
# Attendi 2-3 minuti per timeout
Start-Sleep 180

# Oppure riavvia PLC (se possibile)
```

---

### 4. "Modifiche JavaScript non visibili"

**Causa**: Cache browser

**Soluzione**:
- Ctrl+Shift+R (hard refresh)
- Oppure incrementa versione CSS in MainLayout.razor:
```razor
<link href="css/bootstrap.css?v=1.24" rel="stylesheet" />
```

---

### 5. "Applicazione lenta dopo deploy"

**Causa**: File .pdb copiati (debug symbols)

**Soluzione**:
```powershell
# Rimuovi file pdb
Remove-Item \\192.168.1.230\c$\MESManager\*.pdb -Recurse
```

---

### 6. "Exit code robocopy = 16"

**Causa**: Errore copia (file bloccato o path errato)

**Soluzione**:
```powershell
# Verifica che servizi siano fermati
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager

# Se ancora attivi, forza terminazione
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /F /IM MESManager.*
```

---

## ✅ Checklist Pre-Deploy

- [ ] Versione incrementata in MainLayout.razor
- [ ] CHANGELOG.md aggiornato
- [ ] Build locale completato senza errori
- [ ] Commit Git eseguito
- [ ] Backup configurazione server (opzionale)
- [ ] Utenti avvisati del deploy (se necessario)

---

## 📝 Script Rapido Copy-Paste

```powershell
# DEPLOY COMPLETO - COPIA TUTTO

cd C:\Dev\MESManager

# Build & Publish
dotnet build MESManager.sln -c Release --nologo
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo
dotnet publish MESManager.Worker/MESManager.Worker.csproj -c Release -o publish/Worker --nologo
dotnet publish MESManager.PlcSync/MESManager.PlcSync.csproj -c Release -o publish/PlcSync --nologo

# Stop servizi (ordine: PlcSync → Worker → Web)
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.PlcSync.exe /F
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Worker.exe /F
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F
Start-Sleep 3

# Copia file (escludi sottocartelle Worker/PlcSync da Web per non sovrascrivere)
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups Worker PlcSync /R:2 /W:5
robocopy "C:\Dev\MESManager\publish\Worker" "\\192.168.1.230\c$\MESManager\Worker" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups /R:2 /W:5
robocopy "C:\Dev\MESManager\publish\PlcSync" "\\192.168.1.230\c$\MESManager\PlcSync" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs /R:2 /W:5

# Start servizi (ordine: Web → Worker → PlcSync)
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
Start-Sleep 5
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWorker"
Start-Sleep 3
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESPlcSync"

# Verifica
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager
Write-Host "Deploy completato! Verifica: http://192.168.1.230:5156" -ForegroundColor Green
```

---

## 🔄 Workflow AI Assistant

Quando l'utente dice "pubblica" o "deploy", l'AI deve:

1. **Pre-controlli**:
   - Verificare build: `dotnet build --nologo`
   - Leggere versione attuale da MainLayout.razor
   
2. **Consolidamento**:
   - Incrementare versione
   - Aggiornare CHANGELOG.md
   
3. **Build**:
   - Build Release
   - Publish progetto Web
   
4. **Deploy**:
   - Mostrare comandi step-by-step
   - Evidenziare ordine stop/start servizi
   - Ricordare file da NON copiare
   
5. **Post-deploy**:
   - Chiedere conferma utente
   - Verificare versione online

**File reference**: Questo workflow sostituisce `WORKFLOW-PUBBLICAZIONE.md`
