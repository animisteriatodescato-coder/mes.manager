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
[Authorize] // Richiede autenticazione per tutti gli endpoint
public class PianificazioneController : ControllerBase
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    private readonly ILogger<PianificazioneController> _logger;

    public PianificazioneController(
        MesManagerDbContext context,
        IPianificazioneService pianificazioneService,
        ILogger<PianificazioneController> logger)
    {
        _context = context;
        _pianificazioneService = pianificazioneService;
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

        return new CommessaGanttDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            Description = commessa.Description ?? "",
            NumeroMacchina = commessa.NumeroMacchina,
            NomeMacchina = !string.IsNullOrEmpty(commessa.NumeroMacchina) ? $"Macchina {commessa.NumeroMacchina}" : null,
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
