using MESManager.Application.DTOs;
using MESManager.Domain.Enums;

namespace MESManager.Application.Interfaces;

public interface IManutenzioneService
{
    // Catalogo attività
    Task<List<ManutenzioneAttivitaDto>> GetAttivitaAsync(TipoFrequenzaManutenzione? tipo = null);
    Task<ManutenzioneAttivitaDto> CreateAttivitaAsync(ManutenzioneAttivitaDto dto);
    Task<ManutenzioneAttivitaDto?> UpdateAttivitaAsync(ManutenzioneAttivitaDto dto);
    Task<bool> DeleteAttivitaAsync(Guid id);

    // Schede
    Task<List<ManutenzioneSchedaDto>> GetSchedeAsync(Guid? macchinaId = null, TipoFrequenzaManutenzione? tipo = null, DateTime? dal = null, DateTime? al = null);
    Task<ManutenzioneSchedaDto?> GetSchedaByIdAsync(Guid id);
    Task<ManutenzioneSchedaDto> CreateSchedaAsync(NuovaSchedaRequest request);
    Task<ManutenzioneSchedaDto?> ChiudiSchedaAsync(Guid id);

    // Righe
    Task<ManutenzioneSchedaDto?> UpdateRigaAsync(Guid schedaId, ManutenzioneRigaDto riga);
    Task<string?> UploadFotoRigaAsync(Guid rigaId, Stream fileStream, string fileName);
    Task<bool> DeleteFotoRigaAsync(Guid rigaId);

    // Seed dati iniziali
    Task SeedAttivitaDefaultAsync();
}
