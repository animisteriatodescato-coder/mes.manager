# 🚀 01 — Deploy su Produzione

> Procedura completa e sicura per il deploy di [NOME_PROGETTO] su server produzione.
> **L'AI esegue questa procedura in autonomia quando l'utente ordina il deploy.**

---

## ⛔ REGOLA ASSOLUTA

Deploy **SOLO** su ordine esplicito dell'utente:
> "fai il deploy" / "deploya" / "metti in produzione" / "aggiorna il server"

**MAI** deploiare automaticamente dopo uno sviluppo.

---

## 🔐 Credenziali Produzione

> ⚠️ NON condividere mai queste credenziali in chat o log pubblici.

```
Server:    [IP_PROD]
Utente:    [USER_PROD]
Password:  [PASSWORD_PROD]  (← sostituire con credenziale reale)
Path app:  [PATH_APP_PROD]  (es. C:\[NomeProgetto])
Task:      [TASK_NAME]      (es. Start[NomeProgetto]Web)
```

---

## 📋 Prerequisiti

- [ ] Utente ha confermato esplicitamente il deploy
- [ ] Build in locale è VERDE (0 errori)
- [ ] Test automatici passati
- [ ] Versione in `AppVersion.cs` incrementata
- [ ] `docs/09-CHANGELOG.md` aggiornato
- [ ] Git commit delle modifiche eseguito

---

## 🔄 Workflow Deploy Autonomo

### Step 1 — Build Release
```powershell
cd [PATH_PROGETTO]
dotnet publish -c Release -o publish\Web --nologo
```

### Step 2 — Stop servizi produzione (ordine CRITICO)
```powershell
# Ferma nell'ordine corretto (personalizzare per il progetto)
# Esempio con Task Scheduler Windows:
# schtasks /End /TN "[TaskWorker]"
# schtasks /End /TN "[TaskWeb]"
# Start-Sleep -Seconds 3
```

### Step 3 — Copia file
```powershell
# robocopy con esclusioni sicure (MAI sovrascrivere secrets!)
robocopy "publish\Web" "\\[IP_PROD]\c$\[PATH_APP_PROD]" /MIR /Z /W:5 /R:3 `
    /XF appsettings.Secrets.json `
    /XF appsettings.Database.json `
    /XF *.log /XF *.pdb `
    /XD logs /XD temp
```

> ⚠️ **CRITICO**: `/XF appsettings.Secrets.json` e `/XF appsettings.Database.json` sono **NON NEGOZIABILI**.
> Sovrascrivere i secrets di produzione = interruzione servizio.

### Step 4 — Start servizi produzione
```powershell
# Avvia nell'ordine corretto (personalizzare)
# schtasks /Run /TN "[TaskWeb]"
# Start-Sleep -Seconds 8
```

### Step 5 — Verifica
```powershell
# Verifica processi attivi
# Invoke-WebRequest -Uri "http://[IP_PROD]:[PORTA_PROD]" -TimeoutSec 15 -UseBasicParsing
# Verifica versione nel HTML della response
```

---

## 🗂️ File da NON sovrascrivere mai in deploy

| File | Motivo |
|------|--------|
| `appsettings.Secrets.json` | Credenziali produzione (DPAPI-encrypted) |
| `appsettings.Database.json` | Connection strings produzione |
| `appsettings.Production.json` | Config specifica produzione |
| `*.log` | Log storici produzione |
| `*.pdb` | Non necessario in prod |

---

## 🆘 Rollback

In caso di problemi:
1. Ferma servizi (Step 2)
2. Ripristina backup dalla cartella `backups/`
3. Riavvia servizi (Step 4)
4. Verifica (Step 5)
5. Documenta in `storico/DEPLOY-LESSONS-LEARNED.md`

---

## 📔 Storico Deploy

| Data | Versione | Note | Esito |
|------|---------|------|-------|
| [DATA] | v[X.Y.Z] | Prima installazione | ✅ |

---

*Versione: 1.0 | Aggiornare dopo ogni deploy con esito e note*
