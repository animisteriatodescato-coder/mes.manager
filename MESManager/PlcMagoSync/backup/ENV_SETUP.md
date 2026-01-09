# 🔧 PlcMagoSync - Configurazione con .env.local

## ✅ Novità: Caricamento Automatico Variabili d'Ambiente

L'applicazione ora **automaticamente**:

1. ✅ Crea un file `.env.local` se non esiste
2. ✅ Carica le variabili d'ambiente da `.env.local`
3. ✅ Valida la configurazione all'avvio
4. ✅ Fornisce messaggi di errore chiari

---

## 🚀 Come Usare

### Primo Avvio

Alla **prima esecuzione** di `dotnet run`, l'applicazione:

```
== PlcMagoSync SYNC_MAGO ==
File .env.local non trovato. Creazione in corso...
✓ File .env.local creato. Compila i valori e riavvia l'applicazione.
```

### Step 2: Compilare il File .env.local

Il file `.env.local` sarà creato nella root del progetto:

```bash
# .env.local - Compila con i tuoi valori
GOOGLE_SHEET_ID=1-SoQMJt_5tAZFlSEuSNMOLOYBSwvoXFnrCayehhx1Qg
SERVICE_ACCOUNT_JSON_PATH=C:\Progetti\PlcMultiMachine\service-account.json
MAGO_CONNECTION_STRING=Data Source=192.168.1.72\SQLEXPRESS;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;
```

**Oppure**, copia da `.env.local.example`:

```bash
copy .env.local.example .env.local
# Poi modifica .env.local con i tuoi valori
```

### Step 3: Esegui l'Applicazione

```bash
dotnet run
```

Output atteso:

```
== PlcMagoSync SYNC_MAGO ==
✓ Variabili d'ambiente caricate da .env.local
=== AVVIO SYNC MAGO ===
== SYNC CLIENTI (CLIENTI_MAGO) ==
Clienti letti da Mago: [N]
SYNC CLIENTI completata.
=== SYNC COMPLETATA ===
```

---

## 📁 File Importanti

| File | Scopo | Committare? |
|------|-------|-------------|
| `.env.local` | Configurazione locale | ❌ NO (.gitignore) |
| `.env.local.example` | Template di riferimento | ✅ SÌ |
| `config_mago.json` | Placeholder ${...} | ✅ SÌ |
| `.gitignore` | Protezione credenziali | ✅ SÌ |

---

## 🔐 Sicurezza

✅ `.env.local` è protetto da `.gitignore`  
✅ Nessuna credenziale sarà mai committata  
✅ File `.env.local` rimane locale solo sul tuo PC  
✅ Condividi `.env.local.example` al team (senza credenziali)

---

## 📝 Formato .env.local

```bash
# Commenti iniziano con #
# Le linee vuote sono ignorate

# Variabile = Valore (senza spazi intorno a =)
GOOGLE_SHEET_ID=1-SoQMJt_5tAZFlSEuSNMOLOYBSwvoXFnrCayehhx1Qg

# Valori con spazi vanno senza virgolette
SERVICE_ACCOUNT_JSON_PATH=C:\Progetti\PlcMultiMachine\service-account.json

# Linee che iniziano con # sono ignorate
# MAGO_CONNECTION_STRING=vecchio-valore (non caricato)
```

---

## ⚠️ Troubleshooting

### Errore: "Variabile d'ambiente 'GOOGLE_SHEET_ID' non trovata"

**Soluzione**: Verifica che il file `.env.local` esista e abbia i valori compilati

```bash
# Controlla il file
cat .env.local

# O se vuoi ricrearlo
del .env.local
dotnet run
# Compila di nuovo
```

### Il file .env.local non viene creato

**Soluzione**: Assicurati che il percorso sia writable (permessi di scrittura)

```bash
# Crea manualmente il file
copy nul .env.local
# Compila con i tuoi valori
```

### Le variabili non vengono caricate

**Soluzione**: Controlla il formato del file

```bash
# ✅ Corretto
GOOGLE_SHEET_ID=my-sheet-id

# ❌ Non caricato (spazi)
GOOGLE_SHEET_ID = my-sheet-id

# ❌ Non caricato (commento)
# GOOGLE_SHEET_ID=my-sheet-id
```

---

## 📋 Procedura di Deployment

### Per il Team

1. **Fornisci** `.env.local.example` (senza credenziali)
2. **Team**: Copia in `.env.local`
3. **Team**: Compila con i tuoi valori
4. **Verifica**: `dotnet run`

### Per Production

Usa **variabili di sistema** anziché `.env.local`:

```powershell
# Windows
[System.Environment]::SetEnvironmentVariable("GOOGLE_SHEET_ID", "value", "Machine")

# Linux
export GOOGLE_SHEET_ID="value"
```

L'applicazione legge da `.env.local` se esiste, altrimenti dalle variabili di sistema.

---

## 🔄 Flusso di Caricamento Configurazione

```
Program.cs Main()
    ↓
LoadEnvironmentVariablesFromFile()
    ├─ Se .env.local non esiste
    │   ├─ Crea .env.local con template
    │   └─ Torna (chiedi compilazione)
    ├─ Se .env.local esiste
    │   ├─ Leggi ogni riga
    │   ├─ Ignora commenti (#) e linee vuote
    │   ├─ Parse KEY=VALUE
    │   └─ SetEnvironmentVariable()
    ↓
Leggi config_mago.json
    ├─ Placeholder ${GOOGLE_SHEET_ID}
    ├─ Placeholder ${SERVICE_ACCOUNT_JSON_PATH}
    └─ Placeholder ${MAGO_CONNECTION_STRING}
    ↓
ReplaceWithEnvironmentVariable()
    ├─ Leggi Environment.GetEnvironmentVariable()
    ├─ Se vuoto → Errore
    └─ Se valorizzato → Usa valore
    ↓
ValidateConfig()
    ├─ Controlla GoogleSheetId
    ├─ Controlla ServiceAccountJsonPath
    ├─ Controlla che file esista
    └─ Controlla MagoConnectionString
    ↓
MagoSyncManager.RunAsync()
```

---

**Versione**: 2.0 (Con .env.local Support)  
**Data**: 27 Novembre 2025  
**Status**: ✅ PRODUCTION READY
