# 🏗️ Struttura Progetto Standard

> Struttura di riferimento per gestionali .NET 8 + Blazor Server + Clean Architecture.
> Adattare in base alle esigenze specifiche del progetto.

---

## 📁 Struttura Completa

```
[NomeProgetto]/
│
│   ── SOLUTION ────────────────────────────────────────────────────
├── [NomeProgetto].sln
├── .gitignore
├── README.md
│
│   ── DOCUMENTAZIONE ────────────────────────────────────────────────
├── docs/
│   ├── README.md                      ← Indice
│   ├── BIBBIA-AI-[NomeProgetto].md    ← ⭐ Regole AI (fonte di verità)
│   ├── 01-DEPLOY.md
│   ├── 02-SVILUPPO.md
│   ├── 03-CONFIGURAZIONE.md
│   ├── 04-ARCHITETTURA.md
│   ├── 05-MODULO-CORE.md
│   ├── 06-INSTALLAZIONE.md
│   ├── 07-INTEGRAZIONI.md
│   ├── 08-MODULI-EXTRA.md
│   ├── 09-CHANGELOG.md
│   ├── 10-BUSINESS.md
│   ├── 11-TESTING.md
│   ├── 12-QA-UI.md
│   └── storico/
│       ├── DEPLOY-LESSONS-LEARNED.md
│       └── FIX-[DESCRIZIONE]-[DATA].md   ← aggiungere ad ogni fix importante
│
│   ── COPILOT ──────────────────────────────────────────────────────
├── .github/
│   └── copilot-instructions.md         ← Puntatore alla BIBBIA
│
│   ── DOMAIN LAYER ─────────────────────────────────────────────────
├── [NomeProgetto].Domain/
│   ├── [NomeProgetto].Domain.csproj
│   ├── Entities/
│   │   ├── BaseEntity.cs              ← Id, CreatedAt, UpdatedAt, IsDeleted
│   │   ├── [Entità1].cs
│   │   └── [Entità2].cs
│   ├── Interfaces/
│   │   ├── IGenericRepository.cs      ← CRUD base
│   │   ├── IUnitOfWork.cs             ← se si usa UoW pattern
│   │   ├── I[Entità1]Repository.cs
│   │   └── I[Entità2]Repository.cs
│   ├── ValueObjects/
│   │   └── [VObject].cs               ← se necessari
│   ├── Enums/
│   │   └── [Enum].cs
│   └── Exceptions/
│       ├── DomainException.cs
│       └── [Entità]NotFoundException.cs
│
│   ── APPLICATION LAYER ────────────────────────────────────────────
├── [NomeProgetto].Application/
│   ├── [NomeProgetto].Application.csproj
│   ├── DTOs/
│   │   ├── [Entità1]/
│   │   │   ├── [Entità1]Dto.cs        ← lettura
│   │   │   ├── Crea[Entità1]Dto.cs   ← creazione
│   │   │   └── Aggiorna[Entità1]Dto.cs ← aggiornamento
│   │   └── Common/
│   │       ├── PagedResultDto.cs      ← paginazione generica
│   │       └── ApiResponseDto.cs      ← risposta API standardizzata
│   ├── Interfaces/
│   │   ├── I[Entità1]Service.cs
│   │   └── I[Entità2]Service.cs
│   ├── Services/
│   │   ├── [Entità1]Service.cs
│   │   └── [Entità2]Service.cs
│   ├── Mappings/
│   │   └── AutoMapperProfile.cs       ← se si usa AutoMapper
│   ├── Validators/
│   │   ├── Crea[Entità1]Validator.cs  ← FluentValidation
│   │   └── Aggiorna[Entità1]Validator.cs
│   └── DependencyInjection.cs         ← extension method per DI
│
│   ── INFRASTRUCTURE LAYER ─────────────────────────────────────────
├── [NomeProgetto].Infrastructure/
│   ├── [NomeProgetto].Infrastructure.csproj
│   ├── Data/
│   │   ├── [NomeProgetto]DbContext.cs
│   │   ├── Migrations/
│   │   │   └── [timestamp]_[NomeMigration].cs
│   │   └── Configurations/            ← EntityTypeConfiguration fluent API
│   │       ├── [Entità1]Configuration.cs
│   │       └── [Entità2]Configuration.cs
│   ├── Repositories/
│   │   ├── GenericRepository.cs
│   │   ├── [Entità1]Repository.cs
│   │   └── [Entità2]Repository.cs
│   ├── Services/
│   │   ├── EmailService.cs            ← SMTP / SendGrid
│   │   ├── [ErpNome]Service.cs        ← integrazione ERP se presente
│   │   └── [PlcNome]Service.cs        ← integrazione PLC se presente
│   └── DependencyInjection.cs
│
│   ── WEB LAYER ─────────────────────────────────────────────────
├── [NomeProgetto].Web/
│   ├── [NomeProgetto].Web.csproj
│   ├── Program.cs                     ← DI, pipeline, middleware
│   ├── AppVersion.cs                  ← ⭐ versione sempre aggiornata
│   ├── Pages/
│   │   ├── _Host.cshtml               ← entry point Blazor Server
│   │   ├── Error.cshtml
│   │   ├── Index.razor                ← dashboard home
│   │   └── [Modulo]/
│   │       ├── [Modulo]Lista.razor    ← lista principale
│   │       ├── [Modulo]Dettaglio.razor ← dettaglio/edit
│   │       └── [Modulo]Dialog.razor   ← dialog creazione/modifica
│   ├── Components/
│   │   ├── Shared/
│   │   │   ├── ConfirmDialog.razor    ← dialog conferma generico
│   │   │   ├── LoadingOverlay.razor   ← spinner globale
│   │   │   └── StatusBadge.razor     ← badge stato riusabile
│   │   └── [Modulo]/
│   │       └── [NomeComponente].razor
│   ├── Layout/
│   │   ├── MainLayout.razor           ← layout principale
│   │   ├── MainLayout.razor.css
│   │   └── NavMenu.razor              ← navigazione sidebar
│   ├── Constants/
│   │   ├── DesignTokens.cs            ← ⭐ colori/token UI (fonte di verità)
│   │   └── AppRoutes.cs               ← costanti route
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── wwwroot/
│   │   ├── app.css                    ← ⭐ CSS GLOBALI qui (non inline)
│   │   ├── favicon.ico
│   │   └── js/
│   │       └── app.js
│   └── appsettings.json
│
│   ── WORKER (opzionale) ───────────────────────────────────────────
├── [NomeProgetto].Worker/              ← se servono background jobs
│   ├── [NomeProgetto].Worker.csproj
│   ├── Program.cs
│   ├── Workers/
│   │   └── [NomeWorker]Worker.cs
│   └── appsettings.json
│
│   ── TESTS ─────────────────────────────────────────────────────
├── tests/
│   ├── [NomeProgetto].Tests/
│   │   ├── [NomeProgetto].Tests.csproj
│   │   ├── Unit/
│   │   │   ├── Domain/
│   │   │   └── Application/
│   │   └── Integration/
│   │       └── Repositories/
│   └── [NomeProgetto].E2E/
│       ├── [NomeProgetto].E2E.csproj
│       ├── Tests/
│       │   └── [Feature]Tests.cs
│       ├── Pages/                     ← Page Object Model
│       │   └── [Pagina]Page.cs
│       └── Fixtures/
│           └── TestFixture.cs
│
│   ── SCRIPTS ───────────────────────────────────────────────────
├── scripts/
│   ├── migrations/                    ← script SQL per migazioni prod
│   └── deployment/                    ← script deploy
│
│   ── BACKUPS ───────────────────────────────────────────────────
└── backups/
    └── README.md                      ← istruzioni backup
```

---

## 🏷️ Naming Conventions

| Tipo | Convenzione | Esempio |
|------|-------------|---------|
| Classi | PascalCase | `OrdineService` |
| Interfacce | IPascalCase | `IOrdineService` |
| Metodi | PascalCase | `GetListaAsync` |
| Variabili private | `_camelCase` | `_ordineService` |
| Costanti | UPPER_CASE | `MAX_PAGINA_SIZE` |
| File .razor pagina | `[Entità][Azione].razor` | `OrdiniLista.razor` |
| File .razor dialog | `[Entità]Dialog.razor` | `OrdineDialog.razor` |
| Tabelle DB | `[NomePluraleEntità]` | `Ordini`, `Clienti` |
| Colonne DB | `PascalCase` | `DataOrdine`, `ClienteId` |

---

## 📦 NuGet Packages consigliati

```xml
<!-- Infrastructure -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.*" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
<PackageReference Include="AutoMapper" Version="13.*" />

<!-- Web (Blazor) -->
<PackageReference Include="MudBlazor" Version="8.*" />

<!-- Web (Grid) -->
<PackageReference Include="AG-Grid-Blazor" Version="..." />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="Microsoft.Playwright" Version="1.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
```

---

*Versione: 1.0 | Aprile 2026*
