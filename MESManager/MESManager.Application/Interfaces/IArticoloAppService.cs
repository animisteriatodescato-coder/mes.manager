using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IArticoloAppService
{
    Task<List<ArticoloDto>> GetListaAsync();
    Task<ArticoloDto?> GetByIdAsync(Guid id);
    Task<ArticoloDto> CreaAsync(ArticoloDto dto);
    Task<ArticoloDto> AggiornaAsync(Guid id, ArticoloDto dto);
    Task EliminaAsync(Guid id);
}
