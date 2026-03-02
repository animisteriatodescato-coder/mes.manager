namespace MESManager.Application.DTOs;

/// <summary>
/// Statistiche aggregate per un singolo operatore (usate per premi produttivi e analisi performance)
/// </summary>
public class OperatoreAnalisiDto
{
    public string Nome { get; set; } = string.Empty;
    public int NumeroOperatore { get; set; }

    // Produzione
    public int CicliTotali { get; set; }
    public int ScartiTotali { get; set; }
    public double PercentualeScarti => CicliTotali > 0 ? (double)ScartiTotali / CicliTotali * 100 : 0;

    // Qualità macchina
    public double EfficienzaMedia { get; set; }   // TempoMedio / TempoMedioRilevato * 100
    public double TempoMedioCicloRilevato { get; set; }  // secondi

    // Fermi e stati
    public int NumeroFermi { get; set; }          // snapshot con stato != InProduzione
    public int NumeroSetup { get; set; }          // snapshot in stato Setup/InizioSetup
    public int NumeroAllarmi { get; set; }        // snapshot in stato Allarme/Errore
    public Dictionary<string, int> StatoBreakdown { get; set; } = new(); // stato -> conteggio snapshot

    // Breakdown giornaliero (per grafici trend)
    public List<OperatoreGiornataDto> TrendGiornaliero { get; set; } = new();

    // Score composito per ranking premi produttivi
    // Formula: Cicli(peso 1) - Scarti%(peso 10) - Fermi(peso 5) + Efficienza%(peso 2)
    public double ScoreComposito =>
        CicliTotali * 1.0
        - PercentualeScarti * 10.0
        - NumeroFermi * 5.0
        + EfficienzaMedia * 2.0;

    // Ranking badge
    public string BadgeColore =>
        ScoreComposito > 1500 ? "#43a047" :    // verde - eccellente
        ScoreComposito > 800 ? "#1976d2" :     // blu - buono
        ScoreComposito > 300 ? "#f57c00" :     // arancio - nella media
        "#e53935";                              // rosso - sotto media

    public string BadgeLabel =>
        ScoreComposito > 1500 ? "⭐ Eccellente" :
        ScoreComposito > 800 ? "✅ Buono" :
        ScoreComposito > 300 ? "⚠️ Medio" :
        "❌ Da migliorare";
}

/// <summary>
/// Dati giornalieri per un operatore (usati per grafici trend)
/// </summary>
public class OperatoreGiornataDto
{
    public DateTime Data { get; set; }
    public int Cicli { get; set; }
    public int Scarti { get; set; }
    public int Fermi { get; set; }
    public int Setup { get; set; }
    public double Efficienza { get; set; }
    public double PercentualeScarti => Cicli > 0 ? (double)Scarti / Cicli * 100 : 0;
}

/// <summary>
/// Risultato completo dell'analisi operatori per il periodo selezionato
/// </summary>
public class OperatoreAnalisiResult
{
    public List<OperatoreAnalisiDto> Operatori { get; set; } = new();
    public DateTime Dal { get; set; }
    public DateTime Al { get; set; }
    public int TotaleRecord { get; set; }

    // KPI globali
    public int CicliGlobali => Operatori.Sum(o => o.CicliTotali);
    public double ScartGlobalePercent => CicliGlobali > 0
        ? (double)Operatori.Sum(o => o.ScartiTotali) / CicliGlobali * 100 : 0;
    public double EfficienzaGlobale => Operatori.Any()
        ? Operatori.Where(o => o.EfficienzaMedia > 0).Average(o => o.EfficienzaMedia) : 0;
}
