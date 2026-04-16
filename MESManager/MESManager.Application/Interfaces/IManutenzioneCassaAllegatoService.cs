using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IManutenzioneCassaAllegatoService
{
    Task<AllegatiManutenzioneCassaResponse> GetAllegatiBySchedaAsync(Guid schedaId);
    Task<ManutenzioneCassaAllegatoDto?> GetByIdAsync(int id);
    Task<(byte[] Content, string ContentType, string FileName)?> GetFileContentAsync(int id);
    Task<ManutenzioneCassaAllegatoDto> UploadAsync(Guid schedaId, Stream fileStream, string fileName, long fileSize, string? descrizione);
    Task<bool> DeleteAsync(int id);
}
