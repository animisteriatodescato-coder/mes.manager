# MESManager - Manufacturing Execution System

## Descrizione
MESManager è un sistema MES (Manufacturing Execution System) modulare sviluppato con .NET 8, Blazor Server e MudBlazor per la gestione della produzione industriale.

## Architettura
Il progetto segue i principi della Clean Architecture ed è suddiviso in 5 progetti:

### 1. MESManager.Domain
Contiene le entità del dominio e le regole business:
- **Entità**: Macchina, Articolo, Ricetta, ParametroRicetta, Commessa, Cliente, Operatore, Manutenzione, EventoPLC, PLCRealtime, PLCStorico, ConfigurazionePLC, LogEvento
- **Enumerazioni**: StatoMacchina, StatoCommessa

### 2. MESManager.Application
Contiene i DTO e le interfacce dei servizi applicativi:
- **DTOs**: ArticoloDto, MacchinaDto, CommessaDto, RicettaDto, ParametroRicettaDto, ClienteDto
- **Interfacce**: IArticoloAppService, IMacchinaAppService, ICommessaAppService, IRicettaAppService, IClienteAppService

### 3. MESManager.Infrastructure
Implementazione della persistenza dati e servizi:
- **DbContext**: MesManagerDbContext con Entity Framework Core
- **Servizi**: Implementazione dei servizi applicativi
- **Migrations**: Database SQL Server

### 4. MESManager.Web
Applicazione Blazor Server con interfaccia utente:
- **Framework UI**: MudBlazor 8.15.0
- **Autenticazione**: ASP.NET Core Identity
- **Real-time**: SignalR Hub
- **Temi**: Supporto Dark/Light Mode

### 5. MESManager.Worker
Servizio Windows per elaborazioni background:
- **Ciclo**: Aggiornamenti ogni 4 secondi
- **Simulatore PLC**: Generazione eventi di test

## Struttura Pagine

### Produzione
- `/produzione/dashboard` - Dashboard produzione in tempo reale
- `/produzione/plc-realtime` - Dati PLC in tempo reale
- `/produzione/plc-storico` - Storico dati PLC con filtri data
- `/produzione/incollaggio` - Gestione processo incollaggio

### Programma
- `/programma/gantt-macchine` - Diagramma Gantt pianificazione macchine
- `/programma/commesse-aperte` - Elenco commesse aperte
- `/programma/programma-macchine` - Programmazione macchine

### Cataloghi
- `/cataloghi/commesse` - Anagrafica commesse
- `/cataloghi/articoli` - Anagrafica articoli
- `/cataloghi/clienti` - Anagrafica clienti
- `/cataloghi/ricette` - Anagrafica ricette
- `/cataloghi/foto` - Archivio fotografico

### Manutenzioni
- `/manutenzioni/alert` - Alert manutenzioni programmate
- `/manutenzioni/catalogo` - Catalogo interventi manutenzione

### Sync
- `/sync/mago` - Sincronizzazione con gestionale Mago
- `/sync/macchine` - Sincronizzazione dati macchine
- `/sync/google` - Sincronizzazione servizi Google

### Statistiche
- `/statistiche/produzione` - Statistiche e KPI produzione
- `/statistiche/ordini` - Statistiche ordini e commesse

### Impostazioni
- `/impostazioni/calendario` - Configurazione calendario produzione
- `/impostazioni/utenti` - Gestione utenti e ruoli
- `/impostazioni/generali` - Impostazioni generali sistema

## Requisiti Tecnici
- **.NET SDK**: 8.0.416 o superiore
- **SQL Server**: 2022 Express o superiore
- **Entity Framework Core**: 8.0.11
- **MudBlazor**: 8.15.0

## Configurazione Database
Connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Esecuzione

### Web Application
```powershell
cd MESManager.Web
dotnet run
```
L'applicazione sarà disponibile su: http://localhost:5156

### Worker Service
```powershell
cd MESManager.Worker
dotnet run
```

### Installazione come Servizio Windows
```powershell
cd MESManager.Worker
dotnet publish -c Release
sc create "MESManager Worker" binPath="C:\Path\To\MESManager.Worker.exe"
sc start "MESManager Worker"
```

## Eseguibili senza "dotnet run" (Windows)
- Per generare eseguibili Windows (.exe) senza usare `dotnet run`, usa lo script di publish incluso nella soluzione.

### Build veloce
- PowerShell (dalla cartella della soluzione):

```powershell
./publish-win.ps1
```

- Output per ogni progetto in: `publish/win-x64/Release/<NomeProgetto>/`
  - Esempi:
    - `publish/win-x64/Release/MESManager.Web/MESManager.Web.exe`
    - `publish/win-x64/Release/MESManager.Worker/MESManager.Worker.exe`
    - `publish/win-x64/Release/MESManager.PlcSync/MESManager.PlcSync.exe`

### Pubblicare un singolo progetto
```powershell
./publish-win.ps1 -Projects 'MESManager.Web/MESManager.Web.csproj'
```

Nota: `PlcMagoSync` è escluso di default perché richiede il progetto `PlcShared` non presente. Quando disponibile, puoi pubblicarlo esplicitamente:
```powershell
./publish-win.ps1 -Projects 'PlcMagoSync/PlcMagoSync.csproj'
```
Allo stesso modo, `PlcDashboard` non è incluso di default. Se necessario e presente nel workspace, puoi pubblicarlo così:
```powershell
./publish-win.ps1 -Projects 'PlcDashboard/PlcDashboard.csproj'
```

### Eseguire gli .exe
- Web (Kestrel):
  - Facoltativo: impostare porta/URL
    ```powershell
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    ```
  - Avvio:
    ```powershell
    ./publish/win-x64/Release/MESManager.Web/MESManager.Web.exe
    ```
- Worker/Console/WinForms:
  - Avvio diretto dell'eseguibile nella rispettiva cartella `publish/...`.

### Avvio rapido (doppioclick)
- Web su porta 5156:
  - [MESManager/start-web-5156.cmd](MESManager/start-web-5156.cmd)
- Worker:
  - [MESManager/start-worker.cmd](MESManager/start-worker.cmd)

### Note
- Lo script pubblica per `win-x64`, self-contained e single-file per evitare dipendenza dal runtime installato.
- Per performance aggiuntive puoi usare `-ReadyToRun`:
  ```powershell
  ./publish-win.ps1 -ReadyToRun
  ```
- Il trimming è disabilitato per sicurezza (riflessione, ASP.NET). Abilitalo solo dopo verifica per singoli progetti aggiungendo `/p:PublishTrimmed=true` dove appropriato.
- I file `appsettings*.json` e la `wwwroot` vengono copiati in output tramite `dotnet publish`.

## Migrazioni Database

### Creare una nuova migrazione
```powershell
dotnet ef migrations add NomeMigrazione --project MESManager.Infrastructure --startup-project MESManager.Web
```

### Applicare migrazioni
```powershell
dotnet ef database update --project MESManager.Infrastructure --startup-project MESManager.Web
```

## Ruoli Utente
- **Admin**: Accesso completo al sistema
- **Produzione**: Gestione produzione e macchine
- **Ufficio**: Gestione commesse e pianificazione
- **Manutenzione**: Gestione manutenzioni
- **Visualizzazione**: Solo lettura

## Funzionalità Real-time
Il sistema utilizza SignalR per aggiornamenti in tempo reale:
- Stato macchine
- Dati PLC
- Alert e notifiche
- Dashboard produzione

Hub disponibile su: `/hubs/realtime`

## Menu Navigazione
Il sistema include:
- **Menu laterale**: Navigazione gerarchica con categorie espandibili
- **Menu superiore**: Icone rapide per categorie principali
- **Sottomenu dinamico**: Barra secondaria che cambia in base alla categoria selezionata

## Sviluppo Futuro
- [ ] Implementazione grafici statistiche
- [ ] Integrazione API REST per sistemi esterni
- [ ] Report avanzati con export Excel/PDF
- [ ] Notifiche push
- [ ] Mobile app

## Autore
Sviluppato con .NET 8 e Blazor Server

## Licenza
Proprietario
