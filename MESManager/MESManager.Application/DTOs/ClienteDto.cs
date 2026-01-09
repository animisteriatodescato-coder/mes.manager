namespace MESManager.Application.DTOs;

public class ClienteDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string RagioneSociale { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Attivo { get; set; }
    public DateTime UltimaModifica { get; set; }
    public DateTime TimestampSync { get; set; }
}
