using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface ICommessaAppService
{
    Task<List<CommessaDto>> GetListaAsync();
    Task<CommessaDto?> GetByIdAsync(Guid id);
    Task<CommessaDto> CreaAsync(CommessaDto dto);
    Task<CommessaDto> AggiornaAsync(Guid id, CommessaDto dto);
    Task AggiornaStatoAsync(Guid id, string stato);
    Task EliminaAsync(Guid id);
}
