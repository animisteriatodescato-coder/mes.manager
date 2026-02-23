using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per gestione tipi lavorazione anime e configurazione parametri economici
/// </summary>
public interface IWorkProcessingService
{
    // ═══════════════════════════════════════════════════════════
    // TIPI LAVORAZIONE
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Ottiene lista completa tipi lavorazione (solo attivi se onlyActive = true)
    /// </summary>
    Task<List<WorkProcessingTypeDto>> GetAllTypesAsync(bool onlyActive = true);
    
    /// <summary>
    /// Ottiene dettaglio tipo lavorazione con parametri correnti
    /// </summary>
    Task<WorkProcessingTypeDto?> GetTypeByIdAsync(Guid id);
    
    /// <summary>
    /// Ottiene tipo lavorazione per codice
    /// </summary>
    Task<WorkProcessingTypeDto?> GetTypeByCodeAsync(string codice);
    
    /// <summary>
    /// Crea nuovo tipo lavorazione
    /// </summary>
    Task<WorkProcessingTypeDto> CreateTypeAsync(WorkProcessingTypeSaveDto dto, string? userId = null);
    
    /// <summary>
    /// Aggiorna tipo lavorazione esistente
    /// </summary>
    Task<WorkProcessingTypeDto> UpdateTypeAsync(WorkProcessingTypeSaveDto dto, string? userId = null);
    
    /// <summary>
    /// Archivia tipo lavorazione (soft delete - storicità)
    /// </summary>
    Task<bool> ArchiveTypeAsync(Guid id, string? userId = null);
    
    // ═══════════════════════════════════════════════════════════
    // PARAMETRI ECONOMICI
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Ottiene parametri correnti per tipo lavorazione
    /// </summary>
    Task<WorkProcessingParameterDto?> GetCurrentParametersAsync(Guid workProcessingTypeId);
    
    /// <summary>
    /// Ottiene storico parametri per tipo lavorazione
    /// </summary>
    Task<List<WorkProcessingParameterDto>> GetParametersHistoryAsync(Guid workProcessingTypeId);
    
    /// <summary>
    /// Crea/Aggiorna parametri economici (supporta versioning)
    /// </summary>
    Task<WorkProcessingParameterDto> SaveParametersAsync(WorkProcessingParameterSaveDto dto, string? userId = null);
    
    // ═══════════════════════════════════════════════════════════
    // PRICING ENGINE - CALCOLO AUTOMATICO
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Calcola prezzo vendita per lavorazione anime con breakdown dettagliato
    /// </summary>
    Task<WorkProcessingCalculationResult> CalculatePriceAsync(WorkProcessingCalculationInput input);
    
    /// <summary>
    /// Valida input calcolo (controllo dati tecnici coerenti)
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateCalculationInputAsync(WorkProcessingCalculationInput input);
}
