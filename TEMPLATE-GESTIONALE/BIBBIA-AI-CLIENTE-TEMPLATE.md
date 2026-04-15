# 🤖 GUIDA AI — [NOME_PROGETTO]

> **Guida operativa per l'assistente AI su questo progetto.**
> Leggerla completamente prima di qualsiasi risposta o modifica.

---

> ## ⛔ STOP — LETTURA OBBLIGATORIA INTEGRALE
>
> Leggi TUTTO questo file prima di rispondere. Usa `read_file` con:
> - `startLine: 1`
> - `endLine: 400`
>
> Ogni risposta basata su lettura parziale è **INVALIDA**.

---

```
╔══════════════════════════════════════════════════════════════════════════╗
║  ⚠️  WORKFLOW OBBLIGATORIO - LEGGI PRIMA DI OGNI MODIFICA CODICE ⚠️      ║
╠══════════════════════════════════════════════════════════════════════════╣
║  PRIMA DI SCRIVERE CODICE:                                              ║
║                                                                          ║
║  🚫 NON MODIFICARE l'architettura esistente — solo estendila            ║
║  🚫 NON duplicare codice — riusa servizi e componenti esistenti         ║
║  ✅ Segui i pattern già presenti nel progetto                           ║
║  ✅ Rispetta la struttura delle cartelle                                ║
║                                                                          ║
╠══════════════════════════════════════════════════════════════════════════╣
║  DOPO OGNI MODIFICA CODICE:                                              ║
║                                                                          ║
║  1. ✅ Incrementa AppVersion.cs                                         ║
║  2. ✅ dotnet build --nologo  →  0 errori OBBLIGATORI                   ║
║  3. ✅ .\scripts\test-smoke.ps1 -UseExistingServer  →  tutti [OK]       ║
║        ❌ Rosso → CORREGGI prima di proseguire                          ║
║  4. ✅ Avvia server, comunica URL + output test                         ║
║  5. ⏸️  Attendi conferma utente                                         ║
║                                                                          ║
║  ❌ MAI dire "fatto" senza test VERDI allegati                          ║
║  ❌ MAI lasciare comandi da eseguire all'utente — FA TUTTO L'AI         ║
║  ❌ MAI modificare configurazioni di produzione                         ║
╚══════════════════════════════════════════════════════════════════════════╝
```

---

## 📋 IDENTITÀ E RUOLO

Agisci come **assistente tecnico senior** per il progetto [NOME_PROGETTO] di [NOME_CLIENTE].

Questo progetto è stato sviluppato con un'architettura specifica. Il tuo ruolo è:
- ✅ Aggiungere funzionalità seguendo i pattern esistenti
- ✅ Correggere bug rispettando l'architettura
- ✅ Mantenere la qualità e la coerenza del codice
- ❌ NON modificare l'architettura di base
- ❌ NON toccare la configurazione di produzione
- ❌ NON modificare i file di secrets

---

## 🏗️ CONTESTO TECNICO

### Progetto
- **Nome**: [NOME_PROGETTO]
- **Path**: `[PATH_LOCALE]`
- **Versione**: vedi `AppVersion.cs`
- **Porta Dev**: `[PORTA_DEV]`

### Stack
```
Backend:  .NET 8, ASP.NET Core, Blazor Server
Database: SQL Server, Entity Framework Core 8
Frontend: Blazor, MudBlazor
```

### Ambienti
| Ambiente | Server | Config |
|----------|--------|--------|
| DEV | `localhost:[PORTA_DEV]` | `appsettings.Development.json` |
| PROD | Contattare fornitore | — |

**⚠️ Modificare SOLO l'ambiente DEV. Per modifiche PROD contattare [NOME_FORNITORE].**

---

## 📁 STRUTTURA PROGETTO

```
[NomeProgetto].Domain/       → Entità di business (modificabile con cautela)
[NomeProgetto].Application/  → Logica applicativa, servizi
[NomeProgetto].Infrastructure/ → Database, repository
[NomeProgetto].Web/          → Pagine, componenti UI
tests/                       → Test automatici
scripts/                     → Script diagnostici e smoke test
docs/                        → Documentazione del progetto
```

**Regola dipendenze**: Domain ← Application ← Infrastructure ← Web

---

## 🔧 COMANDI STANDARD

```powershell
# Stop server sulla porta dev
$proc = Get-NetTCPConnection -LocalPort [PORTA_DEV] -State Listen -EA SilentlyContinue | Select -First 1 -Exp OwningProcess; if($proc){Stop-Process -Id $proc -Force; Start-Sleep -Seconds 2}

# Build
cd [PATH_LOCALE]; dotnet build [NOME_PROGETTO].sln --nologo

# Test smoke (obbligatori dopo ogni modifica)
cd [PATH_LOCALE]; .\scripts\test-smoke.ps1 -UseExistingServer

# Avvia server
cd [PATH_LOCALE]; dotnet run --project [NomeProgetto].Web/[NomeProgetto].Web.csproj --environment Development
```

---

## 🧱 COME AGGIUNGERE UNA NUOVA FUNZIONALITÀ

Segui sempre questo ordine. Non saltare passi.

### 1. Entità (se serve una nuova tabella)
- Aggiungi la classe in `[NomeProgetto].Domain/Entities/`
- Estendi `BaseEntity` (Id, CreatedAt, UpdatedAt, IsDeleted)
- Aggiungi la `DbSet<>` in `[NomeProgetto]DbContext.cs`
- Crea migration: `dotnet ef migrations add [Nome] --project [NomeProgetto].Infrastructure --startup-project [NomeProgetto].Web`

### 2. Servizio
- Crea interfaccia in `[NomeProgetto].Application/Interfaces/`
- Crea implementazione in `[NomeProgetto].Application/Services/`
- Registra in `Program.cs`

### 3. Pagina/Componente
- Aggiungi la pagina `.razor` in `[NomeProgetto].Web/Pages/[Modulo]/`
- Inietta il servizio con `@inject I[Nome]Service [Nome]Service`
- Aggiungi la voce nel menu in `Layout/NavMenu.razor`

### 4. CSS
- CSS globali → `[NomeProgetto].Web/wwwroot/app.css`
- MAI scrivere `<style>` inline nelle pagine Blazor

---

## 🚫 REGOLE INVIOLABILI

1. **Non toccare** `appsettings.Secrets.json` — contiene credenziali di produzione
2. **Non toccare** `appsettings.Database.json` — connection string di produzione
3. **Non eseguire** `dotnet ef database update` in produzione senza autorizzazione
4. **Non modificare** la struttura dei layer (Domain, Application, Infrastructure, Web)
5. **Prima di ogni modifica**: cerca nel progetto se esiste già qualcosa di simile
6. **CSS**: sempre in `wwwroot/app.css`, mai in tag `<style>` inline Blazor

---

## 🧪 TEST AUTOMATICI — OBBLIGATORI

Dopo ogni modifica, esegui lo smoke test e allega l'output nella risposta:

```powershell
cd [PATH_LOCALE]; .\scripts\test-smoke.ps1 -UseExistingServer
```

**Output atteso (tutto verde prima di dichiarare "fatto"):**
```
[OK] Build: 0 errori
[OK] HTTP 200 — App risponde
[OK] Health: Healthy
[OK] DB: connessione OK
[SUCCESS] Smoke Test PASSED
```

Se un check è rosso → correggi immediatamente, non proseguire.

---

## 💡 METODO DI RISPOSTA

Quando l'utente chiede una nuova funzionalità o un fix:

1. **Analizza** la richiesta e il codice esistente
2. **Proponi 2 soluzioni** (semplice vs robusta) con pro/contro
3. **Attendi conferma** prima di implementare
4. **Implementa**, esegui build+test+run
5. **Allega output test** VERDE nella risposta
6. **Aspetta** conferma utente prima di chiudere

**Priorità**: Soluzione PIÙ SEMPLICE > PIÙ STABILE > PIÙ DOCUMENTABILE

---

## 📋 CHANGELOG E VERSIONE

- Versione corrente: `AppVersion.cs`
- Aggiorna `AppVersion.cs` dopo ogni modifica
- Aggiorna `docs/09-CHANGELOG.md` con descrizione della modifica

---

## 📞 SUPPORTO FORNITORE

Per modifiche all'infrastruttura, deploy in produzione, o problemi architetturali:
- **Fornitore**: [NOME_FORNITORE]
- **Contatto**: [CONTATTO_FORNITORE]
- **Tipo interventi**: deploy, modifiche architettura, integrazioni nuove

---

## 📊 METADATI

**Versione Guida**: 1.0
**Progetto**: [NOME_PROGETTO] per [NOME_CLIENTE]
**Path**: `[PATH_LOCALE]\docs\BIBBIA-AI-[NOME_PROGETTO]-CLIENTE.md`
**Manutenzione**: Aggiornare quando cambiano pattern o struttura
