using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia servizio ricette articoli (tabella ArticoliRicetta)
/// </summary>
public interface IRicettaGanttService
{
    /// <summary>
    /// Ottiene la ricetta completa per un articolo
    /// </summary>
    Task<RicettaArticoloDto?> GetRicettaByCodiceArticoloAsync(string codiceArticolo);
    
    /// <summary>
    /// Ottiene lista articoli che hanno parametri ricetta configurati
    /// </summary>
    Task<List<ArticoloConRicettaDto>> GetArticoliConRicettaAsync();

    /// <summary>
    /// Aggiorna il valore di un singolo parametro ricetta
    /// </summary>
    Task<bool> UpdateValoreParametroAsync(Guid parametroId, int nuovoValore);
}

/// <summary>
/// DTO semplificato per lista articoli con ricetta
/// </summary>
public class ArticoloConRicettaDto
{
    public string CodiceArticolo { get; set; } = string.Empty;
    public int NumeroParametri { get; set; }
}
