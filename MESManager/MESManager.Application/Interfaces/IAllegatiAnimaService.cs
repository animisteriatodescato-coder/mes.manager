using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces
{
    public interface IAllegatiAnimaService
    {
        Task<AllegatiAnimaResponse> GetAllegatiByIdArchivioAsync(int idArchivio);
        Task<AllegatoAnimaDto?> GetAllegatoByIdAsync(int id);
        Task<byte[]?> GetFileContentAsync(string path);
        Task<string?> GetFileMimeTypeAsync(string path);
    }
}
