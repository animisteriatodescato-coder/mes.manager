# 📁 Cartella `storico/` - Documentazione Storica

> **Scopo**: Raccogliere fix, lezioni apprese e problemi risolti mantenendo tracciabilità temporale.

---

## 📋 Contenuto

Questa cartella contiene:

1. **Lezioni Deploy** - [DEPLOY-LESSONS-LEARNED.md](DEPLOY-LESSONS-LEARNED.md)
   - Problemi critici emersi in produzione
   - Root cause e soluzioni
   - Checklist prevenzione futura

2. **Fix Specifici** - `FIX-[DESCRIZIONE]-[DATA].md`
   - [FIX-GANTT-STATI-COLORI-20260211.md](FIX-GANTT-STATI-COLORI-20260211.md) - Colori e stati automatici Gantt
   - [FIX-GANTT-ACCODAMENTO-20260120.md](FIX-GANTT-ACCODAMENTO-20260120.md) - Commesse sovrapposte
   - [FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md](FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md) - Tabella festivi

3. **Backup Documentazione** - `[NOME]-v[X.Y]-BACKUP.md`
   - [BIBBIA-AI-MESMANAGER-v2.2-BACKUP.md](BIBBIA-AI-MESMANAGER-v2.2-BACKUP.md) - Versione precedente BIBBIA

4. **Analisi Tecniche**
   - [SQL-SERVER-ANALYSIS.md](SQL-SERVER-ANALYSIS.md) - Analisi connessione SQL Server
   - [DIAGNOSTIC_REPORT.md](DIAGNOSTIC_REPORT.md) - Diagnostiche Gantt
   - [verifica-stati-macchine.md](verifica-stati-macchine.md) - Verifica configurazione

---

## 🏗️ Convenzioni Naming

### Fix Specifici
```
FIX-[AREA]-[PROBLEMA]-[YYYYMMDD].md
```

**Esempi**:
- `FIX-GANTT-STATI-COLORI-20260211.md`
- `FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md`
- `FIX-PREFERENZE-GRID-RESET-20260211.md`

### Lezioni Deploy
```
DEPLOY-LESSONS-LEARNED.md (unico file, problemi aggregati)
DEPLOY-LESSONS-[ANNO].md (se supera 1000 righe, split annuale)
```

### Backup Documenti
```
[NOME-ORIGINALE]-v[X.Y]-BACKUP.md
```

---

## 📊 Quando Creare Nuovo File

### Crea `FIX-*.md` quando:
✅ Bug complesso risolto (>50 righe analisi)
✅ Codice modificato in 3+ file
✅ Problema ricorrente o critico
✅ Lezione importante per team

### Aggiungi a `DEPLOY-LESSONS-LEARNED.md` quando:
✅ Problema emerso DOPO deploy produzione
✅ Impatto su utenti finali
✅ Richiede aggiornamento checklist deploy
✅ Errore prevenibile in futuro

### NON creare file separato per:
❌ Fix minori (<20 righe codice)
❌ Typo o refactoring semplice
❌ Modifiche documentazione (usa git commit message)

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
---

## 🔍 Come Cercare

### Problema Deploy Produzione
1. Cerca in [DEPLOY-LESSONS-LEARNED.md](DEPLOY-LESSONS-LEARNED.md) (indice problemi)
2. Se non trovato, cerca `FIX-*-[DATA-CIRCA].md`

### Bug Specifico Area
```powershell
# Cerca tutti i fix Gantt
Get-ChildItem FIX-GANTT-*.md

# Cerca fix per data
Get-ChildItem FIX-*-202602*.md  # Febbraio 2026
```

### Versione Precedente Documento
```powershell
Get-ChildItem *-BACKUP.md
```

---

## 🎯 Template Fix Standard

Quando crei un nuovo `FIX-*.md`, usa questa struttura:

```markdown
# Fix [Area] - [Problema]

**Data**: [gg/mm/aaaa]  
**Versione**: v[x.yy]  
**Gravità**: 🔴 CRITICO / 🟡 MEDIO / 🟢 BASSO  

---

## 📋 Problema

[Descrizione sintomo]

## 🔍 Root Cause

[Analisi tecnica causa]

## ✅ Soluzione

### File Modificati
- file1.cs
- file2.js

### Codice
```csharp
[Codice rilevante]
```

## 🧪 Testing

[Come è stato validato]

## 📚 Lezioni Apprese

[Cosa fare/evitare in futuro]
```

---

## 🧹 Pulizia Periodica

**Ogni 6 mesi**:
- [ ] Archiviare fix molto vecchi (>1 anno) in `archive/[ANNO]/`
- [ ] Consolidare lezioni simili in DEPLOY-LESSONS
- [ ] Rimuovere backup >2 versioni precedenti

**Ogni anno**:
- [ ] Se DEPLOY-LESSONS > 1000 righe → Split per anno
- [ ] Review fix storici: aggiornare o archiviare

---

## 📚 Riferimenti

- [BIBBIA-AI-MESMANAGER.md](../BIBBIA-AI-MESMANAGER.md) - Regole crescita documentale
- [08-CHANGELOG.md](../08-CHANGELOG.md) - Storico versioni pubblicate

---

**Versione**: 2.0  
**Data Creazione**: 11 Febbraio 2026  
**Ultimo Aggiornamento**: 11 Febbraio 2026
