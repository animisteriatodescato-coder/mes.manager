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
│   ├── MainLayout.razor         ← applica CSS vars via ThemeCssService
│   └── NavMenu.razor
├── Pages/
│   ├── Produzione/
│   ├── Programma/
│   ├── Cataloghi/
│   ├── Statistiche/
│   └── Impostazioni/
│       └── ImpostazioniGenerali.razor  ← draft pattern + live preview
└── Shared/
    ├── UserColorIndicator.razor
    └── ColorTokenPicker.razor    ← picker tema riusabile (v1.55.12)

Controllers/
├── PianificazioneController.cs
├── PlcController.cs
├── CommesseController.cs
└── ...

Services/
├── AppSettingsService.cs         ← Singleton, impostazioni globali + Clone()
├── UserThemeService.cs           ← Scoped, preferenze per-utente
├── ThemeModeService.cs           ← Scoped, toggle dark/light
├── ThemeCssService.cs            ← Scoped, AppSettings→CSS vars (v1.55.12)
└── ...

wwwroot/
├── js/
│   ├── gantt/
│   ├── ag-grid/
│   ├── theme-vars.js             ← window.mesTheme.apply() (v1.55.12)
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

**Gestione conflitti concurrency** (v1.58.0):
- `Commessa` ha `RowVersion` (EF optimistic concurrency)
- Al conflitto durante `SaveChanges`: `SaveChangesWithConcurrencyRetryAsync` tenta fino a 3 volte, aggiornando `OriginalValues` dal DB corrente

**Gestione orfani** (v1.58.0):
- Ogni sync verifica se ci sono commesse `Aperte` nel DB il cui `Codice` non esiste più in Mago
- Le orfane vengono chiuse (`Stato = Chiusa`) automaticamente
- Caso tipico: ordine eliminato/chiuso in Mago senza che il sync precedente lo abbia rilevato

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

// Theme System (v1.55.12)
builder.Services.AddSingleton<AppSettingsService>();      // impostazioni globali
builder.Services.AddScoped<UserThemeService>();           // preferenze per-utente
builder.Services.AddScoped<IThemeModeService, ThemeModeService>(); // dark/light
builder.Services.AddScoped<ThemeCssService>();           // AppSettings → CSS vars

// Tabelle Lookup (v1.60.30)
builder.Services.AddSingleton<ITabelleService, TabelleService>(); // persistenza JSON

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

### TabelleService — Lookup Tables con persistenza (v1.60.30)

**Problema risolto**: `LookupTables.cs` era statico e hardcoded — nessuna modifica sopravviveva al riavvio.

**Pattern**:
```
ITabelleService (singleton)
  └── TabelleService
        ├── Carica da: {ContentRootPath}/tabelle-config.json  [se esiste]
        ├── Fallback:  LookupTables default hardcoded
        ├── Al salvataggio: scrive JSON + chiama LookupTables.Aggiorna()
        └── LookupTables static resta fonte di verità per AnimeService/CommessaAppService
```

**Regola**: MAI leggere `LookupTables.Colla` direttamente nei Controller/Razor. Usare `ITabelleService.GetCollaList()` via DI o via `GET /api/Tabelle/colla`.

**API endpoints**:
- `GET  /api/Tabelle/{colla|vernice|sabbia|imballo}` → lista lookup
- `POST /api/Tabelle/{colla|vernice|sabbia|imballo}` → salva (persiste su JSON + aggiorna static)

**File persistenza**: `tabelle-config.json` in `ContentRootPath` (escluso dal deploy con `/XF *.json` nella configurazione robocopy).

---

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

---

## 🎨 Sistema Tema (v1.60.2+)

### Architettura CSS vars

Tutti i colori UI passano attraverso CSS custom properties `--mes-*` definite su `:root`. Nessun valore colore hardcodato nei componenti Razor (zero `style="background:@variable"`).

```
AppSettings (file JSON o DB per-utente)
    ↓
ThemeCssService.BuildVars(settings, isDarkMode)
    → Dictionary<string,string>  (es. "--mes-primary" → "#136724ff")
    ↓
ThemeCssService.ApplyAsync(IJSRuntime, settings, isDarkMode)
    → window.mesTheme.apply(vars)  [theme-vars.js]
    ↓
document.documentElement.style.setProperty("--mes-primary", "#136724ff")
    ↓
CSS: .mud-appbar { background: var(--mes-appbar-bg) !important; }
```

### Regola: nessun colore hardcodato

| ❌ Prima | ✅ Dopo |
|---------|--------|
| `style="background:@_settings.ThemePrimaryColor"` | `style="background:var(--mes-primary)"` |
| `@(_isDarkMode ? "#1a1a2e" : "#f5f5f5")` | `var(--mes-readonly-cell-bg)` |
| `@if (_bgActive) { <style>...</style> }` | Sempre applicato, `.mes-has-bg` per glass |

### Servizi tema

| Servizio | Lifetime | Responsabilità |
|----------|----------|----------------|
| `AppSettingsService` | Singleton | Lettura/scrittura JSON, evento `OnSettingsChanged` |
| `UserThemeService` | Scoped | Preferenze per-utente (DB + localStorage) |
| `IThemeModeService` | Scoped | Toggle dark/light, evento `OnThemeChanged` |
| `ThemeCssService` | Scoped | **Unica fonte verità** AppSettings→CSS vars |

### Flusso cambio tema (impostazioni)

```
Utente modifica colore in ImpostazioniGenerali
    ↓
_draft (copia di lavoro) aggiornato
    ↓
ApplyPreviewAsync() → ThemeCssService.ApplyAsync(JS, _draft, ThemeModeService.IsDarkMode)
                                                             ↑
                                              USA IL LIVE STATE, non _draft.ThemeIsDarkMode
    ↓
CSS vars aggiornate in real-time (senza re-render Blazor)
    ↓
Utente clicca "Salva" → UserThemeService.SaveUserThemeAsync(_draft)
    ↓
OnUserThemeChanged → ThemeCssService.ApplyAsync (persist)
```

### Flusso toggle dark/light (MainLayout)

```
Utente preme ☀️/🌙 in AppBar
    ↓
ToggleTheme():
  1. _isDarkMode = !_isDarkMode
  2. ThemeModeService.UpdateMode(_isDarkMode)     ← notifica tutti i subscriber
  3. effectiveSettings = UserThemeService.GetEffectiveSettings()
     effectiveSettings.ThemeIsDarkMode = _isDarkMode
  4. SE HasUserTheme → UserThemeService.SaveUserThemeAsync(effectiveSettings)
     ALTRIMENTI      → AppSettingsService.SaveSettingsAsync(globalSettings)
  5. ThemeCssService.ApplyAsync(JS, effectiveSettings, _isDarkMode)

⚠️ REGOLA CRITICA: il salvataggio DEVE avvenire sulle impostazioni effettive
   (utente se HasUserTheme, globali altrimenti).
   NON salvare sempre su AppSettingsService quando l'utente ha un tema personale:
   OnAppSettingsChanged rileggerebbe le impostazioni utente invariate e revertivrebbe
   il toggle immediatamente.
```

### Righe tabelle tema-aware (v1.60.8+)

Le righe zebrate delle tabelle (MudTable + AG Grid) seguono la tinta del colore drawer/appbar:

```
MesDesignTokens.IsSufficientlyChromatic(drawerBg)  → true/false
MesDesignTokens.RowOddFromColor(colorSorgente, isDarkMode)  → --mes-row-odd
MesDesignTokens.RowEvenFromColor(colorSorgente, isDarkMode) → --mes-row-even
```

**Cascade sorgente tinting**: drawer → appbar → `null` (token fisso neutro se entrambi acromatici)
**Il Primary non entra MAI nel tinting delle righe.**

**Algoritmo HSL**: estrae hue dal colore sorgente, applica targetS e targetL fissi.
- Riga odd:  `S = min(s*0.85, 0.55)`, `L = 0.87` (light) o `0.23` (dark)
- Riga even: `S = min(s*0.35, 0.25)`, `L = 0.94` (light) o `0.16` (dark)

**Soglie di attivazione** (entrambe richieste):
- `RowTintSaturationThreshold = 0.22f` — esclude grigi con leggero hue-bias
- `RowTintMinLuminance = 0.15f` — esclude colori quasi-neri come `#0E101C` (L≈0.08, S≈0.33)

**Fallback**: se `IsSufficientlyChromatic` ritorna false, usa token fissi `RowOdd(dark)`/`RowEven(dark)`.

### Split Light/Dark per AppBar e Drawer (v1.60.1+)

AppSettings ha 4 campi colore separati:
- `ThemeAppBarBgColor` / `ThemeDrawerBgColor` — light mode
- `ThemeAppBarBgColorDark` / `ThemeDrawerBgColorDark` — dark mode override (vuoto = riusa light)

Logica centralizzata una sola volta in `ThemeCssService.BuildVars()` e riprodotta identicamente
nel blocco SSR `:root{}` di `MainLayout.razor`. **NON duplicare questa logica altrove.**

### ColorTokenPicker

Componente riusabile `Components/Shared/ColorTokenPicker.razor`:

```razor
<ColorTokenPicker Label="Primario" LabelWidth="130px"
                  Value="@_draft.ThemePrimaryColor"
                  ValueChanged="@OnPrimaryColorChanged"
                  Palette="@FullPalette"
                  ShowAuto="false"
                  ShowHexInput="true" />
```

Parametri principali: `Label`, `Value`/`ValueChanged` (string hex), `Palette` (List<string>), `ShowAuto`, `AutoLabel`, `ShowHexInput`, `FallbackColor`.

---

## 🔌 Pattern Centralizzati — Usa Questi, Non Duplicare

> ⚠️ Esistono già. Usarli è **OBBLIGATORIO**. Reimplementare = bug architetturale. Cerca prima con grep/semantic search → estendi → **mai duplica**.

| Vuoi fare... | Estendi/Usa |
|---|---|
| Nuova griglia catalogo | `@inherits CatalogoGridBase` in `Components/Pages/Cataloghi/` |
| Config JS griglia AG Grid | `wwwroot/js/ag-grid-factory.js` → `agGridFactory.setup({...})` |
| Pannello impostazioni griglia | `<GridSettingsPanel @bind-Settings="settings" />` |
| Servizio allegati per nuova entità | `: AllegatoFileServiceBase` in `Application/Services/` |
| Path di rete / MIME type allegati | `ConvertNetworkPath()` / `GetMimeType()` dalla base |
| Colori tema / dark-light mode | `_theme` / `_isDarkMode` in `MainLayout.razor` → 1 punto |
| **Tema dinamico da immagine** | `ColorExtractionService` → `AppSettingsService.ThemePalette` → `MainLayout.BuildThemeFromSettings()` |
| **Token colori / grafica** | `MesDesignTokens` in `Constants/` → UNICA fonte di verità per tutti i colori hardcoded. MAI scrivere hex direttamente |
| **Dark mode iniettabile** | `IThemeModeService` (Scoped) → inietta nei componenti che reagiscono a dark/light. `UpdateMode()` solo da MainLayout |
| **Testo su sfondo Primary** | `AppSettings.ThemeTextOnPrimary` + `AppSettingsService.ComputeTextOnBackground()` → `--mes-text-on-primary` CSS var |
| **Testo brand su sfondo bianco** | `AppSettings.ThemePrimaryTextColor` + `AppSettingsService.ComputePrimaryTextColor()` → `--mes-primary-text` CSS var |
| **Card sfondo bianco in dark mode** | `color: #1a1a1a !important` + override `.mud-typography` figli — MAI `var(--mud-palette-text-primary)` su card con background hardcoded |
| Preferenze utente persistenti | `IPreferenzeUtenteService` → mai localStorage diretto |
| **Colonna Ricetta in AG Grid** | `ricetta-column-shared.js` → `window.ricettaColumnShared.createColumnDef(config)`. Chip verde `✓ N` = ha ricetta, chip grigio `↓ importa` = cliccabile. Cache-bust in `App.razor` (`?v=NNNN`) |
| **Visualizzare ricetta articolo** | `RicettaViewDialog.razor` — param `CodiceArticolo` + `ShowImportButton=true` per "Importa da Macchina" |
| **Importare ricetta da macchina** | `ImportaRicettaMacchinaDialog.razor` — param `CodiceArticolo`. Usa `GET /api/Macchine` + `POST /api/plc/save-recipe-from-plc`. MAI duplicare. |
| **Azione pericolosa PLC (invia a macchina)** | Sempre `await DialogService.ShowMessageBox(...)` confirm prima — vedi `DashboardProduzione.razor` |
| **Generare PDF per un'entità (Scheda Anima)** | `IAnimePdfService` → `AnimePdfService` in `Application/Services/`. Segue pattern `QuotePdfGenerator`. Controller REST: `GET /api/anime/{id}/pdf`. Pulsante nel TitleContent del dialog → `JS.InvokeVoidAsync("open", $"/api/anime/{id}/pdf", "_blank")`. MAI duplicare la logica di rendering QuestPDF — estendi `AnimePdfService` o crea un nuovo `IXxxPdfService` separato. |

