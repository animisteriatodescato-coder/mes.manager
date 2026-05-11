using MESManager.Domain.Constants;

namespace MESManager.Domain.Entities;

public class ImpostazioniProduzione
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tempo setup predefinito in minuti (default: 90)
    /// </summary>
    public int TempoSetupMinuti { get; set; } = ImpostazioniProduzioneDefaults.TempoSetupMinuti;
    
    /// <summary>
    /// Ore lavorative giornaliere (default: 8)
    /// </summary>
    public int OreLavorativeGiornaliere { get; set; } = ImpostazioniProduzioneDefaults.OreLavorativeGiornaliere;
    
    /// <summary>
    /// Giorni lavorativi settimanali (default: 5 - Lunedì-Venerdì)
    /// </summary>
    public int GiorniLavorativiSettimanali { get; set; } = ImpostazioniProduzioneDefaults.GiorniLavorativiSettimanali;
    
    public DateTime UltimaModifica { get; set; }
}
