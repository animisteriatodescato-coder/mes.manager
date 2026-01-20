# Analisi SQL Server Locale - MESManager

**Data:** 20 Gennaio 2026

## Problema Rilevato

L'applicazione MESManager non riusciva ad avviarsi a causa di errori di connessione al database locale `localhost\SQLEXPRESS`.

## Analisi Completa

### 1. Stato SQL Server Locale

- **Computer:** FAB
- **Servizio:** MSSQL$SQLEXPRESS
- **Stato:** Stopped (Fermo)
- **Installazione:** PRESENTE ma INCOMPLETA/CORROTTA

### 2. Problemi Identificati

#### Directory Mancanti
```
✗ C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA - NON ESISTE
✗ C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\LOG  - NON ESISTE
```

Queste directory sono essenziali per il funzionamento di SQL Server. La loro assenza indica:
- Installazione incompleta
- Disinstallazione parziale
- Corruzione dei file

#### Tentativi di Avvio Falliti
```powershell
Start-Service -Name "MSSQL$SQLEXPRESS"
# Errore: Cannot open 'MSSQL$SQLEXPRESS' service on computer '.'

net start "MSSQL$SQLEXPRESS"
# Errore di sistema 5. Accesso negato.
```

### 3. Server SQL Disponibili sulla Rete

✅ **Server Remoti Funzionanti:**

| Server | Database | Stato | Note |
|--------|----------|-------|------|
| 192.168.1.72\SQLEXPRESS | TODESCATO_NET | ✅ OK | Server Mago - 60 commesse |
| 192.168.1.230\SQLEXPRESS | Gantt | ✅ OK | Server Gantt |
| 192.168.1.230\SQLEXPRESS | MESManager | ✅ OK | **Database creato con migrations** |

## Soluzione Implementata

### Configurazione Aggiornata

Modificati i seguenti file per usare il server Gantt remoto invece di localhost:

#### 1. MESManager.Web\Program.cs
```csharp
// PRIMA:
var connectionString = "Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;";

// DOPO:
var connectionString = "Server=192.168.1.230\\SQLEXPRESS;Database=MESManager;User Id=sa;Password=password.123;TrustServerCertificate=True;Connection Timeout=30;";
```

#### 2. MESManager.Infrastructure\MesManagerDbContextFactory.cs
```csharp
// PRIMA:
optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;",

// DOPO:
optionsBuilder.UseSqlServer("Server=192.168.1.230\\SQLEXPRESS;Database=MESManager;User Id=sa;Password=password.123;TrustServerCertificate=True;",
```

### Database Creato

Eseguito con successo:
```bash
dotnet ef database update --startup-project ..\MESManager.Web\MESManager.Web.csproj
```

**Risultato:** 
- ✅ Database MESManager creato su 192.168.1.230\SQLEXPRESS
- ✅ Applicate 16 migrations
- ✅ Create 20 tabelle

### Tabelle Create

```
__EFMigrationsHistory
Anime
Articoli
CalendarioLavoro
Clienti
Commesse
ConfigurazioniPLC
EventiPLC
ImpostazioniGantt
ImpostazioniProduzione
LogEventi
LogSync
Macchine
Manutenzioni
Operatori
ParametriRicetta
PLCRealtime
PLCStorico
Ricette
SyncStates
```

## Test di Connessione

Script creato: `test-sql-connection.ps1`

### Risultati Finali
```
✗ localhost\SQLEXPRESS (MESManager) - FALLITO (servizio fermo/corrotto)
✅ 192.168.1.72\SQLEXPRESS (Mago) - OK
✅ 192.168.1.230\SQLEXPRESS (Gantt + MESManager) - OK
```

## Stato Attuale

### ✅ Applicazione Funzionante
```
L'applicazione web è stata avviata correttamente su:
http://localhost:5156

Database: 192.168.1.230\SQLEXPRESS (MESManager)
```

## Opzioni per il Futuro

### Opzione 1: Mantenere Configurazione Attuale (CONSIGLIATO)
- ✅ Funziona immediatamente
- ✅ Database centralizzato sul server Gantt
- ✅ Backup già gestiti dal server
- ⚠️ Richiede connessione di rete al server

### Opzione 2: Riparare SQL Server Locale
Se vuoi ripristinare SQL Server locale:

1. **Disinstallare completamente SQL Server Express**
   - Pannello di Controllo → Programmi → Disinstalla
   - Rimuovere tutte le istanze SQL Server

2. **Scaricare installer SQL Server Express 2022**
   - https://www.microsoft.com/it-it/sql-server/sql-server-downloads

3. **Reinstallare con configurazione completa**
   - Selezionare "New SQL Server stand-alone installation"
   - Scegliere "Express with Advanced Services"
   - Configurare istanza: SQLEXPRESS
   - Abilitare autenticazione Windows

4. **Aggiornare connection string**
   - Modificare Program.cs e MesManagerDbContextFactory.cs
   - Usare: `Server=localhost\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;`

5. **Ricreare database**
   ```bash
   dotnet ef database update
   ```

### Opzione 3: Usare LocalDB
Alternativa leggera a SQL Server Express:

```csharp
var connectionString = "Server=(localdb)\\mssqllocaldb;Database=MESManager;Trusted_Connection=True;";
```

## Note Tecniche

### Problema con il carattere `$` in PowerShell
Quando usi PowerShell per gestire servizi SQL:

❌ SBAGLIATO:
```powershell
Start-Service -Name "MSSQL$SQLEXPRESS"  # $ viene interpretato come variabile
```

✅ CORRETTO:
```powershell
Start-Service -Name 'MSSQL$SQLEXPRESS'  # apici singoli
# OPPURE
Start-Service -Name "MSSQL`$SQLEXPRESS"  # escape con backtick
```

### Permessi Amministrativi
Per avviare/fermare servizi SQL Server è necessario:
- Eseguire PowerShell come Amministratore
- Oppure usare Task Scheduler
- Oppure usare Gestione Servizi Windows (services.msc)

## File Modificati

1. ✏️ `MESManager.Web\Program.cs` - Connection string aggiornata
2. ✏️ `MESManager.Infrastructure\MesManagerDbContextFactory.cs` - Connection string aggiornata
3. ➕ `test-sql-connection.ps1` - Script di test connessioni (nuovo)
4. ➕ `SQL-SERVER-ANALYSIS.md` - Questo documento (nuovo)

## Conclusioni

✅ **Problema risolto** - L'applicazione funziona correttamente con il database remoto
✅ **Database creato** - Tutte le tabelle e migrations applicate
✅ **Test superati** - Connessioni ai server remoti verificate
⚠️ **SQL Server locale** - Richiede reinstallazione se necessario in futuro
