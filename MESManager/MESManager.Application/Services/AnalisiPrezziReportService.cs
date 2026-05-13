using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Enums;

namespace MESManager.Application.Services;

public class AnalisiPrezziReportService : IAnalisiPrezziReportService
{
    public bool IsCommessaAperta(CommessaDto commessa)
    {
        return string.Equals(commessa.Stato, nameof(StatoCommessa.Aperta), StringComparison.OrdinalIgnoreCase)
            || string.Equals(commessa.Stato, nameof(StatoCommessa.InLavorazione), StringComparison.OrdinalIgnoreCase);
    }

    public bool IsArticoloInCommessaAperta(
        string? codiceArticolo,
        IReadOnlySet<string> codiciArticoliCommesseAperte)
    {
        return !string.IsNullOrWhiteSpace(codiceArticolo)
            && codiciArticoliCommesseAperte.Contains(codiceArticolo.Trim());
    }

    public bool ReportRichiedeSchedaAnima(AnalisiCommessaApertaReportDto report)
    {
        return !report.AnalisiCompleta
            && !string.IsNullOrWhiteSpace(report.CodiceArticolo)
            && !report.Dettaglio.Contains("preventivo", StringComparison.OrdinalIgnoreCase)
            && !report.Dettaglio.Contains("aggiornamento prezzi", StringComparison.OrdinalIgnoreCase);
    }

    public List<AnalisiCommessaApertaReportDto> CreaReportCommesseAperte(
        IReadOnlyList<CommessaDto> commesseAperte,
        IReadOnlyList<AnalisiPrezziRigaDto> righeAnalisi)
    {
        var analisiByCodice = righeAnalisi
            .Where(r => !string.IsNullOrWhiteSpace(r.CodiceArticolo))
            .GroupBy(r => r.CodiceArticolo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return commesseAperte
            .Select((commessa, index) => CreaReportCommessa(commessa, index + 1, analisiByCodice))
            .ToList();
    }

    private static AnalisiCommessaApertaReportDto CreaReportCommessa(
        CommessaDto commessa,
        int ordine,
        IReadOnlyDictionary<string, AnalisiPrezziRigaDto> analisiByCodice)
    {
        var codiceArticolo = commessa.ArticoloCodice?.Trim();
        var report = new AnalisiCommessaApertaReportDto
        {
            Ordine = ordine,
            CodiceCommessa = commessa.Codice,
            Stato = commessa.Stato ?? "-",
            DataConsegna = commessa.DataConsegna,
            NumeroMacchina = commessa.NumeroMacchina,
            CodiceArticolo = codiceArticolo,
            Cliente = commessa.ClienteDisplay
        };

        if (string.IsNullOrWhiteSpace(codiceArticolo))
        {
            report.Esito = "Dati mancanti";
            report.Dettaglio = "Manca il codice articolo sulla commessa.";
            return report;
        }

        if (analisiByCodice.TryGetValue(codiceArticolo, out var analisi))
        {
            report.Analisi = analisi;
            report.AnalisiCompleta = true;
            report.Esito = "Analizzata";
            report.Dettaglio = $"Preventivi: {analisi.NumeroPreventiviTotali}, prezzo catalogo euro {analisi.PrezzoCatalogoAttuale:N4}.";
            return report;
        }

        report.Esito = "Dati mancanti";
        report.Dettaglio = !commessa.ArticoloPrezzo.HasValue || commessa.ArticoloPrezzo.Value <= 0
            ? "Manca il prezzo catalogo o l'articolo non e' presente nel catalogo prezzi."
            : "Manca un preventivo o aggiornamento prezzi per questo articolo.";
        return report;
    }
}
