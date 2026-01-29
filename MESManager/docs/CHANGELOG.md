# 📋 Changelog Versioni MESManager

## v1.14 (29 Gennaio 2026)
**Data:** 29 Gen 2026  
**Status:** ✅ Deploy completato

### Modifiche UI/UX

#### 🎯 Dashboard e PLC Realtime - Filtro Macchine con IP
- Le pagine Dashboard Produzione e PLC Realtime ora mostrano **solo le macchine con indirizzo IP configurato** nelle impostazioni Gantt Macchine
- Filtro applicato in `PlcAppService.GetRealtimeDataAsync()` con `.Where(p => !string.IsNullOrWhiteSpace(p.Macchina.IndirizzoPLC))`

#### 📏 Ottimizzazione Altezza Righe Tabelle
- **PLC Storico**: Ridotta altezza header a 24px e righe a 28px per visualizzare più dati
- **Catalogo Articoli**: Stessa ottimizzazione (headerHeight: 24, rowHeight: 28)
- **Catalogo Clienti**: Stessa ottimizzazione (headerHeight: 24, rowHeight: 28)
- Allineamento al layout già usato su Commesse Aperte

#### 🔧 Fix Doppio Clic Modifica Anima su Commesse
- Corretto handler `onRowDoubleClicked` che non funzionava più
- Problema: `event.colDef.field` poteva essere undefined per alcune colonne
- Soluzione: Uso di `event.column?.getColId?.() || event.colDef?.field` con optional chaining

#### 🏷️ Pulsante Stampa Etichetta su Programma Macchine
- Aggiunta colonna "Stampa Etichetta" con icona stampante 🖨️
- Implementata stessa funzionalità già presente su Commesse Aperte:
  - Dialog preview etichetta con foto articolo da API ERP
  - Validazione campi obbligatori (Peso, Fili, Grammatura, Anime)
  - Stampa etichetta con dati completi o vuota
  - Redirect ad anime se dati mancanti

### File Modificati
- `MESManager.Infrastructure/Services/PlcAppService.cs` - Filtro IP macchine
- `MESManager.Web/wwwroot/lib/ag-grid/plc-storico-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/js/articoli-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/js/clienti-grid.js` - Altezza righe ottimizzata
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` - Fix doppio clic
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` - Colonna stampa etichetta
- `MESManager.Web/Components/Pages/Programma/ProgrammaMacchine.razor` - Logica stampa etichetta

---

## v1.13 (29 Gennaio 2026)
**Data:** 29 Gen 2026 16:10 UTC  
**Status:** ✅ Deploy completato e verificato su 192.168.1.230  
**Commit:** `83fe425`

### Modifiche
- ✅ **Imballo Descrizione**: Ora mostra la descrizione invece del numero nelle griglie
- ✅ **Colonne Anime Unificate**: Creato file `anime-columns-shared.js` come fonte unica per entrambe le griglie
- ✅ **Nuovi campi CommessaDto**: Aggiunti `ImballoDescrizione`, `MacchineSuDisponibiliDescrizione`, `Figure`, `Maschere`, `Assemblata`, `ArmataL`, `TogliereSparo`
- ✅ **CommessaAppService**: Aggiornato per popolare le descrizioni dai lookup tables
- ✅ **No code duplication**: Commesse Aperte e Programma Macchine usano la stessa fonte per le colonne anime
- ✅ **Fix Ordine Colonne Stampa**: La stampa ora rispetta l'ordine delle colonne come visualizzato nella griglia
- ✅ **Fix Refresh Commesse Chiuse**: Dopo spostamento frecce, non appaiono più commesse chiuse da Mago

### Dettaglio Fix Stampa
**Problema:** L'ordine delle colonne nella stampa non corrispondeva all'ordine impostato dall'utente nella griglia.

**Causa:** Le funzioni di stampa (`printViaIframe`, `printInNewWindow`, `generatePrintTable`) usavano `gridApi.getColumns()` che restituisce le colonne nell'ordine della definizione originale, non nell'ordine attuale dopo drag & drop.

**Soluzione:** Sostituito `gridApi.getColumns()` con `gridApi.getAllDisplayedColumns()` che rispetta l'ordine attuale delle colonne come visualizzato dall'utente.

### Dettaglio Fix Refresh Commesse Chiuse
**Problema:** Dopo aver spostato una commessa con le frecce ▲▼, apparivano commesse con `stato = "Chiusa"` (chiuse da Mago), che poi sparivano premendo "Aggiorna".

**Causa:** La funzione JS `refreshGridData()` chiamava `/api/Commesse` e filtrava solo per `statoProgramma !== 'Archiviata'`, ma NON controllava `stato === 'Aperta'`.

**Soluzione:** Aggiunto filtro `c.stato === 'Aperta'` in `refreshGridData()` per allineare il comportamento con il caricamento iniziale Blazor.

**Regola acquisita:** Quando si filtra dati in più punti (Blazor + JS), **i filtri devono essere identici** per evitare discrepanze.

### File Modificati
- `MESManager.Application/DTOs/CommessaDto.cs` - Aggiunti 7 campi mancanti
- `MESManager.Infrastructure/Services/CommessaAppService.cs` - Popolamento descrizioni
- `MESManager.Web/wwwroot/lib/ag-grid/anime-columns-shared.js` - NUOVO: colonne condivise
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` - Usa colonne condivise
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` - Usa colonne condivise + fix stampa + fix refresh
- `MESManager.Web/Components/App.razor` - Caricamento script condiviso
- `MESManager.Web/Controllers/PlcController.cs` - Rimosso warning variabile non usata

### Comandi Deploy Usati
```powershell
# Build
dotnet build MESManager.sln -c Release --nologo

# Publish
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Stop servizio
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Copia file (esclude config sensibili)
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager\Web" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs

# Avvia servizio
$secpwd = ConvertTo-SecureString "A123456!" -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential("Administrator", $secpwd)
Invoke-WmiMethod -ComputerName 192.168.1.230 -Credential $cred -Class Win32_Process -Name Create -ArgumentList "C:\MESManager\Web\MESManager.Web.exe", "C:\MESManager\Web"

# Verifica
Invoke-WebRequest -Uri "http://192.168.1.230:5156" -UseBasicParsing -TimeoutSec 10
```

---

## v1.12 (29 Gennaio 2026)
**Data:** 29 Gen 2026  
**Status:** ✅ Commit `aa4f926`

### Correzioni
- ✅ **Column State Persistence**: Stato colonne salvato da Blazor su DB, JS ora legge da Blazor
- ✅ **Arrow Buttons Fix**: Frecce per riordino commesse funzionanti dopo refresh

---

## v1.11 (29 Gennaio 2026)
**Data:** 29 Gen 2026 10:52 UTC  
**Status:** ✅ Deploy completato su 192.168.1.230  
**Versione mostrata:** Angolo basso-destra pagina

### Correzioni Critiche
- ✅ **Machine Number Selection**: Selezione macchina in "Commesse Aperte" ora salva con formato M001
- ✅ **Database Standardization**: Tutte le anime e commesse aggiornate a formato M001
- ✅ **AG Grid Editor**: Integrato `agSelectCellEditor` nativo per dropdown macchina
- ✅ **Drag & Drop**: Riordino commesse sulla stessa macchina funzionante
- ✅ **UX Fix**: Disabilitato `user-select` al doppio-click (non seleziona più testo)

### File Modificati
- `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js` (34013 bytes)
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js` (51809 bytes)
- `MESManager.Web/Components/Layout/MainLayout.razor` (+versione v1.11)
- SQL: `scripts/utilities/fix-numero-macchina-standard.sql`

### Comandi Deploy Usati
```powershell
# Build
dotnet build MESManager.sln -c Release --nologo

# Publish
dotnet publish MESManager.Web/MESManager.Web.csproj -c Release -o publish/Web --nologo

# Stop
taskkill /S 192.168.1.230 /U Administrator /P "A123456!" /IM MESManager.Web.exe /F

# Copy (CRITICO: escludere appsettings.Secrets.json e appsettings.Database.json)
robocopy "C:\Dev\MESManager\publish\Web" "\\192.168.1.230\c$\MESManager" /E /XF appsettings.Secrets.json appsettings.Database.json *.log *.pdb /XD logs SyncBackups

# Start
schtasks /S 192.168.1.230 /U Administrator /P "A123456!" /Run /TN "StartMESWeb"
```

### Database SQL
```sql
-- Eseguito su MESManager_Prod
-- Script: fix-numero-macchina-standard.sql
-- Aggiornati: 897 anime, 19 commesse
```

### Insegnamenti Acquisiti
🔴 **Errore 1:** Copiare file in `C:\MESManager\Web\wwwroot\` (sbagliato)  
✅ **Soluzione:** Copiare SEMPRE in `C:\MESManager\wwwroot\` (ROOT)

🔴 **Errore 2:** Sovrascrivere `appsettings.Database.json` e `appsettings.Secrets.json`  
✅ **Soluzione:** Usare `robocopy /XF` per escludere file critici

🔴 **Errore 3:** Non incrementare versione prima del deploy  
✅ **Soluzione:** Modificare MainLayout.razor prima di compilare

---

## Formato Versioning

- **Pattern:** `vX.Y`
- **X (Major):** Cambio architettura/schema database (raro)
- **Y (Minor):** Incrementa ad ogni deploy

### Prossimo Deploy
Quando farai il prossimo aggiornamento:
1. Cambia `v1.11` → `v1.12` in MainLayout.razor
2. Aggiungi sezione qui in CHANGELOG.md
3. Esegui script deploy completo
4. Comunica versione ai clienti

