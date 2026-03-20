"""
MESManager RAG - Query Engine
Cerca nel codebase indicizzato e genera risposte contestualizzate con Ollama.
Uso: python ask.py "domanda"
     python ask.py  (modalità interattiva)
"""

import sys
from pathlib import Path

import lancedb
import numpy as np
import ollama
from rich.console import Console
from rich.markdown import Markdown
from rich.panel import Panel
from rich.prompt import Prompt

console = Console()

# ──────────────────────────────────────────
# CONFIGURAZIONE
# ──────────────────────────────────────────

DB_PATH         = Path(__file__).parent / "lancedb"
EMBED_MODEL     = "nomic-embed-text"
CHAT_MODEL      = "llama3.2"       # modello testo; cambia con mistral, deepseek-coder, ecc.
COLLECTION_NAME = "mesmanager"

N_RESULTS       = 6    # chunk da recuperare per query
MAX_CTX_CHARS   = 5000 # massimo contesto inviato al modello (evita overflow)

SYSTEM_PROMPT = """\
Sei un assistente esperto del gestionale MESManager, un sistema MES (Manufacturing Execution System) \
sviluppato in ASP.NET 8 + Blazor Server, con:
- Domain Layer: entità C# (Commessa, Macchina, Articolo, Ricetta, Anime, ecc.)
- Application Layer: servizi, interfacce, DTOs
- Infrastructure Layer: EF Core + SQL Server, PLC sync (Sharp7), replica Mago ERP
- Web Layer: Blazor Server + MudBlazor + Syncfusion Gantt
- Worker: background service per sync e pianificazione

Rispondi SEMPRE in italiano. Usa il contesto fornito dal codebase per dare risposte precise. \
Se mostri codice C# o Razor, usa blocchi markdown con syntax highlighting. \
Se non trovi la risposta nel contesto, dillo chiaramente invece di inventare.\
"""

# ──────────────────────────────────────────
# CORE
# ──────────────────────────────────────────

def get_embedding(text: str) -> list[float]:
    response = ollama.embed(model=EMBED_MODEL, input=text)
    return response["embeddings"][0]

def retrieve_context(table, question: str) -> tuple[str, list[str]]:
    """Recupera i chunk più rilevanti con vector search LanceDB."""
    embedding = get_embedding(question)
    results = table.search(embedding).metric("cosine").limit(N_RESULTS).to_pandas()

    # Filtra per distanza (LanceDB cosine: più basso = più simile)
    filtered = results[results["_distance"] < 0.85] if len(results) > 0 else results
    if len(filtered) == 0:
        filtered = results.head(3)

    sources = []
    context_parts = []
    total_chars = 0

    for _, row in filtered.iterrows():
        src       = row["source"]
        chunk_idx = int(row["chunk_index"])
        dist      = float(row["_distance"])
        doc       = row["text"]
        header = f"### [{src}] (chunk {chunk_idx}, dist={dist:.3f})\n"

        if total_chars + len(doc) > MAX_CTX_CHARS:
            break

        context_parts.append(header + doc)
        total_chars += len(doc)
        if src not in sources:
            sources.append(src)

    context = "\n\n---\n\n".join(context_parts)
    return context, sources

def ask(table, question: str, chat_history: list[dict]) -> str:
    """Esegue una query RAG con streaming e restituisce la risposta completa."""
    console.print("[dim]Ricerca nel codebase...[/]")
    context, sources = retrieve_context(table, question)

    console.print(f"[dim]Fonti: {', '.join(sources[:4])}{'...' if len(sources) > 4 else ''}[/]")
    console.print("[dim]Generazione risposta (streaming)...[/]\n")

    messages = [{"role": "system", "content": SYSTEM_PROMPT}]
    messages.extend(chat_history[-8:])
    messages.append({
        "role": "user",
        "content": f"CONTESTO DAL CODEBASE MESMANAGER:\n{context}\n\n---\nDOMANDA: {question}"
    })

    # Streaming: evita timeout su risposte lunghe
    full_response = []
    for chunk in ollama.chat(model=CHAT_MODEL, messages=messages, stream=True):
        token = chunk["message"]["content"]
        print(token, end="", flush=True)
        full_response.append(token)

    print()  # newline finale
    return "".join(full_response)

# ──────────────────────────────────────────
# MAIN
# ──────────────────────────────────────────

def main():
    console.rule("[bold cyan]MESManager RAG Assistant[/]")

    # Connetti a ChromaDB
    if not DB_PATH.exists():
        console.print("[bold red]Database non trovato.[/]")
        console.print("[yellow]Esegui prima:[/] [bold]python index_codebase.py[/]")
        sys.exit(1)

    db = lancedb.connect(str(DB_PATH))

    try:
        table = db.open_table("mesmanager")
    except Exception:
        console.print("[bold red]Tabella non trovata.[/]")
        console.print("[yellow]Esegui prima:[/] [bold]python index_codebase.py[/]")
        sys.exit(1)

    count = table.count_rows()
    console.print(f"[green]✓ Database caricato:[/] {count} chunk indicizzati")
    console.print(f"[dim]Modello embed: {EMBED_MODEL} | Modello chat: {CHAT_MODEL}[/]")
    console.print("[dim]Scrivi 'exit' o premi Ctrl+C per uscire.[/]\n")

    # Risposta singola da argomento CLI
    if len(sys.argv) > 1:
        question = " ".join(sys.argv[1:])
        console.print(Panel(f"[bold]{question}[/]", title="Domanda", border_style="cyan"))
        try:
            answer = ask(table, question, [])
            # La risposta è già stata stampata in streaming - mostra solo il pannello fonti
        except Exception as e:
            console.print(f"[bold red]Errore: {e}[/]")
        return

    # Modalità interattiva con memoria conversazione
    chat_history: list[dict] = []

    while True:
        try:
            question = Prompt.ask("\n[bold cyan]Domanda[/]")
        except (KeyboardInterrupt, EOFError):
            console.print("\n[dim]Arrivederci![/]")
            break

        if question.lower() in ("exit", "quit", "esci", ""):
            console.print("[dim]Arrivederci![/]")
            break

        # Comandi speciali
        if question.lower() == "clear":
            chat_history.clear()
            console.print("[yellow]Storia conversazione cancellata.[/]")
            continue

        if question.lower() == "history":
            for i, msg in enumerate(chat_history):
                console.print(f"[dim]{i}: [{msg['role']}] {msg['content'][:80]}...[/]")
            continue

        try:
            answer = ask(table, question, chat_history)

            # Aggiorna history
            chat_history.append({"role": "user",      "content": question})
            chat_history.append({"role": "assistant", "content": answer})

        except KeyboardInterrupt:
            console.print("\n[yellow]Interrotto.[/]")
        except Exception as e:
            console.print(f"[bold red]Errore: {e}[/]")

if __name__ == "__main__":
    main()
