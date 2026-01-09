using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IRicettaAppService
{
    Task<List<RicettaDto>> GetListaAsync();
    Task<RicettaDto?> GetByIdAsync(Guid id);
    Task<RicettaDto> CreaAsync(RicettaDto dto);
    Task<RicettaDto> AggiornaAsync(Guid id, RicettaDto dto);
    Task EliminaAsync(Guid id);
}
