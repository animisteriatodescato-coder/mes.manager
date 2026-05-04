using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface INonConformitaService
{
    Task<List<NonConformitaDto>> GetAllAsync();
    Task<List<NonConformitaDto>> GetByCodiceArticoloAsync(string codiceArticolo);
    Task<List<NonConformitaDto>> GetAperteAsync();
    Task<NonConformitaDto?> GetByIdAsync(Guid id);
    Task<NonConformitaDto> CreateAsync(NonConformitaDto dto, string userId);
    Task<NonConformitaDto?> UpdateAsync(NonConformitaDto dto, string userId);
    Task<bool> DeleteAsync(Guid id);
    Task<NonConformitaDto?> ChiudiAsync(Guid id, string? azioneCorrettiva, string userId);
}
