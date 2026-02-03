using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IFestiviAppService
{
    Task<List<FestivoDto>> GetListaAsync();
    Task<FestivoDto?> GetAsync(Guid id);
    Task<FestivoDto> CreaAsync(CreateFestivoRequest request);
    Task AggiornaAsync(Guid id, CreateFestivoRequest request);
    Task EliminaAsync(Guid id);
}
