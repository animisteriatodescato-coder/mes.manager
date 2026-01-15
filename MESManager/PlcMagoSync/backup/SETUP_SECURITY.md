# PlcMagoSync - Guida di Configurazione

## Panoramica
PlcMagoSync è un'applicazione .NET che sincronizza dati tra un database SQL Server (MAGO) e Google Sheets.

## Configurazione Credenziali

⚠️ **IMPORTANTE**: Le credenziali NON devono mai essere commitmate nel repository. Usa variabili d'ambiente.

### Passaggio 1: Configurare le Variabili d'Ambiente

#### Windows (PowerShell):
```powershell
# Imposta le variabili d'ambiente
[System.Environment]::SetEnvironmentVariable("GOOGLE_SHEET_ID", "your-sheet-id-here", "User")
[System.Environment]::SetEnvironmentVariable("SERVICE_ACCOUNT_JSON_PATH", "C:\path\to\service-account.json", "User")
[System.Environment]::SetEnvironmentVariable("MAGO_CONNECTION_STRING", "Data Source=your-server;Initial Catalog=your-db;User Id=user;Password=pass;", "User")
```

#### Windows (Prompt CMD):
```cmd
setx GOOGLE_SHEET_ID "your-sheet-id-here"
setx SERVICE_ACCOUNT_JSON_PATH "C:\path\to\service-account.json"
setx MAGO_CONNECTION_STRING "Data Source=your-server;Initial Catalog=your-db;User Id=user;Password=pass;"
```

#### Linux/macOS (Bash):
```bash
export GOOGLE_SHEET_ID="your-sheet-id-here"
export SERVICE_ACCOUNT_JSON_PATH="/path/to/service-account.json"
export MAGO_CONNECTION_STRING="Data Source=your-server;Initial Catalog=your-db;User Id=user;Password=pass;"
```

### Passaggio 2: Verificare config_mago.json

Il file `config_mago.json` deve contenere SOLO i placeholder, mai le credenziali reali:

```json
{
  "GoogleSheetId": "${GOOGLE_SHEET_ID}",
  "ServiceAccountJsonPath": "${SERVICE_ACCOUNT_JSON_PATH}",
  "MagoConnectionString": "${MAGO_CONNECTION_STRING}",
  "SyncIntervalMinutes": 60
}
```

### Passaggio 3: Service Account Google

1. Scarica il file `service-account.json` da Google Cloud Console
2. Salva il file in una directory sicura (non nel repository)
3. Imposta il percorso nella variabile d'ambiente `SERVICE_ACCOUNT_JSON_PATH`

## Struttura del Progetto

```
PlcMagoSync/
├── SYNC_MAGO/
│   ├── MagoSyncManager.cs          # Orchestratore principale
│   ├── Models/
│   │   ├── ConfigMago.cs           # Modello configurazione
│   │   └── ClienteMago.cs          # Modello cliente
│   ├── Services/
│   │   ├── GoogleSheetsService.cs  # Google Sheets API
│   │   └── MagoDbService.cs        # Database queries
│   └── Modules/
│       ├── SyncClienti.cs          # ✅ Sincronizzazione clienti
│       ├── SyncArticoli.cs         # ⏳ In implementazione
│       └── SyncCommesse.cs         # ⏳ In implementazione
├── Program.cs                       # Entry point
└── config_mago.json                 # Configurazione (placeholder)
```

## Modelli Dati

### ClienteMago
Sincronizza i seguenti campi:
- `Codice`: Identificativo cliente
- `Nome`: Ragione sociale
- `Email`: Email cliente
- `Note`: Note generali
- `UltimaModifica`: Data ultima modifica

## Esecuzione

### Debug
```bash
dotnet run
```

### Release
```bash
dotnet build -c Release
cd bin/Release/net8.0
./PlcMagoSync.exe
```

## Security

- ✅ Variabili d'ambiente per credenziali
- ✅ File config_mago.json con placeholder
- ✅ .gitignore per proteggere file sensibili
- ✅ Validazione configurazione all'avvio

## Dipendenze

- `Google.Apis.Auth` v1.73.0
- `Google.Apis.Sheets.v4` v1.72.0.3966
- `System.Data.SqlClient` v4.9.0
- `.NET 8.0`

## Troubleshooting

### Errore: "Variabile d'ambiente 'GOOGLE_SHEET_ID' non trovata"
- Verifica di aver impostato le variabili d'ambiente
- Riavvia l'IDE o il terminale dopo aver impostato le variabili
- Su Windows, usa `setx` per variabili permanenti, non `set` (temporanee)

### Errore: "File service account non trovato"
- Verifica il percorso in `SERVICE_ACCOUNT_JSON_PATH`
- Assicurati che il file sia leggibile

### Errore di connessione al database
- Verifica la stringa di connessione in `MAGO_CONNECTION_STRING`
- Verifica che il server SQL sia accessibile dalla rete

## Prossimi Passi

- [ ] Implementare `SyncArticoli`
- [ ] Implementare `SyncCommesse`
- [ ] Aggiungere error handling e retry logic
- [ ] Aggiungere logging strutturato
- [ ] Implementare Dependency Injection
- [ ] Aggiungere unit test
