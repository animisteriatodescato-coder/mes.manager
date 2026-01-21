using MESManager.Domain.Entities;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Repository per la gestione degli allegati articolo nel database locale
/// </summary>
public interface IAllegatoArticoloRepository
{
    /// <summary>
    /// Ottiene tutti gli allegati per un articolo specifico
    /// </summary>
    Task<IEnumerable<AllegatoArticolo>> GetByCodiceArticoloAsync(string codiceArticolo);

    /// <summary>
    /// Ottiene tutti gli allegati per un archivio/id
    /// </summary>
    Task<IEnumerable<AllegatoArticolo>> GetByArchivioAsync(string archivio, int idArchivio);

    /// <summary>
    /// Ottiene un allegato per ID
    /// </summary>
    Task<AllegatoArticolo?> GetByIdAsync(int id);

    /// <summary>
    /// Ottiene un allegato per ID Gantt originale (per evitare duplicati in import)
    /// </summary>
    Task<AllegatoArticolo?> GetByIdGanttOriginaleAsync(int idGanttOriginale);

    /// <summary>
    /// Aggiunge un nuovo allegato
    /// </summary>
    Task<AllegatoArticolo> AddAsync(AllegatoArticolo allegato);

    /// <summary>
    /// Aggiunge più allegati in batch
    /// </summary>
    Task AddRangeAsync(IEnumerable<AllegatoArticolo> allegati);

    /// <summary>
    /// Aggiorna un allegato esistente
    /// </summary>
    Task UpdateAsync(AllegatoArticolo allegato);

    /// <summary>
    /// Elimina un allegato
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Conta gli allegati importati da Gantt
    /// </summary>
    Task<int> CountImportatiDaGanttAsync();

    /// <summary>
    /// Ottiene tutti gli allegati (per statistiche/admin)
    /// </summary>
    Task<IEnumerable<AllegatoArticolo>> GetAllAsync();

    /// <summary>
    /// Ottiene solo le foto per un articolo
    /// </summary>
    Task<IEnumerable<AllegatoArticolo>> GetFotoByCodiceArticoloAsync(string codiceArticolo);

    /// <summary>
    /// Ottiene solo i documenti per un articolo
    /// </summary>
    Task<IEnumerable<AllegatoArticolo>> GetDocumentiByCodiceArticoloAsync(string codiceArticolo);
}
