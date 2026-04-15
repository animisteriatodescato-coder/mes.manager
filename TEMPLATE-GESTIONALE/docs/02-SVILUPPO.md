# 🛠️ 02 — Workflow Sviluppo

> Regole operative per ogni modifica al codice di [NOME_PROGETTO].

---

## ⚙️ Sequenza Obbligatoria per Ogni Modifica

```
1. git commit PRIMA delle modifiche (snapshot clean state)
2. Implementa la modifica
3. Incrementa AppVersion.cs
4. dotnet build [NOME_PROGETTO].sln --nologo  →  0 errori OBBLIGATORI
5. git commit DOPO le modifiche
6. Aggiorna docs/09-CHANGELOG.md
7. Avvia server: dotnet run --project ... --environment Development
8. Comunica URL: http://localhost:[PORTA_DEV]/[pagina-modificata]
9. Attendi feedback utente
```

**MAI** saltare i passi 4, 7–9.
**MAI** dire "ho finito" senza build verde + server avviato.
**MAI** lasciare comandi all'utente — l'AI esegue tutto.

---

## 🚫 ZERO DUPLICAZIONE — 4 Domande Prima di Scrivere Codice

Prima di implementare qualsiasi cosa, rispondi a queste 4 domande:

1. **Esiste già questa funzionalità nel codice?**
   - Cerca con `grep -r "keyword" src/` o semantic search
   - Se sì → riusa o estendi, STOP

2. **Esiste un servizio/repository che fa qualcosa di simile?**
   - Se sì → aggiungi metodo a quello esistente, non creare uno nuovo

3. **Questo codice sarà usato in più posti?**
   - Se sì → classe shared in `Application/`, non nel componente

4. **È nel layer architetturale corretto?**
   - Logica business → `Domain/` o `Application/`
   - Accesso DB → `Infrastructure/`
   - UI → `Web/`

---

## 📦 Gestione Versione

### AppVersion.cs
```csharp
// Incrementa sempre: MAJOR.MINOR.PATCH.BUILD
public static class AppVersion
{
    public const string Version = "1.0.0";
    public const string BuildDate = "[DATA]";
}
```

### Convenzione versioni
- **PATCH** (+0.0.1): bug fix, micro-fix UI
- **MINOR** (+0.1.0): nuova feature, refactoring
- **MAJOR** (+1.0.0): breaking change, nuova sezione applicazione

---

## 🗃️ Git — Regole Commit

```powershell
# Commit PRE-modifica (snapshot)
git add -A; git commit -m "chore: snapshot pre-[descrizione-modifica]"

# Commit POST-modifica
git add -A; git commit -m "[tipo]: [descrizione] (v[X.Y.Z])"
```

**Tipi commit**: `feat` | `fix` | `refactor` | `docs` | `style` | `chore`

---

## 🏗️ Aggiunta Nuovo Modulo — Checklist

- [ ] Entità in `Domain/Entities/`
- [ ] Interfaccia repository in `Domain/Interfaces/`
- [ ] DTO in `Application/DTOs/`
- [ ] Interfaccia servizio in `Application/Interfaces/`
- [ ] Implementazione servizio in `Application/Services/`
- [ ] Repository in `Infrastructure/Repositories/`
- [ ] Migration EF Core (`dotnet ef migrations add NomeMigration`)
- [ ] Registrazione DI in `Program.cs`
- [ ] Componente/pagina in `Web/Pages/`
- [ ] Test in `tests/[NomeProgetto].Tests/`
- [ ] E2E test in `tests/[NomeProgetto].E2E/`
- [ ] Docs aggiornate (04-ARCHITETTURA.md + modulo pertinente)
- [ ] CHANGELOG aggiornato

---

## 🔁 EF Core Migrations

```powershell
# Aggiungi migration
cd [PATH_PROGETTO]
dotnet ef migrations add [NomeMigration] --project [NomeProgetto].Infrastructure --startup-project [NomeProgetto].Web

# Applica migration (DEV)
dotnet ef database update --project [NomeProgetto].Infrastructure --startup-project [NomeProgetto].Web

# Script SQL per PROD (non applicare mai direttamente in prod)
dotnet ef migrations script [MigrationPrecedente] [NuovaMigration] -o scripts/migrations/[NomeMigration].sql
```

> ⚠️ In PROD si applicano **solo script SQL**, mai `dotnet ef database update` diretto.

---

## 🧪 Test Locali

```powershell
# Unit tests
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].Tests/

# E2E tests (con server in esecuzione)
cd [PATH_PROGETTO]; dotnet test tests/[NomeProgetto].E2E/ -- playwright.env.E2E_USE_EXISTING_SERVER=1
```

---

*Versione: 1.0*
