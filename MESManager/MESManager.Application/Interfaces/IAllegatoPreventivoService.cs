using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IAllegatoPreventivoService
{
    Task<List<AllegatoPreventivoDto>> GetByPreventivoIdAsync(Guid preventivoId);
    Task<AllegatoPreventivoDto> AddAsync(Guid preventivoId, string nomeFile, string contentType, byte[] dati);
    Task<bool> DeleteAsync(int id);
    /// <summary>Ritorna i raw bytes + contentType + nomeFile per il download.</summary>
    Task<(byte[] Dati, string ContentType, string NomeFile)?> GetFileAsync(int id);
}
