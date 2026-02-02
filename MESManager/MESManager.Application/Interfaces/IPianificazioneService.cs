namespace MESManager.Application.Interfaces;

public interface IPianificazioneService
{
    /// <summary>
    /// Calcola la durata prevista di una commessa in minuti
    /// Formula: TempoSetup + (TempoCiclo * QuantitaRichiesta / NumeroFigure) / 60
    /// </summary>
    int CalcolaDurataPrevistaMinuti(int tempoCicloSecondi, int numeroFigure, decimal quantitaRichiesta, int tempoSetupMinuti);
    
    /// <summary>
    /// Calcola la data/ora fine prevista partendo da una data inizio e una durata
    /// Considera solo le ore lavorative configurate
    /// </summary>
    DateTime CalcolaDataFinePrevista(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali);
    
    /// <summary>
    /// Calcola la data/ora fine prevista considerando anche i festivi
    /// </summary>
    DateTime CalcolaDataFinePrevistaConFestivi(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali, HashSet<DateOnly> festivi);
    
    /// <summary>
    /// Ottiene il colore associato allo stato della commessa per il Gantt
    /// </summary>
    string GetColoreStato(string stato);
}
