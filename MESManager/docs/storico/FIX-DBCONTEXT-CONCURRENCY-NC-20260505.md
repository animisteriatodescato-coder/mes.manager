# FIX: DbContext Concurrency — Navigazione NC da Commesse Aperte

> **Data**: Maggio 2026  
> **Versione**: v1.65.74  
> **Gravità**: 🔴 CRITICA — Crash 100% riproducibile su navigazione

---

## Sintomo

Click colonna NC in griglia Commesse Aperte → redirect a `/cataloghi/non-conformita?articolo=CODICE` → pagina crash:

```
InvalidOperationException: A second operation was started on this context instance 
before a previous operation completed. This is usually caused by different threads 
concurrently using the same instance of DbContext.
   at Microsoft.EntityFrameworkCore.Infrastructure.Internal.ConcurrencyDetector.EnterCriticalSection()
   at ...NonConformitaService.GetAllAsync()
   at ...CatalogoNonConformita.LoadData()
```

**Quando si manifesta**: SOLO sulla navigazione da un'altra pagina, NON su primo caricamento diretto dell'URL.

---

## Analisi Root Cause

### Il DbContext è UNO SOLO per Identity + Business

```
MesManagerDbContext
  ↳ : IdentityDbContext<ApplicationUser>   ← Identity tables (AspNetUsers, AspNetRoles...)
  ↳ DbSet<Commessa>                         ← Business entity
  ↳ DbSet<NonConformita>                    ← Business entity
  ↳ DbSet<Macchina>                         ← Business entity
  ↳ ...
```

In Blazor Server, i servizi `Scoped` vivono **per circuito SignalR** (non per request HTTP come in ASP.NET classico). Il `MesManagerDbContext` è Scoped → **un'unica istanza condivisa tra tutti i componenti** della stessa sessione.

### La Race Condition

Quando l'utente naviga verso `CatalogoNonConformita`, Blazor ricrea il componente e il layout lo riprende — entrambi inizializzano **in parallelo**:

```
Navigazione utente
  ├── MainLayout.OnInitializedAsync()
  │     └── UserManager.FindByIdAsync(userId)     ← usa MesManagerDbContext (Identity tables)
  │
  └── CatalogoNonConformita.OnInitializedAsync()
        └── NonConformitaService.GetAllAsync()    ← usa STESSO MesManagerDbContext
```

Due `await` su due query diverse **sullo stesso DbContext** instance → EF Core rileva la concorrenza → THROW.

### Perché non si vede su caricamento diretto

Su accesso diretto all'URL (`/cataloghi/non-conformita`), il `MainLayout` si inizializza **prima** della pagina figlia perché il circuito SignalR è fresco. Sulla navigazione intra-app, le timing cambiano e il layout si re-inizializza **contestualmente** alla pagina di destinazione.

---

## Soluzione Implementata (v1.65.74)

### Fix 1 — NonConformitaService: IDbContextFactory (thread-safe)

**File**: `MESManager.Infrastructure/Services/NonConformitaService.cs`

**Prima** (❌ DbContext scoped condiviso):
```csharp
public class NonConformitaService : INonConformitaService
{
    private readonly MesManagerDbContext _db;
    public NonConformitaService(MesManagerDbContext db) => _db = db;

    public async Task<List<NonConformita>> GetAllAsync()
        => await _db.NonConformita.OrderBy(n => n.CodiceArticolo).ToListAsync();
}
```

**Dopo** (✅ ogni operazione ha context isolato):
```csharp
public class NonConformitaService : INonConformitaService
{
    private readonly IDbContextFactory<MesManagerDbContext> _dbFactory;
    public NonConformitaService(IDbContextFactory<MesManagerDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<List<NonConformita>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita.OrderBy(n => n.CodiceArticolo).ToListAsync();
    }

    public async Task<List<NonConformita>> GetAperteAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita.Where(n => n.Stato == "Aperta").ToListAsync();
    }

    public async Task<NonConformita?> GetByIdAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NonConformita.FindAsync(id);
    }

    // Stesso pattern per CreateAsync, UpdateAsync, DeleteAsync, ChiudiAsync...
}
```

**Perché `IDbContextFactory`**: il factory crea un context nuovo e indipendente per ogni chiamata. Ogni `await using var db = ...` garantisce isolation totale. È il pattern raccomandato da Microsoft per servizi chiamati da contesti concorrenti in Blazor Server.

---

### Fix 2 — MainLayout: IServiceScopeFactory per Identity

**File**: `MESManager.Web/Components/Layout/MainLayout.razor.cs`

**Prima** (❌ UserManager iniettato direttamente — usa il DbContext scoped condiviso):
```csharp
[Inject] UserManager<ApplicationUser> UserManager { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    var userId = ...;
    var appUser = await UserManager.FindByIdAsync(userId);  // ← usa DbContext scoped
    var roles = await UserManager.GetRolesAsync(appUser);
}
```

**Dopo** (✅ scope isolato per ogni chiamata Identity):
```csharp
[Inject] IServiceScopeFactory ScopeFactory { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    var userId = ...;
    await using var userScope = ScopeFactory.CreateAsyncScope();
    var userManager = userScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var appUser = await userManager.FindByIdAsync(userId);   // ← context proprio dello scope
    var roles = await userManager.GetRolesAsync(appUser);
}
```

Il `CreateAsyncScope()` crea un nuovo scope DI con una propria istanza di `MesManagerDbContext`, completamente separata da quella della pagina figlia.

---

### Fix 3 — CatalogoNonConformita: SemaphoreSlim

**File**: `MESManager.Web/Components/Pages/Cataloghi/CatalogoNonConformita.razor`

Guard contro re-entry su navigazione rapida o doppio render:
```csharp
private readonly SemaphoreSlim _loadLock = new(1, 1);

private async Task LoadData()
{
    await _loadLock.WaitAsync();
    try
    {
        _loading = true;
        StateHasChanged();
        var data = await NcService.GetAllAsync();
        // ... filtro e aggiornamento
    }
    finally
    {
        _loading = false;
        _loadLock.Release();
    }
}
```

---

### Fix 4 — Deep-link da Grid AG Grid

**File**: `MESManager.Web/Components/Pages/Cataloghi/CatalogoNonConformita.razor`  
**File**: `MESManager.Web/wwwroot/lib/ag-grid/commesse-aperte-grid.js`

Il click da Commesse Aperte navigava a `?articolo=CODICE` ma la pagina non leggeva il query param.

**Soluzione**:
```csharp
[SupplyParameterFromQuery(Name = "articolo")]
public string? ArticoloQuery { get; set; }

private string? _lastArticoloQuery;

protected override async Task OnInitializedAsync()
{
    ApplyQueryFilter();
    await LoadData();
}

protected override async Task OnParametersSetAsync()
{
    if (ApplyQueryFilter()) await LoadData();
}

private bool ApplyQueryFilter()
{
    var trimmed = ArticoloQuery?.Trim() ?? string.Empty;
    if (trimmed == _lastArticoloQuery) return false;
    _lastArticoloQuery = trimmed;
    if (!string.IsNullOrEmpty(trimmed)) _searchText = trimmed;
    return true;
}
```

---

## Commit

```
git commit: db171fa — fix: elimina race DbContext su navigazione NC
```

**File committati**:
- `MESManager.Infrastructure/Services/NonConformitaService.cs`
- `MESManager.Web/Components/Layout/MainLayout.razor.cs`
- `MESManager.Web/Components/Pages/Cataloghi/CatalogoNonConformita.razor`
- `MESManager.Web/Constants/AppVersion.cs` (→ 1.65.74)
- `MESManager/docs/09-CHANGELOG.md`

---

## Regola Architetturale (da rispettare sempre)

> **Ogni servizio Infrastructure iniettato in `MainLayout`, in un Hub SignalR, o in qualsiasi componente che si inizializza concorrentemente alle pagine figlie, DEVE usare `IDbContextFactory<MesManagerDbContext>` invece del DbContext Scoped iniettato direttamente.**

Documentazione pattern: [04-ARCHITETTURA.md — Regola Critica: DbContext Concurrency](../04-ARCHITETTURA.md)
