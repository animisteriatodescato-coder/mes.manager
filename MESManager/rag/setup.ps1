# Setup MESManager RAG (esecuzione una tantum)
# Uso: .\setup.ps1

$ErrorActionPreference = "Stop"
$ragDir = $PSScriptRoot

Write-Host "=== MESManager RAG Setup (LanceDB + Ollama) ===" -ForegroundColor Cyan

# 1. Installa dipendenze Python
Write-Host "`n[1/3] Installazione dipendenze Python..." -ForegroundColor Yellow
pip install -r "$ragDir\requirements.txt" --quiet
if ($LASTEXITCODE -ne 0) { Write-Error "Fallita installazione pip"; exit 1 }
Write-Host "    Dipendenze OK" -ForegroundColor Green

# 2. Pull modello embedding
Write-Host "`n[2/3] Download modello embedding 'nomic-embed-text'..." -ForegroundColor Yellow
ollama pull nomic-embed-text
if ($LASTEXITCODE -ne 0) { Write-Error "Fallito pull nomic-embed-text"; exit 1 }
Write-Host "    Modello embedding OK" -ForegroundColor Green

# 3. (Opzionale) suggerisci modello chat migliore
Write-Host "`n[3/3] Verifica modello chat..." -ForegroundColor Yellow
$models = ollama list 2>&1
if ($models -notmatch "llama3|mistral|codellama|gemma|deepseek") {
    Write-Host @"

    Hai solo 'llava:latest' come modello chat.
    Per risposte migliori sul codice, considera di scaricare:

      ollama pull llama3.2          # consigliato, ~2GB, veloce
      ollama pull mistral           # ottimo per codice, ~4GB
      ollama pull deepseek-coder    # specializzato C#/.NET, ~4GB

    Poi aggiorna CHAT_MODEL in rag\ask.py.

"@ -ForegroundColor Yellow
} else {
    Write-Host "    Modello chat trovato: OK" -ForegroundColor Green
}

Write-Host "`n=== Setup completato! ===" -ForegroundColor Green
Write-Host @"

Prossimi passi:
  1. Indicizza il codebase (prima volta o dopo grandi modifiche):
       cd $ragDir
       python index_codebase.py

  2. Interroga il gestionale:
       python ask.py "come funziona lo scheduling delle commesse?"
       python ask.py   (modalita' interattiva con storia)

  3. Re-indicizza completamente:
       python index_codebase.py --reset

"@ -ForegroundColor Cyan
