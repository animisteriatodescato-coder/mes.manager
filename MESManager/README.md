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
- `/produzione/gantt-macchine` - Diagramma Gantt macchine
- `/produzione/mes-stato` - Stato MES realtime con card macchine
- `/produzione/plc-realtime` - Dati PLC in tempo reale
- `/produzione/plc-storico` - Storico dati PLC con filtri data
- `/produzione/incollaggio` - Gestione processo incollaggio

### Programma
- `/programma/commesse-aperte` - Elenco commesse aperte
- `/programma/programma-macchine` - Programmazione macchine
- `/programma/stampa` - Stampa/PDF programma produzione

### Cataloghi
- `/cataloghi/commesse` - Anagrafica commesse
- `/cataloghi/articoli` - Anagrafica articoli
- `/cataloghi/clienti` - Anagrafica clienti
- `/cataloghi/ricette` - Anagrafica ricette
- `/cataloghi/foto` - Archivio fotografico

### Manutenzioni
- `/manutenzioni/alert` - Alert manutenzioni programmate
- `/manutenzioni/catalogo` - Catalogo interventi manutenzione

### Tabelle
- `/tabelle/vernici` - Anagrafica vernici
- `/tabelle/sabbie` - Anagrafica sabbie
- `/tabelle/imballi` - Anagrafica imballi
- `/tabelle/operatori` - Anagrafica operatori
- `/tabelle/colle` - Anagrafica colle

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
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
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
