# 🏗️ Template Gestionale — Guida Utilizzo

> Template generico per creare nuovi gestionali con AI-first workflow.
> Basato sull'esperienza di MESManager (produzione industriale dal 2024).

---

## 📦 Cosa contiene questo template

```
TEMPLATE-GESTIONALE/
├── BIBBIA-AI-TEMPLATE.md              ← 🧠 Regole AI developer (privata)
├── BIBBIA-AI-CLIENTE-TEMPLATE.md      ← 👤 Guida AI per il cliente (safe, no IP)
├── COME-USARE-QUESTO-TEMPLATE.md      ← 📖 Questa guida
├── STRUTTURA-PROGETTO.md              ← 🏗️ Come strutturare il progetto .NET
├── .deliveryignore                    ← 🔒 Lista file MAI da consegnare al cliente
├── scripts/
│   ├── test-smoke.ps1                 ← Test automatici obbligatori
│   └── prepare-customer-delivery.ps1  ← 📦 Genera pacchetto cliente (senza IP)
└── docs/
    ├── README.md, 01..12-*.md         ← Documentazione completa (developer)
    └── storico/
        └── DEPLOY-LESSONS-LEARNED.md  ← Know-how accumulato (privato)
```

---

## 🔒 Due Livelli: Developer vs Cliente

> **Il tuo know-how rimane tuo.** Il cliente riceve il progetto funzionante e una guida AI semplificata, senza accesso alla tua metodologia, storico, pattern esclusivi o lessons learned.

| Cosa | Developer | Cliente |
|------|-----------|---------|
| `BIBBIA-AI-[Progetto].md` | ✅ Accesso completo | ❌ Mai consegnata |
| `docs/storico/` | ✅ Tutto lo storico | ❌ Solo README vuoto |
| `docs/10-BUSINESS.md` | ✅ Logica business | ❌ Non consegnato |
| `docs/01-DEPLOY.md` | ✅ Con credenziali | ❌ Versione senza cred. |
| `BIBBIA-AI-[Progetto]-CLIENTE.md` | ✅ La crei tu | ✅ È la sua guida AI |
| `.github/copilot-instructions.md` | 👁️ Punta alla BIBBIA dev | 👁️ Punta alla BIBBIA cliente |
| Codice sorgente, tests, scripts | ✅ | ✅ |

---

## 🚀 Passi per un Nuovo Progetto

### 1. Crea il nuovo progetto

```powershell
# Crea cartella progetto
New-Item -ItemType Directory -Path "C:\Dev\[NomeNuovoProgetto]" -Force
cd "C:\Dev\[NomeNuovoProgetto]"

# Crea soluzione .NET
dotnet new sln -n [NomeNuovoProgetto]

# Crea i progetti per Clean Architecture
dotnet new classlib -n [NomeNuovoProgetto].Domain
dotnet new classlib -n [NomeNuovoProgetto].Application
dotnet new classlib -n [NomeNuovoProgetto].Infrastructure
dotnet new blazorserver -n [NomeNuovoProgetto].Web    # oppure: webapi, mvc
# dotnet new worker -n [NomeNuovoProgetto].Worker     # se servono background jobs

# Aggiungi alla soluzione
dotnet sln add **/*.csproj

# Aggiungi riferimenti tra progetti
dotnet add [NomeNuovoProgetto].Application/[NomeNuovoProgetto].Application.csproj reference [NomeNuovoProgetto].Domain/[NomeNuovoProgetto].Domain.csproj
dotnet add [NomeNuovoProgetto].Infrastructure/[NomeNuovoProgetto].Infrastructure.csproj reference [NomeNuovoProgetto].Application/[NomeNuovoProgetto].Application.csproj
dotnet add [NomeNuovoProgetto].Web/[NomeNuovoProgetto].Web.csproj reference [NomeNuovoProgetto].Infrastructure/[NomeNuovoProgetto].Infrastructure.csproj
```

### 2. Installa pacchetti NuGet di base

```powershell
cd [NomeNuovoProgetto].Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File

cd ..\[NomeNuovoProgetto].Web
# Se Blazor + MudBlazor:
dotnet add package MudBlazor
# Se AG Grid:
dotnet add package AG-Grid-Blazor  # o equivalente
```

### 3. Copia e personalizza la documentazione

```powershell
# Copia la cartella docs template
Copy-Item -Recurse "C:\Dev\TEMPLATE-GESTIONALE\docs" "C:\Dev\[NomeNuovoProgetto]\docs"

# Copia la BIBBIA
Copy-Item "C:\Dev\TEMPLATE-GESTIONALE\BIBBIA-AI-TEMPLATE.md" `
          "C:\Dev\[NomeNuovoProgetto]\docs\BIBBIA-AI-[NOME_PROGETTO].md"
```

### 4. Personalizza i placeholder

Sostituisci questi placeholder in tutti i file copiati:

| Placeholder | Sostituire con |
|-------------|----------------|
| `[NOME_PROGETTO]` | Nome del progetto (es. `GestioneClienti`) |
| `[NOME_CLIENTE]` | Nome dell'azienda cliente |
| `[NomeProgetto]` | PascalCase del nome (es. `GestioneClienti`) |
| `[nomeprogetto]` | lowercase (es. `gestioneclienti`) |
| `[PATH_LOCALE]` | Path locale (es. `C:\Dev\GestioneClienti`) |
| `[DB_SERVER_DEV]` | Server DB sviluppo (es. `localhost\SQLEXPRESS`) |
| `[DB_SERVER_PROD]` | Server DB produzione |
| `[DB_NAME_DEV]` | Nome DB sviluppo |
| `[DB_NAME_PROD]` | Nome DB produzione |
| `[IP_PROD]` | IP server produzione |
| `[PORTA_DEV]` | Porta locale (default: 5000 o 5156) |
| `[PORTA_PROD]` | Porta produzione |
| `[DATA_CREAZIONE]` | Data odierna |

```powershell
# Script PowerShell per sostituire tutti i placeholder (adattare)
$files = Get-ChildItem -Path "C:\Dev\[NomeNuovoProgetto]\docs" -Recurse -Filter "*.md"
foreach ($file in $files) {
    (Get-Content $file.FullName) `
        -replace '\[NOME_PROGETTO\]', 'GestioneClienti' `
        -replace '\[NOME_CLIENTE\]', 'Azienda Srl' `
        -replace '\[NomeProgetto\]', 'GestioneClienti' `
        -replace '\[PORTA_DEV\]', '5200' `
        -replace '\[IP_PROD\]', '192.168.1.100' `
        | Set-Content $file.FullName
}
```

### 5. Configura copilot-instructions.md — System Prompt Persistente

> **Imposta la Bibbia del progetto come system prompt persistente tramite le Project Custom Instructions del repository.**
> Questo file viene letto automaticamente da GitHub Copilot ad ogni nuova chat: la BIBBIA sarà sempre attiva senza doverla rileggere manualmente.

```powershell
# Crea .github/copilot-instructions.md nella root del progetto
New-Item -ItemType Directory -Path "C:\Dev\[NomeNuovoProgetto]\.github" -Force
```

Contenuto **completo** per `.github/copilot-instructions.md` (copia questo blocco esatto):

```markdown
# [NOME_PROGETTO] — Istruzioni Obbligatorie per GitHub Copilot

> **REGOLA ASSOLUTA**: Ogni risposta, modifica di codice o analisi su questo progetto
> DEVE rispettare tutte le leggi definite nella BIBBIA:
> `[PATH_LOCALE]\docs\BIBBIA-AI-[NOME_PROGETTO].md`
>
> **⛔ LETTURA INTEGRALE OBBLIGATORIA — PRIMA di qualsiasi risposta:**
> Usa `read_file` con `startLine: 1` e `endLine: 600` sulla BIBBIA.
> **MAI** leggere solo parzialmente. Il file ha 500+ righe.
> Ogni risposta basata su lettura parziale è INVALIDA.

---

## Identità e Ruolo

Agisci come **Senior Software Architect, Maintainer e Storico Tecnico** del progetto [NOME_PROGETTO].
Stack: .NET 8, Blazor Server, MudBlazor, SQL Server, EF Core 8.

- DEV: `[DB_SERVER_DEV]` → `[DB_NAME_DEV]`
- PROD: `[DB_SERVER_PROD]` → `[DB_NAME_PROD]`
- Fonte di verità docs: `[PATH_LOCALE]\docs\`
- Versione corrente: vedi `AppVersion.cs`

---

## Workflow Obbligatorio — OGNI Modifica Codice

```
1. git commit PRIMA delle modifiche
2. Implementa la modifica
3. Incrementa AppVersion.cs
4. dotnet build [NOME_PROGETTO].sln --nologo  (0 errori obbligatori)
5. .\scripts\test-smoke.ps1 -UseExistingServer  → tutti [OK] OBBLIGATORI
6. git commit DOPO le modifiche
7. Aggiorna docs/09-CHANGELOG.md
8. Avvia server: dotnet run --project ... --environment Development
9. Comunica URL + output test: http://localhost:[PORTA_DEV]/[pagina-modificata]
10. Attendi feedback utente
```

**MAI** saltare i passi 4–9.
**MAI** dire "ho finito" / "ok" / "apposto" senza output test verde allegato.
**MAI** lasciare comandi all'utente — l'AI esegue tutto.
**MAI** passare al passo successivo se test-smoke.ps1 mostra FAIL.

---

## Regole Architetturali Inviolabili

1. **ZERO Duplicazione** — una sola fonte di verità, mai copiare/incollare codice
2. **Clean Architecture** — DI, Repository Pattern, layer rispettati
3. **Database** — Dev ≠ Prod SEMPRE | Migration EF per schema | Script SQL per prod
4. **Secrets** — MAI sovrascrivere `appsettings.Secrets.json` o `Database.json` in deploy
5. **Frontend** — CSS globali in `wwwroot/app.css` (NON in `<style>` inline Blazor)
6. **Deploy** — SOLO su ordine esplicito dell'utente
```

### 6. Crea AppVersion.cs

```csharp
// [NomeNuovoProgetto].Web/AppVersion.cs
namespace [NomeNuovoProgetto].Web;

public static class AppVersion
{
    public const string Version = "1.0.0";
    public const string BuildDate = "[DATA_CREAZIONE]";
    public const string Description = "[NOME_PROGETTO] — [NOME_CLIENTE]";
}
```

### 7. Init git

```powershell
cd C:\Dev\[NomeNuovoProgetto]
git init
# Crea .gitignore con: appsettings.Secrets.json, appsettings.Database.json, bin/, obj/
git add -A
git commit -m "chore: inizializzazione progetto da template gestionale"
```

---

## 📐 Checklist Avvio Progetto

- [ ] Struttura Clean Architecture creata
- [ ] Pacchetti NuGet base installati
- [ ] Documentazione docs/ copiata e personalizzata
- [ ] BIBBIA personalizzata e salvata in docs/
- [ ] `.github/copilot-instructions.md` creato con riferimento alla BIBBIA (system prompt persistente)
- [ ] `.github/copilot-instructions.md` configurato
- [ ] Database DEV creato
- [ ] Connection string configurata
- [ ] AppVersion.cs creato
- [ ] .gitignore configurato (secrets esclusi!)
- [ ] git init + primo commit
- [ ] `dotnet build` verde (0 errori)
- [ ] Prima pagina accessibile su `http://localhost:[PORTA_DEV]`

---

## 🧠 Come usare l'AI su questo progetto

1. Apri VS Code nella cartella del nuovo progetto
2. La BIBBIA viene letta automaticamente tramite `copilot-instructions.md` — **nessuna azione richiesta**
3. L'AI seguirà le stesse regole di MESManager:
   - Proporrà sempre 4 soluzioni con tabella comparativa
   - Farà **build + test automatici + run** dopo ogni implementazione
   - NON dirà mai "ok" / "fatto" senza allegare l'output dei test
   - Non deplowerà mai senza ordine esplicito
   - Manterrà la documentazione aggiornata

### Come funziona l'integrazione copilot-instructions.md

L'integrazione è definita nel file `copilot-instructions.md`, visibile nel contesto della sessione.

**Posizionamento del file** — Il file va in `.github/copilot-instructions.md`.
VS Code carica automaticamente tutti i file `copilot-instructions.md` trovati nella cartella `.github/` del workspace, iniettandoli come **contesto di sistema** in ogni conversazione.

**Cosa contiene il file:**

| Sezione | Effetto |  
|---------|---------|  
| Lettura BIBBIA obbligatoria | L'AI legge la BIBBIA completa prima di ogni risposta |
| Workflow obbligatorio | git commit → build → test → run → feedback |
| Obbligo test automatici | MAI dichiarare "fatto" con test rossi |
| Regole architetturali | Zero duplicazione, Clean Architecture |
| Comandi standard PowerShell | Stop porta → build → test → run |
| Lesson learned critiche | Errori noti già documentati |
| Regole deploy | Solo su ordine esplicito utente |

**L'utente non deve fare nulla di speciale**: VS Code inietta il file automaticamente ad ogni nuova chat.  
Apri una nuova chat, scrivi qualsiasi domanda sul progetto — l'AI già conosce tutto il contesto.

---

## 💡 Tips dal campo (MESManager lessons learned)

1. **Inizia semplice** — aggiungi moduli incrementalmente
2. **Documenta subito** — scrivi in 10-BUSINESS.md le regole del cliente durante l'analisi
3. **Lesson learned** — ogni problema scoperto va in `storico/` immediatamente
4. **Zero secrets in git** — mai, nemmeno per test
5. **Build sempre verde** — non accumulare warning/errori
6. **Versione ad ogni modifica** — AppVersion.cs anche per micro-fix

---

## 📦 Consegna al Cliente — Procedura Sicura

> Il cliente riceve il progetto funzionante con una guida AI semplificata.
> La tua metodologia, lessons learned e know-how restano **solo da te**.

### Step 1 — Crea la BIBBIA cliente

Copia `BIBBIA-AI-CLIENTE-TEMPLATE.md` nel progetto e personalizzala:

```powershell
Copy-Item "C:\Dev\TEMPLATE-GESTIONALE\BIBBIA-AI-CLIENTE-TEMPLATE.md" `
          "C:\Dev\[NomeProgetto]\docs\BIBBIA-AI-[NomeProgetto]-CLIENTE.md"

# Sostituisci i placeholder nella BIBBIA cliente (non in quella developer)
# [NOME_PROGETTO], [NOME_CLIENTE], [NOME_FORNITORE], [CONTATTO_FORNITORE], ecc.
```

**Cosa contiene la BIBBIA cliente** (safe, no IP):
- Workflow build/test/run semplificato
- Come aggiungere funzionalità seguendo i pattern esistenti
- Regole che non può rompere
- Riferimento a te come fornitore per interventi strutturali

**Cosa NON contiene** (protetto):
- Lessons learned tecniche profonde
- Storico decisioni architetturali
- Pattern esclusivi del developer
- Credenziali e procedure deploy
- Logica business e commerciale

### Step 2 — Genera il pacchetto cliente

```powershell
cd C:\Dev\[NomeProgetto]

.\scripts\prepare-customer-delivery.ps1 `
    -CustomerName "Azienda Srl" `
    -SupplierName "TuoNome / TuaAzienda" `
    -SupplierContact "info@tuodominio.it"
```

Lo script in automatico:
1. Copia tutto il progetto in `customer-delivery\[NomeProgetto]-[data]\`
2. Rimuove BIBBIA developer, storico, business, credenziali
3. Ricrea `storico/` vuoto (solo README)
4. Genera un `copilot-instructions.md` sicuro che punta alla BIBBIA cliente
5. Verifica che nessuna IP privata sia rimasta
6. Stampa una checklist finale

### Step 3 — Verifica prima di consegnare

```powershell
# Controlla manualmente la cartella output
explorer customer-delivery\[NomeProgetto]-[data]

# Fai un test build del pacchetto cliente
cd customer-delivery\[NomeProgetto]-[data]
dotnet build [NomeProgetto].sln --nologo
```

Checklist:
- [ ] `docs/storico/` contiene solo `README.md` (nessun FIX-*.md)
- [ ] `docs/BIBBIA-AI-[NomeProgetto].md` (developer) NON esiste
- [ ] `docs/BIBBIA-AI-[NomeProgetto]-CLIENTE.md` (cliente) ESISTE
- [ ] `.github/copilot-instructions.md` punta alla BIBBIA cliente
- [ ] `appsettings.Secrets.json` NON esiste
- [ ] `docs/10-BUSINESS.md` NON esiste
- [ ] Build verde nella cartella output

### Step 4 — Setup VS Code cliente

Istruzioni da dare al cliente:

```
1. Installa VS Code: https://code.visualstudio.com/
2. Installa estensione GitHub Copilot (richiede account GitHub)
3. Apri la cartella del progetto in VS Code: File → Open Folder
4. Da quel momento Copilot conosce già tutto il progetto automaticamente
   (copilot-instructions.md viene iniettato ad ogni chat)
5. Per chiedere un'implementazione: apri chat Copilot (Ctrl+Shift+I)
   e descrivi cosa vuoi aggiungere o correggere
```

---

*Template versione: 1.0 | Creato: Aprile 2026 | Basato su MESManager v4.5 BIBBIA*
