using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface ICommessaAppService
{
    Task<List<CommessaDto>> GetListaAsync();
    Task<CommessaDto?> GetByIdAsync(Guid id);
    Task<CommessaDto> CreaAsync(CommessaDto dto);
    Task<CommessaDto> AggiornaAsync(Guid id, CommessaDto dto);
    Task AggiornaStatoAsync(Guid id, string stato);
    Task AggiornaStatoProgrammaAsync(Guid id, string statoProgramma, string? note = null, string? utente = null);
    Task AggiornaNumeroMacchinaAsync(Guid id, int? numeroMacchina);
    Task RiordinaCommessaAsync(Guid commessaId, int? nuovoNumeroMacchina, int nuovaPosizioneIndex);
    Task EliminaAsync(Guid id);
    Task<List<StoricoProgrammazioneDto>> GetStoricoProgrammazioneAsync(Guid commessaId);
}
