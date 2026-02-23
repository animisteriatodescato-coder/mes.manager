# 🚨 LEZIONI DEPLOYMENT CRITICHE

> **Scopo**: Documenta errori reali risolti durante deploy in produzione  
> **Obiettivo**: Evitare ripetizione degli stessi problemi  
> **Ultima Modifica**: 19 Febbraio 2026

---

## 📋 INDICE PROBLEMI

| # | Problema | Deploy | Gravità | Status |
|---|----------|--------|---------|--------|
| **1** | Database Produzione Mancante Dati Base | v1.30.10 | 🔴 CRITICO | ✅ RISOLTO |
| **2** | "Carica su Gantt" Assegna TUTTO a Macchina 1 | v1.30.11 | 🔴 CRITICO | ✅ RISOLTO |
| **3** | Stato Colonne AG Grid Perso | v1.30.11 | 🟡 MEDIO | ✅ RISOLTO |
| **4** | Script Deploy Incompleto | v1.30.11 | 🟡 MEDIO | ✅ RISOLTO |
| **5** | Nomi Clienti Errati (Mostra Fornitori) | v1.30.11 | 🔴 CRITICO | ✅ RISOLTO |
| **6** | Preferenze Utente Resettate | v1.30.11 | 🟡 MEDIO | ✅ RISOLTO |
| **7** | Sync Automatica Fallisce (Worker vs Web DB Diversi) | Feb 2026 | 🔴 CRITICO | ✅ RISOLTO |

---

## 🔴 PROBLEMA 1: Database Produzione Mancante Dati Base

**Deploy**: v1.30.10 → v1.30.11 (Feb 2026)

### Sintomo
Dopo deploy, "Carica su Gantt" fallì con errore:
```
Impostazioni produzione mancanti
```

### Root Cause
- Database produzione (`MESManager_Prod`) non aveva record in `ImpostazioniProduzione` e `CalendarioLavoro`
- L'applicazione assumeva che queste tabelle fossero sempre popolate (no fallback/seeding)
- Deploy su dev aveva dati mock, ma DB prod era schema puro senza seed

### Soluzione Immediata
```sql
-- Connesso a 192.168.1.230\SQLEXPRESS01 → MESManager_Prod
INSERT INTO ImpostazioniProduzione (Id, TempoSetupMinuti, OreLavorativeGiornaliere, GiorniLavorativiSettimanali)
VALUES (NEWID(), 90, 8, 5);

INSERT INTO CalendarioLavoro (Id, Lunedi, Martedi, Mercoledi, Giovedi, Venerdi, Sabato, Domenica, OraInizio, OraFine)
VALUES (NEWID(), 1, 1, 1, 1, 1, 0, 0, '08:00:00', '17:00:00');
```

### Lezione Appresa
- ✅ Sempre verificare tabelle critiche dopo deploy in produzione (tramite SQL o UI)
- ✅ Aggiungere healthcheck endpoint che valida dati base: `/api/health/critical-data`
- ✅ Considerare migration seed automatico per tabelle configurazione

### Prevenzione
Aggiunto a checklist deploy (vedi [01-DEPLOY.md](../01-DEPLOY.md)):
```
[ ] DB Prod: Verifica dati base (ImpostazioniProduzione, CalendarioLavoro, Macchine)
```

---

## 🔴 PROBLEMA 2: "Carica su Gantt" Assegna TUTTO a Macchina 1

**Deploy**: v1.30.11 (Feb 2026)

### Sintomo
L'auto-scheduler metteva tutte le commesse sulla macchina 1, ignorando macchine 2-10

### Root Cause
**File**: `PianificazioneEngineService.cs` (linee 1065-1080)

```csharp
// ❌ BUG: GroupBy solo su macchine CHE GIÀ HANNO commesse
var caricoPerMacchina = tutteCommesseAssegnate
    .GroupBy(c => c.NumeroMacchina!)
    .Select(...)
    .OrderBy(x => x.OreTotali);

// Se solo M1 ha commesse → caricoPerMacchina contiene SOLO M1
// Macchine 2-10 mai considerate → default a macchina 1
```

### Soluzione
Query TUTTE le macchine attive + calcolo carico per ognuna:

```csharp
// ✅ Carica TUTTE le macchine attive
var macchineAttive = await _context.Macchine
    .Where(m => m.AttivaInGantt)
    .OrderBy(m => m.OrdineVisualizazione)
    .ToListAsync();

// Estrai numeri da codici ("M001" → 1, "M005" → 5)
var numeriMacchineAttive = macchineAttive
    .Select(m => int.TryParse(m.Codice.Replace("M0", "").Replace("M", ""), out var n) ? n : (int?)null)
    .Where(n => n.HasValue)
    .Select(n => n!.Value)
    .ToList();

// Calcola carico PER OGNI macchina attiva (anche 0h se vuota)
var caricoPerMacchina = numeriMacchineAttive
    .Select(numMacchina => {
        var commesse = tutteCommesseAssegnate.Where(c => c.NumeroMacchina == numMacchina).ToList();
        return new {
            NumeroMacchina = numMacchina,
            OreTotali = commesse.Sum(c => ...), // 0h se vuota
            ...
        };
    })
    .OrderBy(x => x.OreTotali) // Macchine vuote (0h) prima!
    .ToList();
```

### Lezione Appresa
- ✅ Mai assumere che `GroupBy` su dati esistenti copra tutti i record master
- ✅ Sempre query esplicita su tabelle master (Macchine, Utenti, ecc.)
- ✅ Test con macchine vuote per validare logica distribuzione

### File Modificati
- `MESManager.Infrastructure/Services/PianificazioneEngineService.cs`

---

## 🟡 PROBLEMA 3: Stato Colonne AG Grid Perso Durante Deploy

**Deploy**: v1.30.11 (Feb 2026)

### Sintomo
Configurazioni colonne salvate con "Fix" scomparse dopo deploy/restart

### Root Cause
- JS salvava colonne in `localStorage` ad ogni cambio (`onColumnMoved`, ecc.)
- Blazor salvava in DB solo su click "Fix" → chiave `commesse-aperte-grid-fixed-state`
- Ma grid `init()` caricava da `commesse-aperte-grid-settings` (chiave diversa!)
- L'evento `commesseAperteGridStateChanged` era dispatchato ma MAI ascoltato da Blazor
- **DB mai aggiornato automaticamente** → `ColumnStateJson` stale/null

### Diagramma Flusso Vecchio
```
Cambio colonne → JS saveColumnState() → localStorage ✅
               → window.dispatchEvent('...StateChanged') → Blazor ❌ (non ascolta)
               → DB mai aggiornato ❌
Deploy/Restart → Blazor legge DB (null/stale) → Grid fallback a localStorage
              → Se localStorage cancellato → Stato perso! 
```

### Soluzione
Sincronizzazione automatica DB con debounce:

```javascript
// Nuovo: notifyBlazorStateChanged() con debounce
function notifyBlazorStateChanged() {
    window.dispatchEvent(new CustomEvent('commesseAperteGridStateChanged'));
    if (_blazorStateTimer) clearTimeout(_blazorStateTimer);
    _blazorStateTimer = setTimeout(() => {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('SaveGridStateFromJs').catch(() => {});
        }
    }, 1000); // Debounce 1 sec
}
```

### Diagramma Flusso Nuovo
```
Cambio colonne → saveColumnState() → localStorage ✅
               → notifyBlazorStateChanged() (debounce 1s) → dotNetHelper.SaveGridStateFromJs()
               → Blazor SaveSettings() → DB ✅
Deploy/Restart → Blazor legge DB (aggiornato!) → Grid ripristinato correttamente
```

### Lezione Appresa
- ✅ Se UI chiave (grid state, preferenze) va persistita, DB sync deve essere automatico
- ✅ Debounce per evitare flooding su cambi frequenti (1-2 secondi OK)
- ✅ `localStorage` è backup, non source of truth (può essere cancellato)
- ✅ Test post-deploy: refresh browser, verifica stato ripristinato

### File Modificati
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js`

---

## 🟡 PROBLEMA 4: Script Deploy Incompleto - Worker/PlcSync Non Copiati

**Deploy**: v1.30.11 (Feb 2026)

### Sintomo
Documentazione deploy ([01-DEPLOY.md](../01-DEPLOY.md)) mostrava 3 `dotnet publish` ma solo 1 `robocopy`

### Root Cause
- Script pubblicava separatamente: Web, Worker, PlcSync in `publish/Web`, `publish/Worker`, `publish/PlcSync`
- Ma `robocopy` copiava SOLO `publish/Web` → `C:\MESManager` 
- Worker e PlcSync non venivano aggiornati sul server (usavano vecchie versioni)
- Struttura server corretta: `C:\MESManager\` (Web root), `C:\MESManager\Worker\`, `C:\MESManager\PlcSync\`

### Soluzione
Corretta documentazione in [01-DEPLOY.md](../01-DEPLOY.md):

```powershell
# Copia Web (escludendo sottocartelle Worker/PlcSync)
robocopy "publish\Web" "\\192.168.1.230\c$\MESManager" /E /XD Worker PlcSync logs ...

# Copia Worker separatamente
robocopy "publish\Worker" "\\192.168.1.230\c$\MESManager\Worker" /E /XF ... /XD logs

# Copia PlcSync separatamente
robocopy "publish\PlcSync" "\\192.168.1.230\c$\MESManager\PlcSync" /E /XF ... /XD logs
```

### Lezione Appresa
- ✅ Documentazione deve riflettere TUTTI i passaggi necessari
- ✅ Se publish in cartelle separate, OGNI cartella deve essere copiata esplicitamente
- ✅ Esclusione `/XD Worker PlcSync` da copia Web previene sovrascrittura accidentale
- ✅ Validare documentazione eseguendo script step-by-step

---

## 🔴 PROBLEMA 5: Nomi Clienti Errati - Mostra Fornitori Invece di Clienti

**Deploy**: v1.30.11 (10-11 Feb 2026)

### Sintomo
Campi cliente in UI mostravano nomi sbagliati:
- Commessa per cliente "FONDERIA ZARDO" → mostrava "IMMOBILIARE LUPIOLA" (fornitore!)
- Commessa per cliente "ANIMA S.R.L." → mostrava "MULLER ANDRE MARCEL" (persona fisica!)
- **10 commesse su 15 avevano nomi errati**

### Root Cause (CRITICO - Data Quality)
**Due campi cliente in commessa**:
- `ClienteRagioneSociale` - dalla tabella locale `Clienti` (sync separato) → DATI STALE/ERRATI
- `CompanyName` - da query Mago diretta via JOIN su `MA_CustSupp` → DATI CORRETTI

**Tabella Clienti locale**: Sync rotto o mai implementato correttamente  
**Mago MA_CustSupp**: ERP è source of truth ASSOLUTA

### SQL Evidence
Query di verifica eseguita (10 Feb 2026):

```sql
SELECT 
    c.NumeroOrdine,
    c.ClienteRagioneSociale AS 'Locale_Cliente',
    c.CompanyName AS 'Mago_Cliente'
FROM Commesse c
WHERE c.Stato IN ('Aperta', 'InLavorazione')
ORDER BY c.DataConsegna

-- Risultato: 10/15 commesse con nomi DIVERSI
-- Esempio 1: Ord 9900336663
--   Locale: IMMOBILIARE LUPIOLA ❌
--   Mago:   FONDERIA ZARDO S.P.A. ✅
```

### Analisi MagoRepository.cs
```csharp
// Query sync commesse (CORRETTA - mai modificare questo filtro!)
LEFT JOIN MA_CustSupp C ON C.CustSupp = SO.Customer 
    AND C.CustSuppType = 3211264  // ✅ ESSENZIALE: solo CLIENTI

// CompanyName viene da questa JOIN filtrata
// Se filtro rimosso → include fornitori → DISASTRO!
```

### Errore Commesso (e Corretto)
1. ❌ Primo tentativo: cambiato UI da `CompanyName` → `ClienteRagioneSociale` (peggiorato!)
2. ❌ Secondo tentativo: rimosso filtro `CustSuppType = 3211264` (includeva fornitori!)
3. ✅ **Soluzione finale**: 
   - Ripristinato filtro `CustSuppType = 3211264` (ESSENZIALE)
   - Cambiato TUTTA l'UI da `ClienteRagioneSociale` → `CompanyName`

### Lezione Appresa (⚠️ CRITICA - Da Non Dimenticare MAI)
- ✅ **Mago (ERP) = Source of Truth ASSOLUTA** per dati anagrafici
- ✅ Campo `CompanyName` da Mago è SEMPRE più affidabile di tabelle locali
- ✅ Filtro `CustSuppType = 3211264` è **VITALE**: distingue clienti (3211264) da fornitori
- ✅ Mai fidarsi della tabella `Clienti` locale senza verificare sync funzionante
- ⚠️ Se local != Mago → Mago ha ragione (salvo prove schiaccianti contrarie)
- ✅ Test con query di confronto locale vs Mago prima di ogni deploy cliente-sensitive

### Prevenzione
Query sanity check pre-deploy (eseguire su DB prod):

```sql
-- Deve restituire 0 righe con differenze significative
SELECT c.NumeroOrdine, c.ClienteRagioneSociale, c.CompanyName
FROM Commesse c
WHERE c.ClienteRagioneSociale != c.CompanyName
  AND c.Stato IN ('Aperta', 'InLavorazione')
  AND c.CompanyName IS NOT NULL;
```

### File Modificati
- `MagoRepository.cs`: Filtro `CustSuppType = 3211264` RIPRISTINATO
- `commesse-aperte-grid.js`: Campo → `companyName`
- `commesse-grid.js`: Colonna → `CompanyName`
- `CommesseAperte.razor`: Tutte le 5 occorrenze
- `CommessaDto.cs`: Validazione usa `CompanyName`

---

## 🟡 PROBLEMA 6: Preferenze Utente (Colonne Grid) Resettate

**Deploy**: v1.30.11 (11 Feb 2026)

### Sintomo
Dopo deploy, utenti segnalano colonne grid tornate a default:
- Ordine colonne perso
- Larghezze colonne perse
- Colonne nascoste/visibili resettate

### Root Cause (Data Persistence & JSON Incompatibility)
- **Dove**: Tabella `PreferenzeUtente` (database prod) con chiave + valore JSON
- **Come**: `PreferencesService` in Blazor salva stato colonne AG Grid serializzato
- **Problema**: Deploy cambia **nomi campi** nelle grid → JSON salvato diventa **INCOMPATIBILE**
- **Esempio v1.30.11**: Campo `ClienteRagioneSociale` → `CompanyName` → stati salvati invalidati

### Evidence
JSON salvato conteneva:
```json
{"field": "ClienteRagioneSociale", "width": 200, "pinned": "left"}
```

Dopo deploy: campo `ClienteRagioneSociale` NON esiste più → AG Grid ignora JSON → colonne default

### Soluzione Implementata
**MIGRAZIONE SQL DIRETTA** (più semplice degli script PowerShell):

```sql
-- Migrazione campo rinominato (es: ClienteRagioneSociale → CompanyName)
UPDATE PreferenzeUtente
SET ValoreJson = REPLACE(ValoreJson, 'ClienteRagioneSociale', 'CompanyName')
WHERE Chiave LIKE '%grid%' 
  AND ValoreJson LIKE '%ClienteRagioneSociale%';
```

**Risultato**: Tutte le preferenze recuperate! Utenti ricaricarono browser (Ctrl+Shift+R) e colonne tornarono come prima.

### Alternativa: Script Backup/Restore
1. **Script Backup Pre-Deploy**: `scripts/backup-preferenze-utente.ps1`
2. **Script Restore Post-Deploy**: `scripts/restore-preferenze-utente.ps1 -SkipGridStates`

### Lezione Appresa
- ✅ **SEMPRE backup preferenze** prima di deploy che modifica UI
- ✅ **Migrazione SQL REPLACE** recupera preferenze invece di resettarle (preferito!)
- ✅ Se cambi nomi campi DTO usati in grid → usa SQL UPDATE o skip grid states
- ✅ File backup small (~50KB), tenerli per 1 mese
- ✅ Documentare nel CHANGELOG se deploy resetta grid (avvisare utenti)

### Prevenzione Future
- Evitare rename campi DTO se possibile (usa alias/mapping invece)
- Se DEVI rinominare: comunicare 1-2 giorni prima agli utenti
- Testare migrazione SQL in dev prima di produzione

### File Modificati
- `scripts/backup-preferenze-utente.ps1` - Script backup
- `scripts/restore-preferenze-utente.ps1` - Script restore
- `docs2/01-DEPLOY.md` - Aggiunto procedura backup/restore

---

## ✅ CHECKLIST PRE-DEPLOY AGGIORNATA

Basata sulle 6 lezioni apprese:

```
[ ] Build: dotnet build --nologo (0 errori)
[ ] Versione: AppVersion.cs incrementata
[ ] CHANGELOG: v1.X.Y aggiunto
[ ] ⭐ BACKUP PREFERENZE: scripts\backup-preferenze-utente.ps1 (se deploy modifica grid/DTO)
[ ] DB Prod: Verifica dati base (ImpostazioniProduzione, CalendarioLavoro, Macchine)
[ ] DB Prod: Almeno 1 macchina con AttivaInGantt = true
[ ] DB Prod: Query sanity check (se deploy tocca dati cliente/Mago)
[ ] Column States: Test "Fix" → refresh → stato ripristinato (in dev)
[ ] Publish: dotnet publish Web/Worker/PlcSync
[ ] Deploy: robocopy TUTTI i servizi (Web + Worker + PlcSync)
[ ] Servizi: Stop PlcSync → Worker → Web, poi Start Web → Worker → PlcSync
[ ] HTTP 200: https://192.168.1.230:5156 risponde
[ ] Versione UI: Footer mostra v1.X.Y
[ ] Smoke Test: Login, navigazione pagine principali
[ ] Smoke Test: "Carica su Gantt" distribuisce su più macchine
[ ] Smoke Test: Righe assegnate sono verde chiaro
[ ] Smoke Test: Nomi cliente corretti (Mago CompanyName)
[ ] ⭐ RESTORE/MIGRATION: Se cambi campi → SQL UPDATE REPLACE o script restore
[ ] Comunicazione utenti: avvisare se devono riconfigurare grid
[ ] ⭐ Sync Mago: Manuale e Automatica devono puntare stesso DB (verifica log)
```

---

## 🔴 PROBLEMA 7: Sync Automatica Fallisce (Worker vs Web Database Diversi)

**Data**: 19 Febbraio 2026

### Sintomo
- Sincronizzazione **manuale** da UI Blazor (pulsante "Sincronizza") → **funzionava** ✅
- Sincronizzazione **automatica** dal Worker background service → **falliva con errore SQL** ❌

### Root Cause
**Worker e Web leggevano file configurazione DIVERSI**:

| Componente | File Config | Database Target | Risultato |
|------------|-------------|-----------------|-----------|
| **Web** | `MESManager.Web/appsettings.Secrets.json` | `MESManager_Dev` su `localhost` | ✅ OK |
| **Worker** | `appsettings.Database.json` (root) | `MESManager_Prod` su `192.168.1.230` | ❌ FAIL |

**Problema**: In ambiente DEV, il database produzione (`192.168.1.230`) non era raggiungibile, causando errore:

```
Si è verificato un errore di rete o specifico dell'istanza mentre si cercava di stabilire 
una connessione con SQL Server. Il server non è stato trovato o non è accessibile. 
(provider: Interfacce di rete SQL, error: 26)
```

### Soluzione Implementata

**1. Unificato caricamento configurazione nel Worker** ([MESManager.Worker/Program.cs](../../MESManager.Worker/Program.cs))

**Prima** (❌):
```csharp
// Worker caricava SOLO appsettings.Database.json
builder.Configuration.AddJsonFile(
    Path.Combine(Directory.GetParent(builder.Environment.ContentRootPath)!.FullName, 
    "appsettings.Database.json"), 
    optional: false, reloadOnChange: true);
```

**Dopo** (✅):
```csharp
// Usa stessa logica del Web: Secrets.json > Database.json
var solutionRoot = Directory.GetParent(builder.Environment.ContentRootPath)!.FullName;
var secretsPath = Path.Combine(solutionRoot, "appsettings.Secrets.json");
var dbConfigPath = Path.Combine(solutionRoot, "appsettings.Database.json");

if (File.Exists(secretsPath))
{
    // Preferito: usa secrets condiviso con Web
    builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: true);
}
else if (File.Exists(dbConfigPath))
{
    // Fallback legacy per produzione
    builder.Configuration.AddJsonFile(dbConfigPath, optional: false, reloadOnChange: true);
}
```

**2. Popolato `appsettings.Secrets.json` nella root** ([appsettings.Secrets.json](../../appsettings.Secrets.json))

Prima era vuoto, ora contiene:
```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager_Dev;Integrated Security=True;TrustServerCertificate=True;",
    "MagoDb": "Server=192.168.1.72\\SQLEXPRESS01;Database=TODESCATO_NET;User Id=Gantt;Password=Gantt2019;TrustServerCertificate=True;Connection Timeout=30;"
  }
}
```

**3. Fix Dependency Injection Worker** ([MESManager.Worker/Program.cs](../../MESManager.Worker/Program.cs))

Aggiunto servizio mancante richiesto da Infrastructure:
```csharp
// Servizi Application mancanti richiesti da Infrastructure
builder.Services.AddScoped<IPianificazioneService, PianificazioneService>();
```

### Risultati Verifica

Dopo il fix, Worker avviato con successo:

```
info: MESManager.Worker.SyncMagoWorker[0]
      Inizio sincronizzazione Mago alle 02/19/2026 11:48:07 +01:00
info: MESManager.Worker.SyncMagoWorker[0]
      Sincronizzazione Mago completata alle 02/19/2026 11:48:21 +01:00
```

✅ Worker e Web ora condividono stesso database DEV  
✅ Sync automatica funziona correttamente  
✅ Log salvati su `MESManager_Dev`

### Lezione Appresa

**❌ MAI fare questo:**
- Configurazione database duplicata tra Web e Worker
- File config diversi per stesso ambiente
- Assumere che "funziona in manuale = funziona in automatico"

**✅ SEMPRE fare questo:**
- **Un solo file configurazione** per ambiente (preferibilmente `appsettings.Secrets.json`)
- **Stessa logica caricamento config** per tutti i servizi (Web, Worker, PlcSync)
- **Testare ENTRAMBE** le modalità (manuale UI + automatica Worker)
- **Verificare log Worker** con `dotnet run --project MESManager.Worker --environment Development`

### File Modificati

| File | Modifica | Scopo |
|------|----------|-------|
| `MESManager.Worker/Program.cs` | Logica caricamento config unificata | Usa Secrets.json come Web |
| `appsettings.Secrets.json` (root) | Popolato con credenziali DEV | Condiviso tra Web/Worker |
| `MESManager.Worker/Program.cs` | Registrazione `IPianificazioneService` | Fix DI mancante |

### Validazione Post-Fix

```powershell
# 1. Build Worker
cd C:\Dev\MESManager
dotnet build MESManager.Worker/MESManager.Worker.csproj --nologo

# 2. Avvia Worker manualmente
cd C:\Dev
dotnet run --project MESManager/MESManager.Worker/MESManager.Worker.csproj --environment Development

# 3. Verifica log - deve mostrare:
# ✅ "Inizio sincronizzazione Mago"
# ✅ "Sincronizzazione Mago completata"
# ❌ NESSUN errore "server non trovato"
```

---

## 📚 RIFERIMENTI

- [01-DEPLOY.md](../01-DEPLOY.md) - Procedura deploy completa
- [03-CONFIGURAZIONE.md](../03-CONFIGURAZIONE.md) - Gestione secrets e config
- [09-TESTING-FRAMEWORK.md](../09-TESTING-FRAMEWORK.md) - Testing e validazione
- [08-CHANGELOG.md](../08-CHANGELOG.md) - Storico versioni

---

**Versione**: 1.1  
**Data Creazione**: 11 Febbraio 2026  
**Ultimo Aggiornamento**: 19 Febbraio 2026 - Aggiunto Problema 7 (Worker config)  
**Manutenzione**: Aggiungere nuove lezioni man mano che emergono
