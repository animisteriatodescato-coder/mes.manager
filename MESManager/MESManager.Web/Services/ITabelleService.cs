using MESManager.Domain.Constants;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio per la gestione delle tabelle di lookup (Colla, Vernice, Sabbia, Imballo).
/// Carica i dati da file JSON all'avvio e permette il salvataggio persistente.
/// </summary>
public interface ITabelleService
{
    Dictionary<string, string> GetColla();
    Dictionary<string, string> GetVernice();
    Dictionary<string, string> GetSabbia();
    Dictionary<string, string> GetImballo();

    List<LookupItem> GetCollaList();
    List<LookupItem> GetVerniceList();
    List<LookupItem> GetSabbiaList();
    List<LookupItem> GetImballoList();

    Task SalvaCollaAsync(List<LookupItem> items);
    Task SalvaVerniceAsync(List<LookupItem> items);
    Task SalvaSabbiaAsync(List<LookupItem> items);
    Task SalvaImballoAsync(List<LookupItem> items);
}
