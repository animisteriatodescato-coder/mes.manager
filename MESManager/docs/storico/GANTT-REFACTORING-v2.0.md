# RIFATTORIZZAZIONE GANTT MACCHINE - SCHEDULING ROBUSTO

**Data**: 2026-02-04  
**Versione**: 2.0  
**Autore**: AI Development Team  
**Status**: ✅ COMPLETATO (core backend + frontend)  

---

## 📋 PANORAMICA

Rifattorizzazione completa del modulo Gantt Macchine per trasformarlo da sistema fragile accodamento rigido a **sistema industriale robusto** con:

- ✅ Optimistic Concurrency Control
- ✅ Segmenti bloccati e priorità
- ✅ Vincoli temporali utente
- ✅ Setup dinamico e riduzione intelligente
- ✅ Transazioni atomiche
- ✅ Sincronizzazione SignalR ottimizzata
- ✅ UI lock/unlock e indicatori visivi

---

## 🎯 PROBLEMI RISOLTI

### Problema 1: Concorrenza fragile
**Prima**: Due utenti potevano sovrascrivere modifiche reciproche senza warning  
**Ora**: RowVersion su Commesse + DbUpdateConcurrencyException gestita → "Dati modificati, ricarica"

### Problema 2: Pianificazione rigida e distruttiva
**Prima**: Ogni ricalcolo spostava TUTTE le commesse, anche se utente le aveva posizionate manualmente  
**Ora**: Commesse "bloccate" (flag `Bloccata`) definiscono segmenti fissi, il ricalcolo agisce solo sulle non bloccate

### Problema 3: Setup fisso e non realistico
**Prima**: Sempre 90min setup per tutti  
**Ora**: 
- `SetupStimatoMinuti` per override per commessa
- Riduzione automatica 50% se `ClasseLavorazione` consecutiva uguale
- Default da `ImpostazioniProduzione.TempoSetupMinuti`

### Problema 4: Assenza vincoli utente
**Prima**: L'utente non poteva dire "questa commessa NON può iniziare prima di X" o "DEVE finire entro Y"  
**Ora**: 
- `VincoloDataInizio`: la commessa non inizia prima
- `VincoloDataFine`: warning se superato (flag `VincoloDataFineSuperato` visibile in UI)

### Problema 5: Filtri JS che nascondevano commesse
**Prima**: `.filter()` lato client scartava commesse senza date → invisibili nel Gantt  
**Ora**: Filtro rimosso, backend garantisce sempre date, tutte le commesse assegnate sono visibili

### Problema 6: SignalR loop e update stali
**Prima**: `isProcessingUpdate` fragile, loop possibili  
**Ora**: `UpdateVersion` (timestamp ticks) su ogni notifica, client scarta update vecchi

### Problema 7: Mancanza suggerimenti intelligenti
**Prima**: Utente doveva decidere "a occhio" quale macchina assegnare  
**Ora**: Endpoint `/api/pianificazione/suggerisci-macchina` calcola earliest completion time per tutte macchine candidate

---

## 🗄️ MODIFICHE DATABASE

### Nuove colonne su `Commesse`

```sql
RowVersion          ROWVERSION NOT NULL  -- Optimistic concurrency
Priorita            INT DEFAULT 100      -- Più basso = più urgente
Bloccata            BIT DEFAULT 0        -- Lock pianificazione
VincoloDataInizio   DATETIME2 NULL       -- Non può iniziare prima
VincoloDataFine     DATETIME2 NULL       -- Deve finire entro (warning)
SetupStimatoMinuti  INT NULL             -- Override setup
ClasseLavorazione   NVARCHAR(50) NULL    -- Classe per riduzione setup
```

### Nuove colonne su `Articoli`

```sql
ClasseLavorazione   NVARCHAR(50) NULL    -- Default classe per articolo
```

### Nuovi indici

```sql
IX_Commesse_NumeroMacchina_OrdineSequenza  -- Query macchina + ordine
IX_Commesse_NumeroMacchina_Bloccata_Priorita  -- Filtraggio blocchi/priorità
IX_Commesse_VincoloDataInizio_VincoloDataFine  -- Query vincoli
```

### Migration

- **Dev**: `dotnet ef migrations add AddRobustPlanningFeatures`
- **Prod**: Script manuale in `scripts/migration-robust-planning-PROD.sql` (da eseguire in finestra manutenzione)

---

## 🏗️ ARCHITETTURA MODIFICATA

### Layer Backend

#### `PianificazioneEngineService` (RIFATTORIZZATO COMPLETO)

**Metodi chiave**:

1. **`SpostaCommessaAsync`**: Sposta commessa con transaction + concurrency check + lock check
   - Verifica `Bloccata` → rifiuta se true
   - Transaction atomica
   - Gestisce `DbUpdateConcurrencyException`
   - Ritorna `UpdateVersion` per SignalR

2. **`RicalcolaMacchinaConBlocchiAsync`** (NUOVO ALGORITMO):
   - Separa commesse bloccate e non bloccate
   - Commesse bloccate: mantenute date esatte (non toccate)
   - Commesse non bloccate: ricalcolate "intorno" ai blocchi
   - Rispetta `VincoloDataInizio` e `VincoloDataFine`
   - Ordina per `Priorita` → più basse prima
   - Calcola setup dinamico (riduzione se stessa classe)

3. **`SuggerisciMacchinaMiglioreAsync`** (NUOVO):
   - Input: `CommessaId` + opzionale `NumeriMacchineCandidate`
   - Output: macchina con earliest completion time
   - Valutazioni per tutte candidate con:
     - Data fine ultima commessa
     - Data inizio/fine prevista se assegnata
     - Numero commesse in coda
     - Carico totale minuti

4. **`CalcolaDurataConSetupDinamico`** (NUOVO):
   ```csharp
   if (commessa.SetupStimatoMinuti.HasValue)
       setupMinuti = commessa.SetupStimatoMinuti.Value;
   else if (stessaClasseLavorazionePrecedente)
       setupMinuti = impostazioni.TempoSetupMinuti / 2; // Riduzione 50%
   else
       setupMinuti = impostazioni.TempoSetupMinuti;
   ```

#### `PianificazioneController`

**Nuovi endpoint**:

- `POST /api/pianificazione/suggerisci-macchina` → `SuggerisciMacchinaResponse`
- Gestione `DbUpdateConcurrencyException` → HTTP 409 Conflict

**Modifiche esistenti**:
- `POST /api/pianificazione/sposta`: ora ritorna `UpdateVersion` e `MacchineCoinvolte`
- Injection di `PianificazioneNotificationService` per SignalR

---

### Layer SignalR

#### `PianificazioneNotificationService`

**Modifiche**:
- Parametro `updateVersion` in tutte le notifiche
- Campo `MacchineCoinvolte: List<string>` nel payload
- Logging con version number `[v{UpdateVersion}]`

#### `PianificazioneUpdateNotification` DTO

```csharp
public long UpdateVersion { get; set; }  // NEW
public List<string> MacchineCoinvolte { get; set; } = new();  // NEW
```

---

### Layer Frontend

#### JavaScript `gantt-macchine.js`

**Modifiche principali**:

1. **Rimozione filtri distruttivi**:
   ```javascript
   // PRIMA (❌):
   .filter(task => task.dataInizioPrevisione && task.dataFinePrevisione && task.numeroMacchina)
   
   // ORA (✅):
   .filter(task => task.numeroMacchina) // Solo: deve avere macchina
   ```

2. **Gestione lock**:
   ```javascript
   // In onMove callback:
   if (item.bloccata) {
       alert('Impossibile spostare commessa bloccata');
       callback(null); // Blocca drag
       return;
   }
   ```

3. **Update version**:
   ```javascript
   lastUpdateVersion: 0,
   
   updateItemsFromServer(commesse, updateVersion) {
       if (updateVersion && updateVersion <= this.lastUpdateVersion) {
           console.log('Skipping stale update');
           return;
       }
       this.lastUpdateVersion = updateVersion;
       // ... update items
   }
   ```

4. **Icone e tooltip arricchiti**:
   ```javascript
   let icons = '';
   if (task.bloccata) icons += ' 🔒';
   if (task.vincoloDataFineSuperato) icons += ' ⚠️';
   if (task.datiIncompleti) icons += ' ⚠️';
   
   let priorityIndicator = task.priorita < 100 ? ` [P${task.priorita}]` : '';
   
   content: `${task.codice} (${progress}%)${priorityIndicator}${icons}`
   ```

5. **Tooltip esteso**:
   - Priorità
   - Vincoli data inizio/fine
   - Lock status
   - Classe lavorazione
   - Warning dati incompleti

#### CSS `gantt-macchine.css`

```css
.vis-item.commessa-bloccata {
    border: 2px solid #d32f2f !important;
    border-left-width: 4px !important;
    opacity: 0.95;
    cursor: not-allowed !important;
}
```

---

## 📊 DTOs AGGIORNATI

### `CommessaGanttDto`

**Nuovi campi**:
```csharp
public int Priorita { get; set; } = 100;
public bool Bloccata { get; set; }
public DateTime? VincoloDataInizio { get; set; }
public DateTime? VincoloDataFine { get; set; }
public bool VincoloDataFineSuperato { get; set; }  // Calcolato server
public string? ClasseLavorazione { get; set; }
```

### `SpostaCommessaResponse`

**Nuovi campi**:
```csharp
public long UpdateVersion { get; set; }
public List<string> MacchineCoinvolte { get; set; }
```

### `SuggerisciMacchinaRequest/Response` (NUOVI)

```csharp
public class SuggerisciMacchinaRequest {
    public Guid CommessaId { get; set; }
    public List<string>? NumeriMacchineCandidate { get; set; }
}

public class SuggerisciMacchinaResponse {
    public bool Success { get; set; }
    public string? MacchinaSuggerita { get; set; }
    public DateTime? DataInizioPrevista { get; set; }
    public DateTime? DataFinePrevista { get; set; }
    public List<ValutazioneMacchina> Valutazioni { get; set; }
}
```

---

## 🔧 UTILIZZO UTENTE

### Bloccare una commessa

1. Aprire pagina Commesse
2. Selezionare commessa
3. Attivare checkbox "Bloccata"
4. Salvare
5. Nel Gantt: appare icona 🔒 e non è trascinabile

### Impostare priorità

1. Aprire pagina Commesse
2. Impostare campo `Priorita` (default 100, minore = più urgente)
3. Salvare
4. Prossimo ricalcolo: le commesse con priorità minore vengono schedulate prima (a parità di vincoli)

### Impostare vincoli temporali

1. Aprire pagina Commesse
2. **Vincolo Inizio**: campo `VincoloDataInizio` → la commessa non inizierà prima
3. **Vincolo Fine**: campo `VincoloDataFine` → warning se la fine prevista supera questo limite
4. Salvare
5. Nel Gantt: tooltip mostra vincoli, icona ⚠️ se vincolo fine superato

### Suggerire macchina migliore

1. (Da implementare in UI Blazor) Selezionare commessa non assegnata
2. Click "Suggerisci macchina"
3. API valuta tutte macchine e propone quella con earliest completion
4. Utente può accettare o scegliere diversa

---

## ✅ TESTING

### Test da implementare (TODO)

1. **Test concurrency**:
   - Due utenti spostano stessa commessa contemporaneamente
   - Verifica: uno riceve conflict 409, deve ricaricare

2. **Test blocchi**:
   - Commessa bloccata non viene spostata da ricalcolo
   - Drag&drop bloccato in UI

3. **Test vincoli**:
   - `VincoloDataInizio` rispettato
   - `VincoloDataFine` superato → flag warning true

4. **Test setup dinamico**:
   - Setup override funziona
   - Riduzione 50% per stessa classe consecutiva

5. **Test suggerimento macchina**:
   - Earliest completion corretto con 3 macchine diverse caricate

### File test (da creare)

```
MESManager.Tests/
├─ Services/
│  └─ PianificazioneEngineServiceTests.cs
├─ Controllers/
│  └─ PianificazioneControllerTests.cs
└─ Integration/
   └─ GanttWorkflowTests.cs
```

---

## 🚀 DEPLOY

### Sviluppo

1. Applicare migration:
   ```powershell
   dotnet ef database update --project MESManager.Infrastructure
   ```

2. Build:
   ```powershell
   dotnet build MESManager.sln
   ```

3. Run:
   ```powershell
   dotnet run --project MESManager.Web
   ```

### Produzione

1. **Backup database** (OBBLIGATORIO)
2. Eseguire script SQL manuale: `scripts/migration-robust-planning-PROD.sql`
3. Verificare migration OK
4. Pubblicare applicazione secondo workflow standard (01-DEPLOY.md)
5. Riavviare servizi in ordine: Stop(PlcSync→Worker→Web) Start(Web→Worker→PlcSync)
6. Verificare versione incrementata in footer

---

## 📝 CHECKLIST POST-DEPLOY

- [ ] Migration database applicata correttamente
- [ ] Indici creati (verificare con `sp_helpindex 'Commesse'`)
- [ ] Nessun duplicato OrdineSequenza per macchina
- [ ] Gantt carica tutte commesse (anche quelle senza date precedenti)
- [ ] Drag&drop funziona per commesse non bloccate
- [ ] Drag&drop bloccato per commesse con `Bloccata=1`
- [ ] Icone 🔒 e ⚠️ visibili
- [ ] Tooltip mostra priorità/vincoli/lock
- [ ] SignalR aggiorna in real-time senza loop
- [ ] Concurrency: provare modifica simultanea da 2 browser

---

## 🔮 EVOLUZIONI FUTURE (Opzionali)

### UI Blazor per gestione avanzata

- [ ] Pagina "Gestione Commesse Gantt" con:
  - Tabella con campi: Priorita, Bloccata, Vincoli, ClasseLavorazione, SetupStimato
  - Filtri: solo bloccate, solo con vincoli, solo alta priorità
  - Azioni bulk: blocca/sblocca selezione, imposta priorità
  
- [ ] Pulsante "Suggerisci macchina" in pagina Commesse
  - Mostra valutazioni tutte macchine candidate
  - Assegna automaticamente alla migliore (con conferma)

### Ottimizzazioni algoritmo

- [ ] Matrice setup complessa (A→B ha setup diverso da B→C)
- [ ] Algoritmo genetico per ottimizzazione globale (non solo greedy locale)
- [ ] Simulazione "what-if": "cosa succederebbe se blocco questa commessa?"

### Reportistica

- [ ] Report vincoli superati
- [ ] Report macchine sovraccaricate
- [ ] Esportazione Gantt PDF/Excel con filtri

---

## 📚 RIFERIMENTI TECNICI

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Optimistic Concurrency (EF Core)](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [SignalR Best Practices](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Vis-Timeline Documentation](https://visjs.github.io/vis-timeline/docs/timeline/)

---

## 👥 CONTRIBUTORS

- AI Development Team (GitHub Copilot + Claude)
- Business Logic Design: Domain Expert
- Database Schema: Database Architect

---

## 🔄 AGGIORNAMENTI POST-v2.0 (19 Feb 2026)

### v1.42.0 - v1.45.1: Buffer Sistema, Snap Precision, Stack Fix

**Problema Riportato**:  
> "quando carico una commessa nel Gantt su una macchina sulla quale non ho niente in produzione, mi va in produzione all'istante e non mi consente di spostarla"

**Root Cause Analysis**:
1. **Buffer mancante**: Nessun margine temporale tra caricamento e `InProduzione`
2. **Snap grossolano**: Drag-drop saltava di 8 ore invece di 15 minuti
3. **Stack invertito**: `stack: true` creava righe multiple invece di accodamento singolo
4. **Normalizzazione aggressiva**: Date cancellate anche se dentro orario lavorativo
5. **AutoCompleta su GET**: Endpoint read-only cambiava stato delle commesse
6. **Blocco produzione rigido**: Commesse future bloccate se avevano dati produzione

**Soluzioni Implementate**:

#### 1. Buffer Riorganizzazione (v1.42.0)
```csharp
// ImpostazioniGantt.cs
public int BufferInizioProduzioneMinuti { get; set; } = 15;

// PianificazioneController.cs - AutoCompletaCommesseAsync()
var sogliaProduzione = now.AddMinutes(-bufferMinuti);
var commesseDaAvviare = await _context.Commesse
    .Where(c => c.DataInizioPrevisione.Value <= sogliaProduzione)
    .Where(c => c.StatoProgramma == StatoProgramma.Programmata)
    .ToListAsync();
```

**Effetto**: Commesse caricate su Gantt rimangono `Programmata` per 15 minuti, permettendo riorganizzazione.

---

#### 2. Command-Query Separation (v1.43.0)
```csharp
// PianificazioneController.cs - GET /api/pianificazione
[HttpGet]
public async Task<ActionResult<List<CommessaPianificazioneDto>>> GetCommessePianificate()
{
    // ⚠️ IMPORTANTE: NON chiamare AutoCompletaCommesseAsync qui!
    // GET deve essere read-only, senza side-effects
    
    var commesse = await _pianificazioneService.GetCommessePianificateAsync(filtro);
    return Ok(commesse);
}
```

**Effetto**: Aprire Gantt non cambia più `StatoProgramma` delle commesse.

---

#### 3. Normalizzazione Condizionale (v1.44.0)
```csharp
// PianificazioneEngineService.cs
bool dentroOrarioLavorativo = IsInOrarioLavorativo(dataInizioDesiderata, calendario, festivi);

if (!dentroOrarioLavorativo)
{
    // Normalizza SOLO se fuori orario
    dataInizioDesiderata = NormalizzaSuOrarioLavorativo(...);
}
else
{
    // Preserva data scelta dall'utente
    dataInizioEffettiva = dataInizioDesiderata;
}

private bool IsInOrarioLavorativo(DateTime data, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
{
    var giorno = DateOnly.FromDateTime(data);
    if (festivi.Contains(giorno)) return false;
    
    int giornoSettimana = (int)data.DayOfWeek;
    if (giornoSettimana == 0 || giornoSettimana == 6) return false; // Dom/Sab
    
    var ora = data.TimeOfDay;
    return ora >= calendario.OraInizio && ora < calendario.OraFine;
}
```

**Effetto**: Buffer 15 minuti non viene cancellato da normalizzazione se drag è già in orario valido.

---

#### 4. Snap Precision Fix (v1.44.1)
```javascript
// gantt-macchine.js
snap: function(date, scale, step) {
    const interval = 15 * 60 * 1000; // 15 minuti (era 8 * 3600000 = 8 ore!)
    return Math.round(date / interval) * interval;
}
```

**Effetto**: Drag-drop ora posiziona commesse con precisione 15 minuti, non salti da 8 ore.

---

#### 5. Stack Single-Row (v1.44.0)
```javascript
// gantt-macchine.js - Vis-Timeline options
stack: false,  // ❌ DISABILITATO: accodamento su STESSA riga

// Comportamento:
// - stack: true  → Vis-Timeline crea righe multiple per evitare overlap
// - stack: false → Vis-Timeline consente overlap, logica accodamento lato server
```

**Effetto**: Commesse rimangono su singola riga per macchina, accodamento gestito da backend.

---

#### 6. Client-Server Position Sync (v1.45.0)
```javascript
// gantt-macchine.js - onMove callback
fetch('/api/pianificazione/sposta', {
    method: 'POST',
    body: JSON.stringify({
        commessaId: item.id,
        nuovaDataInizio: dataRichiesta
    })
})
.then(response => response.json())
.then(result => {
    // Sync posizione calcolata dal server (queueing)
    const commessaAggiornata = result.commesseAggiornate.find(c => c.id === item.id);
    if (commessaAggiornata) {
        item.start = new Date(commessaAggiornata.dataInizioPrevisione);
        item.end = new Date(commessaAggiornata.dataFinePrevisione);
        
        if (Math.abs(dataRichiesta - item.start) > 60000) {
            console.log('🔄 Commessa accodata dal server', {
                richiesta: dataRichiesta,
                effettiva: item.start
            });
        }
    }
    timeline.setItems(items);
});
```

**Effetto**: Se server rileva overlap e accoda commessa, client mostra posizione corretta subito.

---

#### 7. Trust User Intent (v1.45.1)
```csharp
// PianificazioneEngineService.cs
if (commessa.Bloccata)
{
    _logger.LogInformation("⚠️ Spostamento commessa bloccata - sblocco automatico con consenso utente");
    commessa.Bloccata = false;
}

// ❌ RIMOSSO controllo IsInOrarioProduzioneAsync()
// Motivazione: verificava solo presenza dati, non se produzione è ATTUALMENTE in corso
// Dialog UI già chiede conferma, backend si fida della scelta utente
```

**Effetto**: Commesse programmate per settimana prossima non sono bloccate, anche se hanno dati produzione.

---

### Pattern Architetturali Introdotti

1. **Grace Period Pattern**: Buffer temporale per compensare latenza UX + decisioni utente
2. **Conditional Normalization**: Applicare trasformazioni solo quando strettamente necessario
3. **Server as Authority**: Client visualizza, server decide (queueing, overlap detection)
4. **Trust User Intent**: Meno validazioni automatiche, più conferme esplicite

### Metriche di Successo

- ✅ **Riorganizzazione libera**: Buffer 15 min permette spostamenti senza blocchi istantanei
- ✅ **Precisione posizionamento**: Snap 15 min consente granularità fine per scheduling manuale
- ✅ **Accodamento automatico**: Sovrapposizioni risolte automaticamente senza righe multiple
- ✅ **Coerenza dati**: Client-server sync elimina discrepanze visuale ↔ database
- ✅ **Read-only safety**: GET endpoints mai modificano stato del sistema

---

**Fine Aggiornamenti Post-v2.0**  
**Documentato da**: AI Session 2026-02-19  
**Versioni**: v1.42.0 → v1.45.1  

---

