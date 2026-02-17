using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Application.Services;

/// <summary>
/// Servizio per leggere le ricette articoli dal database MESManager
/// Usa IRicettaRepository per accedere alle entità Ricetta e ParametroRicetta
/// </summary>
public class RicettaGanttService : IRicettaGanttService
{
    private readonly IRicettaRepository _ricettaRepo;
    private readonly ILogger<RicettaGanttService> _logger;

    public RicettaGanttService(
        IRicettaRepository ricettaRepo,
        ILogger<RicettaGanttService> logger)
    {
        _ricettaRepo = ricettaRepo;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene tutti i parametri della ricetta per un articolo specifico
    /// </summary>
    public async Task<RicettaArticoloDto?> GetRicettaByCodiceArticoloAsync(string codiceArticolo)
    {
        if (string.IsNullOrWhiteSpace(codiceArticolo))
            return null;

        _logger.LogInformation("GetRicettaByCodiceArticoloAsync: {CodiceArticolo}", codiceArticolo);

        try
        {
            var articolo = await _ricettaRepo.GetArticoloConRicettaByCodeAsync(codiceArticolo);

            if (articolo?.Ricetta == null)
            {
                _logger.LogWarning("Nessuna ricetta trovata per articolo {CodiceArticolo}", codiceArticolo);
                return null;
            }

            var parametriOrdinati = articolo.Ricetta.Parametri
                .OrderBy(p => p.Indirizzo ?? 9999)
                .ThenBy(p => p.NomeParametro)
                .ToList();
            
            var parametri = parametriOrdinati
                .Select((p, index) => new ParametroRicettaArticoloDto
                {
                    IdRigaRicetta = 0,
                    CodiceArticolo = codiceArticolo,
                    CodiceParametro = p.CodiceParametro ?? (index + 1), // Auto-genera se manca
                    DescrizioneParametro = p.NomeParametro,
                    Indirizzo = p.Indirizzo ?? 0,
                    Area = p.Area ?? string.Empty,
                    Tipo = p.Tipo ?? string.Empty,
                    UM = p.UnitaMisura,
                    Valore = int.TryParse(p.Valore, out var val) ? val : 0
                }).ToList();

            return new RicettaArticoloDto
            {
                CodiceArticolo = codiceArticolo,
                DescrizioneArticolo = articolo.Descrizione,
                Parametri = parametri
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura ricetta per {CodiceArticolo}", codiceArticolo);
            throw;
        }
    }

    /// <summary>
    /// Cerca ricette per codice articolo (ricerca parziale)
    /// </summary>
    public async Task<RicetteSearchResponse> SearchRicetteAsync(string? searchTerm, int maxResults = 50)
    {
        _logger.LogInformation("SearchRicetteAsync: term={SearchTerm}, max={MaxResults}", searchTerm, maxResults);

        var response = new RicetteSearchResponse();

        try
        {
            var articoli = await _ricettaRepo.GetArticoliConRicettaAsync(searchTerm, maxResults);

            foreach (var articolo in articoli)
            {
                if (articolo.Ricetta != null)
                {
                    var parametriOrdinati = articolo.Ricetta.Parametri
                        .OrderBy(p => p.Indirizzo ?? 9999)
                        .ThenBy(p => p.NomeParametro)
                        .ToList();
                    
                    var ricettaDto = new RicettaArticoloDto
                    {
                        CodiceArticolo = articolo.Codice,
                        DescrizioneArticolo = articolo.Descrizione,
                        Parametri = parametriOrdinati
                            .Select((p, index) => new ParametroRicettaArticoloDto
                            {
                                CodiceArticolo = articolo.Codice,
                                CodiceParametro = p.CodiceParametro ?? (index + 1), // Auto-genera se manca
                                DescrizioneParametro = p.NomeParametro,
                                Indirizzo = p.Indirizzo ?? 0,
                                Area = p.Area ?? string.Empty,
                                Tipo = p.Tipo ?? string.Empty,
                                UM = p.UnitaMisura,
                                Valore = int.TryParse(p.Valore, out var val) ? val : 0
                            }).ToList()
                    };

                    response.Ricette.Add(ricettaDto);
                    response.TotaleParametri += ricettaDto.TotaleParametri;
                }
            }

            response.TotaleRicette = response.Ricette.Count;

            _logger.LogInformation("SearchRicetteAsync: trovate {Count} ricette con {Params} parametri totali",
                response.TotaleRicette, response.TotaleParametri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante ricerca ricette");
            throw;
        }

        return response;
    }

    /// <summary>
    /// Ottiene la lista di tutti i codici articolo che hanno ricette
    /// </summary>
    public async Task<List<string>> GetCodiciArticoloConRicetteAsync()
    {
        try
        {
            var articoli = await _ricettaRepo.GetArticoliConRicettaAsync(null, int.MaxValue);
            var codici = articoli.Select(a => a.Codice).ToList();

            _logger.LogInformation("GetCodiciArticoloConRicetteAsync: trovati {Count} articoli con ricette", codici.Count);
            return codici;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura codici articolo");
            throw;
        }
    }

    /// <summary>
    /// Conta il totale delle ricette (articoli distinti con parametri)
    /// </summary>
    public async Task<int> CountRicetteAsync()
    {
        try
        {
            return await _ricettaRepo.CountArticoliConRicettaAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante conteggio ricette");
            return 0;
        }
    }
    
    /// <summary>
    /// Ottiene lista articoli che hanno parametri ricetta configurati
    /// </summary>
    public async Task<List<ArticoloConRicettaDto>> GetArticoliConRicettaAsync()
    {
        try
        {
            var articoli = await _ricettaRepo.GetArticoliConRicettaAsync(null, int.MaxValue);
            
            var result = articoli.Select(a => new ArticoloConRicettaDto
            {
                CodiceArticolo = a.Codice,
                NumeroParametri = a.Ricetta?.Parametri.Count ?? 0
            }).ToList();
            
            _logger.LogInformation("GetArticoliConRicettaAsync: trovati {Count} articoli", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura articoli con ricetta");
            return new List<ArticoloConRicettaDto>();
        }
    }
}
