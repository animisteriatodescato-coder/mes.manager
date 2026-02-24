# 📚 MESManager - Documentazione Essenziale

> **Filosofia**: Documentazione minima, massima efficacia. Zero ripetizioni.

---

## 🎯 Start Rapido

| Cosa devi fare? | Leggi questo |
|-----------------|--------------|
| **Deploy su server** | [01-DEPLOY.md](01-DEPLOY.md) |
| **Sviluppo locale** | [02-SVILUPPO.md](02-SVILUPPO.md) |
| **Configurare sistema** | [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md) |
| **Implementare scheduling** | [05-SCHEDULING-ENGINE.md](05-SCHEDULING-ENGINE.md) ⭐ NEW |
| **Capire architettura** | [05-ARCHITETTURA.md](05-ARCHITETTURA.md) |
| **Replicare sistema** | [06-REPLICA-SISTEMA.md](06-REPLICA-SISTEMA.md) |

---

## 📋 File Documentazione

### Operativi (per deploy e sviluppo)
1. **[01-DEPLOY.md](01-DEPLOY.md)** - Deploy completo su server (credenziali, step-by-step, errori comuni)
2. **[02-SVILUPPO.md](02-SVILUPPO.md)** - Workflow sviluppo locale, build, test
3. **[03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)** - Database, sicurezza, secrets, PLC

### Tecnici (architettura e implementazione)
4. **[04-ARCHITETTURA.md](04-ARCHITETTURA.md)** - Clean Architecture, servizi, integrazioni
5. **[05-SCHEDULING-ENGINE.md](05-SCHEDULING-ENGINE.md)** - ⭐ Algoritmi scheduling (Job Shop, FJSS, RCPSP) + pattern Odoo/OR-Tools
6. **[06-REPLICA-SISTEMA.md](06-REPLICA-SISTEMA.md)** - Setup completo nuovo ambiente
7. **[07-GANTT-ANALISI.md](07-GANTT-ANALISI.md)** - Analisi dettagliata Gantt v2.0
8. **[08-PLC-SYNC.md](08-PLC-SYNC.md)** - Sincronizzazione PLC e troubleshooting
9. **[GANTT-REFACTORING-v2.0.md](GANTT-REFACTORING-v2.0.md)** - Rifattorizzazione scheduling robusto

### Changelog e Business
10. **[09-CHANGELOG.md](09-CHANGELOG.md)** - Storico versioni e workflow AI deploy
11. **[10-BUSINESS.md](10-BUSINESS.md)** - Commerciale, demo, scheda tecnica
12. **[11-TESTING-FRAMEWORK.md](11-TESTING-FRAMEWORK.md)** - Framework unit/integration testing
13. **[12-QA-UI-TESTING.md](12-QA-UI-TESTING.md)** - Testing UI e automazione QA

### Storico e Fix
14. **[storico/](storico/)** - Fix risolti, report diagnostici, analisi tecniche
    - [FIX-GANTT-ACCODAMENTO-20260120.md](storico/FIX-GANTT-ACCODAMENTO-20260120.md)
    - [FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md](storico/FIX-TABELLA-FESTIVI-SQLQUERYRAW-20260203.md)
    - [DIAGNOSTIC_REPORT.md](storico/DIAGNOSTIC_REPORT.md)
    - [SQL-SERVER-ANALYSIS.md](storico/SQL-SERVER-ANALYSIS.md)

### AI Assistant Setup  
15. **[BIBBIA-AI-MESMANAGER.md](BIBBIA-AI-MESMANAGER.md)** - Prompt AI specifico MESManager (308 righe)
16. **[LINEE-GUIDA-DOCUMENTAZIONE.md](LINEE-GUIDA-DOCUMENTAZIONE.md)** - ⭐ Regole per mantenere documentazione organizzata e scalabile
14. **[bibbia-visualstudio-v2.txt](bibbia-visualstudio-v2.txt)** - Prompt iniziale compatto (copia-incolla)

---

## 🔥 Regole d'Oro

### Deploy
1. **Incrementa SEMPRE la versione** in `MainLayout.razor` prima del build
2. **NON copiare MAI** `appsettings.Secrets.json` o `appsettings.Database.json` sul server
3. **Ferma servizi in ordine**: PlcSync → Worker → Web
4. **Avvia servizi in ordine**: Web → Worker → PlcSync

### Sviluppo
1. **Build prima di commit**: `dotnet build MESManager.sln --nologo`
2. **Aggiorna CHANGELOG.md** ad ogni modifica funzionale
3. **Test locale** prima di pubblicare: `dotnet run --environment Development`

### Configurazione
1. **Un solo file per le connection string**: `appsettings.Database.json`
2. **IP macchine sempre nel database**, offset PLC nei file JSON
3. **Secrets cifrati** in produzione con DPAPI

---

## 🗂️ Struttura Documenti vs Vecchia Docs

| Vecchio (docs/) | Nuovo (docs/) | Motivo |
|-----------------|----------------|--------|
| DEPLOY-GUIDA-DEFINITIVA.md<br>GUIDA-DEPLOY-SICURO.md<br>GUIDA-SVILUPPO-LOCALE.md | **01-DEPLOY.md**<br>**02-SVILUPPO.md** | 3 file con 70% duplicazioni → 2 file focalizzati |
| DATABASE-CONFIG-README.md<br>SECURITY-CONFIG.md | **03-CONFIGURAZIONE.md** | 2 file sovrapposti → 1 file completo |
| *(nessuno - new)* | **05-SCHEDULING-ENGINE.md** ⭐ | NEW: Algoritmi consolidati (Odoo, OR-Tools, Dolibarr) |
| GUIDA-REPLICA-SISTEMA.md<br>SERVIZI.md | **05-ARCHITETTURA.md**<br>**06-REPLICA-SISTEMA.md** | Separati ruoli architetturali da replica pratica |
| GanttAnalysis.md | **07-GANTT-ANALISI.md** | Rinominato per coerenza numerica |
| SCHEMA-SINCRONIZZAZIONE-PLC.md | **08-PLC-SYNC.md** | Rinominato per coerenza |
| CHANGELOG.md<br>PENDING-CHANGES.md<br>WORKFLOW-PUBBLICAZIONE.md | **09-CHANGELOG.md** | Unificato tracking e workflow AI |
| Blueprint-Startup.md<br>Guida-Commerciale.md<br>Scheda-Tecnica.md | **10-BUSINESS.md** | 3 file business → 1 completo |
| storico/ (6 file) | **storico/** (6 file) | Mantenuto integralmente per tracciabilità fix |
| *(nessuno)* | **BIBBIA-AI-*.md**<br>**bibbia-visualstudio-v2.txt** | Nuovi: prompt AI assistant |
| PREFERENZE-UTENTE-*.md<br>GUIDA-RAPIDA-ESPORTAZIONE.md | *(rimossi)* | Feature consolidata, doc obsoleta |
| presentazioni/ (HTML) | *(non migrati)* | File HTML non essenziali per sviluppo |

**Risultato**: Da 18 file principali → **13 file essenziali** (9 guide + 3 AI + 1 storico) + 6 fix nello storico + NEW pattern guide

---

## 🌱 Crescita Documentale - Dove Aggiungere Cosa

**Problema da evitare**: Documentazione che cresce disordinata, duplicazioni, file giganti.

**Soluzione**: Regole chiare su DOVE mettere nuovi contenuti.

### Mappa Decisionale Rapida

```
Nuovo contenuto da documentare?
│
├─ È una REGOLA GENERALE progetto?
│  └─ → BIBBIA-AI-MESMANAGER.md (se <20 righe)
│     └─ Altrimenti → File tematico 01-10
│
├─ È un PROBLEMA emerso in DEPLOY PRODUZIONE?
│  └─ → storico/DEPLOY-LESSONS-LEARNED.md
│
├─ È un FIX specifico con codice dettagliato?
│  └─ → storico/FIX-[AREA]-[PROBLEMA]-[DATA].md
│
├─ È una PROCEDURA operativa?
│  └─ → 01-DEPLOY.md, 02-SVILUPPO.md, etc.
│
└─ È un ALGORITMO o PATTERN nuovo?
   └─ → 05-SCHEDULING-ENGINE.md
```

### Limiti Dimensioni File

| File | Max Righe | Se Superato |
|------|-----------|-------------|
| **BIBBIA-AI-MESMANAGER.md** | 350 | Split dettagli → docs/ |
| **File 01-10** | 800 | Crea sottopagine o split |
| **storico/FIX-*.md** | 500 | OK (documenti puntuali) |
| **storico/DEPLOY-LESSONS** | 1000 | Split annuale |

### Riferimento Completo

Vedi **[BIBBIA-AI-MESMANAGER.md](BIBBIA-AI-MESMANAGER.md#-regole-di-crescita-documentale)** per regole dettagliate.

---

## 📞 Supporto

**Credenziali Server**:
```
Server: 192.168.1.230
Admin: Administrator / A123456!
DB: FAB / password.123
```

**Porta Web**: 5156

**Per problemi**: Vedi sezione troubleshooting nel file specifico (01-DEPLOY.md, 03-CONFIGURAZIONE.md, etc.)
