# 🧪 Testing Infrastructure - MESManager

## Panoramica

Sistema di test automatizzati per verificare lo stato e il funzionamento delle API di MESManager senza interferire con l'applicazione in esecuzione.

## File Principali

### start-web.ps1
Avvia l'applicazione MESManager in un processo completamente isolato.

**Uso**:
```powershell
.\start-web.ps1
```

**Cosa fa**:
- Ferma eventuali istanze precedenti dell'app
- Avvia `dotnet run` in un nuovo processo con `UseShellExecute = $true`
- Mostra PID del processo per gestione manuale
- **NON** blocca il terminale corrente

**Output**:
```
=== Avvio MESManager Web ===
Directory: C:\Dev\MESManager
Progetto: MESManager.Web\MESManager.Web.csproj

✓ Applicazione avviata (PID: 12345)

Attendi 10 secondi che l'app sia completamente avviata...
Poi puoi accedere a: http://localhost:5156

Per testare le API: .\test-api.ps1

Per fermare l'app, chiudi la finestra del terminale o:
  Stop-Process -Id 12345
```

---

### test-api.ps1
Suite di test automatici per le API di MESManager.

**Prerequisito**: Applicazione avviata con `.\start-web.ps1` e attesa di 10 secondi

**Uso**:
```powershell
.\test-api.ps1
```

**Test Eseguiti**:

#### 1. Debug Commesse
Endpoint: `GET /api/pianificazione/debug-commesse`

Verifica conteggi critici nel database:
- **totaleCommesse**: Tutte le commesse
- **conMacchina**: Commesse con `NumeroMacchina` assegnato
- **conDate**: Commesse con `DataInizioPrevisione`
- **conMacchinaEDate**: Commesse esportabili (macchina E date)
- **statoProgrammataProgrammata**: Commesse con `StatoProgramma = Programmata`
- **statoAperta**: Commesse con `Stato = Aperta`
- **aperteConMacchina**: Commesse aperte con macchina assegnata

#### 2. Lista Commesse
Endpoint: `GET /api/Commesse`

Carica tutte le commesse e mostra le prime 3 con:
- Codice
- NumeroMacchina
- StatoProgramma
- Stato

#### 3. Filtro Programma Macchine
Filtro lato client (come fa ProgrammaMacchine.razor):

```csharp
Stato == "Aperta" AND NumeroMacchina != null AND StatoProgramma != "Archiviata"
```

**NOTA IMPORTANTE**: Non esiste endpoint `/api/pianificazione/programma-macchine`.  
La pagina Programma Macchine filtra lato client i dati di `/api/Commesse`.

Diagnostica:
- Conta commesse che dovrebbero apparire in Programma Macchine
- Identifica se ci sono commesse archiviate
- Mostra prime 3 commesse programmate

#### 4. Export
Endpoint: `POST /api/pianificazione/esporta-su-programma`

Test interattivo (richiede INVIO):
- Esporta commesse con `NumeroMacchina` E `DataInizioPrevisione`
- Cambia `StatoProgramma` da `NonProgrammata` a `Programmata`
- Mostra conteggio prima/dopo

**Messaggio atteso**:
```
✓ Export completato: X commesse esportate (Y totali)
```

- `X` = Commesse effettivamente cambiate (erano NonProgrammata)
- `Y` = Commesse totali trovate (con macchina E date)

Se `X = 0` e `Y > 0`: Tutte le commesse erano già Programmate (normale dopo export precedente)

#### 5. Diagnostica Post-Export
Riverifica lo stato dopo export:
- Conteggio `statoProgrammataProgrammata` PRIMA/DOPO
- Conta commesse aggiunte a Programma Macchine
- Identifica cause di fallimento

### Riepilogo Finale
Mostra:
- Commesse esportabili
- Commesse programmate
- Commesse in Programma Macchine

**Diagnostica automatica**:
Se ci sono commesse Programmate ma Programma Macchine è vuoto → Identifica causa:
- NumeroMacchina non impostato
- Commesse Archiviate
- Stato diverso da Aperta

---

## Output Colorato

| Colore | Significato | Esempio |
|--------|-------------|---------|
| **Verde** ✓ | Test passato | `✓ SUCCESS` |
| **Rosso** ✗ | Test fallito | `✗ FAILED` |
| **Giallo** ⚠ | Warning/attenzione | `⚠ Nessuna commessa esportabile` |
| **Cyan** | Informazione | `COMMESSE ESPORTABILI: 20` |
| **Magenta** | Sezione/titolo | `║ TEST SUITE ║` |

---

## Workflow Completo

```powershell
# 1. Avvia applicazione
.\start-web.ps1
# ✓ Applicazione avviata (PID: 12345)

# 2. Aspetta 10 secondi che l'app sia pronta
Start-Sleep -Seconds 10

# 3. Esegui test
.\test-api.ps1
# ╔══════════════════════════════════════╗
# ║  TEST SUITE: Verifica Sistema       ║
# ╚══════════════════════════════════════╝
# 
# === TEST: Debug Commesse ===
# ✓ SUCCESS
# { ... }
#
# Prime 3 commesse: ...
# 
# COMMESSE IN PROGRAMMA MACCHINE: 12
# ...
# 
# Premi INVIO per testare export: INVIO
# 
# ✓ Export completato: 0 commesse esportate (20 totali)
# 
# CONFRONTO PRIMA/DOPO:
#   StatoProgrammata: 25 → 25
#   ⚠ Nessuna nuova commessa aggiunta (già presenti)
```

---

## Problemi Comuni

### App non risponde ai test
**Sintomo**: `✗ FAILED - Connessione` o timeout

**Cause**:
1. App non avviata
2. App non completamente inizializzata (< 10 secondi)
3. Porta 5156 occupata da altra app

**Soluzione**:
```powershell
# Verifica processo
Get-Process -Name dotnet | Where-Object {$_.Path -like "*MESManager*"}

# Se esiste: ferma e riavvia
Stop-Process -Id <PID>
.\start-web.ps1
Start-Sleep -Seconds 12
.\test-api.ps1
```

### Test fallisce con timeout
**Sintomo**: `The request was canceled due to the configured HttpClient.Timeout of 10 seconds`

**Cause**:
- Endpoint troppo lento (es. `/api/Commesse` con molte commesse)
- Database non raggiungibile

**Soluzione**: Aumentare timeout in test-api.ps1:
```powershell
$params = @{
    TimeoutSec = 30  # Invece di 10
}
```

### Export 0 commesse
**Sintomo**: `✓ Export completato: 0 commesse esportate (20 totali)`

**Cause**:
1. Tutte già Programmate → **Normale** ✓
2. Filtro troppo restrittivo → Verificare debug endpoint

**Diagnostica**:
```
RISULTATI DEBUG:
  conMacchinaEDate: 20  ← Ci sono commesse esportabili
  statoProgrammataProgrammata: 25  ← Ma 25 sono già Programmate

CONFRONTO:
  StatoProgrammata: 25 → 25  ← Non cambia perché già tutte Programmate
```

**Verifica**: Se `conMacchinaEDate == statoProgrammataProgrammata`, tutte sono già esportate

### Programma Macchine vuota ma commesse Programmate
**Sintomo**: 
```
statoProgrammataProgrammata: 25
COMMESSE IN PROGRAMMA MACCHINE: 0
❌ PROBLEMA CRITICO IDENTIFICATO
```

**Cause**:
1. Commesse `Programmate` ma senza `NumeroMacchina` → Database inconsistente
2. Commesse `Programmate` ma `Stato != Aperta` → Chiuse/Completate
3. Commesse `Programmate` ma `StatoProgramma == Archiviata` → Archiviate

**Diagnostica**:
```
Diagnostica:
  - Aperte con macchina: 12  ← Queste dovrebbero apparire
  - Di cui Archiviate: 12    ← Ma sono tutte archiviate!
  ⚠ Ci sono commesse Archiviate che vengono filtrate!
```

**Soluzione**: Query manuale per identificare commesse problematiche
```sql
-- Commesse Programmate senza macchina
SELECT Codice, StatoProgramma, NumeroMacchina, Stato 
FROM Commesse 
WHERE StatoProgramma = 1 AND NumeroMacchina IS NULL;

-- Commesse Programmate ma non Aperte
SELECT Codice, StatoProgramma, NumeroMacchina, Stato 
FROM Commesse 
WHERE StatoProgramma = 1 AND Stato != 1;

-- Commesse Programmate ma Archiviate
SELECT Codice, StatoProgramma, NumeroMacchina, Stato 
FROM Commesse 
WHERE StatoProgramma = 1 AND StatoProgramma = 4;
```

---

## Debug Avanzato

### Vedere log dell'applicazione
Dopo `.\start-web.ps1`, l'app apre una finestra separata con i log in tempo reale.

**Cercare**:
- `PRIMA dell'update: X commesse trovate` → Quante commesse ha trovato l'export
- `Commessa XXX: Stato=Y, StatoProgramma=Z` → Dettagli per commessa
- `DOPO SaveChanges: Aggiornate X/Y commesse` → Quante effettivamente cambiate

### Verificare database direttamente
```sql
-- Conteggi rapidi
SELECT 
    COUNT(*) AS Totale,
    SUM(CASE WHEN NumeroMacchina IS NOT NULL THEN 1 ELSE 0 END) AS ConMacchina,
    SUM(CASE WHEN DataInizioPrevisione IS NOT NULL THEN 1 ELSE 0 END) AS ConDate,
    SUM(CASE WHEN NumeroMacchina IS NOT NULL AND DataInizioPrevisione IS NOT NULL THEN 1 ELSE 0 END) AS Esportabili,
    SUM(CASE WHEN StatoProgramma = 1 THEN 1 ELSE 0 END) AS Programmate,
    SUM(CASE WHEN Stato = 1 THEN 1 ELSE 0 END) AS Aperte
FROM Commesse;

-- Dettaglio commesse esportabili
SELECT Codice, NumeroMacchina, DataInizioPrevisione, DataFinePrevisione, StatoProgramma, Stato
FROM Commesse
WHERE NumeroMacchina IS NOT NULL AND DataInizioPrevisione IS NOT NULL
ORDER BY UltimaModifica DESC;

-- Commesse che dovrebbero apparire in Programma Macchine
SELECT Codice, NumeroMacchina, StatoProgramma, Stato
FROM Commesse
WHERE Stato = 1 AND NumeroMacchina IS NOT NULL AND StatoProgramma != 4
ORDER BY NumeroMacchina, OrdineSequenza;
```

---

## Note Tecniche

### Isolamento Processi
Il problema originale era che eseguire comandi PowerShell nello stesso terminale dell'app la faceva arrestare.

**Soluzione implementata** in `start-web.ps1`:
```powershell
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --project `"$projectPath`" --environment Development"
$psi.WorkingDirectory = $workingDir
$psi.UseShellExecute = $true  # ← Chiave: processo separato
$psi.CreateNoWindow = $false   # ← Mostra finestra propria

$process = [System.Diagnostics.Process]::Start($psi)
```

Con `UseShellExecute = $true`, il processo dotnet:
- Gira in sessione separata
- Non condivide stdin/stdout con script
- Non si arresta quando script termina
- Mostra propria finestra per log

### Test Non Invasivi
Gli script di test usano solo chiamate HTTP REST alle API pubbliche.

**NON interferiscono con**:
- Processo dell'applicazione
- Database (solo letture, tranne export)
- SignalR connections
- Blazor circuits

**Modifiche al database**:
Solo il test Export modifica il database (cambia StatoProgramma).  
È sicuro perché è esattamente ciò che fa il pulsante "Esporta su Programma" nell'UI.

---

## Manutenzione

### Aggiornare i test
Se aggiungi nuovi endpoint o cambi logica:

1. Modifica `test-api.ps1`
2. Aggiungi nuova funzione `Test-Endpoint`
3. Documenta qui il nuovo test

### Estendere diagnostica
Per nuovi conteggi nel debug endpoint:

1. Modifica `PianificazioneController.cs` → `DebugCommesse()`
2. Aggiungi query EF Core per nuovo conteggio
3. Restituisci nel JSON response
4. Aggiorna `test-api.ps1` per mostrare nuovo campo

### Troubleshooting Script
Se script non funziona:

```powershell
# Test manuale connessione
curl http://localhost:5156/api/pianificazione/debug-commesse

# Se fallisce: app non risponde
# Se funziona: problema nello script PowerShell
```

---

## Roadmap

### Possibili Miglioramenti
- [ ] E2E test con Playwright per UI
- [ ] Test performance (load testing)
- [ ] Test automatici su PR/commit (CI/CD)
- [ ] Mock database per test unitari
- [ ] Coverage report

### Non Implementati (Scelto deliberatamente)
- ❌ xUnit/NUnit backend: Complessità eccessiva per beneficio minimo
- ❌ Selenium: Blazor Server non compatibile facilmente
- ❌ Integration tests: Database reale più affidabile per bug fix

---

## Link Utili

- [Script Source](/test-api.ps1)
- [Launcher Source](/start-web.ps1)
- [Bibbia AI - Testing Section](/docs/BIBBIA-AI-MESMANAGER.md#-testing-e-debug-infrastructure)
- [Controller Debug Endpoint](/MESManager.Web/Controllers/PianificazioneController.cs#L613)

---

**Versione**: 1.0  
**Data**: 5 Febbraio 2026  
**Autore**: AI Assistant + User feedback  
**Manutenzione**: Aggiornare quando si modificano test o endpoint
