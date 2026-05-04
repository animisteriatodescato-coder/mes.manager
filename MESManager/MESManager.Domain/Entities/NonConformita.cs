using System.ComponentModel.DataAnnotations;

namespace MESManager.Domain.Entities;

/// <summary>
/// Non conformità / segnalazione ricevuta da un cliente su un articolo.
/// Aggiunta in v1.65.68 — modulo NC produzione.
/// </summary>
public class NonConformita
{
    public Guid Id { get; set; }

    /// <summary>Codice articolo dal CatalogoAnime (campo libero per articoli non ancora catalogati).</summary>
    [Required]
    [MaxLength(100)]
    public string CodiceArticolo { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? DescrizioneArticolo { get; set; }

    /// <summary>Ragione sociale / nome cliente che ha segnalato.</summary>
    [MaxLength(200)]
    public string? Cliente { get; set; }

    public DateTime DataSegnalazione { get; set; } = DateTime.Today;

    /// <summary>NonConformita | Segnalazione</summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "NonConformita";

    /// <summary>Bassa | Media | Alta | Critica</summary>
    [MaxLength(50)]
    public string Gravita { get; set; } = "Media";

    [Required]
    public string Descrizione { get; set; } = string.Empty;

    public string? AzioneCorrettiva { get; set; }

    /// <summary>Aperta | InGestione | Chiusa</summary>
    [MaxLength(50)]
    public string Stato { get; set; } = "Aperta";

    [MaxLength(200)]
    public string? CreatoDa { get; set; }

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? ModificatoDa { get; set; }

    public DateTime? ModificatoIl { get; set; }

    public DateTime? DataChiusura { get; set; }
}
