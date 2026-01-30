# 📝 Modifiche Pendenti - Buffer Pre-Release

> **SCOPO**: Questo file raccoglie le modifiche fatte durante le sessioni di sviluppo.
> Prima di ogni release/deploy, queste modifiche vanno consolidate in `CHANGELOG.md`.
>
> **WORKFLOW COMPLETO**: Vedi [WORKFLOW-PUBBLICAZIONE.md](WORKFLOW-PUBBLICAZIONE.md)
>
> **TRIGGER**: Quando l'utente dice "pubblica" o "deploy", l'AI segue automaticamente il workflow.

---

## 🔄 Modifiche Non Ancora Consolidate

### Session 30 Gennaio 2026 - Chat 1

#### 1. Fix Macchina 11 Non Visibile in Programma Macchine
**Problema**: Le macchine erano hardcoded (`M001-M010`) nel JavaScript, quindi la macchina 11 non appariva anche se assegnata a commesse.

**Causa Root**: Lista statica in `programma-macchine-grid.js` invece che caricamento dinamico dal database.

**Soluzione Implementata**:
- `MESManager.Web/wwwroot/lib/ag-grid/programma-macchine-grid.js`
  - Rimossa lista hardcoded `const allMachines = ['M001'...'M010']`
  - Aggiunta variabile dinamica `let allMachines = []`
  - Aggiunta funzione `setMachines(machines)` per caricare dal backend
  - Esportata funzione nel return object
  
- `MESManager.Web/Components/Pages/Programma/ProgrammaMacchine.razor`
  - Aggiunta variabile `private List<MacchinaDto> _macchine`
  - Caricamento macchine da `api/Macchine` in `OnInitializedAsync`
  - Chiamata `programmaMacchineGrid.setMachines(_macchine)` prima di `init()`

**Impatto**: Ora tutte le macchine presenti in "Impostazioni Gantt Macchine" appaiono automaticamente nel Programma.

**Regola Appresa**: MAI hardcodare liste che possono crescere. Sempre caricare dal database.

---

#### 2. Unificazione Configurazione IP Macchine
**Problema**: L'IP modificato in "Impostazioni Gantt Macchine" non veniva usato dal servizio PlcSync (leggeva da file JSON separati).

**Causa Root**: Due fonti di verità: database (IndirizzoPLC) e file JSON (`Configuration/machines/*.json`).

**Soluzione Implementata**:
- `MESManager.PlcSync/Worker.cs`
  - Aggiunto inject `IDbContextFactory<MesManagerDbContext>`
  - Modificato `LoadMachineConfigsAsync()`:
    - Prima carica gli IP dal database
    - Poi carica i file JSON per gli offset PLC
    - Sovrascrive l'IP del JSON con quello del database se presente
  - Log quando IP viene aggiornato da database

**Architettura Risultante**:
- **Database (Tabella Macchine)**: Fonte verità per IP e configurazione base
- **File JSON**: Solo per offset PLC specifici (struttura memoria PLC)

**Impatto**: Modificare l'IP in Impostazioni Gantt Macchine ora funziona effettivamente.

**Regola Appresa**: Una sola fonte di verità per ogni dato. Se serve in più posti, leggere dalla fonte primaria.

---

#### 3. Impostazioni Sfondo Home
**Problema**: Non era possibile personalizzare lo sfondo della Home page.

**Soluzione Implementata**:
- `MESManager.Web/Components/Pages/Impostazioni/ImpostazioniGenerali.razor`
  - Riscritta completamente la pagina
  - Aggiunto upload immagine sfondo (max 5MB)
  - Anteprima immagine attuale
  - Usa `AppSettingsService` esistente

- `MESManager.Web/Components/Pages/Home.razor`
  - Aggiunto sfondo dinamico con `background-image`
  - Overlay semi-trasparente per leggibilità
  - Stili per card con sfondo

- `MESManager.Web/Program.cs`
  - Registrato `AppSettingsService` come Singleton (mancava!)

**File Configurazione**: `wwwroot/app-settings.json` contiene `BackgroundImageUrl`

---

#### 4. Icona 💩 per Programma Irene
**Richiesta**: Sostituire icona cuore con emoji cacca rosa per "Programma Irene" nel menu.

**Soluzione**:
- `MESManager.Web/Components/Layout/MainLayout.razor`
  - Usato `TitleContent` invece di `Icon` per MudNavGroup
  - Emoji 💩 con filtro CSS per tonalità rosa

---

## ✅ Modifiche Consolidate (Storico)

> Quando le modifiche vengono consolidate nel CHANGELOG, spostarle qui con riferimento alla versione.

*(Nessuna modifica consolidata ancora)*

---

## 📋 Template Nuova Modifica

```markdown
#### [Numero]. [Titolo Breve]
**Problema**: [Descrizione del problema originale]

**Causa Root**: [Perché succedeva]

**Soluzione Implementata**:
- `[file/path.cs]`
  - [Descrizione modifica]

**Impatto**: [Cosa cambia per l'utente/sistema]

**Regola Appresa**: [Lezione da ricordare per il futuro]

**File Coinvolti**:
- [lista file modificati]

**Test Necessari**:
- [ ] [test da fare]

**Docs da Aggiornare**:
- [ ] [doc da aggiornare]
```

---

## 🔧 Come Consolidare

1. Crea nuova sezione in `CHANGELOG.md` con versione incrementata
2. Raggruppa le modifiche per categoria (Features, Bug Fix, Refactoring, etc.)
3. Incrementa versione in `MainLayout.razor`
4. Sposta le modifiche nella sezione "Consolidate" di questo file
5. Commit con messaggio: `v1.XX - [descrizione breve]`
