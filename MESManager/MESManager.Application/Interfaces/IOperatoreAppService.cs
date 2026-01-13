using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IOperatoreAppService
{
    Task<List<OperatoreDto>> GetAllAsync();
    Task<OperatoreDto?> GetByIdAsync(Guid id);
    Task<OperatoreDto> CreateAsync(OperatoreDto dto);
    Task<OperatoreDto> UpdateAsync(OperatoreDto dto);
    Task DeleteAsync(Guid id);
}
