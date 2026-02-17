using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces;

public interface IRicettaRepository
{
    Task<List<Articolo>> GetArticoliConRicettaAsync(string? searchTerm = null, int maxResults = 50);
    Task<Articolo?> GetArticoloConRicettaByCodeAsync(string codiceArticolo);
    Task<int> CountArticoliConRicettaAsync();
}
