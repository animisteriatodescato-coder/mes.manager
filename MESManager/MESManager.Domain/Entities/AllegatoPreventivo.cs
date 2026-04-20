using System.ComponentModel.DataAnnotations;

namespace MESManager.Domain.Entities;

/// <summary>
/// Allegato (file) associato a un preventivo. I dati sono salvati nel DB.
/// </summary>
public class AllegatoPreventivo
{
    public int Id { get; set; }

    public Guid PreventivoId { get; set; }
    public Preventivo? Preventivo { get; set; }

    [Required]
    [MaxLength(255)]
    public string NomeFile { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] Dati { get; set; } = Array.Empty<byte>();

    public long DimensioneBytes { get; set; }

    public DateTime DataCaricamento { get; set; } = DateTime.Now;
}
