# 📋 Changelog Versioni MESManager

> ⚠️ **REGOLA OBBLIGATORIA VERSIONAMENTO**
> 
> Ad ogni modifica funzionale o bug fix, **DEVI**:
> 1. Incrementare la versione in `MainLayout.razor` (riga 157): `v1.XX` → `v1.XX+1`
> 2. Aggiornare questo CHANGELOG con la nuova versione
> 3. **MAI** deployare senza aver incrementato la versione
>
> La versione è visibile in basso a destra nell'app.
>
> 📋 **Vedi anche**: [WORKFLOW-PUBBLICAZIONE.md](WORKFLOW-PUBBLICAZIONE.md) per istruzioni complete

---

## v1.23 (3 Febbraio 2026)
**Data:** 03 Feb 2026  
**Status:** ✅ Completato

### Modifiche v1.23

#### 🐛 Fix Critico - Tabella Festivi Database
- **Problema**: Errore "Il nome di oggetto 'Festivi' non è valido" su Gantt Macchine
- **Causa**: Migration esistente ma tabella non creata nel database
- **Fix**: Creazione forzata tabella Festivi con tutti i campi necessari
- **SQL**: CREATE TABLE con Id (GUID PK), Data (date), Descrizione (nvarchar200), Ricorrente (bit), Anno (int null), DataCreazione (datetime2)

#### 🎨 Fix Cache CSS - Dark Mode
- **Problema**: Modifiche colori dark mode non visibili nel browser
- **Causa**: Browser cache CSS vecchio
- **Fix**: Aggiunto query string versioning `?v=1.23` a CSS bootstrap
- **Tecnica**: Cache-busting per forzare reload assets

#### ⚙️ Miglioramento Log Debug
- **Aggiunto**: Tag versione `[v1.23]` nei log console JavaScript
- **Scopo**: Verificare quale versione JavaScript è caricata nel browser
- **File**: `commesse-aperte-grid.js`

---

## v1.22 (3 Febbraio 2026)
**Data:** 03 Feb 2026  
**Status:** ✅ Completato

### Modifiche v1.22

#### 🎉 Gestione Festivi - UI Completa
- **Aggiunta**: Nuovo tab "Festivi" in **Impostazioni → Gantt Macchine**
- **Funzionalità**:
  - CRUD completo per festivi (crea, modifica, elimina)
  - Support festivi ricorrenti (es. Natale 25/12, Ferragosto 15/08)
  - Flag automatico Anno quando Ricorrente = false
  - Ordinamento automatico per data
- **UI**: MudTable + MudDialog con DatePicker e switch Ricorrente
- **File**: `ImpostazioniGantt.razor` (aggiunto 4° tab dopo "Tempo Attrezzaggio")

#### 🧑‍💼 Servizio Festivi - Backend
- **Aggiunta**: `FestiviAppService` e `IFestiviAppService`
- **Metodi**: GetListaAsync, GetAsync, CreaAsync, AggiornaAsync, EliminaAsync
- **Integrazione**: Registrato in `DependencyInjection.cs`
- **File**: `MESManager.Application/Services/FestiviAppService.cs`

#### 🎨 Dark Mode Ultra-Leggibile - Grigio Quasi Bianco
- **Problema**: Utente riportava testi ancora troppo scuri in dark mode
- **Soluzione**: Colori grigio quasi bianco per massima leggibilità
- **Modifiche colori**:
  - TextPrimary: `rgba(255,255,255,0.95)` - quasi bianco
  - TextSecondary: `rgba(255,255,255,0.85)` - grigio chiarissimo
  - TextDisabled: `rgba(255,255,255,0.6)` - più visibile
  - Secondary: `#e0e0e0` - grigio quasi bianco (prima #b0b0b0)
  - Surface: `#2d2d2d` - superficie leggermente più chiara
- **File**: `MainLayout.razor.cs` - PaletteDark

#### 🐛 Debug JavaScript - Cache Reload
- **Aggiunta**: Log console con tag `[v1.22]` per debugging
- **Scopo**: Forzare browser a ricaricare JavaScript modificato
- **File**: `commesse-aperte-grid.js`

---

## v1.21 (3 Febbraio 2026)
**Data:** 03 Feb 2026  
**Status:** ✅ Completato

### Modifiche v1.21

#### 🗄️ Database - Tabella Festivi
- **Aggiunta**: Migration per tabella `Festivi` (giorni festivi e chiusure aziendali)
- **Fix**: Errore "Il nome di oggetto 'Festivi' non è valido" durante pianificazione
- **Entità**: `Festivo` con Id, Data, Descrizione, Ricorrente, Attivo

#### 🐛 Fix Critico - Assegnazione Macchina in Commesse Aperte
- **Problema**: Errore "The JSON value could not be converted to System.Int32" quando si assegna macchina
- **Causa**: Regex `replace(/M0*/gi, '')` non gestiva correttamente valori come "01", "02"
- **Fix**: Usato `replace(/\\D/g, '')` per estrarre solo numeri
- **File**: `commesse-aperte-grid.js` - onCellValueChanged handler

#### 🎨 UI - Dark Mode Testi Più Chiari
- **Miglioramento**: Testi grigi più visibili in modalità scura
- **Modifiche colori dark mode**:
  - Secondary: `#757575` → `#b0b0b0`
  - TextSecondary: `rgba(255,255,255,0.7)` (prima default scuro)
  - TextDisabled: `rgba(255,255,255,0.5)` (più visibile)
  - ActionDisabled: `rgba(255,255,255,0.4)`
  - Divider: `rgba(255,255,255,0.15)`
- **File**: `MainLayout.razor.cs` - PaletteDark

#### ⚙️ Pianificazione - Default 8 Ore
- **Aggiunta**: Se mancano TempoCiclo o NumeroFigure, usa 8 ore (480 min) come default
- **File**: `PianificazioneService.cs` - CalcolaDurataPrevistaMinuti
- **Logica**: Setup + 480 minuti di lavorazione standard

#### ⚠️ Gantt - Indicatore Dati Incompleti
- **Aggiunta**: Triangolino ⚠️ nel Gantt per commesse con dati mancanti
- **DTO**: Nuovo campo `DatiIncompleti` in `CommessaGanttDto`
- **Tooltip**: "ATTENZIONE: Dati incompleti (usato 8h standard)"
- **File**: `gantt-macchine.js`, `PianificazioneEngineService.cs`

---

## v1.20 (3 Febbraio 2026)
**Data:** 03 Feb 2026  
**Status:** ✅ Completato

### Modifiche v1.20

#### 🐛 Fix Critico - SignalR Version Mismatch
- **Problema**: L'applicazione non rispondeva ai click dopo l'aggiunta del Gantt
- **Causa**: Pacchetto `Microsoft.AspNetCore.SignalR.Client` versione 10.0.2 incompatibile con .NET 8
- **Errore**: `MissingMethodException: Method not found: IInvocationBinder.GetTarget(ReadOnlySpan<Byte>)`
- **Fix**: Cambiata versione SignalR.Client da `10.0.2` a `8.*` nel csproj

#### 🔧 Fix Configurazione Blazor
- Rimossa chiamata duplicata `AddServerSideBlazor()` in Program.cs
- Consolidato su `AddInteractiveServerComponents()` con `DetailedErrors = true`

#### 🧭 Fix Navigazione Menu
- Corretti link nel NavMenu che puntavano a route sbagliate
- Gantt Macchine ora punta correttamente a `/programma/gantt-macchine`
- Aggiunta voce menu "Commesse Aperte"

---

## v1.19 (2 Febbraio 2026)
**Data:** 02 Feb 2026  
**Status:** ✅ Completato

### Modifiche v1.19

#### 🐛 Fix Critico - Blazor WebSocket Connection
- **Fix PreferencesService**: Aggiunta protezione per chiamate JSInterop durante pre-rendering
- Risolto errore "Server returned an error on close: Connection closed with an error"
- Aggiunto catch specifico per `InvalidOperationException` durante prerendering
- L'applicazione ora si carica correttamente senza disconnessioni WebSocket

---

## v1.18 (2 Febbraio 2026)
**Data:** 02 Feb 2026  
**Status:** 🚧 In sviluppo

### Modifiche v1.18

#### 📅 Sistema Pianificazione Gantt Avanzato
- **Drag & Drop Commesse**: Spostamento visuale delle commesse tra macchine nel Gantt
- **Accodamento Rigido**: Le commesse si accodano automaticamente in sequenza senza sovrapposizioni
- **Ricalcolo Cascata**: Al termine di una commessa, tutte le successive vengono ricalcolate
- **API SpostaCommessa**: Nuovo endpoint per spostare commesse con validazione

#### 🎄 Gestione Festivi
- **Nuova pagina** `/impostazioni/festivi` per gestire giorni festivi
- CRUD completo per festivi ricorrenti e una tantum
- Inizializzazione automatica festivi italiani standard
- I festivi vengono considerati nel calcolo durata commesse

#### 🔄 SignalR Real-Time Sync
- Sincronizzazione real-time del Gantt tra client multipli
- Icona stato connessione nel Gantt (verde/rosso)
- Hub dedicato `/hubs/gantt` per broadcast modifiche

#### 🎨 UX Miglioramenti
- Snackbar feedback su spostamento commesse
- Pulsante "Ricalcola Tutto" nel Gantt
- Voce menu Festivi in Impostazioni

#### 🐛 Fix Compilazione
- Risolti tutti i warning CS8601/CS8602/CS8604 nullable reference
- Fix null check in IssueLogList, IssueLogDetail, AllegatiAnimaService
- Fix null coalescing in AllegatoArticoloService, CommessaAppService
- Rimossa variabile inutilizzata in ProgrammaMacchine

---

## v1.17 (30 Gennaio 2026)
**Data:** 30 Gen 2026  
**Status:** ✅ Completato

### Modifiche v1.17

#### 🏠 Rebranding Home Page
- Titolo AppBar cambiato da "Dashboard" a **"Todescato MES"** con sottotitolo "*powered by Marra*"

#### 💩 Menu "Programma Irene"
- Mantenuta icona cacca **marrone** originale
- Corretto allineamento verticale dell'icona nel menu (era sfalsata)

#### 📊 Nuova Pagina: Statistiche PLC Storico
- Nuova pagina `/statistiche/plc-storico` per analisi dati storici PLC
- Aggiunto feedback visivo quando non ci sono dati nel periodo selezionato
- **KPI Cards**: Cicli totali, Scarti totali, Tempo medio ciclo, Efficienza media
- **Grafici**:
  - Produzione per Macchina (Bar chart)
  - Scarti per Macchina (Bar chart)  
  - Produzione Giornaliera (Line chart)
  - Distribuzione Stati Macchina (Donut chart)
- **Tabelle**: Top 10 Operatori, Top 10 Commesse
- Filtri per intervallo date e singola macchina
- Link aggiunto nel menu Statistiche

---

## v1.16 (30 Gennaio 2026)
**Data:** 30 Gen 2026  
**Status:** ✅ Completato

### Modifiche v1.16

#### 🏠 Home Semplificata
- Rimossa Dashboard Produzione dalla home
- Solo selettore utente piccolo in alto a destra per vedere lo sfondo

#### 🐛 Issue Log nel Menu
- Aggiunto link "Issue Log" nel menu Impostazioni

#### 🛡️ ErrorBoundary Globale
- Implementato ErrorBoundary nel MainLayout per catturare errori runtime
- Gli errori vengono mostrati con alert e pulsante "Riprova"

---

## v1.15 (30 Gennaio 2026)
**Data:** 30 Gen 2026  
**Status:** 🚧 In sviluppo

### Nuova Funzionalità: Issue Log (Archivio Bug & Errori)

Implementato sistema interno per tracciare bug, errori e problemi tecnici con export "pronto per AI".

#### 🐛 Funzionalità Principali
- **Pagina Lista Issues** (`/impostazioni/issue-log`): Tabella con filtri per Stato, Area, Gravità, Ambiente
- **Pagina Dettaglio/Edit** (`/impostazioni/issue-log/{id}`): Form completo con tutti i campi
- **Export per AI**: Bottone che genera Markdown formattato con contesto, problema, passi, log e vincoli
- **Warning Documentazione**: Alert se issue risolto ma docs non aggiornati
- **Menu**: Voce aggiunta sotto "Impostazioni"

#### 📊 Campi Entità TechnicalIssue
- `Environment`: Dev/Prod
- `Area`: DB, Deploy, AGGrid, UX, Security, Performance, PLC, Sync, Other
- `Severity`: Low, Medium, High, Critical
- `Status`: Open, Investigating, Resolved, Documented
- `Title`, `Description`, `ReproSteps`, `Logs`, `AffectedVersion`
- `Solution`, `RulesLearned`
- `DocsUpdated`, `DocsReferencePath`

#### 📁 File Creati/Modificati
- `MESManager.Domain/Enums/IssueEnums.cs` - Enumerazioni
- `MESManager.Domain/Entities/TechnicalIssue.cs` - Entità
- `MESManager.Application/Interfaces/ITechnicalIssueService.cs` - Interfaccia service
- `MESManager.Infrastructure/Services/TechnicalIssueService.cs` - Implementazione
- `MESManager.Infrastructure/Data/MesManagerDbContext.cs` - DbSet + configurazione
- `MESManager.Infrastructure/DependencyInjection.cs` - Registrazione DI
- `MESManager.Infrastructure/Migrations/xxx_AddTechnicalIssuesTable.cs` - Migration
- `MESManager.Web/Components/Pages/IssueLog/IssueLogList.razor` - Pagina lista
- `MESManager.Web/Components/Pages/IssueLog/IssueLogDetail.razor` - Pagina dettaglio
- `MESManager.Web/Components/Layout/NavMenu.razor` - Voce menu

#### 🔐 Sicurezza
- Nessun controllo ruoli (coerente con pattern attuale UtenteApp senza password)
- Tutti gli utenti possono accedere a Issue Log

#### 📝 Note Deploy
- Richiede migration EF Core: `dotnet ef database update`

---

## v1.14 (29 Gennaio 2026)
**Data:** 29 Gen 2026  
**Status:** ✅ Deploy completato

### Modifiche UI/UX

#### 🎯 Dashboard e PLC Realtime - Filtro Macchine con IP
- Le pagine Dashboard Produzione e PLC Realtime ora mostrano **solo le macchine con indirizzo IP configurato** nelle impostazioni Gantt Macchine
- Filtro applicato in `PlcAppService.GetRealtimeDataAsync()` con `.Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))`

#### 📏 Ottimizzazione Altezza Righe Tabelle
- **PLC Storico**: Ridotta altezza header a 24px e righe a 28px per visualizzare più dati
- **Catalogo Articoli**: Stessa ottimizzazione (headerHeight: 24, rowHeight: 28)
- **Catalogo Clienti**: Stessa ottimizzazione (headerHeight: 24, rowHeight: 28)
- Allineamento al layout già usato su Commesse Aperte

#### 🔧 Fix Doppio Clic Modifica Anima su Commesse
- Corretto handler `onRowDoubleClicked` che non funzionava più
- Problema: `event.colDef.field` poteva essere undefined per alcune colonne
- Soluzione: Uso di `event.column?.getColId?.() || event.colDef?.field` con optional chaining

#### 🏷️ Pulsante Stampa Etichetta su Programma Macchine
- Aggiunta colonna "Stampa Etichetta" con icona stampante 🖨️
- Implementata stessa funzionalità già presente su Commesse Aperte:
  - Dialog preview etichetta con foto articolo da API ERP
  - Validazione campi obbligatori (Peso, Fili, Grammatura, Anime)
  - Stampa etichetta con dati completi o vuota
  - Redirect ad anime se dati mancanti

### File Modificati
- `MESManager.Infrastructure/Services/PlcAppService.cs` - Filtro IP macchine
- `MESManager.Web/wwwroot/lib/ag-grid/plc-storico-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/js/articoli-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/js/clienti-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` - Fix doppio clic
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` - Colonna stampa etichetta
- `MESManager.Web/Components/Pages/Programma/ProgrammaMacchine.razor` - Logica stampa etichetta

---

## v1.13 (29 Gennaio 2026)
**Data:** 29 Gen 2026 16:10 UTC  
**Status:** ✅ Deploy completato e verificato su 192.168.1.230  
**Commit:** `83fe425`

### Modifiche
- ✅ **Imballo Descrizione**: Ora mostra la descrizione invece del numero nelle griglie
- ✅ **Colonne Anime Unificate**: Creato file `anime-columns-shared.js` come fonte unica per entrambe le griglie
- ✅ **Nuovi campi CommessaDto**: Aggiunti `ImballoDescrizione`, `MacchineSuDisponibiliDescrizione`, `Figure`, `Maschere`, `Assemblata`, `ArmataL`, `TogliereSparo`
- ✅ **CommessaAppService**: Aggiornato per popolare le descrizioni dai lookup tables
- ✅ **No code duplication**: Commesse Aperte e Programma Macchine usano la stessa fonte per le colonne anime
- ✅ **Fix Ordine Colonne Stampa**: La stampa ora rispetta l'ordine delle colonne come visualizzato nella griglia
- ✅ **Fix Refresh Commesse Chiuse**: Dopo spostamento frecce, non appaiono più commesse chiuse da Mago

### Dettaglio Fix Stampa
**Problema:** L'ordine delle colonne nella stampa non corrispondeva all'ordine impostato dall'utente nella griglia.

**Causa:** Le funzioni di stampa (`printViaIframe`, `printInNewWindow`, `generatePrintTable`) usavano `gridApi.getColumns()` che restituisce le colonne nell'ordine della definizione originale, non nell'ordine attuale dopo drag & drop.

**Soluzione:** Sostituito `gridApi.getColumns()` con `gridApi.getAllDisplayedColumns()` che rispetta l'ordine attuale delle colonne come visualizzato dall'utente.

### Dettaglio Fix Refresh Commesse Chiuse
**Problema:** Dopo aver spostato una commessa con le frecce ▲▼, apparivano commesse con `stato = "Chiusa"` (chiuse da Mago), che poi sparivano premendo "Aggiorna".

**Causa:** La funzione JS `refreshGridData()` chiamava `/api/Commesse` e filtrava solo per `statoProgramma !== 'Archiviata'`, ma NON controllava `stato === 'Aperta'`.

**Soluzione:** Aggiunto filtro `c.stato === 'Aperta'` in `refreshGridData()` per allineare il comportamento con il caricamento iniziale Blazor.

**Regola acquisita:** Quando si filtra dati in più punti (Blazor + JS), **i filtri devono essere identici** per evitare discrepanze.

### File Modificati
- `MESManager.Application/DTOs/CommessaDto.cs` - Aggiunti 7 campi mancanti
- `MESManager.Infrastructure/Services/CommessaAppService.cs` - Popolamento descrizioni
- `MESManager.Web/wwwroot/lib/ag-grid/anime-columns-shared.js` - NUOVO: colonne condivise
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` - Usa colonne condivise
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` - Usa colonne condivise + fix stampa + fix refresh
- `MESManager.Web/Components/App.razor` - Caricamento script condiviso
- `MESManager.Web/Controllers/PlcController.cs` - Rimosso warning variabile non usata

### Comandi Deploy Usati
```powershell
# Build
dotnet build MESManager.sln -c Release --nologo

# Publish
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Stop servizio
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Copia file (esclude config sensibili)
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager\Web" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs

# Avvia servizio
$secpwd = ConvertTo-SecureString "A123456!" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("Administrator", $secpwd)
Invoke-WmiMethod -ComputerName 192.168.1.230 -Credential $cred -Class Win32_Process -Name Create -ArgumentList "C:\MESManager\Web\MESManager.Web.exe", "C:\MESManager\Web"

# Verifica
Invoke-WebRequest -Uri "http://192.168.1.230:5156" -UseBasicParsing -TimeoutSec 10
```

---

## v1.12 (29 Gennaio 2026)
**Data:** 29 Gen 2026  
**Status:** ✅ Commit `aa4f926`

### Correzioni
- ✅ **Column State Persistence**: Stato colonne salvato da Blazor su DB, JS ora legge da Blazor
- ✅ **Arrow Buttons Fix**: Frecce per riordino commesse funzionanti dopo refresh

---

## v1.11 (29 Gennaio 2026)
**Data:** 29 Gen 2026 10:52 UTC  
**Status:** ✅ Deploy completato su 192.168.1.230  
**Versione mostrata:** Angolo basso-destra pagina

### Correzioni Critiche
- ✅ **Machine Number Selection**: Selezione macchina in "Commesse Aperte" ora salva con formato M001
- ✅ **Database Standardization**: Tutte le anime e commesse aggiornate a formato M001
- ✅ **AG Grid Editor**: Integrato `agSelectCellEditor` nativo per dropdown macchina
- ✅ **Drag & Drop**: Riordino commesse sulla stessa macchina funzionante
- ✅ **UX Fix**: Disabilitato `user-select` al doppio-click (non seleziona più testo)

### File Modificati
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` (34013 bytes)
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` (51809 bytes)
- `MESManager.Web/Components/Layout/MainLayout.razor` (+versione v1.11)
- SQL: `scripts/utilities/fix-numero-macchina-standard.sql`

### Comandi Deploy Usati
```powershell
# Build
dotnet build MESManager.sln -c Release --nologo

# Publish
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Stop
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Copy (CRITICO: escludere appsettings.Secrets.json e appsettings.Database.json)
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups

# Start
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
```

### Database SQL
```sql
-- Eseguito su MESManager_Prod
-- Script: fix-numero-macchina-standard.sql
-- Aggiornati: 897 anime, 19 commesse
```

### Insegnamenti Acquisiti
🔴 **Errore 1:** Copiare file in `C:\MESManager\Web\wwwroot\` (sbagliato)  
✅ **Soluzione:** Copiare SEMPRE in `C:\MESManager\wwwroot\` (ROOT)

🔴 **Errore 2:** Sovrascrivere `appsettings.Database.json` e `appsettings.Secrets.json`  
✅ **Soluzione:** Usare `robocopy /XF` per escludere file critici

🔴 **Errore 3:** Non incrementare versione prima del deploy  
✅ **Soluzione:** Modificare MainLayout.razor prima di compilare

---

## Formato Versioning

- **Pattern:** `vX.Y`
- **X (Major):** Cambio architettura/schema database (raro)
- **Y (Minor):** Incrementa ad ogni deploy

### Prossimo Deploy
Quando farai il prossimo aggiornamento:
1. Cambia `v1.11` → `v1.12` in MainLayout.razor
2. Aggiungi sezione qui in CHANGELOG.md
3. Esegui script deploy completo
4. Comunica versione ai clienti

