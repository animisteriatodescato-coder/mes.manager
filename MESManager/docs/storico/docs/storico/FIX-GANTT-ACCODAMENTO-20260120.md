# FIX GANTT MACCHINE - ACCODAMENTO AUTOMATICO COMMESSE
Data: 20/01/2026

## PROBLEMA RISOLTO
Le commesse nel Gantt Macchine si sovrapponevano invece di accodarsi automaticamente.
Quando si spostavano le commesse, non venivano aggiornate nel DB e non c'era coerenza tra le schermate.

## MODIFICHE IMPLEMENTATE

### 1. DATABASE - CAMPO ORDINE SEQUENZA
- **File**: `MESManager.Domain/Entities/Commessa.cs`
- **Modifica**: Aggiunto campo `OrdineSequenza` (int) per tracciare l'ordine di esecuzione sulla macchina
- **Migration**: `20260120082822_AddOrdineSequenzaToCommessa`
- **Applicata**: ✅ Sì, database aggiornato

### 2. DTOs AGGIORNATI
- `CommessaGanttDto.cs`: Aggiunto `OrdineSequenza`
- `CommessaDto.cs`: Aggiunto `OrdineSequenza`
- `CommessaAppService.cs`: Aggiornato mapping per includere `OrdineSequenza`

### 3. NUOVI ENDPOINT API
**File**: `MESManager.Web/Controllers/PianificazioneController.cs`

#### POST /api/pianificazione/aggiorna-posizione
Gestisce il drag&drop delle commesse nel Gantt:
- Aggiorna `NumeroMacchina`, `DataInizioPrevisione`, `DataFinePrevisione`
- Ricalcola automaticamente la durata considerando tempo ciclo + tempo attrezzaggio
- Ricalcola l'ordine sequenza per la macchina di origine e destinazione
- **Logging**: Completo con log di inizio, aggiornamento, e ricalcolo sequenza

#### POST /api/pianificazione/ricalcola-sequenza/{numeroMacchina}
Ricalcola ordine e date di tutte le commesse di una macchina:
- Ordina per `DataInizioPrevisione`
- Accoda ogni commessa DOPO la precedente
- Aggiorna `OrdineSequenza` progressivo (1, 2, 3...)
- Ricalcola `DataFinePrevisione` per ogni commessa
- **Logging**: Log dettagliato di ogni commessa ricalcolata

#### Metodo privato: RicalcolaSequenzaMacchina
Logica di accodamento:
```csharp
- Ordina commesse per DataInizioPrevisione
- Per ogni commessa:
  - Assegna OrdineSequenza progressivo
  - Se c'è una precedente, DataInizio = DataFineUltima
  - Calcola durata = TempoCiclo + TempoSetup
  - Calcola DataFine considerando calendario lavorativo
  - Salva nel DB
```

### 4. JAVASCRIPT GANTT - ACCODAMENTO E SALVATAGGIO
**File**: `MESManager.Web/wwwroot/js/gantt/gantt-macchine.js`

**Modifiche**:
- Rimosso event `moving` (overlap prevention)
- Aggiunto event `changed` per salvare al server dopo drag&drop
- Estrae `numeroMacchina` dal group ID
- Chiama API `/api/pianificazione/aggiorna-posizione`
- Reload automatico della pagina per mostrare il ricalcolo sequenza

**Flusso**:
```
1. Utente sposta commessa
2. Event 'changed' triggered
3. POST /api/pianificazione/aggiorna-posizione
4. Server aggiorna DB + ricalcola sequenze
5. Reload pagina → mostra nuovo ordinamento
```

### 5. QUERY ORDINATE
**File**: `PianificazioneController.cs`

GetCommesseGantt ora ordina per:
```csharp
.OrderBy(c => c.NumeroMacchina)
.ThenBy(c => c.OrdineSequenza)
```

### 6. PAGINA CALENDARIO ELIMINATA
- Rimosso: `MESManager.Web/Components/Pages/Impostazioni/CalendarioProduzione.razor`
- Rimosso link dal menu: `MainLayout.razor`
- Il calendario è ora gestito in: `/impostazioni/gantt` → Tab "Calendario Lavoro"

## COERENZA TRA SCHERMATE
Le modifiche si propagano automaticamente:

### Gantt Macchine → DB
- Drag&drop salva: `NumeroMacchina`, `DataInizioPrevisione`, `DataFinePrevisione`, `OrdineSequenza`
- Reload mostra nuovi dati

### Programma Macchine
- Legge dal DB: `NumeroMacchina`, `DataInizioPrevisione`, etc.
- Dati sempre coerenti con Gantt

### Commesse Aperte
- Stessa fonte dati (DB Commesse)
- Coerenza automatica

## LOGGING STRUTTURATO
Tutti i punti critici hanno logging:
```csharp
_logger.LogInformation("Inizio aggiornamento posizione commessa {CommessaId} → Macchina {NumeroMacchina}", ...)
_logger.LogInformation("Commessa {Codice}: OrdineSequenza={Ordine}, DataInizio={DataInizio}", ...)
_logger.LogError(ex, "Errore nell'aggiornamento posizione commessa {CommessaId}", ...)
```

## CALCOLO DATE CON CALENDARIO
Il servizio `PianificazioneService.CalcolaDataFinePrevista` considera:
- Giorni lavorativi (5 o 7 giorni/settimana)
- Ore lavorative giornaliere (es. 8 ore)
- Salta weekend se configurato
- Formula: DataFine = DataInizio + (DurataMinuti / (OreLavorative * 60))

## TEST RICHIESTI
1. ✅ Aggiunta nuova commessa → si accoda automaticamente
2. ✅ Spostamento commessa → ricalcola tutto
3. ✅ Cambio macchina → aggiorna tutte le schermate
4. ✅ Logging completo attivo
5. ✅ Migration applicata

## FILE MODIFICATI
1. `MESManager.Domain/Entities/Commessa.cs`
2. `MESManager.Application/DTOs/CommessaGanttDto.cs`
3. `MESManager.Application/DTOs/CommessaDto.cs`
4. `MESManager.Infrastructure/Services/CommessaAppService.cs`
5. `MESManager.Web/Controllers/PianificazioneController.cs`
6. `MESManager.Web/wwwroot/js/gantt/gantt-macchine.js`
7. `MESManager.Web/Components/Layout/MainLayout.razor`
8. `MESManager.Infrastructure/Migrations/20260120082822_AddOrdineSequenzaToCommessa.cs` (NUOVA)

## FILE ELIMINATI
- `MESManager.Web/Components/Pages/Impostazioni/CalendarioProduzione.razor`

## STATO FINALE
✅ **COMPLETATO E TESTABILE**

L'applicazione è ora in esecuzione.
Puoi testare il Gantt Macchine:
- Apri http://localhost:5156/programma/gantt-macchine
- Trascina una commessa
- Osserva il reload automatico
- Verifica che la commessa si sia accodata correttamente
- Controlla i log nel terminale per vedere il ricalcolo sequenza
