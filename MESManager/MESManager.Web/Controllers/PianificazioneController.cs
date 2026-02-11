using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Web.Hubs;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class PianificazioneController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    private readonly IPianificazioneEngineService _engineService;
    private readonly PianificazioneNotificationService _notificationService;
    private readonly ILogger<PianificazioneController> _logger;

    public PianificazioneController(
        MesManagerDbContext context,
        IPianificazioneService pianificazioneService,
        IPianificazioneEngineService engineService,
        PianificazioneNotificationService notificationService,
        ILogger<PianificazioneController> logger)
    {
        _context = context;
        _pianificazioneService = pianificazioneService;
        _engineService = engineService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutte le commesse con dati per il Gantt
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommessaGanttDto>>> GetCommesseGantt()
    {
        try
        {
            await AutoCompletaCommesseAsync();

            // Ottieni le impostazioni di produzione (o usa valori di default)
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            var commesse = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina != null && c.StatoProgramma != StatoProgramma.Archiviata) // Solo commesse assegnate
                .OrderBy(c => c.NumeroMacchina)
                .ThenBy(c => c.OrdineSequenza)
                .ToListAsync();

            // Batch loading Anime (fix N+1 queries)
            var articoloCodes = commesse
                .Where(c => c.Articolo != null)
                .Select(c => c.Articolo!.Codice)
                .Distinct()
                .ToList();
            
            var animeData = await _context.Anime
                .Where(a => articoloCodes.Contains(a.CodiceArticolo))
                .ToListAsync();
            
            var animeLookup = animeData
                .GroupBy(a => a.CodiceArticolo)
                .ToDictionary(g => g.Key, g => g.First());

            // Usa metodo centralizzato con batch loading
            var commesseGantt = await _pianificazioneService.MapToGanttDtoBatchAsync(commesse, impostazioni, animeLookup);

            return Ok(commesseGantt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero delle commesse per il Gantt");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Auto-completa commesse che hanno superato la data fine prevista
    /// </summary>
    [HttpPost("auto-completa")]
    public async Task<IActionResult> AutoCompleta()
    {
        try
        {
            var updated = await AutoCompletaCommesseAsync();
            if (updated > 0 && _notificationService != null)
            {
                await _notificationService.NotifyFullRecalculationAsync(0);
            }

            return Ok(new { updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore auto-completamento commesse");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Aggiorna le date di pianificazione di una commessa
    /// </summary>
    [HttpPut("{id}/pianificazione")]
    public async Task<IActionResult> AggiornaPianificazione(Guid id, [FromBody] AggiornaPianificazioneRequest request)
    {
        try
        {
            var commessa = await _context.Commesse
                .Include(c => c.Articolo)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commessa == null)
            {
                return NotFound($"Commessa con ID {id} non trovata");
            }

            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            // Carica calendario lavoro
            var calendario = await GetCalendarioLavoroDtoAsync();

            // Aggiorna le date
            commessa.DataInizioPrevisione = request.DataInizioPrevisione;
            commessa.NumeroMacchina = request.NumeroMacchina;

            // Ricalcola la data fine se presente articolo con dati produttivi
            if (request.DataInizioPrevisione.HasValue && commessa.Articolo != null)
            {
                var durataMinuti = _pianificazioneService.CalcolaDurataPrevistaMinuti(
                    commessa.Articolo.TempoCiclo,
                    commessa.Articolo.NumeroFigure,
                    commessa.QuantitaRichiesta,
                    impostazioni.TempoSetupMinuti
                );

                commessa.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                    request.DataInizioPrevisione.Value,
                    durataMinuti,
                    calendario,
                    new HashSet<DateOnly>()
                );
            }

            commessa.UltimaModifica = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento della pianificazione per la commessa {Id}", id);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Ottiene le impostazioni di produzione
    /// </summary>
    [HttpGet("impostazioni")]
    public async Task<ActionResult<ImpostazioniProduzioneDto>> GetImpostazioni()
    {
        try
        {
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync();
            
            if (impostazioni == null)
            {
                // Crea e salva impostazioni di default
                impostazioni = new ImpostazioniProduzione
                {
                    Id = Guid.NewGuid(),
                    TempoSetupMinuti = 90,
                    OreLavorativeGiornaliere = 8,
                    GiorniLavorativiSettimanali = 5,
                    UltimaModifica = DateTime.UtcNow
                };
                _context.ImpostazioniProduzione.Add(impostazioni);
                await _context.SaveChangesAsync();
            }

            return Ok(new ImpostazioniProduzioneDto
            {
                Id = impostazioni.Id,
                TempoSetupMinuti = impostazioni.TempoSetupMinuti,
                OreLavorativeGiornaliere = impostazioni.OreLavorativeGiornaliere,
                GiorniLavorativiSettimanali = impostazioni.GiorniLavorativiSettimanali,
                UltimaModifica = impostazioni.UltimaModifica
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero delle impostazioni di produzione");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Aggiorna le impostazioni di produzione
    /// </summary>
    [HttpPut("impostazioni")]
    public async Task<IActionResult> AggiornaImpostazioni([FromBody] ImpostazioniProduzioneDto dto)
    {
        try
        {
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync();
            
            if (impostazioni == null)
            {
                impostazioni = new ImpostazioniProduzione { Id = Guid.NewGuid() };
                _context.ImpostazioniProduzione.Add(impostazioni);
            }

            impostazioni.TempoSetupMinuti = dto.TempoSetupMinuti;
            impostazioni.OreLavorativeGiornaliere = dto.OreLavorativeGiornaliere;
            impostazioni.GiorniLavorativiSettimanali = dto.GiorniLavorativiSettimanali;
            impostazioni.UltimaModifica = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento delle impostazioni di produzione");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Sposta una commessa su una macchina con accodamento rigido
    /// </summary>
    [HttpPost("sposta-commessa")]
    public async Task<ActionResult<SpostaCommessaResponse>> SpostaCommessa([FromBody] SpostaCommessaRequest request)
    {
        // VALIDAZIONE INPUT ROBUSTA
        if (request == null)
        {
            return BadRequest(new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = "Request nullo",
                UpdateVersion = DateTime.UtcNow.Ticks
            });
        }

        if (request.CommessaId == Guid.Empty)
        {
            return BadRequest(new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = "ID commessa non valido",
                UpdateVersion = DateTime.UtcNow.Ticks
            });
        }

        if (request.TargetMacchina < 1 || request.TargetMacchina > 99)
        {
            return BadRequest(new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = $"Numero macchina non valido: {request.TargetMacchina}",
                UpdateVersion = DateTime.UtcNow.Ticks
            });
        }

        try
        {
            _logger.LogInformation("Spostamento commessa {CommessaId} su macchina {Macchina}", 
                request.CommessaId, request.TargetMacchina);

            var result = await _engineService.SpostaCommessaAsync(request);
            
            if (!result.Success)
            {
                _logger.LogWarning("Spostamento fallito: {Error}", result.ErrorMessage);
                return BadRequest(result);
            }
            
            // Notifica SignalR delle modifiche
            if (_notificationService != null)
            {
                var allCommesse = result.CommesseAggiornate.ToList();
                if (result.CommesseMacchinaOrigine != null)
                {
                    allCommesse.AddRange(result.CommesseMacchinaOrigine);
                }
                await _notificationService.NotifyCommesseUpdatedAsync(allCommesse, null);
            }
            
            _logger.LogInformation("Commessa spostata con successo, updateVersion: {Version}", result.UpdateVersion);
            return Ok(result);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict spostando commessa {CommessaId}", request.CommessaId);
            return Conflict(new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = "I dati sono stati modificati da un altro utente. Aggiorna la pagina e riprova.",
                UpdateVersion = DateTime.UtcNow.Ticks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nello spostamento della commessa {CommessaId}", request.CommessaId);
            return StatusCode(500, new SpostaCommessaResponse 
            { 
                Success = false, 
                ErrorMessage = "Errore interno del server",
                UpdateVersion = DateTime.UtcNow.Ticks
            });
        }
    }

    /// <summary>
    /// Suggerisce la macchina migliore per una commessa (bilanciamento carico)
    /// </summary>
    [HttpPost("suggerisci-macchina")]
    public async Task<ActionResult<SuggerisciMacchinaResponse>> SuggerisciMacchina([FromBody] SuggerisciMacchinaRequest request)
    {
        try
        {
            var result = await _engineService.SuggerisciMacchinaMiglioreAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel suggerimento macchina per commessa {CommessaId}", request.CommessaId);
            return StatusCode(500, new SuggerisciMacchinaResponse
            {
                Success = false,
                ErrorMessage = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// 🚀 CARICA SUL GANTT - Auto-scheduler intelligente v1.31
    /// Assegna automaticamente una commessa alla macchina con minore carico
    /// </summary>
    [HttpPost("carica-su-gantt/{commessaId}")]
    public async Task<ActionResult<CaricaSuGanttResponse>> CaricaSuGantt(Guid commessaId)
    {
        try
        {
            _logger.LogInformation("🚀 [CARICA GANTT] Request per commessa {CommessaId}", commessaId);
            
            var result = await _engineService.CaricaSuGanttAsync(commessaId);
            
            if (!result.Success)
            {
                _logger.LogWarning("⚠️ Caricamento fallito: {Error}", result.ErrorMessage);
                return BadRequest(result);
            }
            
            _logger.LogInformation("✅ Commessa caricata su macchina {Macchina}", result.MacchinaAssegnata);
            
            // Notifica SignalR per refresh Gantt
            if (_notificationService != null)
            {
                await _notificationService.NotifyFullRecalculationAsync(result.UpdateVersion);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Errore caricamento su Gantt per commessa {CommessaId}", commessaId);
            return StatusCode(500, new CaricaSuGanttResponse
            {
                Success = false,
                ErrorMessage = $"Errore interno: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Ricalcola tutte le commesse di tutte le macchine
    /// </summary>
    [HttpPost("ricalcola-tutto")]
    public async Task<IActionResult> RicalcolaTutto()
    {
        try
        {
            await _engineService.RicalcolaTutteCommesseAsync();
            
            // Notifica SignalR full reload
            if (_notificationService != null)
            {
                await _notificationService.NotifyFullRecalculationAsync();
            }
            
            return Ok(new { message = "Ricalcolo completato" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel ricalcolo delle commesse");
            return StatusCode(500, "Errore interno del server");
        }
    }

    #region Festivi

    /// <summary>
    /// Ottiene tutti i festivi
    /// </summary>
    [HttpGet("festivi")]
    public async Task<ActionResult<IEnumerable<FestivoDto>>> GetFestivi()
    {
        try
        {
            var festivi = await _context.Festivi
                .OrderBy(f => f.Data)
                .Select(f => new FestivoDto
                {
                    Id = f.Id,
                    Data = f.Data,
                    Descrizione = f.Descrizione,
                    Ricorrente = f.Ricorrente,
                    Anno = f.Anno
                })
                .ToListAsync();

            return Ok(festivi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero dei festivi");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Crea un nuovo festivo
    /// </summary>
    [HttpPost("festivi")]
    public async Task<ActionResult<FestivoDto>> CreateFestivo([FromBody] CreateFestivoRequest request)
    {
        try
        {
            var festivo = new Festivo
            {
                Id = Guid.NewGuid(),
                Data = request.Data,
                Descrizione = request.Descrizione,
                Ricorrente = request.Ricorrente,
                Anno = request.Ricorrente ? null : request.Data.Year,
                DataCreazione = DateTime.UtcNow
            };

            _context.Festivi.Add(festivo);
            await _context.SaveChangesAsync();

            // Ricalcola tutte le commesse considerando il nuovo festivo
            await _engineService.RicalcolaTutteCommesseAsync();

            return CreatedAtAction(nameof(GetFestivi), new FestivoDto
            {
                Id = festivo.Id,
                Data = festivo.Data,
                Descrizione = festivo.Descrizione,
                Ricorrente = festivo.Ricorrente,
                Anno = festivo.Anno
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella creazione del festivo");
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Elimina un festivo
    /// </summary>
    [HttpDelete("festivi/{id}")]
    public async Task<IActionResult> DeleteFestivo(Guid id)
    {
        try
        {
            var festivo = await _context.Festivi.FindAsync(id);
            if (festivo == null)
            {
                return NotFound();
            }

            _context.Festivi.Remove(festivo);
            await _context.SaveChangesAsync();

            // Ricalcola tutte le commesse
            await _engineService.RicalcolaTutteCommesseAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'eliminazione del festivo {Id}", id);
            return StatusCode(500, "Errore interno del server");
        }
    }

    /// <summary>
    /// Inizializza i festivi italiani standard (ricorrenti)
    /// </summary>
    [HttpPost("festivi/inizializza-standard")]
    public async Task<IActionResult> InizializzaFestiviStandard()
    {
        try
        {
            // Verifica se esistono già festivi
            if (await _context.Festivi.AnyAsync())
            {
                return BadRequest(new { message = "Festivi già presenti. Eliminarli prima di reinizializzare." });
            }

            var festiviItaliani = new List<Festivo>
            {
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 1, 1), Descrizione = "Capodanno", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 1, 6), Descrizione = "Epifania", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 4, 25), Descrizione = "Festa della Liberazione", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 5, 1), Descrizione = "Festa del Lavoro", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 6, 2), Descrizione = "Festa della Repubblica", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 8, 15), Descrizione = "Ferragosto", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 11, 1), Descrizione = "Tutti i Santi", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 12, 8), Descrizione = "Immacolata Concezione", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 12, 25), Descrizione = "Natale", Ricorrente = true },
                new() { Id = Guid.NewGuid(), Data = new DateOnly(2000, 12, 26), Descrizione = "Santo Stefano", Ricorrente = true },
            };

            _context.Festivi.AddRange(festiviItaliani);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Inizializzati {festiviItaliani.Count} festivi italiani standard" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'inizializzazione dei festivi standard");
            return StatusCode(500, "Errore interno del server");
        }
    }

    #endregion
    
    /// <summary>
    /// Aggiorna la priorità di una commessa
    /// </summary>
    [HttpPut("{id}/priorita")]
    public async Task<IActionResult> AggiornaPriorita(Guid id, [FromBody] AggiornaPrioritaRequest request)
    {
        try
        {
            var commessa = await _context.Commesse.FindAsync(id);
            if (commessa == null)
                return NotFound();
            
            commessa.Priorita = request.Priorita;
            await _context.SaveChangesAsync();
            
            // Ricalcola solo la macchina interessata considerando priorità
            if (commessa.NumeroMacchina.HasValue)
            {
                await _engineService.RicalcolaMacchinaConBlocchiAsync(commessa.NumeroMacchina);
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento priorità commessa {Id}", id);
            return StatusCode(500, "Errore interno del server");
        }
    }
    
    /// <summary>
    /// Blocca o sblocca una commessa
    /// </summary>
    [HttpPut("{id}/blocca")]
    public async Task<IActionResult> AggiornaBlocco(Guid id, [FromBody] AggiornaBloccoRequest request)
    {
        try
        {
            var commessa = await _context.Commesse.FindAsync(id);
            if (commessa == null)
                return NotFound();
            
            commessa.Bloccata = request.Bloccata;
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento blocco commessa {Id}", id);
            return StatusCode(500, "Errore interno del server");
        }
    }
    
    /// <summary>
    /// Aggiorna i vincoli temporali di una commessa
    /// </summary>
    [HttpPut("{id}/vincoli")]
    public async Task<IActionResult> AggiornaVincoli(Guid id, [FromBody] AggiornaVincoliRequest request)
    {
        try
        {
            var commessa = await _context.Commesse.FindAsync(id);
            if (commessa == null)
                return NotFound();
            
            commessa.VincoloDataInizio = request.VincoloDataInizio;
            commessa.VincoloDataFine = request.VincoloDataFine;
            await _context.SaveChangesAsync();
            
            // Ricalcola la macchina considerando i vincoli
            if (commessa.NumeroMacchina.HasValue)
            {
                await _engineService.RicalcolaMacchinaConBlocchiAsync(commessa.NumeroMacchina);
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'aggiornamento vincoli commessa {Id}", id);
            return StatusCode(500, "Errore interno del server");
        }
    }
    
    /// <summary>
    /// Ottiene suggerimenti macchina per una commessa (endpoint GET per comodità)
    /// </summary>
    [HttpGet("suggerisci-macchina/{commessaId}")]
    public async Task<ActionResult<List<SuggerimentoMacchinaDto>>> GetSuggerimentiMacchina(Guid commessaId)
    {
        try
        {
            var request = new SuggerisciMacchinaRequest { CommessaId = commessaId };
            var result = await _engineService.SuggerisciMacchinaMiglioreAsync(request);
            
            if (!result.Success || result.Valutazioni == null)
            {
                return BadRequest(result.ErrorMessage ?? "Errore nel calcolo suggerimenti");
            }
            
            // Converti Valutazioni in SuggerimentoMacchinaDto per il frontend
            var suggerimenti = result.Valutazioni.Select(v => new SuggerimentoMacchinaDto
            {
                NumeroMacchina = int.TryParse(v.NumeroMacchina, out var num) ? num : 0,
                NomeMacchina = v.NomeMacchina,
                CaricoPercentuale = v.CaricoPrevisto / 480, // Converti minuti in % (8h = 480min)
                DataFinePrevista = v.DataFinePrevista ?? DateTime.Now,
                PosizioneInCoda = v.NumeroCommesseInCoda
            }).ToList();
            
            return Ok(suggerimenti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel suggerimento macchina per commessa {CommessaId}", commessaId);
            return StatusCode(500, "Errore interno del server");
        }
    }
    
    /// <summary>
    /// DEBUG: Verifica lo stato delle commesse
    /// </summary>
    [HttpGet("debug-commesse")]
    public async Task<IActionResult> DebugCommesse()
    {
        var total = await _context.Commesse.CountAsync();
        var conMacchina = await _context.Commesse.Where(c => c.NumeroMacchina != null).CountAsync();
        var conDate = await _context.Commesse.Where(c => c.DataInizioPrevisione.HasValue).CountAsync();
        var entrambi = await _context.Commesse.Where(c => c.NumeroMacchina != null && c.DataInizioPrevisione.HasValue).CountAsync();
        var programmate = await _context.Commesse.Where(c => c.StatoProgramma == StatoProgramma.Programmata).CountAsync();
        var aperte = await _context.Commesse.Where(c => c.Stato == StatoCommessa.Aperta).CountAsync();
        var aperteConMacchina = await _context.Commesse.Where(c => c.Stato == StatoCommessa.Aperta && c.NumeroMacchina != null).CountAsync();
        
        return Ok(new {
            totaleCommesse = total,
            conMacchina,
            conDate,
            conMacchinaEDate = entrambi,
            statoProgrammataProgrammata = programmate,
            statoAperta = aperte,
            aperteConMacchina
        });
    }
    
    /// <summary>
    /// 🔍 DEBUG v1.30.8: Test FILTRO ESATTO usato da ProgrammaMacchine.razor
    /// Replica identico il WHERE client-side per verificare cosa passa
    /// </summary>
    [HttpGet("test-filtro-programma")]
    public async Task<IActionResult> TestFiltroProgramma()
    {
        var tutte = await _context.Commesse
            .Include(c => c.Articolo)
            .ToListAsync();
            
        var conMacchina = tutte.Where(c => c.NumeroMacchina.HasValue).ToList();
        var conDate = tutte.Where(c => c.DataInizioPrevisione.HasValue).ToList();
        var nonCompletata = tutte.Where(c => c.StatoProgramma != StatoProgramma.Completata).ToList();
        var nonArchiviata = tutte.Where(c => c.StatoProgramma != StatoProgramma.Archiviata).ToList();
        var aperte = tutte.Where(c => c.Stato == StatoCommessa.Aperta).ToList();
        
        // FILTRO FINALE ESATTO (come in ProgrammaMacchine.razor)
        var filtrate = tutte.Where(c =>
            c.NumeroMacchina.HasValue &&
            c.DataInizioPrevisione.HasValue &&
            c.StatoProgramma != StatoProgramma.Completata &&
            c.StatoProgramma != StatoProgramma.Archiviata &&
            c.Stato == StatoCommessa.Aperta
        ).ToList();
        
        // Sample delle prime 5 che passano il filtro
        var sample = filtrate.Take(5).Select(c => new {
            c.Codice,
            c.NumeroMacchina,
            dataInizio = c.DataInizioPrevisione?.ToString("yyyy-MM-dd"),
            dataFine = c.DataFinePrevisione?.ToString("yyyy-MM-dd"),
            statoProgramma = c.StatoProgramma.ToString(),
            stato = c.Stato.ToString(),
            articolo = c.Articolo?.Codice
        });
        
        return Ok(new {
            totale = tutte.Count,
            stepByStep = new {
                conMacchina = conMacchina.Count,
                conDate = conDate.Count,
                nonCompletata = nonCompletata.Count,
                nonArchiviata = nonArchiviata.Count,
                aperte = aperte.Count
            },
            filtrateFinali = filtrate.Count,
            sample
        });
    }
    
    /// <summary>
    /// Esporta la programmazione Gantt sulla pagina Programma (applica date e ordine)
    /// DEBUG: Logging aggressivo per verificare ogni step
    /// </summary>
    [HttpPost("esporta-su-programma")]
    public async Task<IActionResult> EsportaSuProgramma()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🚀 [EXPORT START] Inizio esportazione su programma");
        
        try
        {
            await AutoCompletaCommesseAsync();

            // STEP 1: Carica tutte commesse (debug)
            var tutteCommesse = await _context.Commesse.ToListAsync();
            _logger.LogInformation("📊 STEP 1: Caricate {Total} commesse totali dal DB", tutteCommesse.Count);
            
            // STEP 2: Filtra per NumeroMacchina
            var conMacchina = tutteCommesse
                .Where(c => c.NumeroMacchina != null
                            && c.StatoProgramma != StatoProgramma.Completata
                            && c.StatoProgramma != StatoProgramma.Archiviata)
                .ToList();
            _logger.LogInformation("🔧 STEP 2: Filtro NumeroMacchina != null: {Count} commesse", conMacchina.Count);
            
            // STEP 3: Filtra per DataInizioPrevisione
            var conDataInizio = conMacchina.Where(c => c.DataInizioPrevisione.HasValue).ToList();
            _logger.LogInformation("📅 STEP 3: Filtro DataInizioPrevisione.HasValue: {Count} commesse", conDataInizio.Count);
            
            // STEP 3b: Log dettagli commesse senza data
            var senzaDataInizio = conMacchina.Where(c => !c.DataInizioPrevisione.HasValue).ToList();
            if (senzaDataInizio.Count > 0)
            {
                _logger.LogWarning("⚠️ STEP 3b: {Count} commesse CON macchina ma SENZA DataInizioPrevisione", senzaDataInizio.Count);
                foreach (var c in senzaDataInizio.Take(5))
                {
                    _logger.LogWarning("   - {Codice} | Macchina={Macchina} | DataInizio={DataInizio}", 
                        c.Codice, c.NumeroMacchina, c.DataInizioPrevisione);
                }
            }
            
            // STEP 4: Verifica se ci sono commesse da esportare
            if (conDataInizio.Count == 0 && conMacchina.Count > 0)
            {
                _logger.LogWarning("⚠️ Nessuna commessa con DataInizioPrevisione. Avvio ricalcolo automatico.");
                await _engineService.RicalcolaTutteCommesseAsync();

                // Ricarica dopo ricalcolo
                conDataInizio = await _context.Commesse
                    .Where(c => c.NumeroMacchina != null
                                && c.DataInizioPrevisione.HasValue
                                && c.StatoProgramma != StatoProgramma.Completata
                                && c.StatoProgramma != StatoProgramma.Archiviata)
                    .ToListAsync();
            }

            if (conDataInizio.Count == 0)
            {
                _logger.LogWarning("❌ EXPORT FALLITO: Nessuna commessa da esportare (Count=0)");
                _logger.LogWarning("   Commesse totali: {Total}", tutteCommesse.Count);
                _logger.LogWarning("   Con Macchina: {ConMacchina}", conMacchina.Count);
                _logger.LogWarning("   Con DataInizio: {ConDataInizio}", conDataInizio.Count);
                
                return Ok(new 
                { 
                    success = false, 
                    message = "Nessuna commessa da esportare",
                    debugInfo = new
                    {
                        totali = tutteCommesse.Count,
                        conMacchina = conMacchina.Count,
                        conDataInizio = conDataInizio.Count
                    }
                });
            }
            
            _logger.LogInformation("✅ Trovate {Count} commesse idonee all'export", conDataInizio.Count);
            
            // STEP 5: Carica dal DB correttamente (non in memoria)
            var commesseDaProgrammare = await _context.Commesse
                .Where(c => c.NumeroMacchina != null
                            && c.DataInizioPrevisione.HasValue
                            && c.StatoProgramma != StatoProgramma.Completata
                            && c.StatoProgramma != StatoProgramma.Archiviata)
                .ToListAsync();
            
            _logger.LogInformation("🔄 STEP 5: Query DB eseguita: {Count} commesse", commesseDaProgrammare.Count);
            
            // STEP 6: Segna tutte come esportate (indipendentemente dallo stato precedente)
            // FIX: La semantica è "esporta tutto ciò che è planificato", non "cambia stato solo se NonProgrammata"
            var aggiornate = 0;
            foreach (var commessa in commesseDaProgrammare)
            {
                var statoPrima = commessa.StatoProgramma;
                
                // NOTA: StatoProgramma è automaticamente impostato a Programmata quando viene assegnata una macchina.
                // L'export segna semplicemente che è stato esportato al programma esterno.
                // Qui verifichiamo che abbia una macchina e una data di inizio (già fatto nel Where)
                
                commessa.StatoProgramma = StatoProgramma.Programmata;
                commessa.DataCambioStatoProgramma = DateTime.Now;
                commessa.UltimaModifica = DateTime.UtcNow;
                aggiornate++;
                
                _logger.LogInformation("📝 Esportazione: {Codice} | Macchina={Macchina} | Inizio={DataInizio}", 
                    commessa.Codice, commessa.NumeroMacchina, commessa.DataInizioPrevisione?.ToString("yyyy-MM-dd HH:mm"));
            }
            
            _logger.LogInformation("💾 STEP 6: Marcatura {Updated}/{Total} commesse come esportate", aggiornate, commesseDaProgrammare.Count);
            
            // STEP 7: SaveChanges
            int rowsAffected = await _context.SaveChangesAsync();
            _logger.LogInformation("✅ SaveChanges completato: {RowsAffected} righe modificate nel DB", rowsAffected);
            
            // STEP 8: Verifica post-save
            var verificaPostSave = await _context.Commesse
                .Where(c => c.DataCambioStatoProgramma != null && c.DataCambioStatoProgramma > startTime.AddSeconds(-1))
                .ToListAsync();
            _logger.LogInformation("🔍 STEP 8: Verifica post-save: {Count} commesse con DataCambioStatoProgramma recente", 
                verificaPostSave.Count);
            
            // STEP 9: Notifica SignalR
            _logger.LogInformation("📢 STEP 9: Invio notifica SignalR...");
            await _notificationService.NotifyFullRecalculationAsync(0);
            _logger.LogInformation("✅ Notifica inviata");
            
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("✨ [EXPORT SUCCESS] Completato in {DurationMs}ms. Esportate {Aggiornate} commesse", 
                duration, aggiornate);
            
            return Ok(new 
            { 
                success = true, 
                message = $"{aggiornate} commesse esportate ({commesseDaProgrammare.Count} totali)",
                debugInfo = new
                {
                    aggiornate = aggiornate,
                    totali = commesseDaProgrammare.Count,
                    rowsAffected = rowsAffected,
                    durationMs = duration
                }
            });
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "❌ [EXPORT ERROR] Errore dopo {DurationMs}ms: {Message}", duration, ex.Message);
            _logger.LogError("Stack: {StackTrace}", ex.StackTrace);
            
            return StatusCode(500, new 
            { 
                success = false,
                message = "Errore interno del server",
                error = ex.Message,
                durationMs = duration
            });
        }
    }

    /// <summary>
    /// Helper: Carica CalendarioLavoro dal database e lo mappa a DTO
    /// </summary>
    private async Task<CalendarioLavoroDto> GetCalendarioLavoroDtoAsync()
    {
        var calendario = await _context.CalendarioLavoro.FirstOrDefaultAsync();
        
        if (calendario == null)
        {
            // Default: Lunedì-Venerdì 08:00-17:00
            return new CalendarioLavoroDto
            {
                Lunedi = true,
                Martedi = true,
                Mercoledi = true,
                Giovedi = true,
                Venerdi = true,
                Sabato = false,
                Domenica = false,
                OraInizio = new TimeOnly(8, 0),
                OraFine = new TimeOnly(17, 0)
            };
        }
        
        return new CalendarioLavoroDto
        {
            Id = calendario.Id,
            Lunedi = calendario.Lunedi,
            Martedi = calendario.Martedi,
            Mercoledi = calendario.Mercoledi,
            Giovedi = calendario.Giovedi,
            Venerdi = calendario.Venerdi,
            Sabato = calendario.Sabato,
            Domenica = calendario.Domenica,
            OraInizio = calendario.OraInizio,
            OraFine = calendario.OraFine
        };
    }

    /// <summary>
    /// Aggiorna automaticamente gli stati delle commesse in base alle date di pianificazione.
    /// - NonProgrammata → Programmata: quando assegnata a macchina
    /// - Programmata → InProduzione: quando data inizio è nel passato
    /// - InProduzione → Completata: quando data fine è nel passato
    /// </summary>
    private async Task<int> AutoCompletaCommesseAsync()
    {
        var now = DateTime.Now;
        int updateCount = 0;

        // 1. Commesse da completare: DataFinePrevisione nel passato
        var commesseDaCompletare = await _context.Commesse
            .Where(c => c.NumeroMacchina != null
                        && c.DataFinePrevisione.HasValue
                        && c.DataFinePrevisione.Value < now
                        && c.StatoProgramma != StatoProgramma.Completata
                        && c.StatoProgramma != StatoProgramma.Archiviata)
            .ToListAsync();

        foreach (var commessa in commesseDaCompletare)
        {
            commessa.StatoProgramma = StatoProgramma.Completata;
            commessa.DataCambioStatoProgramma = DateTime.Now;
            commessa.DataInizioProduzione ??= commessa.DataInizioPrevisione;
            commessa.DataFineProduzione ??= commessa.DataFinePrevisione;
            commessa.UltimaModifica = DateTime.UtcNow;
            updateCount++;
            
            _logger.LogInformation("Auto-completata commessa {Codice} (data fine prevista: {DataFine})", 
                commessa.Codice, commessa.DataFinePrevisione);
        }

        // 2. Commesse da mettere in produzione: DataInizioPrevisione nel passato, DataFinePrevisione nel futuro
        var commesseDaAvviare = await _context.Commesse
            .Where(c => c.NumeroMacchina != null
                        && c.DataInizioPrevisione.HasValue
                        && c.DataInizioPrevisione.Value <= now
                        && c.DataFinePrevisione.HasValue
                        && c.DataFinePrevisione.Value >= now
                        && c.StatoProgramma == StatoProgramma.Programmata)
            .ToListAsync();

        foreach (var commessa in commesseDaAvviare)
        {
            commessa.StatoProgramma = StatoProgramma.InProduzione;
            commessa.DataCambioStatoProgramma = DateTime.Now;
            commessa.DataInizioProduzione ??= commessa.DataInizioPrevisione;
            commessa.UltimaModifica = DateTime.UtcNow;
            updateCount++;
            
            _logger.LogInformation("Auto-avviata produzione commessa {Codice} (data inizio prevista: {DataInizio})", 
                commessa.Codice, commessa.DataInizioPrevisione);
        }

        // 3. Commesse da programmare: hanno NumeroMacchina ma sono ancora NonProgrammata
        var commesseDaProgrammare = await _context.Commesse
            .Where(c => c.NumeroMacchina != null
                        && c.StatoProgramma == StatoProgramma.NonProgrammata)
            .ToListAsync();

        foreach (var commessa in commesseDaProgrammare)
        {
            commessa.StatoProgramma = StatoProgramma.Programmata;
            commessa.DataCambioStatoProgramma = DateTime.Now;
            commessa.UltimaModifica = DateTime.UtcNow;
            updateCount++;
            
            _logger.LogInformation("Auto-programmata commessa {Codice} su macchina {NumeroMacchina}", 
                commessa.Codice, commessa.NumeroMacchina);
        }

        if (updateCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Auto-aggiornati stati di {Count} commesse", updateCount);
        }

        return updateCount;
    }
}

public class AggiornaPianificazioneRequest
{
    public DateTime? DataInizioPrevisione { get; set; }
    public int? NumeroMacchina { get; set; }
}

public class AggiornaPrioritaRequest
{
    public int Priorita { get; set; }
}

public class AggiornaBloccoRequest
{
    public bool Bloccata { get; set; }
}

public class AggiornaVincoliRequest
{
    public DateTime? VincoloDataInizio { get; set; }
    public DateTime? VincoloDataFine { get; set; }
}

public class SuggerimentoMacchinaDto
{
    public int NumeroMacchina { get; set; }
    public string NomeMacchina { get; set; } = "";
    public int CaricoPercentuale { get; set; }
    public DateTime DataFinePrevista { get; set; }
    public int PosizioneInCoda { get; set; }
}
