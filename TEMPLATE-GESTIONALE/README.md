# 🏗️ Template Gestionale .NET

> Template AI-first per la creazione di gestionali .NET 8 + Blazor Server + Clean Architecture.
> Basato sull'esperienza operativa di MESManager (in produzione dal 2024).

---

## 📁 File del Template

| File | Scopo |
|------|-------|
| **[BIBBIA-AI-TEMPLATE.md](BIBBIA-AI-TEMPLATE.md)** | ⭐ Regole AI da personalizzare |
| **[COME-USARE-QUESTO-TEMPLATE.md](COME-USARE-QUESTO-TEMPLATE.md)** | Guida passo-passo nuovo progetto |
| **[STRUTTURA-PROGETTO.md](STRUTTURA-PROGETTO.md)** | Struttura cartelle e naming conventions |
| **[docs/](docs/)** | Documentazione template (da copiare e personalizzare) |

---

## ⚡ Quick Start

```powershell
# 1. Crea il progetto
New-Item -ItemType Directory -Path "C:\Dev\[NomeProgetto]" -Force

# 2. Copia documentazione template
Copy-Item -Recurse "C:\Dev\TEMPLATE-GESTIONALE\docs" "C:\Dev\[NomeProgetto]\docs"
Copy-Item "C:\Dev\TEMPLATE-GESTIONALE\BIBBIA-AI-TEMPLATE.md" `
          "C:\Dev\[NomeProgetto]\docs\BIBBIA-AI-[NomeProgetto].md"

# 3. Personalizza placeholder → vedi COME-USARE-QUESTO-TEMPLATE.md

# 4. Inizializza .NET soluzione → vedi STRUTTURA-PROGETTO.md
```

---

## 🧠 Filosofia

- **AI-first**: documenta le regole per l'AI così come per gli umani
- **Zero duplicazione**: un solo posto per ogni pezzo di logica
- **Clean Architecture**: layer ben separati, testabili, manutenibili
- **Storicità**: ogni decisione, bug e soluzione viene documentata
- **Deploy sicuro**: mai sovrascrivere i secrets, mai deploiare in autonomia

---

*Versione: 1.0 | Aprile 2026 | Basato su MESManager BIBBIA v4.5*
