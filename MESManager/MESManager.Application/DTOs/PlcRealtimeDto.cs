namespace MESManager.Application.DTOs;

public class PlcRealtimeDto
{
    public Guid MacchinaId { get; set; }
    public string MacchinaNumero { get; set; } = string.Empty;
    public string MacchianaNome { get; set; } = string.Empty;
    
    /// <summary>
    /// Indirizzo IP del PLC associato alla macchina.
    /// </summary>
    public string? IndirizzoPLC { get; set; }
    
    /// <summary>
    /// Indica se la macchina è connessa (ha IP e dati recenti).
    /// </summary>
    public bool IsConnessa { get; set; } = true;
    
    public int CicliFatti { get; set; }
    public int QuantitaDaProdurre { get; set; }
    public int CicliScarti { get; set; }
    public int BarcodeLavorazione { get; set; }
    
    public int? NumeroOperatore { get; set; }
    public string? NomeOperatore { get; set; }
    
    public int TempoMedioRilevato { get; set; }
    public int TempoMedio { get; set; }
    public int Figure { get; set; }
    
    public string StatoMacchina { get; set; } = string.Empty;
    public bool QuantitaRaggiunta { get; set; }
    
    public DateTime UltimoAggiornamento { get; set; }
    
    // Percentuale completamento
    public decimal PercentualeCompletamento => 
        QuantitaDaProdurre > 0 ? (decimal)CicliFatti / QuantitaDaProdurre * 100 : 0;
}
