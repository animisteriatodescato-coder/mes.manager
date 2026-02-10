# 🧭 LEZIONI APPRESE - Testing & Debugging Framework

> **Data**: 6 Febbraio 2026 | **Versione**: v1.30  
> **Documento Obbligatorio** - Leggere PRIMA di ogni implementazione futura

---

## ⚠️ REGOLA CRITICA #1: NON Dichiarare "Funziona" Senza Prove

### Il Problema (v1.29)
```
❌ Dichiarato: "Build OK, fix implementato, applicazione funzionante"
✗ Realtà: Export ancora restituiva 0 commesse, date ancora a mezzanotte
❌ Danno: Perdita di 2-3 ore di debug
```

### La Soluzione
**Ogni dichiarazione di "funzionamento" richiede:**

1. ✅ **Test Automatizzato** (PowerShell script)
   - Verifica step-by-step con output visibile
   - Lo script continua solo se il test passa
   - Output loggato da analizzare

2. ✅ **Log Aggressivo** nel codice
   - PRIMA operazione: Log START
   - Ogni step principale: Log con dettagli
   - DOPO operazione: Log SUCCESS o ERROR
   - Nel catch: Log completo stacktrace

3. ✅ **Verifica Post-Applicazione** in DB
   - Query SELECT per verificare risultato
   - Confronto: stato-prima vs stato-dopo
   - Verifica che i dati persistono

4. ✅ **Test Manuale UI** (breve)
   - Navigazione a pagina interessata
   - Screenshot se necessario
   - Comportamento confermato

---

## 🐛 BUG PATTERN #1: OrderBy su Guid = Casualità

### Il Problema
```csharp
// ❌ GENERICO - Ma ordine COMPLETAMENTE casuale
var commesse = await _context.Commesse
    .ToListAsync()
    .OrderBy(c => c.Id)  // Id è Guid - ordine casuale!
    .ToList();
```

**Conseguenza**: Non puoi testare "prima 10 commesse" perché cambiano sempre.

### La Soluzione
```csharp
// ✅ SEMANTICO - Ha significato per l'utente
var commesse = await _context.Commesse
    .OrderBy(c => c.Codice)  // Admin conosce i codici
    .ThenBy(c => c.NumeroMacchina)
    .ToListAsync();
```

**Regola**: 
- **User-facing queries**: Ordina per campo semantico (Codice, Nome, Data)
- **Test queries**: Aggiungi ordinamento deterministico
- **Mai** usare Guid come ordinamento principale

---

## 📊 INSPECTION PATTERN: Query Before + After

### Problema: Non Sai Se L'Update Ha Funzionato

```csharp
// ❌ Non Testabile - Non verifichi risultato
var commesse = await _context.Commesse.Where(...).ToListAsync();
foreach (var c in commesse) 
{
    c.StatoProgramma = StatoProgramma.Programmata;
}
await _context.SaveChangesAsync();
// E poi? Funzionò? Quante sono state aggiornate?
```

### Soluzione: BEFORE + AFTER Inspection

```csharp
// ✅ Testabile - Verifichi il risultato

// BEFORE
var beforeStats = await _context.Commesse
    .GroupBy(c => c.StatoProgramma)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();
_logger.LogInformation("PRIMA: {@Stats}", beforeStats);

// UPDATE
var commesse = await _context.Commesse.Where(...).ToListAsync();
int updated = 0;
foreach (var c in commesse) 
{
    if (c.StatoProgramma != StatoProgramma.Programmata)
    {
        c.StatoProgramma = StatoProgramma.Programmata;
        updated++;
    }
}
await _context.SaveChangesAsync();
_logger.LogInformation("AGGIORNATE: {Updated}/{Total}", updated, commesse.Count);

// AFTER
var afterStats = await _context.Commesse
    .GroupBy(c => c.StatoProgramma)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToListAsync();
_logger.LogInformation("DOPO: {@Stats}", afterStats);

// VERIFICA DELTA
if (afterStats.Count > beforeStats.Count)
    _logger.LogInformation("✅ CAMBIO STATO VERIFICATO");
else
    _logger.LogError("❌ NESSUN CAMBIO STATO!");
```

---

## 🔗 LOGGING PATTERN: Livelli Semantici

### Livelli Corretti per MESManager

```csharp
_logger.LogDebug("Dettaglio tecnico: {Variable}");
// → Usare SOLO in loop (sopprime per default)

_logger.LogInformation("Operazione completata: {Count} righe");
// → Operazioni normali, successi, valori importanti

_logger.LogWarning("Nessun risultato trovato per {Criterio}");
// → Non è un errore, ma è anormale

_logger.LogError(ex, "❌ Operazione fallita: {Reason}");
// → Errore vero e proprio

_logger.LogCritical("DATABASE CORROTTO: {Issue}");
// → Non usare quasi mai
```

### Template da Copiare

```csharp
_logger.LogInformation("🚀 [EXPORT START] Operazione XYZ");
// ...
_logger.LogInformation("✅ [EXPORT SUCCESS] Completato in {DurationMs}ms", duration);

// Nel catch:
_logger.LogError(ex, "❌ [EXPORT ERROR] Fallito dopo {DurationMs}ms: {Message}", duration, ex.Message);
```

---

## 🧪 TEST SCRIPT PATTERN

### Struttura Obbligatoria

```powershell
#!/usr/bin/env pwsh

# 1. HEADER CHIARO
Write-Host "🧪 TEST: Nome Operazione" -ForegroundColor Green

# 2. VERIFICA PREREQUISITI
Write-Host "📋 Prerequisiti..." -ForegroundColor Yellow
# ... verifica DB, URL, etc.

# 3. ESECUZIONE
Write-Host "🚀 Esecuzione test..." -ForegroundColor Yellow

# 4. VERIFICA RISULTATO
Write-Host "✅ Verifica risultato..." -ForegroundColor Green
if ($result -eq $expected) {
    Write-Host "✅ TEST PASSATO"
} else {
    Write-Host "❌ TEST FALLITO"
    exit 1
}

# 5. RIEPILOGO
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RIEPILOGO" -ForegroundColor Green
# ...
```

**Cosa Blocca Il Test**:
```powershell
if ($error) {
    Write-Host "❌ ERRORE CRITICO: " -ForegroundColor Red
    exit 1  # ← Test fallisce qui
}
```

### Esempi Presenti

- `test-export-gantt.ps1` - Export Gantt completo
- `test-fase1-clienti.ps1` - (da creare se non esiste)
- `check-export-logs.ps1` - Diagnostica log

---

## 📋 CHECKLIST PRE-DICHIARAZIONE SUCCESS

### Prima di Dire "Funziona"

- [ ] Build completa senza errori (0 error, avvisi OK)
- [ ] Test script eseguito da riga di comando
- [ ] **Output del test visibile** (screenshot o log)
- [ ] Verifica DB: BEFORE vs AFTER
- [ ] Log applicazione: ZERO errori rilevanti
- [ ] UI: Test manuale rapido (2-3 screenshot)
- [ ] Documentazione: Aggiornato CHANGELOG.md

### Esempio Documentazione

```markdown
### ✅ v1.30 - Export Gantt FUNZIONANTE

**Test Eseguito**:
```
PS> .\test-export-gantt.ps1
[13:45] ✅ Test completati. 5 commesse esportate.
[13:45] Verifica DB: StatoProgrammata passate da 0 → 5
```

**Verifica UI**: 
- Screenshot: ProgrammaMacchine mostra 5 commesse
- Date verificate: 08:00 - 17:00 ✓
- Export button: OK ✓
```

---

## 🔍 DEBUGGING TEMPLATE

Quando qualcosa non funziona, usiamo questo ordine:

### 1. SQL Query (Verifica dati)
```sql
SELECT TOP 10 Codice, DataInizioPrevisione, StatoProgramma 
FROM Commesse 
WHERE DataInizioPrevisione IS NOT NULL
ORDER BY Codice
```

### 2. Log Applicazione
```powershell
.\check-export-logs.ps1  # Vedi logs e queries
```

### 3. Test Script
```powershell
.\test-export-gantt.ps1 -ApiUrl http://localhost:5156
```

### 4. Browser DevTools
- F12 → Network → Verifica richiesta POST
- Vedi response JSON: success=true/false?

### 5. Sorgente Codice
- Aggiungi `[LOG]` davanti a logs importanti
- Verifica if/else logic
- Usa debugger VS: Run → Start Debugging

---

## 📚 ANALISI POST-IMPLEMENTAZIONE (OBBLIGATORIO)

### Dopo Ogni Fix, Prima di Dichiarare Completato:

1. **Rileggi il CHANGELOG** 
   - Descrivi OGNI file modificato
   - Motiva PERCHÉ il bug esisteva
   - Spiega COME è stato risolto

2. **Rileggi la BIBBIA** (docs2/bibbia-*)
   - I tuoi fix respettano la architettura?
   - Hai violato qualche regola critica?
   - Documenta la lezione appresa

3. **Aggiorna BIBBIA**
   - Se hai imparato qualcosa, scrivi
   - Regola nuova? Aggiungi sezione
   - Errore buono per esempio? Metti qui

4. **Review Finale Checklist**
   ```markdown
   - [ ] CHANGELOG aggiornato
   - [ ] Docs2/ aggiornato
   - [ ] BIBBIA aggiornato
   - [ ] Test script creato/aggiornato
   - [ ] Logging aggressivo implementato
   - [ ] ZERO errori in build
   - [ ] Test manuale confermato
   ```

---

## 📌 ERRORI FREQUENTI (Trappole)

### Trappola #1: "Log Dice Success Ma DB Non Cambia"
```csharp
// ❌ BUG - Log è dentro try, ma SaveChanges fallisce silenziosamente
await _context.SaveChangesAsync(); // Eccezione non loggata!
_logger.LogInformation("✅ Success");

// ✅ FIX - Verifica esplicita
int rows = await _context.SaveChangesAsync();
_logger.LogInformation("✅ {Rows} righe modificate", rows);
if (rows == 0) _logger.LogWarning("⚠️ NESSUN CAMBIO!");
```

### Trappola #2: "Test Passa Ma UI Non Mostra"
- Verifica SignalR: Notifiche inviate?
- Verifica autorizzazioni user: Ha accesso a pagina?
- Verifica filter/where: La query esclude il risultato?

### Trappola #3: "Solo Io Vedo Il Problema"
- Svuota cache browser (Ctrl+Shift+Del)
- Riavvia app (.NET)
- Clear database logs
- Esegui test script clean

---

## 📖 RIFERIMENTI DA AGGIUNGERE ALLA BIBBIA

Quando aggiorni BIBBIA-VI MESMANAGER.md, Aggiungi:

```markdown
### 🧪 Testing & Auditing

**Regola Critica**: MAI dichiarare "funziona" senza test visibili
- Script test obbligatorio per ogni feature
- Log aggressivo con [OPERATION START/SUCCESS/ERROR]
- Verifica DB: BEFORE → UPDATE → AFTER

**Pattern Obbligatori**:
- `test-*.ps1` per automation
- `check-*-logs.ps1` per diagnostica
- Logging semantico (Debug/Info/Warning/Error)
- Inspection pattern (conteggi prima/dopo)
```

---

## 🎓 LEZIONI CRITICHE

1. **Test = Prova di Funzionamento**
   - Il test ES script è la tua prova
   - Senza test visibile, è specualzione

2. **Log = Telecamera di Debug**
   - Metti log su OGNI decisione importante
   - Livell corretto: Info=successi, Warning=anomalie, Error=fallimenti

3. **Verifica Delta**
   - Count PRIMA, count DOPO
   - Se delta=0, c'è un bug

4. **Determinismo**
   - OrderBy semantico, non random
   - Testa sempre le edge cases (count=0, count=NULL)

5. **Documentazione Contemporanea**
   - Aggiorna changelog MENTRE tratti bug
   - Non lasciare per dopo (dimentichi dettagli)

---

**Documento Obbligatorio**: Leggi prima di ogni nuova fase di sviluppo.  
**Aggiorna**: Quando scopri una lezione nuova importante.
