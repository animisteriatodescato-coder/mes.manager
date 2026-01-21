using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces
{
    public interface IAllegatiAnimaService
    {
        Task<AllegatiAnimaResponse> GetAllegatiByIdArchivioAsync(int idArchivio);
        Task<AllegatiAnimaResponse> GetAllegatiByCodiceArticoloAsync(string codiceArticolo);
        Task<AllegatoAnimaDto?> GetAllegatoByIdAsync(int id);
        Task<byte[]?> GetFileContentAsync(string path);
        Task<string?> GetFileMimeTypeAsync(string path);
        Task<AllegatoAnimaDto?> UploadAllegatoAsync(string codiceArticolo, string nomeFile, byte[] contenuto, string? descrizione, bool isFoto);
        Task<bool> DeleteAllegatoAsync(int id);
    }
}
