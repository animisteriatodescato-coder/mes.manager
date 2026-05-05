namespace MESManager.Domain.Enums;

public enum StatoSchedaManutenzione
{
    InCompilazione = 0,
    Completata = 1,
    ConAnomalie = 2,
    /// <summary>Scheda segnalata per intervento — modificabile, non chiusa</summary>
    Segnalata = 3,
    /// <summary>Cassa/macchina in lavorazione di manutenzione — modificabile, non chiusa</summary>
    InManutenzione = 4
}
