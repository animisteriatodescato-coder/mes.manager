namespace MESManager.PlcSync.Models;

public class PlcSnapshot
{
    public DateTime Timestamp { get; set; }
    
    // Produzione
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int BarcodeLavorazione { get; set; }
    
    // Operatore
    public int NumeroOperatore { get; set; }
    
    // Tempi
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }
    
    // Stati
    public string StatoMacchina { get; set; } = string.Empty;
    public bool QuantitaRaggiunta { get; set; }
    
    // Eventi (timestamp quando sono passati da 0 a 1)
    public string NuovaProduzioneTs { get; set; } = string.Empty;
    public string InizioSetupTs { get; set; } = string.Empty;
    public string FineSetupTs { get; set; } = string.Empty;
    public string InProduzioneTs { get; set; } = string.Empty;
    
    // Flags eventi attuali
    public bool NuovaProduzione { get; set; }
    public bool InizioSetup { get; set; }
    public bool FineSetup { get; set; }
    public bool InProduzione { get; set; }
}
