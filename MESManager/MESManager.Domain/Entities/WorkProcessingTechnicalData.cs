namespace MESManager.Domain.Entities;

/// <summary>
/// Dati tecnici specifici per lavorazione anime
/// Relazione 1:1 con QuoteRow (quando RowType = WorkProcessing)
/// Contiene parametri produttivi per calcolo automatico prezzo
/// </summary>
public class WorkProcessingTechnicalData
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Riferimento univoco alla riga preventivo
    /// </summary>
    public Guid QuoteRowId { get; set; }
    public QuoteRow? QuoteRow { get; set; }
    
    // ═══════════════════════════════════════════════════════════
    // PARAMETRI TECNICI ARTICOLO
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Peso articolo in kg (es. 0.34 kg)
    /// </summary>
    public decimal PesoKg { get; set; }
    
    /// <summary>
    /// Numero di figure/pezzi per anima (es. 3)
    /// </summary>
    public int Figure { get; set; } = 1;
    
    /// <summary>
    /// Dimensione lotto produttivo (es. 1500 pz)
    /// </summary>
    public int Lotto { get; set; } = 1;
    
    /// <summary>
    /// Spari orari macchina sabbia (es. 42 spari/h)
    /// </summary>
    public decimal SpariOrari { get; set; }
    
    /// <summary>
    /// Pezzi verniciabili per ora (es. 0 = nessuna verniciatura)
    /// </summary>
    public decimal VerniciaturaPezziOra { get; set; } = 0;
    
    /// <summary>
    /// Ore incollaggio necessarie (es. 0 = nessun incollaggio)
    /// </summary>
    public decimal IncollaggioOre { get; set; } = 0;
    
    /// <summary>
    /// Ore imballaggio necessarie (es. 0 = nessun imballaggio specifico)
    /// </summary>
    public decimal ImballaggioOre { get; set; } = 0;
    
    /// <summary>
    /// Peso vernice per pezzo in kg (default 0.053 kg/pz)
    /// </summary>
    public decimal VernicePesoKg { get; set; } = 0.053m;
    
    // ═══════════════════════════════════════════════════════════
    // RISULTATI CALCOLO (STORICIZZATI)
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Costo anima calcolato (€) = sabbiatura + sabbia + attrezzatura
    /// </summary>
    public decimal CostoAnimaCalcolato { get; set; }
    
    /// <summary>
    /// Costo fuori macchina (€) = vernice + verniciatura + incollaggio + imballo
    /// </summary>
    public decimal CostoFuoriMacchina { get; set; }
    
    /// <summary>
    /// Costo totale al pezzo (€) = costo anima + fuori macchina
    /// </summary>
    public decimal CostoTotalePezzo { get; set; }
    
    /// <summary>
    /// Margine applicato al momento del calcolo (%)
    /// </summary>
    public decimal MargineApplicatoPercent { get; set; }
    
    /// <summary>
    /// Prezzo vendita finale al pezzo (€) = costo * (1 + margine%)
    /// </summary>
    public decimal PrezzoVenditaPezzo { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
