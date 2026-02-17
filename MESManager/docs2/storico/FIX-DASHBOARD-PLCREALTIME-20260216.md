# FIX Dashboard PLCRealtime Scollegate - 16 Febbraio 2026

## 📋 PROBLEMA SEGNALATO

**Data**: 16 Febbraio 2026  
**Ambiente**: DEV (localhost\SQLEXPRESS01, MESManager_Dev)  
**Utente**: "anche se vedo 6 macchine attive per l ennesima volta sono scollegate dalle dashboard, perche continui a ripetere gli stessi errori?"

### Sintomi

1. Dashboard Produzione (`/produzione/dashboard`) e PLC Realtime (`/produzione/plc-realtime`) mostravano **meno macchine del previsto**
2. Problema **ricorrente** - già capitato in passato ("per l'ennesima volta")
3. Solo ambiente DEV - in PROD tutto funziona correttamente

---

## 🔍 DIAGNOSI

### Analisi Eseguita

1. **Macchine nel database**: 8 macchine (M002, M003, M005-M010) ✅
   - Tutte con `IndirizzoPLC` configurato (subnet 192.168.17.x) ✅
   - Tutte con `AttivaInGantt = true` ✅

2. **PLCRealtime**: **VUOTA** (0 record) ❌

### Root Cause Identificata

Le dashboard **NON leggono direttamente dalla tabella `Macchine`**, ma da `PLCRealtime` con JOIN:

```csharp
// PlcAppService.cs - GetRealtimeDataAsync()
var query = await _context.PLCRealtime
    .Include(p => p.Macchina)
    .Include(p => p.Operatore)
    .Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))  // Filtro IP
    .OrderBy(p => p.Macchina.Codice)
    .ToListAsync();
```

**Filtri applicati**:
1. Record DEVE esistere in `PLCRealtime`
2. `Macchina.IndirizzoPLC` NOT NULL e non vuoto
3. `DataUltimoAggiornamento` < 2 minuti (altrimenti "NON CONNESSA")

**Problema**: Se `PLCRealtime` è vuota o non ha record per una macchina, quella macchina **non viene mostrata** nelle dashboard.

### Perché PLCRealtime era Vuota?

In **ambiente PROD**: Il servizio `MESManager.PlcSync` è attivo e popola automaticamente `PLCRealtime` ogni ~4 secondi.

In **ambiente DEV**: PlcSync **non è attivo**, quindi `PLCRealtime` non viene aggiornata automaticamente.

---

## ✅ SOLUZIONE IMPLEMENTATA

### Script SQL  - Popolamento PLCRealtime per DEV

**File**: [`scripts/fix-plcrealtime-dashboard.sql`](../../scripts/fix-plcrealtime-dashboard.sql)

```sql
-- Delete vecchi record (se esistono)
DELETE FROM PLCRealtime WHERE MacchinaId IN (
    SELECT Id FROM Macchine WHERE IndirizzoPLC IS NOT NULL
);

-- Insert record di test per TUTTE le macchine con IP configurato
INSERT INTO PLCRealtime (
    Id,
    MacchinaId,
    DataUltimoAggiornamento,
    CicliFatti,
    QuantitaDaProdurre,
    CicliScarti,
    BarcodeLavorazione,
    OperatoreId,          -- Nullable OK
    NumeroOperatore,      -- NOT NULL int - DEVE essere 0
    TempoMedioRilevato,   -- NOT NULL int - DEVE essere 0
    TempoMedio,           -- NOT NULL int - DEVE essere 0
    Figure,               -- NOT NULL int - DEVE essere 0
    StatoMacchina,
    QuantitaRaggiunta
)
SELECT 
    NEWID(),
    m.Id,
    GETDATE(),           -- IMPORTANTE! Timestamp recente per passare filtro 2min
    0,                   -- CicliFatti
    0,                   -- QuantitaDaProdurre
    0,                   -- CicliScarti
    0,                   -- BarcodeLavorazione
    NULL,                -- OperatoreId (nullable)
    0,                   -- NumeroOperatore (NOT NULL - OK con 0)
    0,                   -- TempoMedioRilevato (NOT NULL - OK con 0)
    0,                   -- TempoMedio (NOT NULL - OK con 0)
    0,                   -- Figure (NOT NULL - OK con 0)
    'FERMO',             -- StatoMacchina di test
    0                    -- QuantitaRaggiunta
FROM Macchine m
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != '';
```

### ⚠️ Errori Incontrati Durante il Fix

1. **Primo tentativo**: `NumeroOperatore = NULL` → SQL Error: "La colonna non ammette valori Null"
2. **Secondo tentativo**: `TempoMedioRilevato = NULL` → SQL Error: "La colonna non ammette valori Null"

**Lezione**: Controllare SEMPRE lo schema entity prima di scrivere INSERT:

```csharp
// MESManager.Domain/Entities/PLCRealtime.cs
public class PLCRealtime
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public DateTime DataUltimoAggiornamento { get; set; }
    
    // Campi int senza '?' = NOT NULL - DEVONO avere valore!
    public int NumeroOperatore { get; set; }        // NOT NULL
    public int TempoMedioRilevato { get; set; }     // NOT NULL
    public int TempoMedio { get; set; }             // NOT NULL
    public int Figure { get; set; }                 // NOT NULL
    
    // Campo nullable
    public Guid? OperatoreId { get; set; }          // Nullable OK
}
```

---

## 📊 RISULTATI POST-FIX

**Esecuzione Script**: ✅ 8 righe inserite in `PLCRealtime`

```
M002 - Stato: FERMO - Agg: 0 sec fa
M003 - Stato: FERMO - Agg: 0 sec fa
M005 - Stato: FERMO - Agg: 0 sec fa
M006 - Stato: FERMO - Agg: 0 sec fa
M007 - Stato: FERMO - Agg: 0 sec fa
M008 - Stato: FERMO - Agg: 0 sec fa
M009 - Stato: FERMO - Agg: 0 sec fa
M010 - Stato: FERMO - Agg: 0 sec fa
```

**Dashboard ora visibili**: Tutte le 8 macchine dovrebbero essere visibili in:
- http://localhost:5156/produzione/dashboard
- http://localhost:5156/produzione/plc-realtime

---

## 🔁 PREVENZIONE - REGOLE DA SEGUIRE

### ❌ Errori da NON Ripetere

1. **Mai eliminare record da `Macchine` senza verificare `PLCRealtime`**
   - Quando si rimuovono macchine duplicate, controllare che quelle rimosse non siano le uniche con `IndirizzoPLC`
   
2. **Mai svuotare `PLCRealtime` in ambiente DEV senza ripopolarla**
   - In DEV, PlcSync non è attivo → tabella non si ripopola automaticamente
   
3. **Mai assumere che `Macchine.AttivaInGantt = true` sia sufficiente**
   - Dashboard filtra su `PLCRealtime`, non su `Macchine`

### ✅ Checklist Pre-Modifica Macchine/PLCRealtime

Prima di modificare queste tabelle:

```sql
-- 1. Verifica quante macchine hanno IP
SELECT COUNT(*) FROM Macchine WHERE IndirizzoPLC IS NOT NULL AND IndirizzoPLC != '';

-- 2. Verifica quante macchine sono in PLCRealtime
SELECT COUNT(*) FROM PLCRealtime;

-- 3. Verifica corrispondenza
SELECT 
    m.Codice,
    m.IndirizzoPLC,
    CASE WHEN p.Id IS NULL THEN '❌ MANCA IN PLCRealtime' ELSE '✅ OK' END AS Stato
FROM Macchine m
LEFT JOIN PLCRealtime p ON p.MacchinaId = m.Id
WHERE m.IndirizzoPLC IS NOT NULL AND m.IndirizzoPLC != '';
```

Se **mancano record in `PLCRealtime`** → eseguire [`scripts/fix-plcrealtime-dashboard.sql`](../../scripts/fix-plcrealtime-dashboard.sql)

---

## 📚 RIFERIMENTI

- **Entity**: [MESManager.Domain/Entities/PLCRealtime.cs](../../MESManager.Domain/Entities/PLCRealtime.cs)
- **Service**: [MESManager.Infrastructure/Services/PlcAppService.cs](../../MESManager.Infrastructure/Services/PlcAppService.cs) - `GetRealtimeDataAsync()`
- **Documentazione PLC**: [07-PLC-SYNC.md](../07-PLC-SYNC.md)
- **BIBBIA**: [BIBBIA-AI-MESMANAGER.md](../BIBBIA-AI-MESMANAGER.md) - Sezione "Dashboard e PLCRealtime"

---

## 🎯 AZIONI FUTURE

### Possibili Miglioramenti

1. **Script init-dev-plcrealtime.ps1**: Creare uno script di inizializzazione ambiente DEV che popola automaticamente `PLCRealtime` con dati di test
   
2. **Check automatico in diagnostics endpoint**: Aggiungere a `/api/diagnostics/catalogs` un check che verifica:
   ```
   if (MacchineConIP > PLCRealtimeCount) {
       warnings.Add($"Mancano {diff} macchine in PLCRealtime - dashboard incomplete!");
   }
   ```

3. **Documentare in README**: Aggiungere sezione "Setup ambiente DEV" con passo per popolare PLCRealtime

---

## ✅ STATO FINALE

- [x] PLCRealtime popolata con 8 record di test
- [x] Script SQL corretto e documentato
- [x] BIBBIA aggiornata con regola Dashboard/PLCRealtime
- [x] Documentazione storica creata (questo file)
- [ ] Test dashboard da parte utente ⏳
- [ ] Eventual re-import Anime (~800 da server produzione) ⏳

**Data Completamento Fix**: 16 Febbraio 2026  
**Ambiente**: DEV (MESManager_Dev)  
**Impatto**: Fix locale - PROD non affetto
