# Guida Commerciale & Script Demo — MESManager (Su Misura)

## Posizionamento e Target
- **Target**: PMI manifatturiere che vogliono un gestionale/MES costruito su misura, con logiche dirette e 0 passaggi inutili.
- **Dolori tipici**: prodotti generici adattati che rallentano gli operatori; troppi click; dati frammentati; nessuna vista realtime; pianificazioni manuali.
- **Valore**: progettazione su misura per velocità, semplicità e precisione; coerenza dati via integrazione ERP; una sola fonte di verità.

## Domande di Discovery (Qualifica su Misura)
- Flussi critici: quali operazioni ripetitive fanno perdere più tempo oggi? (target click e secondi)
- Dati minimi: qual è l'input essenziale per chiudere un'attività senza passaggi superflui?
- Pianificazione: come decidete sequenze/vincoli? Quali sono le eccezioni reali?
- ERP: oggetti e processi da sincronizzare (Mago) e cosa resta locale al MES?
- IT/OT: rete PLC, limiti slot, policy sicurezza, PC operatore (mouse/tastiera/touch)?
- KPI: priorità su velocità di inserimento, puntualità piani, scarti, tempi ciclo.

## Script Demo (30–40 minuti)
1. **Introduzione (2 min)**: obiettivi demo e outcome atteso.
2. **Login & Ruoli (2 min)**: ruoli essenziali, viste pulite per evitare distrazioni.
3. **Velocità operativa (5 min)**: inserimento record con scorciatoie/tasti rapidi, default intelligenti, auto‑compilazioni.
4. **Dashboard Produzione (4 min)**: SignalR realtime, alert e stato macchine.
5. **PLC Realtime & Storico (5 min)**: letture live, filtri rapidi, gestione errori.
6. **Pianificazione Gantt (8 min)**: focus su vincoli reali, creazione/modifica in pochi click.
7. **Cataloghi (4 min)**: articoli/ricette/foto con percorsi mappati e ricerca istantanea.
8. **Manutenzioni (3 min)**: alert e interventi programmati.
9. **Sync ERP (4 min)**: coerenza dati con Mago, cosa entra/esce.
10. **KPI & Export (2 min)**: tempi per operazione, puntualità piani, export Excel.
11. **Q&A e Next Steps (2–5 min)**.

## Benefici — Messaggi Chiave (Su Misura)
- 0 passaggi inutili: solo ciò che serve a chi lavora.
- Inserimento dati rapidissimo: scorciatoie, pre‑compilazione, validazioni contestuali.
- Pianificazione veloce: vincoli veri, interfacce pulite, meno errori.
- Coerenza dati: integrazione ERP; una sola fonte di verità.
- Stack moderno: .NET 8 e Blazor per evolvere rapidamente.

## Gestione Obiezioni
- "Già abbiamo l’ERP": l’ERP governa il ciclo ordine-fattura; il MES governa il ciclo produzione‑realtime (complementari).
- "PLC eterogenei": supporto S7 oggi; approccio adapter per estendere ad altri protocolli.
- "Sicurezza": segreti cifrati, ruoli, hardening e segmentazione rete; best practice documentate.
- "Vendor lock‑in": architettura pulita, API, estendibilità; dati su SQL standard.

## Offerta e Modelli di Prezzo (suggerimenti)
- **Licenza**: canone annuale per modulo/utenze o per macchina.
- **Setup**: fee una tantum per installazione, onboarding e mappatura PLC.
- **Personalizzazioni**: giornate a tariffa consultiva.
- **Supporto**: piani Bronze/Silver/Gold (SLA orari, reperibilità, hotfix).

## Percorso d’Acquisto (Proposta su Misura → PoC → Rollout)
1. **Discovery + Fit**: 1–2 workshop (processi, macchine, ERP).
2. **PoC su Misura (2–4 settimane)**: 1–2 macchine; prova velocità inserimento e click‑path target.
3. **MVP (6–10 settimane)**: moduli core in produzione pilota.
4. **Rollout**: estensione a reparti/linee, KPI avanzati, hardening.

## Piano di Implementazione (fasi e deliverable)
- Analisi e disegno integrazioni (schema dati, gate ERP, mappa PLC).
- Analisi UX su ruoli e click‑path; wireframe con scorciatoie e default intelligenti.
- Hardening infrastruttura e rete OT.
- Parametrizzazione moduli e migrazione dati iniziale.
- Test integrati + UAT + formazione.
- Go‑live assistito + hypercare.

## Onboarding & Formazione
- Sessioni per ruoli (Produzione/Ufficio/Manutenzione/Admin).
- Manuali rapidi e video brevi; supporto in orario concordato.

## Metriche di Successo (KPI)
- Secondi per operazione critica e numero di click per task.
- Riduzione tempi attrezzaggio/fermo, puntualità piani, tasso scarti, lead time.

—
Materiale per prospect; dettagli tecnici in: [docs/Scheda-Tecnica-MESManager.md](Scheda-Tecnica-MESManager.md).
