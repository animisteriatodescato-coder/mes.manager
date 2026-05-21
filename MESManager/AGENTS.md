# Istruzioni Codex - MESManager

Prima di rispondere a qualunque domanda o svolgere qualunque attivita in questo repository, in ogni nuova chat e per ogni nuova richiesta MESManager, devi leggere e applicare integralmente:

`docs/BIBBIA-AI-MESMANAGER.md`

Regole operative:

- Considera `docs/BIBBIA-AI-MESMANAGER.md` la fonte di verita primaria per contesto, architettura, workflow, deploy, testing e stile di risposta.
- Se la richiesta riguarda un'area specifica, dopo la Bibbia leggi anche il documento tematico indicato dalla Bibbia o da `docs/README.md`.
- Non modificare codice senza aver verificato prima le regole pertinenti nella documentazione.
- Mantieni la documentazione come fonte viva: quando emergono decisioni, bug o lesson learned importanti, aggiorna il file docs corretto.
- Non fare deploy se l'utente non lo chiede esplicitamente con una formula chiara come "fai il deploy", "deploya" o "metti in produzione".
- Per dati aziendali, usare solo quelli presenti nella Bibbia; se mancano, chiedere all'utente.

Queste istruzioni servono come aggancio permanente per Codex: la Bibbia resta il documento completo, questo file serve solo a obbligarne la consultazione.

---

## ⚠️ REGOLA CRITICA — Come avviare il server (Codex)

Codex NON ha `run_in_terminal` con `isBackground=true`. L'unico metodo affidabile per avviare il server in background è un **PowerShell Job**. MAI usare `Start-Process` con `-RedirectStandardOutput` (fallisce se il log è bloccato) e MAI eseguire direttamente `MESManager.Web.exe` (ASP.NET Core non trova `appsettings.json` fuori dalla working directory corretta).

### Comando standard obbligatorio per avviare il server

```powershell
# 1. Ferma istanza precedente
$proc = Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess
if ($proc) { Stop-Process -Id $proc -Force; Start-Sleep -Seconds 2 }

# 2. Avvia come PowerShell Job (processo persistente, sopravvive alla sessione del tool)
Start-Job -Name "MESManager" -ScriptBlock {
    Set-Location 'C:\Dev\MESManager\MESManager.Web'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --urls http://localhost:5156
} | Out-Null

# 3. Attendi avvio (20 secondi per la compilazione iniziale)
Start-Sleep -Seconds 20

# 4. Verifica
Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue | Select-Object LocalAddress,LocalPort,OwningProcess
```

### Per verificare log del Job

```powershell
Receive-Job -Name "MESManager" -Keep | Select-Object -Last 30
```

### Per fermare il server

```powershell
Stop-Job -Name "MESManager"; Remove-Job -Name "MESManager"
# oppure via porta:
$proc = Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess
if ($proc) { Stop-Process -Id $proc -Force }
```

### ❌ VIETATO

- `Start-Process -FilePath "...MESManager.Web.exe"` — l'exe non trova appsettings.json
- `Start-Process -FilePath dotnet -RedirectStandardOutput file.log` — fallisce se il log è bloccato
- Lanciare `dotnet run` in foreground senza job/background — il processo muore con la sessione
