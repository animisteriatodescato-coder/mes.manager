# ⚡ QUICK START - Fix Critici PlcMagoSync

> **Status**: ✅ Tutti i fix implementati e testati  
> **Data**: 27 Novembre 2025

---

## 🔴 PRIMA DI ESEGUIRE - Configurazione Obbligatoria

### Windows (PowerShell)
```powershell
[System.Environment]::SetEnvironmentVariable("GOOGLE_SHEET_ID", "your-id", "User")
[System.Environment]::SetEnvironmentVariable("SERVICE_ACCOUNT_JSON_PATH", "C:\path\sa.json", "User")
[System.Environment]::SetEnvironmentVariable("MAGO_CONNECTION_STRING", "your-conn-string", "User")
# RIAVVIA PowerShell/IDE
```

### Linux/macOS (Bash)
```bash
export GOOGLE_SHEET_ID="your-id"
export SERVICE_ACCOUNT_JSON_PATH="/path/to/sa.json"
export MAGO_CONNECTION_STRING="your-conn-string"
```

---

## ✅ Fix Implementati

| # | Fix | File | Status |
|----|-----|------|--------|
| 1 | Unificato GoogleSheetId | ConfigMago.cs | ✅ |
| 2 | ClienteMago → 5 campi | ClienteMago.cs | ✅ |
| 3 | SQL query corretta | SyncClienti.cs | ✅ |
| 4 | Range A2:E | GoogleSheetsService.cs | ✅ |
| 5 | Protezione credenziali | config_mago.json | ✅ |
| 6 | Validazione config | Program.cs | ✅ |
| 7 | .gitignore updated | .gitignore | ✅ |

---

## 🚀 Esecuzione

```bash
# Build
dotnet build

# Run
dotnet run

# Expected output:
# == PlcMagoSync SYNC_MAGO ==
# === AVVIO SYNC MAGO ===
# == SYNC CLIENTI (CLIENTI_MAGO) ==
# Clienti letti da Mago: [N]
# SYNC CLIENTI completata.
# === SYNC COMPLETATA ===
```

---

## 📚 Leggi (nell'ordine)

1. **`README_FIXES.md`** ← START HERE (overview rapido)
2. **`SETUP_SECURITY.md`** ← Configurazione dettagliata
3. **`VERIFICATION.md`** ← Verifica tecnica
4. **`DEPLOYMENT_READY.md`** ← Checklist finale

---

## 🔧 Campi Finali

### ClienteMago (5 campi)
```csharp
- Codice
- Nome
- Email
- Note
- UltimaModifica
```

### ConfigMago (3 property)
```csharp
- MagoConnectionString
- ServiceAccountJsonPath
- GoogleSheetId
```

---

## ⚠️ IMPORTANTE

- ✅ NO credenziali hardcoded
- ✅ Usa variabili d'ambiente
- ✅ Configura prima di eseguire
- ✅ Riavvia IDE/Terminal dopo config

---

**Build**: ✅ OK  
**Security**: ✅ OK  
**Ready**: ✅ YES
