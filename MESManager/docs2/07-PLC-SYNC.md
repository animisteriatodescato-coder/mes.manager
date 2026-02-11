# 07 - PLC Sync - Sincronizzazione Siemens S7

> **Scopo**: Comunicazione real-time con PLC, architettura, configurazione e troubleshooting

---

## 🏗️ Architettura Completa

```
┌─────────────────────────────────────────────────────────────┐
│                    UI - MESManager.Web                       │
├─────────────────────────────────────────────────────────────┤
│  ImpostazioniGantt.razor    PlcRealtime.razor               │
│  └─→ Modifica IndirizzoPLC  └─→ Visualizza dati             │
└─────────────────────────────────────────────────────────────┘
                    │                    ↑
                    ▼                    │
┌─────────────────────────────────────────────────────────────┐
│                  DATABASE - SQL Server                       │
├─────────────────────────────────────────────────────────────┤
│  Macchine         PLCRealtime       PLCStorico               │
│  ├─ IndirizzoPLC  ├─ CicliFatti     ├─ CicliFatti            │
│  └─ Codice        └─ Stato          └─ Timestamp             │
└─────────────────────────────────────────────────────────────┘
                    ↑                    │
                    │                    │
┌─────────────────────────────────────────────────────────────┐
│            MESManager.PlcSync (Worker Service)               │
├─────────────────────────────────────────────────────────────┤
│  Worker.cs                                                   │
│  ├─ LoadMachineConfigsAsync()  ← Carica IP da DB            │
│  ├─ ExecuteAsync()             ← Polling loop                │
│  └─ ReadPlcDataAsync()         ← Legge PLC                   │
│                                                              │
│  Configuration/machines/                                     │
│  ├─ macchina_002.json  ← Offset PLC                          │
│  ├─ macchina_003.json                                        │
│  └─ ...                                                      │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│           PLC Siemens S7-300/400/1200/1500                   │
│  IP: 192.168.17.xx    Rack: 0    Slot: 1    DB: 55           │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Configurazione Ibrida (Database + JSON)

### Fonti di Verità

| Dato | Fonte | Dove Modificarlo |
|------|-------|------------------|
| **Elenco macchine** | Database | Impostazioni → Gantt Macchine |
| **IP PLC** | Database | Impostazioni → Gantt Macchine |
| **Codice macchina** | Database | Impostazioni → Gantt Macchine |
| **Offset PLC** | File JSON | `PlcSync/Configuration/machines/*.json` |
| **Rack/Slot/DB** | File JSON | `PlcSync/Configuration/machines/*.json` |

---

### Come Funziona

**PlcSync Worker** all'avvio:

1. Carica macchine dal database (con IP da campo `IndirizzoPLC`)
2. Carica file JSON per offset memoria PLC
3. **Sovrascrive IP del JSON con quello del database**
4. Usa configurazione risultante per connettersi ai PLC

**REGOLA D'ORO**: IP sempre dal database, offset sempre dal JSON!

---

## 📄 Esempio File JSON

`PlcSync/Configuration/machines/macchina_003.json`:

```json
{
  "MachineId": "11111111-1111-1111-1111-111111111103",
  "MachineCode": "M003",
  "PlcIp": "192.168.17.24",  // ← SOVRASCRITTO dal DB all'avvio!
  "Rack": 0,
  "Slot": 1,
  "DbNumber": 55,
  "Offsets": {
    "CicliFattiOffset": 0,
    "StatoOffset": 4,
    "BarcodeOffset": 8,
    "BarcodeLength": 20,
    "QuantitaDaProdurreOffset": 28,
    "CicliScartiOffset": 32,
    "NumeroOperatoreOffset": 36
  }
}
```

**⚠️ IMPORTANTE**: L'IP nel JSON viene **ignorato** e sostituito con quello del database!

---

## 🔌 Modificare IP Macchina

### Procedura Corretta

1. Vai su **Impostazioni → Gantt Macchine**
2. Trova la macchina (es. M003)
3. Modifica campo **Indirizzo PLC** (es. da `192.168.17.24` a `192.168.17.30`)
4. Salva
5. **Riavvia PlcSync** (non serve modificare JSON!)

```powershell
# Ferma PlcSync
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.PlcSync.exe /F

# Riavvia
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESPlcSync"
```

---

## 🔄 Flusso Sincronizzazione

### 1. Caricamento Configurazioni (Avvio)

```csharp
private async Task LoadMachineConfigsAsync()
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();
    
    // 1. Carica macchine dal DB
    var macchineDb = await dbContext.Macchine
        .Where(m => m.AttivaInGantt)
        .ToListAsync();
    
    // 2. Carica file JSON
    var jsonFiles = Directory.GetFiles("Configuration/machines", "*.json");
    
    foreach (var file in jsonFiles)
    {
        var config = JsonSerializer.Deserialize<PlcMachineConfig>(File.ReadAllText(file));
        
        // 3. Trova macchina corrispondente nel DB
        var macchinaDb = macchineDb.FirstOrDefault(m => m.Id == config.MachineId);
        
        // 4. SOVRASCRIVE IP dal database
        if (macchinaDb != null && !string.IsNullOrEmpty(macchinaDb.IndirizzoPLC))
        {
            config.PlcIp = macchinaDb.IndirizzoPLC;
        }
        
        _machineConfigs.Add(config);
    }
}
```

---

### 2. Polling Loop (Esecuzione)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await LoadMachineConfigsAsync();
    
    while (!stoppingToken.IsCancellationRequested)
    {
        foreach (var config in _machineConfigs)
        {
            try
            {
                await ReadPlcDataAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore lettura PLC {MachineCode}", config.MachineCode);
            }
        }
        
        await Task.Delay(1000, stoppingToken);  // Polling 1 secondo
    }
}
```

---

### 3. Lettura Dati PLC (Sharp7)

```csharp
private async Task ReadPlcDataAsync(PlcMachineConfig config)
{
    var plc = new S7Client();
    
    // 1. Connessione
    int result = plc.ConnectTo(config.PlcIp, config.Rack, config.Slot);
    if (result != 0)
    {
        _logger.LogWarning("Connessione fallita: {Ip} - {Error}", config.PlcIp, plc.ErrorText(result));
        return;
    }
    
    // 2. Lettura Data Block
    byte[] buffer = new byte[256];
    result = plc.DBRead(config.DbNumber, 0, 256, buffer);
    if (result != 0)
    {
        _logger.LogError("Lettura DB fallita: {Error}", plc.ErrorText(result));
        plc.Disconnect();
        return;
    }
    
    // 3. Estrazione dati
    var cicliFatti = S7.GetIntAt(buffer, config.Offsets.CicliFattiOffset);
    var stato = S7.GetStringAt(buffer, config.Offsets.StatoOffset);
    var barcode = S7.GetStringAt(buffer, config.Offsets.BarcodeOffset);
    
    // 4. Salvataggio DB
    await SavePlcDataAsync(config.MachineId, cicliFatti, stato, barcode);
    
    // 5. Disconnessione
    plc.Disconnect();
}
```

---

### 4. Salvataggio Database

```csharp
private async Task SavePlcDataAsync(Guid machineId, int cicliFatti, string stato, string barcode)
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();
    
    // Update o Insert in PLCRealtime
    var realtime = await dbContext.PLCRealtime
        .FirstOrDefaultAsync(p => p.MacchinaId == machineId);
    
    if (realtime == null)
    {
        realtime = new PLCRealtime { MacchinaId = machineId };
        dbContext.PLCRealtime.Add(realtime);
    }
    
    realtime.CicliFatti = cicliFatti;
    realtime.StatoMacchina = stato;
    realtime.BarcodeLavorazione = barcode;
    realtime.DataUltimoAggiornamento = DateTime.Now;
    
    // Salva snapshot in storico (se cambio stato)
    if (realtime.StatoMacchina != stato)
    {
        dbContext.PLCStorico.Add(new PLCStorico
        {
            MacchinaId = machineId,
            CicliFatti = cicliFatti,
            StatoMacchina = stato,
            DataRegistrazione = DateTime.Now
        });
    }
    
    await dbContext.SaveChangesAsync();
}
```

---

## 🐛 Problemi Comuni

### ❌ PlcSync non si connette ai PLC

**Causa 1**: IP errato nel database

**Soluzione**:
```sql
-- Verifica IP nel database
SELECT Codice, IndirizzoPLC FROM Macchine;

-- Aggiorna se necessario
UPDATE Macchine SET IndirizzoPLC = '192.168.17.30' WHERE Codice = 'M003';
```

---

**Causa 2**: Connessioni S7 "appese" (slot PLC occupati)

**Soluzione**:
```powershell
# Attendi 2-3 minuti per timeout automatico
Start-Sleep 180

# Riavvia PlcSync
taskkill /IM MESManager.PlcSync.exe /F
schtasks /Run /TN "StartMESPlcSync"

# Se persiste, riavvia PLC (ultima risorsa)
```

---

**Causa 3**: Firewall blocca porta 102 (S7 protocol)

**Soluzione**:
```powershell
# Aggiungi regola firewall
New-NetFirewallRule -DisplayName "Allow S7 Protocol" -Direction Outbound -Protocol TCP -LocalPort 102 -Action Allow
```

---

### ❌ Dati non si aggiornano in PLCRealtime

**Causa**: Offset errati nel file JSON

**Soluzione**:
1. Verifica offset con TIA Portal o altro tool PLC
2. Aggiorna file JSON:
```json
"Offsets": {
  "CicliFattiOffset": 0,    // Verifica byte corretto!
  "StatoOffset": 4
}
```
3. Riavvia PlcSync

---

### ❌ PlcSync crash al riavvio

**Causa**: File JSON malformato o MachineId errato

**Soluzione**:
```powershell
# Controlla log
Get-Content "C:\MESManager\PlcSync\logs\*.log" -Tail 50

# Verifica JSON syntax
Get-Content "C:\MESManager\PlcSync\Configuration\machines\macchina_003.json" | ConvertFrom-Json

# Verifica MachineId nel database
sqlcmd -S localhost\SQLEXPRESS01 -Q "SELECT Id, Codice FROM Macchine"
```

---

### ❌ "Access Violation" con Sharp7

**Causa**: Buffer troppo piccolo per lettura DB

**Soluzione**:
```csharp
// Aumenta dimensione buffer
byte[] buffer = new byte[512];  // Era 256
```

---

## ⚙️ Configurazione Avanzata

### appsettings.json (PlcSync)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MESManager.PlcSync": "Debug"
    }
  },
  "PlcSync": {
    "PollingIntervalMs": 1000,      // Polling ogni 1 secondo
    "ConnectionTimeoutMs": 5000,     // Timeout connessione 5 sec
    "RetryAttempts": 3,              // Tentativi riconnessione
    "MachineConfigPath": "Configuration/machines",
    "GracefulShutdownTimeoutMs": 10000  // 10 sec per chiudere connessioni
  }
}
```

---

### Graceful Shutdown

```csharp
public override async Task StopAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("PlcSync fermandosi, chiusura connessioni PLC...");
    
    // Disconnetti tutti i PLC
    foreach (var plcConnection in _activePlcConnections)
    {
        try
        {
            plcConnection.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore disconnessione PLC");
        }
    }
    
    await base.StopAsync(cancellationToken);
}
```

**⚠️ CRITICO**: Sempre fermare PlcSync con graceful shutdown per liberare slot PLC!

---

## 🔍 Monitoring e Diagnostica

### Verifica Status PlcSync

```sql
-- Ultimo heartbeat
SELECT * FROM PlcServiceStatus;

-- Log errori
SELECT TOP 50 * FROM PlcSyncLogs 
WHERE Level = 'Error' 
ORDER BY Timestamp DESC;
```

---

### Verifica Dati Realtime

```sql
-- Ultimo aggiornamento per macchina
SELECT 
    m.Codice,
    p.CicliFatti,
    p.StatoMacchina,
    p.DataUltimoAggiornamento,
    DATEDIFF(SECOND, p.DataUltimoAggiornamento, GETDATE()) AS SecondiDaAggiornamento
FROM PLCRealtime p
INNER JOIN Macchine m ON m.Id = p.MacchinaId
ORDER BY m.Codice;
```

Se `SecondiDaAggiornamento` > 10, PlcSync probabilmente non funziona!

---

## 📊 Performance

### Polling Interval

```
1000ms (1 sec)  = Ottimo per real-time, carico moderato
500ms (0.5 sec) = Real-time aggressivo, carico alto
2000ms (2 sec)  = Carico basso, meno responsivo
```

**Raccomandato**: 1000ms

---

### Connessioni Simultanee

**Limite PLC**: ~32 connessioni simultanee (varia per modello)

**Best practice**: Riutilizza connessioni invece di aprire/chiudere ogni ciclo

```csharp
// ❌ Sbagliato: Apri/chiudi ogni ciclo
foreach (var config in _machineConfigs)
{
    var plc = new S7Client();
    plc.ConnectTo(...);
    plc.DBRead(...);
    plc.Disconnect();  // Slot liberato lentamente!
}

// ✅ Corretto: Pool di connessioni
private Dictionary<Guid, S7Client> _plcPool = new();
```

---

## 📡 Sistema Trasmissione Ricette PLC (v1.34.0)

> **Aggiunto**: 11 Febbraio 2026  
> **Scopo**: Caricamento automatico/manuale ricette da DB a PLC DB52

### Architettura

```
┌──────────────────────────────────────────────────────────┐
│                     TRIGGER EVENTI                        │
├──────────────────────────────────────────────────────────┤
│  PlcSyncService (4s poll)                                 │
│  └─→ Rileva cambio Barcode in DB55                       │
│  └─→ Emette evento: CommessaCambiataEventArgs            │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│                  EVENT HANDLER                            │
├──────────────────────────────────────────────────────────┤
│  RecipeAutoLoaderWorker (BackgroundService)               │
│  └─→ Ascolta evento CommessaCambiata                      │
│  └─→ Chiama RecipeAutoLoaderService                       │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│                 BUSINESS LOGIC                            │
├──────────────────────────────────────────────────────────┤
│  RecipeAutoLoaderService                                  │
│  └─→ GetProssimaCommessaDalGanttAsync()                  │
│  └─→ Recupera ricetta articolo dal catalogo              │
│  └─→ Chiama PlcRecipeWriterService                       │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│                 PLC WRITER (Sharp7)                       │
├──────────────────────────────────────────────────────────┤
│  PlcRecipeWriterService                                   │
│  ├─→ WriteRecipeToDb52Async() - Scrive ricetta           │
│  ├─→ ReadDb55Async() - Legge stato macchina              │
│  ├─→ ReadDb52Async() - Legge ricetta corrente            │
│  └─→ CopyDb55ToDb52Async() - Copia lettura → scrittura   │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│           PLC Siemens - Data Blocks                       │
├──────────────────────────────────────────────────────────┤
│  DB55 (READ ONLY - Status)    │  DB52 (WRITE - Recipe)    │
│  ├─ CicliFatti                │  ├─ CodiceArticolo       │
│  ├─ Stato                     │  ├─ Parametri[]          │
│  ├─ Barcode                   │  ├─ Timestamp            │
│  └─ ...                       │  └─ Status               │
└──────────────────────────────────────────────────────────┘
```

### Data Layout DB52 (Ricetta)

| Offset | Tipo | Campo | Note |
|--------|------|-------|------|
| 0 | STRING[20] | CodiceArticolo | Codice identificativo |
| 20 | INT | NumeroParametri | Conteggio parametri |
| 22+ | BYTE[4] | Parametri[N] | Valori singoli parametri |
| 500 | LINT | Timestamp | Unix timestamp scrittura |
| 508 | INT | Status | 0=vuoto, 1=caricato, 2=errore |

### API Endpoints

| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| POST | `/api/plc/load-next-recipe-manual/{macchinaId}` | Carica prossima ricetta da Gantt |
| POST | `/api/plc/load-recipe-by-article` | Carica ricetta per codice articolo |
| GET | `/api/plc/db55/{macchinaId}` | Legge DB55 (stato macchina) |
| GET | `/api/plc/db52/{macchinaId}` | Legge DB52 (ricetta corrente) |
| POST | `/api/plc/copy-db55-to-db52/{macchinaId}` | Copia DB55 → DB52 |

### Comportamento PLC Offline

```
PLC Online:  Scrittura immediata DB52 ✅
PLC Offline: BLOCCA scrittura (no queue) ⛔
             Pause monitoring eventi
PLC Reconnect: Auto-retry ultima ricetta fallita 🔄
```

### UI Dashboard Integration

- **Doppio-click** su card macchina → Popup viewer DB55/DB52
- **Pulsante "Prossima"** → Carica manualmente prossima ricetta
- **Pulsante "DB"** → Apre popup alternativo

### File Coinvolti

| File | Layer | Responsabilità |
|------|-------|----------------|
| `PlcRecipeWriterService.cs` | Infrastructure | Sharp7 communication |
| `RecipeAutoLoaderService.cs` | Infrastructure | Business logic caricamento |
| `RecipeAutoLoaderWorker.cs` | Worker | Event listener |
| `PlcController.cs` | Web | REST API endpoints |
| `PlcDbViewerPopup.razor` | Web/UI | Visualizzatore DB |
| `DashboardProduzione.razor` | Web/UI | Double-click + buttons |

---

## 🆘 Supporto

Per configurazione generale: [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md)  
Per architettura: [04-ARCHITETTURA.md](04-ARCHITETTURA.md)  
Per deploy: [01-DEPLOY.md](01-DEPLOY.md)  
Per replica sistema: [05-REPLICA-SISTEMA.md](05-REPLICA-SISTEMA.md)
