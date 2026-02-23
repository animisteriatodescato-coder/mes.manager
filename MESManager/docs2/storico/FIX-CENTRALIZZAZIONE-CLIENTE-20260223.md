# FIX - Centralizzazione Cliente Display con Cache Busting (23 Feb 2026)

**Data**: 23 Febbraio 2026  
**Versione**: v1.48.0  
**Severity**: 🔴 CRITICAL - Dati errati visualizzati agli utenti  
**Status**: ✅ RISOLTO - Verificato da utente

---

## 📋 Problema Iniziale

**Sintomo**: Pagine diverse mostravano clienti DIVERSI per le stesse commesse:
- **Catalogo Commesse** (`/cataloghi/commesse`): Mostrava fornitori (TIM S.P.A., TONER ITALIA)
- **Commesse Aperte** (`/programma/commesse-aperte`): Mostrava fonderie corrette (OLMAT SRL, GDC CAST)

**Aspettativa**: Secondo principio **ZERO DUPLICAZIONE** della BIBBIA, entrambe le pagine dovrebbero mostrare dati IDENTICI letti dalla stessa fonte.

---

## 🔍 Root Cause Analysis - FASE 1: Backend

### Duplicazione Fonte Dati

Esistevano 2 campi per rappresentare il cliente nella `CommessaDto`:

```csharp
public class CommessaDto {
    public string? CompanyName { get; set; }              // Da sync Mago
    public string? ClienteRagioneSociale { get; set; }    // Da Clienti table via FK
}
```

### Problema Dati Fonte

**CompanyName (sync Mago)**:
- ✅ Contiene dati CORRETTI: fonderie cliente (OLMAT SRL, GDC CAST S.P.A., VDP FONDERIA)
- ⚠️ Sempre popolato dalla sincronizzazione Mago
- ⚠️ Dati misti: alcuni record contengono fornitori invece di clienti

**ClienteRagioneSociale (tabella Clienti)**:
- ❌ Contiene dati ERRATI: fornitori invece di clienti (TIM S.P.A., TONER ITALIA)
- ⚠️ Spesso NULL: ClienteId FK non sempre valorizzato nella sync Mago
- 🐛 Root cause: Tabella `Clienti` popolata con entità sbagliate (fornitori inseriti come clienti)

### Tentativo Fix Fallito

**Approccio**: Prioritizzare ClienteRagioneSociale come fonte "ufficiale"

```csharp
// TENTATIVO 1 - FALLITO ❌
public string ClienteDisplay => ClienteRagioneSociale ?? CompanyName ?? "N/D";
```

**Risultato**: 
- Peggiora il problema
- Mostra fornitori (TIM, TONER) invece delle fonderie
- Causa: la priorità era invertita - fonte "errata" aveva precedenza su fonte "corretta"

### Fix Backend Corretto

**Soluzione**: Invertire priorità - CompanyName (Mago) è fonte affidabile

```csharp
// FIX BACKEND - CORRETTO ✅
public string ClienteDisplay => CompanyName ?? ClienteRagioneSociale ?? "N/D";
```

**File**: `MESManager.Application/DTOs/CommessaDto.cs`

**Motivazione**:
- CompanyName contiene dati sync ERP reali
- ClienteRagioneSociale dipende da FK ClienteId che ha data quality scadente
- Fallback garantisce sempre visualizzazione (mai campo vuoto)

---

## 🔍 Root Cause Analysis - FASE 2: Frontend NON Centralizzato

### Il Problema Persisteva

Nonostante il fix backend, **il problema rimaneva**:
- Catalogo Commesse: ancora TIM/TONER ❌
- Commesse Aperte: fonderie corrette ✅

**Diagnosi**: Se backend ritorna campo centralized (`clienteDisplay`), come è possibile vedere dati diversi?

### Investigazione File JavaScript

**Scoperta CRITICA**: I file JS usavano campi DIVERSI!

#### File 1: `/lib/ag-grid/commesse-grid.js` (Catalogo Commesse)

```javascript
// PRIMA - SBAGLIATO ❌
{ 
    field: 'clienteRagioneSociale',  // Campo VECCHIO errato
    headerName: 'Cliente', 
    sortable: true, 
    filter: true, 
    width: 250 
}
```

**Risultato**: Leggeva campo con fornitori (TIM, TONER) ❌

#### File 2: `/lib/ag-grid/commesse-aperte-grid.js` (Commesse Aperte)

```javascript
// GIÀ CORRETTO ✅
{ 
    field: 'clienteDisplay',  // Campo NUOVO centralizzato
    headerName: 'Cliente', 
    sortable: true, 
    filter: true, 
    width: 250 
}
```

**Risultato**: Leggeva campo con fonderie corrette ✅

### Problema #1 - NON Centralizzazione

**Violazione BIBBIA**: 
```
🚫 ZERO DUPLICAZIONE - Codice duplicato = ERRORE GRAVE
✅ UNA fonte di verità - Modificabile da UN solo punto
```

**Realtà**: DUE fonti diverse, DUE risultati diversi

---

## 🔍 Problemi Aggiuntivi Trovati

### Problema #2 - Programma Macchine Grid

**File**: `/lib/ag-grid/programma-macchine-grid.js`

**Occorrenze**: 2 punti usavano `clienteRagioneSociale`:
1. Check dati etichetta completi (linea ~60)
2. Colonna grid cliente (linea ~152)

### Problema #3 - Cache Busting Mancante

**File**: `MESManager.Web/Components/App.razor`

```html
<!-- PRIMA - Versione NON incrementata -->
<script src="/lib/ag-grid/commesse-grid.js?v=1455"></script>
<script src="/lib/ag-grid/commesse-aperte-grid.js?v=1455"></script>
```

**Problema**: 
- File JS modificati in wwwroot
- Parametro `?v=1455` NON incrementato
- Browser serviva versione CACHED vecchia
- Hard refresh non sufficiente → serve cambio query string

---

## ✅ Soluzione Definitiva

### Fix 1 - Centralizzazione JavaScript

**commesse-grid.js** (Catalogo Commesse):
```javascript
{ 
    field: 'clienteDisplay',  // ✅ CORRETTO
    headerName: 'Cliente', 
    sortable: true, 
    filter: true, 
    width: 250 
}
```

### Fix 2 - Programma Macchine Grid

**programma-macchine-grid.js** (2 occorrenze):

```javascript
// Check dati etichetta
const hasData = params.data && 
                params.data.codiceAnime && 
                params.data.clienteDisplay;  // ✅ CORRETTO

// Colonna grid
{ 
    field: 'clienteDisplay',  // ✅ CORRETTO
    headerName: 'Cliente', 
    sortable: true, 
    filter: true, 
    width: 250 
}
```

### Fix 3 - Commesse Aperte Fallback Logic

**commesse-aperte-grid.js**:

```javascript
// PRIMA - Fallback duplicato in FE ❌
function hasDatiEtichettaCompleti(data) {
    return data && 
           data.codiceAnime && 
           (data.clienteRagioneSociale || data.companyName); // Logica duplicata
}

// DOPO - Usa campo centralizzato ✅
function hasDatiEtichettaCompleti(data) {
    return data && 
           data.codiceAnime && 
           data.clienteDisplay; // Backend gestisce fallback
}
```

### Fix 4 - Cache Busting

**App.razor**:

```html
<!-- DOPO - Versione incrementata ✅ -->
<script src="/lib/ag-grid/commesse-grid.js?v=1457"></script>
<script src="/lib/ag-grid/commesse-aperte-grid.js?v=1457"></script>
<script src="/lib/ag-grid/programma-macchine-grid.js?v=1457"></script>
```

**Incremento**: `v=1455` → `v=1457` (saltato 1456 in iterazione precedente)

---

## 📊 File Modificati Totali

### Backend
1. `MESManager.Application/DTOs/CommessaDto.cs` - Proprietà ClienteDisplay
2. `MESManager.Infrastructure/Services/CommessaAppService.cs` - Mapping CompanyName

### Frontend
3. `MESManager.Web/wwwroot/lib/ag-grid/commesse-grid.js` - clienteDisplay
4. `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` - clienteDisplay
5. `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` - clienteDisplay (2x)
6. `MESManager.Web/Components/App.razor` - Cache busting v=1457
7. `MESManager.Web/Components/Pages/Programma/CommesseAperte.razor` - Label print

### Docs
8. `docs2/08-CHANGELOG.md` - Entry v1.48.0 completa
9. `docs2/BIBBIA-AI-MESMANAGER.md` - Regola cache busting
10. `docs2/storico/FIX-CENTRALIZZAZIONE-CLIENTE-20260223.md` - Questo file

---

## 🎓 Lezioni Apprese

### 1. Backend Fix NON Basta

**Errore**: Presumere che fix backend risolva automaticamente frontend  
**Realtà**: Frontend può leggere campi diversi/obsoleti  
**Soluzione**: Verificare SEMPRE field binding in tutti i componenti UI

### 2. Ricerca Pattern Incompleta

**Errore comune**:
```bash
# Cercare solo un file tipo
grep -r "clienteRagioneSociale" wwwroot/js/
```

**Ricerca corretta**:
```bash
# Cercare in TUTTI i file statici
grep -r "clienteRagioneSociale" wwwroot/
grep -r "companyName" wwwroot/
```

**Lezione**: Cercare in directory intere, non solo sottocartelle note

### 3. Cache Busting È OBBLIGATORIO

**Problema**: Hard refresh browser (CTRL+F5) NON garantisce download nuovo file

**Motivo**: 
- Browser controlla `ETag` e `Last-Modified` headers
- Se file timestamp uguale → serve cached version
- Query string diversa → forza download nuovo file

**Soluzione**: 
```html
<!-- SEMPRE incrementare ?v=XXXX dopo modifica -->
<script src="/lib/ag-grid/file.js?v=1457"></script>
```

**Best Practice**: Incremento monotono (mai decrementare, mai riutilizzare)

### 4. Debugging Multi-Layer

**Flow corretto**:
1. ✅ Verifica API ritorna dati corretti (`Invoke-RestMethod`)
2. ✅ Verifica DTO serializzazione corretta (JSON fields)
3. ✅ Verifica file JS usano field corretti (grep search)
4. ✅ Verifica browser riceve file aggiornati (Network tab, ?v param)
5. ✅ Verifica case sensitivity (JS: camelCase vs C#: PascalCase)

**NON fermarsi al fix backend** - problema può essere a qualsiasi layer

### 5. ZERO DUPLICAZIONE - Logica Centralizzata

**PRIMA** (SBAGLIATO ❌):
- Backend: `CompanyName` + `ClienteRagioneSociale` separati
- Frontend 1: Legge `clienteRagioneSociale`
- Frontend 2: Legge `clienteDisplay`
- Frontend 3: Legge `companyName || clienteRagioneSociale` (fallback duplicato)

**DOPO** (CORRETTO ✅):
- Backend: `ClienteDisplay` (proprietà calcolata, logica fallback centralizzata)
- Frontend 1: Legge `clienteDisplay`
- Frontend 2: Legge `clienteDisplay`
- Frontend 3: Legge `clienteDisplay`

**Risultato**: IMPOSSIBILE vedere dati diversi - UNA fonte, UN calcolo, TUTTI leggono

---

## 🔧 Tool/Comandi Utili

### Verifica API Response

```powershell
# Test endpoint API
$data = Invoke-RestMethod -Uri "http://localhost:5156/api/Commesse"

# Verifica field presence
$data | Get-Member | Select-Object Name
$data[0] | Format-List

# Filtra record specifici
$data | Where-Object {$_.companyName -like '*TIM*'} | Select-Object companyName, clienteRagioneSociale, clienteDisplay
```

### Cerca Pattern in JS Files

```powershell
# Pattern search in wwwroot
cd C:\Dev\MESManager\MESManager.Web\wwwroot
Select-String -Pattern "clienteRagioneSociale|companyName" -Path **/*.js -CaseSensitive

# Grep via VSCode
# CTRL+SHIFT+F → includePattern: **/*.js → isRegexp: true → query: field:\s*['"]cliente
```

### Cache Busting Verification

```powershell
# Check current version in App.razor
Select-String -Path "MESManager.Web/Components/App.razor" -Pattern "\?v=\d+"

# Network tab browser (F12)
# Filtra: JS → Verifica query string ?v=XXXX
```

---

## 📋 Checklist Fix Simili

Quando si verifica problema "dati diversi su pagine diverse":

- [ ] **Backend**: Verificare campi DTO (duplicazioni?)
- [ ] **API**: Test endpoint con `Invoke-RestMethod`
- [ ] **Serialization**: JSON contiene field corretto? (camelCase)
- [ ] **Frontend**: Grep search TUTTI file JS/Razor per field names
- [ ] **Case sensitivity**: JS usa camelCase, C# usa PascalCase
- [ ] **Cache busting**: Incrementato ?v=XXXX per file statici modificati?
- [ ] **Browser cache**: Hard refresh + verifica Network tab
- [ ] **Centralizzazione**: UNA sola fonte verità, ZERO duplicazione logica
- [ ] **Verifica utente**: Test ENTRAMBE le pagine dopo fix

---

## 🎯 Principi BIBBIA Applicati

✅ **ZERO DUPLICAZIONE**: Eliminata duplicazione logica display cliente  
✅ **UNA fonte di verità**: Backend calcola, FE solo legge  
✅ **Scalabile e manutenibile**: Futuri grid usano stesso campo centralizzato  
✅ **Build + Run + Test**: Ogni modifica → rebuild → server restart → verifica utente  

---

## ⚠️ Action Items Futuri

### Data Quality Improvement

**Problema**: Tabella `Clienti` contiene fornitori invece di clienti reali

**Todo**:
1. [ ] Analisi: Query per identificare record errati (fornitori in tabella Clienti)
2. [ ] Cleanup: Script SQL per rimuovere/spostare fornitori
3. [ ] Validazione: Controlli in sync Mago per impedire inserimento fornitori
4. [ ] Migration: Associare `ClienteId` FK alle commesse esistenti basandosi su `CompanyName`

**Obiettivo finale**: Eliminare fallback, usare solo `ClienteRagioneSociale` come fonte unica

### Sync Mago Improvement

**Problema**: `ClienteId` FK non sempre valorizzato durante sync

**Todo**:
1. [ ] Analisi: Perché ClienteId è NULL in alcune commesse?
2. [ ] Fix sync: Match `CompanyName` con tabella `Clienti.RagioneSociale`
3. [ ] Valorizza: ClienteId FK per tutte le commesse future
4. [ ] Test: Verifica ClienteRagioneSociale = CompanyName dopo fix

---

## 📚 Riferimenti

- [BIBBIA-AI-MESMANAGER.md](../BIBBIA-AI-MESMANAGER.md) - Principio ZERO DUPLICAZIONE
- [08-CHANGELOG.md](../08-CHANGELOG.md#v1480) - Entry versione v1.48.0
- [04-ARCHITETTURA.md](../04-ARCHITETTURA.md) - Clean Architecture, DTO patterns

---

**Autore Fix**: AI Agent (GitHub Copilot)  
**Verificato da**: Utente (23 Feb 2026)  
**Status finale**: ✅ RISOLTO - Entrambe pagine mostrano fonderie corrette
