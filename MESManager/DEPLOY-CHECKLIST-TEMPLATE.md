# ✅ Checklist Pre-Deploy MESManager

**Data Deploy:** ________________  
**Versione Precedente:** ________  
**Versione Nuova:** ________  
**Persona che fa deploy:** ________________________

---

## 📋 Prima di Iniziare

- [ ] Ho letto **DEPLOY-GUIDA-DEFINITIVA.md**
- [ ] Ho credenziali server: `Administrator / A123456!`
- [ ] Ho credenziali DB: `FAB / password.123`
- [ ] Terminal aperto in: `C:\Dev\MESManager`
- [ ] Nessun altro sta modificando il codice contemporaneamente

---

## 🔧 Preparazione (10 minuti)

- [ ] **Versione incrementata** in `MESManager.Web/Components/Layout/MainLayout.razor` (linea ~126)
  ```html
  v1.12  <!-- Cambia il numero -->
  ```
  
- [ ] **CHANGELOG.md aggiornato** con:
  - [ ] Titolo versione
  - [ ] Data deploy
  - [ ] Cosa è cambiato
  - [ ] File modificati

- [ ] **Git commit** (se usi versionamento)
  ```powershell
  git add .
  git commit -m "Release v1.12: [descrizione breve modifiche]"
  ```

- [ ] **Notifiche inviate ai team** che arriverà downtime

---

## 🔨 Build Locale (5 minuti)

```powershell
# STEP 1: Build
Write-Host "🔨 Building..." -ForegroundColor Cyan
dotnet build MESManager.sln -c Release --nologo
```

- [ ] Build completato **SENZA ERRORI** (exit code 0)
- [ ] Controllato output per warning importanti

```powershell
# STEP 2: Publish
Write-Host "📦 Publishing..." -ForegroundColor Cyan
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo
```

- [ ] Publish completato **SENZA ERRORI** (exit code 0)
- [ ] File exe verificato esiste:
  ```powershell
  Get-Item "C:\Dev\MESManager\publish\Web\MESManager.Web.exe"
  ```

---

## 🛑 Stop Applicazione (2 minuti)

```powershell
# STEP 3: Stop
Write-Host "🛑 Stopping..." -ForegroundColor Yellow
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F
Start-Sleep 2
```

- [ ] Processo terminato (nessun output di errore)
- [ ] Verificato è fermo:
  ```powershell
  tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager.Web
  # Output: (nulla) = OK
  ```

---

## 📋 Copia File (3 minuti)

**⚠️ CRITICO: Verificare PERCORSO e ESCLUSIONI**

```powershell
# STEP 4: Copy (ESCLUSIONI OBBLIGATORIE!)
Write-Host "📋 Copying..." -ForegroundColor Cyan
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups
```

- [ ] **VERIFICHE CRITICHE:**
  - [ ] Destinazione: `\\192.168.1.230\c$\MESManager` (ROOT, non Web\wwwroot\)
  - [ ] File esclusi: `appsettings.Secrets.json`, `appsettings.Database.json`
  - [ ] Exit code: 0-7 (OK), >7 (ERRORE)

- [ ] Verificato file copiato:
  ```powershell
  Get-Item "\\192.168.1.230\c$\MESManager\MESManager.Web.exe"
  ```

---

## 🚀 Riavvia Applicazione (3 minuti)

```powershell
# STEP 5: Start
Write-Host "🚀 Starting..." -ForegroundColor Green
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
Start-Sleep 6
```

- [ ] Task eseguito con successo (no errori)
- [ ] Atteso 6 secondi per startup

```powershell
# STEP 6: Verify
tasklist /S 192.168.1.230 /U Administrator /P "A123456!" | findstr MESManager.Web
```

- [ ] Output mostra `MESManager.Web.exe` con memoria allocata (es. 173 KB)
- [ ] Applicazione è in esecuzione

```powershell
# STEP 7: Test connessione
Test-NetConnection -ComputerName 192.168.1.230 -Port 5156
```

- [ ] `TcpTestSucceeded: True` = OK
- [ ] Applicazione risponde sulla porta 5156

---

## 🌐 Verifica Web

- [ ] Aperto browser: `http://192.168.1.230:5156`
- [ ] Applicazione carica normalmente (no errori)
- [ ] **Versione nuova visibile** in basso a destra (es. `v1.12`)
- [ ] Pagina principale funziona (es. clicca menu)

---

## 📡 Comunicazioni Utenti

```
🔔 Notifica da mandare ai client:

"Aggiornamento MESManager disponibile!

Versione: v1.X
Data: [data]

Azioni necessarie:
1. Aggiorna il browser: Ctrl+Shift+R (Windows) o Cmd+Shift+R (Mac)
2. Se cache persistente: Cancella cookie e localStorage (F12 → Applicazione)
3. Riaccedi alla piattaforma

Novità:
- [Elencare principali modifiche]

Downtime previsto: ~10 minuti (già completato)
Contatta IT se problemi"
```

- [ ] Notifica mandata a: ________________________
- [ ] Assistenza IT informata
- [ ] Utenti sanno che versione è cambiata

---

## ✅ Post-Deploy (15 minuti dopo)

- [ ] Visitato `http://192.168.1.230:5156` da client diverso
- [ ] Numero versione corretto in basso a destra
- [ ] Navigato almeno 3 pagine (nessun errore)
- [ ] Testato almeno una funzione critica (es. Commesse, Programma)
- [ ] Verificato database è connesso (niente errori SQL)
- [ ] Log server controllati (no errori critici)
  ```powershell
  Get-Content "\\192.168.1.230\c$\MESManager\powershell.log" -Tail 20
  ```

---

## 🔴 Se Qualcosa va Male

**STOP IMMEDIATO!** Non continuare.

### Errore: "File non trovato"
```powershell
# Verifica che publish esista
Get-Item "C:\Dev\MESManager\publish\Web\MESManager.Web.exe"
```

### Errore: "Bad Request - Invalid Hostname"
- [ ] Verificato `appsettings.json` contiene: `http://0.0.0.0:5156`
- [ ] Riavviato app

### Errore: "Database connection failed"
- [ ] Verificato `appsettings.Database.json` NON è stato sovrascritto
- [ ] Ricreato manualmente se necessario

### App non risponde
```powershell
# Force stop + restart
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F /T
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
```

---

## 📊 Finale

**Deploy Completato:** [ ] SÌ  [ ] NO (Descrivere problema)

**Note/Osservazioni:**
_____________________________________________________________________________
_____________________________________________________________________________
_____________________________________________________________________________

**Firma:** ________________________  **Data/Ora:** ________________________

---

## 🔗 Referenze

- **Guida Completa:** [DEPLOY-GUIDA-DEFINITIVA.md](docs/DEPLOY-GUIDA-DEFINITIVA.md)
- **Changelog:** [CHANGELOG.md](docs/CHANGELOG.md)
- **Credenziali:** `.credentials.local` (nella root del progetto)
