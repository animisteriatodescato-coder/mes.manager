namespace MESManager.Application.DTOs;

public class EventoPLCDto
{
    public Guid Id { get; set; }
    public Guid MacchinaId { get; set; }
    public string MacchinaNumero { get; set; } = string.Empty;
    public string MacchianNome { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? Dettagli { get; set; }
}
