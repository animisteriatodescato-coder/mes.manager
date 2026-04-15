# 🔧 06 — Installazione da Zero

> Guida completa per installare [NOME_PROGETTO] su un nuovo server/ambiente.

---

## 📋 Prerequisiti

### Software richiesto
- [ ] .NET 8 SDK / Runtime
- [ ] SQL Server [VERSIONE] (Express o superiore)
- [ ] Git
- [ ] [Altri tool specifici]

### Porte necessarie
- `[PORTA_DEV]` — Applicazione web
- `1433` — SQL Server (se accesso remoto)

---

## 🪟 Installazione Windows Server

### 1. Prepara il server

```powershell
# Crea cartella applicazione
New-Item -ItemType Directory -Path "C:\[NomeProgetto]" -Force

# Crea utente servizio (se necessario)
# net user [NomeServizio] [Password] /add
# net localgroup Administrators [NomeServizio] /add
```

### 2. Installa .NET Runtime

```powershell
# Scarica e installa .NET 8 Hosting Bundle
# https://dotnet.microsoft.com/download/dotnet/8.0
# → .NET 8 Hosting Bundle (per IIS) o .NET 8 Runtime (per self-hosted)
```

### 3. Configura SQL Server

```sql
-- Crea database
CREATE DATABASE [DB_NAME_PROD];

-- Crea utente (se non si usa Windows Auth)
CREATE LOGIN [APP_USER] WITH PASSWORD = '[PASSWORD]';
CREATE USER [APP_USER] FOR LOGIN [APP_USER];
ALTER ROLE db_owner ADD MEMBER [APP_USER];
```

### 4. Copia file applicazione

```powershell
# Dal server di sviluppo o da cartella publish
robocopy "publish\Web" "\\[IP_PROD]\c$\[NomeProgetto]" /MIR /Z
```

### 5. Configura appsettings.Production.json

```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  }
}
```

### 6. Crea appsettings.Secrets.json (solo su server prod)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=[DB_SERVER_PROD];Database=[DB_NAME_PROD];User=[APP_USER];Password=[PASSWORD];TrustServerCertificate=True"
  }
}
```

### 7. Applica migration database

```powershell
cd C:\[NomeProgetto]
dotnet [NomeProgetto].Web.dll --migrate
# OPPURE applicare script SQL manualmente (preferito per prod)
```

### 8. Configura Windows Service / Task Scheduler

```powershell
# Opzione A: Task Scheduler (più semplice)
schtasks /Create /TN "Start[NomeProgetto]Web" /TR "dotnet C:\[NomeProgetto]\[NomeProgetto].Web.dll" /SC ONSTART /DELAY 0001:00 /RU SYSTEM

# Opzione B: sc.exe Windows Service
# sc create [NomeProgetto] binPath= "dotnet C:\[NomeProgetto]\[NomeProgetto].Web.dll"
```

---

## 🐧 Installazione Linux (alternativa)

```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-runtime-8.0

# Crea servizio systemd
sudo nano /etc/systemd/system/[nomeprogetto].service
```

```ini
[Unit]
Description=[NOME_PROGETTO]
After=network.target

[Service]
WorkingDirectory=/var/[nomeprogetto]
ExecStart=/usr/bin/dotnet /var/[nomeprogetto]/[NomeProgetto].Web.dll
Restart=always
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:[PORTA_PROD]

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable [nomeprogetto]
sudo systemctl start [nomeprogetto]
```

---

## ✅ Verifica installazione

```powershell
# Verifica applicazione risponde
Invoke-WebRequest -Uri "http://localhost:[PORTA_PROD]/health" -UseBasicParsing

# Verifica versione
# (cercare nel response HTML la versione AppVersion)
```

---

## 🔁 Backup iniziale

```powershell
# Backup database dopo prima installazione
Backup-SqlDatabase -ServerInstance "[DB_SERVER_PROD]" -Database "[DB_NAME_PROD]" -BackupFile "C:\backups\[DB_NAME_PROD]_initial.bak"
```

---

*Versione: 1.0*
