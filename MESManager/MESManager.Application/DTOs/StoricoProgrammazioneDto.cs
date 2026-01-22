namespace MESManager.Application.DTOs;

public class StoricoProgrammazioneDto
{
    public Guid Id { get; set; }
    public Guid CommessaId { get; set; }
    public string? NumeroCommessa { get; set; }
    public string StatoPrecedente { get; set; } = string.Empty;
    public string StatoNuovo { get; set; } = string.Empty;
    public DateTime DataModifica { get; set; }
    public string? UtenteModifica { get; set; }
    public string? Note { get; set; }
}
