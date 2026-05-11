using MESManager.Domain.Constants;

namespace MESManager.Application.DTOs;

public class ImpostazioniProduzioneDto
{
    public Guid Id { get; set; }
    public int TempoSetupMinuti { get; set; } = ImpostazioniProduzioneDefaults.TempoSetupMinuti;
    public int OreLavorativeGiornaliere { get; set; } = ImpostazioniProduzioneDefaults.OreLavorativeGiornaliere;
    public int GiorniLavorativiSettimanali { get; set; } = ImpostazioniProduzioneDefaults.GiorniLavorativiSettimanali;
    public DateTime UltimaModifica { get; set; }
}
