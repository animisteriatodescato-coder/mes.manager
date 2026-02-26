# 04 - Architettura

> **Scopo**: Struttura del sistema, Clean Architecture, servizi e integrazioni

---

## 🏗️ Clean Architecture

### Layering

```
┌─────────────────────────────────────────────────────────┐
│                  MESManager.Web                         │
│              (Blazor Server + Controllers)              │
│  - UI Components (Razor)                                │
│  - Controllers (API)                                    │
│  - SignalR Hubs                                         │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│              MESManager.Application                     │
│                  (Business Logic)                       │
│  - Application Services (AppService)                    │
│  - DTOs (Data Transfer Objects)                         │
│  - Interfaces                                           │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│              MESManager.Infrastructure                  │
│              (Data Access + External)                   │
│  - EF Core DbContext                                    │
│  - Repositories                                         │
│  - Migrations                                           │
│  - External Services (Email, File, etc.)                │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│                MESManager.Domain                        │
│                   (Core Logic)                          │
│  - Entities                                             │
│  - Enums                                                │
│  - Constants                                            │
│  - Domain Logic (no dependencies!)                      │
└─────────────────────────────────────────────────────────┘
```

**Regole**:
- Domain **non** dipende da nessuno
- Application dipende **solo** da Domain
- Infrastructure dipende da Domain e Application
- Web dipende da tutti (presentation layer)

---

## 🧩 Progetti e Responsabilità

### MESManager.Domain

**Scopo**: Entità core business senza dipendenze esterne

```
Entities/
├── Commessa.cs
├── Articolo.cs
├── Macchina.cs
├── PLCRealtime.cs
├── PLCStorico.cs
├── UtenteApp.cs
└── ...

Enums/
├── StatoCommessa.cs
├── TipoEvento.cs
└── ...

Constants/
└── AppConstants.cs
```

**Nessuna dipendenza NuGet!**

---

### MESManager.Application

**Scopo**: Logica applicativa e orchestrazione

```
Services/
├── CommessaAppService.cs
├── MacchinaAppService.cs
├── PlcAppService.cs
├── PianificazioneService.cs
└── ...

DTOs/
├── CommessaDto.cs
├── MacchinaDto.cs
├── PlcRealtimeDto.cs
└── ...

Interfaces/
├── ICommessaAppService.cs
├── IMacchinaAppService.cs
└── ...
```

**Dipendenze NuGet**:
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Options`
- `EPPlus` (export Excel)

---

### MESManager.Infrastructure

**Scopo**: Accesso dati e servizi esterni

```
Data/
└── MesManagerDbContext.cs

Repositories/
├── CommessaRepository.cs
├── MacchinaRepository.cs
└── ...

Migrations/
├── 20260101_Initial.cs
├── 20260203_AddFestiviTable.cs
└── ...

Services/
├── FileService.cs
├── EmailService.cs
└── ...
```

**Dipendenze NuGet**:
- `Microsoft.EntityFrameworkCore` (8.0.11)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `EPPlus`

---

### MESManager.Web

**Scopo**: UI e API HTTP

```
Components/
├── Layout/
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── Pages/
│   ├── Produzione/
│   ├── Programma/
│   ├── Cataloghi/
│   ├── Statistiche/
│   └── Impostazioni/
└── Shared/
    └── UserColorIndicator.razor

Controllers/
├── PianificazioneController.cs
├── PlcController.cs
├── CommesseController.cs
└── ...

wwwroot/
├── js/
│   ├── gantt/
│   ├── ag-grid/
│   └── ...
├── css/
└── lib/
```

**Dipendenze NuGet**:
- `MudBlazor` (8.x)
- `Syncfusion.Blazor.Gantt` (32.1.23)
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`

---

### MESManager.Worker

**Scopo**: Sync batch con ERP Mago

```
Services/
└── MagoSyncService.cs

Program.cs
appsettings.json
```

**Dipendenze NuGet**:
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Hosting.WindowsServices`
- Riferimento a `MESManager.Sync`

---

### MESManager.PlcSync

**Scopo**: Comunicazione real-time con PLC Siemens

```
Services/
├── PlcConnectionService.cs
├── PlcReaderService.cs
└── PlcSyncService.cs

Configuration/
└── machines/
    ├── macchina_002.json
    ├── macchina_003.json
    └── ...

Worker.cs
Program.cs
appsettings.json
```

**Dipendenze NuGet**:
- `Sharp7` (1.1.84) - Driver Siemens S7
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Hosting.WindowsServices`

---

### MESManager.Sync

**Scopo**: Libreria condivisa per integrazione ERP

```
Services/
├── MagoDataService.cs
└── SyncEngine.cs

Repositories/
└── MagoRepository.cs

Configuration/
└── MagoConfig.cs
```

**Dipendenze NuGet**:
- `Microsoft.Data.SqlClient`
- `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## 🔌 Integrazioni

### PLC Siemens S7 (Sharp7)

**Flow Lettura (esistente)**:
```
PlcSync Worker (polling 1s)
    ↓
Sharp7 → TCP/IP → PLC (192.168.17.xx)
    ↓
Legge DB55 (offset configurabili)
    ↓
Scrive in PLCRealtime (DB SQL)
    ↓
Archivia in PLCStorico (ogni cambio stato)
```

**Flow Scrittura Ricette (v1.34.0)**:
```
Evento: Cambio Barcode in DB55
    ↓
RecipeAutoLoaderWorker (listener)
    ↓
RecipeAutoLoaderService (business logic)
    ↓
PlcRecipeWriterService (Sharp7 write)
    ↓
Scrive ricetta in DB52 → PLC Macchina
```

**Servizi PLC**:
| Servizio | Layer | Responsabilità |
|----------|-------|----------------|
| `PlcSyncService` | PlcSync | Polling lettura DB55 |
| `PlcRecipeWriterService` | Infrastructure | Scrittura DB52, Lettura DB55/DB52 |
| `RecipeAutoLoaderService` | Infrastructure | Logic auto-caricamento ricette |
| `RecipeAutoLoaderWorker` | Worker | Event listener BackgroundService |

**Configurazione**:
- IP macchina: Database (tabella Macchine)
- Offset memoria: File JSON (Configuration/machines/)
- Polling interval: appsettings.json (PlcSync)

**Data Blocks**:
- **DB55** (Read): Stato macchina real-time (cicli, barcode, stato)
- **DB52** (Write): Ricetta da eseguire (codice articolo, parametri)

---

### ERP Mago (SQL Direct)

**Flow**:
```
Worker Service (polling 5 min)
    ↓
SQL Query → MagoDB (192.168.1.72)
    ↓
Sync Ordini/Commesse/Articoli
    ↓
Scrive in MESManagerDb (locale)
```

**Tabelle sincronizzate**:
- Ordini → Commesse
- Articoli → Articoli
- Clienti → Clienti

**Modalità**: One-way (Mago → MES)

---

### SignalR (Real-time)

**Hub**: `ProductionHub.cs`

**Eventi**:
- `OnPlcDataUpdated` - Dati PLC aggiornati
- `OnMachineStatusChanged` - Cambio stato macchina
- `OnCommessaUpdated` - Commessa modificata

**Subscriber**:
- Dashboard Produzione
- PLC Realtime
- Gantt Macchine (auto-refresh)

---

## 🗄️ Database Schema

### Tabelle Principali

#### Commesse
```sql
CREATE TABLE Commesse (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Codice NVARCHAR(50),
    Descrizione NVARCHAR(500),
    ArticoloId UNIQUEIDENTIFIER,
    NumeroMacchina INT,
    OrdineSequenza INT,
    QuantitaRichiesta INT,
    DataConsegna DATETIME2,
    Stato NVARCHAR(50),
    ...
)
```

#### Macchine
```sql
CREATE TABLE Macchine (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Codice NVARCHAR(10),
    Nome NVARCHAR(100),
    IndirizzoPLC NVARCHAR(50),
    AttivaInGantt BIT,
    ...
)
```

#### PLCRealtime
```sql
CREATE TABLE PLCRealtime (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    MacchinaId UNIQUEIDENTIFIER,
    CicliFatti INT,
    Stato NVARCHAR(50),
    Barcode NVARCHAR(100),
    UltimoAggiornamento DATETIME2,
    ...
)
```

#### PLCStorico
```sql
CREATE TABLE PLCStorico (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    MacchinaId UNIQUEIDENTIFIER,
    CicliFatti INT,
    Stato NVARCHAR(50),
    Timestamp DATETIME2,
    ...
)
```

---

## 🚦 Servizi Windows

### Ordine di Avvio/Stop

**CRITICO**: Rispettare ordine per evitare problemi!

#### Stop (durante deploy)
```
1. PlcSync     (libera connessioni PLC)
2. Worker      (ferma sync Mago)
3. Web         (disconnette utenti)
```

#### Start (dopo deploy)
```
1. Web         (avvia interfaccia)
2. Worker      (riprende sync)
3. PlcSync     (riconnette PLC)
```

**Motivo**: PlcSync deve essere ultimo a partire per evitare slot PLC occupati prima che Web/Worker siano pronti.

---

### MESManager.Web

**Ruolo**: Interfaccia utente Blazor Server

**Porta**: 5156 (HTTP)

**Dipendenze**:
- Database SQL (MESManagerDb)
- File wwwroot (JS/CSS)

**Impatto riavvio**: Disconnette utenti; SignalR tenta riconnessione automatica.

---

### MESManager.Worker (Sync Mago)

**Ruolo**: Sincronizzazione batch con ERP

**Interval**: 5 minuti (configurabile)

**Dipendenze**:
- MagoDb (192.168.1.72)
- MESManagerDb (locale)

**Impatto riavvio**: Ritardo sync, nessuna perdita dati (riconcilia al riavvio).

---

### MESManager.PlcSync

**Ruolo**: Comunicazione real-time con PLC

**Polling**: 1 secondo (configurabile)

**Dipendenze**:
- PLC Siemens (192.168.17.xx)
- Database (tabella Macchine per IP)
- File JSON (Configuration/machines/ per offset)

**Impatto riavvio**: 
- Connessioni S7 "appese" se shutdown brusco
- Possibile consumo slot PLC (limite ~32)
- Timeout connessioni dopo 2-3 minuti

**Best practice**: Sempre graceful shutdown!

---

## 🔒 Sicurezza

### Identity

**Provider**: ASP.NET Core Identity

**Tabelle**:
- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`

**Ruoli predefiniti**:
- Admin
- Produzione
- Ufficio
- Manutenzione
- Visualizzazione

---

### Autorizzazione

**Policy-based**:
```csharp
[Authorize(Roles = "Admin,Produzione")]
public class CommesseController : Controller
{
    ...
}
```

**Razor**:
```razor
<AuthorizeView Roles="Admin">
    <Authorized>
        <button>Elimina</button>
    </Authorized>
</AuthorizeView>
```

---

### Password Policy

```csharp
options.Password.RequireDigit = true;
options.Password.RequiredLength = 8;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequireLowercase = true;
options.Lockout.MaxFailedAccessAttempts = 5;
```

---

## 📦 Dependency Injection

### Registrazione Servizi

**Program.cs**:
```csharp
// Infrastructure
builder.Services.AddDbContextFactory<MesManagerDbContext>();

// Application Services
builder.Services.AddScoped<ICommessaAppService, CommessaAppService>();
builder.Services.AddScoped<IMacchinaAppService, MacchinaAppService>();
builder.Services.AddScoped<IPlcAppService, PlcAppService>();

// Blazor
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// SignalR
builder.Services.AddSignalR();
```

---

## 🧪 Testing

### End-to-End (E2E)

**Framework**: Playwright

**Progetto**: `tests/MESManager.E2E`

**Test**:
```csharp
[Test]
public async Task Login_WithValidCredentials_Success()
{
    await Page.GotoAsync("http://localhost:5156");
    await Page.FillAsync("#username", "admin");
    await Page.FillAsync("#password", "Admin123!");
    await Page.ClickAsync("button[type=submit]");
    await Expect(Page).ToHaveURLAsync("http://localhost:5156/");
}
```

---

## 📊 Metriche Performance

### Database

- **Connection pooling**: Abilitato (default EF Core)
- **Retry policy**: 3 tentativi su errori transienti
- **Timeout query**: 30 secondi

### SignalR

- **Keepalive**: 15 secondi
- **Client timeout**: 30 secondi
- **Handshake timeout**: 15 secondi

### PlcSync

- **Polling interval**: 1 secondo (1000ms)
- **Connection timeout**: 5 secondi
- **Retry attempts**: 3

---

## 🆘 Supporto

Per configurazione: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)  
Per deploy: [01-DEPLOY.md](01-DEPLOY.md)  
Per replica sistema: [05-REPLICA-SISTEMA.md](05-REPLICA-SISTEMA.md)  
Per PLC: [07-PLC-SYNC.md](07-PLC-SYNC.md)

---

## 🧩 Pattern di Centralizzazione (v1.50.0+)

### Catalog Grid Pattern

Per aggiungere un **nuovo catalog con AG Grid**, creare:

1. **Grid JS** (`wwwroot/lib/ag-grid/nuovotipo-grid.js`) — solo `columnDefs` + `agGridFactory.setup(...)`:
```js
const columnDefs = [ /* colonne specifiche */ ];
agGridFactory.setup({
    namespace: 'nuovoTipoGrid',
    columnDefs,
    exportFileName: 'export-nuovo-tipo.csv',
    storageKeyBase: 'nuovo-tipo-grid-columnState',
    eventName: 'nuovoTipoGridStatsChanged',
});
```

2. **Razor Page** — usa `@inherits CatalogoGridBase`, 3 righe di identità, solo logica specifica:
```razor
@page "/cataloghi/nuovotipo"
@inherits CatalogoGridBase
@inject HttpClient Http

@code {
    protected override string GridNamespace => "nuovoTipoGrid";
    protected override string SettingsKey   => "nuovo-tipo-grid";
    protected override string PageKey       => "nuovotipo";

    private List<NuovoTipoDto> _items = new();

    protected override async Task OnInitializedAsync()
    {
        PageToolbarService.SetActivePage(PageKey, this);
        _items = await Http.GetFromJsonAsync<List<NuovoTipoDto>>("api/NuovoTipo") ?? new();
        totalRows = _items.Count;
        await LoadSavedSettings();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { await Task.Delay(100); await InitializeGridJs(_items); }
    }
}
```

**Tutto il resto** (settings, stats, search, export, AppBar, column panel, dispose) è gestito da `CatalogoGridBase`.

### File architettura centralizzazione

| File | Scopo |
|------|-------|
| `wwwroot/js/ag-grid-factory.js` | UNICA implementazione logica AG Grid |
| `Components/Shared/GridSettingsPanel.razor` | UI pannello impostazioni condiviso |
| `Components/Pages/Cataloghi/CatalogoGridBase.cs` | Base class C# per tutti i catalog |
| `Models/GridStats.cs` | DTO stats griglia (Total/Filtered/Selected) |
| `Models/GridUiSettings.cs` | Settings UI griglia + `GetDensityPadding()` |

