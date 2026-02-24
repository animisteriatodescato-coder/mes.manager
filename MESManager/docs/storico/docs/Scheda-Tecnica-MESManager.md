# Scheda Tecnica Prodotto — MESManager (Manufacturing Execution System)

## Executive Summary
- **Cos’è**: MES modulare per aziende manifatturiere, real-time e integrato con ERP e PLC.
- **Per chi**: PMI e imprese discrete/processo che vogliono visibilità end‑to‑end e controllo operativo.
- **Punti chiave**: dati PLC in real-time, pianificazione a Gantt, sincronizzazione con ERP Mago, ruoli e sicurezza enterprise, architettura Clean Architecture su .NET 8.

## Moduli e Funzionalità
- **Produzione**: Dashboard live, `PLC realtime`, `PLC storico` con filtri, gestione processo di incollaggio.
- **Programmazione**: `Gantt macchine`, `Commesse aperte`, `Programma macchine` con vista di carico.
- **Cataloghi**: Anagrafica `commesse`, `articoli`, `clienti`, `ricette`, archivio `foto`/allegati.
- **Manutenzioni**: Alert manutenzioni programmate, catalogo interventi.
- **Sync**: Integrazione `ERP Mago`, sincronizzazione dati `macchine`, integrazione servizi `Google` selezionati.
- **Statistiche**: KPI produzione e ordini/commesse (espandibile a OEE, scarti, tempi ciclo).
- **Impostazioni**: Calendario produzione, gestione `utenti e ruoli`, impostazioni generali.

## Architettura
- **Clean Architecture**: `Domain`, `Application`, `Infrastructure`, `Web` (Blazor Server), `Worker`, `PlcSync`.
- **Realtime**: `SignalR` per aggiornamenti istantanei di dashboard e stato macchine.
- **UI**: `MudBlazor` + componenti `Syncfusion` per griglie e interazioni ricche.
- **Integrazioni PLC**: Servizio `MESManager.PlcSync` con driver Siemens S7 (Sharp7), polling, riconnessione, scrittura stato realtime/storico su DB.
- **Integrazione ERP**: Servizio `MESManager.Worker`/`MESManager.Sync` per sincronizzazioni batch con Mago (ordini/commesse/anagrafiche).
- **Database**: SQL Server con `Entity Framework Core`, resilienza (retry on failure) e DbContext factory per thread safety.
- **Sicurezza config**: Ordine di caricamento impostazioni con `appsettings.Secrets.encrypted` (DPAPI), fallback a `appsettings.Secrets.json` e legacy `appsettings.Database.json`.

## Tecnologia e Requisiti
- **Piattaforma**: .NET 8 (Blazor Server), Windows Server consigliato.
- **DB**: SQL Server 2022 Express o superiore.
- **Librerie**: EF Core 8, MudBlazor 8.15, Syncfusion Blazor, SignalR, EPPlus (export Excel), Sharp7 (PLC S7).
- **Runtime/Build**: Eseguibili self‑contained (publish), script di deploy e start rapidi.

## Integrazioni e Dati di Campo (OT)
- **PLC Siemens S7**: configurazione ibrida con IP macchina da database e offset PLC da file JSON dedicati.
- **Gestione connessioni**: graceful shutdown per liberare slot PLC; timeout di shutdown esteso; riconnessioni automatiche.
- **Sicurezza OT**: separazione tra layer Web/IT e PlcSync/OT; raccomandata segmentazione di rete e whitelisting.

## Sicurezza e Identity
- **Identity**: ASP.NET Core Identity con password policy rinforzata, lockout e token providers.
- **Autorizzazioni**: Ruoli predefiniti (Admin, Produzione, Ufficio, Manutenzione, Visualizzazione) estendibili.
- **Protezione segreti**: Preferenza per `appsettings.Secrets.encrypted` (DPAPI, per-macchina/utente), fallback JSON in chiaro in sviluppo.
- **Best practice**: principle of least privilege su SQL, TLS/HTTPS in produzione, esclusione file sensibili dal deploy.

## Scalabilità, Affidabilità e Manutenibilità
- **Separazione servizi**: Web, Worker e PlcSync indipendenti per scalare e riavviare senza impatti trasversali.
- **Resilienza DB**: retry su errori transitori; DB factory per background tasks.
- **DevOps**: script di publish Windows x64 single-file, deploy differenziale (solo wwwroot per update JS/CSS), guida deploy con checklist ed errori comuni.
- **Test E2E**: Playwright per verifiche automatiche su UI critiche e assenza errori console.

## Deployment e Operatività
- **Eseguibili**: build/publish self‑contained; avvio via Task Scheduler o servizi Windows.
- **Ordine stop/start**: Stop `PlcSync` → `Worker` → `Web`; Start `Web` → `Worker` → `PlcSync`.
- **Percorsi**: file statici sotto `C:\MESManager\wwwroot\` (ROOT); esclusione tassativa di `appsettings.Secrets*.json` nei copy.
- **Ambienti**: Development (dettagli errori) e Production (HSTS, HTTPS, antiforgery, static files).

## Personalizzazioni ed Estendibilità (Su Misura)
- **Approccio**: analisi tecnica preliminare, mappatura click‑path e input minimi, co‑design con i reparti.
- **UX**: scorciatoie tastiera, default intelligenti, interfacce senza elementi superflui.
- **Componenti UI**: temi light/dark, toolbar contestuale, griglie avanzate.
- **API**: controller REST già mappati; facile estendere servizi applicativi (Application) e repository (Infrastructure).
- **Domini**: entità chiave già modellate (Macchina, Commessa, Articolo, Ricetta, ParametroRicetta, Cliente, Operatore, Manutenzione, EventoPLC, ecc.).

## Conformità e Privacy
- **GDPR‑ready**: autenticazione, ruoli, audit tramite log applicativi; configurazioni esterne per credenziali; separazione dati statici/allegati.
- **Pratiche consigliate**: criptazione connessioni SQL, rotazione credenziali, segmentazione rete PLC, hardening server.

## Roadmap (estratto)
- Grafici statistiche avanzate e OEE
- API REST per integrazioni esterne
- Reportistica Excel/PDF
- Notifiche push e Mobile app

## Requisiti Minimi
- **Server**: CPU 4 core, 8–16 GB RAM, Windows Server 2019+.
- **DB**: SQL Server 2022 Express+; storage SSD; backup pianificati.
- **Rete**: raggiungibilità PLC (porte S7), latenza LAN bassa.

## Valore per il Cliente
- Visibilità in tempo reale, riduzione tempi di fermo, pianificazione più accurata, integrazione ERP per coerenza dati, base moderna per evoluzioni future.

—
Documento di prodotto per prospect; dettagli operativi in: [docs/DEPLOY-GUIDA-DEFINITIVA.md](DEPLOY-GUIDA-DEFINITIVA.md), [docs/SERVIZI.md](SERVIZI.md), [docs/SECURITY-CONFIG.md](SECURITY-CONFIG.md).
