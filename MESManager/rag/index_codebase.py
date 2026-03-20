"""
MESManager RAG - Indexer
Indicizza tutto il codebase (.cs, .razor, .md) in LanceDB con embeddings nomic-embed-text.
Uso: python index_codebase.py [--reset]
"""

import os
import sys
import hashlib
from pathlib import Path

import lancedb
import numpy as np
import pyarrow as pa
import ollama
from rich.console import Console
from rich.progress import Progress, SpinnerColumn, BarColumn, TextColumn, TimeElapsedColumn

console = Console()

# ──────────────────────────────────────────
# CONFIGURAZIONE
# ──────────────────────────────────────────

REPO_ROOT = Path(__file__).parent.parent  # c:\Dev\MESManager
DB_PATH   = Path(__file__).parent / "lancedb"
EMBED_MODEL = "nomic-embed-text"
TABLE_NAME = "mesmanager"
EMBED_DIM   = 768  # dimensione vettore nomic-embed-text

# File da indicizzare
INCLUDE_EXTENSIONS = {".cs", ".razor", ".md", ".json", ".csproj"}

# Cartelle da escludere completamente
EXCLUDE_DIRS = {
    "bin", "obj", "node_modules", ".git", "publish",
    "chroma_db", "rag", "backups", "wwwroot",
    "Migrations",  # troppe righe generate automaticamente
}

# File singoli da escludere
EXCLUDE_FILES = {
    "appsettings.Secrets.json",
    "appsettings.Secrets.encrypted",
    "appsettings.Database.Development.json",
}

# Dimensione chunk (caratteri) e overlap
CHUNK_SIZE    = 1500
CHUNK_OVERLAP = 200

# ──────────────────────────────────────────
# UTILITÀ
# ──────────────────────────────────────────

def file_id(path: Path) -> str:
    """ID stabile basato sul path relativo al REPO_ROOT."""
    rel = path.relative_to(REPO_ROOT).as_posix()
    return hashlib.md5(rel.encode()).hexdigest()

def chunk_text(text: str, path_label: str) -> list[dict]:
    """Divide il testo in chunk con overlap, aggiungendo metadata."""
    chunks = []
    start = 0
    idx = 0
    while start < len(text):
        end = start + CHUNK_SIZE
        chunk = text[start:end]
        chunks.append({
            "text": chunk,
            "metadata": {
                "source": path_label,
                "chunk_index": idx,
                "total_chars": len(text),
            }
        })
        start += CHUNK_SIZE - CHUNK_OVERLAP
        idx += 1
    return chunks

def collect_files() -> list[Path]:
    """Raccoglie tutti i file da indicizzare."""
    files = []
    for ext in INCLUDE_EXTENSIONS:
        for p in REPO_ROOT.rglob(f"*{ext}"):
            # Escludi dir
            parts = set(p.parts)
            if parts & {str(REPO_ROOT / d) for d in EXCLUDE_DIRS}:
                continue
            # Controllo più robusto: nessun componente del path è in EXCLUDE_DIRS
            if any(part in EXCLUDE_DIRS for part in p.parts):
                continue
            # Escludi file specifici
            if p.name in EXCLUDE_FILES:
                continue
            files.append(p)
    return sorted(files)

def get_embedding(text: str) -> list[float]:
    """Genera embedding con nomic-embed-text via Ollama."""
    response = ollama.embed(model=EMBED_MODEL, input=text)
    return response["embeddings"][0]

def get_embeddings_batch(texts: list[str]) -> list[list[float]]:
    """Genera embeddings in batch (più veloce per file grandi)."""
    response = ollama.embed(model=EMBED_MODEL, input=texts)
    return response["embeddings"]

# ──────────────────────────────────────────
# MAIN
# ──────────────────────────────────────────

# Schema LanceDB
def make_schema() -> pa.Schema:
    return pa.schema([
        pa.field("id",          pa.string()),
        pa.field("vector",      pa.list_(pa.float32(), EMBED_DIM)),
        pa.field("text",        pa.string()),
        pa.field("source",      pa.string()),
        pa.field("chunk_index", pa.int32()),
    ])

def main():
    reset = "--reset" in sys.argv

    console.rule("[bold cyan]MESManager RAG - Indexer[/]")

    # Verifica modello embedding
    try:
        console.print(f"[dim]Verifica modello embedding '[cyan]{EMBED_MODEL}[/]'...[/]")
        get_embedding("test")
        console.print(f"[green]✓ Modello {EMBED_MODEL} disponibile[/]")
    except Exception as e:
        console.print(f"[bold red]✗ Modello {EMBED_MODEL} non disponibile.[/]")
        console.print(f"[yellow]Esegui prima: [bold]ollama pull {EMBED_MODEL}[/][/]")
        sys.exit(1)

    # Connetti a LanceDB
    db = lancedb.connect(str(DB_PATH))

    if reset and TABLE_NAME in db.table_names():
        console.print("[yellow]Resetting table...[/]")
        db.drop_table(TABLE_NAME)

    if TABLE_NAME in db.table_names():
        table = db.open_table(TABLE_NAME)
        existing_ids = set(table.to_pandas()["id"].tolist())
        console.print(f"[dim]Chunk già presenti: {len(existing_ids)}[/]")
    else:
        table = db.create_table(TABLE_NAME, schema=make_schema())
        existing_ids = set()

    # Raccolta file
    files = collect_files()
    console.print(f"[cyan]File trovati:[/] {len(files)}")

    total_chunks  = 0
    skipped_files = 0
    indexed_files = 0
    errors        = 0

    with Progress(
        SpinnerColumn(),
        TextColumn("[progress.description]{task.description}"),
        BarColumn(),
        TextColumn("{task.completed}/{task.total}"),
        TimeElapsedColumn(),
        console=console,
    ) as progress:
        task = progress.add_task("Indicizzazione...", total=len(files))

        for fp in files:
            rel_label = fp.relative_to(REPO_ROOT).as_posix()
            progress.update(task, description=f"[dim]{rel_label[:60]}[/]")

            try:
                text = fp.read_text(encoding="utf-8", errors="ignore")
                if not text.strip():
                    progress.advance(task)
                    skipped_files += 1
                    continue

                chunks = chunk_text(text, rel_label)

                # Filtra chunk nuovi
                new_chunks = [
                    c for c in chunks
                    if f"{file_id(fp)}__{c['metadata']['chunk_index']}" not in existing_ids
                ]

                if not new_chunks:
                    indexed_files += 1
                    progress.advance(task)
                    continue

                # Batch embedding per file intero
                texts_to_embed = [c["text"] for c in new_chunks]
                embeddings = get_embeddings_batch(texts_to_embed)

                rows = []
                for chunk, emb in zip(new_chunks, embeddings):
                    chunk_id = f"{file_id(fp)}__{chunk['metadata']['chunk_index']}"
                    rows.append({
                        "id":          chunk_id,
                        "vector":      [float(x) for x in emb],
                        "text":        chunk["text"],
                        "source":      rel_label,
                        "chunk_index": chunk["metadata"]["chunk_index"],
                    })
                    existing_ids.add(chunk_id)

                table.add(rows)
                total_chunks += len(rows)
                indexed_files += 1

            except Exception as e:
                console.print(f"[red]Errore {rel_label}: {e}[/]")
                errors += 1

            progress.advance(task)

    console.rule()
    console.print(f"[bold green]✓ Completato![/]")
    console.print(f"  File:         {indexed_files} indicizzati, {skipped_files} saltati, {errors} errori")
    console.print(f"  Chunk aggiunti: {total_chunks}")
    console.print(f"  Totale in DB: {table.count_rows()} chunk")
    console.print(f"\n[dim]Ora usa:[/] [bold cyan]python ask.py \"la tua domanda\"[/]")

if __name__ == "__main__":
    main()
