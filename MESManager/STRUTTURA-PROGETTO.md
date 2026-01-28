# 📐 Schema Struttura Progetto MESManager

**Ultimo aggiornamento:** 28 Gennaio 2026  
**Versione:** Post-Refactoring

---

## 🏗️ Architettura Generale

```
┌──────────────────────────────────────────────────────────────────┐
│                         MESManager                                │
│                    (Clean Architecture)                           │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│   │  Web/API    │  │   PlcSync   │  │   Worker    │  Presentation│
│   │  (Blazor)   │  │  (Console)  │  │ (Background)│              │
│   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
│          │                │                │                      │
│          └────────────────┼────────────────┘                      │
│                           │                                       │
│                    ┌──────▼──────┐                                │
│                    │ Application │  Business Logic                │
│                    │  Services   │                                │
│                    └──────┬──────┘                                │
│                           │                                       │
│          ┌────────────────┼────────────────┐                      │
│          │                │                │                      │
│   ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐              │
│   │   Domain    │  │Infrastructure│  │    Sync    │   Data Layer │
│   │  Entities   │  │ Repositories │  │  (Mago)    │              │
│   └─────────────┘  └─────────────┘  └─────────────┘              │
│                                                                   │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📁 Struttura Cartelle

```
MESManager/
│
├── 📦 MESManager.Domain/              # Entità di dominio e costanti
│   ├── Constants/
│   │   ├── LookupTables.cs           # ⭐ Tabelle lookup centralizzate (Colla, Vernice, Sabbia, Imballo)
│   │   └── FileConstants.cs          # ⭐ Costanti per gestione file
│   ├── Entities/
│   │   ├── Anima.cs                  # Entità catalogo anime
│   │   ├── Articolo.cs               # Entità articolo
│   │   ├── Cliente.cs                # Entità cliente
│   │   ├── Commessa.cs               # Entità commessa (ordine di produzione)
│   │   ├── Macchina.cs               # Entità macchina
│   │   ├── Operatore.cs              # Entità operatore
│   │   ├── PLCRealtime.cs            # Dati realtime PLC
│   │   ├── PLCStorico.cs             # Storico dati PLC
│   │   ├── UtenteApp.cs              # Utente applicazione
│   │   └── PreferenzaUtente.cs       # Preferenze UI utente
│   └── Enums/
│       └── StatoMacchina.cs          # Enum stati macchina
│
├── 📦 MESManager.Application/         # Logica di business
│   ├── Configuration/
│   │   └── DatabaseConfiguration.cs  # Configurazione connessioni DB
│   ├── DTOs/
│   │   ├── AnimeDto.cs
│   │   ├── ArticoloDto.cs
│   │   ├── ClienteDto.cs
│   │   ├── CommessaDto.cs
│   │   ├── MacchinaDto.cs
│   │   └── ...
│   ├── Interfaces/
│   │   ├── IAnimeService.cs
│   │   ├── IAnimeRepository.cs
│   │   └── ...
│   └── Services/
│       ├── AnimeService.cs           # CRUD catalogo anime
│       ├── AllegatoArticoloService.cs # Gestione allegati con import da Gantt
│       ├── AllegatiAnimaService.cs   # Allegati anime
│       ├── CommessaAppService.cs     # Calcolo durate/date/colori
│       ├── CurrentUserService.cs     # Sessione utente corrente
│       └── ...
│
├── 📦 MESManager.Infrastructure/      # Accesso ai dati
│   ├── Data/
│   │   └── MesManagerDbContext.cs    # Entity Framework DbContext
│   ├── Migrations/                   # EF Core migrations
│   ├── Repositories/
│   │   ├── AnimeRepository.cs
│   │   ├── AllegatoArticoloRepository.cs
│   │   └── ...
│   └── Services/
│       ├── ArticoloService.cs        # CRUD articoli
│       ├── MacchinaService.cs        # CRUD macchine
│       ├── CommessaService.cs        # CRUD commesse
│       ├── ClienteService.cs         # CRUD clienti
│       ├── OperatoreService.cs       # CRUD operatori
│       ├── PlcAppService.cs          # Dati realtime/storico PLC
│       ├── PlcStatusService.cs       # Status servizio PlcSync
│       ├── PlcSyncCoordinatorService.cs # Sync manuale PLC
│       ├── CalendarioLavoroService.cs # Calendario produzione
│       ├── PianificazioneService.cs  # Impostazioni Gantt
│       ├── UtenteAppService.cs       # Utenti applicazione
│       ├── PreferenzaUtenteService.cs # Preferenze utente
│       └── ExcelImportService.cs     # Import anime da Excel
│
├── 📦 MESManager.Web/                 # Applicazione Blazor Server + API
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor      # Layout principale con menu
│   │   │   └── NavMenu.razor         # Menu di navigazione
│   │   ├── Pages/
│   │   │   ├── Cataloghi/            # Pagine gestione cataloghi
│   │   │   │   ├── CatalogoAnime.razor      # ✅ Griglia AG-Grid anime
│   │   │   │   ├── CatalogoArticoli.razor   # ✅ Griglia articoli
│   │   │   │   ├── CatalogoClienti.razor    # ✅ Griglia clienti
│   │   │   │   ├── CatalogoCommesse.razor   # ✅ Griglia commesse
│   │   │   │   ├── CatalogoFoto.razor       # ⚠️ Stub
│   │   │   │   └── CatalogoRicette.razor    # ⚠️ Stub
│   │   │   ├── Programma/            # Programmazione produzione
│   │   │   │   ├── CommesseAperte.razor     # ✅ Lista commesse aperte
│   │   │   │   ├── ProgrammaMacchine.razor  # ✅ Assegnazione macchine
│   │   │   │   └── GanttMacchine.razor      # ✅ Vista Gantt
│   │   │   ├── Produzione/           # Monitor produzione
│   │   │   │   ├── DashboardProduzione.razor # ✅ Dashboard macchine
│   │   │   │   ├── PlcRealtime.razor        # ✅ Dati realtime PLC
│   │   │   │   ├── PlcStorico.razor         # ✅ Storico dati PLC
│   │   │   │   └── Incollaggio.razor        # ⚠️ Stub
│   │   │   ├── Pianificazione/       # Pianificazione
│   │   │   │   └── Pianificazione.razor     # ✅ Impostazioni Gantt
│   │   │   ├── Impostazioni/         # Configurazioni
│   │   │   │   ├── GestioneOperatori.razor  # ✅ CRUD operatori
│   │   │   │   ├── GestioneUtenti.razor     # ✅ Gestione utenti
│   │   │   │   ├── ImpostazioniGantt.razor  # ✅ Config macchine/calendario
│   │   │   │   ├── CalendarioProduzione.razor # ⚠️ Parziale
│   │   │   │   ├── ImpostazioniGenerali.razor # ⚠️ Stub
│   │   │   │   └── ImpostazioniTabelle.razor  # ⚠️ TODO
│   │   │   ├── Sync/                 # Sincronizzazione
│   │   │   │   ├── SyncMago.razor           # ✅ Sync con ERP Mago
│   │   │   │   ├── SyncGantt.razor          # ✅ Import da Gantt/Excel
│   │   │   │   ├── SyncMacchine.razor       # ✅ Controllo PlcSync
│   │   │   │   └── SyncGoogle.razor         # ⚠️ Stub
│   │   │   └── Home.razor            # Home page
│   │   ├── Dialogs/
│   │   │   └── AnimeEditDialog.razor # Dialog modifica anime
│   │   └── Shared/
│   │       ├── FixResetMenu.razor    # Menu fix/reset griglia
│   │       └── UserSelector.razor    # Selezione utente
│   ├── Controllers/                  # API REST
│   │   ├── AnimeController.cs        # /api/Anime
│   │   ├── ArticoliController.cs     # /api/Articoli
│   │   ├── ClientiController.cs      # /api/Clienti
│   │   ├── CommesseController.cs     # /api/Commesse
│   │   ├── MacchineController.cs     # /api/Macchine
│   │   ├── OperatoriController.cs    # /api/Operatori
│   │   ├── PlcController.cs          # /api/Plc
│   │   ├── SyncController.cs         # /api/Sync
│   │   ├── PianificazioneController.cs # /api/Pianificazione
│   │   ├── AllegatoArticoloController.cs # /api/AllegatiArticolo
│   │   ├── AllegatiAnimaController.cs    # /api/AllegatiAnima
│   │   ├── TabelleController.cs      # /api/Tabelle (usa LookupTables)
│   │   └── UtentiController.cs       # /api/Utenti
│   ├── Services/
│   │   ├── RealtimeStateService.cs   # Polling + SignalR
│   │   ├── PreferencesService.cs     # Preferenze localStorage/DB
│   │   ├── PlcHttpClientService.cs   # Client HTTP per PLC
│   │   ├── PageToolbarService.cs     # Toolbar dinamica
│   │   └── AppBarContentService.cs   # Contenuto AppBar
│   ├── wwwroot/
│   │   ├── css/                      # Stili
│   │   └── lib/ag-grid/              # JavaScript per AG-Grid
│   │       ├── anime-grid.js
│   │       ├── articoli-grid.js
│   │       ├── clienti-grid.js
│   │       ├── commesse-grid.js
│   │       ├── commesse-aperte-grid.js
│   │       ├── programma-macchine-grid.js
│   │       ├── gantt-macchine.js
│   │       └── plc-storico-grid.js
│   └── Program.cs                    # Entry point + DI
│
├── 📦 MESManager.PlcSync/             # Worker sincronizzazione PLC
│   ├── Services/
│   │   ├── PlcConnectionService.cs   # Connessione S7.Net
│   │   ├── PlcDataService.cs         # Lettura variabili PLC
│   │   ├── PlcSnapshotService.cs     # Sync PLC → DB
│   │   └── PlcStatusUpdater.cs       # Status servizio su DB
│   ├── PlcSyncWorker.cs              # Background worker
│   └── Program.cs
│
├── 📦 MESManager.Sync/                # Sincronizzazione ERP Mago
│   ├── Services/
│   │   ├── SyncCoordinator.cs        # Orchestrazione sync
│   │   ├── CommessaSyncService.cs    # Sync commesse
│   │   ├── ArticoloSyncService.cs    # Sync articoli
│   │   └── ClienteSyncService.cs     # Sync clienti
│   ├── SyncMagoWorker.cs             # Background worker
│   └── Program.cs
│
├── 📦 MESManager.Worker/              # Worker servizi background
│   ├── SyncMagoBackgroundService.cs
│   └── Program.cs
│
├── 📦 MESManager.E2E/                 # Test End-to-End
│
├── 📦 TestMagoConnection/             # Utility test connessione Mago
│
├── 📁 scripts/                        # Script di supporto organizzati
│   ├── deploy/                       # Deploy e pubblicazione
│   │   ├── deploy-production.ps1
│   │   ├── publish-win.ps1
│   │   ├── restart-services.ps1
│   │   └── migrate-database-to-production.ps1
│   ├── setup/                        # Setup iniziale
│   │   ├── create-fab-user.ps1
│   │   ├── create-fab-user.sql
│   │   ├── create-prod-secrets.ps1
│   │   ├── protect-secrets.ps1
│   │   ├── insert-plc-configurations.sql
│   │   ├── insert-plc-machines.sql
│   │   └── migration_plc_status.sql
│   ├── diagnostics/                  # Test e diagnostica
│   │   ├── test-database-config.ps1
│   │   ├── test-sql-connection.ps1
│   │   ├── test-sync-commesse.ps1
│   │   └── verifica-*.ps1
│   ├── utilities/                    # Utilità varie
│   │   ├── sync-preferenze-utenti.ps1
│   │   ├── export-preferenze-localstorage.ps1
│   │   ├── copy-data-from-production.ps1
│   │   ├── import-anime.ps1
│   │   └── fix-all-machines-correct-codes.ps1
│   ├── maintenance/                  # Manutenzione sistema
│   │   ├── repair-sqlserver.ps1
│   │   ├── reinstall-sqlserver.ps1
│   │   ├── add-plc-ip.bat
│   │   └── remove-plc-ip.bat
│   └── sql-data/                     # Script SQL dati
│       ├── test-data-plc.sql
│       ├── verifica-*.sql
│       └── ...
│
├── 📁 docs/                           # Documentazione
│   ├── DATABASE-CONFIG-README.md     # Configurazione database
│   ├── DEPLOY-README.md              # Guida deploy
│   ├── DEPLOY-SAFE-GUIDE.md          # Deploy sicuro
│   ├── SECURITY-CONFIG.md            # Configurazione sicurezza
│   ├── PREFERENZE-UTENTE-IMPLEMENTAZIONE.md
│   ├── GanttAnalysis.md              # Analisi struttura Gantt
│   └── storico/                      # Report storici archiviati
│       ├── DIAGNOSTIC_REPORT.md
│       ├── FIX_IMPLEMENTED.md
│       └── ...
│
├── 📁 publish/                        # Output pubblicazione
│
├── 📁 wwwroot/                        # Static files condivisi
│
├── 📄 MESManager.sln                  # Solution file
├── 📄 README.md                       # Documentazione principale
├── 📄 appsettings.Database.json       # Config database centralizzata
├── 📄 appsettings.Secrets.json.template
├── 📄 start-web-5156.cmd              # Avvio rapido Web
├── 📄 start-worker.cmd                # Avvio rapido Worker
└── 📄 start-plcsync.cmd               # Avvio rapido PlcSync
```

---

## 🔄 Flusso Dati

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   ERP Mago  │◄────►│   Worker    │◄────►│  SQL Server │
│  (Esterno)  │ Sync │  (Sync)     │      │   (Locale)  │
└─────────────┘      └─────────────┘      └──────┬──────┘
                                                  │
┌─────────────┐      ┌─────────────┐              │
│  PLC Siemens│◄────►│   PlcSync   │◄─────────────┤
│  (S7-1200)  │ S7Net│  (Worker)   │              │
└─────────────┘      └─────────────┘              │
                                                  │
┌─────────────┐      ┌─────────────┐              │
│   Browser   │◄────►│  Blazor Web │◄─────────────┘
│   Client    │ HTTP │   + API     │
└─────────────┘      └─────────────┘
```

---

## 📊 Database Schema Principale

```sql
-- Entità principali
Anime             -- Catalogo anime (schede prodotto)
Articoli          -- Catalogo articoli
Clienti           -- Anagrafica clienti
Commesse          -- Ordini di produzione
Macchine          -- Macchine produzione (11 totali)
Operatori         -- Operatori di linea

-- Entità PLC
PLCRealtime       -- Dati realtime (snapshot corrente)
PLCStorico        -- Storico produzione
PlcServiceStatus  -- Status servizio PlcSync
PlcSyncLogs       -- Log sincronizzazioni

-- Entità utente
UtentiApp         -- Utenti applicazione
PreferenzeUtente  -- Preferenze UI per utente/pagina

-- Allegati
AllegatiArticoli  -- Foto e documenti allegati
```

---

## 🎨 Tecnologie Utilizzate

| Componente | Tecnologia |
|------------|------------|
| **Frontend** | Blazor Server, MudBlazor, AG-Grid |
| **Backend** | ASP.NET Core 8, Entity Framework Core |
| **Database** | SQL Server Express |
| **PLC** | S7.Net (Siemens S7-1200/1500) |
| **Gantt** | Vis-Timeline, Syncfusion |
| **Build** | .NET SDK, dotnet publish |

---

## 🚀 Avvio Rapido

```powershell
# Sviluppo
dotnet run --project MESManager/MESManager.Web

# Produzione
.\start-web-5156.cmd
.\start-worker.cmd
.\start-plcsync.cmd
```

---

## ✅ Refactoring Completato

| Azione | Stato |
|--------|-------|
| File log eliminati | ✅ |
| Template Blazor rimossi | ✅ |
| Codice duplicato centralizzato | ✅ |
| Console.WriteLine rimossi | ✅ |
| Script organizzati | ✅ |
| Documenti archiviati | ✅ |
| Compilazione verificata | ✅ |

**Linee di codice debug rimosse:** ~90  
**File eliminati/spostati:** ~50  
**Classi centralizzate create:** 2 (`LookupTables`, `FileConstants`)
