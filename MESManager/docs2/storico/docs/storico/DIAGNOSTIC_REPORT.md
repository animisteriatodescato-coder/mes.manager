# REPORT DIAGNOSTICO GANTT MACCHINE
**Data**: 20 Gennaio 2026  
**Obiettivo**: Identificare problematiche reali prima dei fix incrementali

---

## 🔍 RISULTATI QUERY DIAGNOSTICHE

### ✅ **Query 1: Commesse senza date**
```sql
SELECT COUNT(*) FROM Commesse 
WHERE NumeroMacchina IS NOT NULL 
  AND (DataInizioPrevisione IS NULL OR DataFinePrevisione IS NULL)
```
**Risultato**: `0`  
**Stato**: ✅ **NESSUN PROBLEMA** - Tutte le 20 commesse pianificate hanno date complete

---

### 🔴 **Query 2: OrdineSequenza duplicati**
```sql
SELECT NumeroMacchina, OrdineSequenza, COUNT(*) as Cnt 
FROM Commesse 
WHERE NumeroMacchina IS NOT NULL 
GROUP BY NumeroMacchina, OrdineSequenza 
HAVING COUNT(*) > 1
```
**Risultato**:
| NumeroMacchina | OrdineSequenza | Count |
|----------------|----------------|-------|
| 1              | 0              | 3     |
| 2              | 0              | 4     |
| 6              | 0              | 2     |

**Stato**: 🔴 **PROBLEMA CRITICO**  
**Evidenza**: 9 commesse con `OrdineSequenza = 0` (valore di default non assegnato)

**Dettaglio Macchina 1**:
```
Codice                 | NumeroMacchina | OrdineSequenza
-----------------------|----------------|---------------
04214-OM08800/0201-01  | 1              | 0  ← NON ASSEGNATO
04214-OM08800/0201-02  | 1              | 0  ← NON ASSEGNATO
11000-30306005         | 1              | 0  ← NON ASSEGNATO
266702-A28616A02B      | 1              | 1  ✓
87-WW08150             | 1              | 2  ✓
89258-302601           | 1              | 3  ✓
89445-300371           | 1              | 4  ✓
```

**Analisi Sequenza per Macchina**:
| Macchina | Totale Commesse | Sequenze Uniche | Gap Presenti |
|----------|-----------------|-----------------|--------------|
| 1        | 7               | 5               | 3 con seq=0  |
| 2        | 4               | 1               | 4 con seq=0  |
| 3        | 2               | 2               | ✓ OK         |
| 4        | 3               | 3               | ✓ OK         |
| 6        | 3               | 2               | 2 con seq=0  |
| 8        | 1               | 1               | ✓ OK         |

**Root Cause**: Le commesse aggiunte a macchine SENZA chiamare `RicalcolaSequenzaMacchina` mantengono il valore di default (0). Questo succede se:
- Commesse assegnate manualmente via Programma Macchine (non tramite drag Gantt)
- Modifiche dirette al database
- Vecchi dati prima dell'implementazione di OrdineSequenza

---

### 🟡 **Query 3: Articoli problematici**
```sql
SELECT COUNT(*) FROM Articoli 
WHERE TempoCiclo IS NULL OR TempoCiclo = 0 
   OR NumeroFigure IS NULL OR NumeroFigure = 0
```
**Risultato**: `2095` articoli nel catalogo  
**Articoli problematici IN USO nel Gantt**: `6` su 20 commesse

**Stato**: 🟡 **IMPATTO MEDIO**  
**Evidenza**: 2095/totale articoli senza dati produttivi, ma solo 6 impattano commesse pianificate
**Conseguenza**: Durata calcolata = solo `TempoSetupMinuti` (90 min) senza tempo ciclo

---

### ✅ **Query 4: Impostazioni Produzione**
```sql
SELECT TempoSetupMinuti, OreLavorativeGiornaliere, GiorniLavorativiSettimanali 
FROM ImpostazioniProduzione
```
**Risultato**:
| Campo                         | Valore | Validazione |
|------------------------------|--------|-------------|
| TempoSetupMinuti             | 90     | ✓ OK        |
| OreLavorativeGiornaliere     | 8      | ✓ OK        |
| GiorniLavorativiSettimanali  | 5      | ✓ OK        |

**Stato**: ✅ **CONFIGURAZIONE CORRETTA**

---

### ✅ **Query 5: Calendario Lavoro**
```sql
SELECT COUNT(*) FROM CalendarioLavoro
```
**Risultato**: `1` record presente

**Dettaglio**:
| Campo     | Valore   | Significato |
|-----------|----------|-------------|
| OraInizio | 08:00    | ✓ OK        |
| OraFine   | 17:00    | ✓ OK (9h disponibili, 8h lavorative + 1h pausa) |
| Lunedì    | 1 (Sì)   | ✓ Giorno lavorativo |
| Martedì   | 1 (Sì)   | ✓ Giorno lavorativo |
| Mercoledì | 1 (Sì)   | ✓ Giorno lavorativo |
| Giovedì   | 1 (Sì)   | ✓ Giorno lavorativo |
| Venerdì   | 1 (Sì)   | ✓ Giorno lavorativo |
| Sabato    | 0 (No)   | ✓ Weekend |
| Domenica  | 0 (No)   | ✓ Weekend |

**Stato**: ✅ **CALENDARIO CONFIGURATO CORRETTAMENTE**  
**Problema**: ⚠️ **NON VIENE USATO** - PianificazioneService usa solo `GiorniLavorativiSettimanali` (5) per saltare weekend, ma ignora `OraInizio/OraFine`

---

## 🎯 PROBLEMATICHE CONFERMATE

### 🔴 **CRITICHE (da fixare subito)**

#### **P1 - OrdineSequenza non sincronizzato**
- **Evidenza**: 9 commesse su 20 hanno `OrdineSequenza = 0`
- **Impatto**: Ordinamento errato in Gantt e ricalcoli sbagliati
- **Root Cause**: Commesse aggiunte manualmente senza trigger `RicalcolaSequenzaMacchina`
- **Fix Priorità**: ALTA - Fix query SQL immediata + modifica API Programma Macchine

#### **P2 - Evento `changed` + `location.reload()`**
- **Evidenza**: Codice linea 110-156 gantt-macchine.js
- **Impatto**: Nessun rollback possibile, perde zoom/scroll, UX pessima
- **Root Cause**: Architettura "fire and forget" invece di callback-based
- **Fix Priorità**: ALTA - Sostituire con `onMove` callback

#### **P3 - Accodamento forza date utente**
- **Evidenza**: RicalcolaSequenzaMacchina linea 310 PianificazioneController.cs  
  `commessa.DataInizioPrevisione = dataFineUltima.Value`
- **Impatto**: Drag utente ignorato, posizione ricalcolata server-side
- **Root Cause**: Logic business "accodamento forzato" sovrascrive input utente
- **Fix Priorità**: ALTA - Parametrizzare: "accodamento auto" vs "posizione manuale"

#### **P4 - API ritorna solo 204 NoContent**
- **Evidenza**: AggiornaPosizione linea 244 PianificazioneController.cs
- **Impatto**: Client non sa nuove date/sequenze → DEVE ricaricare
- **Root Cause**: Endpoint non ritorna stato aggiornato
- **Fix Priorità**: ALTA - Ritornare JSON con commesse ricalcolate

#### **P5 - Regex `match(/\d+/)` fragile per NumeroMacchina**
- **Evidenza**: gantt-macchine.js linea 128
- **Impatto**: Se formato codice macchina cambia → mapping break
- **Root Cause**: Groups.id = "M01" (string) invece di NumeroMacchina (int)
- **Fix Priorità**: MEDIA - Usare direttamente NumeroMacchina come group id

---

### 🟡 **IMPORTANTI (miglioramento UX)**

#### **P6 - Calendario orari ignorato**
- **Evidenza**: CalendarioLavoro esiste ma PianificazioneService.cs non lo usa
- **Impatto**: Date calcolate senza considerare OraInizio 08:00 e OraFine 17:00
- **Root Cause**: CalcolaDataFinePrevista usa solo `OreLavorativeGiornaliere` (8h) ma non normalizza orari
- **Fix Priorità**: MEDIA - Estendere PianificazioneService per usare orari calendario

#### **P7 - Articoli senza dati produttivi**
- **Evidenza**: 6 articoli in Gantt con TempoCiclo=0 o NumeroFigure=0
- **Impatto**: Durata = solo setup (90 min) senza produzione → sottostima tempi
- **Root Cause**: Catalogo articoli incompleto
- **Fix Priorità**: BASSA - Data quality + warning visuale in Gantt

#### **P8 - Nessuna validazione overlap server**
- **Evidenza**: Nessun check in AggiornaPosizione
- **Impatto**: Possibili commesse sovrapposte in DB (anche se Gantt le ricalcola)
- **Root Cause**: Business logic mancante
- **Fix Priorità**: MEDIA - Aggiungere validazione prima di SaveChanges()

---

### 🟢 **NON PROBLEMATICHE (falsi positivi dalla checklist)**

✅ **Date mancanti**: 0 commesse → NESSUN PROBLEMA  
✅ **Impostazioni produzione errate**: Valori corretti (90, 8, 5)  
✅ **Calendario mancante**: Presente e configurato (anche se non usato)  
✅ **Timezone**: DateTime2 in DB, calcoli in locale → OK per ora  
✅ **Transazioni**: SaveChanges() singolo per macchina → OK con lock ottimistico EF

---

## 📋 PIANO DI AZIONE PRIORITIZZATO

### **FASE IMMEDIATE (Pre-Fix Codice)**

#### **FIX-0: Pulizia Dati OrdineSequenza** 🔴 URGENTE
**Azione**: Eseguire SQL per ricalcolare OrdineSequenza mancanti
```sql
-- Ricalcola OrdineSequenza per tutte le macchine con commesse a seq=0
WITH NumeriRiga AS (
    SELECT 
        Id,
        NumeroMacchina,
        ROW_NUMBER() OVER (
            PARTITION BY NumeroMacchina 
            ORDER BY DataInizioPrevisione, Codice
        ) as NuovaSequenza
    FROM Commesse
    WHERE NumeroMacchina IS NOT NULL
)
UPDATE c
SET c.OrdineSequenza = nr.NuovaSequenza
FROM Commesse c
INNER JOIN NumeriRiga nr ON c.Id = nr.Id
WHERE c.OrdineSequenza = 0 OR c.OrdineSequenza IS NULL
```
**Test**: Verifica con query duplicati → dovrebbe ritornare 0 righe

---

### **STEP 1: Frontend Drag&Drop Robusto** 🔴 PRIORITÀ 1
**File**: `wwwroot/js/gantt/gantt-macchine.js`  
**Modifiche**:
1. Sostituire evento `changed` con `onMove(item, callback)` (callback-based)
2. Implementare rollback con `callback(null)` se POST fallisce
3. Aggiornare dataset via `itemsData.update()` invece di `location.reload()`
4. Aggiungere debounce 300ms per evitare POST multipli

**Rischio**: BASSO - Solo refactor JS isolato  
**Test**: Drag item → POST → success → aggiorna vista SENZA reload

---

### **STEP 2: API Ritorna Stato Aggiornato** 🔴 PRIORITÀ 1
**File**: `Controllers/PianificazioneController.cs`  
**Modifiche**:
1. Cambiare `AggiornaPosizione` da `NoContent()` a `Ok(commesseAggiornate)`
2. Ritornare array con commesse della macchina target + origine (se cambio macchina)
3. Include: Id, Codice, NumeroMacchina, OrdineSequenza, DataInizio, DataFine

**Rischio**: BASSO - Solo cambio return type  
**Test**: POST /aggiorna-posizione → response 200 con JSON array

---

### **STEP 3: Mapping GroupId Robusto** 🟡 PRIORITÀ 2
**File**: `wwwroot/js/gantt/gantt-macchine.js` + `GanttMacchine.razor`  
**Modifiche**:
1. Groups.id = NumeroMacchina (int o string "1", "2"...) invece di "M01"
2. Items.group = NumeroMacchina direttamente (no mapping Map())
3. Blazor passa `numeroMacchina` invece di generare codici

**Rischio**: MEDIO - Cambia contratto Blazor→JS  
**Test**: Verifica groups=[1,2,3...] invece di ["M01","M02"...]

---

### **STEP 4: Parametrizzare Accodamento** 🟡 PRIORITÀ 2
**File**: `Controllers/PianificazioneController.cs`  
**Modifiche**:
1. Aggiungere parametro `bool forzaAccodamento` a `RicalcolaSequenzaMacchina`
2. Se `false`: mantieni DataInizioPrevisione utente, calcola solo DataFine
3. Se `true`: comportamento attuale (accoda dopo precedente)
4. Default = `false` per drag manuale, `true` per ricalcoli automatici

**Rischio**: MEDIO - Cambia business logic  
**Test**: Drag a ore 14:00 → DataInizio rimane 14:00 (no accodamento forzato)

---

### **STEP 5: Calendario Orari** 🟢 PRIORITÀ 3
**File**: `Services/PianificazioneService.cs`  
**Modifiche**:
1. Metodo `NormalizzaDataInizioSuOrarioLavoro(DateTime data, CalendarioLavoro calendario)`
2. Snap a OraInizio (08:00) se fuori orario
3. Usare in `CalcolaDataFinePrevista` per rispettare 08:00-17:00

**Rischio**: BASSO - Estensione funzionalità  
**Test**: Calcolo durata 10h → distribuito su 2 giorni (8h + 2h)

---

## 🧪 TEST DI VALIDAZIONE

### **Test Set 1: OrdineSequenza**
1. Query duplicati → 0 righe
2. Tutte commesse con sequenza 1..N (no gap, no duplicati)
3. Ordine coerente con DataInizioPrevisione

### **Test Set 2: Drag & Drop**
1. Drag item ore 10:00 → rimane 10:00 (no accodamento forzato)
2. Drag fallito → rollback visuale, nessun reload
3. Drag success → update dataset, zoom/scroll preservati

### **Test Set 3: API Response**
1. POST /aggiorna-posizione → 200 OK + JSON array
2. Array contiene tutti items della macchina con nuove date/sequenze
3. Frontend consuma JSON e aggiorna Gantt senza reload

---

## 📊 METRICHE SUCCESSO

| Metrica | Valore Attuale | Target Post-Fix |
|---------|----------------|-----------------|
| OrdineSequenza duplicati | 9 | 0 |
| Reload dopo drag | 100% | 0% |
| Tempo risposta drag | ~2s (reload) | <300ms (update) |
| Zoom preservato | No | Sì |
| Accodamento forzato | Sempre | Configurabile |
| Calendario orari usato | No | Sì |
| Validazione overlap | No | Sì (opzionale) |

---

## ✅ CHECKLIST PRE-FIX

- [x] Query diagnostiche eseguite
- [x] Problematiche confermate e prioritizzate
- [ ] FIX-0: SQL OrdineSequenza eseguito e testato
- [ ] STEP 1: Frontend callback-based implementato
- [ ] STEP 2: API ritorna JSON implementato
- [ ] STEP 3: Mapping groupId refactored
- [ ] STEP 4: Accodamento parametrizzato
- [ ] STEP 5: Calendario orari integrato

---

**PROSSIMA AZIONE**: Eseguire FIX-0 (SQL OrdineSequenza) e procedere con STEP 1
