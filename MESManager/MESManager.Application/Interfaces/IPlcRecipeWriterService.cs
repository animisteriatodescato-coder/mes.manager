using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio PLC con mapping operativo:
/// - DB55 offset 0-99: lettura stati
/// - DB55 offset 100+: scrittura ricette
/// - DB56: lettura tempi di esecuzione
/// </summary>
public interface IPlcRecipeWriterService
{
    /// <summary>
    /// Scrive parametri ricetta in DB55 offset 100+ del PLC specificato
    /// (nome metodo mantenuto per compatibilità)
    /// </summary>
    Task<RecipeWriteResult> WriteRecipeToDb56Async(Guid macchinaId, RicettaArticoloDto ricetta, CancellationToken ct = default);
    
    /// <summary>
    /// Sincronizza area ricetta DB56 -> DB55 (offset 100+)
    /// (nome metodo mantenuto per compatibilità)
    /// </summary>
    Task<RecipeWriteResult> CopyDb55ToDb56Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Legge contenuto completo DB56 (tempi/valori di esecuzione) per visualizzazione
    /// </summary>
    Task<List<PlcDbEntryDto>> ReadDb56Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Legge contenuto completo DB55 per visualizzazione
    /// </summary>
    Task<List<PlcDbEntryDto>> ReadDb55Async(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Verifica connessione PLC e restituisce status
    /// </summary>
    Task<bool> CheckPlcConnectionAsync(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Scansiona i DB disponibili su un PLC (da DB1 a maxDb)
    /// </summary>
    Task<List<PlcDbScanResultDto>> ScanAvailableDbsAsync(Guid macchinaId, int maxDb = 100, CancellationToken ct = default);
}
