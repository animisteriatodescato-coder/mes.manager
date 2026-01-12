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
    
    public string? Dati { get; set; }
}
