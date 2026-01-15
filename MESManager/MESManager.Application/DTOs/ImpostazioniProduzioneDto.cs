namespace MESManager.Application.DTOs;

public class ImpostazioniProduzioneDto
{
    public Guid Id { get; set; }
    public int TempoSetupMinuti { get; set; } = 90;
    public int OreLavorativeGiornaliere { get; set; } = 8;
    public int GiorniLavorativiSettimanali { get; set; } = 5;
    public DateTime UltimaModifica { get; set; }
}
