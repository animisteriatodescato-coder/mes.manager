namespace MESManager.Application.DTOs;

public class AllegatoPreventivoDto
{
    public int Id { get; set; }
    public Guid PreventivoId { get; set; }
    public string NomeFile { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long DimensioneBytes { get; set; }
    public DateTime DataCaricamento { get; set; }
}
