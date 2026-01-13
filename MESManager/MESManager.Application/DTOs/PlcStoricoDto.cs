namespace MESManager.Application.DTOs;

public class PlcStoricoDto
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string MacchinaNumero { get; set; } = string.Empty;
    public string MacchianaNome { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    public string StatoMacchina { get; set; } = string.Empty;
    
    public int? NumeroOperatore { get; set; }
    public string? NomeOperatore { get; set; }
    
    public int BarcodeLavorazione { get; set; }
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }

    // Event timestamps (string form from snapshot)
    public string? NuovaProduzioneTs { get; set; }
    public string? InizioSetupTs { get; set; }
    public string? FineSetupTs { get; set; }
    public string? InProduzioneTs { get; set; }

    // Raw JSON for audit (optional)
    public string? Dati { get; set; }
}
