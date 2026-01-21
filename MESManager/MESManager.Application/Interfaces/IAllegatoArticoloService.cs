using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Service per la gestione degli allegati articolo
/// </summary>
public interface IAllegatoArticoloService
{
    /// <summary>
    /// Ottiene tutti gli allegati per un articolo, separati in Foto e Documenti
    /// </summary>
    Task<AllegatiArticoloResponse> GetAllegatiByArticoloAsync(string codiceArticolo, int? idArchivio = null);

    /// <summary>
    /// Ottiene un allegato per ID
    /// </summary>
    Task<AllegatoArticoloDto?> GetByIdAsync(int id);

    /// <summary>
    /// Carica un nuovo allegato (foto o documento)
    /// </summary>
    Task<AllegatoArticoloDto> UploadAsync(Stream fileStream, string fileName, long fileSize, UploadAllegatoRequest request);

    /// <summary>
    /// Elimina un allegato (sia dal DB che dal filesystem)
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Aggiorna la priorità di un allegato
    /// </summary>
    Task<bool> UpdatePrioritaAsync(int id, int priorita);

    /// <summary>
    /// Ottiene il contenuto binario di un file
    /// </summary>
    Task<(byte[] Content, string ContentType, string FileName)?> GetFileContentAsync(int id);

    /// <summary>
    /// Importa allegati da Gantt DB per un articolo specifico
    /// </summary>
    Task<ImportAllegatiResult> ImportFromGanttAsync(string codiceArticolo);

    /// <summary>
    /// Importa tutti gli allegati da Gantt DB
    /// </summary>
    Task<ImportAllegatiResult> ImportAllFromGanttAsync();

    /// <summary>
    /// Conta totale allegati nel catalogo locale
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// Conta allegati importati da Gantt
    /// </summary>
    Task<int> CountImportatiDaGanttAsync();

    /// <summary>
    /// Ottiene il conteggio foto/documenti per ogni articolo (per arricchire griglia catalogo)
    /// </summary>
    Task<Dictionary<string, (int Foto, int Documenti)>> GetConteggioPerArticoloAsync();
}
