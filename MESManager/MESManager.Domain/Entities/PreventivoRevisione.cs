namespace MESManager.Domain.Entities;

/// <summary>
/// Snapshot storico di un preventivo ad ogni salvataggio (v1.65.7).
/// </summary>
public class PreventivoRevisione
{
    public Guid Id { get; set; }
    /// <summary>FK → Preventivi.Id</summary>
    public Guid PreventivoId { get; set; }
    /// <summary>Numero revisione progressivo (1, 2, 3…)</summary>
    public int NumeroRevisione { get; set; }
    public DateTime DataRevisione { get; set; } = DateTime.UtcNow;
    /// <summary>Snapshot JSON della PreventivoDto prima della modifica</summary>
    public string DtoJson { get; set; } = string.Empty;
    /// <summary>Note opzionali sulla revisione</summary>
    public string? NoteRevisione { get; set; }
}
