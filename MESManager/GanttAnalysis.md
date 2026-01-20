# Analisi Dettagliata della Struttura del Gantt Macchine

## **1. ARCHITETTURA A 3 LIVELLI**

### **Backend (API Layer)**
- **Endpoint**: `GET /api/pianificazione`
- **Controller**: [PianificazioneController.cs](c:\Dev\MESManager\MESManager.Web\Controllers\PianificazioneController.cs#L33-L58)
- **Query EF Core**:
  ```csharp
  _context.Commesse
    .Include(c => c.Articolo)
    .Where(c => c.NumeroMacchina != null)
    .OrderBy(c => c.NumeroMacchina)
    .ThenBy(c => c.OrdineSequenza)
  ```

### **Frontend (Blazor Component)**
- **Componente**: [GanttMacchine.razor](c:\Dev\MESManager\MESManager.Web\Components\Pages\Programma\GanttMacchine.razor)
- **Responsabilità**: Carica dati da API e li passa a JavaScript

### **Visualizzazione (JavaScript + Vis-Timeline)**
- **Libreria**: Vis-Timeline (visualizzazione Gantt chart)
- **File**: [gantt-macchine.js](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js)

---

## **2. FLUSSO DATI COMPLETO**

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

## **3. STRUTTURA DATI**

### **CommessaGanttDto** (35 proprietà)
```csharp
// Identificazione
Id, Codice, Description, Stato, ColoreStato

// Assegnazione Macchina
NumeroMacchina (int?), NomeMacchina (string?), OrdineSequenza (int)

// Date Pianificazione
DataInizioPrevisione, DataFinePrevisione
DataInizioProduzione, DataFineProduzione

// Dati Produttivi
QuantitaRichiesta, UoM, DataConsegna

// Calcolo Tempi
TempoCicloSecondi, NumeroFigure, TempoSetupMinuti, DurataPrevistaMinuti

// Stato Avanzamento
PercentualeCompletamento (0-100)
```

### **Trasformazione per JavaScript**
Da Blazor:
```csharp
tasks = commesseGantt.Select(c => new {
    id = c.Id,
    codice = c.Codice,
    description = c.Description,
    numeroMacchina = c.NumeroMacchina,
    nomeMacchina = c.NomeMacchina,
    quantita = c.QuantitaRichiesta,
    dataInizio = c.DataInizioPrevisione,
    dataFine = c.DataFinePrevisione,
    durataMinuti = c.DurataPrevistaMinuti,
    stato = c.Stato,
    coloreStato = c.ColoreStato,
    percentualeCompletamento = c.PercentualeCompletamento
})
```

---

## **4. LOGICA JAVASCRIPT DETTAGLIATA**

### **Inizializzazione**
Linee [6-117](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L6-L117):

**A. Creazione Groups (Macchine)**
```javascript
const groups = settings.machines.map(m => ({ 
    id: m.codice,           // Es. "M01", "M02"
    content: m.nome,         // Es. "Macchina 01"
    order: m.ordineVisualizazione  // Per ordinamento
}))
```

**B. Mapping NumeroMacchina → Codice**
Linee [52-55](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L52-L55):
```javascript
const machineMap = new Map();
// Codice "M01" → NumeroMacchina 1
// Codice "M02" → NumeroMacchina 2
```

**C. Filtro Critico**
Linea [61](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L61):
```javascript
.filter(task => task.dataInizio && task.dataFine && task.numeroMacchina)
```
⚠️ **IMPORTANTE**: Se una commessa manca di `DataInizioPrevisione` o `DataFinePrevisione`, viene ESCLUSA dal Gantt

**D. Creazione Items (Commesse)**
Linee [62-85](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L62-L85):
```javascript
items = tasks.map(task => {
    const groupId = machineMap.get(task.numeroMacchina);
    const progress = task.percentualeCompletamento || 0;
    const baseColor = this.getStatusColor(task.stato);
    
    // Gradient progressivo
    const progressStyle = `background: linear-gradient(to right, 
        ${baseColor} ${progress}%, 
        rgba(..., 0.3) ${progress}%)`;
    
    return {
        id: task.id,
        group: groupId,           // "M01", "M02"...
        content: `${task.codice} (${Math.round(progress)}%)`,
        start: new Date(task.dataInizio),
        end: new Date(task.dataFine),
        className: 'commessa-item',
        style: progressStyle,
        title: `${task.description}\nQuantità: ${task.quantita}\nStato: ${task.stato}`
    };
})
```

### **Configurazione Vis-Timeline**
Linee [92-105](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L92-L105):
```javascript
const options = {
    editable: {
        add: false,
        updateTime: true,   // ✅ Drag temporale abilitato
        updateGroup: true,  // ✅ Drag tra macchine abilitato
        remove: false
    },
    stack: false,  // ❌ NO sovrapposizione visuale
    orientation: 'top',
    groupOrder: 'order',
    margin: { item: 10, axis: 5 },
    start: [data minima items],
    end: [data massima items]
}
```

---

## **5. GESTIONE DRAG & DROP**

### **Event Handler "changed"**
Linee [110-156](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L110-L156):

```javascript
this.timeline.on('changed', async function (properties) {
    // 1. Estrai item trascinato
    const itemId = properties.items[0];
    const item = self.timeline.itemsData.get(itemId);
    
    // 2. Estrai numeroMacchina dal group
    const machineCode = item.group;  // "M01"
    const match = machineCode.match(/\d+/);
    const numeroMacchina = parseInt(match[0], 10);  // 1
    
    // 3. POST al server
    const response = await fetch('/api/pianificazione/aggiorna-posizione', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            commessaId: item.id,
            numeroMacchina: numeroMacchina,
            dataInizioPrevisione: item.start.toISOString()
        })
    });
    
    // 4. Ricarica pagina per mostrare ricalcoli
    location.reload();
})
```

### **Logica Backend Drag&Drop**
[PianificazioneController.cs](c:\Dev\MESManager\MESManager.Web\Controllers\PianificazioneController.cs#L187-L254):

1. **Aggiorna posizione commessa**:
   - Cambia `NumeroMacchina`
   - Imposta `DataInizioPrevisione`
   - Ricalcola `DataFinePrevisione` usando `PianificazioneService`

2. **Ricalcola sequenza macchina di destinazione**:
   - Ordina per `DataInizioPrevisione`
   - Assegna `OrdineSequenza` sequenziale (1,2,3...)
   - **ACCODAMENTO**: `DataInizio[n] = DataFine[n-1]`
   - Ricalcola tutte le `DataFinePrevisione`

3. **Ricalcola macchina di origine** (se cambio macchina)

---

## **6. CALCOLI TEMPORALI**

### **Durata Prevista**
[PianificazioneService.cs](c:\Dev\MESManager\MESManager.Application\Services\PianificazioneService.cs#L7-L24):
```csharp
durataMinuti = tempoSetupMinuti + (tempoCicloSecondi * quantitaRichiesta / numeroFigure) / 60
```

**Esempio**:
- TempoCiclo: 120 sec
- NumeroFigure: 4 (4 pezzi per ciclo)
- QuantitàRichiesta: 1000
- TempoSetup: 90 minuti

Calcolo:
```
Cicli = 1000 / 4 = 250 cicli
TempoProduzione = 120 * 250 = 30.000 sec = 500 min
DurataTotale = 90 + 500 = 590 minuti
```

### **Data Fine Prevista**
[PianificazioneService.cs](c:\Dev\MESManager\MESManager.Application\Services\PianificazioneService.cs#L26-L67):
```csharp
// Considera:
// - OreLavorativeGiornaliere (es. 8h = 480 min/giorno)
// - GiorniLavorativiSettimanali (es. 5 = Lun-Ven)
// - Salta weekend se giorniLavorativi = 5
```

---

## **7. SISTEMA COLORI E PROGRESSIONE**

### **Colori per Stato**
Linee [170-182](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L170-L182):
```javascript
'InProgrammazione': '#2196F3'  // Blu
'Programmata': '#4CAF50'        // Verde
'InCorso': '#FF9800'            // Arancione
'Completata': '#9E9E9E'         // Grigio
'Sospesa': '#F44336'            // Rosso
```

### **Barra Progressiva**
Linea [74](c:\Dev\MESManager\MESManager.Web\wwwroot\js\gantt\gantt-macchine.js#L74):
```javascript
background: linear-gradient(to right, 
    #4CAF50 45%,           // Parte completata (solida)
    rgba(76,175,80,0.3) 45%)  // Parte rimanente (trasparente)
```

---

## **8. VINCOLI E LIMITAZIONI ATTUALI**

### **Vincoli Implementati** ✅
1. **Ordinamento**: NumeroMacchina → OrdineSequenza
2. **Accodamento**: Commessa[n] inizia quando Commessa[n-1] finisce
3. **Drag&Drop**: Cambia macchina e date, trigger ricalcolo
4. **Filtro visualizzazione**: Solo commesse con date complete e macchina assegnata

### **Limitazioni** ⚠️
1. **Nessun controllo sovrapposizioni**: `stack: false` impedisce visivamente, ma non lato server
2. **Calendario ignorato**: Non considera festività dal `CalendarioLavoro`
3. **Reload completo**: Ogni drag ricarica l'intera pagina
4. **Nessuna validazione conflitti**: Non verifica disponibilità macchina
5. **Percentuale completamento**: Calcolata solo su tempo trascorso, non su quantità prodotta

---

## **9. PUNTI CRITICI PER DEBUGGING**

### **Se Gantt vuoto**:
1. Verifica `SELECT COUNT(*) FROM Commesse WHERE NumeroMacchina IS NOT NULL` → deve essere > 0
2. Verifica `DataInizioPrevisione IS NOT NULL AND DataFinePrevisione IS NOT NULL`
3. Console browser: Controlla `console.log('Items created from real data:', items)`
4. Network tab: Verifica risposta `/api/pianificazione` contenga array con elementi

### **Se drag non salva**:
1. Console browser: Cerca errori fetch `/api/pianificazione/aggiorna-posizione`
2. Verifica logs backend: `"Inizio aggiornamento posizione commessa"`
3. Controlla `NumeroMacchina` estratto correttamente da group code

---

## **10. CONFIGURAZIONI ESTERNE**

### **Impostazioni Gantt**
- **Fonte**: Tabella `ImpostazioniProduzione`
- **Campi usati**:
  - `TempoSetupMinuti` (default: 90)
  - `OreLavorativeGiornaliere` (default: 8)
  - `GiorniLavorativiSettimanali` (default: 5)

### **Macchine**
- **Fonte**: Tabella `Macchine`
- **Filtro**: `AttivaInGantt = true`
- **Ordinamento**: `OrdineVisualizazione`

---

## **RIEPILOGO ESECUTIVO**

Il Gantt attuale è un **sistema a 3 livelli** (Backend API → Blazor → Vis-Timeline JS) che:
- Visualizza commesse ordinate per macchina e sequenza
- Permette drag&drop con persistenza server-side
- Ricalcola automaticamente accodamento dopo ogni spostamento
- Filtra solo commesse con date complete e macchina assegnata
- Usa colori dinamici basati su stato e percentuale completamento

**Punto debole**: Reload completo dopo ogni drag impedisce UX fluida, e manca validazione sovrapposizioni lato server.