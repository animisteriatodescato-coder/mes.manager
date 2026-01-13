using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IPlcAppService
{
    Task<List<PlcRealtimeDto>> GetRealtimeDataAsync();
    Task<List<PlcStoricoDto>> GetStoricoAsync(Guid macchinaId, DateTime? from, DateTime? to);
    Task<List<PlcStoricoDto>> GetAllStoricoAsync(DateTime? from, DateTime? to);
    Task<List<EventoPLCDto>> GetEventiAsync(Guid macchinaId, DateTime? from, DateTime? to);
}
