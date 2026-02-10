# 📊 GUIDA COMPLETA PER REPLICARE IL SISTEMA GANTT MACCHINE

> **Documento tecnico di riferimento per implementare un sistema Gantt completo con drag&drop, accodamento rigido e sincronizzazione real-time**
> 
> Versione: 1.0  
> Data: 4 Febbraio 2026  
> Sistema: MESManager - Pianificazione Gantt Macchine

---

## 📑 INDICE

1. [Architettura del Sistema](#1-architettura-del-sistema)
2. [Stack Tecnologico e Dipendenze](#2-stack-tecnologico-e-dipendenze)
3. [Struttura Database](#3-struttura-database)
4. [Layer Backend (API)](#4-layer-backend-api)
5. [Layer Frontend (Blazor)](#5-layer-frontend-blazor)
6. [Layer Visualizzazione (JavaScript)](#6-layer-visualizzazione-javascript)
7. [Sistema di Sincronizzazione Real-Time](#7-sistema-di-sincronizzazione-real-time)
8. [Algoritmo di Accodamento Rigido](#8-algoritmo-di-accodamento-rigido)
9. [Calcoli Temporali e Calendario](#9-calcoli-temporali-e-calendario)
10. [Configurazione e Setup](#10-configurazione-e-setup)
11. [Testing e Debugging](#11-testing-e-debugging)
12. [Estensioni Future](#12-estensioni-future)

---

## 1. ARCHITETTURA DEL SISTEMA

### 1.1 Panoramica

Il sistema Gantt è basato su un'architettura **a 3 livelli**:

```
┌─────────────────────────────────────────────────────┐
│  Layer 1: DATABASE (SQL Server)                     │
│  - Tabella Commesse                                 │
│  - Tabella Articoli                                 │
│  - Tabella Macchine                                 │
│  - Tabella ImpostazioniProduzione                   │
│  - Tabella Festivi                                  │
└─────────────────┬───────────────────────────────────┘
                  │
                  │ EF Core Queries
                  ▼
┌─────────────────────────────────────────────────────┐
│  Layer 2: BACKEND (.NET 8 / C#)                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ PianificazioneController.cs                 │   │
│  │  - GET /api/pianificazione                  │   │
│  │  - POST /api/pianificazione/sposta          │   │
│  │  - POST /api/pianificazione/ricalcola-tutto │   │
│  └─────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────┐   │
│  │ PianificazioneEngineService.cs              │   │
│  │  - SpostaCommessaAsync()                    │   │
│  │  - RicalcolaTutteCommesseAsync()            │   │
│  │  - RicalcolaAcqueMacchinaInternalAsync()    │   │
│  └─────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────┐   │
│  │ PianificazioneService.cs                    │   │
│  │  - CalcolaDurataPrevistaMinuti()            │   │
│  │  - CalcolaDataFinePrevistaConFestivi()      │   │
│  └─────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────┐   │
│  │ PianificazioneHub.cs (SignalR)              │   │
│  │  - NotifyPianificazioneUpdated()            │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────┬───────────────────────────────────┘
                  │
                  │ JSON API + SignalR WebSocket
                  ▼
┌─────────────────────────────────────────────────────┐
│  Layer 3: FRONTEND (Blazor Server + JavaScript)     │
│  ┌─────────────────────────────────────────────┐   │
│  │ GanttMacchine.razor                         │   │
│  │  - Carica dati da API                       │   │
│  │  - Gestisce stato componente                │   │
│  │  - Riceve notifiche SignalR                 │   │
│  └─────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────┐   │
│  │ gantt-macchine.js                           │   │
│  │  - Renderizza Vis-Timeline                  │   │
│  │  - Gestisce Drag&Drop                       │   │
│  │  - Sincronizza con server                   │   │
│  └─────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────┐   │
│  │ Vis-Timeline Library (vis-timeline.min.js)  │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### 1.2 Flusso Dati Completo

```
INIZIALIZZAZIONE:
Database → EF Core → MapToGanttDto() → JSON API Response → 
Blazor OnInitializedAsync() → JS initialize() → Vis-Timeline Render

DRAG & DROP:
User Drag → JS onMove() → POST /api/pianificazione/sposta → 
PianificazioneEngineService → Update DB → RicalcolaAcqueMacchina() → 
Response con CommesseAggiornate → JS updateItemsFromServer() → 
SignalR NotifyPianificazioneUpdated() → Altri client aggiornati

RICALCOLO GLOBALE:
Button Click → POST /api/pianificazione/ricalcola-tutto → 
RicalcolaTutteCommesseAsync() → Update DB → 
SignalR FullRecalculation → Blazor LoadCommesseGantt() → 
JS updateTasks() → Gantt Refresh
```

---

## 2. STACK TECNOLOGICO E DIPENDENZE

### 2.1 Backend Dependencies (.NET 8)

**File**: `MESManager.Web/MESManager.Web.csproj`

```xml
<ItemGroup>
  <!-- Entity Framework Core -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
  
  <!-- SignalR per real-time -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
  
  <!-- Blazor Server -->
  <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  
  <!-- MudBlazor per UI -->
  <PackageReference Include="MudBlazor" Version="6.11.0" />
</ItemGroup>
```

### 2.2 Frontend Dependencies (JavaScript)

**File**: `MESManager.Web/Pages/_Host.cshtml` o `App.razor`

```html
<!-- Vis-Timeline per Gantt Chart -->
<script src="https://unpkg.com/vis-timeline@7.7.0/standalone/umd/vis-timeline-graph2d.min.js"></script>
<link href="https://unpkg.com/vis-timeline@7.7.0/styles/vis-timeline-graph2d.min.css" rel="stylesheet" />

<!-- SignalR Client -->
<script src="_framework/blazor.server.js"></script>

<!-- Custom Gantt Script -->
<script src="~/js/gantt/gantt-macchine.js"></script>
```

### 2.3 Versioni Critiche

| Libreria | Versione | Note |
|----------|----------|------|
| .NET | 8.0 | Framework principale |
| EF Core | 8.0.0 | ORM per database |
| Vis-Timeline | 7.7.0 | **IMPORTANTE**: Versioni > 7.x hanno API diverse |
| MudBlazor | 6.11.0 | UI Components |
| SignalR | Incluso in .NET 8 | Real-time communication |
| SQL Server | 2019+ | Database engine |

---

## 3. STRUTTURA DATABASE

### 3.1 Tabella `Commesse` (Core)

```sql
CREATE TABLE Commesse (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Identificazione
    Codice NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    
    -- Assegnazione Macchina
    NumeroMacchina NVARCHAR(10) NULL,  -- "1", "2", "3" etc. NULL = non assegnata
    OrdineSequenza INT NOT NULL DEFAULT 0,  -- Ordine nella coda macchina (1, 2, 3...)
    
    -- Date Pianificazione
    DataInizioPrevisione DATETIME2 NULL,
    DataFinePrevisione DATETIME2 NULL,
    DataInizioProduzione DATETIME2 NULL,  -- Data effettiva inizio
    DataFineProduzione DATETIME2 NULL,    -- Data effettiva fine
    
    -- Dati Produttivi
    QuantitaRichiesta DECIMAL(18,2) NOT NULL,
    UoM NVARCHAR(10),  -- Unità di misura
    DataConsegna DATETIME2 NULL,
    
    -- Collegamento Articolo (per dati produttivi)
    ArticoloId UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (ArticoloId) REFERENCES Articoli(Id),
    
    -- Stato
    Stato NVARCHAR(50) NOT NULL DEFAULT 'InProgrammazione',  
    -- Valori: InProgrammazione, Programmata, InCorso, Completata, Sospesa
    
    -- Audit
    UltimaModifica DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TimestampSync DATETIME2 NULL,
    
    -- Indici per performance
    INDEX IX_Commesse_NumeroMacchina_OrdineSequenza (NumeroMacchina, OrdineSequenza),
    INDEX IX_Commesse_DataInizioPrevisione (DataInizioPrevisione),
    INDEX IX_Commesse_Stato (Stato)
);
```

### 3.2 Tabella `Articoli` (Dati Tecnici)

```sql
CREATE TABLE Articoli (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    Codice NVARCHAR(50) NOT NULL UNIQUE,
    Descrizione NVARCHAR(500),
    
    -- Dati Produttivi (critici per calcoli Gantt)
    TempoCiclo INT NOT NULL DEFAULT 0,  -- Secondi per ciclo
    NumeroFigure INT NOT NULL DEFAULT 1,  -- Pezzi prodotti per ciclo
    
    -- Altri dati...
    Prezzo DECIMAL(18,2),
    UnitaMisura NVARCHAR(10)
);
```

### 3.3 Tabella `Macchine`

```sql
CREATE TABLE Macchine (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    Codice NVARCHAR(10) NOT NULL UNIQUE,  -- "M01", "M02", "M03"...
    Nome NVARCHAR(100) NOT NULL,  -- "Macchina 01"
    
    AttivaInGantt BIT NOT NULL DEFAULT 1,  -- Se FALSE, nascosta dal Gantt
    OrdineVisualizazione INT NOT NULL DEFAULT 0,  -- Ordine visualizzazione nel Gantt
    
    INDEX IX_Macchine_AttivaInGantt_Ordine (AttivaInGantt, OrdineVisualizazione)
);
```

### 3.4 Tabella `ImpostazioniProduzione`

```sql
CREATE TABLE ImpostazioniProduzione (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- Impostazioni Tempo Setup (attrezzaggio macchina)
    TempoSetupMinuti INT NOT NULL DEFAULT 90,  -- Minuti fissi per setup
    
    -- Impostazioni Calendario Lavorativo
    OreLavorativeGiornaliere INT NOT NULL DEFAULT 8,  -- Ore al giorno (es. 8h)
    GiorniLavorativiSettimanali INT NOT NULL DEFAULT 5,  -- Lun-Ven (5 giorni)
    
    UltimaModifica DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### 3.5 Tabella `Festivi`

```sql
CREATE TABLE Festivi (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    Data DATE NOT NULL,
    Descrizione NVARCHAR(200),
    
    Ricorrente BIT NOT NULL DEFAULT 0,  -- Se TRUE, si ripete ogni anno
    Anno INT NULL,  -- NULL se ricorrente, altrimenti anno specifico
    
    DataCreazione DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_Festivi_Data (Data)
);
```

### 3.6 Tabella `ImpostazioniGantt` (Opzionale)

```sql
CREATE TABLE ImpostazioniGantt (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    AbilitaTempoAttrezzaggio BIT NOT NULL DEFAULT 0,
    TempoAttrezzaggioMinutiDefault INT NOT NULL DEFAULT 30,
    
    DataCreazione DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DataModifica DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

---

## 4. LAYER BACKEND (API)

### 4.1 DTO (Data Transfer Objects)

**File**: `MESManager.Application/DTOs/CommessaGanttDto.cs`

```csharp
namespace MESManager.Application.DTOs;

public class CommessaGanttDto
{
    // Identificazione
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Assegnazione Macchina
    public int? NumeroMacchina { get; set; }  // IMPORTANTE: int? non string
    public string? NomeMacchina { get; set; }
    public int OrdineSequenza { get; set; }
    
    // Date Pianificazione ⚠️ CAMPO CRITICO
    public DateTime? DataInizioPrevisione { get; set; }
    public DateTime? DataFinePrevisione { get; set; }
    public DateTime? DataInizioProduzione { get; set; }
    public DateTime? DataFineProduzione { get; set; }
    
    // Dati Produttivi
    public decimal QuantitaRichiesta { get; set; }
    public string? UoM { get; set; }
    public DateTime? DataConsegna { get; set; }
    
    // Calcolo Tempi
    public int TempoCicloSecondi { get; set; }
    public int NumeroFigure { get; set; }
    public int TempoSetupMinuti { get; set; }
    public int DurataPrevistaMinuti { get; set; }
    
    // Stato e Visualizzazione
    public string Stato { get; set; } = string.Empty;
    public string ColoreStato { get; set; } = string.Empty;
    public decimal PercentualeCompletamento { get; set; }
    
    // Flag Dati Incompleti (per triangolino ⚠️)
    public bool DatiIncompleti { get; set; }
}
```

**File**: `MESManager.Application/DTOs/SpostaCommessaRequest.cs`

```csharp
public class SpostaCommessaRequest
{
    public Guid CommessaId { get; set; }
    public int TargetMacchina { get; set; }  // Numero macchina destinazione
    public DateTime? TargetDataInizio { get; set; }  // Data richiesta (opzionale)
    public Guid? InsertBeforeCommessaId { get; set; }  // Per inserimento specifico
}
```

**File**: `MESManager.Application/DTOs/SpostaCommessaResponse.cs`

```csharp
public class SpostaCommessaResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Liste commesse aggiornate
    public List<CommessaGanttDto> CommesseAggiornate { get; set; } = new();
    public List<CommessaGanttDto>? CommesseMacchinaOrigine { get; set; }
}
```

### 4.2 Controller API

**File**: `MESManager.Web/Controllers/PianificazioneController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class PianificazioneController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    private readonly IPianificazioneEngineService _engineService;
    
    // 📌 ENDPOINT 1: Carica tutte le commesse per il Gantt
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommessaGanttDto>>> GetCommesseGantt()
    {
        var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
            ?? new ImpostazioniProduzione { 
                TempoSetupMinuti = 90, 
                OreLavorativeGiornaliere = 8, 
                GiorniLavorativiSettimanali = 5 
            };

        var commesse = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina != null)  // ⚠️ FILTRO CRITICO
            .OrderBy(c => c.NumeroMacchina)
            .ThenBy(c => c.OrdineSequenza)
            .ToListAsync();

        var commesseGantt = commesse
            .Select(c => MapToGanttDto(c, impostazioni))
            .ToList();

        return Ok(commesseGantt);
    }
    
    // 📌 ENDPOINT 2: Sposta commessa con accodamento rigido
    [HttpPost("sposta")]
    public async Task<ActionResult<SpostaCommessaResponse>> SpostaCommessa(
        [FromBody] SpostaCommessaRequest request)
    {
        var result = await _engineService.SpostaCommessaAsync(request);
        
        if (!result.Success)
            return BadRequest(result);
        
        return Ok(result);
    }
    
    // 📌 ENDPOINT 3: Ricalcola tutte le macchine
    [HttpPost("ricalcola-tutto")]
    public async Task<IActionResult> RicalcolaTutto()
    {
        await _engineService.RicalcolaTutteCommesseAsync();
        return Ok(new { message = "Ricalcolo completato" });
    }
    
    // 🔧 HELPER: Mapping Entity → DTO
    private CommessaGanttDto MapToGanttDto(
        Commessa commessa, 
        ImpostazioniProduzione impostazioni)
    {
        var dto = new CommessaGanttDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            Description = commessa.Description,
            
            // Parse NumeroMacchina da string a int
            NumeroMacchina = int.TryParse(commessa.NumeroMacchina, out var numMacchina) 
                ? numMacchina 
                : null,
            OrdineSequenza = commessa.OrdineSequenza,
            
            // Date ⚠️ USA I CAMPI CORRETTI
            DataInizioPrevisione = commessa.DataInizioPrevisione,
            DataFinePrevisione = commessa.DataFinePrevisione,
            DataInizioProduzione = commessa.DataInizioProduzione,
            DataFineProduzione = commessa.DataFineProduzione,
            
            QuantitaRichiesta = commessa.QuantitaRichiesta,
            UoM = commessa.UoM,
            DataConsegna = commessa.DataConsegna,
            
            Stato = commessa.Stato,
            ColoreStato = GetColorForStato(commessa.Stato),
            
            // Calcoli
            PercentualeCompletamento = CalcolaPercentualeCompletamento(commessa),
            DatiIncompleti = false  // Calcola in base a logica specifica
        };
        
        // Se esiste articolo, aggiungi dati tecnici
        if (commessa.Articolo != null)
        {
            dto.TempoCicloSecondi = commessa.Articolo.TempoCiclo;
            dto.NumeroFigure = commessa.Articolo.NumeroFigure;
            dto.TempoSetupMinuti = impostazioni.TempoSetupMinuti;
            
            dto.DurataPrevistaMinuti = _pianificazioneService
                .CalcolaDurataPrevistaMinuti(
                    dto.TempoCicloSecondi,
                    dto.NumeroFigure,
                    dto.QuantitaRichiesta,
                    dto.TempoSetupMinuti
                );
        }
        else
        {
            // ⚠️ DATI INCOMPLETI - Usa valori di default
            dto.DatiIncompleti = true;
            dto.TempoCicloSecondi = 0;
            dto.NumeroFigure = 1;
            dto.TempoSetupMinuti = impostazioni.TempoSetupMinuti;
            dto.DurataPrevistaMinuti = 480; // 8h standard
        }
        
        return dto;
    }
}
```

### 4.3 Servizio Pianificazione Base

**File**: `MESManager.Application/Services/PianificazioneService.cs`

```csharp
public class PianificazioneService : IPianificazioneService
{
    /// <summary>
    /// Calcola la durata prevista in minuti
    /// Formula: TempoSetup + (TempoCiclo * Quantità / NumeroFigure) / 60
    /// </summary>
    public int CalcolaDurataPrevistaMinuti(
        int tempoCicloSecondi,
        int numeroFigure,
        decimal quantitaRichiesta,
        int tempoSetupMinuti)
    {
        if (numeroFigure == 0) 
            numeroFigure = 1;  // Evita divisione per zero
        
        // Calcola numero cicli necessari
        var numeroCicli = (int)Math.Ceiling(quantitaRichiesta / numeroFigure);
        
        // Tempo produzione in secondi
        var tempoProduzione = tempoCicloSecondi * numeroCicli;
        
        // Converti in minuti e aggiungi setup
        var tempoProduzioneMinuti = (int)Math.Ceiling(tempoProduzione / 60.0);
        
        return tempoSetupMinuti + tempoProduzioneMinuti;
    }
    
    /// <summary>
    /// Calcola data fine prevista considerando calendario lavorativo e festivi
    /// </summary>
    public DateTime CalcolaDataFinePrevistaConFestivi(
        DateTime dataInizio,
        int durataMinuti,
        int oreLavorativeGiornaliere,
        int giorniLavorativiSettimanali,
        HashSet<DateTime> festivi)
    {
        var minutiLavorativiPerGiorno = oreLavorativeGiornaliere * 60;
        var dataCorrente = dataInizio;
        var minutiRimanenti = durataMinuti;
        
        while (minutiRimanenti > 0)
        {
            // Salta weekend se giorni lavorativi < 7
            if (giorniLavorativiSettimanali < 7)
            {
                if (dataCorrente.DayOfWeek == DayOfWeek.Saturday)
                {
                    dataCorrente = dataCorrente.AddDays(2);  // Salta a lunedì
                    continue;
                }
                if (dataCorrente.DayOfWeek == DayOfWeek.Sunday)
                {
                    dataCorrente = dataCorrente.AddDays(1);
                    continue;
                }
            }
            
            // Salta festivi
            if (festivi.Contains(dataCorrente.Date))
            {
                dataCorrente = dataCorrente.AddDays(1);
                continue;
            }
            
            // Consuma minuti dal giorno corrente
            if (minutiRimanenti >= minutiLavorativiPerGiorno)
            {
                minutiRimanenti -= minutiLavorativiPerGiorno;
                dataCorrente = dataCorrente.AddDays(1);
            }
            else
            {
                dataCorrente = dataCorrente.AddMinutes(minutiRimanenti);
                minutiRimanenti = 0;
            }
        }
        
        return dataCorrente;
    }
}
```

### 4.4 Servizio Engine (Accodamento Rigido)

**File**: `MESManager.Infrastructure/Services/PianificazioneEngineService.cs`

```csharp
public class PianificazioneEngineService : IPianificazioneEngineService
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    
    /// <summary>
    /// Sposta una commessa su una macchina con accodamento rigido
    /// ALGORITMO CHIAVE: Previene sovrapposizioni, ricalcola sequenza
    /// </summary>
    public async Task<SpostaCommessaResponse> SpostaCommessaAsync(
        SpostaCommessaRequest request)
    {
        // 1. Carica commessa e impostazioni
        var commessa = await _context.Commesse
            .Include(c => c.Articolo)
            .FirstOrDefaultAsync(c => c.Id == request.CommessaId);
        
        if (commessa == null)
            return new SpostaCommessaResponse 
            { 
                Success = false, 
                ErrorMessage = "Commessa non trovata" 
            };
        
        var impostazioni = await GetImpostazioniAsync();
        var festivi = await GetFestiviSetAsync();
        
        // 2. Salva macchina origine per ricalcolo cascata
        var macchinaOrigine = commessa.NumeroMacchina;
        var targetMacchinaStr = request.TargetMacchina.ToString();
        
        // 3. Carica commesse destinazione (esclusa quella da spostare)
        var commesseDestinazione = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == targetMacchinaStr 
                     && c.Id != request.CommessaId)
            .OrderBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataInizioPrevisione)
            .ToListAsync();
        
        // 4. Determina posizione inserimento e nuova data inizio
        int nuovoOrdine;
        DateTime dataInizio;
        
        if (request.TargetDataInizio.HasValue)
        {
            // 📌 CASO 1: Data specifica richiesta
            dataInizio = request.TargetDataInizio.Value;
            
            // Trova prima commessa dopo questa data
            var commessaDopoRichiesta = commesseDestinazione
                .OrderBy(c => c.DataInizioPrevisione ?? DateTime.MaxValue)
                .FirstOrDefault(c => c.DataInizioPrevisione >= dataInizio);
            
            if (commessaDopoRichiesta != null)
            {
                nuovoOrdine = commessaDopoRichiesta.OrdineSequenza;
                
                // Shifta tutte le successive
                foreach (var c in commesseDestinazione
                    .Where(c => c.OrdineSequenza >= nuovoOrdine))
                {
                    c.OrdineSequenza++;
                }
            }
            else
            {
                // Accoda in fondo
                nuovoOrdine = (commesseDestinazione
                    .Max(c => (int?)c.OrdineSequenza) ?? 0) + 1;
                
                // ⚠️ ACCODAMENTO RIGIDO: se ultima commessa finisce dopo data richiesta
                var ultimaCommessa = commesseDestinazione
                    .OrderByDescending(c => c.DataFinePrevisione)
                    .FirstOrDefault();
                
                if (ultimaCommessa?.DataFinePrevisione > dataInizio)
                {
                    dataInizio = ultimaCommessa.DataFinePrevisione.Value;
                }
            }
        }
        else
        {
            // 📌 CASO 2: Accoda in fondo (comportamento default)
            nuovoOrdine = (commesseDestinazione
                .Max(c => (int?)c.OrdineSequenza) ?? 0) + 1;
            
            var ultimaCommessa = commesseDestinazione
                .OrderByDescending(c => c.DataFinePrevisione)
                .FirstOrDefault();
            
            dataInizio = ultimaCommessa?.DataFinePrevisione ?? DateTime.Now;
        }
        
        // 5. Aggiorna commessa spostata
        commessa.NumeroMacchina = targetMacchinaStr;
        commessa.OrdineSequenza = nuovoOrdine;
        commessa.DataInizioPrevisione = dataInizio;
        commessa.UltimaModifica = DateTime.UtcNow;
        
        // 6. Calcola durata e data fine
        var durataMinuti = CalcolaDurata(commessa, impostazioni);
        commessa.DataFinePrevisione = _pianificazioneService
            .CalcolaDataFinePrevistaConFestivi(
                dataInizio,
                durataMinuti,
                impostazioni.OreLavorativeGiornaliere,
                impostazioni.GiorniLavorativiSettimanali,
                festivi
            );
        
        // 7. ⚠️ RICALCOLO CASCATA: tutte commesse macchina destinazione
        await RicalcolaAcqueMacchinaInternalAsync(
            targetMacchinaStr, 
            impostazioni, 
            festivi
        );
        
        // 8. Se macchina origine diversa, ricalcola anche quella
        List<CommessaGanttDto>? commesseMacchinaOrigine = null;
        if (macchinaOrigine != null && macchinaOrigine != targetMacchinaStr)
        {
            await RicalcolaAcqueMacchinaInternalAsync(
                macchinaOrigine, 
                impostazioni, 
                festivi
            );
            
            // Ricarica commesse origine per response
            var commesseOrigine = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == macchinaOrigine)
                .OrderBy(c => c.OrdineSequenza)
                .ToListAsync();
            
            commesseMacchinaOrigine = commesseOrigine
                .Select(c => MapToGanttDto(c, impostazioni))
                .ToList();
        }
        
        // 9. Salva modifiche al database
        await _context.SaveChangesAsync();
        
        // 10. Ricarica commesse aggiornate per response
        var commesseAggiornate = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == targetMacchinaStr)
            .OrderBy(c => c.OrdineSequenza)
            .ToListAsync();
        
        return new SpostaCommessaResponse
        {
            Success = true,
            CommesseAggiornate = commesseAggiornate
                .Select(c => MapToGanttDto(c, impostazioni))
                .ToList(),
            CommesseMacchinaOrigine = commesseMacchinaOrigine
        };
    }
    
    /// <summary>
    /// ⚠️ ALGORITMO ACCODAMENTO RIGIDO
    /// Ricalcola sequenzialmente tutte le commesse di una macchina
    /// Garantisce: Nessuna sovrapposizione, ordine corretto
    /// </summary>
    private async Task RicalcolaAcqueMacchinaInternalAsync(
        string numeroMacchina,
        ImpostazioniProduzione impostazioni,
        HashSet<DateTime> festivi)
    {
        // Carica tutte commesse della macchina ordinate per sequenza
        var commesseMacchina = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == numeroMacchina)
            .OrderBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataInizioPrevisione)
            .ToListAsync();
        
        if (!commesseMacchina.Any()) 
            return;
        
        // Riassegna OrdineSequenza sequenziale (1, 2, 3...)
        for (int i = 0; i < commesseMacchina.Count; i++)
        {
            commesseMacchina[i].OrdineSequenza = i + 1;
        }
        
        // Ricalcola date con accodamento rigido
        DateTime? dataFinePrecedente = null;
        
        foreach (var commessa in commesseMacchina)
        {
            // 📌 ACCODAMENTO: inizio = fine precedente (o ora se prima commessa)
            if (dataFinePrecedente.HasValue)
            {
                commessa.DataInizioPrevisione = dataFinePrecedente.Value;
            }
            else if (!commessa.DataInizioPrevisione.HasValue)
            {
                commessa.DataInizioPrevisione = DateTime.Now;
            }
            // Altrimenti mantieni data esistente (per prima commessa)
            
            // Calcola durata
            var durataMinuti = CalcolaDurata(commessa, impostazioni);
            
            // Calcola data fine considerando calendario
            commessa.DataFinePrevisione = _pianificazioneService
                .CalcolaDataFinePrevistaConFestivi(
                    commessa.DataInizioPrevisione.Value,
                    durataMinuti,
                    impostazioni.OreLavorativeGiornaliere,
                    impostazioni.GiorniLavorativiSettimanali,
                    festivi
                );
            
            // Aggiorna riferimento per prossima iterazione
            dataFinePrecedente = commessa.DataFinePrevisione;
            commessa.UltimaModifica = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Ricalcola TUTTE le commesse di TUTTE le macchine
    /// </summary>
    public async Task RicalcolaTutteCommesseAsync()
    {
        var impostazioni = await GetImpostazioniAsync();
        var festivi = await GetFestiviSetAsync();
        
        // Trova tutte le macchine con commesse assegnate
        var macchineConCommesse = await _context.Commesse
            .Where(c => c.NumeroMacchina != null)
            .Select(c => c.NumeroMacchina)
            .Distinct()
            .ToListAsync();
        
        // Ricalcola ogni macchina sequenzialmente
        foreach (var numeroMacchina in macchineConCommesse)
        {
            await RicalcolaAcqueMacchinaInternalAsync(
                numeroMacchina!, 
                impostazioni, 
                festivi
            );
        }
        
        await _context.SaveChangesAsync();
    }
    
    // Helper privati
    private int CalcolaDurata(
        Commessa commessa, 
        ImpostazioniProduzione impostazioni)
    {
        if (commessa.Articolo == null)
            return 480;  // 8h default se nessun articolo
        
        return _pianificazioneService.CalcolaDurataPrevistaMinuti(
            commessa.Articolo.TempoCiclo,
            commessa.Articolo.NumeroFigure,
            commessa.QuantitaRichiesta,
            impostazioni.TempoSetupMinuti
        );
    }
}
```

---

## 5. LAYER FRONTEND (BLAZOR)

### 5.1 Componente Razor

**File**: `MESManager.Web/Components/Pages/Programma/GanttMacchine.razor`

```razor
@page "/programma/gantt-macchine"
@layout MainLayout
@using MESManager.Application.DTOs
@using Microsoft.AspNetCore.SignalR.Client
@inject IJSRuntime JS
@inject HttpClient Http
@inject IMacchinaAppService MacchinaService
@inject IImpostazioniGanttAppService ImpostazioniGanttService
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@implements IAsyncDisposable

<PageTitle>Gantt Macchine</PageTitle>

<MudPaper Class="pa-4 mb-4">
    <MudText Typo="Typo.h5" Class="mb-2">
        Pianificazione Macchine - Vista Gantt
    </MudText>
    <MudText Typo="Typo.body2" Color="Color.Secondary">
        Visualizzazione temporale delle commesse pianificate per macchina
        @if (_isConnected)
        {
            <MudIcon Icon="@Icons.Material.Filled.Wifi" 
                     Color="Color.Success" 
                     Size="Size.Small" 
                     Class="ml-2" />
            <span style="color: var(--mud-palette-success);">
                Sincronizzazione attiva
            </span>
        }
    </MudText>
</MudPaper>

<MudPaper Class="pa-4 mb-4">
    <MudStack Row="true" Spacing="2">
        <MudButton Variant="Variant.Outlined" 
                   Color="Color.Primary" 
                   OnClick="@RefreshGantt">
            <MudIcon Icon="@Icons.Material.Filled.Refresh" Class="mr-1" /> 
            Aggiorna
        </MudButton>
        <MudButton Variant="Variant.Outlined" 
                   Color="Color.Secondary" 
                   OnClick="@RicalcolaTutto">
            <MudIcon Icon="@Icons.Material.Filled.Calculate" Class="mr-1" /> 
            Ricalcola Tutto
        </MudButton>
    </MudStack>
    <MudText Typo="Typo.caption" Class="mt-2" Color="Color.Secondary">
        Trascina le commesse per spostarle tra macchine. 
        L'accodamento è automatico (nessuna sovrapposizione).
    </MudText>
</MudPaper>

<MudPaper Class="pa-4">
    <div id="gantt-chart" style="width: 100%; height: 600px;"></div>
</MudPaper>

@code {
    private List<MacchinaDto> macchine = new();
    private ImpostazioniGanttDto? impostazioniGantt;
    private List<CommessaGanttDto> commesseGantt = new();
    private HubConnection? _hubConnection;
    private bool _isConnected;
    private DotNetObjectReference<GanttMacchine>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        // Carica configurazioni
        macchine = await MacchinaService.GetListaAsync();
        impostazioniGantt = await ImpostazioniGanttAppService.GetAsync();
        
        // Carica dati commesse
        await LoadCommesseGantt();
        
        // Inizializza SignalR
        await InitializeSignalR();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            
            // Prepara settings per JavaScript
            var settings = new
            {
                machines = macchine.Select(m => new
                {
                    id = m.Id,
                    codice = m.Codice,
                    nome = m.Nome,
                    ordineVisualizazione = m.OrdineVisualizazione
                }).ToArray(),
                
                tasks = commesseGantt.Select(c => new
                {
                    id = c.Id,
                    codice = c.Codice,
                    description = c.Description,
                    numeroMacchina = c.NumeroMacchina,
                    nomeMacchina = c.NomeMacchina,
                    quantita = c.QuantitaRichiesta,
                    quantitaRichiesta = c.QuantitaRichiesta,
                    uom = c.UoM,
                    uoM = c.UoM,
                    
                    // ⚠️ CAMPO CRITICO: usa nomi corretti
                    dataInizioPrevisione = c.DataInizioPrevisione,
                    dataFinePrevisione = c.DataFinePrevisione,
                    
                    // Mantieni anche fallback per compatibilità
                    dataInizio = c.DataInizioPrevisione,
                    dataFine = c.DataFinePrevisione,
                    
                    durataMinuti = c.DurataPrevistaMinuti,
                    durataPrevistaMinuti = c.DurataPrevistaMinuti,
                    stato = c.Stato,
                    coloreStato = c.ColoreStato,
                    percentualeCompletamento = c.PercentualeCompletamento,
                    ordineSequenza = c.OrdineSequenza,
                    
                    // ⚠️ TRIANGOLINO DATI INCOMPLETI
                    datiIncompleti = c.DatiIncompleti
                }).ToArray()
            };
            
            // Inizializza Gantt JavaScript
            await JS.InvokeVoidAsync(
                "GanttMacchine.initialize", 
                "gantt-chart", 
                settings
            );
            
            // Registra helper .NET per callbacks
            await JS.InvokeVoidAsync(
                "GanttMacchine.setDotNetHelper", 
                _dotNetRef
            );
        }
    }
    
    private async Task LoadCommesseGantt()
    {
        try
        {
            var response = await Http.GetFromJsonAsync<List<CommessaGanttDto>>(
                "api/pianificazione"
            );
            commesseGantt = response ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore caricamento dati: {ex.Message}", 
                         Severity.Error);
        }
    }
    
    private async Task RefreshGantt()
    {
        await LoadCommesseGantt();
        await UpdateGanttTasks();
        Snackbar.Add("Gantt aggiornato", Severity.Success);
    }
    
    private async Task RicalcolaTutto()
    {
        try
        {
            var response = await Http.PostAsync(
                "api/pianificazione/ricalcola-tutto", 
                null
            );
            
            if (response.IsSuccessStatusCode)
            {
                await RefreshGantt();
                Snackbar.Add("Ricalcolo completato", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore ricalcolo: {ex.Message}", Severity.Error);
        }
    }
    
    private async Task UpdateGanttTasks()
    {
        var tasks = commesseGantt.Select(c => new
        {
            id = c.Id,
            codice = c.Codice,
            description = c.Description,
            numeroMacchina = c.NumeroMacchina,
            dataInizioPrevisione = c.DataInizioPrevisione,
            dataFinePrevisione = c.DataFinePrevisione,
            dataInizio = c.DataInizioPrevisione,
            dataFine = c.DataFinePrevisione,
            quantita = c.QuantitaRichiesta,
            quantitaRichiesta = c.QuantitaRichiesta,
            uom = c.UoM,
            uoM = c.UoM,
            durataMinuti = c.DurataPrevistaMinuti,
            durataPrevistaMinuti = c.DurataPrevistaMinuti,
            stato = c.Stato,
            percentualeCompletamento = c.PercentualeCompletamento,
            ordineSequenza = c.OrdineSequenza,
            datiIncompleti = c.DatiIncompleti
        }).ToArray();
        
        await JS.InvokeVoidAsync("GanttMacchine.updateTasks", tasks);
    }
    
    private async Task InitializeSignalR()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/pianificazione"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<GanttUpdateNotification>(
                "PianificazioneUpdated", 
                async (notification) =>
            {
                if (notification.Type == "FullRecalculation")
                {
                    await LoadCommesseGantt();
                    await UpdateGanttTasks();
                    await InvokeAsync(StateHasChanged);
                }
            });

            _hubConnection.Reconnected += async (connectionId) =>
            {
                _isConnected = true;
                await InvokeAsync(StateHasChanged);
            };

            _hubConnection.Closed += async (error) =>
            {
                _isConnected = false;
                await InvokeAsync(StateHasChanged);
            };

            await _hubConnection.StartAsync();
            _isConnected = true;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore SignalR: {ex.Message}", Severity.Warning);
        }
    }
    
    // Callback da JavaScript
    [JSInvokable]
    public async Task OnCommessaMoved(SpostaCommessaResponse result)
    {
        if (result.Success)
        {
            Snackbar.Add("Commessa spostata con successo", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Errore: {result.ErrorMessage}", Severity.Error);
        }
    }
    
    [JSInvokable]
    public async Task OnFullRecalculation()
    {
        await RefreshGantt();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}
```

---

## 6. LAYER VISUALIZZAZIONE (JAVASCRIPT)

### 6.1 Script Gantt Completo

**File**: `MESManager.Web/wwwroot/js/gantt/gantt-macchine.js`

```javascript
// ═══════════════════════════════════════════════════════════════
// GANTT MACCHINE - Vis-Timeline con Drag&Drop e SignalR Sync
// ═══════════════════════════════════════════════════════════════

window.GanttMacchine = {
    timeline: null,
    settings: null,
    hubConnection: null,
    machineMap: new Map(),          // numeroMacchina → codice ("1" → "M01")
    reverseMachineMap: new Map(),   // codice → numeroMacchina ("M01" → 1)
    isProcessingUpdate: false,
    dotNetHelper: null,
    
    /**
     * 📌 INIZIALIZZAZIONE GANTT
     * @param {string} elementId - ID container HTML
     * @param {object} settings - Configurazioni (machines, tasks)
     */
    initialize: function(elementId, settings) {
        console.log('🚀 Initializing Vis-Timeline Gantt:', elementId);
        console.log('⚙️ Settings:', settings);
        
        this.settings = settings || {};
        
        // Verifica libreria Vis-Timeline caricata
        if (typeof vis === 'undefined') {
            console.error('❌ Vis-Timeline library not loaded!');
            return;
        }
        
        const container = document.getElementById(elementId);
        if (!container) {
            console.error('❌ Container not found:', elementId);
            return;
        }
        
        // 📊 CREA GROUPS (Macchine)
        const groups = this.settings.machines && this.settings.machines.length > 0
            ? this.settings.machines.map(m => ({ 
                id: m.codice || m.id, 
                content: m.nome,
                order: m.ordineVisualizazione || 0
            }))
            : [
                { id: 'M01', content: 'Macchina 01', order: 1 },
                { id: 'M02', content: 'Macchina 02', order: 2 },
                { id: 'M03', content: 'Macchina 03', order: 3 }
            ];
        
        console.log('📋 Groups created:', groups);
        
        // 🔢 CREA MAPPING MACCHINE: numeroMacchina ↔ codice
        this.machineMap.clear();
        this.reverseMachineMap.clear();
        
        this.settings.machines.forEach(m => {
            const match = m.codice.match(/\d+/);  // Estrai numero da "M01"
            if (match) {
                const numMacchina = parseInt(match[0], 10);
                this.machineMap.set(numMacchina, m.codice || m.id);
                this.reverseMachineMap.set(m.codice || m.id, numMacchina);
            }
        });
        
        console.log('🔗 Machine mapping:', Array.from(this.machineMap.entries()));
        
        // 📦 CREA ITEMS (Commesse)
        let items = this.createItemsFromTasks(this.settings.tasks);
        console.log(`✅ Created ${items.length} items from tasks`);
        
        // ⚙️ CONFIGURAZIONE VIS-TIMELINE
        const options = {
            editable: {
                add: false,
                updateTime: true,   // ✅ Drag temporale
                updateGroup: true,  // ✅ Drag tra macchine
                remove: false,
                overrideItems: false
            },
            stack: false,  // ❌ NO sovrapposizione visuale (accodamento rigido)
            orientation: 'top',
            groupOrder: 'order',
            margin: {
                item: 10,
                axis: 5
            },
            snap: null,  // Nessuno snap automatico
            start: items.length > 0 
                ? new Date(Math.min(...items.map(i => new Date(i.start))))
                : new Date(),
            end: items.length > 0 
                ? new Date(Math.max(...items.map(i => new Date(i.end))))
                : new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
        };
        
        // 🎨 CREA TIMELINE
        this.timeline = new vis.Timeline(container, items, groups, options);
        
        const self = this;
        
        // ═══════════════════════════════════════════════════════
        // 📌 EVENTO: MOVING (Durante il drag - feedback visuale)
        // ═══════════════════════════════════════════════════════
        this.timeline.on('moving', function (item, callback) {
            if (self.isProcessingUpdate) {
                callback(null);  // Annulla se già in aggiornamento server
                return;
            }
            
            // Trova altre commesse nello stesso gruppo
            const allItems = self.timeline.itemsData.get();
            const itemsInGroup = allItems.filter(i => 
                i.group === item.group && i.id !== item.id
            );
            
            // ⚠️ ANTI-SOVRAPPOSIZIONE CLIENT-SIDE
            for (let other of itemsInGroup) {
                const otherStart = new Date(other.start).getTime();
                const otherEnd = new Date(other.end).getTime();
                const itemStart = new Date(item.start).getTime();
                const itemEnd = new Date(item.end).getTime();
                
                // Se sovrapposizione → accoda DOPO
                if (itemStart < otherEnd && itemEnd > otherStart) {
                    const duration = itemEnd - itemStart;
                    item.start = new Date(otherEnd);
                    item.end = new Date(otherEnd + duration);
                    break;
                }
            }
            
            callback(item);
        });
        
        // ═══════════════════════════════════════════════════════
        // 📌 EVENTO: MOVE (Dopo drop - persistenza server)
        // ═══════════════════════════════════════════════════════
        this.timeline.setOptions({
            onMove: async function(item, callback) {
                if (self.isProcessingUpdate) {
                    callback(null);
                    return;
                }
                
                // Estrai numero macchina da group code
                const targetMacchina = self.reverseMachineMap.get(item.group);
                
                if (!targetMacchina) {
                    console.error('❌ Target machine not found:', item.group);
                    callback(null);
                    return;
                }
                
                console.log(`📤 Moving item ${item.id} to machine ${targetMacchina} at ${item.start}`);
                
                try {
                    // 🌐 API CALL: Sposta commessa
                    const response = await fetch('/api/pianificazione/sposta', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            commessaId: item.id,
                            targetMacchina: targetMacchina,
                            targetDataInizio: item.start.toISOString(),
                            insertBeforeCommessaId: null
                        })
                    });
                    
                    if (!response.ok) {
                        const errorData = await response.json();
                        console.error('❌ Server error:', errorData);
                        alert('Errore spostamento: ' + (errorData.errorMessage || 'Errore sconosciuto'));
                        callback(null);  // Annulla move
                        return;
                    }
                    
                    const result = await response.json();
                    console.log('✅ Move result:', result);
                    
                    if (result.success) {
                        // 🔄 Aggiorna tutti gli item con dati server (date ricalcolate)
                        self.updateItemsFromServer(result.commesseAggiornate);
                        
                        if (result.commesseMacchinaOrigine) {
                            self.updateItemsFromServer(result.commesseMacchinaOrigine);
                        }
                        
                        // 📣 Notifica Blazor (callback)
                        if (self.dotNetHelper) {
                            self.dotNetHelper.invokeMethodAsync('OnCommessaMoved', result);
                        }
                        
                        callback(item);  // Conferma move
                    } else {
                        alert('Errore: ' + result.errorMessage);
                        callback(null);  // Annulla move
                    }
                } catch (error) {
                    console.error('❌ Error moving item:', error);
                    alert('Errore di comunicazione con il server');
                    callback(null);
                }
            }
        });
        
        // 🔌 Inizializza SignalR
        this.initSignalR();
        
        console.log('✅ Vis-Timeline Gantt initialized successfully');
    },
    
    /**
     * 📦 CREA ITEMS DA TASKS
     * Filtra solo commesse con date complete e macchina assegnata
     */
    createItemsFromTasks: function(tasks) {
        if (!tasks || tasks.length === 0) {
            console.warn('⚠️ No tasks data - Gantt will be empty');
            return [];
        }
        
        const self = this;
        return tasks
            .filter(task => {
                // ⚠️ FILTRO CRITICO: Solo commesse con date e macchina
                const hasDataInizio = task.dataInizioPrevisione || task.dataInizio;
                const hasDataFine = task.dataFinePrevisione || task.dataFine;
                const hasMacchina = task.numeroMacchina != null;
                
                return hasDataInizio && hasDataFine && hasMacchina;
            })
            .map(task => {
                const groupId = self.machineMap.get(task.numeroMacchina) || 'M01';
                const progress = task.percentualeCompletamento || 0;
                const baseColor = self.getStatusColor(task.stato);
                
                // 🎨 GRADIENT PROGRESSIVO
                const progressStyle = 
                    `background: linear-gradient(to right, ` +
                    `${baseColor} ${progress}%, ` +
                    `rgba(${self.hexToRgb(baseColor)}, 0.3) ${progress}%); ` +
                    `color: white;`;
                
                // ⚠️ TRIANGOLINO DATI INCOMPLETI
                const warningIcon = task.datiIncompleti ? ' ⚠️' : '';
                
                // 📅 USA CAMPI CORRETTI CON FALLBACK
                const dataInizio = task.dataInizioPrevisione || task.dataInizio;
                const dataFine = task.dataFinePrevisione || task.dataFine;
                
                return {
                    id: task.id,
                    group: groupId,
                    content: `${task.codice} (${Math.round(progress)}%)${warningIcon}`,
                    start: new Date(dataInizio),
                    end: new Date(dataFine),
                    className: 'commessa-item',
                    style: progressStyle,
                    title: self.createTooltip(task),
                    datiIncompleti: task.datiIncompleti  // Conserva flag
                };
            });
    },
    
    /**
     * 💬 CREA TOOLTIP PER ITEM
     */
    createTooltip: function(task) {
        const warningText = task.datiIncompleti 
            ? '\n⚠️ ATTENZIONE: Dati incompleti (usato 8h standard)' 
            : '';
        
        // Gestisci varianti nomi campi (quantita vs quantitaRichiesta)
        const quantita = task.quantita || task.quantitaRichiesta || 0;
        const durata = task.durataMinuti || task.durataPrevistaMinuti || 0;
        const uom = task.uom || task.uoM || '';
        
        return `${task.description || task.codice}
Quantità: ${quantita} ${uom}
Durata: ${durata} min
Stato: ${task.stato}
Ordine: ${task.ordineSequenza || '-'}${warningText}`;
    },
    
    /**
     * 🔄 AGGIORNA ITEMS DA SERVER (dopo spostamento o ricalcolo)
     * ⚠️ PRESERVA FLAG datiIncompleti per triangolino
     */
    updateItemsFromServer: function(commesse) {
        if (!commesse || !this.timeline) return;
        
        this.isProcessingUpdate = true;
        
        try {
            commesse.forEach(c => {
                const groupId = this.machineMap.get(c.numeroMacchina) || 'M01';
                const progress = c.percentualeCompletamento || 0;
                const baseColor = this.getStatusColor(c.stato);
                const progressStyle = 
                    `background: linear-gradient(to right, ` +
                    `${baseColor} ${progress}%, ` +
                    `rgba(${this.hexToRgb(baseColor)}, 0.3) ${progress}%); ` +
                    `color: white;`;
                
                // ⚠️ MANTIENI TRIANGOLINO
                const warningIcon = c.datiIncompleti ? ' ⚠️' : '';
                
                // 📅 USA CAMPI CORRETTI
                const dataInizio = c.dataInizioPrevisione || c.dataInizio;
                const dataFine = c.dataFinePrevisione || c.dataFine;
                
                const existingItem = this.timeline.itemsData.get(c.id);
                
                if (existingItem) {
                    // UPDATE
                    this.timeline.itemsData.update({
                        id: c.id,
                        group: groupId,
                        content: `${c.codice} (${Math.round(progress)}%)${warningIcon}`,
                        start: new Date(dataInizio),
                        end: new Date(dataFine),
                        style: progressStyle,
                        title: this.createTooltip(c),
                        datiIncompleti: c.datiIncompleti  // ⚠️ PRESERVA FLAG
                    });
                } else {
                    // ADD
                    this.timeline.itemsData.add({
                        id: c.id,
                        group: groupId,
                        content: `${c.codice} (${Math.round(progress)}%)${warningIcon}`,
                        start: new Date(dataInizio),
                        end: new Date(dataFine),
                        className: 'commessa-item',
                        style: progressStyle,
                        title: this.createTooltip(c),
                        datiIncompleti: c.datiIncompleti
                    });
                }
            });
        } finally {
            this.isProcessingUpdate = false;
        }
    },
    
    /**
     * 🔌 INIZIALIZZA SIGNALR PER REAL-TIME
     */
    initSignalR: async function() {
        try {
            if (typeof signalR === 'undefined') {
                console.warn('⚠️ SignalR library not loaded - real-time updates disabled');
                return;
            }
            
            this.hubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/pianificazione')
                .withAutomaticReconnect()
                .build();
            
            const self = this;
            
            // 📬 RICEVI NOTIFICHE PIANIFICAZIONE
            this.hubConnection.on('PianificazioneUpdated', function(notification) {
                console.log('📨 Received PianificazioneUpdated:', notification);
                
                if (notification.type === 'CommesseUpdated' && notification.commesseAggiornate) {
                    self.updateItemsFromServer(notification.commesseAggiornate);
                } else if (notification.type === 'FullRecalculation') {
                    // Reload completo
                    if (self.dotNetHelper) {
                        self.dotNetHelper.invokeMethodAsync('OnFullRecalculation');
                    }
                }
            });
            
            await this.hubConnection.start();
            console.log('✅ SignalR PianificazioneHub connected');
            
            // Sottoscrivi a updates Gantt
            await this.hubConnection.invoke('SubscribeToGantt');
            
        } catch (error) {
            console.error('❌ SignalR connection error:', error);
        }
    },
    
    /**
     * 🔧 HELPER: Registra riferimento .NET per callbacks
     */
    setDotNetHelper: function(helper) {
        this.dotNetHelper = helper;
    },
    
    /**
     * 🎨 MAPPA STATI → COLORI
     */
    getStatusColor: function(stato) {
        const statusColors = {
            'InProgrammazione': '#2196F3',  // Blu
            'Programmata': '#4CAF50',        // Verde
            'InCorso': '#FF9800',            // Arancione
            'Completata': '#9E9E9E',         // Grigio
            'Sospesa': '#F44336',            // Rosso
            'Default': '#607D8B'             // Blu-Grigio
        };
        return statusColors[stato] || statusColors['Default'];
    },
    
    /**
     * 🎨 HELPER: Hex → RGB
     */
    hexToRgb: function(hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? 
            `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : 
            '96, 125, 139';
    },
    
    /**
     * 🔄 AGGIORNA TASKS (da Blazor)
     */
    updateTasks: function(tasks) {
        if (this.timeline) {
            const items = this.createItemsFromTasks(tasks);
            this.timeline.setItems(items);
        }
    },
    
    /**
     * 🧹 CLEANUP
     */
    destroy: function() {
        if (this.timeline) {
            this.timeline.destroy();
            this.timeline = null;
        }
        if (this.hubConnection) {
            this.hubConnection.stop();
            this.hubConnection = null;
        }
    }
};
```

---

## 7. SISTEMA DI SINCRONIZZAZIONE REAL-TIME

### 7.1 SignalR Hub

**File**: `MESManager.Web/Hubs/PianificazioneHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;
using MESManager.Application.DTOs;

namespace MESManager.Web.Hubs;

public class PianificazioneHub : Hub
{
    private readonly ILogger<PianificazioneHub> _logger;
    
    public PianificazioneHub(ILogger<PianificazioneHub> logger)
    {
        _logger = logger;
    }
    
    // Client chiama questo per iscriversi a updates
    public async Task SubscribeToGantt()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "gantt_updates");
        _logger.LogDebug("Client {ConnectionId} sottoscritto a gantt_updates", 
                         Context.ConnectionId);
    }
    
    // Server chiama questo per notificare tutti i client
    public async Task NotifyPianificazioneUpdated(GanttUpdateNotification notification)
    {
        await Clients.Group("gantt_updates")
            .SendAsync("PianificazioneUpdated", notification);
    }
    
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client PianificazioneHub connesso: {ConnectionId}", 
                              Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client PianificazioneHub disconnesso: {ConnectionId}", 
                              Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

// DTO per notifiche
public class GanttUpdateNotification
{
    public string Type { get; set; } = string.Empty;  // "CommesseUpdated" | "FullRecalculation"
    public List<CommessaGanttDto>? CommesseAggiornate { get; set; }
}
```

### 7.2 Registrazione Hub in Program.cs

**File**: `MESManager.Web/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... altri servizi

builder.Services.AddSignalR();

var app = builder.Build();

// ... middleware

app.MapBlazorHub();
app.MapHub<PianificazioneHub>("/hubs/pianificazione");  // ⚠️ ROUTE HUB
app.MapFallbackToPage("/_Host");

app.Run();
```

### 7.3 Utilizzo Hub nel Service

Aggiungi al `PianificazioneEngineService.cs`:

```csharp
private readonly IHubContext<PianificazioneHub> _hubContext;

public PianificazioneEngineService(
    MesManagerDbContext context,
    IPianificazioneService pianificazioneService,
    IHubContext<PianificazioneHub> hubContext,  // ⚠️ INJECT
    ILogger<PianificazioneEngineService> logger)
{
    _context = context;
    _pianificazioneService = pianificazioneService;
    _hubContext = hubContext;  // ⚠️ STORE
    _logger = logger;
}

// Dopo RicalcolaTutteCommesseAsync():
await _hubContext.Clients.Group("gantt_updates")
    .SendAsync("PianificazioneUpdated", new GanttUpdateNotification
    {
        Type = "FullRecalculation"
    });
```

---

## 8. ALGORITMO DI ACCODAMENTO RIGIDO

### 8.1 Principi Fondamentali

**OBIETTIVO**: Garantire che le commesse sulla stessa macchina NON si sovrappongano mai.

**REGOLE**:
1. Ogni commessa ha `OrdineSequenza` (1, 2, 3...)
2. `DataInizio[n] = DataFine[n-1]` (accodamento stretto)
3. Dopo ogni spostamento → ricalcolo cascata intera macchina
4. Considera calendario lavorativo e festivi

### 8.2 Pseudocodice

```
ALGORITMO: SpostaCommessa(commessaId, targetMacchina, targetDataInizio?)

1. Carica commessa da spostare
2. Carica tutte commesse macchina destinazione (esclusa commessa da spostare)
3. Determina posizione inserimento:
   
   IF targetDataInizio specificata:
       trova prima commessa che inizia DOPO targetDataInizio
       
       IF trovata:
           nuovoOrdine = ordine commessa trovata
           shifta tutte successive +1
           
           // ⚠️ ACCODAMENTO RIGIDO
           commessaPrecedente = commessa ordine nuovoOrdine-1
           IF commessaPrecedente esiste:
               dataInizio = commessaPrecedente.DataFinePrevisione
           ELSE:
               dataInizio = targetDataInizio
       ELSE:
           nuovoOrdine = maxOrdine + 1 (accoda in fondo)
           ultimaCommessa = commessa con max DataFinePrevisione
           
           // ⚠️ ACCODAMENTO RIGIDO
           IF ultimaCommessa.DataFinePrevisione > targetDataInizio:
               dataInizio = ultimaCommessa.DataFinePrevisione
           ELSE:
               dataInizio = targetDataInizio
   ELSE:
       nuovoOrdine = maxOrdine + 1
       ultimaCommessa = commessa con max DataFinePrevisione
       dataInizio = ultimaCommessa.DataFinePrevisione ?? Now

4. Aggiorna commessa spostata:
   - NumeroMacchina = targetMacchina
   - OrdineSequenza = nuovoOrdine
   - DataInizioPrevisione = dataInizio
   - DataFinePrevisione = CalcolaDataFine(dataInizio, durata, calendario, festivi)

5. RicalcolaAcqueMacchina(targetMacchina):
   FOR EACH commessa IN macchina ORDER BY OrdineSequenza:
       IF prima commessa:
           mantieni DataInizioPrevisione esistente
       ELSE:
           DataInizioPrevisione = commessaPrecedente.DataFinePrevisione
       
       DataFinePrevisione = CalcolaDataFine(DataInizioPrevisione, durata, calendario, festivi)
       
6. IF macchinaOrigine ≠ targetMacchina:
   RicalcolaAcqueMacchina(macchinaOrigine)

7. Salva database
8. Notifica SignalR
9. Return commesse aggiornate
```

---

## 9. CALCOLI TEMPORALI E CALENDARIO

### 9.1 Formula Durata Prevista

```
durataPrevistaMinuti = tempoSetupMinuti + (tempoProduzione / 60)

dove:
  tempoProduzione = tempoCicloSecondi * numeroCicli
  numeroCicli = CEILING(quantitaRichiesta / numeroFigure)
```

**Esempio**:
```
TempoCiclo: 120 sec
NumeroFigure: 4 (4 pezzi per ciclo)
QuantitàRichiesta: 1000 pezzi
TempoSetup: 90 minuti

Calcolo:
numeroCicli = CEILING(1000 / 4) = 250 cicli
tempoProduzione = 120 * 250 = 30.000 sec = 500 min
durataTotale = 90 + 500 = 590 minuti (9h 50m)
```

### 9.2 Calcolo Data Fine con Calendario

```
FUNZIONE CalcolaDataFine(dataInizio, durataMinuti, oreGiorno, giorniSettimana, festivi):
    
    minutiPerGiorno = oreGiorno * 60
    dataCorrente = dataInizio
    minutiRimanenti = durataMinuti
    
    WHILE minutiRimanenti > 0:
        // Salta weekend
        IF giorniSettimana == 5:  // Lun-Ven
            IF dataCorrente.DayOfWeek == Sabato:
                dataCorrente += 2 giorni  // Salta a lunedì
                CONTINUE
            IF dataCorrente.DayOfWeek == Domenica:
                dataCorrente += 1 giorno
                CONTINUE
        
        // Salta festivi
        IF dataCorrente.Date IN festivi:
            dataCorrente += 1 giorno
            CONTINUE
        
        // Consuma minuti
        IF minutiRimanenti >= minutiPerGiorno:
            minutiRimanenti -= minutiPerGiorno
            dataCorrente += 1 giorno
        ELSE:
            dataCorrente += minutiRimanenti (minuti)
            minutiRimanenti = 0
    
    RETURN dataCorrente
```

**Esempio**:
```
DataInizio: Venerdì 7 Feb 2026 ore 10:00
Durata: 2400 minuti (40 ore = 5 giorni lavorativi)
Calendario: 8h/giorno, 5 giorni/settimana
Festivi: Nessuno

Calcolo:
Ven 7: consuma 8h → rimangono 32h
Sab 8: SALTA (weekend)
Dom 9: SALTA (weekend)
Lun 10: consuma 8h → rimangono 24h
Mar 11: consuma 8h → rimangono 16h
Mer 12: consuma 8h → rimangono 8h
Gio 13: consuma 8h → rimangono 0h

DataFine: Giovedì 13 Feb 2026 ore 18:00
```

---

## 10. CONFIGURAZIONE E SETUP

### 10.1 Passaggi Setup Iniziale

```bash
# 1. Crea database SQL Server
CREATE DATABASE MESManager_Prod;

# 2. Applica migrations EF Core
cd MESManager.Infrastructure
dotnet ef migrations add InitialGantt --startup-project ../MESManager.Web
dotnet ef database update --startup-project ../MESManager.Web

# 3. Seed dati iniziali
# Inserisci almeno:
# - 3 macchine in Macchine (M01, M02, M03)
# - 1 record in ImpostazioniProduzione
# - Alcuni articoli con TempoCiclo e NumeroFigure

# 4. Installa Vis-Timeline
# Aggiungi in _Host.cshtml o App.razor:
<script src="https://unpkg.com/vis-timeline@7.7.0/standalone/umd/vis-timeline-graph2d.min.js"></script>
<link href="https://unpkg.com/vis-timeline@7.7.0/styles/vis-timeline-graph2d.min.css" rel="stylesheet" />

# 5. Copia gantt-macchine.js in wwwroot/js/gantt/

# 6. Registra servizi in Program.cs
builder.Services.AddScoped<IPianificazioneService, PianificazioneService>();
builder.Services.AddScoped<IPianificazioneEngineService, PianificazioneEngineService>();
builder.Services.AddSignalR();

# 7. Test endpoint API
GET http://localhost:5156/api/pianificazione
# Deve ritornare array CommessaGanttDto (anche vuoto)

# 8. Apri Gantt
http://localhost:5156/programma/gantt-macchine
```

### 10.2 Seed Dati Esempio

```sql
-- Macchine
INSERT INTO Macchine (Id, Codice, Nome, AttivaInGantt, OrdineVisualizazione)
VALUES 
(NEWID(), 'M01', 'Macchina 01', 1, 1),
(NEWID(), 'M02', 'Macchina 02', 1, 2),
(NEWID(), 'M03', 'Macchina 03', 1, 3);

-- Impostazioni Produzione
INSERT INTO ImpostazioniProduzione 
(Id, TempoSetupMinuti, OreLavorativeGiornaliere, GiorniLavorativiSettimanali)
VALUES 
(NEWID(), 90, 8, 5);

-- Articolo esempio
INSERT INTO Articoli (Id, Codice, Descrizione, TempoCiclo, NumeroFigure, Prezzo, UnitaMisura)
VALUES 
(NEWID(), 'ART001', 'Articolo Test', 120, 4, 10.50, 'PZ');

-- Commessa esempio
DECLARE @ArticoloId UNIQUEIDENTIFIER = (SELECT Id FROM Articoli WHERE Codice = 'ART001');

INSERT INTO Commesse 
(Id, Codice, Description, NumeroMacchina, OrdineSequenza, 
 DataInizioPrevisione, QuantitaRichiesta, UoM, ArticoloId, Stato)
VALUES 
(NEWID(), 'COM001', 'Commessa Test 1', '1', 1, 
 GETDATE(), 1000, 'PZ', @ArticoloId, 'Programmata');
```

---

## 11. TESTING E DEBUGGING

### 11.1 Checklist Verifica Funzionamento

✅ **Test 1: Gantt Visualizza Dati**
```
1. Apri http://localhost:5156/programma/gantt-macchine
2. Verifica presenza gruppi (M01, M02, M03)
3. Verifica presenza items (barre colorate)
4. Console browser: nessun errore JavaScript
```

✅ **Test 2: Drag & Drop Funziona**
```
1. Trascina una commessa su altra posizione
2. Rilascia mouse
3. Verifica alert successo
4. Verifica che altre commesse si siano riposizionate (accodamento)
5. Console browser: "✅ Move result: {success: true}"
```

✅ **Test 3: Accodamento Rigido**
```
1. Trascina COM001 sopra COM002 (sovrapposizione)
2. Durante drag: COM001 si accoda automaticamente DOPO COM002
3. Dopo drop: tutte commesse macchina hanno date sequential
4. Database: SELECT DataInizioPrevisione, DataFinePrevisione FROM Commesse WHERE NumeroMacchina = '1' ORDER BY OrdineSequenza
   → DataInizio[n] == DataFine[n-1]
```

✅ **Test 4: Ricalcola Tutto**
```
1. Click bottone "Ricalcola Tutto"
2. Verifica loading
3. Verifica alert "Ricalcolo completato"
4. Gantt si ricarica con date aggiornate
```

✅ **Test 5: SignalR Real-Time**
```
1. Apri 2 tab browser su Gantt
2. In tab A: sposta commessa
3. In tab B: vedi update automatico senza refresh
4. Console tab B: "📨 Received PianificazioneUpdated"
```

### 11.2 Debugging Common Issues

**PROBLEMA**: Gantt vuoto (nessuna barra)

```
DEBUG:
1. Console browser → Cerca errori JavaScript
2. Network tab → Verifica GET /api/pianificazione ritorna 200 con dati
3. SQL: SELECT COUNT(*) FROM Commesse WHERE NumeroMacchina IS NOT NULL
   → Deve essere > 0
4. SQL: SELECT COUNT(*) FROM Commesse WHERE DataInizioPrevisione IS NULL OR DataFinePrevisione IS NULL
   → Queste commesse sono ESCLUSE dal Gantt
5. Console browser → "✅ Created X items from tasks"
   → Se X = 0, problema filtro lato JS
```

**PROBLEMA**: Drag non salva (barra torna indietro)

```
DEBUG:
1. Console browser → Cerca errore fetch POST /api/pianificazione/sposta
2. Network tab → Verifica response 200 vs 400/500
3. Se 400: leggi errorMessage in response body
4. Se 500: leggi logs backend
5. Verifica CommessaId esiste: SELECT * FROM Commesse WHERE Id = '...'
```

**PROBLEMA**: Accodamento non funziona (sovrapposizioni)

```
DEBUG:
1. Verifica stack: false in options Vis-Timeline
2. Aggiungi console.log in JS onMove per vedere targetMacchina e targetDataInizio
3. SQL dopo spostamento:
   SELECT NumeroMacchina, OrdineSequenza, DataInizioPrevisione, DataFinePrevisione 
   FROM Commesse 
   WHERE NumeroMacchina = '1'
   ORDER BY OrdineSequenza
   
   Verifica:
   - OrdineSequenza sia sequenziale (1, 2, 3...)
   - DataInizio[2] == DataFine[1]
   - DataInizio[3] == DataFine[2]
```

**PROBLEMA**: Date sbagliate (non considera weekend/festivi)

```
DEBUG:
1. Verifica ImpostazioniProduzione.GiorniLavorativiSettimanali = 5
2. SQL: SELECT * FROM Festivi → Verifica festivi inseriti correttamente
3. Aggiungi breakpoint in CalcolaDataFinePrevistaConFestivi
4. Verifica che festivi HashSet contenga date corrette
5. Test manuale: DataInizio = Venerdì 17:00, Durata = 2h
   → DataFine dovrebbe essere Lunedì 09:00 (se 8h/giorno)
```

---

## 12. ESTENSIONI FUTURE

### 12.1 Funzionalità da Implementare

**🔹 Validazione Conflitti Lato Server**
```csharp
// Prima di salvare spostamento
var sovrapposizioni = await _context.Commesse
    .Where(c => c.NumeroMacchina == targetMacchina
             && c.DataInizioPrevisione < nuovaDataFine
             && c.DataFinePrevisione > nuovaDataInizio)
    .ToListAsync();

if (sovrapposizioni.Any())
{
    return new SpostaCommessaResponse 
    { 
        Success = false, 
        ErrorMessage = "Sovrapposizione rilevata con altre commesse" 
    };
}
```

**🔹 Zoom Temporale Dinamico**
```javascript
// Aggiungi bottoni Zoom in/out
timeline.setWindow({
    start: startDate,
    end: endDate
});
```

**🔹 Filtri Gantt**
```javascript
// Filtra per stato
const filteredTasks = tasks.filter(t => t.stato === 'InCorso');
GanttMacchine.updateTasks(filteredTasks);
```

**🔹 Export Gantt come Immagine**
```javascript
function exportGanttAsImage() {
    const container = document.getElementById('gantt-chart');
    html2canvas(container).then(canvas => {
        const link = document.createElement('a');
        link.download = 'gantt.png';
        link.href = canvas.toDataURL();
        link.click();
    });
}
```

**🔹 Milestone (Traguardi)**
```javascript
// Aggiungi markers speciali per date consegna
{
    id: 'milestone-1',
    content: '🎯 Consegna',
    start: new Date(task.dataConsegna),
    type: 'point',  // Marker punto
    className: 'milestone'
}
```

**🔹 Dipendenze Tra Commesse**
```csharp
// Tabella CommesseDipendenze
CommessaId (FK) → CommessaDipendenteId (FK)
Tipo: FinishToStart | StartToStart | FinishToFinish

// Visualizzazione
timeline.setOptions({
    editable: {
        updateTime: function(item, callback) {
            // Verifica dipendenze prima di permettere spostamento
        }
    }
});
```

---

## 📌 CONCLUSIONE E CHECKLIST RAPIDA

### Checklist Replicazione Completa

- [ ] **Database**: Tabelle create (Commesse, Articoli, Macchine, ImpostazioniProduzione, Festivi)
- [ ] **Backend DTOs**: CommessaGanttDto, SpostaCommessaRequest, SpostaCommessaResponse
- [ ] **Backend Services**: PianificazioneService, PianificazioneEngineService
- [ ] **Backend Controller**: PianificazioneController con endpoint GET/POST
- [ ] **SignalR Hub**: PianificazioneHub configurato
- [ ] **Frontend Blazor**: GanttMacchine.razor componente creato
- [ ] **JavaScript**: gantt-macchine.js con Vis-Timeline integrato
- [ ] **CDN Vis-Timeline**: Script e CSS in _Host.cshtml
- [ ] **Program.cs**: AddSignalR() e MapHub configurati
- [ ] **Seed Dati**: Almeno 1 macchina, 1 articolo, 1 commessa
- [ ] **Test Drag&Drop**: Funzionante
- [ ] **Test Accodamento**: Nessuna sovrapposizione
- [ ] **Test Real-Time**: SignalR sincronizza tra tab

### Punti Critici da Ricordare

⚠️ **Campo Date**: Usa sempre `DataInizioPrevisione` e `DataFinePrevisione` (non DataInizio/DataFine)
⚠️ **NumeroMacchina**: È string nel DB ma va parsato a int per DTO
⚠️ **Filtro Gantt**: Solo commesse con NumeroMacchina != NULL e date complete
⚠️ **Accodamento**: Sempre ricalcola tutta la macchina dopo spostamento
⚠️ **Vis-Timeline**: Usa versione 7.7.0 (versioni più nuove hanno API diverse)
⚠️ **SignalR Group**: Client deve chiamare `SubscribeToGantt()` dopo connessione

---

**🎯 RISULTATO FINALE**: Sistema Gantt completo con drag&drop, accodamento rigido, calcoli temporali intelligenti, sincronizzazione real-time e visualizzazione progressiva stato commesse.

**📖 RIFERIMENTI**:
- Vis-Timeline Docs: https://visjs.github.io/vis-timeline/docs/timeline/
- SignalR Docs: https://learn.microsoft.com/en-us/aspnet/core/signalr/
- EF Core: https://learn.microsoft.com/en-us/ef/core/

---

*Documento generato il 4 Febbraio 2026 per MESManager v1.23*
