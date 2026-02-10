# Storico Fix e Analisi Tecniche

Questa cartella contiene la documentazione dettagliata di tutti i problemi risolti e le analisi tecniche del progetto MESManager.

## 📁 Contenuto

### Fix Implementati
| File | Data | Problema | Gravità | Status |
|------|------|----------|---------|--------|
| [FIX-GANTT-ACCODAMENTO-20260120.md](FIX-GANTT-ACCODAMENTO-20260120.md) | 20/01/2026 | Commesse sovrapposte invece di accodate | 🔴 CRITICO | ✅ RISOLTO |
| [FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md](FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md) | 03/02/2026 | Tabella Festivi mancante + bug SqlQueryRaw | 🔴 CRITICO | ✅ RISOLTO |

### Report Diagnostici
| File | Data | Scopo |
|------|------|-------|
| [DIAGNOSTIC_REPORT.md](DIAGNOSTIC_REPORT.md) | 20/01/2026 | Analisi problematiche Gantt Macchine |
| [SQL-SERVER-ANALYSIS.md](SQL-SERVER-ANALYSIS.md) | 20/01/2026 | Analisi connessione SQL Server locale |
| [verifica-stati-macchine.md](verifica-stati-macchine.md) | - | Verifica configurazione stati macchine |

### Sommari
| File | Descrizione |
|------|-------------|
| [FIX_IMPLEMENTED.md](FIX_IMPLEMENTED.md) | Elenco completo di tutti i fix implementati |

## 🎯 Come Usare Questo Storico

### Per Risolvere un Problema Simile
1. Cerca nei fix esistenti problemi simili
2. Leggi **Root Cause** per capire la causa
3. Adatta la **Soluzione** al tuo caso
4. Documenta il nuovo fix seguendo il template

### Template Fix (da aggiungere al prossimo fix)
```markdown
# Fix [Nome Funzionalità] - [Breve Descrizione]

**Data**: [gg/mm/aaaa]
**Versione**: v[x.yy]
**Gravità**: 🔴 CRITICO / 🟡 MEDIO / 🟢 BASSO
**Status**: ✅ RISOLTO / ⚠️ PARZIALE / ❌ NON RISOLTO

---

## 📋 Problema Riscontrato

### Sintomo Utente
- **Pagina**: [URL o funzionalità]
- **Errore visualizzato**: 
  ```
  [Messaggio errore]
  ```
- **Impatto**: [Descrizione impatto su utenti]

---

## 🔍 Analisi Root Cause

### Investigazione
1. [Passo 1]
2. [Passo 2]
3. [Causa identificata]

### Cause Identificate
- **Causa principale**: [Descrizione]
- **Cause secondarie**: [Se presenti]

---

## ✅ Soluzione Implementata

### File Modificati
1. **[File1.cs]**
   ```csharp
   [Codice rilevante]
   ```
   
2. **[File2.cs]**
   ```csharp
   [Codice rilevante]
   ```

### Migration (se presente)
- **Nome**: `[timestamp]_[Nome]`
- **Applicata**: ✅ Sì / ❌ No

### Script SQL (se presente)
```sql
[Script SQL manuale]
```

---

## 🧪 Test Effettuati

- [x] Test funzionale pagina [X]
- [x] Test regressione funzionalità [Y]
- [x] Verifica database dev
- [x] Verifica database prod

---

## 📚 Lezioni Apprese

1. **[Lezione 1]**: [Descrizione]
2. **[Lezione 2]**: [Descrizione]

---

## 🔗 File Correlati
- [../01-DEPLOY.md](../01-DEPLOY.md)
- [../03-CONFIGURAZIONE.md](../03-CONFIGURAZIONE.md)

---

**Autore**: [Nome]  
**Reviewer**: [Nome se presente]
```

## ⚠️ Regole Storico

1. **Mai Eliminare** - Questi file sono permanenti
2. **Solo Aggiungere** - Nuovi fix si aggiungono, vecchi restano
3. **Linkare** - Citare fix correlati
4. **Dettagliare** - Più dettagli = più valore futuro
5. **Datare** - Sempre indicare data precisa

## 🔍 Pattern Comuni (da questi fix)

### Pattern 1: Migration Senza Tabella Fisica
**Sintomo**: Migration applicata ma tabella mancante  
**Causa**: Rollback parziale o modifica manuale DB  
**Soluzione**: Script SQL manuale + sincronizzazione migration  
**Vedi**: [FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md](FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md)

### Pattern 2: SqlQueryRaw con SELECT Scalare
**Sintomo**: Errore "Il nome di colonna 'Value' non è valido"  
**Causa**: EF Core richiede alias per colonne calcolate  
**Soluzione**: Usare alias o `ExecuteSqlRaw` per DDL  
**Vedi**: [FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md](FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md#-soluzione-implementata)

### Pattern 3: Ordinamento con Valore Default
**Sintomo**: OrdineSequenza duplicati a 0  
**Causa**: Campo non popolato al salvataggio  
**Soluzione**: Metodo RicalcolaSequenzaMacchina + endpoint ricalcolo  
**Vedi**: [FIX-GANTT-ACCODAMENTO-20260120.md](FIX-GANTT-ACCODAMENTO-20260120.md#3-nuovi-endpoint-api)

## 📊 Statistiche

- **Fix Critici Risolti**: 2
- **Report Diagnostici**: 3
- **Periodo**: Gen 2026 - Feb 2026
- **Pattern Identificati**: 3

---

**Ultimo Aggiornamento**: 4 Febbraio 2026
