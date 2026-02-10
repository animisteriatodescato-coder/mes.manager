# 📚 Documentazione MESManager

## 🚀 Leggi Prima di Fare Deploy

**👉 [DEPLOY-GUIDA-DEFINITIVA.md](DEPLOY-GUIDA-DEFINITIVA.md)** ← **LEGGI QUESTA**

Guida unica, completa e senza ambiguità per eseguire deploy su server di produzione (192.168.1.230).

**Contiene:**
- ✅ Credenziali (Windows, Database)
- ✅ Architettura server (dove copiare i file)
- ✅ Step-by-step deploy completo
- ✅ Deploy solo JavaScript (senza recompile)
- ✅ Deploy SQL (migrazioni database)
- ✅ 6 Errori Comuni e soluzioni
- ✅ Checklist pre-deploy
- ✅ Script rapido copy-paste

---

## 📋 Altre Documentazioni

### Setup e Configurazione
- **[DATABASE-CONFIG-README.md](DATABASE-CONFIG-README.md)** - Configurazione database
- **[SECURITY-CONFIG.md](SECURITY-CONFIG.md)** - Configurazione sicurezza
- **[SERVIZI.md](SERVIZI.md)** - Descrizione servizi (Web, Worker, PlcSync)

### User Guide
- **[GUIDA-RAPIDA-ESPORTAZIONE.md](GUIDA-RAPIDA-ESPORTAZIONE.md)** - Esportazione preferenze utenti
- **[PREFERENZE-UTENTE-IMPLEMENTAZIONE.md](PREFERENZE-UTENTE-IMPLEMENTAZIONE.md)** - Documentazione preferenze

### Implementazioni Specifiche
- **[GanttAnalysis.md](GanttAnalysis.md)** - Analisi Gantt chart
- **[GUIDA-REPLICA-SISTEMA.md](GUIDA-REPLICA-SISTEMA.md)** - Replicazione sistema

### Changelog
- **[CHANGELOG.md](CHANGELOG.md)** - Storico versioni e modifiche
- **[PENDING-CHANGES.md](PENDING-CHANGES.md)** - Modifiche in attesa di consolidamento
- **[WORKFLOW-PUBBLICAZIONE.md](WORKFLOW-PUBBLICAZIONE.md)** - 🆕 Istruzioni per AI su deploy

---

## 🗂️ Storico (Documenti Obsoleti)

Cartella `storico/` contiene vecchie guide, non usarle:
- `DEPLOY-README.md.old` ← Obsoleto, usa DEPLOY-GUIDA-DEFINITIVA.md
- `DEPLOY-SAFE-GUIDE.md.old` ← Obsoleto
- `GUIDA-DEPLOY-SICURO.md.old` ← Obsoleto
- `DEPLOY-RAPIDO.md.old` ← Obsoleto
- `DIAGNOSTIC_REPORT.md` ← Storico diagnostica
- `FIX_IMPLEMENTED.md` ← Storico fix
- `SQL-SERVER-ANALYSIS.md` ← Storico analisi
- `FIX-GANTT-ACCODAMENTO-20260120.md` ← Fix specifico gantt
- `verifica-stati-macchine.md` ← Verifica stati

---

## 🎯 Quick Links

### Per Fare Deploy
1. Leggi [DEPLOY-GUIDA-DEFINITIVA.md](DEPLOY-GUIDA-DEFINITIVA.md)
2. Incrementa versione in `MainLayout.razor`
3. Esegui script build/publish/copy/start
4. Aggiorna CHANGELOG.md

### Per Problemi
Vedi sezione "Errori Comuni" in [DEPLOY-GUIDA-DEFINITIVA.md](DEPLOY-GUIDA-DEFINITIVA.md#errori-comuni-e-soluzioni)

### Credenziali
```
Server: 192.168.1.230
Admin: Administrator / A123456!
DB: FAB / password.123
```

---

## 📝 Note Importanti

### 🔴 Errori da EVITARE
1. ❌ Copiare file in `C:\MESManager\Web\wwwroot\` (sbagliato!)
   - ✅ Corretto: `C:\MESManager\wwwroot\` (ROOT)

2. ❌ Copiare `appsettings.Secrets.json` o `appsettings.Database.json`
   - ✅ Usare robocopy con `/XF` per escluderli

3. ❌ Non incrementare versione prima di compilare
   - ✅ Modifica MainLayout.razor prima di build

4. ❌ Riavviare app senza stoppare il processo
   - ✅ Sempre `taskkill` prima di start

5. ❌ Non svuotare cache nel browser dopo JS update
   - ✅ Comunicare agli utenti: Ctrl+Shift+R

6. ❌ Ignorare exit code di script
   - ✅ Sempre verificare che build/publish abbiano exit code 0

### ✅ Best Practice
- Usa sempre la guida DEPLOY-GUIDA-DEFINITIVA.md
- Incrementa versione PRIMA di compilare
- Escludi SEMPRE file sensibili con robocopy
- Verifica che app si avvia dopo copia
- Comunica versione nuova ai clienti
- Aggiorna CHANGELOG.md
- Fai backup prima di migrazioni SQL critiche
