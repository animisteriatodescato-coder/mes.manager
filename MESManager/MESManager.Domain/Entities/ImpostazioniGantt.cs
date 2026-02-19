namespace MESManager.Domain.Entities;

public class ImpostazioniGantt
{
    public Guid Id { get; set; }
    public bool AbilitaTempoAttrezzaggio { get; set; } = false;
    public int TempoAttrezzaggioMinutiDefault { get; set; } = 30;
    public int BufferInizioProduzioneMinuti { get; set; } = 15;
    
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
    public DateTime DataModifica { get; set; } = DateTime.UtcNow;
}
