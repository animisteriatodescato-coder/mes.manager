using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IMacchinaAppService
{
    Task<List<MacchinaDto>> GetListaAsync();
    Task<MacchinaDto?> GetByIdAsync(Guid id);
    Task<MacchinaDto> CreaAsync(MacchinaDto dto);
    Task<MacchinaDto> AggiornaAsync(Guid id, MacchinaDto dto);
    Task EliminaAsync(Guid id);
}
