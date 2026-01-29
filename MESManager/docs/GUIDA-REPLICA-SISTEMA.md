# 🏭 MESManager - Guida Completa per Replica del Sistema

**Versione:** 1.0  
**Data:** 28 Gennaio 2026  
**Framework:** .NET 8.0

---

## 📑 Indice

1. [Requisiti di Sistema](#1-requisiti-di-sistema)
2. [Dipendenze NuGet](#2-dipendenze-nuget)
3. [Struttura Database](#3-struttura-database)
4. [Configurazione](#4-configurazione)
5. [Setup Iniziale](#5-setup-iniziale)
6. [Deployment](#6-deployment)
7. [Integrazione PLC](#7-integrazione-plc)
8. [Integrazione ERP Mago](#8-integrazione-erp-mago)

---

## 1. Requisiti di Sistema

### Hardware Minimo
| Componente | Requisito |
|------------|-----------|
| **CPU** | Intel Core i5 o equivalente |
| **RAM** | 8 GB (16 GB consigliati) |
| **Storage** | 50 GB SSD |
| **Rete** | Gigabit Ethernet |

### Software Richiesto

| Software | Versione | Note |
|----------|----------|------|
| **Windows** | 10/11 o Server 2019+ | Necessario per servizi Windows |
| **.NET SDK** | 8.0.x | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **SQL Server** | Express 2019+ | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) |
| **Node.js** | 18.x+ | Solo per sviluppo frontend |
| **Git** | 2.x | Version control |

### Software Opzionale
| Software | Uso |
|----------|-----|
| **SQL Server Management Studio** | Gestione database |
| **Visual Studio 2022** | Sviluppo (o VS Code) |
| **TIA Portal** | Programmazione PLC Siemens |

---

## 2. Dipendenze NuGet

### MESManager.Domain
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
<!-- Nessuna dipendenza esterna -->
```

### MESManager.Application
```xml
<PackageReference Include="EPPlus" Version="7.1.3" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
```

### MESManager.Infrastructure
```xml
<PackageReference Include="EPPlus" Version="7.1.3" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="8.0.0" />
```

### MESManager.Web (Blazor Server)
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*" />
<PackageReference Include="MudBlazor" Version="8.*" />
<PackageReference Include="Syncfusion.Blazor.Gantt" Version="32.1.23" />
```

### MESManager.PlcSync (Worker Service)
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.0.1" />
<PackageReference Include="Sharp7" Version="1.1.84" />
```

### MESManager.Sync (ERP Integration)
```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

### MESManager.Worker (Background Service)
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="8.*" />
```

### Licenze Richieste
| Pacchetto | Licenza | Note |
|-----------|---------|------|
| **EPPlus** | Polyform Noncommercial | Gratuito per uso non commerciale |
| **Syncfusion** | Community License | Gratuito < $1M ricavi |
| **MudBlazor** | MIT | Open source |
| **Sharp7** | MIT | Open source |

---

## 3. Struttura Database

### 3.1 Database Locale (MESManager)

#### Tabelle Principali

```sql
-- =============================================
-- ANAGRAFICA
-- =============================================

CREATE TABLE Macchine (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Codice NVARCHAR(50) NOT NULL,
    Nome NVARCHAR(200) NOT NULL,
    Stato INT NOT NULL DEFAULT 0,              -- Enum: 0=Sconosciuto, 1=Ferma, 2=InFunzione, 3=Manutenzione, 4=Allarme
    AttivaInGantt BIT NOT NULL DEFAULT 1,
    OrdineVisualizazione INT NOT NULL DEFAULT 0,
    IndirizzoPLC NVARCHAR(50) NULL             -- IP del PLC Siemens (es: 192.168.0.1)
);
CREATE INDEX IX_Macchine_Codice ON Macchine(Codice);

CREATE TABLE Articoli (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Codice NVARCHAR(100) NOT NULL UNIQUE,
    Descrizione NVARCHAR(500) NULL,
    UnitaMisura NVARCHAR(20) NULL,
    TempoCicloStandard INT NULL,               -- Tempo ciclo in secondi
    DataCreazione DATETIME2 NOT NULL DEFAULT GETDATE(),
    DataModifica DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE UNIQUE INDEX IX_Articoli_Codice ON Articoli(Codice);

CREATE TABLE Clienti (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Codice NVARCHAR(50) NOT NULL UNIQUE,
    RagioneSociale NVARCHAR(200) NOT NULL,
    PartitaIva NVARCHAR(20) NULL,
    Indirizzo NVARCHAR(300) NULL,
    Citta NVARCHAR(100) NULL,
    Provincia NVARCHAR(5) NULL,
    Cap NVARCHAR(10) NULL,
    Telefono NVARCHAR(50) NULL,
    Email NVARCHAR(200) NULL,
    DataCreazione DATETIME2 NOT NULL DEFAULT GETDATE(),
    DataModifica DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE UNIQUE INDEX IX_Clienti_Codice ON Clienti(Codice);

CREATE TABLE Operatori (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Codice NVARCHAR(50) NOT NULL,
    Nome NVARCHAR(100) NOT NULL,
    Cognome NVARCHAR(100) NOT NULL,
    Attivo BIT NOT NULL DEFAULT 1,
    DataCreazione DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- CATALOGO ANIME (Schede Prodotto)
-- =============================================

CREATE TABLE Anime (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodiceArticolo NVARCHAR(100) NOT NULL,
    DescrizioneArticolo NVARCHAR(500) NOT NULL,
    DataModificaRecord DATETIME2 NULL,
    UtenteModificaRecord NVARCHAR(100) NULL,
    UnitaMisura NVARCHAR(20) NULL,
    Larghezza INT NULL,
    Altezza INT NULL,
    Profondita INT NULL,
    Imballo INT NULL,                          -- Lookup: -1=CASSA GRANDE, -2=CASSA PICCOLA, etc.
    Note NVARCHAR(MAX) NULL,
    Allegato NVARCHAR(500) NULL,
    Peso NVARCHAR(50) NULL,
    Ubicazione NVARCHAR(100) NULL,
    Ciclo NVARCHAR(100) NULL,
    CodiceCassa NVARCHAR(100) NULL,
    CodiceAnime NVARCHAR(100) NULL,
    IdArticolo INT NULL,
    MacchineSuDisponibili NVARCHAR(200) NULL,  -- Es: "M001;M002;M003"
    TrasmettiTutto BIT NOT NULL DEFAULT 0,
    Colla NVARCHAR(10) NULL,                   -- Lookup: -1=BIANCA, -2=A CALDO, -3=ROSSA S.G
    Sabbia NVARCHAR(50) NULL,                  -- Es: "310/60", "OLIVINA", etc.
    Vernice NVARCHAR(10) NULL,                 -- Lookup: -1="", -2=YELLOW COVER, etc.
    Cliente NVARCHAR(200) NULL,
    TogliereSparo NVARCHAR(50) NULL,
    QuantitaPiano INT NULL,
    NumeroPiani INT NULL,
    Figure NVARCHAR(50) NULL,
    Maschere NVARCHAR(50) NULL,
    Assemblata NVARCHAR(50) NULL,
    ArmataL NVARCHAR(50) NULL,
    DataImportazione DATETIME2 NOT NULL DEFAULT GETDATE(),
    ModificatoLocalmente BIT NOT NULL DEFAULT 0,
    DataUltimaModificaLocale DATETIME2 NULL,
    UtenteUltimaModificaLocale NVARCHAR(100) NULL
);

-- =============================================
-- COMMESSE (Ordini di Produzione)
-- =============================================

CREATE TABLE Commesse (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Codice NVARCHAR(100) NOT NULL,             -- Formato: InternalOrdNo-Line
    
    -- Riferimenti ERP Mago
    SaleOrdId NVARCHAR(50) NULL,
    InternalOrdNo NVARCHAR(50) NULL,
    ExternalOrdNo NVARCHAR(50) NULL,
    Line NVARCHAR(10) NULL,
    
    -- Relazioni
    ArticoloId UNIQUEIDENTIFIER NULL REFERENCES Articoli(Id),
    ClienteId UNIQUEIDENTIFIER NULL REFERENCES Clienti(Id) ON DELETE SET NULL,
    CompanyName NVARCHAR(200) NULL,            -- Denormalizzato per performance
    
    -- Dati commessa
    Description NVARCHAR(500) NULL,
    QuantitaRichiesta DECIMAL(18,2) NOT NULL DEFAULT 0,
    UoM NVARCHAR(20) NULL,
    DataConsegna DATETIME2 NULL,
    Stato INT NOT NULL DEFAULT 0,              -- Enum StatoCommessa
    
    -- Riferimenti
    RiferimentoOrdineCliente NVARCHAR(200) NULL,
    OurReference NVARCHAR(200) NULL,
    
    -- Programmazione
    NumeroMacchina NVARCHAR(50) NULL,          -- Codice macchina assegnata
    OrdineSequenza INT NOT NULL DEFAULT 0,     -- Per drag&drop
    
    -- Pianificazione
    DataInizioPrevisione DATETIME2 NULL,
    DataFinePrevisione DATETIME2 NULL,
    DataInizioProduzione DATETIME2 NULL,
    DataFineProduzione DATETIME2 NULL,
    
    -- Stato programma interno
    StatoProgramma INT NOT NULL DEFAULT 0,     -- Enum StatoProgramma
    DataCambioStatoProgramma DATETIME2 NULL,
    
    -- Audit
    UltimaModifica DATETIME2 NOT NULL DEFAULT GETDATE(),
    TimestampSync DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE INDEX IX_Commesse_Codice ON Commesse(Codice);
CREATE INDEX IX_Commesse_StatoProgramma ON Commesse(StatoProgramma);

-- =============================================
-- PLC - DATI REALTIME E STORICO
-- =============================================

CREATE TABLE PLCRealtime (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MacchinaId UNIQUEIDENTIFIER NOT NULL REFERENCES Macchine(Id),
    DataUltimoAggiornamento DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Produzione
    CicliFatti INT NOT NULL DEFAULT 0,
    QuantitaDaProdurre INT NOT NULL DEFAULT 0,
    CicliScarti INT NOT NULL DEFAULT 0,
    BarcodeLavorazione INT NOT NULL DEFAULT 0,
    
    -- Operatore
    OperatoreId UNIQUEIDENTIFIER NULL,
    NumeroOperatore INT NOT NULL DEFAULT 0,
    
    -- Tempi
    TempoMedioRilevato INT NOT NULL DEFAULT 0,
    TempoMedio INT NOT NULL DEFAULT 0,
    Figure INT NOT NULL DEFAULT 0,
    
    -- Stati
    StatoMacchina NVARCHAR(50) NOT NULL DEFAULT '',
    QuantitaRaggiunta BIT NOT NULL DEFAULT 0
);

CREATE TABLE PLCStorico (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MacchinaId UNIQUEIDENTIFIER NOT NULL REFERENCES Macchine(Id),
    DataRegistrazione DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Snapshot completo dati PLC (JSON)
    DatiJson NVARCHAR(MAX) NOT NULL,
    
    -- Dati estratti per query veloci
    CicliFatti INT NULL,
    CicliScarti INT NULL,
    OperatoreId UNIQUEIDENTIFIER NULL,
    StatoMacchina NVARCHAR(50) NULL
);
CREATE INDEX IX_PLCStorico_MacchinaId ON PLCStorico(MacchinaId);
CREATE INDEX IX_PLCStorico_Data ON PLCStorico(DataRegistrazione);

-- =============================================
-- SERVIZIO PLC SYNC
-- =============================================

CREATE TABLE PlcServiceStatus (
    Id INT PRIMARY KEY DEFAULT 1,
    IsRunning BIT NOT NULL DEFAULT 0,
    LastHeartbeat DATETIME2 NULL,
    LastError NVARCHAR(MAX) NULL,
    StartedAt DATETIME2 NULL,
    MacchineConnesse INT NOT NULL DEFAULT 0,
    IntervalloPollingMs INT NOT NULL DEFAULT 1000
);

CREATE TABLE PlcSyncLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    Level NVARCHAR(20) NOT NULL,               -- Info, Warning, Error
    MacchinaId UNIQUEIDENTIFIER NULL REFERENCES Macchine(Id) ON DELETE SET NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Exception NVARCHAR(MAX) NULL
);
CREATE INDEX IX_PlcSyncLogs_Timestamp ON PlcSyncLogs(Timestamp);
CREATE INDEX IX_PlcSyncLogs_Level ON PlcSyncLogs(Level);

-- =============================================
-- UTENTI E PREFERENZE
-- =============================================

CREATE TABLE UtentiApp (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Nome NVARCHAR(100) NOT NULL UNIQUE,
    Attivo BIT NOT NULL DEFAULT 1,
    Ordine INT NOT NULL DEFAULT 0,
    Colore NVARCHAR(20) NULL,                  -- Colore per identificazione (es: #FF5722)
    DataCreazione DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE TABLE PreferenzeUtente (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UtenteAppId UNIQUEIDENTIFIER NOT NULL REFERENCES UtentiApp(Id) ON DELETE CASCADE,
    Chiave NVARCHAR(200) NOT NULL,             -- Es: "CatalogoAnime_gridState"
    Valore NVARCHAR(MAX) NOT NULL,             -- JSON con preferenze
    DataModifica DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE UNIQUE INDEX IX_PreferenzeUtente_Chiave ON PreferenzeUtente(UtenteAppId, Chiave);

-- =============================================
-- CONFIGURAZIONE E IMPOSTAZIONI
-- =============================================

CREATE TABLE ImpostazioniGantt (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OreGiornaliere INT NOT NULL DEFAULT 8,
    GiorniLavorativi NVARCHAR(50) NOT NULL DEFAULT '1,2,3,4,5',  -- 1=Lun, 7=Dom
    OraInizio TIME NOT NULL DEFAULT '08:00',
    OraFine TIME NOT NULL DEFAULT '17:00',
    TempoAttrezzaggioDefault INT NOT NULL DEFAULT 30,            -- Minuti
    AbilitaTempoAttrezzaggio BIT NOT NULL DEFAULT 1
);

CREATE TABLE CalendarioLavoro (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Data DATE NOT NULL UNIQUE,
    IsLavorativo BIT NOT NULL DEFAULT 1,
    OraInizio TIME NULL,
    OraFine TIME NULL,
    Note NVARCHAR(500) NULL
);

CREATE TABLE ConfigurazioniPLC (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MacchinaId UNIQUEIDENTIFIER NOT NULL REFERENCES Macchine(Id) ON DELETE CASCADE,
    IndirizzoPLC NVARCHAR(50) NOT NULL,        -- IP del PLC
    Rack INT NOT NULL DEFAULT 0,
    Slot INT NOT NULL DEFAULT 1,
    DBNumber INT NOT NULL DEFAULT 1,           -- Data Block number
    Attivo BIT NOT NULL DEFAULT 1
);

-- =============================================
-- ALLEGATI E DOCUMENTI
-- =============================================

CREATE TABLE AllegatiArticoli (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodiceArticolo NVARCHAR(100) NOT NULL,
    Archivio NVARCHAR(50) NULL,
    IdArchivio INT NULL,
    NomeFile NVARCHAR(255) NOT NULL,
    PathCompleto NVARCHAR(500) NOT NULL,
    Estensione NVARCHAR(20) NOT NULL,
    Dimensione BIGINT NULL,
    IsFoto BIT NOT NULL DEFAULT 0,
    Descrizione NVARCHAR(500) NULL,
    Priorita INT NOT NULL DEFAULT 0,
    IdGanttOriginale INT NULL,
    DataCreazione DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE INDEX IX_AllegatiArticoli_Codice ON AllegatiArticoli(CodiceArticolo);
CREATE INDEX IX_AllegatiArticoli_Archivio ON AllegatiArticoli(Archivio, IdArchivio);

-- =============================================
-- STORICO E LOG
-- =============================================

CREATE TABLE StoricoProgrammazione (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CommessaId UNIQUEIDENTIFIER NOT NULL REFERENCES Commesse(Id) ON DELETE CASCADE,
    DataModifica DATETIME2 NOT NULL DEFAULT GETDATE(),
    NumeroMacchinaPrecedente NVARCHAR(50) NULL,
    NumeroMacchinaNuovo NVARCHAR(50) NULL,
    StatoPrecedente INT NULL,
    StatoNuovo INT NULL,
    Utente NVARCHAR(100) NULL,
    Note NVARCHAR(500) NULL
);
CREATE INDEX IX_StoricoProgrammazione_Commessa ON StoricoProgrammazione(CommessaId);
CREATE INDEX IX_StoricoProgrammazione_Data ON StoricoProgrammazione(DataModifica);

CREATE TABLE SyncStates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Modulo NVARCHAR(50) NOT NULL UNIQUE,       -- Es: "Commesse", "Clienti", "Articoli"
    UltimaSync DATETIME2 NULL,
    Stato NVARCHAR(50) NOT NULL DEFAULT 'Idle',
    Messaggio NVARCHAR(500) NULL
);

CREATE TABLE LogEventi (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    Livello NVARCHAR(20) NOT NULL,
    Categoria NVARCHAR(100) NOT NULL,
    Messaggio NVARCHAR(MAX) NOT NULL,
    Eccezione NVARCHAR(MAX) NULL
);

CREATE TABLE LogSync (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
    Modulo NVARCHAR(50) NOT NULL,
    Operazione NVARCHAR(100) NOT NULL,
    RecordAffetti INT NOT NULL DEFAULT 0,
    Durata INT NOT NULL DEFAULT 0,             -- Millisecondi
    Successo BIT NOT NULL DEFAULT 1,
    Errore NVARCHAR(MAX) NULL
);
```

### 3.2 Enum Values

```csharp
// StatoCommessa (sincronizzato con Mago)
public enum StatoCommessa
{
    Sconosciuto = 0,
    Aperta = 1,
    InLavorazione = 2,
    Completata = 3,
    Chiusa = 4
}

// StatoMacchina
public enum StatoMacchina
{
    Sconosciuto = 0,
    Ferma = 1,
    InFunzione = 2,
    Manutenzione = 3,
    Allarme = 4
}

// StatoProgramma (stato interno, NON sincronizzato)
public enum StatoProgramma
{
    NonProgrammata = 0,
    Programmata = 1,
    InProduzione = 2,
    Completata = 3,
    Archiviata = 4
}
```

### 3.3 Database Esterni

#### Mago (ERP - Solo lettura)
```sql
-- Vista/Tabella usata per sync commesse
-- Database: TODESCATO_NET (o altro nome aziendale)
SELECT 
    SaleOrdId,
    InternalOrdNo,
    ExternalOrdNo,
    Line,
    Item,                    -- Codice articolo
    Description,
    Qty,                     -- Quantità
    UoM,                     -- Unità misura
    ExpectedDeliveryDate,    -- Data consegna
    CompanyName,             -- Nome cliente
    YourReference,           -- Riferimento cliente
    Delivered                -- Flag consegnato
FROM MA_SaleOrdDetails
INNER JOIN MA_SaleOrd ON ...
INNER JOIN MA_CustSupp ON ...
WHERE Delivered = 0          -- Solo non consegnate
```

#### Gantt (Legacy - Import anime)
```sql
-- Tabella anime nel vecchio sistema
-- Database: Gantt
SELECT 
    IdArchivio,
    CodiceArticolo,
    DescrizioneArticolo,
    MacchineSuDisponibili,
    Colla, Sabbia, Vernice,
    Imballo,
    -- altri campi...
FROM tAnime
```

---

## 4. Configurazione

### 4.1 appsettings.json (Base)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 4.2 appsettings.Database.json (Connection Strings)
```json
{
  "ConnectionStrings": {
    "MESManagerDb": "Server=localhost\\SQLEXPRESS01;Database=MESManager;Trusted_Connection=True;TrustServerCertificate=True;",
    "MagoDb": "Data Source=192.168.1.72\\SQLEXPRESS01;Initial Catalog=TODESCATO_NET;User Id=Gantt;Password=***;TrustServerCertificate=True;Connection Timeout=30;",
    "GanttDb": "Server=192.168.1.230\\SQLEXPRESS01;Database=Gantt;User Id=fab;Password=***;TrustServerCertificate=True;"
  }
}
```

### 4.3 appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5156"
      }
    }
  }
}
```

### 4.4 Configurazione Macchine PLC (11 macchine standard)
```sql
INSERT INTO Macchine (Id, Codice, Nome, OrdineVisualizazione, IndirizzoPLC, AttivaInGantt) VALUES
(NEWID(), 'M001', 'Macchina 1', 1, '192.168.0.11', 1),
(NEWID(), 'M002', 'Macchina 2', 2, '192.168.0.12', 1),
(NEWID(), 'M003', 'Macchina 3', 3, '192.168.0.13', 1),
(NEWID(), 'M004', 'Macchina 4', 4, '192.168.0.14', 1),
(NEWID(), 'M005', 'Macchina 5', 5, '192.168.0.15', 1),
(NEWID(), 'M006', 'Macchina 6', 6, '192.168.0.16', 1),
(NEWID(), 'M007', 'Macchina 7', 7, '192.168.0.17', 1),
(NEWID(), 'M008', 'Macchina 8', 8, '192.168.0.18', 1),
(NEWID(), 'M009', 'Macchina 9', 9, '192.168.0.19', 1),
(NEWID(), 'M010', 'Macchina 10', 10, '192.168.0.20', 1),
(NEWID(), 'M011', 'Macchina 11', 11, '192.168.0.21', 1);
```

---

## 5. Setup Iniziale

### 5.1 Prerequisiti
```powershell
# Verifica .NET SDK
dotnet --version  # Deve essere 8.0.x

# Verifica SQL Server
sqlcmd -S localhost\SQLEXPRESS01 -Q "SELECT @@VERSION"
```

### 5.2 Clona Repository
```powershell
git clone <repository-url> MESManager
cd MESManager
```

### 5.3 Crea Database
```powershell
# Crea database vuoto
sqlcmd -S localhost\SQLEXPRESS01 -Q "CREATE DATABASE MESManager"

# Applica migrations Entity Framework
cd MESManager.Web
dotnet ef database update --project ../MESManager.Infrastructure
```

### 5.4 Configura Connection Strings
```powershell
# Copia template
Copy-Item appsettings.Secrets.json.template appsettings.Secrets.json

# Modifica con le tue credenziali
notepad appsettings.Secrets.json
```

### 5.5 Inserisci Dati Base
```powershell
# Esegui script SQL per macchine e configurazioni
sqlcmd -S localhost\SQLEXPRESS01 -d MESManager -i scripts/setup/insert-plc-machines.sql
sqlcmd -S localhost\SQLEXPRESS01 -d MESManager -i scripts/setup/insert-plc-configurations.sql

# Inizializza impostazioni Gantt
sqlcmd -S localhost\SQLEXPRESS01 -d MESManager -Q "INSERT INTO ImpostazioniGantt DEFAULT VALUES"
```

### 5.6 Avvia Applicazione
```powershell
# Sviluppo
dotnet run --project MESManager.Web

# Produzione
dotnet publish -c Release -o publish
.\publish\MESManager.Web.exe
```

---

## 6. Deployment

### 6.1 Build per Produzione
```powershell
# Pubblica tutti i progetti
.\scripts\deploy\publish-win.ps1

# Output in cartella publish/
# - MESManager.Web.exe
# - MESManager.Worker.exe
# - MESManager.PlcSync.exe
```

### 6.2 Installazione come Servizi Windows
```powershell
# Web Application
sc.exe create "MESManager.Web" binPath="C:\MESManager\publish\MESManager.Web.exe" start=auto

# Worker (Sync Mago)
sc.exe create "MESManager.Worker" binPath="C:\MESManager\publish\MESManager.Worker.exe" start=auto

# PlcSync (Lettura PLC)
sc.exe create "MESManager.PlcSync" binPath="C:\MESManager\publish\MESManager.PlcSync.exe" start=auto
```

### 6.3 Porte di Rete
| Servizio | Porta | Protocollo |
|----------|-------|------------|
| Web App | 5156 | HTTP |
| SQL Server | 1433 | TCP |
| PLC Siemens | 102 | TCP (S7) |

### 6.4 Firewall
```powershell
# Apri porta web
netsh advfirewall firewall add rule name="MESManager Web" dir=in action=allow protocol=TCP localport=5156

# Apri porta PLC (solo rete interna)
netsh advfirewall firewall add rule name="MESManager PLC" dir=out action=allow protocol=TCP remoteport=102
```

---

## 7. Integrazione PLC

### 7.1 PLC Supportati
- **Siemens S7-1200** (testato)
- **Siemens S7-1500** (compatibile)
- **Siemens S7-300/400** (compatibile con Sharp7)

### 7.2 Configurazione PLC

#### Requisiti TIA Portal
1. **PUT/GET Communication** abilitato
2. **Ottimizzazione accesso DB** disabilitata
3. **Sicurezza PLC** configurata per accesso remoto

#### Data Block (DB1) - Struttura Standard
```
Offset  Tipo    Nome                    Descrizione
0       INT     CicliFatti              Contatore cicli completati
2       INT     QuantitaDaProdurre      Target produzione
4       INT     CicliScarti             Contatore scarti
6       INT     BarcodeLavorazione      Codice lavorazione attiva
8       INT     NumeroOperatore         ID operatore loggato
10      INT     TempoMedioRilevato      Tempo ciclo rilevato (sec*10)
12      INT     TempoMedio              Tempo ciclo impostato
14      INT     Figure                  Figure per ciclo
16      INT     StatoMacchina           0=Ferma, 1=InCiclo, 2=Pausa
18      BOOL    QuantitaRaggiunta       Flag target raggiunto
```

### 7.3 Configurazione IP PLC
```sql
-- Assegna IP a macchina
UPDATE Macchine 
SET IndirizzoPLC = '192.168.0.11' 
WHERE Codice = 'M001';
```

### 7.4 Test Connessione
```powershell
# Ping PLC
ping 192.168.0.11

# Test porta S7
Test-NetConnection -ComputerName 192.168.0.11 -Port 102
```

---

## 8. Integrazione ERP Mago

### 8.1 Requisiti Mago
- **Versione:** Mago.Net 4.x o superiore
- **Accesso:** Utente SQL con permessi SELECT su:
  - `MA_SaleOrd` (Testata ordini)
  - `MA_SaleOrdDetails` (Righe ordini)
  - `MA_CustSupp` (Clienti)
  - `MA_Items` (Articoli)

### 8.2 Query Sync Commesse
```sql
-- Query usata per sincronizzare commesse aperte
SELECT 
    d.SaleOrdId,
    o.InternalOrdNo,
    o.ExternalOrdNo,
    d.Line,
    d.Item AS CodiceArticolo,
    d.Description,
    d.Qty AS Quantita,
    d.UoM,
    d.ExpectedDeliveryDate AS DataConsegna,
    c.CompanyName AS NomeCliente,
    o.YourReference AS RiferimentoCliente,
    d.Delivered
FROM MA_SaleOrdDetails d
INNER JOIN MA_SaleOrd o ON d.SaleOrdId = o.SaleOrdId
INNER JOIN MA_CustSupp c ON o.CustSupp = c.CustSupp
WHERE d.Delivered = 0
  AND o.DocumentDate >= DATEADD(MONTH, -6, GETDATE())
ORDER BY o.InternalOrdNo, d.Line
```

### 8.3 Mapping Stati
| Stato Mago | StatoCommessa MES |
|------------|-------------------|
| `Delivered = 0` | Aperta (1) |
| `Delivered = 1` | Chiusa (4) |
| (logica interna) | InLavorazione (2) |
| (logica interna) | Completata (3) |

### 8.4 Frequenza Sync
| Modulo | Intervallo | Note |
|--------|------------|------|
| Commesse | 5 minuti | Solo non consegnate |
| Clienti | 1 ora | Anagrafica completa |
| Articoli | 1 ora | Anagrafica completa |

---

## 📋 Checklist Replica Sistema

- [ ] Installato .NET 8.0 SDK
- [ ] Installato SQL Server Express
- [ ] Creato database MESManager
- [ ] Eseguito migrations EF Core
- [ ] Configurato appsettings con connection strings
- [ ] Inserite macchine base (11)
- [ ] Configurati IP PLC per ogni macchina
- [ ] Verificata connessione a Mago (se presente)
- [ ] Verificata connessione a Gantt (se import)
- [ ] Testata compilazione `dotnet build`
- [ ] Testato avvio `dotnet run`
- [ ] Configurati servizi Windows (produzione)
- [ ] Configurato firewall

---

## 🆘 Troubleshooting

### Errore: "Cannot connect to SQL Server"
```powershell
# Verifica servizio SQL
Get-Service | Where-Object { $_.Name -like "*SQL*" }

# Avvia servizio
Start-Service "MSSQL`$SQLEXPRESS01"
```

### Errore: "Cannot connect to PLC"
```powershell
# Verifica rete
ping 192.168.0.11

# Aggiungi IP secondario per subnet PLC
netsh interface ip add address "Ethernet" 192.168.0.100 255.255.255.0
```

### Errore: "Migration failed"
```powershell
# Ricrea migrations
cd MESManager.Infrastructure
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

**Documento generato per MESManager v1.0**
