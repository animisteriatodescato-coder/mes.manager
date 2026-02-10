# 06 - Gantt Macchine - Analisi Tecnica

> **Scopo**: Analisi dettagliata struttura e funzionamento del Gantt chart  
> **Ultima Revisione**: 6 Febbraio 2026 - v1.29 (Fix Calendario Lavoro)

---

## 📌 Versioni Storiche

| Versione | Data | Milestone |
|----------|------|-----------|
| **v1.29** | 6 Feb 2026 | 🐛 Fix CalendarioLavoro ignorato (date realistiche + export) |
| **v1.28** | 5 Feb 2026 | 🎨 UX Revolution (gradazione, export, fix sovrapposizione) |
| **v1.27** | 5 Feb 2026 | 🏗️ Refactoring completo (Clean Arch, -150 righe, N+1 fix) |
| **v1.26.1** | 5 Feb 2026 | ✅ FIX UX Polish (timer %, background rosso, triangolino) |
| **v1.26** | 5 Feb 2026 | 🔄 Paradigma GANTT-FIRST (no reload, live updates) |
| v1.25 | Gen 2026 | Accodamento robusto + vincoli |
| v1.24 | Gen 2026 | Implementazione base Vis-Timeline |

---

## 🏗️ Architettura a 3 Livelli (v1.28)

### Backend (API Layer)

**Endpoint**: `GET /api/pianificazione`

**Controller**: [PianificazioneController.cs](../MESManager.Web/Controllers/PianificazioneController.cs)

**Query EF Core**:
```csharp
_context.Commesse
    .Include(c => c.Articolo)
    .Where(c => c.NumeroMacchina != null)
    .OrderBy(c => c.NumeroMacchina)
    .ThenBy(c => c.OrdineSequenza)
    .ToListAsync();
```

---

### Frontend (Blazor Component)

**Componente**: [GanttMacchine.razor](../MESManager.Web/Components/Pages/Programma/GanttMacchine.razor)

**Responsabilità**:
- Carica dati da API
- Passa JSON a JavaScript
- Gestisce refresh

---

### Visualizzazione (JavaScript + Vis-Timeline)

**Libreria**: Vis-Timeline

**File**: [gantt-macchine.js](../MESManager.Web/wwwroot/js/gantt/gantt-macchine.js)

**Responsabilità**:
- Rendering chart
- Drag & drop
- Eventi interattivi

---

## 📊 Flusso Dati Completo

```
Database (SQL Server)
    ↓
EF Core Query (Commesse + Articolo)
    ↓
MapToGanttDto() → CommessaGanttDto
    ↓
API Response (JSON)
    ↓
Blazor OnInitializedAsync() → LoadCommesseGantt()
    ↓
OnAfterRenderAsync() → Serializza in JSON
    ↓
JS GanttMacchine.initialize(elementId, settings)
    ↓
Vis-Timeline renderizza chart
```

---

## ✅ Regola UI GANTT-first (v1.30.3)

- **Commesse Aperte**: nessuna assegnazione macchina in griglia. L'unico ingresso e' il pulsante "Carica su Gantt" (usa `POST /api/pianificazione/carica-su-gantt/{id}` con selezione riga).
- **Programma Macchine**: nessun riordino legacy. Gli spostamenti avvengono tramite Gantt (drag) o endpoint engine (`POST /api/pianificazione/sposta-commessa`).
- **Single source of truth**: `PianificazioneEngineService` gestisce ordine, date e vincoli.
- **Auto-completamento**: se `DataFinePrevisione` e' nel passato, la commessa passa a `StatoProgramma=Completata`.
- **Visibilita' completate**: sul Gantt le commesse `Completata` restano visibili in grigio/trasparente per ripristino manuale.
- **Archiviazione**: dal Gantt si puo' archiviare la commessa selezionata (scompare dal Gantt).
- **Export**: `/api/pianificazione/esporta-su-programma` esporta solo commesse attive (non completate/archiviate).

---

## 📦 CommessaGanttDto (35 proprietà)

```csharp
public class CommessaGanttDto
{
    // Identificazione
    public Guid Id { get; set; }
    public string Codice { get; set; }
    public string Description { get; set; }
    public string Stato { get; set; }
    public string ColoreStato { get; set; }
    
    // Assegnazione Macchina
    public int? NumeroMacchina { get; set; }
    public string? NomeMacchina { get; set; }
    public int OrdineSequenza { get; set; }
    
    // Date Pianificazione
    public DateTime? DataInizioPrevisione { get; set; }
    public DateTime? DataFinePrevisione { get; set; }
    public DateTime? DataInizioProduzione { get; set; }
    public DateTime? DataFineProduzione { get; set; }
    
    // Dati Produttivi
    public decimal QuantitaRichiesta { get; set; }
    public string UoM { get; set; }
    public DateTime? DataConsegna { get; set; }
    
    // Calcolo Tempi
    public int? TempoCicloSecondi { get; set; }
    public int? NumeroFigure { get; set; }
    public int TempoSetupMinuti { get; set; }
    public int DurataPrevistaMinuti { get; set; }
    
    // Stato Avanzamento
    public int PercentualeCompletamento { get; set; }  // 0-100
    public bool DatiIncompleti { get; set; }
}
```

---

## 🎨 Inizializzazione JavaScript

### A. Creazione Groups (Macchine)

```javascript
const groups = settings.machines.map(m => ({ 
    id: m.codice,                    // "M01", "M02"
    content: m.nome,                 // "Macchina 01"
    order: m.ordineVisualizazione    // Per ordinamento
}));
```

---

### B. Mapping NumeroMacchina → Codice

```javascript
const machineMap = new Map();
settings.machines.forEach(m => {
    machineMap.set(m.numeroMacchina, m.codice);
});
// Es: 1 → "M01", 2 → "M02"
```

---

### C. Filtro Critico

```javascript
items = tasks
    .filter(task => task.dataInizio && task.dataFine && task.numeroMacchina)
    .map(task => { ... });
```

**⚠️ IMPORTANTE**: Commesse senza `DataInizioPrevisione` o `DataFinePrevisione` vengono **ESCLUSE** dal Gantt!

---

### D. Creazione Items (Commesse)

```javascript
items = tasks.map(task => {
    const groupId = machineMap.get(task.numeroMacchina);
    const progress = task.percentualeCompletamento || 0;
    const baseColor = this.getStatusColor(task.stato);
    
    // Gradient progressivo (%)
    const progressStyle = `background: linear-gradient(to right, 
        ${baseColor} ${progress}%, 
        rgba(${baseColor}, 0.3) ${progress}%)`;
    
    return {
        id: task.id,
        group: groupId,           // "M01", "M02"
        content: `${task.codice} (${Math.round(progress)}%)`,
        start: new Date(task.dataInizio),
        end: new Date(task.dataFine),
        className: 'commessa-item',
        style: progressStyle,
        title: `${task.description}\nQuantità: ${task.quantita}\nStato: ${task.stato}`
    };
});
```

---

### E. Configurazione Vis-Timeline

```javascript
const options = {
    editable: {
        add: false,
        updateTime: true,    // ✅ Drag temporale
        updateGroup: true,   // ✅ Drag tra macchine
        remove: false
    },
    stack: false,            // ❌ NO sovrapposizione
    orientation: 'top',
    groupOrder: 'order',
    margin: { item: 10, axis: 5 },
    start: [data minima items],
    end: [data massima items]
};

this.timeline = new vis.Timeline(container, itemsData, groups, options);
```

---

## 🖱️ Drag & Drop Management - PARADIGMA GANTT-FIRST (v16)

### Architettura GANTT-FIRST

**Flusso Nuovo (5 Febbraio 2026)**:
```
User drag commessa su Gantt
    ↓
JavaScript onMove callback
    ↓
Estrai: item.id, targetMacchina, item.start (posizione esatta)
    ↓
POST /api/pianificazione/sposta-commessa
    ↓
Service: CHECK SOVRAPPOSIZIONE
    ↓
    ├─ NO overlap → POSIZIONE ESATTA (usa item.start)
    │                ↓
    │           Ricalcola SOLO successive
    │
    └─ Overlap → ACCODAMENTO (dopo commessa sovrapposta)
                    ↓
               Ricalcola SOLO successive
    ↓
Response: { commessaSpostata, commesseSuccessive }
    ↓
JavaScript: updateItemsFromServer() (NO RELOAD!)
    ↓
SignalR notifica altre sessioni
```

### Event Handler "onMove" - Validazione Robusta (v16)

```javascript
onMove: async function(item, callback) {
    // 1. Lock check
    if (item.bloccata) {
        alert('Impossibile spostare una commessa bloccata.');
        callback(null);
        return;
    }

    // 2. Estrai macchina target
    const targetMacchina = self.reverseMachineMap.get(item.group);
    
    // 3. VALIDAZIONE INPUT (v15-v16)
    if (!targetMacchina || isNaN(parseInt(targetMacchina)) || parseInt(targetMacchina) < 1) {
        console.error('❌ Numero macchina non valido:', { group: item.group, targetMacchina });
        alert('Errore: numero macchina non valido.');
        callback(null);
        return;
    }

    // 4. POST con POSIZIONE ESATTA (v16 - GANTT-FIRST)
    const response = await fetch('/api/pianificazione/sposta-commessa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            commessaId: item.id,
            targetMacchina: parseInt(targetMacchina),
            targetDataInizio: item.start.toISOString(), // ← VINCOLO ESATTO
            insertBeforeCommessaId: null
        })
    });

    // 5. Handle response
    const result = await response.json();
    
    if (result.success) {
        // v16: NO location.reload()! Aggiorna via updateItemsFromServer()
        self.updateItemsFromServer(result.commesseAggiornate, result.updateVersion);
        
        if (result.commesseMacchinaOrigine) {
            self.updateItemsFromServer(result.commesseMacchinaOrigine, result.updateVersion);
        }
        
        // Notify Blazor
        if (self.dotNetHelper) {
            self.dotNetHelper.invokeMethodAsync('OnCommessaMoved', result);
        }
        
        callback(item); // ✅ Accept move
    } else {
        alert('Errore: ' + result.errorMessage);
        callback(null); // ❌ Cancel move
    }
}
```

**Differenze v16 vs v15**:
- ✅ **NO `location.reload()`**: Aggiornamento live via `updateItemsFromServer()`
- ✅ **Posizione ESATTA**: `targetDataInizio` rispettata dal Service
- ✅ **Update Incrementale**: Solo commesse coinvolte, non fullpage refresh

---
        console.error('❌ Numero macchina non valido:', { group: item.group, targetMacchina });
        alert('Errore: numero macchina non valido. Ricaricare la pagina.');
        callback(null);
        return;
    }
    
    // 4. POST al server con POSIZIONE ESATTA
    const response = await fetch('/api/pianificazione/sposta-commessa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            commessaId: item.id,
            targetMacchina: parseInt(targetMacchina),
            targetDataInizio: item.start.toISOString(), // ← POSIZIONE DRAG
            insertBeforeCommessaId: null
        })
    });
    
    // 5. Gestione risposta SENZA RELOAD (v16)
    const result = await response.json();
    
    if (result.success) {
        // ✅ Update solo commesse modificate (ottimizzato)
        self.updateItemsFromServer(result.commesseAggiornate, result.updateVersion);
        
        if (result.commesseMacchinaOrigine) {
            self.updateItemsFromServer(result.commesseMacchinaOrigine, result.updateVersion);
        }
        
        // ✅ NO location.reload() - aggiornamento fluido
        callback(item); // Accept the move
    } else {
        alert('Errore: ' + result.errorMessage);
        callback(null); // Cancel the move
    }
}
```

**Miglioramenti v16 (GANTT-FIRST)**:
- ❌ **RIMOSSO** `location.reload()` → UX fluida senza page refresh
- ✅ **Posizione esatta** rispettata (se no overlap)
- ✅ **Check sovrapposizione** server-side
- ✅ **Accodamento intelligente** (solo se overlap)
- ✅ **Update ottimizzato** (solo commesse modificate, non full recalc)
- ✅ **SignalR** per sync altre sessioni

### Calcolo Avanzamento Real-Time (v16)

```javascript
// CALCOLO % AVANZAMENTO REAL-TIME se in produzione
let progress = task.percentualeCompletamento || 0;
if (task.stato === 'InProduzione' && task.dataInizioPrevisione && task.dataFinePrevisione) {
    const now = new Date();
    const start = new Date(task.dataInizioPrevisione);
    const end = new Date(task.dataFinePrevisione);
    
    if (now >= start && now <= end) {
        const totalDuration = end - start;
        const elapsed = now - start;
        progress = Math.min(100, Math.max(0, (elapsed / totalDuration) * 100));
        // ✅ Progress segue linea rossa DateTime.Now
    } else if (now > end) {
        progress = 100; // Dovrebbe essere completata
    }
}
```

**Comportamento**:
- Commesse **InProduzione**: % calcolata in base a `DateTime.Now` vs date previste
- Commesse **Altre stati**: % statica da DB
- **Update automatico** ogni render (segue linea tempo corrente)

---

```javascript
this.timeline.on('changed', async function (properties) {
    // 1. Estrai item trascinato
    const itemId = properties.items[0];
    const item = self.timeline.itemsData.get(itemId);
    
    // 2. Estrai numeroMacchina dal group
    const machineCode = item.group;  // "M01"
    const match = machineCode.match(/\d+/);
    const targetMacchina = match ? match[0] : null;
    
    // 3. VALIDAZIONE INPUT (v15 - Fix 404 errors)
    if (!targetMacchina || isNaN(parseInt(targetMacchina)) || parseInt(targetMacchina) < 1) {
        console.error('❌ Numero macchina non valido:', { group: item.group, targetMacchina });
        alert('Errore: numero macchina non valido. Ricaricare la pagina.');
        callback(null);  // Blocca drag
        return;
    }
    
    // 4. POST al server con URL corretto
    const response = await fetch('/api/pianificazione/sposta-commessa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            commessaId: item.id,
            targetMacchina: parseInt(targetMacchina),
            targetDataInizio: item.start.toISOString()
        })
    });
    
    // 5. Error handling robusto con try-catch
    let result;
    try {
        result = await response.json();
    } catch (jsonError) {
        console.error('❌ Errore parsing JSON:', jsonError);
        const text = await response.text();
        console.error('❌ Response raw:', text);
        alert('Errore server: risposta non valida');
        return;
    }
    
    // 6. Ricarica per ricalcoli (se successo)
    if (result.success) {
        console.log('✓ Spostamento riuscito, updateVersion:', result.updateVersion);
        location.reload();
    } else {
        console.error('✗ Errore spostamento:', result.errorMessage);
        alert('Errore: ' + result.errorMessage);
    }
});
```

**Miglioramenti v15**:
- ✅ Validazione numero macchina: null check, isNaN, range > 0
- ✅ Error handling JSON con try-catch e fallback a text
- ✅ Logging dettagliato console con simboli ✓/✗
- ✅ Messaggi utente user-friendly
- ✅ Block drag se input non valido

---

## 🎨 Colori Stati

```javascript
getStatusColor(stato) {
    const colors = {
        'NonPianificata': '#757575',     // Grigio
        'Pianificata': '#2196F3',        // Blu
        'InProduzione': '#4CAF50',       // Verde
        'Completata': '#00C853',         // Verde scuro
        'InRitardo': '#F44336',          // Rosso
        'Annullata': '#9E9E9E'           // Grigio chiaro
    };
    return colors[stato] || '#757575';
}
```

---

## ⚙️ Backend - Aggiornamento Posizione

### PianificazioneController.AggiornaPosizioneCommessa

```csharp
[HttpPost("aggiorna-posizione")]
public async Task<IActionResult> AggiornaPosizioneCommessa([FromBody] AggiornaPosizioneDto dto)
{
    var commessa = await _context.Commesse.FindAsync(dto.CommessaId);
    if (commessa == null) return NotFound();
    
    // 1. Aggiorna macchina e data inizio
    commessa.NumeroMacchina = dto.NumeroMacchina;
    commessa.DataInizioPrevisione = dto.DataInizioPrevisione;
    
    // 2. Ricalcola data fine
    var durata = await _pianificazioneService.CalcolaDurataPrevistaMinuti(commessa.Id);
    commessa.DataFinePrevisione = dto.DataInizioPrevisione.AddMinutes(durata);
    
    // 3. Ricalcola sequenze
    await RicalcolaSequenzeAsync(dto.NumeroMacchina);
    
    await _context.SaveChangesAsync();
    return Ok();
}
```

---

### RicalcolaSequenzeAsync

```csharp
private async Task RicalcolaSequenzeAsync(int numeroMacchina)
{
    var commesse = await _context.Commesse
        .Where(c => c.NumeroMacchina == numeroMacchina)
        .OrderBy(c => c.DataInizioPrevisione)
        .ToListAsync();
    
    for (int i = 0; i < commesse.Count; i++)
    {
        commesse[i].OrdineSequenza = i + 1;
    }
    
    await _context.SaveChangesAsync();
}
```

---

## 🧮 Calcolo Durata Prevista

### PianificazioneService.CalcolaDurataPrevistaMinuti

```csharp
public async Task<int> CalcolaDurataPrevistaMinuti(Guid commessaId)
{
    var commessa = await _context.Commesse
        .Include(c => c.Articolo)
        .FirstOrDefaultAsync(c => c.Id == commessaId);
    
    if (commessa == null) return 480;  // Default 8 ore
    
    // Setup time (da ImpostazioniGantt)
    var impostazioni = await _context.ImpostazioniGantt.FirstOrDefaultAsync();
    var tempoSetup = impostazioni?.TempoAttrezzaggioDefault ?? 30;
    
    // Tempo ciclo
    var tempoCiclo = commessa.Articolo?.TempoCicloStandard ?? 0;
    if (tempoCiclo == 0) return tempoSetup + 480;  // Default 8h
    
    // Calcolo: Setup + (Quantità * TempoCiclo / Figure)
    var figure = commessa.NumeroFigure ?? 1;
    var tempoProduzione = (int)((commessa.QuantitaRichiesta * tempoCiclo) / figure / 60);
    
    return tempoSetup + tempoProduzione;
}
```

---

## 🚦 Indicatori Visivi

### Triangolino ⚠️ per Dati Incompleti

Se `commessa.DatiIncompleti == true`:

```javascript
content: `⚠️ ${task.codice} (${Math.round(progress)}%)`
```

**Tooltip**:
```javascript
title: "ATTENZIONE: Dati incompleti (usato 8h standard)"
```

**Campo DTO**:
```csharp
public bool DatiIncompleti { get; set; }
```

Impostato a `true` quando:
- `TempoCicloStandard` mancante
- `NumeroFigure` mancante
- Usato tempo default 8 ore (480 min)

---

## 🔄 Refresh Automatico (SignalR)

### Hub Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/productionHub")
    .build();

connection.on("OnCommessaUpdated", function (commessaId) {
    // Ricarica Gantt
    GanttMacchine.refresh();
});
```

---

## 📋 Problemi Comuni

### ❌ Commesse non appaiono nel Gantt

**Causa 1**: Mancano `DataInizioPrevisione` o `DataFinePrevisione`

**Soluzione**: Assegna date in "Commesse Aperte" o "Programma Macchine"

**Causa 2**: `NumeroMacchina` è `NULL`

**Soluzione**: Assegna macchina alla commessa

---

### ❌ Drag & drop non funziona

**Causa**: `editable.updateTime` o `editable.updateGroup` = false

**Soluzione**: Verifica options in gantt-macchine.js:
```javascript
editable: {
    updateTime: true,
    updateGroup: true
}
```

---

### ❌ Colori non corretti

**Causa**: Stato commessa non riconosciuto

**Soluzione**: Verifica enum `StatoCommessa` nel backend coincida con i colori JS

---

### ❌ Sovrapposizione commesse

**Causa**: `stack: true` in options

**Soluzione**: Imposta `stack: false` per evitare sovrapposizioni visive

---

---

## 🏗️ ARCHITETTURA v2.0 - SCHEDULING ROBUSTO

> **⚠️ IMPORTANTE**: Dalla versione 2.0 (4 Feb 2026), il sistema è stato completamente rifattorizzato.  
> Vedi documentazione completa: [GANTT-REFACTORING-v2.0.md](GANTT-REFACTORING-v2.0.md)

### 🔐 Optimistic Concurrency Control

**Problema Risolto**: Due utenti modificavano contemporaneamente la stessa commessa, l'ultimo sovrascriveva il primo

**Implementazione**:
```csharp
// Entità Commessa
public byte[] RowVersion { get; set; } = Array.Empty<byte>();

// DbContext Configuration
modelBuilder.Entity<Commessa>()
    .Property(c => c.RowVersion)
    .IsRowVersion();
```

**Gestione Conflitto**:
```csharp
try {
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException) {
    return BadRequest(new { error = "I dati sono stati modificati da un altro utente. Ricarica la pagina." });
}
```

**Frontend**: HTTP 409 Conflict → alert utente + reload

---

### 🔒 Sistema Lock e Priorità

**Nuovi Campi Commessa**:
```csharp
public int Priorita { get; set; } = 100;        // Più basso = più urgente
public bool Bloccata { get; set; } = false;     // Lock pianificazione
```

**Comportamento**:
- `Bloccata = true`: 
  - ❌ Non spostabile con drag&drop (callback bloccato in JS)
  - ❌ Non rischedulata da ricalcoli automatici
  - ✅ Posizione fissa garantita
  - 🎨 Bordo rosso 4px + icona 🔒

- `Priorita < 100`: Schedulata prima delle altre (a parità di vincoli)

**JavaScript Lock Check**:
```javascript
timeline.on('moving', function(item, callback) {
    if (item.bloccata) {
        alert('Impossibile spostare commessa bloccata');
        callback(null);  // Annulla drag
    } else {
        callback(item);
    }
});
```

---

### 📅 Vincoli Temporali Utente

**Nuovi Campi Commessa**:
```csharp
public DateTime? VincoloDataInizio { get; set; }  // Non può iniziare prima
public DateTime? VincoloDataFine { get; set; }    // Deve finire entro (warning)
```

**Algoritmo Ricalcolo**:
```csharp
DateTime dataInizio = ultimaDataFine;

// Rispetta vincolo inizio
if (commessa.VincoloDataInizio.HasValue && dataInizio < commessa.VincoloDataInizio.Value)
{
    dataInizio = commessa.VincoloDataInizio.Value;
}

var dataFine = dataInizio.AddMinutes(durata);

// Calcola warning vincolo fine
commessa.VincoloDataFineSuperato = commessa.VincoloDataFine.HasValue 
    && dataFine > commessa.VincoloDataFine.Value;
```

**Visualizzazione**:
- `VincoloDataFineSuperato = true` → Icona ⚠️ + bordo arancione
- Tooltip mostra vincoli e avvisa se superati

---

### ⚙️ Setup Dinamico e Riduzione Intelligente

**Nuovi Campi**:
```csharp
// Commessa
public int? SetupStimatoMinuti { get; set; }    // Override per questa commessa
public string? ClasseLavorazione { get; set; }   // "Fusione", "Finitura", etc.

// Articolo
public string? ClasseLavorazione { get; set; }   // Default dalla anagrafica
```

**Formula Calcolo**:
```csharp
private int CalcolaDurataConSetupDinamico(Commessa commessa, Commessa? precedente, ImpostazioniProduzione impostazioni)
{
    int setupMinuti;
    
    // 1. Override specifico
    if (commessa.SetupStimatoMinuti.HasValue)
    {
        setupMinuti = commessa.SetupStimatoMinuti.Value;
    }
    // 2. Riduzione 50% se stessa classe consecutiva
    else if (precedente != null 
             && !string.IsNullOrEmpty(commessa.ClasseLavorazione)
             && commessa.ClasseLavorazione == precedente.ClasseLavorazione)
    {
        setupMinuti = impostazioni.TempoSetupMinuti / 2;  // 90 → 45 min
    }
    // 3. Setup standard
    else
    {
        setupMinuti = impostazioni.TempoSetupMinuti;  // Default 90 min
    }
    
    int tempoLavorazioneMinuti = CalcolaTempoLavorazione(commessa);
    return setupMinuti + tempoLavorazioneMinuti;
}
```

**Esempio**:
- Commessa A (classe "Fusione") → Setup 90 min
- Commessa B (classe "Fusione") → Setup 45 min (riduzione 50%)
- Commessa C (classe "Finitura") → Setup 90 min (classe diversa)

---

### 🧩 Algoritmo Scheduling a Segmenti

**Problema Risolto**: Ricalcoli globali distruggevano posizionamenti manuali utente

**Soluzione**: `RicalcolaMacchinaConBlocchiAsync()`

**Logica**:
```csharp
// 1. Separa bloccate e non bloccate
var commesseBloccate = tutte.Where(c => c.Bloccata).OrderBy(c => c.OrdineSequenza).ToList();
var commesseNonBloccate = tutte.Where(c => !c.Bloccata).OrderBy(c => c.Priorita).ToList();

// 2. Commesse bloccate: posizioni IMMUTABILI
foreach (var bloccata in commesseBloccate)
{
    // Date mantenute esatte, nessun ricalcolo
}

// 3. Commesse non bloccate: rischedulazione INTORNO ai blocchi
DateTime cursore = dataInizioMacchina;

foreach (var nonBloccata in commesseNonBloccate)
{
    // Trova prossimo slot libero dopo cursore, evitando sovrapposizioni con bloccate
    DateTime slotInizio = TrovaSlotLibero(cursore, commesseBloccate, nonBloccata.Durata);
    
    nonBloccata.DataInizioPrevisione = slotInizio;
    nonBloccata.DataFinePrevisione = slotInizio.AddMinutes(nonBloccata.Durata);
    
    cursore = nonBloccata.DataFinePrevisione;
}

// 4. Ricalcola OrdineSequenza finale per tutte
var tutteSorted = tutte.OrderBy(c => c.DataInizioPrevisione).ToList();
for (int i = 0; i < tutteSorted.Count; i++)
{
    tutteSorted[i].OrdineSequenza = i + 1;
}
```

**Beneficio**: Utente può "fissare" commesse critiche, il sistema riorganizza il resto

---

### 🎯 Suggerimento Macchina Intelligente

**Nuovo Endpoint**: `POST /api/pianificazione/suggerisci-macchina`

**Request**:
```json
{
  "commessaId": "guid-commessa",
  "numeriMacchineCandidate": [1, 2, 3]  // Opzionale, altrimenti tutte
}
```

**Response**:
```json
{
  "success": true,
  "macchinaSuggerita": "M02",
  "dataInizioPrevista": "2026-02-10T08:00:00",
  "dataFinePrevista": "2026-02-10T16:30:00",
  "valutazioni": [
    {
      "numeroMacchina": 1,
      "codiceMacchina": "M01",
      "dataFineUltimaCommessa": "2026-02-12T14:00:00",
      "dataInizioPrevista": "2026-02-12T14:00:00",
      "dataFinePrevista": "2026-02-12T22:30:00",
      "numeroCommesseInCoda": 5,
      "caricoTotaleMinuti": 2400
    },
    {
      "numeroMacchina": 2,
      "codiceMacchina": "M02",
      "dataFineUltimaCommessa": "2026-02-10T08:00:00",
      "dataInizioPrevista": "2026-02-10T08:00:00",
      "dataFinePrevista": "2026-02-10T16:30:00",
      "numeroCommesseInCoda": 2,
      "caricoTotaleMinuti": 960
    }
  ]
}
```

**Algoritmo**:
```csharp
// Per ogni macchina candidata:
// 1. Trova data fine ultima commessa
// 2. Simula inserimento commessa
// 3. Calcola data fine prevista
// 4. Ordina per earliest completion time
// 5. Ritorna macchina con data fine minore
```

---

### 🔄 SignalR UpdateVersion Anti-Loop

**Problema Risolto**: Loop infiniti di notifiche, update stali da altri client

**Implementazione**:

**Server**:
```csharp
// Genera version timestamp
long updateVersion = DateTime.UtcNow.Ticks;

await _notificationService.NotifyCommesseUpdatedAsync(
    macchineCoinvolte, 
    updateVersion
);
```

**Notifica SignalR**:
```json
{
  "updateVersion": 638123456789012345,
  "macchineCoinvolte": ["M01", "M03"]
}
```

**Client JavaScript**:
```javascript
const GanttMacchine = {
    lastUpdateVersion: 0,
    
    initSignalR() {
        connection.on("PianificazioneUpdated", (notification) => {
            // Skip update stali
            if (notification.updateVersion <= this.lastUpdateVersion) {
                console.log('Skipping stale update');
                return;
            }
            
            this.lastUpdateVersion = notification.updateVersion;
            
            // Update solo macchine coinvolte (targeting)
            this.updateItemsFromServer(notification.commesse, notification.updateVersion);
        });
    }
};
```

**Benefici**:
- ❌ Nessun loop: client ignora update con version vecchia
- ❌ Nessun stale update: solo l'ultimo viene applicato
- ✅ Targeting: update solo macchine modificate (non global refresh)

---

### 🚫 Rimozione Filtri Distruttivi JavaScript

**PRIMA (v1.x - ❌ FRAGILE)**:
```javascript
// Questo nascondeva commesse senza date!
items = tasks.filter(task => 
    task.dataInizioPrevisione && 
    task.dataFinePrevisione && 
    task.numeroMacchina
).map(...);
```

**ORA (v2.0 - ✅ ROBUSTO)**:
```javascript
// Backend garantisce SEMPRE date per commesse assegnate
items = tasks.filter(task => 
    task.numeroMacchina  // Solo: deve avere macchina
).map(...);
```

**Garanzia Backend**:
```csharp
// CalcolaDataPianificazioneAsync garantisce date se macchina assegnata
if (commessa.NumeroMacchina.HasValue)
{
    if (!commessa.DataInizioPrevisione.HasValue)
        commessa.DataInizioPrevisione = DateTime.Now;
    if (!commessa.DataFinePrevisione.HasValue)
        commessa.DataFinePrevisione = commessa.DataInizioPrevisione.Value.AddMinutes(480);
}
```

---

### 🎨 Indicatori Visivi Estesi

**Icone nel Gantt**:
```javascript
let icons = '';
if (task.bloccata) icons += ' 🔒';
if (task.vincoloDataFineSuperato) icons += ' ⚠️';
if (task.datiIncompleti) icons += ' ⚠️';

let priorityIndicator = task.priorita < 100 ? ` [P${task.priorita}]` : '';

content: `${task.codice} (${progress}%)${priorityIndicator}${icons}`
```

**CSS Commesse Bloccate**:
```css
.vis-item.commessa-bloccata {
    border: 2px solid #d32f2f !important;
    border-left-width: 4px !important;
    opacity: 0.95;
    cursor: not-allowed !important;
}

.vis-item.commessa-bloccata:hover {
    box-shadow: 0 0 8px rgba(211, 47, 47, 0.6) !important;
}
```

**Tooltip Arricchito**:
```javascript
title: `${task.description}
Quantità: ${task.quantita} ${task.uom}
Stato: ${task.stato}
Priorità: ${task.priorita}
${task.vincoloDataInizio ? 'Vincolo Inizio: ' + formatDate(task.vincoloDataInizio) : ''}
${task.vincoloDataFine ? 'Vincolo Fine: ' + formatDate(task.vincoloDataFine) : ''}
${task.vincoloDataFineSuperato ? '⚠️ VINCOLO FINE SUPERATO' : ''}
${task.bloccata ? '🔒 COMMESSA BLOCCATA' : ''}
${task.classeLavorazione ? 'Classe: ' + task.classeLavorazione : ''}`
```

---

## 🗓️ Calendario Lavoro - Implementazione (v1.29)

### Problema Risolto (6 Febbraio 2026)

**Issue**: CalendarioLavoro ignorato nei calcoli date Gantt

**Sintomi**:
- ❌ Impostazioni giorni lavorativi (Lunedì-Domenica) **ignorate**
- ❌ Orari lavoro (08:00-17:00) **non rispettati**
- ❌ Date Gantt iniziano a mezzanotte invece che OraInizio
- ❌ Esportazione programma **vuota** (commesse escluse per date NULL/sbagliate)

### Root Cause

**Prima (v1.28 e precedenti)**:

```csharp
// PianificazioneService.cs - METODO VECCHIO
public DateTime CalcolaDataFinePrevistaConFestivi(
    DateTime dataInizio, 
    int durataMinuti, 
    int oreLavorativeGiornaliere,      // ❌ Int generico (es. 8)
    int giorniLavorativiSettimanali,   // ❌ Int generico (es. 5)
    HashSet<DateOnly> festivi)
{
    // ❌ HARDCODED: Assume solo Sabato/Domenica come weekend
    if (giorniLavorativiSettimanali == 5)  
    {
        while (dataFine.DayOfWeek == DayOfWeek.Saturday || 
               dataFine.DayOfWeek == DayOfWeek.Sunday)
        {
            dataFine = dataFine.AddDays(1).Date;
        }
    }
    
    // ❌ NON usa OraInizio/OraFine per normalizzare le date
    // ❌ NON controlla i singoli giorni (Lunedì, Martedì, etc.)
}
```

**Problema**: Anche se utente configurava "Solo Lunedì-Giovedì", il sistema **calcolava Venerdì** come lavorativo.

### Soluzione Implementata

**Nuova Architettura (v1.29)**:

```csharp
// 1. NUOVA FIRMA: Accetta CalendarioLavoroDto completo
public DateTime CalcolaDataFinePrevistaConFestivi(
    DateTime dataInizio, 
    int durataMinuti, 
    CalendarioLavoroDto calendario,  // ✅ Oggetto completo
    HashSet<DateOnly> festivi)
{
    if (durataMinuti <= 0) return dataInizio;
    
    // 1. Normalizza data inizio all'orario lavorativo
    var dataFine = NormalizzaInizioGiorno(dataInizio, calendario.OraInizio);
    var minutiRimanenti = durataMinuti;
    var minutiPerGiorno = (int)(calendario.OraFine - calendario.OraInizio).TotalMinutes;
    
    while (minutiRimanenti > 0)
    {
        // 2. Salta giorni NON lavorativi (controlla calendario specifico)
        while (!IsGiornoLavorativo(dataFine, calendario) || 
               festivi.Contains(DateOnly.FromDateTime(dataFine)))
        {
            dataFine = dataFine.AddDays(1).Date + calendario.OraInizio.ToTimeSpan();
        }
        
        // 3. Calcola minuti disponibili oggi (rispetta OraInizio/OraFine)
        var oraCorrente = TimeOnly.FromDateTime(dataFine);
        int minutiDisponibiliOggi;
        
        if (oraCorrente < calendario.OraInizio)
        {
            minutiDisponibiliOggi = minutiPerGiorno;
            dataFine = dataFine.Date + calendario.OraInizio.ToTimeSpan();
        }
        else if (oraCorrente >= calendario.OraFine)
        {
            // Siamo dopo fine lavoro: passa al giorno successivo
            dataFine = dataFine.Date.AddDays(1) + calendario.OraInizio.ToTimeSpan();
            continue;
        }
        else
        {
            minutiDisponibiliOggi = (int)(calendario.OraFine - oraCorrente).TotalMinutes;
        }
        
        if (minutiRimanenti <= minutiDisponibiliOggi)
        {
            dataFine = dataFine.AddMinutes(minutiRimanenti);
            minutiRimanenti = 0;
        }
        else
        {
            minutiRimanenti -= minutiDisponibiliOggi;
            dataFine = dataFine.Date.AddDays(1) + calendario.OraInizio.ToTimeSpan();
        }
    }
    
    return dataFine;
}

// 2. HELPER: Verifica giorno specifico
private bool IsGiornoLavorativo(DateTime data, CalendarioLavoroDto calendario)
{
    return data.DayOfWeek switch
    {
        DayOfWeek.Monday    => calendario.Lunedi,
        DayOfWeek.Tuesday   => calendario.Martedi,
        DayOfWeek.Wednesday => calendario.Mercoledi,
        DayOfWeek.Thursday  => calendario.Giovedi,
        DayOfWeek.Friday    => calendario.Venerdi,
        DayOfWeek.Saturday  => calendario.Sabato,
        DayOfWeek.Sunday    => calendario.Domenica,
        _ => false
    };
}

// 3. HELPER: Normalizza a OraInizio
private DateTime NormalizzaInizioGiorno(DateTime data, TimeOnly oraInizio)
{
    var ora = TimeOnly.FromDateTime(data);
    if (ora < oraInizio)
        return data.Date + oraInizio.ToTimeSpan();
    return data;
}
```

### Esempio Concreto

**Input**:
- Data Inizio: `Lunedì 2026-02-09 14:00`
- Durata: `600 minuti` (10 ore)
- Calendario:
  - Giorni: **Solo Lunedì, Martedì, Mercoledì, Giovedì** (NO Venerdì)
  - Orari: **08:00 - 17:00** (9 ore/giorno = 540 minuti)

**Calcolo Nuovo (v1.29)**:
1. Normalizza 14:00 → OK (già in 08:00-17:00)
2. Minuti disponibili oggi: 17:00 - 14:00 = **180 min**
3. Usa 180 min → rimangono **420 min**
4. Fine giornata → Passa a **Martedì 08:00**
5. Martedì è lavorativo? ✅ SÌ (`calendario.Martedi = true`)
6. Usa 540 min → rimangono **0 min** (420 < 540)
7. **Data Fine**: `Martedì 2026-02-10 15:00`

**Calcolo Vecchio (v1.28)**:
1. Aggiungi 10 ore → `Martedì 00:00` (❌ sbagliato: mezzanotte!)
2. Sabato/Domenica skippati, ma **Venerdì considerato lavorativo**
3. Risultato incoerente con impostazioni utente

### Files Modificati

| File | Righe | Modifiche |
|------|-------|-----------|
| [IPianificazioneService.cs](../MESManager.Application/Interfaces/IPianificazioneService.cs) | +5 | Nuova firma + overload legacy `[Obsolete]` |
| [PianificazioneService.cs](../MESManager.Application/Services/PianificazioneService.cs) | +90 | Refactoring calcolo + 2 helper |
| [PianificazioneEngineService.cs](../MESManager.Infrastructure/Services/PianificazioneEngineService.cs) | +40 | Helper `GetCalendarioLavoroDtoAsync()` + 5 chiamate aggiornate |
| [PianificazioneController.cs](../MESManager.Web/Controllers/PianificazioneController.cs) | +40 | Helper `GetCalendarioLavoroDtoAsync()` + 1 chiamata aggiornata |

### Impatto

**Prima (v1.28)**:
- ✅ Impostazioni salvate in DB (`CalendarioLavoro` table)
- ✅ UI mostra correttamente giorni/orari configurati
- ❌ **Calcoli ignorano completamente le impostazioni**
- ❌ Date sbagliate → Esportazione fallisce

**Dopo (v1.29)**:
- ✅ Giorni lavorativi **rispettati** (es. Lun-Gio funziona)
- ✅ Orari **normalizzati** (inizio 08:00, non mezzanotte)
- ✅ Date Gantt **realistiche**
- ✅ Esportazione **funzionante** (commesse con date corrette incluse)

### Backward Compatibility

**Overload Legacy Mantenuto**:
```csharp
[Obsolete("Usare overload con CalendarioLavoroDto per rispettare le impostazioni calendario utente")]
public DateTime CalcolaDataFinePrevistaConFestivi(
    DateTime dataInizio, int durataMinuti, 
    int oreLavorativeGiornaliere, int giorniLavorativiSettimanali, 
    HashSet<DateOnly> festivi)
{
    // Delega al metodo nuovo creando calendario fittizio
    var calendarioDefault = new CalendarioLavoroDto
    {
        Lunedi = true, Martedi = true, Mercoledi = true, 
        Giovedi = true, Venerdi = true,
        Sabato = giorniLavorativiSettimanali > 5,
        Domenica = giorniLavorativiSettimanali > 6,
        OraInizio = new TimeOnly(8, 0),
        OraFine = new TimeOnly(8 + oreLavorativeGiornaliere, 0)
    };
    return CalcolaDataFinePrevistaConFestivi(dataInizio, durataMinuti, calendarioDefault, festivi);
}
```

**Risultato**: Nessun breaking change per eventuali chiamanti esterni.

### Testing Raccomandato

1. **Test Configurazione Personalizzata**:
   - Impostazioni → Gantt Macchine → Calendario
   - Seleziona **solo Lunedì-Mercoledì**
   - Orario: **09:00 - 18:00**
   - Crea commessa 8 ore il Mercoledì 15:00
   - ✅ Verifica: deve finire Lunedì 16:00 (salta Gio-Dom)

2. **Test Esportazione**:
   - Gantt con 5 commesse pianificate
   - Click "Esporta su Programma"
   - ✅ Verifica: tutte 5 esportate (nessuna esclusa)

3. **Test Drag & Drop**:
   - Sposta commessa tra macchine
   - ✅ Verifica: date ricalcolate rispettano calendario

---

## 🆘 Supporto

Per rifattorizzazione completa v2.0: [GANTT-REFACTORING-v2.0.md](GANTT-REFACTORING-v2.0.md)  
Per architettura generale: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)  
Per configurazione: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)  
Per deploy: [01-DEPLOY.md](01-DEPLOY.md)


---

##  Refactoring History v1.27-v1.28

### FASE 1: Refactoring Architetturale (v1.27 - 5 Feb 2026)

**Problema**: Codice duplicato (150+ righe), N+1 queries, magic numbers

**Soluzioni**:
1. Centralizzato MapToGanttDto in PianificazioneService.cs
2. Batch loading Anime con Dictionary<string, Anime>
3. Costanti in GanttConstants.js (63 righe)
4. CSS espanso da 124 a 310 righe (animazioni, accessibility)

**Metriche**:
- Righe duplicate: 150+  0
- Query Anime: N  1
- Magic numbers: 15+  0
- Build errors: 6  0

### FASE 2: UX Revolution (v1.28 - 5 Feb 2026)

**Problema**: 7 issue visive dopo test v1.27

**Soluzioni**:
1. **Gradazione scura**: darkenColor(hex, 30%) per parte completata
2. **Triangolino PRIMA**:  45% CODICE_CASSA [P10]
3. **Commesse rosse**: Fix classe .commessa-bloccata
4. **Export**: POST /esporta-su-programma
5. **Sovrapposizione**: stackSubgroups: false

**Funzione darkenColor() Aggiunta**:
javascript
darkenColor: function(hex, percent) {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (!result) return hex;
    const r = Math.max(0, parseInt(result[1], 16) * (100 - percent) / 100);
    const g = Math.max(0, parseInt(result[2], 16) * (100 - percent) / 100);
    const b = Math.max(0, parseInt(result[3], 16) * (100 - percent) / 100);
    return '#' + Math.round(r).toString(16).padStart(2,'0') + 
                 Math.round(g).toString(16).padStart(2,'0') + 
                 Math.round(b).toString(16).padStart(2,'0');
}


---

##  Lezioni Apprese (v1.27-v1.28)

### 1. Clean Architecture è Inviolabile
**Problema**: Application layer non può referenziare Infrastructure  
**Soluzione**: Outer layers caricano dati  passano Dictionary  
**Regola**: Mai violare dipendenze per comodità

### 2. Batch Loading è Critico
**Problema**: N+1 queries (N chiamate DB per N commesse)  
**Soluzione**: 1 query batch + Dictionary lookup  
**Regola**: Sempre batch loading su loop con query

### 3. UX Feedback Visivo Essenziale
**Problema**: % piatta, commesse senza colore  
**Soluzione**: Gradazione scura, colori, animazioni  
**Regola**: Ogni stato visivamente distinto

### 4. CSS Specificità Conta
**Problema**: .vis-item.commessa-bloccata non applicata  
**Soluzione**: Duplicare regole con/senza prefisso  
**Regola**: Testare specificità con ispettore

### 5. Documentazione è Risparmio Futuro
**Problema**: Dimenticare perché decisione presa  
**Soluzione**: Docs2/ con cronistoria + lezioni  
**Regola**: Ogni modifica documentata subito

---

**Fine Documento**  
Ultima Revisione: 5 Febbraio 2026 - v1.28  
Prossimo Aggiornamento: v1.29+ nuove feature Gantt
