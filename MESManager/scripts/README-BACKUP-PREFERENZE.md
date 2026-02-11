# Script Backup/Restore Preferenze Utente

Script PowerShell per salvare e ripristinare le preferenze utente (stati colonne grid, preferenze UI) dal database di produzione.

## 📋 Indice

- [backup-preferenze-utente.ps1](#backup-preferenze-utenteps1) - Salva preferenze da DB prod
- [restore-preferenze-utente.ps1](#restore-preferenze-utenteps1) - Ripristina preferenze da backup

---

## 🎯 Quando Usare

### ✅ SEMPRE fare backup prima di deploy che:
- Modifica nomi campi DTO usati in grid (es: `ClienteRagioneSociale` → `CompanyName`)
- Aggiunge/rimuove colonne nelle grid
- Cambia struttura dati visualizzati
- Modifica entity/DTO usati in Blazor components

### ⚠️ OPZIONALE ma raccomandato:
- Deploy di sole modifiche backend/logica
- Deploy di nuove feature non correlate a grid esistenti
- Precauzione generale (il backup è veloce, < 5 secondi)

---

## backup-preferenze-utente.ps1

### Descrizione
Estrae tutte le preferenze dalla tabella `PreferenzeUtente` del database di produzione e le salva in un file JSON timestampato.

### Uso

```powershell
cd C:\Dev\MESManager\scripts
.\backup-preferenze-utente.ps1
```

### Output

```
================================================
BACKUP PREFERENZE UTENTE - Database Produzione
================================================

[1/3] Connessione a 192.168.1.230\SQLEXPRESS01...
[2/3] Salvataggio in: C:\Dev\MESManager\scripts\backup-preferenze\preferenze_20260211_085500.json
[3/3] ✅ Backup completato!

Record salvati: 45
File: backup-preferenze\preferenze_20260211_085500.json
Dimensione: 127.3 KB

Preferenze per utente:
  - admin: 12 preferenze
  - operatore1: 8 preferenze
  - operatore2: 6 preferenze
```

### File Generato

- **Path**: `C:\Dev\MESManager\scripts\backup-preferenze\preferenze_YYYYMMDD_HHmmss.json`
- **Formato**: JSON array con oggetti:
  ```json
  [
    {
      "Id": "guid-here",
      "UtenteId": "user-guid",
      "Username": "admin",
      "Key": "commesse-aperte-grid-settings",
      "Value": "{\"FontSize\":14,\"RowHeight\":32,...}",
      "DataCreazione": "2026-01-15T...",
      "DataUltimaModifica": "2026-02-10T..."
    },
    ...
  ]
  ```

### Note
- Il file di backup contiene anche password/token se salvati in preferenze (attualmente solo grid settings)
- Mantieni i backup per almeno 1 mese prima di cancellarli
- Dimensione tipica: 50-200 KB (dipende da numero utenti e preferenze)

---

## restore-preferenze-utente.ps1

### Descrizione
Ripristina le preferenze utente dal file di backup più recente (o specificato) nel database di produzione.

### Uso Base (Restore Completo)

Ripristina TUTTE le preferenze, inclusi stati colonne. **Usare solo se il deploy NON ha modificato le colonne grid**.

```powershell
cd C:\Dev\MESManager\scripts

# Usa il backup più recente
.\restore-preferenze-utente.ps1

# Oppure specifica un backup
.\restore-preferenze-utente.ps1 -BackupFile "backup-preferenze\preferenze_20260211_085500.json"
```

### Uso Avanzato (Skip Grid States)

Ripristina solo preferenze UI **NON** legate alle colonne. **Usare quando il deploy ha modificato nomi campi o struttura colonne**.

```powershell
# Salta gli stati grid (ordine/larghezza colonne)
.\restore-preferenze-utente.ps1 -SkipGridStates
```

### Parametri

| Parametro | Tipo | Descrizione |
|-----------|------|-------------|
| `-BackupFile` | string | Path del file backup (opzionale, default = più recente) |
| `-SkipGridStates` | switch | Salta preferenze grid incompatibili (ordine colonne, larghezze, visibilità) |

### Output Restore Completo

```
================================================
RESTORE PREFERENZE UTENTE - Database Produzione
================================================

📁 Usando backup più recente: preferenze_20260211_085500.json

[1/4] Lettura backup: backup-preferenze\preferenze_20260211_085500.json
      Record totali nel backup: 45
[2/4] Connessione a database produzione...
[3/4] Ripristino preferenze...
  ✓ admin - commesse-aperte-grid-settings
  ✓ admin - commesse-aperte-grid-fixed-state
  ✓ admin - programma-macchine-grid-settings
  ...

[4/4] ✅ Restore completato!

Riepilogo:
  - Ripristinate: 45

================================================
Preferenze ripristinate. Aggiorna il browser (Ctrl+F5).
================================================
```

### Output Restore con -SkipGridStates

```
================================================
RESTORE PREFERENZE UTENTE - Database Produzione
================================================

📁 Usando backup più recente: preferenze_20260211_085500.json

[1/4] Lettura backup: backup-preferenze\preferenze_20260211_085500.json
      Record totali nel backup: 45

⚠️  SKIP GRID STATES attivo - stati colonne NON verranno ripristinati
      Record da ripristinare: 22

[2/4] Connessione a database produzione...
[3/4] Ripristino preferenze...
  ✓ admin - ui-theme
  ✓ admin - font-size-global
  ✓ operatore1 - density-preference
  ...

[4/4] ✅ Restore completato!

Riepilogo:
  - Ripristinate: 22
  - Saltate (grid states): 23

================================================
⚠️  Gli utenti dovranno riconfigurare le colonne delle grid!
================================================
```

### Chiavi Saltate con -SkipGridStates

Quando usi `-SkipGridStates`, lo script NON ripristina queste chiavi:

- `commesse-aperte-grid-fixed-state`
- `commesse-aperte-grid-settings`
- `commesse-grid-fixed-state`
- `commesse-grid-settings`
- `programma-macchine-grid-fixed-state`
- `programma-macchine-grid-settings`
- `anime-grid-fixed-state`
- `anime-grid-settings`
- `articoli-grid-fixed-state`
- `articoli-grid-settings`
- `clienti-grid-fixed-state`
- `clienti-grid-settings`

### Note
- Lo script fa **UPSERT**: aggiorna se esiste, inserisce se non esiste
- Non cancella preferenze non presenti nel backup (merge, non replace)
- Gli utenti devono fare **Ctrl+Shift+R** (hard refresh) per vedere le preferenze ripristinate

---

## 🔄 Workflow Tipico Deploy

### Scenario A: Deploy con Modifiche Colonne

```powershell
# 1. Backup pre-deploy
cd C:\Dev\MESManager\scripts
.\backup-preferenze-utente.ps1

# 2. Deploy normale (build, publish, stop, copy, start)
# ... vedi docs2/01-DEPLOY.md ...

# 3. Restore parziale (salta grid states incompatibili)
.\restore-preferenze-utente.ps1 -SkipGridStates

# 4. Comunicare agli utenti via email/Slack:
#    "Dopo il deploy, dovrete riordinare le colonne delle grid e cliccare 'Fix'"
```

### Scenario B: Deploy senza Modifiche Colonne

```powershell
# 1. Backup (opzionale ma raccomandato)
.\backup-preferenze-utente.ps1

# 2. Deploy normale
# ...

# 3. Restore completo (include grid states)
.\restore-preferenze-utente.ps1

# Gli utenti non noteranno differenze
```

### Scenario C: Rollback a Backup Precedente

```powershell
# Lista backup disponibili
Get-ChildItem backup-preferenze\preferenze_*.json | Sort-Object LastWriteTime -Descending

# Restore da backup specifico (es: pre-v1.30)
.\restore-preferenze-utente.ps1 -BackupFile "backup-preferenze\preferenze_20260201_143000.json"
```

### Scenario D: Migrazione Campo Rinominato (SQL Diretto)

Se hai cambiato il nome di un campo (es: `ClienteRagioneSociale` → `CompanyName`) e vuoi migrare le preferenze invece di resettarle:

```powershell
# Migrazione SQL diretta (più affidabile degli script PowerShell)
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
    -U "FAB" -P "password.123" -C `
    -Q "UPDATE PreferenzeUtente 
        SET ValoreJson = REPLACE(ValoreJson, 'ClienteRagioneSociale', 'CompanyName') 
        WHERE Chiave LIKE '%grid%' 
          AND ValoreJson LIKE '%ClienteRagioneSociale%'; 
        SELECT @@ROWCOUNT AS 'Preferenze Migrate';"
```

**Verifica migrazione**:
```powershell
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
    -U "FAB" -P "password.123" -C `
    -Q "SELECT u.Nome, pu.Chiave, 
        CASE 
          WHEN pu.ValoreJson LIKE '%VecchioCampo%' THEN '❌ DA MIGRARE'
          WHEN pu.ValoreJson LIKE '%NuovoCampo%' THEN '✅ MIGRATO'
          ELSE 'N/A'
        END AS Stato
        FROM PreferenzeUtente pu
        INNER JOIN UtentiApp u ON u.Id = pu.UtenteAppId
        WHERE pu.Chiave LIKE '%grid%'
        ORDER BY u.Nome;" -W
```

**Dopo migrazione**:
- Comunicare agli utenti: "Ricaricate il browser (Ctrl+Shift+R)"
- Le colonne torneranno esattamente come prima!

---

## 🛠️ Troubleshooting

### ❌ "Errore sqlcmd: L'accesso non è riuscito per l'utente"

**Causa**: Credenziali database sbagliate in `backup-preferenze-utente.ps1` o `restore-preferenze-utente.ps1`

**Soluzione**: Verifica user/password in:
```powershell
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" `
    -U "MesManagerApp" -P "MesManager2024!" ...
```

Controlla in `appsettings.Production.json` le credenziali corrette.

### ❌ "Impossibile trovare il file specificato"

**Causa**: Path backup non trovato

**Soluzione**:
```powershell
# Crea directory backup se non esiste
New-Item -ItemType Directory -Path "C:\Dev\MESManager\scripts\backup-preferenze" -Force
```

### ❌ "Risultato query non è JSON valido"

**Causa**: Query restituisce errore SQL invece di JSON

**Soluzione**: Esegui manualmente la query in SSMS per vedere l'errore:
```sql
USE MESManager_Prod;
SELECT * FROM PreferenzeUtente;
```

### ⚠️ Restore OK ma colonne ancora resettate

**Causa**: Browser ha cache, o restore ha usato `-SkipGridStates`

**Soluzione**:
1. Fai **Ctrl+Shift+R** (hard refresh) nel browser
2. Verifica che il restore non abbia usato `-SkipGridStates` se volevi restore completo
3. Controlla `localStorage` browser: F12 → Application → Local Storage → verifica chiavi `commesse-aperte-grid-settings`

---

## 📚 Vedi Anche

- [docs2/01-DEPLOY.md](../../docs2/01-DEPLOY.md) - Procedura deploy completa (include STEP 2.5 backup e STEP 7 restore)
- [docs2/BIBBIA-AI-MESMANAGER.md](../../docs2/BIBBIA-AI-MESMANAGER.md) - Problema 6: Preferenze Utente Resettate

---

## 📝 Note Sviluppo

- Gli script usano `sqlcmd` (installato con SQL Server Client Tools)
- Compatibili con PowerShell 5.1+ e PowerShell 7+
- Testati su Windows Server 2022 e Windows 11
- JSON è salvato come UTF-8 (supporta caratteri speciali italiani)

**Manutenzione**:
- Aggiungere nuove chiavi grid in `$gridStateKeys` array se crei nuove grid
- Considera retention policy: cancella backup > 3 mesi
- Backup size tipico: 50-200KB (1 anno di backup = ~2-5MB → trascurabile)
