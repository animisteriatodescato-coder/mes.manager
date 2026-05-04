using System.ComponentModel.DataAnnotations;

namespace MESManager.Domain.Entities;

/// <summary>
/// Allegato (file) associato a una Non Conformità. I dati binari sono salvati nel DB.
/// </summary>
public class AllegatoNonConformita
{
    public int Id { get; set; }

    public Guid NonConformitaId { get; set; }
    public NonConformita? NonConformita { get; set; }

    [Required]
    [MaxLength(255)]
    public string NomeFile { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] Dati { get; set; } = Array.Empty<byte>();

    public long DimensioneBytes { get; set; }

    public DateTime DataCaricamento { get; set; } = DateTime.Now;
}
