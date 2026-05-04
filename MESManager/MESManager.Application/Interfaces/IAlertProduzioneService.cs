using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio centralizzato S4: aggrega alert di produzione da più fonti (NC, future: Manutenzioni).
/// Utilizzato da PlcAppService per arricchire PlcRealtimeDto in un'unica batch query.
/// </summary>
public interface IAlertProduzioneService
{
    /// <summary>
    /// Ritorna tutti gli alert attivi (NC aperte) per i codici articolo forniti.
    /// Batch-safe: una sola query SQL per tutti i codici.
    /// </summary>
    Task<Dictionary<string, List<AlertProduzioneDto>>> GetAlertPerArticoliBatchAsync(IEnumerable<string> codiciArticolo);
}
