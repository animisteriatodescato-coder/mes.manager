# Guida Nuova Installazione — MESManager

> Documento operativo: leggilo dall'inizio alla fine prima di toccare qualsiasi file.  
> Tempo stimato: 30–60 minuti.

---

## Prerequisiti

| Componente | Versione minima | Note |
|---|---|---|
| Windows Server / Windows 10+ | — | 64-bit |
| .NET 8 Runtime / SDK | 8.x | [download](https://dotnet.microsoft.com/download/dotnet/8) |
| SQL Server Express | 2019+ | su localhost o server raggiungibile |
| IIS (opzionale) | — | solo se deploy su server, non servizio locale |

---

## Struttura del repository

```
MESManager/
├── MESManager.Web/           ← App principale (Blazor + API)
├── MESManager.Application/   ← Logica business + DTO
├── MESManager.Infrastructure/← Database EF Core + repository
├── MESManager.Domain/        ← Entità + enum
├── MESManager.Worker/        ← Sync ERP Mago (batch)
├── MESManager.PlcSync/       ← Lettura dati PLC Siemens S7
├── MESManager.Sync/          ← Servizi di sincronizzazione
└── docs/                    ← Documentazione tecnica
```

---

## STEP 1 — Copia e configura i file segreti

### 1a) Credenziali database principale

```
Copia: appsettings.Secrets.json.template
In:    appsettings.Secrets.json          ← NON va in git mai
```

Apri `appsettings.Secrets.json` e compila:

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=IP_SERVER\\SQLEXPRESS;Database=MESManager;User Id=UTENTE;Password=PASSWORD;TrustServerCertificate=True;",
    "MagoDb":       "Data Source=IP_MAGO\\SQLEXPRESS;Initial Catalog=NOME_DB_MAGO;User Id=UTENTE;Password=PASSWORD;TrustServerCertificate=True;Connection Timeout=30;",
    "GanttDb":      "Server=IP_SERVER\\SQLEXPRESS;Database=Gantt;User Id=UTENTE;Password=PASSWORD;TrustServerCertificate=True;"
  },
  "Security": {
    "AllowedHosts": "localhost;192.168.1.X",
    "ApiKey": "chiave-api-generata-casualmente"
  }
}
```

> ℹ️ `MagoDb` e `GanttDb` possono anche puntare allo stesso server di `MESManagerDb`.  
> `GanttDb` è usato per importare allegati dal sistema Gantt legacy.

### 1b) Configurazione produzione (percorsi + logging)

File: `MESManager.Web/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "...",   ← stesso di sopra
    "MagoDb": "...",
    "GanttDb": "..."
  },
  "Files": {
    "AllegatiBasePath": "C:\\Dati\\Documenti\\AA SCHEDE PRODUZIONE\\foto cel",
    "PathMappings": [
      "P:\\Documenti->C:\\Dati\\Documenti",
      "P:\\->C:\\Dati\\"
    ]
  },
  "AllowedHosts": "*"
}
```

**`Files.AllegatiBasePath`** = cartella dove vengono salvati i nuovi allegati caricati dagli utenti.  
**`Files.PathMappings`** = mappature da percorsi di rete (es. unità P:) a percorsi locali sul server.  
Formato: `"RETE->LOCALE"`, es. `"P:\\Documenti->C:\\Dati\\Documenti"`

---

## STEP 2 — Database

### 2a) Crea il database

```sql
-- Esegui su SQL Server Management Studio (SSMS)
CREATE DATABASE MESManager;
```

### 2b) Applica le migrations

```powershell
cd MESManager

# Applica tutte le migrations
dotnet ef database update --project MESManager.Infrastructure --startup-project MESManager.Web
```

> Se dotnet ef non è disponibile: `dotnet tool install -g dotnet-ef`

### 2c) Verifica

Dopo le migrations il database deve avere le tabelle principali:
`Macchine`, `Commesse`, `Anime`, `Articoli`, `Clienti`, `Operatori`, `PLCRealtime`, `PLCStorico`, etc.

---

## STEP 3 — Configurazione PLC (se presenti)

File: `MESManager.PlcSync/appsettings.json`

```json
{
  "PlcSync": {
    "PollingIntervalSeconds": 4,
    "PlcDefaults": {
      "Rack": 0,
      "Slot": 1,
      "DbNumber": 55,
      "TimeoutMs": 2000
    }
  }
}
```

I file di configurazione macchina PLC sono in: `MESManager.PlcSync/Configuration/machines/`  
Ogni macchina ha il suo file JSON con l'indirizzo IP del PLC.

> Se non ci sono PLC, MESManager.PlcSync non deve essere avviato.

---

## STEP 4 — Avvio

### Sviluppo / test locale

```powershell
cd MESManager
dotnet run --project MESManager/MESManager.Web/MESManager.Web.csproj --environment Development
# Apri: http://localhost:5156
```

### Produzione (come servizio Windows)

```powershell
# Pubblica
dotnet publish MESManager/MESManager.Web/MESManager.Web.csproj -c Release -o C:\MESManager\Web

# Registra come servizio Windows (una tantum)
sc create MESManager-Web binPath="C:\MESManager\Web\MESManager.Web.exe" start=auto
sc start MESManager-Web
```

### Worker Sync (opzionale, sync ERP Mago)

```powershell
dotnet publish MESManager/MESManager.Worker/MESManager.Worker.csproj -c Release -o C:\MESManager\Worker
sc create MESManager-Worker binPath="C:\MESManager\Worker\MESManager.Worker.exe" start=auto
```

### PlcSync (opzionale, solo se ci sono PLC fisici)

```powershell
dotnet publish MESManager/MESManager.PlcSync/MESManager.PlcSync.csproj -c Release -o C:\MESManager\PlcSync
sc create MESManager-PlcSync binPath="C:\MESManager\PlcSync\MESManager.PlcSync.exe" start=auto
```

---

## STEP 5 — Primo accesso

1. Apri il browser su `http://localhost:5156` (o l'IP del server)
2. Vai in **Impostazioni → Utenti** → aggiungi il primo utente operatore
3. Vai in **Impostazioni → Generali** → imposta il nome azienda
4. Vai in **Sync → Mago** → esegui una sincronizzazione iniziale di Articoli, Clienti, Commesse

---

## Dove cambiare cosa — Mappa rapida

| Cosa vuoi cambiare | Dove |
|---|---|
| **Connection string database** | `appsettings.Secrets.json` → `ConnectionStrings` |
| **Percorso allegati su disco** | `appsettings.Production.json` → `Files.AllegatiBasePath` |
| **Mapping percorsi di rete** | `appsettings.Production.json` → `Files.PathMappings` |
| **Host consentiti** | `appsettings.Production.json` → `AllowedHosts` |
| **Porta ascolto web** | `MESManager.Web/Properties/launchSettings.json` |
| **Intervallo polling PLC** | `MESManager.PlcSync/appsettings.json` → `PlcSync.PollingIntervalSeconds` |
| **IP / config macchina PLC** | `MESManager.PlcSync/Configuration/machines/NomeMacchina.json` |
| **Versione app (deploy)** | `MESManager.Web/Constants/AppVersion.cs` → `Current` |
| **Impostazioni Gantt (orari, macchine)** | UI → Impostazioni → Gantt |
| **Festivi / calendario** | UI → Impostazioni → Festivi |

---

## Aggiungere una nuova griglia catalogo (es. "Fornitori")

1. **JS** — crea `wwwroot/lib/ag-grid/fornitori-grid.js`:
```js
const columnDefs = [
    { field: 'Codice', headerName: 'Codice', width: 120 },
    { field: 'RagioneSociale', headerName: 'Ragione Sociale', flex: 1 },
    // ... altre colonne
];
agGridFactory.setup({
    namespace:      'fornitoriGrid',
    columnDefs,
    exportFileName: 'fornitori.csv',
    storageKeyBase: 'fornitori-grid-columnState',
    eventName:      'fornitoriGridStatsChanged',
});
```

2. **Razor page** — crea `Components/Pages/Cataloghi/CatalogoFornitori.razor`:
```razor
@page "/cataloghi/fornitori"
@inherits CatalogoGridBase
@inject HttpClient Http

@* ... HTML con <GridSettingsPanel> e <div id="fornitoriGrid"> ... *@

@code {
    protected override string GridNamespace => "fornitoriGrid";
    protected override string SettingsKey   => "fornitori-grid";
    protected override string PageKey       => "fornitori";

    private List<FornitoreDto> _fornitori = new();

    protected override async Task OnInitializedAsync()
    {
        PageToolbarService.SetActivePage(PageKey, this);
        _fornitori = await Http.GetFromJsonAsync<List<FornitoreDto>>("api/Fornitori") ?? new();
        totalRows = _fornitori.Count;
        await LoadSavedSettings();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { await Task.Delay(100); await InitializeGridJs(_fornitori); }
    }
}
```

3. **Aggiungi** il tag `<script src="/lib/ag-grid/fornitori-grid.js" type="module">` in `App.razor`
4. **Aggiungi** la voce di menu in `NavMenu.razor`

Tutto il resto (settings, stats, search, export, column panel) è già incluso in `CatalogoGridBase`.

---

## File da NON committare mai

```gitignore
appsettings.Secrets.json          ← credenziali database
appsettings.Database.*.json       ← credenziali database alternativa
*.encrypted                       ← file cifrati con credenziali
```

---

## Documentazione tecnica completa

Per approfondire: `docs/` — in particolare:

| File | Contenuto |
|---|---|
| `04-ARCHITETTURA.md` | Struttura Clean Architecture + pattern centralizzazione griglia |
| `01-DEPLOY.md` | Guida deploy dettagliata produzione |
| `03-CONFIGURAZIONE.md` | Tutti i parametri di configurazione |
| `08-PLC-SYNC.md` | Configurazione integrazione PLC Siemens S7 |
| `09-CHANGELOG.md` | Versione corrente e storico modifiche |
| `06-REPLICA-SISTEMA.md` | Come replicare il sistema per un nuovo cliente |
