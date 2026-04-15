# 📚 Documentazione — [NOME_PROGETTO]

> Indice e quick reference per AI e sviluppatori.
> Fonte di verità del progetto. Leggi questo file prima di tutto il resto.

---

## 🗂️ File Documentazione

| File | Scopo | Priorità |
|------|-------|----------|
| [BIBBIA-AI-[NOME_PROGETTO].md](BIBBIA-AI-TEMPLATE.md) | ⭐ Regole AI, workflow, architettura | **PRIMO DA LEGGERE** |
| [01-DEPLOY.md](01-DEPLOY.md) | Procedura deploy su produzione | Ogni pubblicazione |
| [02-SVILUPPO.md](02-SVILUPPO.md) | Workflow sviluppo, comandi, zero-dup | Ogni modifica codice |
| [03-CONFIGURAZIONE.md](03-CONFIGURAZIONE.md) | DB, secrets, variabili d'ambiente | Setup, troubleshooting |
| [04-ARCHITETTURA.md](04-ARCHITETTURA.md) | Layer, pattern centralizzati, DI | Implementazione feature |
| [05-MODULO-CORE.md](05-MODULO-CORE.md) | Modulo principale del sistema | Prima di modificarlo |
| [06-INSTALLAZIONE.md](06-INSTALLAZIONE.md) | Setup da zero su nuovo server | Nuove installazioni |
| [07-INTEGRAZIONI.md](07-INTEGRAZIONI.md) | API esterne, ERP, PLC, webhooks | Problemi integrazioni |
| [08-MODULI-EXTRA.md](08-MODULI-EXTRA.md) | Funzionalità aggiuntive | Sviluppo moduli |
| [09-CHANGELOG.md](09-CHANGELOG.md) | Storico versioni e modifiche | Ogni deploy |
| [10-BUSINESS.md](10-BUSINESS.md) | Regole di business, logica cliente | Analisi requisiti |
| [11-TESTING.md](11-TESTING.md) | Testing framework, script, debugging | Feature nuove, debug |
| [12-QA-UI.md](12-QA-UI.md) | Test E2E Playwright, visual regression | QA Automation |
| [storico/DEPLOY-LESSONS-LEARNED.md](storico/DEPLOY-LESSONS-LEARNED.md) | Lezioni deploy produzione | Prima di ogni deploy |

---

## ⚡ Quick Reference

### Comandi essenziali
```powershell
# Build
cd [PATH_PROGETTO]; dotnet build [NOME_PROGETTO].sln --nologo

# Run locale
cd [PATH_PROGETTO]; dotnet run --project [NomeProgetto].Web/[NomeProgetto].Web.csproj --environment Development

# Stop porta
$proc = Get-NetTCPConnection -LocalPort [PORTA_DEV] -State Listen -EA SilentlyContinue | Select -First 1 -Exp OwningProcess; if($proc){Stop-Process -Id $proc -Force}
```

### URL Ambienti
- **DEV**: `http://localhost:[PORTA_DEV]`
- **PROD**: `http://[IP_PROD]:[PORTA_PROD]`

### Version
- `AppVersion.cs` → versione corrente
- `docs/09-CHANGELOG.md` → storico completo

---

## 📌 Regole Essenziali (reminder)

1. **Zero Duplicazione** — cerca prima, riutilizza, NON copiare
2. **Build prima di tutto** — 0 errori obbligatori
3. **Git commit** — prima E dopo ogni modifica
4. **Docs aggiornate** — ogni fix/feature → aggiorna file pertinente
5. **Deploy solo su ordine** — mai in autonomia

---

*Versione Docs: 1.0 | Creato da template generico*
