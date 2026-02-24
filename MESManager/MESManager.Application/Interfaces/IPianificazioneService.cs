using MESManager.Application.DTOs;
using MESManager.Domain.Entities;

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
    /// Calcola la data/ora fine prevista considerando anche i festivi e il calendario lavoro
    /// VERSIONE PRINCIPALE - Usa CalendarioLavoroDto per rispettare giorni e orari configurati
    /// </summary>
    DateTime CalcolaDataFinePrevistaConFestivi(DateTime dataInizio, int durataMinuti, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi);
    
    /// <summary>
    /// [DEPRECATO] Calcola la data/ora fine prevista con parametri generici
    /// Usare overload con CalendarioLavoroDto per pieno supporto calendario
    /// </summary>
    [Obsolete("Usare overload con CalendarioLavoroDto per rispettare le impostazioni calendario utente")]
    DateTime CalcolaDataFinePrevistaConFestivi(DateTime dataInizio, int durataMinuti, int oreLavorativeGiornaliere, int giorniLavorativiSettimanali, HashSet<DateOnly> festivi);
    
    /// <summary>
    /// Ottiene il colore associato allo stato della commessa per il Gantt
    /// </summary>
    string GetColoreStato(string stato);
    
    /// <summary>
    /// Mappa una lista di commesse a DTOs per il Gantt con batch loading ottimizzato.
    /// Elimina duplicazione e problema N+1 queries.
    /// </summary>
    /// <summary>
    /// Mappa una lista di commesse a DTOs per il Gantt con batch loading ottimizzato.
    /// plcLookup: key = NumeroMacchina (int), value = (CicliFatti, QuantitaDaProdurre).
    /// Se null o macchina assente → fallback date-based per PercentualeCompletamento.
    /// </summary>
    Task<List<CommessaGanttDto>> MapToGanttDtoBatchAsync(
        List<Commessa> commesse,
        ImpostazioniProduzione impostazioni,
        Dictionary<string, Anime>? animeLookup = null,
        Dictionary<int, (int CicliFatti, int QuantitaDaProdurre)>? plcLookup = null);
    
    /// <summary>
    /// Calcola la percentuale di completamento.
    /// Priorità: dati PLC reali (cicliFattiPlc/quantitaDaProdurrePlc) > calcolo date-based.
    /// Se PLC non disponibile o QuantitaDaProdurre=0 → fallback date-based.
    /// </summary>
    decimal CalcolaPercentualeCompletamento(Commessa commessa, int? cicliFattiPlc = null, int? quantitaDaProdurrePlc = null);
}
