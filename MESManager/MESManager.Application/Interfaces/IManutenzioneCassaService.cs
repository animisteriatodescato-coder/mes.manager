using MESManager.Application.DTOs;
using MESManager.Domain.Enums;

namespace MESManager.Application.Interfaces;

public interface IManutenzioneCassaService
{
    // Catalogo attività
    Task<List<ManutenzioneCassaAttivitaDto>> GetAttivitaAsync();
    Task<ManutenzioneCassaAttivitaDto> CreateAttivitaAsync(ManutenzioneCassaAttivitaDto dto);
    Task<ManutenzioneCassaAttivitaDto?> UpdateAttivitaAsync(ManutenzioneCassaAttivitaDto dto);
    Task<bool> DeleteAttivitaAsync(Guid id);

    // Schede
    Task<List<ManutenzioneCassaSchedaDto>> GetSchedeAsync(string? codiceCassa = null, DateTime? dal = null, DateTime? al = null);
    Task<ManutenzioneCassaSchedaDto?> GetSchedaByIdAsync(Guid id);
    Task<ManutenzioneCassaSchedaDto> GetOrCreateSchedaAsync(string codiceCassa, DateTime data, string? operatoreId, string? nomeOperatore);
    Task<ManutenzioneCassaSchedaDto?> ChiudiSchedaAsync(Guid id);
    Task<bool> DeleteSchedaAsync(Guid id);

    // Righe
    Task<ManutenzioneCassaSchedaDto?> UpdateRigaAsync(Guid schedaId, ManutenzioneCassaRigaDto riga);
    Task<bool> SaveNoteAsync(Guid schedaId, string? note);
    Task<bool> SaveProblematicheAsync(Guid schedaId, List<string> problematiche);

    // Lista casse disponibili (da Anime.CodiceCassa distinti)
    Task<List<string>> GetCasseDisponibiliAsync();

    // Seed dati iniziali
    Task SeedAttivitaDefaultAsync();
}
