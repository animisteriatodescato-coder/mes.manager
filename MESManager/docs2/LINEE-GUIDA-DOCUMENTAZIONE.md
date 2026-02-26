# Linee Guida Documentazione MESManager

> **Scopo**: Regole per mantenere la documentazione organizzata, sintetica e scalabile.

---

## 🎯 Principio Fondamentale

**BIBBIA = Prompt AI Generale**  
**docs2/ = Dettagli Implementativi**

La BIBBIA deve rimanere **sotto 350 righe** e contenere solo:
- Regole generali
- Workflow obbligatori
- Indice documenti
- Riferimenti rapidi

Tutto il resto va nei documenti tematici di `docs2/`.

---

## 📂 DOVE Aggiungere Contenuto

| Tipo Contenuto | File Destinazione | Esempio |
|----------------|-------------------|---------|
| **Regola generale progetto** | BIBBIA-AI-MESMANAGER.md | "Mai mescolare dev/prod" |
| **Workflow operativo** | BIBBIA-AI-MESMANAGER.md | "Prima di ogni deploy..." |
| **Procedura deploy** | 01-DEPLOY.md | Script robocopy completo |
| **Script test specifici** | 09-TESTING-FRAMEWORK.md | Dettagli test-api.ps1 |
| **Problema deploy risolto** | storico/DEPLOY-LESSONS-LEARNED.md | "Database prod mancante dati" |
| **Fix bug specifico** | storico/FIX-[NOME]-[DATA].md | FIX-GANTT-STATI-COLORI |
| **Algoritmo scheduling** | 04-SCHEDULING-ENGINE-PATTERNS.md | Job Shop, FJSS patterns |
| **Configurazione PLC** | 07-PLC-SYNC.md | Offset, IP, connessioni |
| **Configurazione Database** | 03-CONFIGURAZIONE.md | Connection strings, allegati |
| **Test E2E** | 10-QA-UI-TESTING.md | Playwright, visual regression |
| **Linee guida docs** | LINEE-GUIDA-DOCUMENTAZIONE.md | Questo file |

---

## ✅ PERMESSO nella BIBBIA

- Identità e ruolo AI
- Stack tecnologico (linguaggi, framework)
- Ambienti (dev/prod)
- Indice file docs2/ (tabella)
- Workflow obbligatori (checklist brevi - max 10 righe)
- Regole architetturali inviolabili (principi generali)
- Metodo di risposta AI
- Filosofia progetto
- Comandi base build/run (PowerShell standard - max 15 righe)

---

## ❌ VIETATO nella BIBBIA

- Script PowerShell completi (→ 09-TESTING-FRAMEWORK.md)
- Lezioni deploy specifiche (→ storico/DEPLOY-LESSONS-LEARNED.md)
- Dettagli endpoint API (→ 09-TESTING-FRAMEWORK.md o README specifico)
- Query SQL lunghe (→ storico/ o 03-CONFIGURAZIONE.md)
- Codice C# completo (→ storico/FIX-*.md)
- Checklist deployment dettagliate (→ 01-DEPLOY.md)
- Configurazioni JSON complete (→ 03-CONFIGURAZIONE.md)
- Template documenti (→ LINEE-GUIDA-DOCUMENTAZIONE.md)
- Esempi pratici lunghi (→ documenti tematici)

---

## 📏 Limite Righe File

| File | Max Righe | Azione se Superato |
|------|-----------|-------------------|
| BIBBIA-AI-MESMANAGER.md | **350** | Split dettagli → docs2/ |
| 0X-[NOME].md | **800** | Crea sottopagine o split temi |
| storico/FIX-*.md | **500** | OK (documenti puntuali) |
| storico/DEPLOY-LESSONS-LEARNED.md | **1000** | Crea DEPLOY-LESSONS-2027.md |

---

## 🔄 Template Decisione: "Dove Metto Questo?"

```
1. È una REGOLA GENERALE applicabile a tutto il progetto?
   → SÌ: BIBBIA (se <20 righe) o LINEE-GUIDA-DOCUMENTAZIONE.md
   → NO: Vai al punto 2

2. È un PROBLEMA RISOLTO durante deploy produzione?
   → SÌ: storico/DEPLOY-LESSONS-LEARNED.md
   → NO: Vai al punto 3

3. È un FIX specifico con codice/analisi dettagliata?
   → SÌ: storico/FIX-[DESCRIZIONE]-[DATA].md
   → NO: Vai al punto 4

4. È una PROCEDURA operativa (deploy, test, sviluppo)?
   → SÌ: File numerato 01-10 pertinente
   → NO: Considera creare nuovo file tematico
```

---

## 📝 Template Aggiornamento Docs

Quando documenti una scoperta/fix:

```markdown
## [Data] - [Titolo Scoperta]

### Problema
[Cosa non funzionava]

### Causa Root
[Perché accadeva]

### Soluzione Implementata
[Cosa è stato fatto]

**File modificati:**
- file1.cs
- file2.js

### Impatto
[Conseguenze e benefici]

### Lezione Appresa
[Regola da seguire in futuro]
```

---

## 🔍 Quando Aggiornare Documentazione

Durante il lavoro, quando emerge uno dei seguenti casi:

- 🐛 Bug scoperto e risolto
- 🏗️ Problema architetturale identificato
- 🚧 Limite tecnico scoperto
- ✅ Decisione importante presa
- ⚠️ Errore commesso e corretto
- 🔧 Workaround necessario applicato
- 📏 Regola nuova da rispettare

**DEVI**:

1. **Segnalare** che la conoscenza va documentata
2. **Decidere il file corretto** (vedi tabella "DOVE Aggiungere Contenuto")
3. **Proporre**:
   - File docs2/ da aggiornare (o crearne uno nuovo in storico/)
   - Contenuto chiaro e pratico con esempi
4. **Mantenere STORICITÀ**:
   - Cosa non funzionava prima
   - Perché falliva
   - Soluzione adottata
   - Conseguenze evitate

---

## 📖 File docs2/ - Descrizione Completa

### File Principali (01-10)

| File | Scopo | Contenuto Tipico |
|------|-------|------------------|
| **01-DEPLOY.md** | Procedure deploy produzione | Script, checklist, path, ordine servizi |
| **02-SVILUPPO.md** | Workflow sviluppo locale | Setup, debug, migration, testing |
| **03-CONFIGURAZIONE.md** | Config database, secrets, PLC | Connection strings, allegati, IP macchine |
| **04-ARCHITETTURA.md** | Clean Architecture, servizi | Layer, DI, repository pattern |
| **04-SCHEDULING-ENGINE-PATTERNS.md** | Algoritmi scheduling | Job Shop, FJSS, vincoli, ottimizzazione |
| **05-REPLICA-SISTEMA.md** | Setup nuovo ambiente | Installazione completa da zero |
| **06-GANTT-ANALISI.md** | Gantt chart dettagli | Syncfusion, task, timeline, caricamento |
| **07-PLC-SYNC.md** | Sincronizzazione PLC | Sharp7, offset, stati macchina |
| **08-CHANGELOG.md** | Storico versioni | Workflow AI, deploy tracking |
| **09-TESTING-FRAMEWORK.md** | Testing e debugging | Script PowerShell, pattern test, log |
| **10-QA-UI-TESTING.md** | Test E2E e visual | Playwright, baselines, CI/CD |
| **09-BUSINESS.md** | Commerciale e demo | Pitch, prezzi, presentazioni |

### File Storico

I file in `storico/` sono documenti puntuali che descrivono:
- Fix specifici con data (`FIX-[NOME]-[DATA].md`)
- Lezioni apprese da deploy (`DEPLOY-LESSONS-LEARNED.md`)
- Analisi problemi (`DIAGNOSTIC_REPORT.md`)

Questi file possono essere più lunghi (fino a 500-1000 righe) perché documentano casi specifici completi.

---

## ✅ Checklist Manutenzione Documentazione

### Quando Modifichi la BIBBIA

- [ ] Contenuto allineato con regole (vedi "PERMESSO nella BIBBIA")
- [ ] Nessun codice completo (solo snippet <10 righe)
- [ ] Nessun esempio pratico lungo (sintetizza o sposta)
- [ ] File < 350 righe
- [ ] Riferimenti a docs2/ per dettagli
- [ ] Versione e data aggiornate a fine file

### Quando Crei/Aggiorni File docs2/

- [ ] Nome file chiaro e posizionamento corretto (numerato o storico/)
- [ ] Header con scopo del documento
- [ ] Sezioni ben strutturate con H2/H3
- [ ] Esempi pratici dove serve
- [ ] Link incrociati ad altri docs2/
- [ ] Storicità mantenuta (non riscrivere completamente, aggiungi sezioni)

---

## 🎯 Obiettivo Finale

Ogni persona (umana o AI) che legge `docs2/` deve:
1. Trovare rapidamente l'informazione cercata
2. Capire il "perché" dietro ogni decisione
3. Poter replicare setup/procedure senza aiuto
4. Imparare dagli errori passati documentati

**Motto**: "Documenta oggi, risparmia domani"

---

**Versione**: 1.0  
**Data**: 20 Febbraio 2026  
**Path**: `C:\Dev\MESManager\docs2\LINEE-GUIDA-DOCUMENTAZIONE.md`
