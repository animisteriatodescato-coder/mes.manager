# Istruzioni Codex - Workspace C:\Dev

Quando lavori sul progetto MESManager, prima di rispondere a qualunque richiesta o svolgere qualunque attivita devi leggere e applicare integralmente:

`MESManager/docs/BIBBIA-AI-MESMANAGER.md`

Regole operative:

- Questa istruzione vale per ogni nuova chat avviata da `C:\Dev` e per ogni richiesta relativa a MESManager.
- Considera la Bibbia la fonte di verita primaria per contesto, architettura, workflow, deploy, testing, documentazione e stile di risposta.
- Dopo la Bibbia, se la richiesta riguarda un'area specifica, leggi anche il documento tematico indicato dalla Bibbia o da `MESManager/docs/README.md`.
- Non modificare codice senza aver verificato prima le regole pertinenti nella documentazione.
- Mantieni la documentazione come fonte viva: quando emergono decisioni, bug o lesson learned importanti, aggiorna il file docs corretto.
- Non fare deploy se l'utente non lo chiede esplicitamente con una formula chiara come "fai il deploy", "deploya" o "metti in produzione".
- Per dati aziendali, usare solo quelli presenti nella Bibbia; se mancano, chiedere all'utente.

Questo file e' solo un aggancio persistente: la Bibbia resta il documento completo e vincolante.

---

## ⚠️ REGOLA CRITICA — Come avviare il server (Codex)

Codex NON ha `run_in_terminal` con `isBackground=true`. L'unico metodo affidabile per avviare il server in background è un **PowerShell Job**. MAI usare `Start-Process` con `-RedirectStandardOutput` (fallisce se il log è bloccato) e MAI eseguire direttamente `MESManager.Web.exe` (ASP.NET Core non trova `appsettings.json`).

### Comando obbligatorio

```powershell
# Ferma istanza precedente
$proc = Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess
if ($proc) { Stop-Process -Id $proc -Force; Start-Sleep -Seconds 2 }

# Avvia come PowerShell Job (sopravvive alla sessione del tool)
Start-Job -Name "MESManager" -ScriptBlock {
    Set-Location 'C:\Dev\MESManager\MESManager.Web'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --urls http://localhost:5156
} | Out-Null

# Attendi 20 secondi per compilazione iniziale, poi verifica
Start-Sleep -Seconds 20
Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue
```

### ❌ VIETATO

- `Start-Process -FilePath "...MESManager.Web.exe"` — non trova appsettings.json
- `Start-Process -FilePath dotnet -RedirectStandardOutput file.log` — fallisce se log bloccato
- `dotnet run` in foreground senza job — muore con la sessione
