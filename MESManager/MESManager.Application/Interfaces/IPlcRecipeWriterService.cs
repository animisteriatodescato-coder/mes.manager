using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per scrittura ricette su DB52 PLC tramite Sharp7
/// </summary>
public interface IPlcRecipeWriterService
{
    /// <summary>
    /// Scrive parametri ricetta su DB52 del PLC specificato
    /// </summary>
    Task<RecipeWriteResult> WriteRecipeToDb52Async(Guid macchinaId, RicettaArticoloDto ricetta, CancellationToken ct = default);
    
    /// <summary>
    /// Copia parametri da DB55 (corrente) a DB52 (prossima)
    /// Usato da "Carica Ricetta da DB55" nel popup
    /// </summary>
    Task<RecipeWriteResult> CopyDb55ToDb52Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Legge contenuto completo DB52 per visualizzazione
    /// </summary>
    Task<List<PlcDbEntryDto>> ReadDb52Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Legge contenuto completo DB55 per visualizzazione
    /// </summary>
    Task<List<PlcDbEntryDto>> ReadDb55Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Verifica connessione PLC e restituisce status
    /// </summary>
    Task<bool> CheckPlcConnectionAsync(Guid macchinaId, CancellationToken ct = default);
}
