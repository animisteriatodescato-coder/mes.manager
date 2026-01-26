# Sistema di Preferenze Utente con Indicatore Colore

## 📋 Riepilogo Implementazione

### ✅ Completato

Implementazione completa del sistema di preferenze per utente che salva le impostazioni delle griglie nel database invece che nel localStorage. Ogni utente (Irene, Fabio, Giulia) avrà le proprie preferenze personali salvate sul server.

---

## 🎨 Nuove Funzionalità

### 1. **Preferenze per Utente**
- ✅ Le preferenze delle griglie ora vengono salvate nel database associate all'utente corrente
- ✅ Ogni utente ha le proprie impostazioni (colonne, ordine, filtri, larghezze, ecc.)
- ✅ Le preferenze vengono sincronizzate tra PC e server
- ✅ Fallback automatico a localStorage se nessun utente è selezionato

### 2. **Indicatore Colore Utente**
- ✅ Riga colorata di 3px sotto l'header principale
- ✅ Ogni utente può avere il proprio colore identificativo
- ✅ Visibile solo quando un utente è selezionato
- ✅ Aggiornamento automatico al cambio utente

### 3. **Gestione Colori in Impostazioni Utenti**
- ✅ Color picker nella pagina Impostazioni → Utenti
- ✅ Anteprima del colore selezionato
- ✅ Salvataggio automatico al cambio colore

---

## 📝 File Modificati

### Backend
1. **MESManager.Domain/Entities/UtenteApp.cs**
   - Aggiunto campo `Colore` (nullable string)

2. **MESManager.Web/Services/PreferencesService.cs**
   - Integrato con `CurrentUserService` e `PreferenzeUtenteService`
   - Logica: se utente selezionato → database, altrimenti → localStorage

3. **MESManager.Infrastructure/Migrations/**
   - Migration `AddColoreToUtenteApp` applicata con successo

### Frontend
4. **MESManager.Web/Components/Shared/UserColorIndicator.razor** ⭐ NUOVO
   - Componente per visualizzare riga colorata sotto header
   - Si aggiorna automaticamente al cambio utente

5. **MESManager.Web/Components/Layout/MainLayout.razor**
   - Aggiunto `<UserColorIndicator />` sotto l'AppBar

6. **MESManager.Web/Components/Pages/Impostazioni/GestioneUtenti.razor**
   - Aggiunta colonna "Colore" con MudColorPicker
   - Metodo `CambiaColore()` per salvare il colore selezionato

### Utilità
7. **export-preferenze-localstorage.ps1** ⭐ NUOVO
   - Script PowerShell per esportare preferenze da localStorage
   - Genera SQL per inizializzare utenti con preferenze default

8. **wwwroot/export-preferenze.html** ⭐ NUOVO
   - Pagina HTML interattiva per esportare preferenze
   - Più semplice da usare: basta aprirla nel browser
   - Genera automaticamente lo script SQL

---

## 🚀 Come Utilizzare

### Passaggio 1: Esporta Preferenze dal PC Locale
Due metodi disponibili:

#### Metodo A: Pagina HTML (Consigliato)
1. Apri nel browser: `http://localhost:5156/export-preferenze.html`
2. Clicca "📦 Estrai Preferenze da localStorage"
3. Clicca "📋 Copia SQL"
4. Vai al Passaggio 2

#### Metodo B: Script PowerShell
1. Apri Chrome DevTools (F12) su `http://localhost:5156`
2. Nella Console, esegui:
   ```javascript
   JSON.stringify(Object.keys(localStorage).filter(k => k.includes('grid')).reduce((obj, key) => { obj[key] = localStorage.getItem(key); return obj; }, {}), null, 2)
   ```
3. Copia l'output JSON
4. Esegui: `.\export-preferenze-localstorage.ps1`
5. Incolla il JSON quando richiesto

### Passaggio 2: Applica SQL al Server
```sql
-- Esegui lo script generato sul database del server
sqlcmd -S <server> -d MesManager -i init-preferenze-utenti.sql
```

Oppure apri il file SQL in SQL Server Management Studio ed eseguilo manualmente.

### Passaggio 3: Assegna Colori agli Utenti
1. Vai su **Impostazioni → Utenti**
2. Clicca sul color picker per ogni utente
3. Seleziona i colori:
   - **IRENE**: #E91E63 (Rosa/Pink) 💗
   - **FABIO**: #2196F3 (Blu) 💙
   - **GIULIA**: #4CAF50 (Verde) 💚

### Passaggio 4: Seleziona Utente
1. Nel dropdown utenti (già presente nell'app)
2. Seleziona l'utente (es: IRENE)
3. Vedrai:
   - Riga colorata sotto l'header
   - Le tue preferenze personali caricate

---

## 🔧 Griglie Supportate

Tutte le griglie ora salvano le preferenze per utente:

| Griglia | Chiave Preferenza |
|---------|-------------------|
| Catalogo Commesse | `commesse-grid-settings` |
| Commesse Aperte | `commesse-aperte-grid-settings` |
| Catalogo Articoli | `articoli-grid-settings` |
| Catalogo Clienti | `clienti-grid-settings` |
| Catalogo Anime | `anime-grid-settings` |
| Programma Macchine | `programma-macchine-grid-settings` |
| PLC Realtime | `plc-realtime-grid-columns` |
| PLC Storico | `plc-storico-grid-columns` |

---

## 🎯 Vantaggi

✅ **Preferenze Personali**: Ogni utente vede le colonne come le ha configurate
✅ **Sincronizzazione**: Stesse preferenze su PC e server
✅ **Identificazione Visiva**: Colore personale sempre visibile
✅ **Backup Automatico**: Preferenze salvate nel database
✅ **Nessuna Perdita Dati**: Se cambi browser, le preferenze rimangono
✅ **Punto di Partenza**: Script per impostare configurazione default dal PC attuale

---

## 🔍 Testing

1. **Test Locale (PC con impostazioni corrette)**
   - Seleziona IRENE
   - Configura una griglia (es: ordine colonne, larghezze)
   - Ricarica la pagina
   - ✓ Le impostazioni dovrebbero essere mantenute

2. **Test Cambio Utente**
   - Configura griglia con utente IRENE
   - Cambia utente → FABIO
   - ✓ Le impostazioni di IRENE non dovrebbero apparire
   - Configura griglia diversamente per FABIO
   - Torna a IRENE
   - ✓ Le impostazioni originali di IRENE dovrebbero tornare

3. **Test Server**
   - Dopo aver esportato e applicato le preferenze
   - Apri l'app sul server
   - Seleziona IRENE
   - ✓ Dovresti vedere le stesse colonne configurate sul PC

---

## 🎨 Colori Suggeriti per Utenti

```css
IRENE:  #E91E63  /* Pink/Magenta - Distintivo e vivace */
FABIO:  #2196F3  /* Blue - Professionale e chiaro */
GIULIA: #4CAF50  /* Green - Fresco e positivo */
```

Alternative:
- IRENE: #FF1744 (Red), #9C27B0 (Purple)
- FABIO: #00BCD4 (Cyan), #3F51B5 (Indigo)
- GIULIA: #8BC34A (Light Green), #00897B (Teal)

---

## 📊 Database Schema

```sql
-- UtenteApp: aggiunto campo Colore
ALTER TABLE UtentiApp
ADD Colore NVARCHAR(50) NULL;

-- PreferenzeUtente: tabella esistente (nessuna modifica)
-- Struttura: Id, UtenteAppId, Chiave, ValoreJson, DataCreazione, UltimaModifica
```

---

## 🐛 Troubleshooting

### Problema: Preferenze non si salvano
**Causa**: Nessun utente selezionato
**Soluzione**: Seleziona un utente dal dropdown

### Problema: Riga colorata non appare
**Causa**: Colore non assegnato all'utente
**Soluzione**: Vai su Impostazioni → Utenti e assegna un colore

### Problema: Sul server vedo colonne diverse
**Causa**: Preferenze non esportate/applicate
**Soluzione**: Esegui lo script di esportazione e applica SQL al database del server

### Problema: Cambio utente ma vedo stesse preferenze
**Causa**: Cache browser
**Soluzione**: Fai un hard refresh (CTRL+F5)

---

## 📚 Architettura

```
┌─────────────────┐
│   Razor Page    │
│   (Griglia)     │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│  PreferencesService     │
│  • GetAsync<T>(key)     │
│  • SetAsync<T>(key, val)│
└─────┬──────────────┬────┘
      │              │
      │ HasUser?     │
      ├──────────────┤
      │ Yes          │ No
      ▼              ▼
┌─────────────┐   ┌──────────────┐
│  Database   │   │ localStorage │
│ (per user)  │   │  (fallback)  │
└─────────────┘   └──────────────┘
```

---

## ✨ Prossimi Passi

1. ✅ Compila il progetto
2. ✅ Applica migration al database
3. 🔲 Testa in locale con selezione utenti
4. 🔲 Esporta preferenze dal PC locale
5. 🔲 Applica preferenze al server
6. 🔲 Assegna colori agli utenti
7. 🔲 Test finale sul server

---

**Data Implementazione**: 26 Gennaio 2026
**Versione**: 1.0.0
**Status**: ✅ COMPLETATO
