using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IAnalisiPrezziReportService
{
    bool IsCommessaAperta(CommessaDto commessa);
    bool IsArticoloInCommessaAperta(string? codiceArticolo, IReadOnlySet<string> codiciArticoliCommesseAperte);
    bool ReportRichiedeSchedaAnima(AnalisiCommessaApertaReportDto report);
    List<AnalisiCommessaApertaReportDto> CreaReportCommesseAperte(
        IReadOnlyList<CommessaDto> commesseAperte,
        IReadOnlyList<AnalisiPrezziRigaDto> righeAnalisi);
}
