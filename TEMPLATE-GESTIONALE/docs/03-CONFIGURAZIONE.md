# ⚙️ 03 — Configurazione

> Database, secrets, variabili d'ambiente e integrazioni esterne di [NOME_PROGETTO].

---

## 🗄️ Database

### Ambienti
| Env | Server | Database | Auth |
|-----|--------|----------|------|
| DEV | `[DB_SERVER_DEV]` | `[DB_NAME_DEV]` | Windows Auth / SA |
| PROD | `[DB_SERVER_PROD]` | `[DB_NAME_PROD]` | SA / Service Account |

### Connection String (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=[DB_SERVER_DEV];Database=[DB_NAME_DEV];Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

---

## 🔐 Gestione Secrets

### File secrets (NON in git)
```
appsettings.Secrets.json          ← credenziali sensibili (DPAPI in prod)
appsettings.Database.json         ← connection strings produzione
```

### Template secrets (in git — senza valori reali)
```
appsettings.Secrets.json.template ← solo struttura, nessun valore
```

### Struttura appsettings.Secrets.json
```json
{
  "ExternalApi": {
    "ApiKey": "[NON COMMITTARE — sostituire con valore reale]",
    "BaseUrl": "[NON COMMITTARE]"
  },
  "EmailSettings": {
    "SmtpPassword": "[NON COMMITTARE]"
  }
}
```

> ⚠️ MAI committare in git file con valori secrets reali.
> In produzione questi file sono protetti da DPAPI Windows.

---

## 📧 Email (se utilizzata)
```json
{
  "EmailSettings": {
    "SmtpHost": "[SMTP_HOST]",
    "SmtpPort": 587,
    "SmtpUser": "[SMTP_USER]",
    "SmtpPassword": "[in Secrets.json]",
    "FromAddress": "[FROM_EMAIL]",
    "FromName": "[NOME_PROGETTO]"
  }
}
```

---

## 🌐 Integrazioni Esterne (personalizzare)

> Documenta qui ogni integrazione esterna del progetto.

### [Nome Integrazione 1] — es. ERP Esterno
```json
{
  "[NomeIntegrazione]": {
    "BaseUrl": "[BASE_URL]",
    "ApiKey": "[in Secrets.json]",
    "Timeout": 30
  }
}
```

### [Nome Integrazione 2] — es. PLC / dispositivo IoT
```json
{
  "PlcSettings": {
    "IpAddress": "[IP_PLC]",
    "Port": 102,
    "Rack": 0,
    "Slot": 1
  }
}
```

---

## 🔧 Variabili d'Ambiente

| Variabile | DEV | PROD | Scopo |
|-----------|-----|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` | Env attivo |
| `ASPNETCORE_URLS` | `http://localhost:[PORTA_DEV]` | `http://0.0.0.0:[PORTA_PROD]` | Bind URL |
| `[VARIABILE_CUSTOM]` | `[VALORE_DEV]` | `[VALORE_PROD]` | [Scopo] |

---

## 📁 Struttura File Configurazione

```
[NomeProgetto].Web/
├── appsettings.json                     ← config base (in git)
├── appsettings.Development.json         ← override dev (in git, no secrets)
├── appsettings.Production.json          ← override prod (in git, no secrets)
├── appsettings.Secrets.json             ← ⛔ NON in git
├── appsettings.Secrets.json.template    ← struttura vuota (in git)
└── appsettings.Database.json            ← ⛔ NON in git (solo prod)
```

---

## 🏥 Health Check

```
GET http://localhost:[PORTA_DEV]/health
```

Risposta attesa: `{"status":"Healthy"}`

---

*Versione: 1.0*
