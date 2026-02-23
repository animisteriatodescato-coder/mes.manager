namespace MESManager.Domain.Entities;

/// <summary>
/// Tipologia di lavorazione anime/sabbia (Distributori, Cerabeads, Sabbia Ghisa, etc.)
/// Master data - configurazione tipo processo produttivo
/// </summary>
public class WorkProcessingType
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Nome tipo lavorazione (es. "Distributori 33BD600X", "Cerabeads", "Sabbia Ghisa Normale")
    /// </summary>
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Codice univoco identificativo (es. "DISTRIBUT", "CERABEADS", "SABBIA_NORM")
    /// </summary>
    public string Codice { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrizione estesa del processo
    /// </summary>
    public string? Descrizione { get; set; }
    
    /// <summary>
    /// Categoria (per raggruppamento UI - es. "Sabbiatura", "Finiture", etc.)
    /// </summary>
    public string? Categoria { get; set; }
    
    /// <summary>
    /// Ordine di visualizzazione nelle liste
    /// </summary>
    public int Ordinamento { get; set; }
    
    /// <summary>
    /// Flag attivazione (false = nascosto da selezione)
    /// </summary>
    public bool Attivo { get; set; } = true;
    
    /// <summary>
    /// Flag archiviato (storicità - non eliminare mai fisicamente)
    /// </summary>
    public bool Archiviato { get; set; } = false;
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Navigazioni
    public ICollection<WorkProcessingParameter> Parametri { get; set; } = new List<WorkProcessingParameter>();
    public ICollection<QuoteRow> RighePreventivo { get; set; } = new List<QuoteRow>();
}
