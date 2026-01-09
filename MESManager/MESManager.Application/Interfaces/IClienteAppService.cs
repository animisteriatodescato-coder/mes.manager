using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IClienteAppService
{
    Task<List<ClienteDto>> GetListaAsync();
    Task<ClienteDto?> GetByIdAsync(Guid id);
    Task<ClienteDto> CreaAsync(ClienteDto dto);
    Task<ClienteDto> AggiornaAsync(Guid id, ClienteDto dto);
    Task EliminaAsync(Guid id);
}
