# Configurazione PlcSync

Questa cartella contiene i file di configurazione per la sincronizzazione dati PLC.

## File di Configurazione

### 1. `appsettings.json`
Configurazione generale del servizio (intervalli polling, connessione database, ecc.)

### 2. `machines/` - Configurazioni Macchine
Ogni file JSON nella cartella `machines/` rappresenta una macchina PLC:
- `macchina_002.json` - Tornio CNC 2 (192.168.17.26)
- `macchina_003.json` - Fresatrice 3 (192.168.17.24)
- `macchina_005.json` - Saldatrice 5 (192.168.17.27)
- ...ecc

**Struttura:**
```json
{
  "MachineId": "11111111-1111-1111-1111-000000000002",
  "Numero": 2,
  "Nome": "Tornio CNC 2",
  "PlcIp": "192.168.17.26",
  "Enabled": true,
  "Offsets": { ... }
}
```

### 3. `operatori.json`
Mapping numero operatore → nome completo
```json
{
  "1": "MARCHIORI MARISA",
  "2": "DAS NAYON",
  ...
}
```

### 4. `column-labels.json`
Etichette colonne per UI (italiano/inglese)
- `Realtime`: Label per griglia real-time
- `Storico`: Label per griglia storico
- `Eventi`: Label per eventi PLC

### 5. `plc-offsets.json`
Offsets standard DB55 per tutti i PLC Siemens S7
- DbNumber: 55
- Offsets per: cicli, scarti, operatore, tempi, stati, ecc.

## Modifica Configurazione

### Aggiungere una nuova macchina:
1. Crea file `machines/macchina_XXX.json`
2. Copia da un file esistente
3. Modifica: MachineId (GUID univoco), Numero, Nome, PlcIp
4. Riavvia il servizio PlcSync

### Modificare offsets PLC:
1. Apri `machines/macchina_XXX.json`
2. Modifica sezione `"Offsets"`
3. Riavvia il servizio

### Aggiungere operatori:
1. Aggiungi entry in `operatori.json`
2. Inserisci record in tabella `Operatori` del database
3. Riavvia il servizio (auto-reload config)

## Note
- Tutti gli offsets sono per tipo INT (2 bytes)
- BarcodeLavorazione può variare (int o string a seconda del PLC)
- Gli stati macchina sono booleani (0/1) negli offset
