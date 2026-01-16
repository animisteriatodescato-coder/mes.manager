namespace MESManager.Domain.Entities;

public class CalendarioLavoro
{
    public Guid Id { get; set; }
    public bool Lunedi { get; set; } = true;
    public bool Martedi { get; set; } = true;
    public bool Mercoledi { get; set; } = true;
    public bool Giovedi { get; set; } = true;
    public bool Venerdi { get; set; } = true;
    public bool Sabato { get; set; } = false;
    public bool Domenica { get; set; } = false;
    
    public TimeOnly OraInizio { get; set; } = new TimeOnly(8, 0);
    public TimeOnly OraFine { get; set; } = new TimeOnly(17, 0);
    
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
    public DateTime DataModifica { get; set; } = DateTime.UtcNow;
}
