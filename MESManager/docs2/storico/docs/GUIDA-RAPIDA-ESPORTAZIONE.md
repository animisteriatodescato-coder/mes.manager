# 🚀 GUIDA RAPIDA - Esportazione Preferenze

## Metodo Consigliato: Pagina HTML

### 1️⃣ Apri la Pagina di Esportazione
Nel browser, vai su:
```
http://localhost:5156/export-preferenze.html
```

### 2️⃣ Estrai Preferenze
- Clicca il pulsante **"📦 Estrai Preferenze da localStorage"**
- Aspetta che lo script SQL venga generato
- Vedrai statistiche sul numero di preferenze trovate

### 3️⃣ Copia lo Script SQL
- Clicca **"📋 Copia SQL"**
- Lo script è ora negli appunti

### 4️⃣ Applica al Database
Apri SQL Server Management Studio (o usa sqlcmd) e:

```sql
-- Incolla lo script copiato ed eseguilo
-- Il file si chiama: init-preferenze-utenti.sql
```

Oppure da command line:
```powershell
# Salva lo script in un file e poi:
sqlcmd -S localhost -d MesManager -i init-preferenze-utenti.sql
```

### 5️⃣ Verifica
Esegui lo script di verifica:
```sql
-- Esegui: verifica-preferenze-utenti.sql
-- Dovresti vedere:
-- - 3 utenti (IRENE, FABIO, GIULIA)
-- - Colori assegnati
-- - Preferenze griglie per ogni utente
```

---

## 🎨 Assegna Colori agli Utenti

Vai su: **Impostazioni → Utenti**

Clicca sul color picker per ogni utente:
- 💗 **IRENE**: #E91E63 (Rosa)
- 💙 **FABIO**: #2196F3 (Blu)
- 💚 **GIULIA**: #4CAF50 (Verde)

---

## ✅ Test Finale

1. **Seleziona utente IRENE** dal dropdown
2. **Verifica riga colorata rosa** sotto l'header
3. **Apri una griglia** (es: Catalogo Commesse)
4. **Le colonne dovrebbero essere ordinate** come sul PC locale
5. **Cambia utente → FABIO**
6. **Riga diventa blu** e preferenze cambiano

---

## 🔧 Se qualcosa non funziona

### Preferenze non si caricano?
```sql
-- Verifica che le preferenze siano state salvate
SELECT * FROM PreferenzeUtente WHERE UtenteAppId IN (
    SELECT Id FROM UtentiApp WHERE Nome IN ('IRENE', 'FABIO', 'GIULIA')
);
```

### Riga colorata non appare?
```sql
-- Verifica che i colori siano assegnati
SELECT Nome, Colore FROM UtentiApp;

-- Se NULL, aggiorna manualmente:
UPDATE UtentiApp SET Colore = '#E91E63' WHERE Nome = 'IRENE';
UPDATE UtentiApp SET Colore = '#2196F3' WHERE Nome = 'FABIO';
UPDATE UtentiApp SET Colore = '#4CAF50' WHERE Nome = 'GIULIA';
```

---

## 📞 Supporto

File di supporto creati:
- ✅ `export-preferenze.html` - Pagina interattiva esportazione
- ✅ `export-preferenze-localstorage.ps1` - Script PowerShell
- ✅ `verifica-preferenze-utenti.sql` - Verifica database
- ✅ `PREFERENZE-UTENTE-IMPLEMENTAZIONE.md` - Documentazione completa

Tutto pronto! 🎉
