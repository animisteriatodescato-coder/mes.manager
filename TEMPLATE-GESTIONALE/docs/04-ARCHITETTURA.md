# 🏛️ 04 — Architettura

> Pattern centralizzati, layer, DI e struttura progetto [NOME_PROGETTO].

---

## 📐 Clean Architecture — Layer

```
┌─────────────────────────────────────────────────────┐
│                   Web / Presentation                 │
│  Blazor Pages/Components | Controllers | API        │
├─────────────────────────────────────────────────────┤
│                   Application                        │
│  Use Cases | Services | DTOs | Interfaces            │
├─────────────────────────────────────────────────────┤
│                   Infrastructure                     │
│  EF Core | Repositories | External APIs | Email      │
├─────────────────────────────────────────────────────┤
│                   Domain                             │
│  Entities | Value Objects | Domain Interfaces        │
└─────────────────────────────────────────────────────┘
```

**Regola dipendenze**: Domain ← Application ← Infrastructure ← Web

---

## 🏗️ Struttura Progetto

```
[NomeProgetto]/
├── [NomeProgetto].Domain/
│   ├── Entities/            ← entità di business
│   ├── Interfaces/          ← interfacce repository
│   ├── ValueObjects/        ← value objects
│   └── Exceptions/          ← eccezioni di dominio
│
├── [NomeProgetto].Application/
│   ├── DTOs/                ← Data Transfer Objects
│   ├── Interfaces/          ← interfacce servizi
│   ├── Services/            ← implementazioni use case
│   ├── Mappings/            ← AutoMapper profiles
│   └── Validators/          ← FluentValidation validators
│
├── [NomeProgetto].Infrastructure/
│   ├── Data/
│   │   ├── [NomeProgetto]DbContext.cs
│   │   ├── Migrations/
│   │   └── Configurations/  ← EntityTypeConfiguration
│   ├── Repositories/        ← implementazioni repository
│   └── Services/            ← servizi esterni (email, API, PLC)
│
├── [NomeProgetto].Web/
│   ├── Pages/               ← pagine Blazor (.razor)
│   ├── Components/          ← componenti riusabili
│   ├── Layout/              ← MainLayout, NavMenu
│   ├── Constants/           ← design tokens, costanti UI
│   ├── wwwroot/
│   │   ├── app.css          ← ⭐ CSS GLOBALI QUI (non inline)
│   │   └── js/
│   ├── Program.cs           ← DI registration, pipeline
│   └── AppVersion.cs        ← versione applicazione
│
├── tests/
│   ├── [NomeProgetto].Tests/    ← unit + integration
│   └── [NomeProgetto].E2E/     ← Playwright E2E
│
└── docs/                    ← documentazione
```

---

## 🔌 Pattern Centralizzati — USA QUESTI, NON DUPLICARE

> Prima di implementare: cerca con grep. Reimplementare = bug architetturale.

### Repository Pattern

```csharp
// Domain/Interfaces/IGenericRepository.cs
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Infrastructure/Repositories/GenericRepository.cs
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly [NomeProgetto]DbContext _context;
    // ... implementazione base
}
```

### Service Pattern

```csharp
// Application/Interfaces/I[Entità]Service.cs
public interface I[Entità]Service
{
    Task<IEnumerable<[Entità]Dto>> GetListaAsync();
    Task<[Entità]Dto?> GetByIdAsync(int id);
    Task<[Entità]Dto> CreaAsync(Crea[Entità]Dto dto);
    Task AggiornAsync(int id, Aggiorna[Entità]Dto dto);
    Task EliminaAsync(int id);
}
```

### Gestione Errori Globale

```csharp
// Web/Middleware/GlobalExceptionMiddleware.cs
// ✅ Un solo punto di gestione errori HTTP
```

### Notifiche UI (Snackbar/Toast)

```csharp
// MAI istanziare ISnackbar in ogni pagina separatamente
// USA il servizio centralizzato: INotificationService
// → una sola implementazione che wrappa MudSnackbar o equivalente
```

---

## 💉 Dependency Injection — Registrazioni

```csharp
// Program.cs — sezione organizzata per layer

// Domain (nessuna registrazione di solito)

// Application
builder.Services.AddScoped<I[Entità]Service, [Entità]Service>();

// Infrastructure
builder.Services.AddDbContext<[NomeProgetto]DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<I[Entità]Repository, [Entità]Repository>();

// Cross-cutting
builder.Services.AddSingleton<INotificationService, NotificationService>();
```

---

## 🎨 CSS e Tema

### Regola fondamentale CSS (Blazor Server)

```
app.css          ← CSS GLOBALI (tabelle, grids, layout, dark mode)
MainLayout.razor ← Solo variabili CSS calcolate da C# (:root { --var: @value })
*.razor          ← Solo <style> per stili ESCLUSIVI del componente
```

**Anti-pattern** (non usare):
```html
<!-- ❌ CSS inline in Blazor che verranno ignorati da SignalR re-render -->
<style> .my-class { color: @_colorCalcolato; } </style>
```

### Design Tokens

```csharp
// Constants/DesignTokens.cs — UNICA fonte di verità per colori/dimensioni
public static class DesignTokens
{
    public static string RowOdd(bool isDark) => isDark ? "#1e1e2e" : "#f5f5f9";
    public static string RowEven(bool isDark) => isDark ? "#252535" : "#ffffff";
    // ...
}
```

---

## 📊 Grids / Tabelle

> Definire qui quale grid library si usa e il pattern standard.

```
AG Grid Community:
  - ColDef centralizzate in [Entità]GridColumns.cs
  - cellClassRules per stati/colori → MainLayout inline style (non app.css)

MudBlazor MudTable:
  - Pagination, sorting, filtering standard
  - Nessuna logica business nel componente
```

---

## 🔒 Autenticazione / Autorizzazione

> Definire qui il sistema auth del progetto.

```
ASP.NET Core Identity:
  - Ruoli: [RUOLO_1], [RUOLO_2], [RUOLO_ADMIN]
  - Policy-based authorization

Azure AD / OIDC:
  - Tenant: [TENANT_ID]
  - ClientId: [CLIENT_ID] (in Secrets.json)
```

---

*Versione: 1.0 — Aggiornare con ogni nuovo pattern adottato*
