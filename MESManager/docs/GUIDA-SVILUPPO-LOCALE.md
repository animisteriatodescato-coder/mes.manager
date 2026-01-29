# Guida Sviluppo e Test Locale

## 🔄 Workflow Standard per Modifiche

### PRIMA di iniziare qualsiasi modifica:

```powershell
cd C:\Dev\MESManager

# 1. Verifica lo stato git
git status

# 2. Commit delle modifiche pendenti (se ce ne sono)
git add .
git commit -m "descrizione delle modifiche"
```

---

## 📝 Flusso Completo: Modifica → Test → Commit

### 1️⃣ COMMIT INIZIALE (se ci sono modifiche pendenti)
```powershell
cd C:\Dev\MESManager
git add .
git commit -m "wip: salvataggio prima di nuove modifiche"
```

### 2️⃣ CHIUDERE l'applicazione in esecuzione
- Se c'è un terminale con `dotnet run` attivo: premi `Ctrl+C`
- Oppure chiudi il terminale

### 3️⃣ APPLICARE LE MODIFICHE
(fai le modifiche ai file necessari)

### 4️⃣ BUILD
```powershell
cd C:\Dev\MESManager
dotnet build MESManager.sln -c Release --nologo
```
Verifica: **0 errori**

### 5️⃣ AVVIO APPLICAZIONE
```powershell
cd C:\Dev\MESManager\MESManager.Web
dotnet run --environment Development
```

L'output deve mostrare:
```
Now listening on: http://localhost:5156
Application started. Press Ctrl+C to shut down.
```

### 6️⃣ TEST nel Browser
Apri: http://localhost:5156

### 7️⃣ COMMIT FINALE (dopo aver testato)
```powershell
# Ctrl+C per fermare l'app
cd C:\Dev\MESManager
git add .
git commit -m "feat: descrizione della modifica"
```

---

## 🛑 Problemi Comuni

### ❌ "Il file è bloccato da MESManager.Web"
**Causa**: L'applicazione è ancora in esecuzione

**Soluzione**:
```powershell
# Chiudi l'applicazione
taskkill /IM dotnet.exe /F

# Oppure trova e termina il processo
Get-Process -Name dotnet | Stop-Process -Force
```

### ❌ "Il file di progetto non esiste"
**Causa**: Path errato

**Soluzione**: Esegui dalla directory corretta:
```powershell
cd C:\Dev\MESManager\MESManager.Web
dotnet run --environment Development
```

### ❌ "Porta già in uso"
**Causa**: Un'altra istanza è in esecuzione

**Soluzione**:
```powershell
# Trova cosa sta usando la porta 5156
netstat -ano | findstr :5156

# Termina il processo (sostituisci PID con il numero trovato)
taskkill /PID <PID> /F
```

---

## 📋 Comandi Rapidi (Copia/Incolla)

### Build Completa
```powershell
cd C:\Dev\MESManager; dotnet build MESManager.sln -c Release --nologo
```

### Avvio Rapido
```powershell
cd C:\Dev\MESManager\MESManager.Web; dotnet run --environment Development
```

### Ferma + Build + Avvia
```powershell
taskkill /IM dotnet.exe /F 2>$null; cd C:\Dev\MESManager; dotnet build MESManager.sln -c Release --nologo; cd MESManager.Web; dotnet run --environment Development
```

### Commit Rapido
```powershell
cd C:\Dev\MESManager; git add .; git commit -m "modifica"
```

---

## 🌐 URL Principali per Test

| Pagina | URL |
|--------|-----|
| Home | http://localhost:5156 |
| Programma Macchine | http://localhost:5156/programma/programma-macchine |
| Commesse Aperte | http://localhost:5156/programma/commesse-aperte |
| Catalogo Anime | http://localhost:5156/cataloghi/anime |
| PLC Monitor | http://localhost:5156/plc-monitor |

---

## 🔁 Task VS Code Disponibili

In VS Code puoi usare i task pre-configurati:
- `Ctrl+Shift+P` → "Tasks: Run Task"
- Seleziona: **Run MESManager Web Dev**

---

## ⚠️ IMPORTANTE per Copilot/AI Assistant

Quando esegui modifiche, segui SEMPRE questo ordine:

1. **COMMIT** delle modifiche pendenti
2. **CHIUDI** l'applicazione in esecuzione  
3. **MODIFICA** i file
4. **BUILD** per verificare errori
5. **AVVIA** l'applicazione
6. **TESTA** nel browser
7. **COMMIT** le nuove modifiche

Non saltare mai il passo 2 (chiusura) altrimenti la build fallisce con "file bloccato"!
