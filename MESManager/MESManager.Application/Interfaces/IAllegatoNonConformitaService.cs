using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IAllegatoNonConformitaService
{
    Task<List<AllegatoNonConformitaDto>> GetByNcIdAsync(Guid nonConformitaId);
    Task<AllegatoNonConformitaDto> AddAsync(Guid nonConformitaId, string nomeFile, string contentType, byte[] dati);
    Task<bool> DeleteAsync(int id);
    Task<(byte[] Dati, string ContentType, string NomeFile)?> GetFileAsync(int id);
}
