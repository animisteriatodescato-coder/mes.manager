# MESManager RAG — Assistente AI locale

Pipeline RAG (Retrieval-Augmented Generation) che indicizza l'intero codebase di MESManager in **ChromaDB** e risponde alle domande usando **Ollama** con contesto reale dal codice.

## Architettura

```
domanda
   │
   ▼
nomic-embed-text  ──► ChromaDB (cosine search) ──► top-8 chunk del codice
                                                         │
                                                         ▼
                                              llava / llama3 / mistral
                                                         │
                                                         ▼
                                               risposta in italiano
```

## Setup (una volta sola)

```powershell
cd C:\Dev\MESManager\rag
.\setup.ps1
```

Oppure manualmente:
```powershell
pip install -r requirements.txt
ollama pull nomic-embed-text
```

## Utilizzo

### 1. Indicizzazione del codebase
```powershell
cd C:\Dev\MESManager\rag
python index_codebase.py          # indicizza nuovi file (skip se già presenti)
python index_codebase.py --reset  # reindicizza tutto da zero
```

La prima indicizzazione richiede ~5-15 minuti a seconda dei file.  
Le re-indicizzazioni successive sono veloci (skip dei chunk già presenti).

### 2. Query

**Domanda singola:**
```powershell
python ask.py "come funziona il calcolo della data fine Gantt?"
python ask.py "dove viene gestita la sincronizzazione con Mago?"
python ask.py "qual è la struttura dell'entità Commessa?"
```

**Modalità interattiva** (con memoria della conversazione):
```powershell
python ask.py
```
Comandi speciali in modalità interattiva:
- `clear` — cancella la storia della conversazione
- `history` — mostra i messaggi precedenti
- `exit` — esci

## Configurazione

Modifica `ask.py` per cambiare il modello chat:
```python
CHAT_MODEL = "llama3.2"    # più veloce e preciso di llava
CHAT_MODEL = "mistral"     # ottimo per codice
CHAT_MODEL = "deepseek-coder"  # specializzato .NET/C#
```

Modifica `index_codebase.py` per aggiungere/togliere estensioni o cartelle:
```python
INCLUDE_EXTENSIONS = {".cs", ".razor", ".md", ".json", ".csproj"}
EXCLUDE_DIRS = {"bin", "obj", "Migrations", ...}
```

## Re-indicizzazione consigliata

Ri-esegui l'indicizzazione quando:
- Aggiungi nuove entità o servizi importanti
- Cambi architettura significativamente
- Una volta a settimana durante sviluppo attivo

## File generati

| File/Cartella     | Descrizione                              |
|-------------------|------------------------------------------|
| `lancedb/`         | Database LanceDB (non committare su git) |
| `requirements.txt`| Dipendenze Python                        |
| `index_codebase.py`| Script di indicizzazione               |
| `ask.py`          | Script di query RAG                      |
| `setup.ps1`       | Script di setup automatico               |
