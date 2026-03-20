using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IPlcAppService
{
    Task<List<PlcRealtimeDto>> GetRealtimeDataAsync();
    Task<List<PlcStoricoDto>> GetStoricoAsync(Guid macchinaId, DateTime? from, DateTime? to);
    Task<List<PlcStoricoDto>> GetAllStoricoAsync(DateTime? from, DateTime? to, int? limit = 5000);
    Task<List<EventoPLCDto>> GetEventiAsync(Guid macchinaId, DateTime? from, DateTime? to);

    /// <summary>Segmenta PLCStorico in intervalli a stato costante per il Gantt storico.</summary>
    Task<List<PlcGanttSegmentoDto>> GetGanttStoricoAsync(DateTime from, DateTime to, Guid? macchinaId = null);

    /// <summary>KPI aggregati per macchina (% Automatico, Allarme, Emergenza, Manuale, Setup) nel periodo.</summary>
    Task<List<PlcKpiStoricoDto>> GetKpiStoricoAsync(DateTime from, DateTime to, Guid? macchinaId = null);
}
