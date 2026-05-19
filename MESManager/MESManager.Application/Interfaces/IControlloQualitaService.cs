using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IControlloQualitaService
{
    Task<List<ControlloQualitaAttivitaDto>> GetAttivitaAsync();
    Task<List<ControlloQualitaSchedaDto>> GetSchedeAsync(Guid? macchinaId = null, DateTime? dal = null, DateTime? al = null);
    Task<ControlloQualitaSchedaDto?> GetSchedaByIdAsync(Guid id);
    Task<ControlloQualitaSchedaDto> CreateSchedaAsync(NuovaSchedaControlloQualitaRequest request);
    Task<ControlloQualitaSchedaDto> GetOrCreateSchedaAsync(Guid macchinaId, DateTime data, string? operatoreId, string? nomeOperatore);
    Task<ControlloQualitaSchedaDto?> UpdateRigaAsync(Guid schedaId, ControlloQualitaRigaDto riga);
    Task<ControlloQualitaSchedaDto?> ChiudiSchedaAsync(Guid id);
    Task SeedAttivitaDefaultAsync();
}
