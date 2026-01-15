namespace PlcMagoSync.SYNC_MAGO.Models;

public class ArticoloMago
{
    public string Codice { get; set; }
    public string Descrizione { get; set; }
    public decimal Prezzo { get; set; }
    public bool Attivo { get; set; }
    public string UltimaModifica { get; set; }
    public bool StatoCancellato { get; set; }
    public string TimestampSync { get; set; }
}
