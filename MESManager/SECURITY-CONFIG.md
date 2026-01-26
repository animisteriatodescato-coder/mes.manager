# 🔒 Configurazione Sicurezza MESManager

Questa guida spiega come configurare in modo sicuro le credenziali per MESManager.

## ⚠️ IMPORTANTE

**NON committare MAI credenziali reali nel repository Git!**

Le credenziali sono gestite tramite file separati che sono esclusi dal controllo versione.

---

## 📁 Struttura File di Configurazione

```
MESManager/
├── appsettings.Secrets.json          # ← LE TUE CREDENZIALI (NON in git)
├── appsettings.Secrets.json.template # ← Template da copiare
├── appsettings.Database.json         # ← Legacy (retrocompatibilità)
└── MESManager.Web/
    ├── appsettings.json              # Configurazione base (senza credenziali)
    └── appsettings.Development.json  # Configurazione sviluppo
```

---

## 🚀 Setup Iniziale

### 1. Crea il file delle credenziali

```powershell
# Dalla root della solution
Copy-Item "appsettings.Secrets.json.template" "appsettings.Secrets.json"
```

### 2. Modifica le credenziali

Apri `appsettings.Secrets.json` e inserisci le credenziali reali:

```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=TUO_SERVER\\SQLEXPRESS;Database=MESManager;User Id=TUO_USER;Password=TUA_PASSWORD;TrustServerCertificate=True;",
    "MagoDb": "Data Source=SERVER_MAGO\\SQLEXPRESS;Initial Catalog=NOME_DB;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;",
    "GanttDb": "Server=SERVER_GANTT\\SQLEXPRESS;Database=Gantt;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;"
  },
  "Mago": {
    "ConnectionString": "Data Source=SERVER_MAGO\\SQLEXPRESS;Initial Catalog=NOME_DB;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;"
  }
}
```

### 3. Verifica che il file sia escluso da Git

```powershell
git status
# Il file appsettings.Secrets.json NON deve apparire
```

---

## 🔐 Ordine di Caricamento Configurazione

L'applicazione carica le configurazioni in questo ordine (ultimo vince):

1. `appsettings.json` - Configurazione base
2. `appsettings.{Environment}.json` - Per ambiente (Development/Production)
3. `appsettings.Secrets.json` - **Credenziali reali** (non in git)
4. Variabili d'ambiente - Per container/produzione

---

## 🏭 Configurazione Produzione

### Opzione 1: File appsettings.Secrets.json

Copia il file nella directory dell'applicazione deployata:

```powershell
Copy-Item "appsettings.Secrets.json" "C:\MESManager\Web\"
```

### Opzione 2: Variabili d'ambiente (consigliato per container)

```powershell
# PowerShell
$env:ConnectionStrings__MESManagerDb = "Server=...;Password=..."
$env:ConnectionStrings__MagoDb = "Server=...;Password=..."
$env:ConnectionStrings__GanttDb = "Server=...;Password=..."
```

```bash
# Linux/Docker
export ConnectionStrings__MESManagerDb="Server=...;Password=..."
```

---

## 🛡️ Best Practices Sicurezza

### ✅ Cosa FARE

- [ ] Usare account SQL con **privilegi minimi** (non `sa`!)
- [ ] Creare utenti dedicati per ogni applicazione
- [ ] Usare password complesse (min. 12 caratteri, miste)
- [ ] Abilitare connessioni criptate (`Encrypt=True`)
- [ ] Ruotare le password periodicamente
- [ ] Usare Azure Key Vault o AWS Secrets Manager in cloud

### ❌ Cosa NON FARE

- [ ] Mai usare l'account `sa` in produzione
- [ ] Mai committare password nel codice
- [ ] Mai usare password semplici come `password.123`
- [ ] Mai disabilitare `TrustServerCertificate` in produzione senza un certificato valido

---

## 🔄 Migrazione da Configurazione Legacy

Se avevi credenziali nel vecchio `appsettings.Database.json`:

1. Crea `appsettings.Secrets.json` dal template
2. Copia le credenziali dal vecchio file
3. Elimina o svuota `appsettings.Database.json`
4. Verifica che l'app funzioni

---

## 📋 Checklist Sicurezza

Prima del deploy, verifica:

- [ ] `appsettings.Secrets.json` esiste ed è configurato
- [ ] Il file NON è nel repository (`.gitignore` attivo)
- [ ] Account SQL dedicato con privilegi minimi
- [ ] Password complesse per tutti gli account
- [ ] API protette con `[Authorize]`
- [ ] HTTPS abilitato in produzione

---

## 🆘 Troubleshooting

### Errore: "Connection string 'MESManagerDb' not found"

Il file `appsettings.Secrets.json` non esiste o non è nel path corretto.

**Soluzione:**
```powershell
# Verifica che il file esista
Test-Path "C:\Dev\MESManager\appsettings.Secrets.json"

# Se non esiste, crealo dal template
Copy-Item "appsettings.Secrets.json.template" "appsettings.Secrets.json"
```

### Errore: "Login failed for user"

Le credenziali nel file sono errate o l'utente non ha i permessi.

**Soluzione:**
1. Verifica username e password
2. Verifica che l'utente abbia accesso al database
3. Testa la connessione con SSMS

---

## 📞 Supporto

Per problemi di configurazione, contatta l'amministratore di sistema.
