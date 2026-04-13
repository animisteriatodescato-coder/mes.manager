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

    // ── Numero progressivo ────────────────────────────────────────────────
    /// <summary>Numero preventivo progressivo (auto-assegnato, parte da 1000)</summary>
    public int NumeroPreventivo { get; set; }

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
    /// <summary>Lotto principale (obbligatorio)</summary>
    public int Lotto { get; set; }
    /// <summary>Lotti aggiuntivi opzionali (multi-lotto preventivo)</summary>
    public int? Lotto2 { get; set; }
    public int? Lotto3 { get; set; }
    public int? Lotto4 { get; set; }
    public int SpariOrari { get; set; }
    public decimal CostoAttrezzatura { get; set; }

    // ── Margini per lotto ───────────────────────────────────────────────
    /// <summary>Percentuale di margine applicata su ogni lotto (0 = nessun margine)</summary>
    public decimal Margine1 { get; set; }
    public decimal Margine2 { get; set; }
    public decimal Margine3 { get; set; }
    public decimal Margine4 { get; set; }

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
    /// <summary>InAttesa | Inviato | Approvato | Rifiutato</summary>
    public string Stato { get; set; } = "InAttesa";

    // ── Feature v1.65.7 ─────────────────────────────────────────────────
    /// <summary>Note interne (non stampate sul modulo cliente)</summary>
    public string? NoteInterne { get; set; }
    /// <summary>Sconto commerciale % applicato dopo il margine</summary>
    public decimal Sconto { get; set; }
    /// <summary>FK commessa collegata (valorizzata quando approvato e collegato)</summary>
    public Guid? CommessaId { get; set; }
    /// <summary>Email destinatario a cui è stato inviato il preventivo</summary>
    public string? EmailDestinatario { get; set; }
    /// <summary>Data e ora invio email (null = mai inviato)</summary>
    public DateTime? EmailInviatoIl { get; set; }
}
