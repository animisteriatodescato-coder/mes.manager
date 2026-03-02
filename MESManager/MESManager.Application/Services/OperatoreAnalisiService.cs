using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Application.Services;

/// <summary>
/// Servizio di analisi performance operatori basato sui dati PLC storici.
/// Utilizzato per classifiche premi produttivi, analisi fermi e scarti.
/// NON accede al DB direttamente: delega a IPlcAppService.
/// </summary>
public class OperatoreAnalisiService : IOperatoreAnalisiService
{
    private readonly IPlcAppService _plcService;

    // Stati considerati "fermo macchina"
    private static readonly HashSet<string> StatiFermo = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ferma", "Fermo", "Stop", "Alarm", "Allarme", "Errore", "Error",
        "Manutenzione", "Pausa", "Blocco"
    };

    // Stati considerati "setup"
    private static readonly HashSet<string> StatiSetup = new(StringComparer.OrdinalIgnoreCase)
    {
        "Setup", "InizioSetup", "Settaggio", "Cambio", "Attrezzaggio"
    };

    // Stati considerati "in produzione"
    private static readonly HashSet<string> StatiProduzione = new(StringComparer.OrdinalIgnoreCase)
    {
        "InProduzione", "Produzione", "Running", "Ciclo", "CicloInCorso"
    };

    public OperatoreAnalisiService(IPlcAppService plcService)
    {
        _plcService = plcService;
    }

    public async Task<OperatoreAnalisiResult> AnalizzaAsync(
        DateTime dal,
        DateTime al,
        string? filtraMacchina = null)
    {
        // Carica tutti i dati storici del periodo SENZA limite di record.
        // NOTA: limit = null rimuove il Take(5000) — sicuro perché il filtro date è obbligatorio.
        var tuttiDati = await _plcService.GetAllStoricoAsync(dal, al.AddDays(1), limit: null);

        // Filtra per macchina se richiesto
        if (!string.IsNullOrEmpty(filtraMacchina))
            tuttiDati = tuttiDati.Where(x => x.MacchinaNumero == filtraMacchina).ToList();

        // Ordina per timestamp ASC: necessario per il calcolo incrementi consecutivi
        tuttiDati = tuttiDati.OrderBy(x => x.MacchinaId).ThenBy(x => x.BarcodeLavorazione).ThenBy(x => x.Timestamp).ToList();

        // Considera solo record con operatore assegnato
        var conOperatore = tuttiDati
            .Where(x => x.NumeroOperatore.HasValue && x.NumeroOperatore > 0)
            .ToList();

        var operatori = conOperatore
            .GroupBy(x => x.NumeroOperatore!.Value)
            .Select(grpOp => CalcolaStatistiche(grpOp.Key, grpOp.ToList()))
            .OrderByDescending(o => o.ScoreComposito)
            .ToList();

        return new OperatoreAnalisiResult
        {
            Operatori = operatori,
            Dal = dal,
            Al = al,
            TotaleRecord = tuttiDati.Count
        };
    }

    private static OperatoreAnalisiDto CalcolaStatistiche(int numeroOp, List<PlcStoricoDto> records)
    {
        // Nome: prendi il più recente non nullo
        var nome = records
            .Where(r => !string.IsNullOrEmpty(r.NomeOperatore))
            .OrderByDescending(r => r.Timestamp)
            .Select(r => r.NomeOperatore!)
            .FirstOrDefault() ?? $"Operatore {numeroOp}";

        // ---- PRODUZIONE ----
        // CicliFatti nel DB è il contatore CUMULATIVO del PLC per quella commessa/barcode.
        // Usiamo la SOMMA DEGLI INCREMENTI CONSECUTIVI (max(0, r[i]-r[i-1])) per ogni
        // gruppo macchina+barcode:  questo gestisce correttamente:
        //   - Contatori già avanzati a inizio periodo (partiamo dall'incremento reale)
        //   - Reset del contatore mid-period (Math.Max(0,...) ignora decrementi)
        var latestPerMacchinaCommessa = records
            .GroupBy(r => new { r.MacchinaId, r.BarcodeLavorazione })
            .Select(g =>
            {
                var ordinati = g.OrderBy(x => x.Timestamp).ToList();
                var ultimo = ordinati.Last();

                // Somma incrementi positivi tra snapshot consecutivi
                int cicli = 0, scarti = 0;
                for (int i = 1; i < ordinati.Count; i++)
                {
                    cicli  += Math.Max(0, ordinati[i].CicliFatti  - ordinati[i - 1].CicliFatti);
                    scarti += Math.Max(0, ordinati[i].CicliScarti - ordinati[i - 1].CicliScarti);
                }

                // Se c'è un solo snapshot nel periodo (es. cambio stato),
                // non possiamo calcolare un delta: lasciamo 0 per quel gruppo.
                // Il valore è comunque conteggiato nel gruppo successivo quando arriva 
                // un altro snapshot con il contatore avanzato.
                return new
                {
                    Cicli = cicli,
                    Scarti = scarti,
                    UltimoTempoMedioRil = ultimo.TempoMedioRilevato,
                    UltimoTempoMedio = ultimo.TempoMedio
                };
            })
            .ToList();

        int cicliTotali = latestPerMacchinaCommessa.Sum(x => x.Cicli);
        int scartiTotali = latestPerMacchinaCommessa.Sum(x => x.Scarti);

        // Efficienza media (solo dove abbiamo dati validi)
        var conTempi = latestPerMacchinaCommessa
            .Where(x => x.UltimoTempoMedio > 0 && x.UltimoTempoMedioRil > 0)
            .ToList();
        double efficienzaMedia = conTempi.Any()
            ? Math.Min(100, conTempi.Average(x => (double)x.UltimoTempoMedio / x.UltimoTempoMedioRil * 100))
            : 0;

        double tempoMedioCiclo = records
            .Where(r => r.TempoMedioRilevato > 0)
            .Select(r => (double)r.TempoMedioRilevato)
            .DefaultIfEmpty(0)
            .Average();

        // ---- STATI / FERMI ----
        var statoBreakdown = records
            .GroupBy(r => string.IsNullOrEmpty(r.StatoMacchina) ? "Sconosciuto" : r.StatoMacchina)
            .ToDictionary(g => g.Key, g => g.Count());

        int numeroFermi = records.Count(r => StatiFermo.Contains(r.StatoMacchina ?? ""));
        int numeroSetup = records.Count(r => StatiSetup.Contains(r.StatoMacchina ?? ""));
        int numeroAllarmi = records.Count(r =>
            (r.StatoMacchina ?? "").IndexOf("alarm", StringComparison.OrdinalIgnoreCase) >= 0 ||
            (r.StatoMacchina ?? "").IndexOf("allarme", StringComparison.OrdinalIgnoreCase) >= 0 ||
            (r.StatoMacchina ?? "").IndexOf("errore", StringComparison.OrdinalIgnoreCase) >= 0);

        // ---- TREND GIORNALIERO ----
        var trendGiornaliero = records
            .GroupBy(r => r.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var latestMC = g
                    .GroupBy(r => new { r.MacchinaId, r.BarcodeLavorazione })
                    .Select(gc =>
                    {
                        var ord = gc.OrderBy(x => x.Timestamp).ToList();
                        int c = 0, s = 0;
                        for (int i = 1; i < ord.Count; i++)
                        {
                            c += Math.Max(0, ord[i].CicliFatti  - ord[i - 1].CicliFatti);
                            s += Math.Max(0, ord[i].CicliScarti - ord[i - 1].CicliScarti);
                        }
                        return new
                        {
                            Cicli = c,
                            Scarti = s,
                            TempoMedio = ord.Last().TempoMedio,
                            TempoMedioRil = ord.Last().TempoMedioRilevato
                        };
                    })
                    .ToList();

                var conT = latestMC.Where(x => x.TempoMedio > 0 && x.TempoMedioRil > 0).ToList();
                double eff = conT.Any()
                    ? Math.Min(100, conT.Average(x => (double)x.TempoMedio / x.TempoMedioRil * 100))
                    : 0;

                return new OperatoreGiornataDto
                {
                    Data = g.Key,
                    Cicli = latestMC.Sum(x => x.Cicli),
                    Scarti = latestMC.Sum(x => x.Scarti),
                    Fermi = g.Count(r => StatiFermo.Contains(r.StatoMacchina ?? "")),
                    Setup = g.Count(r => StatiSetup.Contains(r.StatoMacchina ?? "")),
                    Efficienza = eff
                };
            })
            .ToList();

        return new OperatoreAnalisiDto
        {
            Nome = nome,
            NumeroOperatore = numeroOp,
            CicliTotali = cicliTotali,
            ScartiTotali = scartiTotali,
            EfficienzaMedia = efficienzaMedia,
            TempoMedioCicloRilevato = tempoMedioCiclo,
            NumeroFermi = numeroFermi,
            NumeroSetup = numeroSetup,
            NumeroAllarmi = numeroAllarmi,
            StatoBreakdown = statoBreakdown,
            TrendGiornaliero = trendGiornaliero
        };
    }
}
