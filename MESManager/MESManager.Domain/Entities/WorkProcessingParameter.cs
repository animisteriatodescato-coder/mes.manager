namespace MESManager.Domain.Entities;

/// <summary>
/// Parametri economici/produttivi per tipo lavorazione anime
/// Supporta storico versioni (ValidFrom/ValidTo) per tracciabilità prezzi
/// </summary>
public class WorkProcessingParameter
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento al tipo lavorazione
    /// </summary>
    public Guid WorkProcessingTypeId { get; set; }
    public WorkProcessingType? WorkProcessingType { get; set; }
    
    // ═══════════════════════════════════════════════════════════
    // PARAMETRI ECONOMICI (€)
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Costo orario lavorazione (€/ora) - es. 70 €/h per Distributori
    /// </summary>
    public decimal EuroOra { get; set; }
    
    /// <summary>
    /// Costo sabbia al kg (€/kg) - es. 1.30 €/kg per Distributori
    /// </summary>
    public decimal SabbiaCostoKg { get; set; }
    
    /// <summary>
    /// Costo attrezzatura fisso (€) - ammortizzato sul lotto
    /// </summary>
    public decimal CostoAttrezzatura { get; set; }
    
    /// <summary>
    /// Costo vernice per pezzo (€/pz) - default 3 €
    /// </summary>
    public decimal VerniceCostoPezzo { get; set; }
    
    /// <summary>
    /// Costo orario verniciatura (€/ora) - es. 40 €/h
    /// </summary>
    public decimal VerniciaturaCostoOra { get; set; }
    
    /// <summary>
    /// Costo orario incollaggio (€/ora) - es. 40 €/h
    /// </summary>
    public decimal IncollaggioCostoOra { get; set; }
    
    /// <summary>
    /// Costo orario imballaggio (€/ora) - es. 40 €/h
    /// </summary>
    public decimal ImballaggioOra { get; set; }
    
    /// <summary>
    /// Margine di guadagno predefinito (%) - es. 30%
    /// </summary>
    public decimal MargineDefaultPercent { get; set; } = 30m;
    
    // ═══════════════════════════════════════════════════════════
    // STORICITÀ & VALIDITÀ
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Data inizio validità parametri (per versioning prezzi)
    /// </summary>
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data fine validità (null = attualmente valido)
    /// </summary>
    public DateTime? ValidTo { get; set; }
    
    /// <summary>
    /// Flag versione corrente attiva
    /// </summary>
    public bool IsCurrent { get; set; } = true;
    
    /// <summary>
    /// Note sulla versione (es. "Rincaro sabbia Gennaio 2026")
    /// </summary>
    public string? VersionNotes { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
