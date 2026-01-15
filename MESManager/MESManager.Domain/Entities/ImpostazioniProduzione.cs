namespace MESManager.Domain.Entities;

public class ImpostazioniProduzione
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tempo setup predefinito in minuti (default: 90)
    /// </summary>
    public int TempoSetupMinuti { get; set; } = 90;
    
    /// <summary>
    /// Ore lavorative giornaliere (default: 8)
    /// </summary>
    public int OreLavorativeGiornaliere { get; set; } = 8;
    
    /// <summary>
    /// Giorni lavorativi settimanali (default: 5 - Lunedì-Venerdì)
    /// </summary>
    public int GiorniLavorativiSettimanali { get; set; } = 5;
    
    public DateTime UltimaModifica { get; set; }
}
