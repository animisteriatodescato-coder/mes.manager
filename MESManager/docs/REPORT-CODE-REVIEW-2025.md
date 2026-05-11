# Report di Code Review — MESManager
> Analisi diagnostica prodotta da due motori AI indipendenti (Claude + Codex) e riconciliata.  
> **Nessun codice modificato.**  
> Data: Maggio 2026

---

## 0. Nota Metodologica — Doppia Analisi

Questo report integra due analisi indipendenti sullo stesso codebase:

- **Claude** (GitHub Copilot): ha privilegiato l'analisi architetturale, gli smell concettuali e i rischi di concorrenza/scalabilità.
- **Codex** (OpenAI): ha eseguito un audit più operativo — linee specifiche, route, configurazioni, dipendenze DI, file-by-file.

Le due analisi **convergono sugli stessi problemi strutturali**, il che aumenta la confidenza: non sono allucinazioni del modello, sono problemi reali e documentati.

### Punti di convergenza forte (alta confidenza)
- ✅ Doppia registrazione `MesManagerDbContext`
- ✅ Controller che bypassano il service layer (DbContext diretto)
- ✅ Config e secrets caricati in modo diverso tra Web / Worker / PlcSync
- ✅ File enormi non scomponibili (`PianificazioneEngineService`, `PianificazioneController`, CSS/JS globali)
- ✅ Codice morto accumulato (.bak, stub, servizi non registrati)

### Differenza di approccio
| Aspetto | Claude | Codex |
|---|---|---|
| Forza | Smell architetturali, rischi enterprise, concorrenza | Audit operativo, linee specifiche, piano concreto |
| Debolezza | Meno preciso file-by-file | Meno orientato a pattern concettuali |
| Uso ideale | Capire *perché* è un problema | Capire *dove* e *come* fixarlo |

---

## 1. Mappa Architetturale Sintetica

```
MESManager.Domain
  ├── Entities          (Anime, Commessa, Macchina, PLC, NonConformita, Preventivo…)
  ├── Enums             (StatoCommessa, TipoNotifica…)
  └── Constants         (LookupTables, MesDesignTokens, PaginaPolicy)

MESManager.Application
  ├── Interfaces        (IAnimeService, ICommessaAppService, IPianificazioneEngineService…)
  ├── Services          (AnimeService, CommessaAppService, AlertProduzioneService…)
  ├── DTOs              (AnimeDto, CommessaDto, PlcRealtimeDto…)
  └── Configuration     (DatabaseConfiguration, FileConfiguration)

MESManager.Infrastructure
  ├── Data              (MesManagerDbContext : IdentityDbContext<ApplicationUser>)
  ├── Services          (CommessaAppService, PlcSyncCoordinator, AnimeExcelImportService…)
  ├── Repositories      (AnimeRepository, AllegatoPreventivoRepository…)
  ├── Entities          (ApplicationUser)
  └── DependencyInjection.cs

MESManager.Web (Blazor Server)
  ├── Program.cs
  ├── Controllers       (MacchineController, PianificazioneController, PlcController…)
  ├── Components/Pages  (~35 pagine Razor)
  ├── Components/Layout (MainLayout, NavMenu)
  ├── Hubs              (RealtimeHub, PianificazioneHub)
  └── Services          (RealtimeStateService, PlcDataService, AppSettingsService, UserThemeService…)

MESManager.Worker      (SyncMagoWorker, SimulatorePLCWorker, RecipeAutoLoaderWorker)
MESManager.PlcSync     (PlcSyncService, PlcSyncCoordinator — processo separato)
MESManager.Sync        (SyncCommesseService, SyncArticoliService, SyncClientiService)
```

**Pattern principali rilevati:**
- Repository Pattern con interfacce in Application (ben applicato per Anime, Commesse, NC, Manutenzioni)
- Clean Architecture rispettata *in generale*, con **eccezioni significative** nei controller (vedi §3)
- Identity + ASP.NET Core per autenticazione; ruoli: Admin, Produzione, Manutenzione, Ufficio, Visualizzazione
- CascadingValue in MainLayout per `IsReadOnly` e `IsAdmin`
- AG Grid via JSInterop per le griglie tabellari
- Syncfusion Gantt v32 per pianificazione

---

## 2. [P0] Rischi Critici — Concorrenza Blazor Server + EF Core

> Questo è il **problema più pericoloso** identificato, concordato da entrambe le analisi.  
> Va risolto prima di tutto il resto.

### 2.1 [P0 — CRITICO] DbContext non thread-safe + circuiti Blazor concorrenti

Blazor Server mantiene circuiti utente attivi con servizi Scoped vivi. `MesManagerDbContext` **non è thread-safe** — non può essere usato contemporaneamente da più operazioni sullo stesso circuito né condiviso tra circuiti.

Situazioni rischiose attuali:
- `PlcController` inietta DbContext direttamente **e** ha 8 dipendenze su servizi che usano `IDbContextFactory` → lifecycle misto
- `MainLayout.razor.cs` usa `IServiceScopeFactory` per isolare `UserManager` (buona pratica), ma non sempre i servizi figli applicano lo stesso pattern
- `PianificazioneController` inietta sia DbContext diretto che servizi con proprio DbContext → possibile "A second operation was started on this context" in produzione
- `Task.WhenAll` su metodi che usano lo stesso DbContext scoped → eccezione garantita

Sintomi tipici: errori random "second operation started", dati corrotti intermittenti, eccezioni difficili da replicare.

**Soluzione:** ogni operazione parallela deve usare uno scope isolato (`IServiceScopeFactory.CreateAsyncScope()`). Nessun `DbContext` diretto nei controller — solo via servizi che gestiscono correttamente il lifetime.

### 2.2 [P0 — CRITICO] Doppia registrazione MesManagerDbContext
**File:** `MESManager.Infrastructure/DependencyInjection.cs` + `MESManager.Web/Program.cs`

`AddInfrastructure()` registra già `AddDbContext<MesManagerDbContext>` **e** `AddDbContextFactory<MesManagerDbContext>`. Program.cs aggiunge poi un secondo `AddDbContext<MesManagerDbContext>` — la seconda sovrascrive la prima con opzioni potenzialmente diverse.

Rischi:
- Lifetime incoerenti tra la registrazione DI.cs e quella Program.cs
- Tracking incoerente (una copia con tracking, l'altra con AsNoTracking implicito)
- Worker e Web potrebbero usare configurazioni diverse
- Memory leak da DbContext instances non correttamente disposed

### 2.3 [ALTO] Statici mutabili condivisi tra tutti i circuiti
**File:** `MESManager.Domain/Constants/LookupTables.cs`

I dizionari `Colla`, `Vernice`, `Sabbia`, `Imballo`, `TipologiaNc` sono `public static Dictionary<string, string>` con un metodo `Aggiorna()` che li muta in-place. In Blazor Server con N circuiti utente attivi, tutti condividono la stessa memoria statica.

Scenario di bug: utente A salva le impostazioni → `Aggiorna()` muta il dizionario → utente B legge un dizionario in stato intermedio → dati corrotti silenziosamente.

**Soluzione:** `IReadOnlyDictionary<string, string>` per le proprietà pubbliche. Il metodo `Aggiorna()` costruisce una nuova istanza e la assegna atomicamente tramite `Interlocked.Exchange` o lock.

### 2.4 [ALTO] Polling PLC duplicato — due servizi non coordinati
- `PlcDataService` (Scoped, `System.Threading.Timer`, chiama HTTP API — uno per ogni circuit attivo)
- `RealtimeStateService` (Singleton, `PeriodicTimer`, interroga DB via `IPlcAppService`, spinge via SignalR)

Con 10 utenti sulla pagina PlcStorico: 10 timer `PlcDataService` + 1 timer `RealtimeStateService` → 11 query/secondo allo stesso endpoint/DB. Nessun meccanismo di throttling, cache condivisa, o backpressure.

---

## 3. Configurazione Frammentata (Concordata da entrambe le analisi)

### 3.1 [ALTO] Segreti e connection string caricati in modo diverso per progetto
| Progetto | Come carica i segreti |
|---|---|
| `MESManager.Web` | `appsettings.Secrets.json` + DPAPI decryption |
| `MESManager.Worker` | Config separata, bootstrap diverso |
| `MESManager.PlcSync` | Processo separato, config propria |
| `MESManager.Sync` | Config propria |

Nel tempo, Web e Worker possono finire a usare connection string diverse (dopo una rotazione delle credenziali aggiornata solo in un posto). Bug "fantasma" classico nei MES.

**Soluzione a lungo termine:** centralizzare il bootstrap della configurazione in un assembly condiviso (es. `MESManager.Infrastructure` o un nuovo `MESManager.Hosting`).

### 3.2 [MEDIO] GanttDb — connection string legacy ancora referenziata
`Program.cs` commenta _"GanttDb non più usato - dati migrati in MESManagerDb"_, ma:
- `DatabaseConfiguration.cs` espone ancora `GanttDb`
- `ArticoloCatalogoService` la usa
- `AnimeImportService` la usa
- `AllegatoArticoloService` la usa con doppio fallback

Se `GanttDb` è davvero non più necessaria, va rimossa dalla `DatabaseConfiguration` e da tutti i servizi che la referenziano. Se è ancora usata, va rimosso il commento fuorviante.

---

## 4. Codice Duplicato / Confuso

### 4.1 [CRITICO] Dizionario VerniceLookup duplicato
**File:** `MESManager.Infrastructure/Services/CommessaAppService.cs` righe 19–29  
Copia letterale di `LookupTables.Vernice` (Domain). Il commento dice esplicitamente _"stesse di AnimeService"_ — violazione diretta ZERO DUPLICAZIONE (BIBBIA).  
**Soluzione:** rimuovere il campo privato e usare `LookupTables.Vernice`.

### 4.2 [ALTO] GridSettingsPanel non usato dove dovrebbe
`GridSettingsPanel.razor` è stato creato come componente condiviso, ma `CommesseAperte.razor`, `ProgrammaMacchine.razor` e `PlcRealtime.razor` **copiano il blocco inline**. Solo i cataloghi lo usano correttamente.

### 4.3 [MEDIO] Due librerie di immagini coesistenti nel Web project
- `Magick.NET-Q8-AnyCPU` — conversione HEIC → JPEG
- `SixLabors.ImageSharp` — compressione JPEG

Stesso controller (`AllegatiManutenzioneCasseController`) usa entrambe. Una sola libreria potrebbe coprire entrambi i casi d'uso.

### 4.4 [MEDIO] Mismatch nome file / nome classe nel Web/Services
| File | Classe/Interfaccia contenuta |
|---|---|
| `IArticoliPageService.cs` | `IPageToolbarService` |
| `ArticoliPageService.cs` | `PageToolbarService` |

### 4.5 [MEDIO] Repository che implementa interfaccia Service
- `AllegatoPreventivoRepository` implements `IAllegatoPreventivoService`
- `AllegatoNonConformitaRepository` implements `IAllegatoNonConformitaService`

Viola la convenzione Repository → IRepository / Service → IService. Rende difficile aggiungere un layer di caching o separare responsabilità in futuro.

### 4.6 [BASSO] IPlcSyncCoordinator definita nello stesso file dell'implementazione
**File:** `MESManager.Infrastructure/Services/PlcSyncCoordinator.cs`  
L'interfaccia dovrebbe stare in `MESManager.Application/Interfaces/`.

### 4.7 [BASSO] AnimeImportService senza interfaccia
`AnimeImportService` (Application) e `AnimeExcelImportService` (Infrastructure) sono classi concrete registrate direttamente nel DI. Non testabili tramite mock.

---

## 5. Codice Probabilmente Morto

> **Nota:** nei sistemi MES il codice "morto" non va eliminato frettolosamente — spesso viene usato da qualcosa che tutti hanno dimenticato. Verificare sempre prima di cancellare.

### 5.1 [CERTO] File da eliminare
| File | Motivo |
|---|---|
| `MainLayout.razor.bak` | Vecchio backup in source tree, non compilato ma confonde |
| `MESManager.Infrastructure/Repositories/IAnimeRepository.cs` | Contiene solo un commento "// Interfaccia vera in Application.Interfaces.IAnimeRepository" — file fantasma |
| `SyncGoogle.razor` | Pagina stub: solo "Pagina predisposta per sincronizzazione con servizi Google", zero funzionalità |

### 3.2 [PROBABILE] Entità Domain senza DbSet né UI
Le seguenti entità esistono nel Domain ma **non hanno DbSet** in `MesManagerDbContext` e non vengono referenziate da nessun service o pagina UI:
- `Quote`, `QuoteRow`, `QuoteAttachment`
- `PriceList`, `PriceListItem`
- `WorkProcessingType`, `WorkProcessingParameter`, `WorkProcessingTechnicalData`

Queste entità sono nominate in inglese (anomalia rispetto al resto italiano) e sembrano un tentativo precedente di modulo Preventivi/Listini poi sostituito dal modulo `Preventivo` italiano attuale.

### 5.3 [PROBABILE] Servizi non registrati nel DI
| Servizio | Note |
|---|---|
| `ArticoloCatalogoService` | Legge da GanttDb, non registrato nel DI, nessun riferimento fuori dal proprio file |
| `AnimeSyncValidation` | Utility di sviluppo, non registrata, non usata in pagine o controller |

### 5.4 [LEGACY] Migrazioni con UtenteApp
Le migrazioni `20260122135120_AddUtentiAppEPreferenze` e `20260126080956_AddColoreToUtenteApp` referenziano `UtenteApp` — entità non più nel Domain (sostituita da `ApplicationUser`). Restano per storia ma confondono se si cerca "UtenteApp" nel codice.

---

## 6. Logica Ripetuta Da Centralizzare

| Problema | Dove si ripete | Soluzione |
|---|---|---|
| Pannello impostazioni griglia (FontSize / RowHeight / Density / Zebra) | CommesseAperte.razor, ProgrammaMacchine.razor, PlcRealtime.razor (inline) | Usare `GridSettingsPanel.razor` già esistente |
| Toolbar toolbar-sticky con ricerca + bottone Aggiorna + Export | ~8 pagine Razor | Valutare componente `GridToolbar.razor` condiviso |
| Blocco `FixResetMenu` + salvataggio preferenze griglia | ~6 pagine | Già gestito da `CatalogoGridBase` per i cataloghi; le pagine Programma/Produzione non ereditano da essa |

---

## 7. Violazioni Clean Architecture

### 7.1 [CRITICO] Controller che accedono direttamente a DbContext
| Controller | DbContext diretto | Note |
|---|---|---|
| `MacchineController` | Sì | LINQ su DbContext invece di `IMacchinaAppService` |
| `PianificazioneController` | Sì + servizi | Mix DbContext diretto e servizi — concurrency risk concreto |
| `PlcController` | Sì + 8 servizi | 8 dipendenze iniettate incluso DbContext diretto |
| `DiagnosticsController` | Sì | Admin-only, diagnostica cataloghi |
| `DbMaintenanceController` | Sì + SQL raw | Crea tabella `Festivi` via `ExecuteSqlRawAsync` — workaround per migration mancante |

### 7.2 [MEDIO] AllegatiAnimaController inietta classe concreta
```csharp
// Sbagliato — classe concreta:
AllegatiAnimaService allegatiService
// Corretto — interfaccia:
IAllegatiAnimaService allegatiService
```

---

## 8. Dipendenze NuGet Ridondanti

| Package | Progetti | Note |
|---|---|---|
| `EPPlus` | Application + Infrastructure | Dovrebbe stare in uno solo dei due layer |
| `QuestPDF` | Application + Infrastructure | `AnimePdfService` è in Application, Infrastructure lo referenzia inutilmente |
| `Sharp7` | Infrastructure + PlcSync | PlcSync è processo separato; verificare se Infrastructure ne ha davvero bisogno |
| `SixLabors.ImageSharp` + `Magick.NET` | Web | Stessa funzione, due librerie nello stesso controller |

---

## 9. Rischi Sicurezza e Scalabilità Residui

### 9.1 [MEDIO] AppSettingsService scrive su file senza lock
`wwwroot/app-settings.json` scritto senza lock esplicito. Scrittura concorrente da due Admin → corruzione JSON. Attualmente accettabile (Admin sono pochi) ma va documentato esplicitamente.

### 9.2 [MEDIO] DbMaintenanceController — schema via SQL raw
`ExecuteSqlRawAsync(createTableSql)` — non è SQL injection (nessun input utente), ma bypassa completamente EF Migrations. Lo schema DB può divergere dallo snapshot delle migration ufficiali.  
**Soluzione:** migration EF apposita per `Festivi`, poi eliminare questo controller.

### 9.3 [BASSO] AllegatiAnimaController — crash ritardato da configurazione errata
Se la configurazione è sbagliata, iniettando la classe concreta il crash avviene solo al primo utilizzo in runtime, non al startup.

---

## 10. Aree Non Approfondite da Nessuna Analisi

Entrambe le analisi hanno un punto cieco comune — richiedono un'analisi dedicata separata:

### 10.1 Thread-safety del layer PLC (PlcSync)
Non è stata analizzata la gestione di: queue, buffering, retry su disconnessione PLC, throttling, reconnect automatico. In un MES industriale questi aspetti sono critici per la continuità operativa.

### 10.2 Memory pressure Blazor Server
Con AG Grid (JS heavy), molti circuiti attivi e JSInterop frequente su GanttMacchine, non è stato analizzato il consumo di memoria per circuito, la frequenza di GC pressure, né il comportamento sotto carico con 10+ utenti contemporanei.

### 10.3 Transaction boundaries e Unit of Work
Non è stato verificato se le operazioni multi-entità (es. chiusura commessa + aggiornamento macchina + log storico) usano transazioni esplicite o si affidano a singoli `SaveChanges`. In assenza di transazioni, una failure parziale lascia il DB in stato inconsistente.

### 10.4 Dipendenze circolari latenti
Il layering `Application → Infrastructure` è dichiarato Clean, ma non è stato eseguito un dependency graph formale (es. con `dotnet-depends` o NDepend) per verificare che non ci siano leakage latenti da Infrastructure verso Web.

---

## 11. Piano di Refactoring — Ordine Corretto

> **Ordine corretto: Sicurezza Strutturale → Centralizzazione → Riduzione Monoliti → Codice Morto (per ultimo)**  
> Il codice "morto" va eliminato solo a valle — nei MES spesso risulta usato da qualcosa dimenticata da tutti.

### FASE 1 — Sicurezza Strutturale (P0, alta urgenza)

| # | Azione | File | Rischio |
|---|---|---|---|
| 1a | Rimuovere doppia `AddDbContext` da Program.cs | `Program.cs` | Medio |
| 1b | Auditare tutti i `Task.WhenAll` che toccano DbContext | controller / servizi | Alto |
| 1c | Rendere LookupTables thread-safe (`IReadOnlyDictionary` + lock) | `LookupTables.cs` | Medio |
| 1d | Rimuovere `DbContext` diretto da `MacchineController` → `IMacchinaAppService` | `MacchineController.cs` | Medio |
| 1e | `DbMaintenanceController` → migration EF per Festivi, poi elimina controller | `DbMaintenanceController.cs` | Medio |

**Test dopo fase 1:** build verde + login + CRUD commessa + pagina PLC Realtime + 2 sessioni parallele (verifica no "second operation on context")

### FASE 2 — Centralizzazione Config e DI

| # | Azione | File | Rischio |
|---|---|---|---|
| 2a | Allineare bootstrap config tra Web / Worker / PlcSync | `Program.cs` di ogni progetto | Medio |
| 2b | Decidere su `GanttDb`: rimuovere o documentare come attivo | `DatabaseConfiguration.cs` | Basso |
| 2c | Centralizzare logging policy comune tra i progetti | tutti | Basso |
| 2d | Spostare `IPlcSyncCoordinator` in `Application/Interfaces/` | `PlcSyncCoordinator.cs` | Basso |

**Test dopo fase 2:** build verde + avvio Worker + avvio PlcSync + verifica connessioni

### FASE 3 — Riduzione Monoliti

| # | Azione | Rischio |
|---|---|---|
| 3a | Scomporre `PianificazioneEngineService` in servizi con responsabilità singola | Alto |
| 3b | Scomporre `PianificazioneController` (inietta troppo, fa troppo) | Alto |
| 3c | Completare fix controller-DbContext rimasti: `PianificazioneController`, `PlcController`, `DiagnosticsController` | Alto |
| 3d | Usare `GridSettingsPanel` nelle 3 pagine che lo duplicano (CommesseAperte, ProgrammaMacchine, PlcRealtime) | Basso |
| 3e | Fix `VerniceLookup` duplicato in CommessaAppService | Basso |

**Test dopo fase 3:** test E2E completo (Gantt, Pianificazione, PLC)

### FASE 4 — Pulizie e Codice Morto (ultima)

| # | Azione | File | Rischio |
|---|---|---|---|
| 4a | Eliminare file morti certi | `.bak`, `IAnimeRepository.cs` fantasma, `SyncGoogle.razor` | Nullo |
| 4b | Rinominare file con nome sbagliato | `IArticoliPageService.cs` → `IPageToolbarService.cs` | Basso |
| 4c | Verificare e archiviare entità orfane | `Quote`, `PriceList`, `WorkProcessingType`… | Basso |
| 4d | Rimuovere `ArticoloCatalogoService` e `AnimeSyncValidation` se confermato inutilizzate | Application/Services/ | Basso |

---

## 12. Quick Wins — File da Toccare per Primi

| Priorità | File | Azione | Rischio |
|---|---|---|---|
| 1 | `Web/Program.cs` ~riga 200 | Rimuovere doppia `AddDbContext` | Medio |
| 2 | `Domain/Constants/LookupTables.cs` | Rendere `IReadOnlyDictionary` + lock su `Aggiorna()` | Medio |
| 3 | `Web/Controllers/MacchineController.cs` | Delegare a `IMacchinaAppService` | Medio |
| 4 | `Web/Controllers/DbMaintenanceController.cs` | Migration EF per Festivi + elimina controller | Medio |
| 5 | `Infrastructure/Services/CommessaAppService.cs` righe 19-29 | Rimuovi `VerniceLookup`, usa `LookupTables.Vernice` | Basso |
| 6 | `Web/Services/IArticoliPageService.cs` | Rinomina → `IPageToolbarService.cs` | Basso |
| 7 | `Web/Services/ArticoliPageService.cs` | Rinomina → `PageToolbarService.cs` | Basso |
| 8 | `MainLayout.razor.bak` | Elimina | Nullo |
| 9 | `Infrastructure/Repositories/IAnimeRepository.cs` | Elimina | Nullo |
| 10 | `Web/Components/Pages/Sync/SyncGoogle.razor` | Elimina o documenta come placeholder | Nullo |

---

## 13. Test Da Eseguire Dopo Ogni Fase

1. `dotnet build MESManager.sln --nologo` → **0 errori obbligatori**
2. Avvio server: `dotnet run --project MESManager.Web/MESManager.Web.csproj --environment Development`
3. Login + navigazione 3 pagine principali (Commesse Aperte, Gantt Macchine, PLC Realtime)
4. Test CRUD commessa (crea, modifica, chiudi)
5. Test salvataggio impostazioni griglia
6. Test cambio tema (dark/light)
7. Test export Excel/CSV su almeno una pagina
8. **Dopo fase 1:** aprire 2 sessioni browser in parallelo + operazione CRUD contemporanea → verificare assenza "A second operation was started on this context instance"

Per le fasi 3+: eseguire anche `dotnet test` se i test E2E nel progetto `tests/` sono configurati.

---

## Appendice A: Servizi Senza Interfaccia Registrati come Concreti

| Servizio | Layer | Registrazione DI | Problema |
|---|---|---|---|
| `AnimeImportService` | Application | `AddScoped<AnimeImportService>()` in Program.cs | Non mockabile |
| `AnimeExcelImportService` | Infrastructure | Non trovata nel DI | Probabile dead code o iniezione nascosta |
| `AllegatiAnimaService` | Application | `AddScoped<AllegatiAnimaService>()` in Program.cs | Ha interfaccia ma il controller non la usa |
| `ArticoloCatalogoService` | Application | **Non registrata** | Probabile dead code |
| `AnimeSyncValidation` | Application | **Non registrata** | Probabile dead code |

---

## Appendice B: Giudizio Finale

Il progetto **non è disastroso**. È cresciuto velocemente — funzionale, pragmatico, pieno di workaround, con refactor incompleti. Normalissimo per un MES industriale realtime costruito mentre l'azienda lavora.

Le fondamenta sono già corrette:
- ✅ Layering Clean Architecture dichiarato
- ✅ BIBBIA come punto di verità del progetto
- ✅ Test E2E presenti
- ✅ Separazione Worker / PLC / Web
- ✅ Naming abbastanza coerente

Il momento è quello giusto: il progetto sta entrando nella fase dove ogni modifica futura inizierà a diventare più rischiosa se non si consolida prima. I problemi trovati sono **centralizzabili, modularizzabili, correggibili gradualmente** — non richiedono una riscrittura.

*Report integrato da analisi Claude (GitHub Copilot) + Codex (OpenAI). Nessuna modifica apportata al codice.*

