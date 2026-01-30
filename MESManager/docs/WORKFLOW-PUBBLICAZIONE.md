# 🚀 Workflow Pubblicazione - Istruzioni per AI Assistant

> **SCOPO**: Questo file definisce il processo ESATTO che l'AI Assistant deve seguire quando l'utente dice:
> - "pubblica sul server"
> - "deploy"
> - "vai in produzione"
> - "ok pubblichiamo"
>
> **REGOLA**: L'AI DEVE seguire questi step in ordine, senza saltarne nessuno.

---

## 📋 Trigger Comandi

Quando l'utente dice una di queste frasi, esegui il workflow completo:
- `pubblica`, `deploy`, `vai live`, `metti in produzione`
- `ok pubblichiamo tutto sul server`
- `pronto per il deploy`

---

## 🔄 Workflow Completo (Step-by-Step)

### FASE 1: Pre-Controlli

```
□ 1.1 Verifica compilazione
      Esegui: dotnet build MESManager.sln --nologo
      Se errori → STOP e risolvi

□ 1.2 Leggi PENDING-CHANGES.md
      Verifica se ci sono modifiche non consolidate
      Se vuoto → Chiedi conferma all'utente

□ 1.3 Identifica versione attuale
      Leggi MainLayout.razor riga 157 per trovare v1.XX
```

### FASE 2: Consolidamento Documentazione

```
□ 2.1 Determina nuova versione
      Incrementa: v1.XX → v1.(XX+1)

□ 2.2 Aggiorna CHANGELOG.md
      - Crea nuova sezione per la versione
      - Copia contenuto da PENDING-CHANGES.md
      - Raggruppa per categoria (Features, Bug Fix, etc.)
      - Aggiungi data e status

□ 2.3 Aggiorna MainLayout.razor
      - Modifica la versione visualizzata (riga ~124)
      - Formato: v1.XX (DD Mese AAAA)

□ 2.4 Svuota PENDING-CHANGES.md
      - Sposta modifiche nella sezione "Consolidate"
      - Mantieni solo il template vuoto
```

### FASE 3: Build per Produzione

```
□ 3.1 Pulisci e ricompila
      Esegui: dotnet clean MESManager.sln
      Esegui: dotnet build MESManager.sln -c Release

□ 3.2 Pubblica Web
      Esegui: dotnet publish MESManager.Web -c Release -o publish/Web

□ 3.3 Pubblica PlcSync (se modificato)
      Esegui: dotnet publish MESManager.PlcSync -c Release -o publish/PlcSync

□ 3.4 Pubblica Worker (se modificato)
      Esegui: dotnet publish MESManager.Worker -c Release -o publish/Worker
```

### FASE 4: Istruzioni Deploy Server

```
□ 4.1 Genera istruzioni per l'utente
      Mostra comandi da eseguire sul server 192.168.1.230
      Segui DEPLOY-GUIDA-DEFINITIVA.md per i path

□ 4.2 Checklist file da copiare
      - publish/Web/* → C:\MESManager\Web\
      - publish/PlcSync/* → C:\MESManager\PlcSync\ (se modificato)
      - Se solo JS modificati → copia solo wwwroot/

□ 4.3 Ricorda all'utente
      - NON sovrascrivere appsettings.Database.json
      - NON sovrascrivere appsettings.Secrets.json
      - Ordine stop: PlcSync → Worker → Web
      - Ordine start: Web → Worker → PlcSync
```

### FASE 5: Post-Deploy

```
□ 5.1 Chiedi conferma all'utente
      "Deploy completato? Confermami quando il server è online"

□ 5.2 Aggiorna CHANGELOG.md status
      Cambia status da "🚧 In sviluppo" a "✅ Deploy completato"
```

---

## 📁 File Coinvolti nel Workflow

| File | Ruolo |
|------|-------|
| `docs/PENDING-CHANGES.md` | Buffer modifiche non consolidate |
| `docs/CHANGELOG.md` | Storico versioni ufficiale |
| `MainLayout.razor` | Versione visualizzata nell'app |
| `docs/DEPLOY-GUIDA-DEFINITIVA.md` | Guida deploy dettagliata |

---

## 🎯 Esempio Output AI

Quando l'utente dice "pubblica sul server", l'AI deve rispondere con:

```markdown
## 🚀 Avvio Workflow Pubblicazione

### ✅ FASE 1: Pre-Controlli
- [x] Compilazione OK
- [x] Trovate 4 modifiche in PENDING-CHANGES.md
- [x] Versione attuale: v1.16

### 📝 FASE 2: Consolidamento
- Nuova versione: **v1.17**
- [Mostra riepilogo modifiche]
- [Aggiorna CHANGELOG.md]
- [Aggiorna MainLayout.razor]

### 🔨 FASE 3: Build
[Esegue comandi build]

### 📋 FASE 4: Istruzioni Deploy
[Mostra comandi da eseguire sul server]

### ❓ Conferma
Procedo con la build per produzione?
```

---

## ⚠️ Casi Speciali

### Deploy Solo JavaScript (senza recompile)
Se le modifiche sono solo in file `.js` o `.css`:
1. Salta FASE 3 (build)
2. Istruisci l'utente a copiare solo `wwwroot/`

### Deploy Solo SQL
Se ci sono migrazioni database:
1. Genera script SQL con `dotnet ef migrations script`
2. Includi nelle istruzioni deploy

### Rollback
Se l'utente chiede di annullare:
1. Riferisci a `docs/DEPLOY-GUIDA-DEFINITIVA.md` sezione rollback
2. Mantieni backup versione precedente

---

## 📞 Comunicazione con Utente

### Chiedi Sempre
- Se ci sono test da eseguire prima
- Se vuole vedere le modifiche in preview
- Se vuole backup del server prima del deploy

### Non Assumere Mai
- Non fare deploy automatico senza conferma
- Non saltare la fase di consolidamento docs
- Non dimenticare di incrementare la versione

---

## 🔗 Riferimenti

- [DEPLOY-GUIDA-DEFINITIVA.md](DEPLOY-GUIDA-DEFINITIVA.md) - Guida completa deploy
- [PENDING-CHANGES.md](PENDING-CHANGES.md) - Buffer modifiche
- [CHANGELOG.md](CHANGELOG.md) - Storico versioni
- [SERVIZI.md](SERVIZI.md) - Ordine stop/start servizi
