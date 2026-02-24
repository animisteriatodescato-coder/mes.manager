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
"MESManagerDb": "Server=NOME_SERVER\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;"
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
    "MESManagerDb": "Server=PROD_SERVER\\SQLEXPRESS01;Database=MESManager;User Id=app_user;Password=secure_password;TrustServerCertificate=True;",
    "MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Connection Timeout=30;",
    "GanttDb": "Server=192.168.1.230\\SQLEXPRESS01;Database=Gantt;User Id=fab;Password=fabpwd;TrustServerCertificate=True;"
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

---

## 🔧 Configurazione Macchine PLC

### Fonti di Verità (Architettura Ibrida)

| Dato | Fonte | Dove si modifica |
|------|-------|------------------|
| **Elenco macchine** | Database (Macchine) | Impostazioni → Gantt Macchine |
| **IP PLC** | Database (Macchine.IndirizzoPLC) | Impostazioni → Gantt Macchine |
| **Codice macchina** | Database (Macchine.Codice) | Impostazioni → Gantt Macchine |
| **Offset PLC** | File JSON | `PlcSync/Configuration/machines/*.json` |
| **Parametri connessione PLC** | File JSON | `PlcSync/Configuration/machines/*.json` |

### Come funziona

1. **PlcSync Worker** all'avvio:
   - Carica tutte le macchine dal database con i loro IP
   - Carica i file JSON per gli offset PLC
   - **Sovrascrive** l'IP del JSON con quello del database
   - Usa il risultato per connettersi ai PLC

2. **Programma Macchine** (griglia):
   - Carica l'elenco macchine dinamicamente dall'API `/api/Macchine`
   - Mostra tutte le macchine presenti nel database

### Aggiungere una Nuova Macchina (es. M011)

#### Step 1: Aggiungi nel Database
1. Vai in **Impostazioni → Gantt Macchine**
2. Click "Aggiungi Macchina"
3. Compila:
   - Codice: `M011`
   - Nome: `Macchina 11`
   - Indirizzo IP PLC: `192.168.17.XX` (l'IP reale del PLC)
   - Attiva nel Gantt: ✓

#### Step 2: Crea il file JSON per gli offset
1. Copia un file esistente, es: `macchina_010.json` → `macchina_011.json`
2. Modifica i campi:
```json
{
  "MachineId": "11111111-1111-1111-1111-000000000011",
  "Numero": 11,
  "Nome": "11",
  "PlcIp": "192.168.17.XX",  // Verrà sovrascritto dal DB
  "Enabled": true,
  // ... offset specifici della macchina
}
```

**⚠️ IMPORTANTE**: Il `MachineId` nel JSON deve corrispondere all'Id della macchina nel database!

#### Step 3: Verifica corrispondenza ID
```sql
-- Trova l'ID della macchina nel database
SELECT Id, Codice, Nome, IndirizzoPLC 
FROM Macchine 
WHERE Codice = 'M011';
```
Usa questo ID nel campo `MachineId` del file JSON.

### Modificare IP di una Macchina Esistente

**Basta modificare in Impostazioni → Gantt Macchine**.

Il PlcSync al prossimo avvio leggerà l'IP aggiornato dal database.

Non è più necessario modificare i file JSON per cambiare IP!

### Troubleshooting

**La macchina non appare nel Programma Macchine**
- Verifica che la macchina sia presente in Impostazioni Gantt
- Ricarica la pagina (F5)

**PlcSync non si connette alla macchina**
- Verifica che esista il file `macchina_XXX.json` con gli offset
- Verifica che il `MachineId` nel JSON corrisponda all'Id nel database
- Controlla i log di PlcSync per errori di connessione

**L'IP non viene aggiornato**
- Riavvia il servizio PlcSync dopo aver modificato l'IP
- Controlla i log: dovrebbe mostrare "IP aggiornato da database"

