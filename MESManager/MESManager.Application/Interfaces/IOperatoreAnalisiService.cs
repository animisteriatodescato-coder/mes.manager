using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IOperatoreAnalisiService
{
    /// <summary>
    /// Analizza le performance di tutti gli operatori nel periodo indicato,
    /// con facoltativo filtro per macchina (null = tutte).
    /// </summary>
    Task<OperatoreAnalisiResult> AnalizzaAsync(
        DateTime dal,
        DateTime al,
        string? filtraMacchina = null);
}
