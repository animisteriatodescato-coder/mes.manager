namespace MESManager.Application.DTOs;

public class ImpostazioniGanttDto
{
    public Guid Id { get; set; }
    public bool AbilitaTempoAttrezzaggio { get; set; } = false;
    public int TempoAttrezzaggioMinutiDefault { get; set; } = 30;
    public int BufferInizioProduzioneMinuti { get; set; } = 15;
}
