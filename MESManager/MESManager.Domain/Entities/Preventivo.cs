namespace MESManager.Domain.Entities;

/// <summary>
/// Preventivo per anime da fonderia.
/// Il lotto è un singolo valore (un preventivo = un lotto).
/// Per preventivi multi-lotto creare N record separati o usare i campi Lotto2/Lotto3.
/// </summary>
public class Preventivo
{
    public Guid Id { get; set; }
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

    // ── Dati cliente / articolo ─────────────────────────────────────────
    public string Cliente { get; set; } = string.Empty;
    public string CodiceArticolo { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? NoteCliente { get; set; }

    // ── Sabbia ──────────────────────────────────────────────────────────
    /// <summary>FK → PreventivoTipoSabbia (nullable per retrocompatibilità)</summary>
    public Guid? TipoSabbiaId { get; set; }
    public PreventivoTipoSabbia? TipoSabbia { get; set; }
    /// <summary>Snapshot al momento del salvataggio (in caso il tipo venga modificato)</summary>
    public string SabbiaSnapshot { get; set; } = string.Empty;
    public decimal EuroOraSabbia { get; set; }
    public decimal PrezzoSabbiaKg { get; set; }

    // ── Parametri produzione ────────────────────────────────────────────
    public int Figure { get; set; }
    public decimal PesoAnima { get; set; }
    public int Lotto { get; set; }
    public int SpariOrari { get; set; }
    public decimal CostoAttrezzatura { get; set; }

    // ── Verniciatura ────────────────────────────────────────────────────
    public bool VerniciaturaRichiesta { get; set; }
    public Guid? TipoVerniceId { get; set; }
    public PreventivoTipoVernice? TipoVernice { get; set; }
    public string? VerniceSnapshot { get; set; }
    public decimal CostoVerniceKg { get; set; }
    public decimal PercentualeVernice { get; set; }
    public int VerniciaturaPzOra { get; set; }
    public decimal EuroOraVerniciatura { get; set; }

    // ── Servizi aggiuntivi ──────────────────────────────────────────────
    public bool IncollaggioRichiesto { get; set; }
    public decimal EuroOraIncollaggio { get; set; }
    public int IncollaggioPzOra { get; set; }
    public bool ImballaggioRichiesto { get; set; }
    public decimal EuroOraImballaggio { get; set; }
    public int ImballaggioPzOra { get; set; }

    // ── Risultati calcolati (snapshot per archivio) ─────────────────────
    public decimal CalcCostoAnima { get; set; }
    public decimal CalcVerniciaturaTot { get; set; }
    public decimal CalcPrezzoVendita { get; set; }

    // ── Stato ───────────────────────────────────────────────────────────
    /// <summary>InAttesa | Approvato | Rifiutato</summary>
    public string Stato { get; set; } = "InAttesa";
}
