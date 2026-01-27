# Configurazione Database Centralizzata

## File: `appsettings.Database.json`

Questo file centralizza **TUTTE** le connection string del progetto MESManager.

### Perché questo file?

Prima avevamo le connection string duplicate in 4 file diversi:
- ❌ `MESManager.Web/appsettings.json`
- ❌ `MESManager.Worker/Program.cs`
- ❌ `MESManager.PlcSync/appsettings.json`
- ❌ `MESManager.Infrastructure/MesManagerDbContextFactory.cs`

**Problema**: Ogni volta che cambiavi server SQL, dovevi modificare 4 file diversi!

**Soluzione**: Ora c'è un unico file `appsettings.Database.json` nella root del progetto.

### Come funziona

Tutti i progetti (Web, Worker, PlcSync, Infrastructure) **leggono automaticamente** questo file all'avvio.

### Come cambiare server SQL

**Modifica SOLO questo file:** `appsettings.Database.json`

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=TUO_SERVER;Database=MESManager;..."
  }
}
```

Esempi:

#### Locale (Sviluppo)
```json
"MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### Server Remoto (con autenticazione SQL)
```json
"MESManagerDb": "Server=192.168.1.230\\SQLEXPRESS01;Database=MESManager_Prod;User Id=fab;Password=fabpwd;TrustServerCertificate=True;"
```

#### Server Remoto (con autenticazione Windows)
```json
"MESManagerDb": "Server=NOME_SERVER\\SQLEXPRESS;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
```

### Connection Strings Disponibili

| Nome | Descrizione |
|------|-------------|
| `MESManagerDb` | Database principale MESManager (locale o remoto) |
| `MagoDb` | Database Mago ERP (sempre remoto: 192.168.1.72) |
| `GanttDb` | Database Gantt (sempre remoto: 192.168.1.230) |

### Importante

⚠️ **NON modificare** i file `Program.cs` dei singoli progetti - leggono automaticamente da `appsettings.Database.json`

⚠️ **NON duplicare** le connection string nei file `appsettings.json` dei progetti

✅ **Modifica SOLO** il file `appsettings.Database.json` nella root del progetto

### Test della configurazione

Dopo aver modificato `appsettings.Database.json`:

1. **Verifica la sintassi JSON**
   - Il file deve essere JSON valido
   - Attenzione ai caratteri `\` (devono essere doppi: `\\`)

2. **Test connessione**
   ```powershell
   .\test-sql-connection.ps1
   ```

3. **Riavvia i servizi**
   ```powershell
   # Ferma tutti i processi
   Get-Process -Name "dotnet" | Stop-Process -Force
   
   # Avvia il web server
   .\start-web-5156.cmd
   ```

### Struttura del file

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "<connection-string-principale>",
    "MagoDb": "<connection-string-mago>",
    "GanttDb": "<connection-string-gantt>"
  }
}
```

### Deploy in Produzione

Quando passi in produzione, **copia** il file `appsettings.Database.json` sul server e modifica solo la connection string `MESManagerDb`:

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=PROD_SERVER\\SQLEXPRESS;Database=MESManager;User Id=app_user;Password=secure_password;TrustServerCertificate=True;",
    "MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Connection Timeout=30;",
    "GanttDb": "Server=192.168.1.230\\SQLEXPRESS;Database=Gantt;User Id=fab;Password=fabpwd;TrustServerCertificate=True;"
  }
}
```

### Troubleshooting

**Errore: "Connection string 'MESManagerDb' not found"**
- Verifica che il file `appsettings.Database.json` sia presente nella root del progetto
- Controlla che il nome della connection string sia esattamente `MESManagerDb` (case-sensitive)

**Errore: "Could not find file appsettings.Database.json"**
- Il file deve stare nella cartella `C:\Dev\MESManager\` (root del progetto)
- Verifica di aver compilato il progetto almeno una volta

**La connection string non viene letta**
- Riavvia Visual Studio
- Pulisci e ricompila la solution: `dotnet clean && dotnet build`
