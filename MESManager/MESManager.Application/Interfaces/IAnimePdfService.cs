namespace MESManager.Application.Interfaces;

/// <summary>
/// Genera il PDF "Scheda Anima" per un'anima dato il suo ID.
/// Usa IAnimeService e IAllegatoArticoloService esistenti — zero duplicazione.
/// </summary>
public interface IAnimePdfService
{
    /// <summary>
    /// Genera il PDF scheda anima e restituisce lo stream pronto per il download.
    /// </summary>
    /// <param name="animeId">ID dell'anima</param>
    /// <returns>Stream del PDF, o null se l'anima non esiste</returns>
    Task<Stream?> GenerateSchedaAsync(int animeId);
}
