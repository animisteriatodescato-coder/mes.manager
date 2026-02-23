using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia per il servizio di pianificazione avanzata con accodamento rigido.
/// </summary>
public interface IPianificazioneEngineService
{
    /// <summary>
    /// Sposta una commessa su una macchina con gestione accodamento rigido.
    /// </summary>
    Task<SpostaCommessaResponse> SpostaCommessaAsync(SpostaCommessaRequest request);
    
    /// <summary>
    /// Ricalcola tutte le commesse di una macchina a cascata.
    /// </summary>
    Task RicalcolaAcqueMacchinaAsync(int? numeroMacchina);
    
    /// <summary>
    /// Ricalcola tutte le commesse di una macchina rispettando i blocchi.
    /// </summary>
    Task RicalcolaMacchinaConBlocchiAsync(int? numeroMacchina);
    
    /// <summary>
    /// Ricalcola tutte le commesse di tutte le macchine.
    /// </summary>
    Task<List<CommessaGanttDto>> RicalcolaTutteCommesseAsync();
    
    /// <summary>
    /// Ottiene i festivi come HashSet per calcoli rapidi.
    /// </summary>
    Task<HashSet<DateOnly>> GetFestiviSetAsync();
    
    /// <summary>
    /// Ottiene la lista dei festivi.
    /// </summary>
    Task<List<FestivoDto>> GetFestiviAsync();
    
    /// <summary>
    /// Aggiunge un festivo.
    /// </summary>
    Task<FestivoDto> AddFestivoAsync(CreateFestivoRequest request);
    
    /// <summary>
    /// Rimuove un festivo.
    /// </summary>
    Task<bool> DeleteFestivoAsync(int id);
    
    /// <summary>
    /// Inizializza i festivi standard italiani per un anno.
    /// </summary>
    Task<List<FestivoDto>> InizializzaFestiviStandardAsync(int anno);
    
    /// <summary>
    /// Suggerisce la macchina migliore per una commessa (earliest completion time).
    /// </summary>
    Task<SuggerisciMacchinaResponse> SuggerisciMacchinaMiglioreAsync(SuggerisciMacchinaRequest request);
    
    /// <summary>
    /// Carica automaticamente una commessa sul Gantt con algoritmo intelligente.
    /// Considera: data consegna, carico macchine, disponibilità.
    /// </summary>
    /// <param name="commessaId">ID della commessa da caricare</param>
    /// <param name="numeroMacchinaManuale">Se specificato, forza il caricamento su questa macchina (bypass auto-scheduler)</param>
    Task<CaricaSuGanttResponse> CaricaSuGanttAsync(Guid commessaId, int? numeroMacchinaManuale = null);
}
