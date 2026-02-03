# 📊 Schema Completo Sincronizzazione PLC

> **Data**: 3 Febbraio 2026  
> **Scopo**: Documentare architettura, problemi e soluzioni della sincronizzazione PLC

---

## 1. 🏗️ ARCHITETTURA ATTUALE

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            UI - MESManager.Web                                       │
├─────────────────────────────────────────────────────────────────────────────────────┤
│  ImpostazioniGantt.razor          PlcRealtime.razor         SyncMacchine.razor      │
│  └─→ Modifica IndirizzoPLC        └─→ Visualizza dati       └─→ Sync manuale        │
│      └─→ MacchinaAppService           └─→ PlcAppService         └─→ PlcController   │
└─────────────────────────────────────────────────────────────────────────────────────┘
                    │                           ↑                        │
                    ▼                           │                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          DATABASE - SQL Server                                       │
├─────────────────────────────────────────────────────────────────────────────────────┤
│  Macchine         PLCRealtime       PLCStorico        EventoPLC       PlcServiceStatus│
│  ├─ Id            ├─ MacchinaId     ├─ MacchinaId     ├─ MacchinaId   ├─ IsRunning    │
│  ├─ Codice        ├─ CicliFatti     ├─ CicliFatti     ├─ TipoEvento   ├─ Heartbeat    │
│  ├─ Nome          ├─ Stato          ├─ Timestamp      ├─ Timestamp    ├─ PollingInt   │
│  ├─ IndirizzoPLC  ├─ Barcode        └─ ...            └─ ...          └─ ...          │
│  └─ AttivaInGantt └─ ...                                                             │
└─────────────────────────────────────────────────────────────────────────────────────┘
                    ↑                           │
                    │                           │
                    │                           │
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        MESManager.PlcSync (Worker Service)                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐              │
│   │   PlcSyncWorker  │───▶│ PlcConnectionSvc │───▶│  PlcReaderSvc    │              │
│   │  (BackgroundSvc) │    │   (Sharp7 S7)    │    │  (DBRead PLC)    │              │
│   └──────────────────┘    └──────────────────┘    └──────────────────┘              │
│           │                                               │                          │
│           ▼                                               ▼                          │
│   ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐              │
│   │ LoadMachineConfigs│   │   PlcSyncSvc     │◀───│   PlcSnapshot    │              │
│   │  (JSON + DB IP)  │    │ (Write to DB)    │    │   (DTO dati)     │              │
│   └──────────────────┘    └──────────────────┘    └──────────────────┘              │
│           │                       │                                                  │
│           ▼                       │                                                  │
│   ┌──────────────────┐           │                                                  │
│   │  Configuration/  │           │                                                  │
│   │  machines/*.json │───────────┘ (solo per OFFSETS, IP viene dal DB!)             │
│   └──────────────────┘                                                              │
└─────────────────────────────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           PLC Siemens S7-300/400/1200/1500                           │
│   IP: 192.168.17.xx    Rack: 0    Slot: 1    DB: 55    Offset: 0-200 bytes          │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 🔴 PROBLEMA CRITICO: DISALLINEAMENTO GUID

### 2.1 Stato Attuale (ERRATO)

| File JSON | MachineId nel JSON | Numero JSON | → | DB Codice | DB Nome | DB IP |
|-----------|-------------------|-------------|---|-----------|---------|-------|
| `macchina_002.json` | `11111111-...02` | 2 | ❌ | **M001** | 01 | NULL |
| `macchina_003.json` | `11111111-...03` | 3 | ❌ | **M002** | 02 | 192.168.17.26 |
| `macchina_005.json` | `11111111-...05` | 5 | ❌ | **M003** | 03 | 192.168.17.24 |
| `macchina_006.json` | `11111111-...06` | 6 | ❌ | **M004** | 04 | NULL |
| `macchina_007.json` | `11111111-...07` | 7 | ❌ | **M005** | 05 | 192.168.17.27 |
| `macchina_008.json` | `11111111-...08` | 8 | ❌ | **M006** | 06 | 192.168.17.25 |
| `macchina_009.json` | `11111111-...09` | 9 | ❌ | **M007** | 07 | 192.168.17.23 |
| `macchina_010.json` | `11111111-...10` | 10 | ❌ | **M008** | 08 | 192.168.17.21 |

**⚠️ Il problema**: Il file `macchina_003.json` con GUID `...03` viene collegato alla macchina M002 (che è la macchina fisica 02), ma la sua configurazione IP e offsets potrebbero essere pensati per la macchina 03!

### 2.2 Conseguenze

1. **IP sbagliato**: Quando il Worker cerca l'IP per GUID `...03`, prende l'IP di M002 (che potrebbe essere corretto per caso)
2. **Dati mischiati**: I dati letti dal PLC di una macchina vengono salvati su PLCRealtime di un'altra
3. **Offsets errati**: Gli offset memoria PLC nel JSON potrebbero non corrispondere alla macchina reale

### 2.3 Macchine senza file JSON

Le macchine M009, M010, M011 hanno GUID diversi:
- **M009**: `53A810FA-75D4-4D82-C583-08DE58C59F6F` → esiste `macchina_009.json` ma ha GUID `...09`!
- **M010**: `57A8288D-3766-4C3B-C584-08DE58C59F6F` → esiste `macchina_010.json` ma ha GUID `...10`!
- **M011**: `617516EF-396B-409A-C585-08DE58C59F6F` → **NESSUN FILE JSON**

---

## 3. ⚙️ FLUSSO SINCRONIZZAZIONE DETTAGLIATO

### 3.1 Caricamento Configurazioni (all'avvio Worker)

```csharp
// Worker.cs - LoadMachineConfigsAsync()

// STEP 1: Carica IP dal database
var macchineDb = await context.Macchine.ToListAsync();
macchineDbIps = macchineDb.ToDictionary(m => m.Id, m => m.IndirizzoPLC);
// Risultato: { "...02" → NULL, "...03" → "192.168.17.26", ... }

// STEP 2: Carica file JSON
foreach (var file in Directory.GetFiles(configPath, "macchina_*.json"))
{
    var config = JsonSerializer.Deserialize<PlcMachineConfig>(json);
    // config.MacchinaId = "11111111-...03" (dal file macchina_003.json)
    
    // STEP 3: Cerca IP nel dizionario DB
    if (macchineDbIps.TryGetValue(config.MacchinaId, out var dbIp))
    {
        // TROVATO! Usa IP dal DB
        config.PlcIp = dbIp;  // "192.168.17.26" (IP di M002)
    }
    // Se non trovato, usa IP dal JSON
}
```

### 3.2 Problema nel Lookup

Il file `macchina_003.json` ha:
- `MachineId`: `11111111-1111-1111-1111-000000000003`
- `PlcIp` (JSON): `192.168.17.24`

Nel database, il GUID `...000000000003` corrisponde a **M002** con IP `192.168.17.26`.

**Risultato**: L'IP viene sovrascritto da 192.168.17.24 → 192.168.17.26 ✓ (corretto per M002)

**MA** gli offset nel JSON sono pensati per la "macchina 3" fisica, non per M002!

---

## 4. 🛠️ SOLUZIONE PROPOSTA: SEMPLIFICAZIONE

### 4.1 Approccio: Eliminare i File JSON

**Spostare TUTTO nel database** - una sola fonte di verità.

Nuova tabella `ConfigurazioniPLC`:

```sql
CREATE TABLE ConfigurazioniPLC (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MacchinaId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Macchine(Id),
    Enabled BIT DEFAULT 1,
    Rack INT DEFAULT 0,
    Slot INT DEFAULT 1,
    DbNumber INT DEFAULT 55,
    DbStart INT DEFAULT 0,
    DbLength INT DEFAULT 200,
    -- Offsets
    OffsetCicliFatti INT DEFAULT 18,
    OffsetCicliScarti INT DEFAULT 20,
    OffsetNumeroOperatore INT DEFAULT 22,
    OffsetStatoEmergenza INT DEFAULT 34,
    OffsetStatoManuale INT DEFAULT 36,
    OffsetStatoAutomatico INT DEFAULT 38,
    OffsetStatoCiclo INT DEFAULT 40,
    OffsetBarcodeLavorazione INT DEFAULT 46,
    OffsetQuantitaDaProd INT DEFAULT 162,
    OffsetTempoMedio INT DEFAULT 164,
    OffsetFigure INT DEFAULT 170
);
```

### 4.2 Workflow Semplificato

```
┌──────────────────────────────────────────┐
│      Impostazioni Macchine (UI)          │
│  ┌────────────────────────────────────┐  │
│  │ Macchina: M002                     │  │
│  │ Nome: 02                           │  │
│  │ IP PLC: 192.168.17.26              │  │
│  │ ☑ Abilita PLC Sync                 │  │
│  │ Rack: 0  Slot: 1  DB: 55           │  │
│  │ [Configura Offsets Avanzati]       │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    │
                    ▼ SaveAsync()
┌──────────────────────────────────────────┐
│           Database                        │
│  Macchine.IndirizzoPLC = "192.168.17.26" │
│  ConfigurazioniPLC.Enabled = true        │
│  ConfigurazioniPLC.Offsets = {...}       │
└──────────────────────────────────────────┘
                    │
                    ▼ (Worker legge)
┌──────────────────────────────────────────┐
│         PlcSyncWorker                     │
│  LoadFromDatabase() - NO MORE JSON!      │
└──────────────────────────────────────────┘
```

---

## 5. 📋 FIX IMMEDIATO (senza ristrutturazione)

### 5.1 Correggere i File JSON

Aggiornare i file JSON con i GUID e numeri corretti:

```json
// macchina_002.json → dovrebbe essere per M002 (codice M002, nome 02)
{
  "MachineId": "11111111-1111-1111-1111-000000000003", // GUID di M002
  "Numero": 2,
  "Nome": "M002",
  "PlcIp": "192.168.17.26",
  ...
}
```

### 5.2 Mappatura Corretta

| File da Rinominare | Nuovo MachineId | Codice DB | Nome DB | IP |
|-------------------|-----------------|-----------|---------|-----|
| `macchina_M002.json` | `11111111-...-000000000003` | M002 | 02 | 192.168.17.26 |
| `macchina_M003.json` | `11111111-...-000000000005` | M003 | 03 | 192.168.17.24 |
| `macchina_M005.json` | `11111111-...-000000000007` | M005 | 05 | 192.168.17.27 |
| `macchina_M006.json` | `11111111-...-000000000008` | M006 | 06 | 192.168.17.25 |
| `macchina_M007.json` | `11111111-...-000000000009` | M007 | 07 | 192.168.17.23 |
| `macchina_M008.json` | `11111111-...-000000000010` | M008 | 08 | 192.168.17.21 |
| `macchina_M009.json` | `53A810FA-75D4-4D82-C583-08DE58C59F6F` | M009 | 09 | 192.168.17.29 |
| `macchina_M010.json` | `57A8288D-3766-4C3B-C584-08DE58C59F6F` | M010 | 10 | 192.168.17.22 |

---

## 6. 📊 TABELLA SERVIZI E RESPONSABILITÀ

| Servizio | Progetto | Responsabilità | Input | Output |
|----------|----------|----------------|-------|--------|
| `PlcSyncWorker` | PlcSync | Loop principale ogni 4s | Config | Orchestrazione |
| `PlcConnectionService` | PlcSync | Connessione Sharp7 a PLC | IP, Rack, Slot | S7Client |
| `PlcReaderService` | PlcSync | Lettura DB55 | Config, Client | PlcSnapshot |
| `PlcSyncService` | PlcSync | Scrittura DB | Snapshot | PLCRealtime, Storico, Eventi |
| `PlcStatusWriterService` | PlcSync | Heartbeat e log | Status | PlcServiceStatus |
| `PlcAppService` | Infrastructure | Lettura per UI | MacchinaId | PlcRealtimeDto |
| `PlcSyncCoordinator` | Infrastructure | Sync manuale | MacchinaId | PlcSyncResult |
| `MacchinaAppService` | Infrastructure | CRUD Macchine | MacchinaDto | Macchina |

---

## 7. ✅ CHECKLIST AZIONI

- [ ] Creare script SQL per estrarre mappatura corretta GUID ↔ Macchine
- [ ] Aggiornare tutti i file JSON con i GUID corretti
- [ ] Rinominare i file JSON per coerenza (es. `macchina_M002.json`)
- [ ] Verificare che gli OFFSET siano corretti per ogni macchina fisica
- [ ] Testare la sincronizzazione dopo le modifiche
- [ ] (Futuro) Migrare offsets nel database ed eliminare i file JSON
