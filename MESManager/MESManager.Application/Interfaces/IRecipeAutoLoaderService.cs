using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio event-driven per auto-caricamento ricette su DB52
/// Trigger: cambio commessa rilevato su DB55
/// </summary>
public interface IRecipeAutoLoaderService
{
    /// <summary>
    /// Handler chiamato da PlcSync quando rileva cambio barcode su DB55
    /// </summary>
    Task OnCommessaCambiataAsync(Guid macchinaId, string nuovoBarcode, CancellationToken ct = default);
    
    /// <summary>
    /// Caricamento manuale prossima ricetta (override automatismo)
    /// </summary>
    Task<RecipeWriteResult> LoadNextRecipeManualAsync(Guid macchinaId, CancellationToken ct = default);
    
    /// <summary>
    /// Update status PLC online/offline - trigger auto-recovery se torna online
    /// </summary>
    void UpdatePlcStatus(Guid macchinaId, bool isOnline);
    
    /// <summary>
    /// Ottiene info prossima ricetta in coda per macchina
    /// </summary>
    Task<(string? CodiceArticolo, bool IsLoaded)> GetNextRecipeStatusAsync(Guid macchinaId, CancellationToken ct = default);
}
