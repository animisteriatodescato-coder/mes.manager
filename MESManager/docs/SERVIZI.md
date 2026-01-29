# Descrizione dei Servizi MESManager

Documento sintetico che descrive i tre servizi principali e le loro responsabilità.

---

## MESManager.Web
- Scopo: interfaccia web (Blazor Server) per gli utenti finali; fornisce UI, autenticazione, pagine e hub SignalR per aggiornamenti realtime.
- Responsabilità: rendering pagine, gestione sessioni e permessi, endpoint HTTP, notifiche realtime ai client.
- Dipendenze: `MESManagerDb` (letture/scritture applicative), librerie UI (MudBlazor, Syncfusion), file `appsettings.*` e `appsettings.Secrets.json` (cred.).
- Impatto riavvio: disconnette gli utenti; SignalR tenta riconnessione automatica. Non perde dati DB a condizione che `Worker`/`PlcSync` siano fermati/avviati nell'ordine corretto.
- Raccomandazione deploy: riavviare dopo `Worker` e `PlcSync` (ordine stop: `PlcSync` → `Worker` → `Web`; start: `Web` → `Worker` → `PlcSync`).

---

## MESManager.Worker (Sync Mago)
- Scopo: sincronizzare dati con ERP Mago e applicare aggiornamenti al DB applicativo.
- Responsabilità: polling/elaborazione batch, code processing, aggiornamenti su `MESManagerDb` e integrazione con `MagoDb`.
- Dipendenze: `MagoDb`, `MESManagerDb`, librerie di sincronizzazione (`MESManager.Sync`), script/cron interni.
- Impatto riavvio: se fermato durante una sincronizzazione può causare ritardi o transazioni parziali; normalmente riconcilia al riavvio.
- Raccomandazione deploy: fermare dopo `PlcSync` e prima del `Web` stop; avviare dopo `Web` e prima di `PlcSync` start per rispettare la sequenza logica.

---

## MESManager.PlcSync
- Scopo: comunicazione diretta con PLC Siemens S7 per lettura/scrittura I/O e aggiornamento stato realtime e storico.
- Responsabilità: mantenimento connessioni S7 (Sharp7), polling variabile per macchine, scrittura dati realtime nel DB, gestione errori/ri-connessione.
- Dipendenze: accesso di rete ai PLC, file `PlcSync/Configuration/machines/*` (IP e mapping variabili), `Sharp7.dll` o driver simili.
- Rischi di shutdown brusco: connessioni S7 "appese" che consumano slot di connessione del PLC (limite tipico ~32), causando malfunzionamenti fino allo scadere delle connessioni.
- Raccomandazione deploy: sempre fermare PRIMA degli altri servizi, eseguire graceful shutdown che chiuda socket e logghi lo stato; riavviare per ultimo.

---

## Note comuni e best-practice
- MAI sovrascrivere `appsettings.Secrets.json` o `appsettings.Database.json` sul server durante il deploy.
- Seguire l'ordine di stop/start indicato sopra per evitare connessioni orfane, incoerenze e interruzioni di servizio.
- Verificare log (`Web/logs`, `PlcSync/output.log`, `Worker/logs`) dopo il riavvio.
- Se `PlcSync` è terminato bruscamente e i PLC risultano non rispondere, attendere 2–3 minuti o riavviare i PLC se necessario.

---

File generato automaticamente per riferimento rapido nei processi di deploy.
