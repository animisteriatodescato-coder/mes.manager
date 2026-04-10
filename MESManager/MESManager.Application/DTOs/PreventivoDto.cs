namespace MESManager.Application.DTOs;

public class PreventivoTipoSabbiaDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Famiglia { get; set; } = string.Empty;
    public decimal EuroOra { get; set; }
    public decimal PrezzoKg { get; set; }
    public int SpariDefault { get; set; }
    public bool Attivo { get; set; } = true;
    public int Ordine { get; set; }
}

public class PreventivoTipoVerniceDto
{
    public Guid Id { get; set; }
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Famiglia { get; set; } = string.Empty;
    public decimal PrezzoKg { get; set; }
    public decimal PercentualeApplicazione { get; set; } = 8;
    public bool Attivo { get; set; } = true;
    public int Ordine { get; set; }
}

public class PreventivoDto
{
    public Guid Id { get; set; }
    public DateTime DataCreazione { get; set; } = DateTime.Today;
    public string Cliente { get; set; } = string.Empty;
    public string CodiceArticolo { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
    public string? NoteCliente { get; set; }

    // Sabbia
    public Guid? TipoSabbiaId { get; set; }
    public string SabbiaSnapshot { get; set; } = string.Empty;
    public decimal EuroOraSabbia { get; set; }
    public decimal PrezzoSabbiaKg { get; set; }

    // Parametri produzione
    public int Figure { get; set; }
    public decimal PesoAnima { get; set; }
    public int Lotto { get; set; }
    public int? Lotto2 { get; set; }
    public int? Lotto3 { get; set; }
    public int? Lotto4 { get; set; }
    public int SpariOrari { get; set; }
    public decimal CostoAttrezzatura { get; set; } = 100;

    // Verniciatura
    public bool VerniciaturaRichiesta { get; set; }
    public Guid? TipoVerniceId { get; set; }
    public string? VerniceSnapshot { get; set; }
    public decimal CostoVerniceKg { get; set; }
    public decimal PercentualeVernice { get; set; } = 8;
    public int VerniciaturaPzOra { get; set; }
    public decimal EuroOraVerniciatura { get; set; } = 40;

    // Servizi aggiuntivi
    public bool IncollaggioRichiesto { get; set; }
    public decimal EuroOraIncollaggio { get; set; } = 40;
    public int IncollaggioPzOra { get; set; }
    public bool ImballaggioRichiesto { get; set; }
    public decimal EuroOraImballaggio { get; set; } = 40;
    public int ImballaggioPzOra { get; set; } = 180;

    // Risultati calcolati
    public decimal CalcCostoAnima { get; set; }
    public decimal CalcVerniciaturaTot { get; set; }
    public decimal CalcPrezzoVendita { get; set; }

    public string Stato { get; set; } = "InAttesa";
}

/// <summary>
/// Risultato del calcolo preventivo lato server/client.
/// </summary>
public class PreventivoCalcoloResult
{
    public decimal Lavorazione { get; set; }
    public decimal Sabbia { get; set; }
    public decimal LavSabFig { get; set; }
    public decimal RipartizioneAtt { get; set; }
    public decimal CostoAnima { get; set; }
    public decimal VernMateriale { get; set; }
    public decimal VernManodopera { get; set; }
    public decimal VernTot { get; set; }
    public decimal Incollaggio { get; set; }
    public decimal Imballaggio { get; set; }
    public decimal PrezzoVendita { get; set; }
}
