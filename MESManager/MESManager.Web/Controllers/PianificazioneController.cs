using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;

namespace MESManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Temporaneamente disabilitato per sviluppo - riabilitare in produzione
public class PianificazioneController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    private readonly IPianificazioneEngineService _engineService;
    private readonly ILogger<PianificazioneController> _logger;

    public PianificazioneController(
        MesManagerDbContext context,
        IPianificazioneService pianificazioneService,
        IPianificazioneEngineService engineService,
        ILogger<PianificazioneController> logger)
    {
        _context = context;
        _pianificazioneService = pianificazioneService;
        _engineService = engineService;
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
            // Ottieni le impostazioni di produzione (o usa valori di default)
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            var commesse = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina != null) // Solo commesse assegnate
                .OrderBy(c => c.NumeroMacchina)
                .ThenBy(c => c.OrdineSequenza)
                .ToListAsync();

            var commesseGantt = commesse.Select(c => MapToGanttDto(c, impostazioni)).ToList();

            return Ok(commesseGantt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero delle commesse per il Gantt");
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

                commessa.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevista(
                    request.DataInizioPrevisione.Value,
                    durataMinuti,
                    impostazioni.OreLavorativeGiornaliere,
                    impostazioni.GiorniLavorativiSettimanali
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
    [HttpPost("sposta")]
    public async Task<ActionResult<SpostaCommessaResponse>> SpostaCommessa([FromBody] SpostaCommessaRequest request)
    {
        try
        {
            var result = await _engineService.SpostaCommessaAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nello spostamento della commessa {CommessaId}", request.CommessaId);
            return StatusCode(500, new SpostaCommessaResponse 
            { 
                Success = false, 
                ErrorMessage = "Errore interno del server" 
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

    private CommessaGanttDto MapToGanttDto(Commessa commessa, ImpostazioniProduzione impostazioni)
    {
        var tempoCiclo = commessa.Articolo?.TempoCiclo ?? 0;
        var numeroFigure = commessa.Articolo?.NumeroFigure ?? 0;
        
        var durataMinuti = _pianificazioneService.CalcolaDurataPrevistaMinuti(
            tempoCiclo,
            numeroFigure,
            commessa.QuantitaRichiesta,
            impostazioni.TempoSetupMinuti
        );

        // Calcola DataFinePrevisione se mancante ma presente DataInizioPrevisione
        DateTime? dataFinePrevisione = commessa.DataFinePrevisione;
        if (commessa.DataInizioPrevisione.HasValue && !dataFinePrevisione.HasValue && durataMinuti > 0)
        {
            dataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevista(
                commessa.DataInizioPrevisione.Value,
                durataMinuti,
                impostazioni.OreLavorativeGiornaliere,
                impostazioni.GiorniLavorativiSettimanali
            );
        }

        // Parse NumeroMacchina to int
        int? numeroMacchinaInt = null;
        if (!string.IsNullOrEmpty(commessa.NumeroMacchina) && int.TryParse(commessa.NumeroMacchina, out var num))
        {
            numeroMacchinaInt = num;
        }

        return new CommessaGanttDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            Description = commessa.Description ?? "",
            NumeroMacchina = numeroMacchinaInt,
            NomeMacchina = !string.IsNullOrEmpty(commessa.NumeroMacchina) ? $"Macchina {commessa.NumeroMacchina}" : null,
            OrdineSequenza = commessa.OrdineSequenza,
            DataInizioPrevisione = commessa.DataInizioPrevisione,
            DataFinePrevisione = dataFinePrevisione,
            DataInizioProduzione = commessa.DataInizioProduzione,
            DataFineProduzione = commessa.DataFineProduzione,
            QuantitaRichiesta = commessa.QuantitaRichiesta,
            UoM = commessa.UoM,
            DataConsegna = commessa.DataConsegna,
            TempoCicloSecondi = tempoCiclo,
            NumeroFigure = numeroFigure,
            TempoSetupMinuti = impostazioni.TempoSetupMinuti,
            DurataPrevistaMinuti = durataMinuti,
            Stato = commessa.Stato.ToString(),
            ColoreStato = _pianificazioneService.GetColoreStato(commessa.Stato.ToString()),
            PercentualeCompletamento = CalcolaPercentualeCompletamento(commessa)
        };
    }

    private decimal CalcolaPercentualeCompletamento(Commessa commessa)
    {
        if (commessa.Stato == StatoCommessa.Completata)
            return 100;

        if (commessa.DataInizioProduzione == null || commessa.DataFinePrevisione == null)
            return 0;

        var now = DateTime.Now;
        if (now < commessa.DataInizioProduzione)
            return 0;

        if (now >= commessa.DataFinePrevisione)
            return 100;

        var totalDuration = (commessa.DataFinePrevisione.Value - commessa.DataInizioProduzione.Value).TotalMinutes;
        var elapsed = (now - commessa.DataInizioProduzione.Value).TotalMinutes;

        return (decimal)(elapsed / totalDuration * 100);
    }
}

public class AggiornaPianificazioneRequest
{
    public DateTime? DataInizioPrevisione { get; set; }
    public string? NumeroMacchina { get; set; }
}
