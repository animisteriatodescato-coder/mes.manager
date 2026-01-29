# Istruzioni Deploy MESManager su Server di Produzione

## Configurazione Attuale

- **Server Locale (Sviluppo)**: `localhost\SQLEXPRESS01` - Database: `MESManager`
- **Server Produzione**: `192.168.1.230` - Database: `MESManager_Prod`
- **Utente SQL**: `FAB` / Password: `password.123`
- **Utenti App**: IRENE, FABIO, GIULIA (gestibili da /impostazioni/utenti)

## Sistema Multi-Utente

L'applicazione supporta 3+ utenti che:
- Condividono gli stessi dati (commesse, articoli, macchine, etc.)
- Hanno preferenze individuali (impostazioni griglia, ordinamenti, filtri salvati)
- Selezionano il proprio profilo da un dropdown nella navbar

### Entità Coinvolte

1. **UtentiApp**: Tabella utenti app (non autenticazione, solo selezione profilo)
   - Campi: Id, Nome (unique), Attivo, Ordine, DataCreazione, UltimaModifica
   
2. **PreferenzeUtente**: Preferenze individuali per utente
   - Campi: Id, UtenteAppId (FK), Chiave, ValoreJson, timestamps

## Passaggi per il Deploy

### 1. Migrare il Database

Eseguire lo script PowerShell per creare il backup:

```powershell
.\migrate-database-to-production.ps1
```

Lo script:
- Crea un backup del database locale
- Genera uno script SQL per il restore sul server

### 2. Restore sul Server

1. Copiare il file `.bak` sul server `192.168.1.230` in `C:\Temp\`
2. Copiare lo script SQL generato `restore-on-server_*.sql`
3. Eseguire con SSMS o sqlcmd:

```cmd
sqlcmd -S 192.168.1.230 -U FAB -P password.123 -i restore-on-server_*.sql
```

### 3. Pubblicare l'Applicazione

```powershell
# Pubblica per Windows
dotnet publish MESManager/MESManager.Web/MESManager.Web.csproj -c Release -o ./publish

# Copia su server
Copy-Item -Path ./publish/* -Destination \\192.168.1.230\c$\MESManager\ -Recurse
```

### 4. Configurare IIS/Kestrel

#### Opzione A: IIS

1. Installare .NET 8 Hosting Bundle
2. Creare sito in IIS puntando a `C:\MESManager`
3. Configurare Application Pool con "No Managed Code"

#### Opzione B: Kestrel come Servizio

```powershell
# Creare servizio Windows
sc create MESManager binPath="c:\MESManager\MESManager.Web.exe"
sc start MESManager
```

### 5. Variabili d'Ambiente

Impostare l'ambiente di produzione:

```powershell
# Variabile d'ambiente per ASP.NET Core
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
```

Questo farà usare `appsettings.Production.json` con la connection string del server.

## File di Configurazione

- `appsettings.json`: Configurazione base
- `appsettings.Development.json`: Configurazione sviluppo (localhost)
- `appsettings.Production.json`: Configurazione produzione (192.168.1.230)

## Troubleshooting

### Connessione DB fallita
```powershell
# Testare connessione SQL
sqlcmd -S 192.168.1.230 -U FAB -P password.123 -Q "SELECT @@VERSION"
```

### Firewall
Assicurarsi che la porta 1433 sia aperta tra il server web e il server SQL.

### Utenti non visibili
Verificare che esistano utenti attivi:
```sql
SELECT * FROM UtentiApp WHERE Attivo = 1
```

## Gestione Utenti

Gli utenti possono essere gestiti da:
- **UI**: Navigare a `/impostazioni/utenti`
- **SQL**: 
  ```sql
  INSERT INTO UtentiApp (Id, Nome, Attivo, Ordine, DataCreazione, UltimaModifica)
  VALUES (NEWID(), 'NUOVO_UTENTE', 1, 4, GETDATE(), GETDATE())
  ```
