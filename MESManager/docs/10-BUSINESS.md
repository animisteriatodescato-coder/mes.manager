# 09 - Business e Commerciale

> **Scopo**: Positioning, demo, scheda tecnica e blueprint startup

---

## 🎯 Posizionamento Prodotto

### Target
**PMI manifatturiere** che vogliono un MES costruito **su misura**, con:
- Logiche dirette e 0 passaggi inutili
- Velocità operativa (pochi click, tasti rapidi)
- Real-time PLC e coerenza dati ERP

### Pain Points Clienti
- ❌ Prodotti generici adattati che rallentano
- ❌ Troppi click per operazioni semplici
- ❌ Dati frammentati tra sistemi
- ❌ Nessuna vista real-time produzione
- ❌ Pianificazioni manuali su Excel

### Value Proposition
✅ Progettazione su misura per velocità e semplicità  
✅ Integrazione ERP (Mago) - una sola fonte di verità  
✅ Real-time PLC per monitoraggio produzione  
✅ Stack moderno (.NET 8, Blazor) per evoluzione rapida  

---

## 📊 Scheda Tecnica Prodotto

### Moduli Funzionali

| Modulo | Funzionalità | Utenti Target |
|--------|--------------|---------------|
| **Produzione** | Dashboard live, PLC realtime, PLC storico, processo incollaggio | Operatori, Capi turno |
| **Programmazione** | Gantt macchine, Commesse aperte, Programma macchine | Ufficio tecnico |
| **Cataloghi** | Commesse, Articoli, Clienti, Ricette, Foto/allegati | Ufficio, Admin |
| **Manutenzioni** | Alert programmate, catalogo interventi | Manutenzione |
| **Sync** | ERP Mago, Macchine, Google services | Admin |
| **Statistiche** | KPI produzione, ordini/commesse, OEE (espandibile) | Management |
| **Impostazioni** | Calendario produzione, Utenti e ruoli, Configurazioni | Admin |

---

### Architettura Tecnica

**Stack**:
- **Backend**: .NET 8, ASP.NET Core
- **Frontend**: Blazor Server
- **Database**: SQL Server 2022+
- **Real-time**: SignalR
- **UI Components**: MudBlazor 8.15, Syncfusion Gantt
- **PLC Integration**: Sharp7 (Siemens S7)
- **ERP Integration**: SQL direct (Mago)

**Architettura**:
- Clean Architecture (Domain, Application, Infrastructure, Web)
- Repository Pattern
- Dependency Injection
- Entity Framework Core 8

**Deployment**:
- Windows Server (self-contained exe)
- 3 servizi: Web, Worker (Mago sync), PlcSync
- Task Scheduler o Windows Services

---

### Requisiti Sistema

**Minimo**:
- CPU: Intel Core i5
- RAM: 8 GB
- Storage: 50 GB SSD
- Rete: Gigabit Ethernet
- OS: Windows 10/11 o Server 2019+

**Software**:
- .NET 8 Runtime
- SQL Server Express 2019+
- IIS o standalone (Kestrel)

---

### Licenze e Costi

| Componente | Licenza | Costo |
|------------|---------|-------|
| .NET 8 | MIT | Gratuito |
| MudBlazor | MIT | Gratuito |
| EPPlus | Polyform Noncommercial | Gratuito (non commercial) |
| Syncfusion | Community License | Gratuito < $1M ricavi |
| Sharp7 | MIT | Gratuito |

---

## 🎬 Script Demo (30-40 min)

### 1. Introduzione (2 min)
- Obiettivi demo
- Outcome atteso
- Contesto cliente

### 2. Login & Ruoli (2 min)
- Ruoli essenziali: Admin, Produzione, Ufficio, Manutenzione
- Viste pulite per evitare distrazioni
- **Key message**: "Zero informazioni inutili"

### 3. Velocità Operativa (5 min)
- Inserimento record con scorciatoie
- Default intelligenti
- Auto-compilazioni
- **Key message**: "Meno click = più produttività"

### 4. Dashboard Produzione (4 min)
- SignalR realtime
- Alert e stato macchine
- **Key message**: "Tutto in un colpo d'occhio"

### 5. PLC Realtime & Storico (5 min)
- Letture live da PLC Siemens
- Filtri rapidi
- Gestione errori
- **Key message**: "Dati real-time dalla fabbrica"

### 6. Pianificazione Gantt (8 min)
- Vincoli reali (non teorici)
- Drag & drop macchine/tempo
- Creazione/modifica rapida
- **Key message**: "Pianifica in secondi, non ore"

### 7. Cataloghi (4 min)
- Articoli/Ricette/Foto
- Percorsi mappati
- Ricerca istantanea
- **Key message**: "Tutto catalogato e accessibile"

### 8. Manutenzioni (3 min)
- Alert programmati
- Interventi pianificati
- **Key message**: "Manutenzione predittiva, non reattiva"

### 9. Sync ERP (4 min)
- Coerenza dati con Mago
- Cosa entra/esce
- **Key message**: "Una sola fonte di verità"

### 10. KPI & Export (2 min)
- Tempi per operazione
- Puntualità piani
- Export Excel
- **Key message**: "Dati per decisioni rapide"

### 11. Q&A e Next Steps (2-5 min)

---

## 💬 Domande di Discovery

Usa queste domande per qualificare il cliente **su misura**:

### Flussi Critici
- Quali operazioni ripetitive fanno perdere più tempo oggi?
- Quanti click servono mediamente per chiudere un'attività?
- Quanto tempo serve per pianificare una settimana di produzione?

### Dati Minimi
- Qual è l'input essenziale per avviare una produzione?
- Quali campi sono davvero necessari (vs "nice to have")?
- Cosa è duplicato/ridondante oggi?

### Pianificazione
- Come decidete sequenze/priorità di produzione?
- Quali sono le eccezioni reali (vs teoriche)?
- Come gestite ritardi/urgenze?

### ERP/IT
- Quali oggetti/processi sincronizzare con ERP?
- Cosa deve restare locale al MES?
- Quali report/export vi servono davvero?

### OT (Operational Technology)
- Rete PLC: marca, modello, limiti?
- Policy sicurezza IT/OT?
- PC operatore: mouse/tastiera/touch?

### KPI
- Priorità: velocità inserimento, puntualità piani, scarti, tempi ciclo?
- Come misurate successo oggi?
- Quali metriche vi mancano?

---

## 🛡️ Gestione Obiezioni

### "Abbiamo già l'ERP"
**Risposta**: L'ERP governa ordine-fattura, il MES governa produzione-realtime. Sono complementari, non sostitutivi.

### "PLC eterogenei"
**Risposta**: Supporto S7 nativo. Approccio adapter per estendere ad altri protocolli (Modbus, OPC UA, etc.).

### "Sicurezza OT"
**Risposta**: Segreti cifrati, ruoli, hardening, segmentazione rete. Best practice documentate.

### "Vendor lock-in"
**Risposta**: Architettura pulita, API standard, dati su SQL. Facile estendere/integrare.

### "Costi nascosti"
**Risposta**: Trasparenza completa: licenza + setup + personalizzazioni + supporto. Nessun costo sorpresa.

---

## 💰 Modelli di Prezzo (Suggerimenti)

### Licenza Annuale
- **Per utente**: €X/utente/anno
- **Per macchina**: €Y/macchina/anno
- **Flat**: €Z/anno (illimitato)

### Setup Una Tantum
- Installazione base: €A
- Onboarding utenti: €B/giorno
- Mappatura PLC: €C/macchina
- Integrazione ERP: €D

### Personalizzazioni
- Giornate consulenza: €E/giorno
- Feature su misura: preventivo ad hoc

### Supporto
- **Bronze**: email 24h, €F/anno
- **Silver**: email 8h + telefono, €G/anno
- **Gold**: email 4h + telefono + reperibilità, €H/anno

---

## 🚀 Percorso d'Acquisto

### 1. Discovery + Fit (1-2 workshop)
- Processi attuali
- Pain points
- Macchine e ERP
- Obiettivi quantitativi

### 2. PoC su Misura (2-4 settimane)
- 1-2 macchine
- Proof velocità inserimento
- Click-path target
- Metriche baseline vs migliorato

### 3. MVP (6-10 settimane)
- Moduli core
- Produzione pilota (1 reparto/linea)
- Formazione iniziale

### 4. Rollout Completo
- Estensione reparti/linee
- KPI avanzati
- Hardening produzione
- Supporto continuativo

---

## 🏗️ Piano Implementazione

### Fase 1: Analisi e Design (2-3 settimane)
- Schema dati e integrazioni
- Mappa PLC (IP, offset, data blocks)
- Analisi UX ruoli e click-path
- Wireframe con scorciatoie

### Fase 2: Setup Infrastruttura (1-2 settimane)
- Server e database
- Rete OT (segmentazione)
- Firewall e hardening
- Backup e DR

### Fase 3: Sviluppo Core (4-6 settimane)
- Moduli base (Produzione, Programmazione)
- Integrazione PLC
- Sync ERP
- UI/UX ottimizzata

### Fase 4: Test e UAT (2 settimane)
- Test integrati
- User Acceptance Testing
- Formazione power users

### Fase 5: Go-Live (1 settimana)
- Migrazione dati
- Avvio assistito
- Hypercare (supporto intensivo)

---

## 🎓 Onboarding & Formazione

### Sessioni per Ruolo
- **Produzione**: Dashboard, inserimento dati, PLC realtime (2h)
- **Ufficio**: Pianificazione, Gantt, cataloghi (3h)
- **Manutenzione**: Alert, interventi (1h)
- **Admin**: Configurazioni, utenti, sicurezza (2h)

### Materiali
- Manuali rapidi PDF
- Video brevi (3-5 min per funzione)
- Cheat sheet scorciatoie

### Supporto Post-Go-Live
- Hypercare 2 settimane (orario esteso)
- Email support 30 giorni
- Sessioni di follow-up (30, 60, 90 giorni)

---

## 📈 Metriche di Successo

### Velocità Operativa
- **Click per task**: Riduzione 50%+
- **Secondi per operazione**: Riduzione 60%+
- **Tempo pianificazione**: Riduzione 70%+

### Produzione
- **Lead time**: -20%
- **Puntualità consegne**: +30%
- **OEE (Overall Equipment Effectiveness)**: +15%

### Qualità
- **Tasso scarti**: -10%
- **Errori inserimento dati**: -80%

### ROI
- **Payback**: 12-18 mesi
- **Risparmio annuo**: €X (tempo operatori + efficienza)

---

## 🏢 Blueprint Startup - Gestionali Su Misura

### Visione
**Mission**: Portare dati real-time e processi digitali nelle PMI industriali con soluzioni su misura, rapide e sostenibili.

**Verticali**: Manifattura discreta, lavorazioni conto terzi, packaging, gomma/plastica, alimentare.

---

### Organizzazione (Ruoli Chiave)

| Ruolo | Responsabilità |
|-------|----------------|
| **CEO/COO** | Strategia, operations, P&L |
| **CTO** | Architettura, qualità tecnica, sicurezza |
| **Sales Lead** | Pipeline, partnership, offerte |
| **Project Manager** | Delivery, tempi/costi, stakeholder |
| **Solution Architect (.NET/OT)** | Design end-to-end, integrazioni |
| **Full-stack .NET (Blazor)** | Feature UI/backend |
| **Integration Engineer (ERP)** | Connettori Mago/REST/DB |
| **PLC/OT Engineer** | Mappatura segnali, reti industriali |
| **QA/Testing (E2E)** | Piani test, Playwright, automazione |
| **DevOps/SecOps** | CI/CD, hardening, backup/monitoring |
| **UX/UI** | Design usabilità, design system |
| **Customer Success** | Onboarding, SLA, helpdesk |

---

### Hiring Plan (12 mesi)

**T1-T4 (Mesi 1-4)**: CTO, 2 dev full-stack, PM  
**T5-T8 (Mesi 5-8)**: OT engineer, QA, DevOps  
**T9-T12 (Mesi 9-12)**: Sales dedicato, UX/UI  

---

### Processi e Metodologie

**Prevendita**: Discovery, mappa processi, stima T-shirt, SoW  
**Delivery**: Agile (Scrum/Kanban), sprint 2 settimane  
**Quality**: Code review, SAST/DAST, test unit/E2E  
**Security**: Gestione segreti, least privilege, segmentazione OT  

---

### Tooling

| Categoria | Tool |
|-----------|------|
| **Repo/CI** | GitHub/Azure DevOps |
| **Tracking** | Azure Boards/Jira |
| **QA** | Playwright |
| **Observability** | AppInsights/Seq |
| **ITSM** | Freshdesk/Zendesk |
| **CRM** | HubSpot/Pipedrive |

---

### Go-To-Market

**Lead gen**:
- Casi studio clienti
- Webinar tecnici
- Referral system
- Partnership system integrator

**PoC**: Pacchetto fisso 2-4 settimane su 1-2 linee/macchine

---

### Piano 90 Giorni

**0-30 giorni**: Pacchetto demo, sito, 3 lead attivi  
**31-60 giorni**: Primo cliente in produzione (MVP)  
**61-90 giorni**: 2-3 PoC paralleli, processi stabili  

---

### Rischi e Mitigazioni

| Rischio | Mitigazione |
|---------|-------------|
| Dipendenza singolo vertical | Diversificare casi d'uso |
| Sicurezza OT | Audit periodici, segmentazione |
| Scalabilità team | Standardizzazione, acceleratori |

---

## 🆘 Supporto

Per deploy: [01-DEPLOY.md](01-DEPLOY.md)  
Per architettura: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)  
Per configurazione: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)
