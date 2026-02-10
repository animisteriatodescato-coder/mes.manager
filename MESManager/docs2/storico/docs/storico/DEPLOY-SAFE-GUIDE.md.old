# Guida: Deploy Sicuro senza Interrompere la Sincronizzazione

## Il Problema

Quando si aggiorna il server, la sincronizzazione con Mago e le macchine PLC si rompe perché:

1. **Shutdown brusco**: I servizi vengono terminati senza dare tempo di chiudere le connessioni
2. **Connessioni PLC orfane**: I PLC Siemens S7 hanno un limite di connessioni (~32) e le connessioni non chiuse correttamente restano "appese"
3. **Sync Mago interrotta**: Se la sincronizzazione è in corso durante lo shutdown, può lasciare dati inconsistenti
4. **Stato in memoria perduto**: Il `RealtimeStateService` perde lo stato e i client SignalR vengono disconnessi

## Soluzioni Implementate

### 1. Graceful Shutdown per PlcSyncWorker

Il worker PLC ora:
- Riceve il segnale di shutdown tramite `IHostApplicationLifetime`
- Smette di processare nuovi cicli
- Chiude tutte le connessioni PLC in modo ordinato
- Logga lo stato di shutdown nel database

```csharp
// Registrato automaticamente nel costruttore
_appLifetime.ApplicationStopping.Register(OnApplicationStopping);
```

### 2. Timeout di Shutdown Esteso

I servizi ora hanno 30 secondi per completare lo shutdown invece di 5:

```csharp
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

### 3. Script di Deploy Migliorato

Lo script `deploy-production.ps1` ora:
- Attende fino a 30 secondi per il graceful shutdown
- Verifica che il servizio sia effettivamente fermo
- Forza il kill solo dopo il timeout
- Attende che il servizio sia effettivamente ripartito

### 4. Script di Restart Sicuro

Nuovo script `restart-services.ps1` per riavviare i servizi in modo sicuro:

```powershell
# Riavvia tutti i servizi
.\restart-services.ps1

# Riavvia solo PlcSync
.\restart-services.ps1 -Service PlcSync

# Con timeout personalizzato
.\restart-services.ps1 -WaitTime 45
```

## Best Practices per il Deploy

### 1. Prima del Deploy

```powershell
# Verifica lo stato dei servizi
Get-Service MESManager* | Format-Table Name, Status
```

### 2. Durante il Deploy

Usa sempre lo script di deploy:
```powershell
.\deploy-production.ps1 -Target All
```

**NON** fare:
- `Stop-Service` manuale senza attendere
- Kill del processo diretto
- Copiare file mentre il servizio gira

### 3. Dopo il Deploy

```powershell
# Verifica che i servizi siano partiti
Get-Service MESManager* | Format-Table Name, Status

# Controlla i log per errori
Get-EventLog -LogName Application -Source MESManager* -Newest 20
```

## Troubleshooting

### Connessioni PLC bloccate

Se i PLC non rispondono dopo un restart:

1. **Attendere 2-3 minuti**: Le connessioni S7 scadono automaticamente
2. **Riavviare il servizio PlcSync**:
   ```powershell
   .\restart-services.ps1 -Service PlcSync
   ```
3. **Se persistono problemi**, riavviare il modulo comunicazione del PLC (ultimo resort)

### Sync Mago incompleta

Se la sincronizzazione Mago sembra bloccata:

1. Controlla lo stato nel database:
   ```sql
   SELECT * FROM SyncLogs ORDER BY DataSync DESC
   ```

2. Riavvia il worker:
   ```powershell
   .\restart-services.ps1 -Service Worker
   ```

3. Forza una sync manuale dall'interfaccia web

### Client disconnessi (SignalR)

I client Blazor si riconnetteranno automaticamente. Se non lo fanno:
- Fare refresh della pagina (F5)
- Il `RealtimeStateService` ripartirà automaticamente il polling

## Monitoraggio

### Verificare lo stato dei servizi

```powershell
# Stato servizi
Get-Service MESManager* | Format-Table Name, Status, StartType

# Processi in esecuzione
Get-Process -Name "MESManager*" | Format-Table Name, Id, CPU, WorkingSet64
```

### Verificare lo stato nel database

```sql
-- Stato servizio PLC
SELECT * FROM PlcServiceStatus ORDER BY LastHeartbeat DESC

-- Ultime sync Mago
SELECT TOP 10 * FROM SyncLogs ORDER BY DataSync DESC

-- Stato connessioni PLC
SELECT m.Numero, m.Nome, r.UltimoAggiornamento, r.Connesso
FROM Macchine m
LEFT JOIN PlcRealtime r ON m.Id = r.MacchinaId
ORDER BY m.Numero
```

## Architettura Resiliente

```
┌──────────────────────────────────────────────────────────────┐
│                     DEPLOY PROCESS                           │
├──────────────────────────────────────────────────────────────┤
│  1. Stop Service (graceful)                                  │
│     └─> Service riceve SIGTERM                               │
│         └─> ApplicationStopping event fired                  │
│             └─> PlcSyncWorker._isShuttingDown = true         │
│                 └─> Loop principale termina                  │
│                     └─> _connectionService.DisconnectAll()   │
│                         └─> Tutte le connessioni S7 chiuse   │
│                                                              │
│  2. Wait (max 30 sec)                                        │
│     └─> Verifica Status == Stopped                           │
│         └─> Se timeout, force kill                           │
│                                                              │
│  3. Copy Files                                               │
│     └─> File copiati in sicurezza                            │
│                                                              │
│  4. Start Service                                            │
│     └─> Wait for Status == Running                           │
│         └─> PlcSyncWorker.ExecuteAsync() inizia              │
│             └─> Riconnessione a tutti i PLC                  │
│                 └─> Polling riprende                         │
└──────────────────────────────────────────────────────────────┘
```

## Configurazione Windows Services

Per registrare come Windows Service:

```powershell
# PlcSync
sc.exe create MESManager.PlcSync binPath="C:\MESManager\PlcSync\MESManager.PlcSync.exe" start=auto

# Worker
sc.exe create MESManager.Worker binPath="C:\MESManager\Worker\MESManager.Worker.exe" start=auto

# Configurare recovery
sc.exe failure MESManager.PlcSync reset=86400 actions=restart/60000/restart/60000/restart/60000
sc.exe failure MESManager.Worker reset=86400 actions=restart/60000/restart/60000/restart/60000
```

Questo configura il servizio per riavviarsi automaticamente dopo 60 secondi in caso di crash.
