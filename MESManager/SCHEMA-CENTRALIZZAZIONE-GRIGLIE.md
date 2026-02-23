# 📊 SCHEMA CENTRALIZZAZIONE GRIGLIE - MESManager

> **Scopo**: Documento di analisi per validare l'approccio di centralizzazione dati tabelle  
> **Data**: 23 Febbraio 2026  
> **Versione**: 1.0 (Pre-implementazione - DA VALIDARE)

---

## 🎯 OBIETTIVO CENTRALIZZAZIONE

**PROBLEMA ATTUALE**: 
- Duplicazione codice JavaScript (6 file grid quasi identici)
- Duplicazione codice C# Blazor (4 pagine Catalogo con toolbar/settings ripetuti)
- Logica business sparsa tra backend DTO, mapping frontend, cellRenderer JS

**SOLUZIONE PROPOSTA**:
1. **Un DTO Backend → Un Campo Calcolato** (es: `ClienteDisplay`)
2. **Factory JS Centralizzata** (`ag-grid-factory.js`)
3. **Componente Blazor Unificato** (`UnifiedGrid.razor`)

---

## 📋 TABELLA 1: COMMESSE (3 Grid Diverse)

### Grid Esistenti
1. **CatalogoCommesse.razor** → `commesses-grid.js` (tutte commesse)
2. **CommesseAperte.razor** → `commesse-aperte-grid.js` (commesse programma)
3. Gantt Tasks (inline data, non grid separata)

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Codice** | `CommessaDto.Codice` | ✅ **Campo Diretto** | Univoco, no calcolo |
| **Num. Ordine** | `CommessaDto.InternalOrdNo` | ✅ **Campo Diretto** | Da Mago |
| **Ordine Esterno** | `CommessaDto.ExternalOrdNo` | ✅ **Campo Diretto** | Da Mago |
| **Linea** | `CommessaDto.Line` | ✅ **Campo Diretto** | Da Mago |
| **Cod. Articolo** | `CommessaDto.ArticoloCodice` | ✅ **Campo Diretto** | Join Articolo |
| **Ricetta** | `CommessaDto.HasRicetta` + `NumeroParametri` | 🔧 **Badge Calcolato JS** | ✅ **CENTRALIZZATO** in `ricetta-column-shared.js` |
| **Descrizione** | `CommessaDto.Description` | ✅ **Campo Diretto** | Da Mago |
| **Cliente** | 🎯 **`CommessaDto.ClienteDisplay`** | ⭐ **CAMPO CALCOLATO BACKEND** | **NUOVA CENTRALIZZAZIONE** |
| **Quantità** | `CommessaDto.QuantitaRichiesta` | ✅ **Campo Diretto** | Numeric |
| **U.M.** | `CommessaDto.UoM` | ✅ **Campo Diretto** | Unit of Measure |
| **Data Consegna** | `CommessaDto.DataConsegna` | ✅ **Campo Diretto** + formatter locale | Date |
| **Stato** | `CommessaDto.Stato` (enum) | 🎨 **Styled Badge** | Aperta/Chiusa con colori |
| **Stato Programma** | `CommessaDto.StatoProgramma` (enum) | 🎨 **Styled Badge** | Solo CommesseAperte |
| **Macchina** | `CommessaDto.NumeroMacchina` | ✅ **Campo Diretto** | Solo se pianificata |
| **Priorità** | `CommessaDto.Priorita` | 🎨 **Color-coded** | 1-5 con colori |
| **Rif. Cliente** | `CommessaDto.RiferimentoOrdineCliente` | ✅ **Campo Diretto** | |
| **Ns. Riferimento** | `CommessaDto.OurReference` | ✅ **Campo Diretto** | |
| **Ultima Modifica** | `CommessaDto.UltimaModifica` | ✅ **Campo Diretto** + formatter | DateTime |
| **Sync** | `CommessaDto.TimestampSync` | ✅ **Campo Diretto** + formatter | DateTime |

### ⭐ CLIENTE DISPLAY - LOGICA CENTRALIZZATA

```csharp
// Backend: CommessaDto (DTO UNICO)
public string? ClienteDisplay => 
    !string.IsNullOrWhiteSpace(ClienteRagioneSociale) 
        ? ClienteRagioneSociale 
        : CompanyName;  // Fallback intelligente
```

**Fonte Originale Dati**:
- `ClienteRagioneSociale`: JOIN `Clienti` via FK `ClienteId`
- `CompanyName`: Campo Mago `MA_SaleOrd.CompanyName` (se Cliente non trovato)

**Frontend (TUTTE le grid)**:
```javascript
{ 
    field: 'clienteDisplay',  // Legge campo calcolato backend
    headerName: 'Cliente',
    sortable: true
}
```

✅ **VANTAGGIO**: 
- Backend decide una volta la logica fallback
- Frontend legge solo il campo finale
- Se domani aggiungiamo `ClienteCodice` come terzo fallback, modifichiamo solo CommessaDto

---

## 📋 TABELLA 2: ARTICOLI

### Grid Esistenti
1. **CatalogoArticoli.razor** → `articoli-grid.js`

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Codice** | `ArticoloDto.Codice` | ✅ **Campo Diretto** | Primary display |
| **Descrizione** | `ArticoloDto.Descrizione` | ✅ **Campo Diretto** | |
| **Prezzo** | `ArticoloDto.Prezzo` | ✅ **Campo Diretto** + formatter € | Decimal 18,4 |
| **Attivo** | `ArticoloDto.Attivo` | ✅ **Campo Diretto** + formatter Sì/No | Boolean |
| **Ultima Modifica** | `ArticoloDto.UltimaModifica` | ✅ **Campo Diretto** + formatter | DateTime |
| **Timestamp Sync** | `ArticoloDto.TimestampSync` | ✅ **Campo Diretto** + formatter | DateTime |
| **ID** | `ArticoloDto.Id` | ✅ **Campo Diretto** | GUID, hidden |

### 🎯 PROPOSTA FUTURA (non implementata in commit)

**Aggiungere campo calcolato**:
```csharp
public string? ArticoloDisplayCompleto => $"{Codice} - {Descrizione}"; // Per dropdown/select
```

**Uso**: Combo box selezione articolo in dialogs

---

## 📋 TABELLA 3: CLIENTI

### Grid Esistenti
1. **CatalogoClienti.razor** → `clienti-grid.js`

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Codice** | `ClienteDto.Codice` | ✅ **Campo Diretto** | Codice Mago |
| **Ragione Sociale** | `ClienteDto.RagioneSociale` | ✅ **Campo Diretto** | Primary name |
| **Email** | `ClienteDto.Email` | ✅ **Campo Diretto** | Contact |
| **Note** | `ClienteDto.Note` | ✅ **Campo Diretto** | Free text |
| **Attivo** | `ClienteDto.Attivo` | ✅ **Campo Diretto** + formatter Sì/No | Boolean |
| **Ultima Modifica** | `ClienteDto.UltimaModifica` | ✅ **Campo Diretto** + formatter | DateTime |
| **Timestamp Sync** | `ClienteDto.TimestampSync` | ✅ **Campo Diretto** + formatter | DateTime |
| **ID** | `ClienteDto.Id` | ✅ **Campo Diretto** | GUID, hidden |

### 🎯 PROPOSTA FUTURA

**Campo calcolato per display compact**:
```csharp
public string ClienteDisplayBreve => 
    $"{Codice} - {RagioneSociale.Substring(0, Math.Min(30, RagioneSociale.Length))}"; 
```

---

## 📋 TABELLA 4: ANIME

### Grid Esistenti
1. **CatalogoAnime.razor** → `anime-grid.js`

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Codice Anime** | `AnimeDto.CodiceAnime` | ✅ **Campo Diretto** | Identificativo scheda |
| **Codice Articolo** | `AnimeDto.CodiceArticolo` | ✅ **Campo Diretto** | Link ad Articolo |
| **Ricetta** | `AnimeDto.HasRicetta` + `NumeroParametri` | 🔧 **Badge Calcolato** | ✅ **CENTRALIZZATO** in `ricetta-column-shared.js` |
| **Descrizione** | `AnimeDto.DescrizioneArticolo` | ✅ **Campo Diretto** | |
| **Cliente** | `AnimeDto.Cliente` | ✅ **Campo Diretto** | Nome cliente dalla scheda |
| **Figure** | `AnimeDto.Figure` | ✅ **Campo Diretto** | Numeric |
| **Larghezza** | `AnimeDto.Larghezza` | ✅ **Campo Diretto** | mm |
| **Altezza** | `AnimeDto.Altezza` | ✅ **Campo Diretto** | mm |
| **Profondità** | `AnimeDto.Profondita` | ✅ **Campo Diretto** | mm |
| **Peso** | `AnimeDto.Peso` | ✅ **Campo Diretto** | kg (string) |
| **Quantità Piano** | `AnimeDto.QuantitaPiano` | ✅ **Campo Diretto** | Pezzi/piano |
| **Numero Piani** | `AnimeDto.NumeroPiani` | ✅ **Campo Diretto** | Piani |
| **Macchine Disponibili** | `AnimeDto.MacchineSuDisponibili` | 🎯 **Lista Parsabile** | Es: "1,2,5" |
| **Note** | `AnimeDto.Note` | ✅ **Campo Diretto** | Free text |
| **Modificato Localmente** | `AnimeDto.ModificatoLocalmente` | ✅ **Campo Diretto** + badge | Boolean |

### 🎯 CAMPO CALCOLATO PROPOSTO

```csharp
// Backend: AnimeDto
public string DimensioniDisplay => 
    $"{Larghezza}×{Altezza}×{Profondita} mm";  // Formato compatto

public int? TotalePezziCalcolato => 
    (QuantitaPiano ?? 0) * (NumeroPiani ?? 0);  // Calcolo lotto
```

**Frontend**:
```javascript
{ field: 'dimensioniDisplay', headerName: 'Dimensioni' }
{ field: 'totalePezziCalcolato', headerName: 'Tot. Pezzi' }
```

---

## 📋 TABELLA 5: PLC REALTIME

### Grid Esistenti
1. **PlcRealtime.razor** → `plc-realtime-grid.js` (inline table MudBlazor)

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Macchina** | `PLCRealtimeDto.MacchinaNumero` / `MacchineNome` | ✅ **Campo Diretto** | |
| **Operatore** | `PLCRealtimeDto.NumeroOperatore` | ✅ **Campo Diretto** | Numero PLC |
| **Stato** | `PLCRealtimeDto.StatoMacchina` | 🎨 **Badge Colorato** | Auto/Manuale/Fermo |
| **Barcode** | `PLCRealtimeDto.BarcodeLavorazione` | ✅ **Campo Diretto** | ID lavorazione |
| **Cicli Fatti** | `PLCRealtimeDto.CicliFatti` | ✅ **Campo Diretto** | Counter |
| **Quantità** | `PLCRealtimeDto.QuantitaDaProdurre` | ✅ **Campo Diretto** | Target |
| **Figure** | `PLCRealtimeDto.Figure` | ✅ **Campo Diretto** | |
| **Tempo Medio** | `PLCRealtimeDto.TempoMedio` | ✅ **Campo Diretto** + formatter sec | |
| **Scarti** | `PLCRealtimeDto.CicliScarti` | 🎨 **Rosso se >0** | |
| **Ultimo Agg.** | `PLCRealtimeDto.DataUltimoAggiornamento` | ✅ **Campo Diretto** + formatter | DateTime |

### 🎯 CAMPO CALCOLATO PROPOSTO

```csharp
// Backend: PLCRealtimeDto
public decimal PercentualeCompletamento => 
    QuantitaDaProdurre > 0 
        ? (decimal)CicliFatti / QuantitaDaProdurre * 100 
        : 0;

public string StatoProduzioneDisplay => 
    QuantitaRaggiunta ? "✅ Completata" : 
    CicliFatti == 0 ? "⏸️ Non iniziata" : 
    "🔄 In corso";
```

---

## 📋 TABELLA 6: PLC STORICO

### Grid Esistenti
1. **PlcStorico.razor** → `plc-storico-grid.js`

### SCHEMA COLONNE CENTRALIZZATE

| **Colonna UI** | **Fonte Dati Backend** | **Tipo Centralizzazione** | **Note** |
|----------------|------------------------|---------------------------|----------|
| **Data/Ora** | `PLCStoricoDto.DataOra` | ✅ **Campo Diretto** + formatter | DateTime |
| **Macchina** | `PLCStoricoDto.MacchinaNumero` | ✅ **Campo Diretto** | |
| **Operatore** | `PLCStoricoDto.NumeroOperatore` | ✅ **Campo Diretto** | |
| **Stato** | Parsed da `PLCStoricoDto.Dati` (JSON) | 🔧 **JSON Parsed** | Da centralizzare |
| **Dati Completi** | `PLCStoricoDto.Dati` (JSON string) | ✅ **Campo Diretto** | Collapsible |

### ⚠️ PROBLEMA ATTUALE

**Dati JSON non strutturati** - Parsing frontend

### 🎯 SOLUZIONE PROPOSTA

```csharp
// Backend: PLCStoricoDto
public PlcDataSnapshot? DatiStrutturati { get; set; }  // Deserializzato

// Invece di JSON string, esporre proprietà tipizzate
public int CicliFatti => DatiStrutturati?.CicliFatti ?? 0;
public int Scarti => DatiStrutturati?.Scarti ?? 0;
// ...etc
```

---

## 🏗️ CENTRALIZZAZIONE JAVASCRIPT PROPOSTA

### FILE ATTUALE (Duplicati)

```
wwwroot/
├── js/
│   ├── commesse-grid.js         (233 righe)  ❌ DUPLICATO
│   ├── articoli-grid.js         (379 righe)  ❌ DUPLICATO
│   ├── clienti-grid.js          (352 righe)  ❌ DUPLICATO
│   ├── anime-grid.js            (450+ righe) ❌ DUPLICATO
│   └── ricetta-column-shared.js (86 righe)   ✅ GIÀ CENTRALIZZATO
├── lib/ag-grid/
│   ├── commesse-grid.js         (297 righe)  ❌ DUPLICATO (versione diversa!)
│   ├── commesse-aperte-grid.js  (250+ righe) ❌ DUPLICATO
│   └── plc-storico-grid.js      (...)        ❌ DUPLICATO
```

**Codice duplicato totale**: ~1800+ righe

### FILE PROPOSTO (Centralizzato)

```
wwwroot/
├── js/
│   ├── ag-grid-factory.js       (300 righe) ⭐ FACTORY CENTRALIZZATA
│   ├── ricetta-column-shared.js (86 righe)  ✅ GIÀ FATTO
│   └── grid-configs/
│       ├── commesse-config.js   (50 righe)  📋 Solo columnDefs
│       ├── articoli-config.js   (30 righe)  📋 Solo columnDefs
│       ├── clienti-config.js    (30 righe)  📋 Solo columnDefs
│       └── anime-config.js      (40 righe)  📋 Solo columnDefs
```

**Codice totale**: ~536 righe (**-70% duplicazione**)

### 🎯 ag-grid-factory.js - SCHEMA

```javascript
window.agGridFactory = (function() {
    
    // Metodi comuni centralizzati
    function init(gridId, columnDefs, data, options) { /* ... */ }
    function setState(gridApi, state) { /* ... */ }
    function getState(gridApi) { /* ... */ }
    function exportCsv(gridApi, filename) { /* ... */ }
    function setQuickFilter(gridApi, text) { /* ... */ }
    function setUiVars(gridApi, settings) { /* ... */ }
    
    return {
        createGrid: function(config) {
            // config = { gridId, columnDefs, data, savedState, options }
            const gridApi = init(config.gridId, config.columnDefs, config.data, config.options);
            
            // Register namespace for easy access
            window[config.namespace] = {
                gridApi: gridApi,
                getState: () => getState(gridApi),
                setState: (state) => setState(gridApi, state),
                exportCsv: () => exportCsv(gridApi, config.namespace),
                setQuickFilter: (text) => setQuickFilter(gridApi, text),
                setUiVars: (settings) => setUiVars(gridApi, settings)
            };
            
            return gridApi;
        }
    };
})();
```

**Uso in Blazor**:
```javascript
await JSRuntime.InvokeVoidAsync("agGridFactory.createGrid", new {
    gridId = "commesseGrid",
    namespace = "commesseGrid",
    columnDefs = CommesseColumnConfig.GetColumns(),  // Config separata
    data = _commesse,
    savedState = settings.ColumnStateJson
});
```

---

## 🎨 CENTRALIZZAZIONE BLAZOR PROPOSTA

### FILE ATTUALE (Duplicati)

```
Components/Pages/Cataloghi/
├── CatalogoCommesse.razor     (475 righe) ❌ 80% duplicato
├── CatalogoArticoli.razor     (450+ righe) ❌ 80% duplicato
├── CatalogoClienti.razor      (450+ righe) ❌ 80% duplicato
└── CatalogoAnime.razor        (500+ righe) ❌ 80% duplicato
```

**Toolbar/Settings panel duplicato**: ~150 righe × 4 = **600 righe duplicate**

### FILE PROPOSTO (Centralizzato)

```
Components/Shared/
├── UnifiedGrid.razor          (250 righe) ⭐ COMPONENTE BASE
├── GridToolbar.razor          (100 righe) 🔧 Toolbar riutilizzabile
└── GridSettingsPanel.razor    (80 righe)  ⚙️ Settings riutilizzabile
```

### 🎯 UnifiedGrid.razor - SCHEMA

```razor
@typeparam TItem
@code {
    [Parameter] public string GridId { get; set; }
    [Parameter] public string ApiEndpoint { get; set; }
    [Parameter] public List<ColumnDefinition> Columns { get; set; }
    [Parameter] public bool ShowToolbar { get; set; } = true;
    [Parameter] public bool EnableExport { get; set; } = true;
    [Parameter] public RenderFragment<TItem>? CustomActions { get; set; }
}

<div class="unified-grid-container">
    @if (ShowToolbar)
    {
        <GridToolbar OnSearch="@OnSearch" 
                     OnExport="@OnExport" 
                     OnRefresh="@LoadData" />
    }
    
    <div id="@GridId" class="ag-theme-alpine"></div>
    
    <GridSettingsPanel @bind-Settings="settings" 
                      OnSettingsChanged="@ApplySettings" />
</div>
```

**Uso nelle pagine**:
```razor
<UnifiedGrid TItem="CommessaDto" 
             GridId="commesseGrid"
             ApiEndpoint="/api/Commesse"
             Columns="@CommesseColumns"
             ShowToolbar="true"
             EnableExport="true">
    <CustomActions Context="commessa">
        <MudButton OnClick="@(() => EditCommessa(commessa.Id))">Edit</MudButton>
    </CustomActions>
</UnifiedGrid>
```

---

## ✅ CHECKLIST VALIDAZIONE

Prima di procedere con implementazione, conferma:

### Backend (DTO)
- [ ] ✅ `ClienteDisplay` su `CommessaDto` è corretto?
- [ ] 📋 Servono altri campi calcolati? (es: `DimensioniDisplay` per Anime)
- [ ] 🔄 Logica fallback Cliente → CompanyName è accettabile?

### JavaScript
- [ ] ✅ Factory pattern `ag-grid-factory.js` è la strada giusta?
- [ ] 📂 Separare columnDefs in file config è meglio di inline?
- [ ] 🔧 `ricetta-column-shared.js` è un buon esempio da replicare?

### Blazor
- [ ] ✅ `UnifiedGrid<TItem>` con generics è la soluzione corretta?
- [ ] 🎨 Toolbar/Settings separati in componenti riutilizzabili OK?
- [ ] 📋 CustomActions via RenderFragment è sufficiente per casi speciali?

### Generale
- [ ] 🚫 Ci sono colonne che NON possono essere centralizzate?
- [ ] ⚠️ Casi edge particolari da gestire?
- [ ] 🔄 Priorità implementazione: quali grid fare per prime?

---

## 📊 RIEPILOGO CENTRALIZZAZIONI

| **Ambito** | **Duplicazione Attuale** | **After Centralizzazione** | **Risparmio** |
|------------|--------------------------|----------------------------|---------------|
| **JavaScript Grid** | ~1800 righe (6 file) | ~536 righe | **-70%** |
| **Blazor Pages** | ~1900 righe (4 file) | ~700 righe | **-63%** |
| **Backend DTO Logic** | Sparsa in FE | Centralizzata in DTO | **100% backend** |

**Totale righe risparmiate**: ~2500 righe

**Manutenibilità**: 
- Da 10 punti di modifica → **2-3 punti** (Factory + UnifiedGrid + DTO)

---

## 🚀 PROSSIMI STEP

**Se schema approvato**:

1. **Phase 1 - Backend** (2h)
   - Aggiungere campi calcolati mancanti ai DTO
   - Test unitari su logica calcolo

2. **Phase 2 - JavaScript Factory** (4h)
   - Creare `ag-grid-factory.js`
   - Migrare prima grid (commesse)
   - Test funzionale

3. **Phase 3 - Blazor Component** (6h)
   - Creare `UnifiedGrid.razor`
   - Migrare CatalogoCommesse
   - Test E2E

4. **Phase 4 - Migrazione Completa** (8h)
   - Migrare tutte le altre grid
   - Eliminare file duplicati
   - Documentazione

**Tempo totale stimato**: 20 ore (2.5 giorni)

---

**Aspetto conferma per procedere** 🎯
