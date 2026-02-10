# 02 - Sviluppo Locale

> **Scopo**: Workflow standard per sviluppo e test in locale

---

## 🔄 Workflow Standard

### Prima di Iniziare

```powershell
cd C:\Dev\MESManager

# Verifica stato Git
git status

# Commit modifiche pendenti
git add .
git commit -m "descrizione modifiche"
```

---

## 📝 Flusso: Modifica → Test → Commit

### 1️⃣ Chiudi Applicazione (se in esecuzione)

```powershell
# Se dotnet run è attivo: Ctrl+C nel terminale

# Oppure termina tutti i processi dotnet
taskkill /IM dotnet.exe /F
```

---

### 2️⃣ Applica le Modifiche

Fai le modifiche ai file necessari (C#, Razor, JS, CSS, etc.)

---

### 3️⃣ Build

```powershell
cd C:\Dev\MESManager

dotnet build MESManager.sln -c Release --nologo
```

**Verifica**: Output deve mostrare **0 Error(s)**

---

### 4️⃣ Avvia Applicazione

```powershell
cd C:\Dev\MESManager\MESManager.Web

dotnet run --environment Development
```

**Output atteso**:
```
Now listening on: http://localhost:5156
Application started. Press Ctrl+C to shut down.
```

---

### 5️⃣ Test nel Browser

Apri: http://localhost:5156

**Test checklist**:
- [ ] Login funziona
- [ ] Pagina modificata appare correttamente
- [ ] Console browser senza errori (F12)
- [ ] Funzionalità testate manualmente

---

### 6️⃣ Commit Finale

```powershell
# Ctrl+C per fermare l'app

cd C:\Dev\MESManager
git add .
git commit -m "feat: descrizione della modifica"

# Push (opzionale)
git push
```

---

## 🛑 Problemi Comuni

### ❌ "Il file è bloccato da MESManager.Web"

**Causa**: Applicazione ancora in esecuzione

**Soluzione**:
```powershell
# Termina tutti i processi dotnet
taskkill /IM dotnet.exe /F

# Verifica
Get-Process -Name dotnet -ErrorAction SilentlyContinue
```

---

### ❌ "Il file di progetto non esiste"

**Causa**: Path errato

**Soluzione**:
```powershell
# Esegui dalla directory corretta
cd C:\Dev\MESManager\MESManager.Web
dotnet run --environment Development
```

---

### ❌ "Porta già in uso" (Port 5156)

**Causa**: Altra istanza attiva

**Soluzione**:
```powershell
# Trova processo sulla porta 5156
netstat -ano | findstr :5156

# Termina processo (sostituisci PID)
taskkill /PID <PID> /F
```

---

### ❌ "Connection string non valida"

**Causa**: File `appsettings.Database.json` mancante o errato

**Soluzione**:
```powershell
# Verifica esistenza file
Test-Path "C:\Dev\MESManager\appsettings.Database.json"

# Deve contenere:
# Server=localhost\SQLEXPRESS01;Database=MESManager;...
```

Vedi [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md) per dettagli.

---

### ❌ Build fallisce con errori di compilazione

**Causa**: Errori nel codice

**Soluzione**:
```powershell
# Pulisci build
dotnet clean MESManager.sln

# Rebuild
dotnet build MESManager.sln --nologo

# Leggi errori e correggi
```

---

### ❌ Modifiche JavaScript non visibili

**Causa**: Cache browser

**Soluzione**:
- **Ctrl+Shift+R** (hard refresh)
- Oppure disabilita cache in DevTools:
  - F12 → Network → Disable cache (checkbox)

---

## 🧪 Test Rapido Modifiche

### Solo Modifiche C#/Razor

```powershell
# 1. Build
dotnet build MESManager.sln -c Release --nologo

# 2. Run
cd MESManager.Web
dotnet run --environment Development

# 3. Test browser: http://localhost:5156

# 4. Stop: Ctrl+C
```

---

### Solo Modifiche JavaScript/CSS

**Non serve ricompilare C#!**

```powershell
# 1. Modifica file JS/CSS in:
#    MESManager.Web/wwwroot/js/
#    MESManager.Web/wwwroot/css/

# 2. Se app è in esecuzione, ricarica browser:
#    Ctrl+Shift+R

# 3. Se app NON è in esecuzione:
cd C:\Dev\MESManager\MESManager.Web
dotnet run --environment Development
```

---

## 📦 Test Migrazioni Database

### Creare Nuova Migration

```powershell
cd C:\Dev\MESManager\MESManager.Infrastructure

# Crea migration
dotnet ef migrations add NomeMigration --startup-project ../MESManager.Web

# Esempio:
dotnet ef migrations add AddColonnaTabellaX --startup-project ../MESManager.Web
```

---

### Applicare Migration al Database

```powershell
cd C:\Dev\MESManager\MESManager.Infrastructure

# Applica migration
dotnet ef database update --startup-project ../MESManager.Web
```

---

### Rollback Migration

```powershell
# Torna alla migration precedente
dotnet ef database update NomeMigrationPrecedente --startup-project ../MESManager.Web

# Rimuovi migration
dotnet ef migrations remove --startup-project ../MESManager.Web
```

---

## 🔍 Debug in Visual Studio

### Avvio con Debug

1. Apri `MESManager.sln` in Visual Studio
2. Imposta `MESManager.Web` come progetto di avvio (tasto destro → Set as Startup Project)
3. Premi **F5** (o Ctrl+F5 senza debug)

---

### Breakpoint

1. Clicca nel margine sinistro del codice per impostare breakpoint
2. Avvia con F5
3. Esegui azione nel browser che attiva il breakpoint
4. Visual Studio si fermerà sulla riga

**Shortcut utili**:
- **F10**: Step Over
- **F11**: Step Into
- **Shift+F11**: Step Out
- **F5**: Continue

---

## 🧰 Tool Utili

### Verifica Versioni

```powershell
# .NET SDK
dotnet --version
# Output: 8.0.x

# EF Core Tools
dotnet ef --version
# Output: 8.0.x
```

---

### Pulizia Completa

```powershell
cd C:\Dev\MESManager

# Rimuovi bin e obj
Get-ChildItem -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force

# Rebuild
dotnet build MESManager.sln -c Release --nologo
```

---

### Verifica Connection String Locale

```powershell
# Test connessione SQL Server
sqlcmd -S localhost\SQLEXPRESS01 -Q "SELECT DB_NAME()"

# Se fallisce, verifica:
# 1. SQL Server è in esecuzione
# 2. Istanza SQLEXPRESS01 esiste
# 3. Windows Authentication abilitata
```

---

## 📋 Checklist Sviluppo

Prima di committare:

- [ ] Build completato senza errori
- [ ] Test manuale della funzionalità
- [ ] Console browser senza errori (F12)
- [ ] Nessun warning critico nel codice
- [ ] CHANGELOG.md aggiornato (se feature o bugfix)
- [ ] Versione incrementata (se deploy imminente)

---

## 🛡️ Best Practice - Validazione Defense-in-Depth

**Regola**: Validare input a TUTTI i layer per robustezza totale.

### Esempio: Endpoint `POST /api/pianificazione/sposta-commessa`

**Layer 1 - JavaScript (gantt-macchine.js)**:
```javascript
// Validazione client-side PRIMA della chiamata API
if (!targetMacchina || isNaN(parseInt(targetMacchina)) || parseInt(targetMacchina) < 1) {
    console.error('❌ Numero macchina non valido:', { targetMacchina });
    alert('Errore: numero macchina non valido.');
    return; // BLOCCA request
}
```

**Layer 2 - Controller (PianificazioneController.cs)**:
```csharp
// Validazione controller-side (anche se JS validato)
if (request.TargetMacchina < 1 || request.TargetMacchina > 99)
{
    _logger.LogWarning("Controller: TargetMacchina non valida: {TargetMacchina}", request.TargetMacchina);
    return BadRequest(new { ErrorMessage = "Numero macchina non valido: deve essere tra 1 e 99" });
}
```

**Layer 3 - Service (PianificazioneEngineService.cs)**:
```csharp
// Validazione service-side (defense-in-depth)
if (request.TargetMacchina < 1 || request.TargetMacchina > 99)
{
    _logger.LogWarning("Service: TargetMacchina non valida: {TargetMacchina}", request.TargetMacchina);
    return new SpostaCommessaResponse 
    { 
        Success = false, 
        ErrorMessage = $"Numero macchina non valido: {request.TargetMacchina}" 
    };
}
```

**Lezione**: Anche se JavaScript valida, Controller e Service DEVONO rivalidare:
- API può essere chiamata direttamente (Postman, curl, Swagger)
- JavaScript può essere disabilitato o bypassato
- Sicurezza: mai fidarsi del client

**Riferimento**: CHANGELOG.md v1.25 - Fix validazione sposta commessa (4 Febbraio 2026)

---

## 🔄 Ambiente Sviluppo

### Database Locale

```
Server:   localhost\SQLEXPRESS01
Database: MESManager
Auth:     Windows Authentication
```

### Porte

```
Web:   http://localhost:5156
```

### Variabili Ambiente

```
ASPNETCORE_ENVIRONMENT=Development
```

Nel file `launchSettings.json`:
```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

---

## 🆘 Supporto

Per configurazione database: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)  
Per architettura: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)  
Per deploy: [01-DEPLOY.md](01-DEPLOY.md)
