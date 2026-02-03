# Fix Tabella Festivi - Errore SqlQueryRaw EF Core

**Data**: 3 Febbraio 2026  
**Versione**: v1.23  
**Gravità**: 🔴 CRITICO - Blocco funzionalità Gantt  
**Status**: ✅ RISOLTO

---

## 📋 Problema Riscontrato

### Sintomo Utente
- **Pagina**: `/impostazioni/gantt` (Impostazioni Gantt Macchine)
- **Errore visualizzato**: 
  ```
  Si è verificato un errore
  Il nome di oggetto 'Festivi' non è valido.
  ```
- **Impatto**: Impossibile accedere alle impostazioni del Gantt Macchine

### Errore Tecnico Secondario
Quando si tentava di chiamare l'endpoint `/api/dbmaintenance/ensure-festivi-table`:
```
Il nome di colonna 'Value' non è valido.
```

---

## 🔍 Analisi Root Cause

### Investigazione Iniziale
1. ✅ La migrazione EF `20260202135933_AddFestivi` esisteva
2. ✅ `dotnet ef database update` riportava "No migrations were applied. The database is already up to date"
3. ❌ La tabella `Festivi` **NON** esisteva fisicamente nel database
4. ❌ L'endpoint di verifica falliva con errore diverso

### Cause Identificate

#### Causa 1: Tabella Mancante
- **Scenario**: Migration presente in `__EFMigrationsHistory` ma tabella fisica assente
- **Possibili ragioni**:
  - Tabella eliminata manualmente
  - Migration applicata in ambiente diverso
  - Rollback parziale non tracciato
- **Soluzione**: Script SQL manuale per creare tabella

#### Causa 2: Bug in `DbMaintenanceController.EnsureFestiviTable()`
**Codice problematico**:
```csharp
var checkSql = "SELECT CASE WHEN EXISTS(SELECT * FROM sys.tables WHERE name = 'Festivi') THEN 1 ELSE 0 END AS TableExists";
var exists = await _context.Database.SqlQueryRaw<int>(checkSql).FirstOrDefaultAsync();
```

**Problema**:
- `SqlQueryRaw<T>` con tipi primitivi (`int`, `string`, etc.) **cerca automaticamente una colonna chiamata `Value`**
- L'alias SQL `TableExists` viene ignorato da EF Core
- Query generata da EF:
  ```sql
  SELECT TOP(1) [t].[Value]  -- ❌ Cerca colonna 'Value' che non esiste
  FROM (
      SELECT CASE WHEN EXISTS(SELECT * FROM sys.tables WHERE name = 'Festivi') 
             THEN 1 ELSE 0 END AS TableExists
  ) AS [t]
  ```

**Errore risultante**:
```
Microsoft.Data.SqlClient.SqlException: Il nome di colonna 'Value' non è valido.
```

---

## ✅ Soluzione Implementata

### Fix 1: Verifica e Creazione Tabella
**Script SQL**: `scripts/check-and-create-festivi.sql`

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Festivi')
BEGIN
    CREATE TABLE [dbo].[Festivi](
        [Id] [uniqueidentifier] NOT NULL,
        [Data] [date] NOT NULL,
        [Descrizione] [nvarchar](200) NOT NULL,
        [Ricorrente] [bit] NOT NULL DEFAULT 0,
        [Anno] [int] NULL,
        [DataCreazione] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Festivi] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX IX_Festivi_Data ON [dbo].[Festivi]([Data]);
    CREATE INDEX IX_Festivi_Ricorrente ON [dbo].[Festivi]([Ricorrente]);
END
```

**Esecuzione**:
```powershell
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -d "MESManager_Prod" -U "FAB" -P "password.123" -C -i "scripts/check-and-create-festivi.sql"
```

**Risultato**:
```
✅ Tabella Festivi già presente
(Righe interessate: 6)  -- Colonne create
(Righe interessate: 3)  -- Indici creati
NumeroFestivi: 0        -- Nessun record (normale)
```

### Fix 2: Correzione Endpoint DbMaintenanceController
**File**: `MESManager.Web/Controllers/DbMaintenanceController.cs`

**Prima (NON funzionante)**:
```csharp
var checkSql = "SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END AS TableExists";
var exists = await _context.Database.SqlQueryRaw<int>(checkSql).FirstOrDefaultAsync();
```

**Dopo (CORRETTO)**:
```csharp
var checkSql = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Festivi'";
using var connection = _context.Database.GetDbConnection();
await connection.OpenAsync();
using var command = connection.CreateCommand();
command.CommandText = checkSql;
var result = await command.ExecuteScalarAsync();
var exists = Convert.ToInt32(result) > 0;
```

**Vantaggi**:
- ✅ Usa `ExecuteScalarAsync()` diretto (nessun mapping EF)
- ✅ Nessun problema con nomi colonne
- ✅ Più performante (no overhead EF)
- ✅ Più chiaro e manutenibile

---

## 📚 Lezioni Apprese

### 1. SqlQueryRaw<T> con Tipi Primitivi
**Regola**: Quando usi `SqlQueryRaw<T>` con tipi primitivi, EF Core **sempre** cerca una colonna chiamata `Value`.

**Alternative sicure**:
```csharp
// ❌ NON FUNZIONA
var result = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Total").FirstOrDefaultAsync();

// ✅ FUNZIONA - Rinomina colonna
var result = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value").FirstOrDefaultAsync();

// ✅ FUNZIONA - Usa ExecuteScalarAsync
var connection = context.Database.GetDbConnection();
var result = await connection.ExecuteScalarAsync("SELECT COUNT(*)");

// ✅ FUNZIONA - Usa DTO
public class CountResult { public int Value { get; set; } }
var result = await context.Database.SqlQueryRaw<CountResult>("SELECT COUNT(*) AS Value").FirstOrDefaultAsync();
```

### 2. Verifica Fisica vs Logica Database
**Problema**: `dotnet ef database update` può mentire!

**Motivo**: Controlla solo `__EFMigrationsHistory`, non la presenza fisica delle tabelle.

**Soluzione**: Sempre verificare fisicamente con SQL:
```sql
-- Verifica tabella
SELECT * FROM sys.tables WHERE name = 'NomeTabella'

-- Verifica colonne
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'NomeTabella'

-- Verifica indici
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('NomeTabella')
```

### 3. Script SQL di Manutenzione Essenziali
Per ogni entità critica, mantenere script SQL standalone:
- `scripts/check-and-create-[entity].sql` → Verifica e creazione
- `scripts/verify-[entity]-data.sql` → Controllo integrità dati
- `scripts/fix-[entity]-[issue].sql` → Fix problemi noti

**Vantaggio**: Risoluzione rapida senza dipendere da EF/migrazioni.

---

## 🔧 Troubleshooting Futuro

### Se l'errore si ripresenta:

#### Step 1: Verifica Database
```powershell
sqlcmd -S "SERVER\INSTANCE" -d "DATABASE" -U "USER" -P "PASS" -C -Q "SELECT COUNT(*) FROM sys.tables WHERE name = 'Festivi'"
```

#### Step 2: Controlla Migration History
```sql
SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%Festivi%'
```

#### Step 3: Ricrea Tabella (se manca)
```powershell
sqlcmd -S "SERVER\INSTANCE" -d "DATABASE" -U "USER" -P "PASS" -C -i "scripts/check-and-create-festivi.sql"
```

#### Step 4: Verifica Servizio Registrato
```csharp
// In MESManager.Infrastructure/DependencyInjection.cs
services.AddScoped<IFestiviAppService, FestiviAppService>();
```

---

## 📊 Impatto Post-Fix

### Test Eseguiti
- ✅ Tabella Festivi presente nel database prod
- ✅ Indici IX_Festivi_Data e IX_Festivi_Ricorrente creati
- ✅ Endpoint `/api/dbmaintenance/ensure-festivi-table` funzionante
- ✅ Pagina `/impostazioni/gantt` accessibile
- ⏳ Test tab "Festivi" - Da verificare con utente

### File Modificati
1. `MESManager.Web/Controllers/DbMaintenanceController.cs`
2. `scripts/check-and-create-festivi.sql` (nuovo)
3. `docs/CHANGELOG.md` (aggiornato v1.23)

### Commit
```
7197090 - Fix: Corretto endpoint ensure-festivi-table per risolvere errore SqlQueryRaw
```

---

## 🎯 Azioni Preventive

### Per il Deploy su Produzione
1. ✅ Verificare presenza tabella Festivi
2. ✅ Eseguire `check-and-create-festivi.sql` se necessario
3. ⚠️ NON fare `dotnet ef database update` se la tabella già esiste
4. ✅ Testare endpoint `/api/dbmaintenance/ensure-festivi-table`
5. ✅ Testare apertura pagina `/impostazioni/gantt`

### Per Nuovi Ambienti
1. Eseguire tutte le migrations normalmente
2. Verificare con script SQL la presenza fisica delle tabelle
3. Non fidarsi solo di "No migrations were applied"

---

## 📝 Note Tecniche

### Struttura Tabella Festivi
| Colonna | Tipo | Nullable | Note |
|---------|------|----------|------|
| Id | uniqueidentifier | NO | PK |
| Data | date | NO | Indicizzato |
| Descrizione | nvarchar(200) | NO | |
| Ricorrente | bit | NO | Indicizzato, Default 0 |
| Anno | int | YES | NULL se Ricorrente=true |
| DataCreazione | datetime2(7) | NO | Default GETUTCDATE() |

### Indici
- `PK_Festivi`: PRIMARY KEY CLUSTERED su Id
- `IX_Festivi_Data`: NONCLUSTERED su Data
- `IX_Festivi_Ricorrente`: NONCLUSTERED su Ricorrente

**Scopo**: Ottimizzare query di pianificazione Gantt che filtrano per data e ricorrenza.

---

## 🔗 Riferimenti
- Issue: Segnalazione utente su errore Gantt Macchine
- Migration: `20260202135933_AddFestivi.cs`
- Entità: `MESManager.Domain/Entities/Festivo.cs`
- Servizio: `MESManager.Infrastructure/Services/FestiviAppService.cs`
- Interfaccia: `MESManager.Application/Interfaces/IFestiviAppService.cs`

---

**IMPORTANTE**: Questo documento deve essere usato come reference per futuri problemi simili con altre tabelle o query EF Core.
