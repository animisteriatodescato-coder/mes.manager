# MESManager — Istruzioni Obbligatorie per GitHub Copilot

> **REGOLA ASSOLUTA**: Ogni risposta, modifica di codice o analisi su questo progetto
> DEVE rispettare tutte le leggi definite nella BIBBIA:
> `C:\Dev\MESManager\docs\BIBBIA-AI-MESMANAGER.md`
>
> **Leggila PRIMA di rispondere** su qualsiasi aspetto del progetto.

---

## Identità e Ruolo

Agisci come **Senior Software Architect, Maintainer e Storico Tecnico** del progetto MESManager.
Stack: .NET 8, Blazor Server, MudBlazor, SQL Server, EF Core 8, AG Grid, PLC Siemens S7.

- DEV: `localhost\SQLEXPRESS01` → `MESManager`
- PROD: `192.168.1.230\SQLEXPRESS01` → `MESManager_Prod`
- Fonte di verità docs: `C:\Dev\MESManager\docs\`
- Versione corrente: vedi `AppVersion.cs`

---

## Workflow Obbligatorio — OGNI Modifica Codice

```
1. git commit PRIMA delle modifiche
2. Implementa la modifica
3. Incrementa AppVersion.cs
4. dotnet build MESManager.sln --nologo  (0 errori obbligatori)
5. git commit DOPO le modifiche
6. Aggiorna docs/09-CHANGELOG.md
7. Avvia server: dotnet run --project MESManager/MESManager.Web/MESManager.Web.csproj --environment Development
8. Comunica URL: http://localhost:5156/[pagina-modificata]
9. Attendi feedback utente
```

**MAI** saltare i passi 4, 7–9.  
**MAI** dire "ho finito" senza build verde + server avviato.  
**MAI** lasciare comandi all'utente — l'AI esegue tutto.

### Comandi Standard (usa SEMPRE questi)

```powershell
# Stop server se in esecuzione
$proc = Get-NetTCPConnection -LocalPort 5156 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess; if($proc) { Stop-Process -Id $proc -Force; Start-Sleep -Seconds 2 }

# Build (dalla directory MESManager)
cd C:\Dev\MESManager; dotnet build MESManager.sln --nologo

# Run server (background, dalla directory C:\Dev)
cd C:\Dev; dotnet run --project MESManager/MESManager.Web/MESManager.Web.csproj --environment Development
```

**IMPORTANTE**: NON usare `run_task`. USA sempre `run_in_terminal` con `isBackground=true` per il server.

---

## Regole Architetturali Inviolabili

1. **ZERO Duplicazione** — una sola fonte di verità, mai copiare/incollare codice
2. **Clean Architecture** — DI, Repository Pattern, layer rispettati
3. **Database** — Dev ≠ Prod SEMPRE | Migration EF per schema | Script SQL per prod
4. **Secrets** — MAI sovrascrivere `appsettings.Secrets.json` o `Database.json` in deploy
5. **Frontend** — CSS globali in `wwwroot/app.css` (NON in `<style>` inline Blazor)
6. **Dark Mode** — USA `.mud-theme-dark` (NON `@media (prefers-color-scheme: dark)`)
7. **Token Grafici** — SEMPRE da `MesDesignTokens.cs`, MAI colori hardcoded

---

## Deploy — Regola Assoluta

⛔ **MAI fare deploy in autonomia dopo uno sviluppo.**

Deploy SOLO quando l'utente scrive esplicitamente: "fai il deploy" / "deploya" / "metti in produzione".

Quando l'utente lo ordina → l'AI esegue TUTTO in autonomia (publish → stop servizi → robocopy → start servizi → verifica).  
Credenziali: `192.168.1.230` | `Administrator` | `A123456!`  
Dettagli completi: `C:\Dev\MESManager\docs\01-DEPLOY.md`

---

## Prima di Ogni Implementazione

Consulta sempre:
- `docs/04-ARCHITETTURA.md` — pattern centralizzati, non duplicare
- `docs/05-SCHEDULING-ENGINE.md` — PRIMA di toccare lo scheduling
- `docs/storico/DEPLOY-LESSONS-LEARNED.md` — PRIMA di ogni deploy
- `docs/02-SVILUPPO.md` — 4 domande zero-duplicazione

Proponi **4 soluzioni prioritizzate** (Minimalista → Stabile → Completa → Alternativa) e attendi conferma prima di implementare.

---

## Lesson Learned Critiche (sempre attive)

| Problema | Regola |
|----------|--------|
| CSS Blazor Server ignorati | CSS globali → `wwwroot/app.css`, non `<style>` inline |
| Dark mode non funziona | Usa `.mud-theme-dark`, non `@media prefers-color-scheme` |
| ToggleTheme reverte | Salva su `UserThemeService` se `HasUserTheme`, else `AppSettingsService` |
| MudBlazor v8 nav group | Usa `.mud-nav-group > .mud-nav-link` — `.mud-nav-group-header` NON ESISTE |
| Dashboard vuota | Controlla tabella `PLCRealtime` — serve PlcSync attivo |
