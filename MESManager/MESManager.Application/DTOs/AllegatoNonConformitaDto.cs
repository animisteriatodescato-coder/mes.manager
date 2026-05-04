namespace MESManager.Application.DTOs;

public class AllegatoNonConformitaDto
{
    public int Id { get; set; }
    public Guid NonConformitaId { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long DimensioneBytes { get; set; }
    public DateTime DataCaricamento { get; set; }
}
