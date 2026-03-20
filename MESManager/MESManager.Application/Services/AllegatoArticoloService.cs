using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MESManager.Application.Configuration;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Constants;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services;

/// <summary>
/// Service per la gestione degli allegati articolo con storage locale
/// </summary>
public class AllegatoArticoloService : AllegatoFileServiceBase, IAllegatoArticoloService
{
    private readonly IAllegatoArticoloRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AllegatoArticoloService> _logger;
    private readonly string _ganttConnectionString;

    public AllegatoArticoloService(
        IAllegatoArticoloRepository repository,
        IConfiguration configuration,
        IOptions<FileConfiguration> fileConfiguration,
        ILogger<AllegatoArticoloService> logger)
        : base(logger, fileConfiguration.Value)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;

        // Connection string per Gantt - prova GanttDb prima di GanttConnection
        _ganttConnectionString = configuration.GetConnectionString("GanttDb") 
            ?? configuration.GetConnectionString("GanttConnection")
            ?? throw new InvalidOperationException("Connection string 'GanttDb' non trovata.");
        
        _logger.LogInformation("AllegatoArticoloService initialized. BasePath={Path}, PathMappings={Mappings}",
            AllegatiBasePath,
            string.Join("; ", PathMappings.Select(m => $"{m.Source} -> {m.Target}")));
    }

    public async Task<AllegatiArticoloResponse> GetAllegatiByArticoloAsync(string codiceArticolo, int? idArchivio = null)
    {
        _logger.LogDebug("GetAllegatiByArticoloAsync: CodiceArticolo={CodiceArticolo}, IdArchivio={IdArchivio}", 
            codiceArticolo, idArchivio);
        
        var allegati = await _repository.GetByCodiceArticoloAsync(codiceArticolo);
        
        // Se non ci sono allegati locali, prova a importare da Gantt
        if (!allegati.Any())
        {
            _logger.LogInformation("No local allegati for {CodiceArticolo}, trying import from Gantt...", codiceArticolo);
            var importResult = await ImportFromGanttAsync(codiceArticolo);
            if (importResult.TotaleImportati > 0)
            {
                _logger.LogInformation("Imported {Count} allegati from Gantt for {CodiceArticolo}", 
                    importResult.TotaleImportati, codiceArticolo);
                allegati = await _repository.GetByCodiceArticoloAsync(codiceArticolo);
            }
        }
        
        var dtos = allegati.Select(MapToDto).ToList();
        
        return new AllegatiArticoloResponse
        {
            Foto = dtos.Where(d => d.IsFoto).ToList(),
            Documenti = dtos.Where(d => !d.IsFoto).ToList()
        };
    }

    public async Task<AllegatoArticoloDto?> GetByIdAsync(int id)
    {
        var allegato = await _repository.GetByIdAsync(id);
        return allegato == null ? null : MapToDto(allegato);
    }

    public async Task<AllegatoArticoloDto> UploadAsync(Stream fileStream, string fileName, long fileSize, UploadAllegatoRequest request)
    {
        _logger.LogInformation("UploadAsync: CodiceArticolo={CodiceArticolo}, File={FileName}, Size={Size}", 
            request.CodiceArticolo, fileName, fileSize);
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var isFoto = FileConstants.FotoExtensions.Contains(extension);
        
        // Sanitizza il codice articolo per il nome file: / e \ sono separatori di path
        var safeCode = request.CodiceArticolo
            .Replace("/", "-")
            .Replace("\\", "-");
        
        // Auto-assegna priorità se non specificata (0 = non impostata)
        var priorita = request.Priorita;
        if (priorita <= 0)
        {
            var existing = await _repository.GetByCodiceArticoloAsync(request.CodiceArticolo);
            priorita = existing.Any() ? existing.Max(x => x.Priorita) + 1 : 1;
        }
        
        // Nome file = "{SafeCode} {Priorita}{ext}" — convenzione naming MESManager
        var safeFileName = $"{safeCode} {priorita}{extension}";
        var fullPath = Path.Combine(AllegatiBasePath, safeFileName);
        
        // Assicura che la directory esista
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        // Salva file su disco (FileMode.Create sovrascrive se già esiste stessa priorità)
        await using (var outputStream = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }
        
        _logger.LogInformation("File saved: {Path}", fullPath);
        
        // Crea record nel DB
        var allegato = new AllegatoArticolo
        {
            Archivio = "Articoli",
            IdArchivio = request.IdArchivio,
            CodiceArticolo = request.CodiceArticolo,
            PathFile = fullPath,
            NomeFile = safeFileName,
            Descrizione = request.Descrizione,
            Priorita = priorita,
            TipoFile = isFoto ? "Foto" : "Documento",
            Estensione = extension,
            DimensioneBytes = fileSize,
            DataCreazione = DateTime.Now,
            ImportatoDaGantt = false,
            IdGanttOriginale = null
        };
        
        var saved = await _repository.AddAsync(allegato);
        _logger.LogInformation("Allegato saved with Id={Id}", saved.Id);
        
        return MapToDto(saved);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var allegato = await _repository.GetByIdAsync(id);
        if (allegato == null)
        {
            _logger.LogWarning("DeleteAsync: Allegato Id={Id} not found", id);
            return false;
        }
        
        // Elimina file da disco
        if (File.Exists(allegato.PathFile))
        {
            try
            {
                File.Delete(allegato.PathFile);
                _logger.LogInformation("File deleted: {Path}", allegato.PathFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file: {Path}", allegato.PathFile);
                // Continua comunque con eliminazione dal DB
            }
        }
        
        // Elimina record dal DB
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Allegato deleted: Id={Id}", id);
        
        return true;
    }

    public async Task<bool> UpdatePrioritaAsync(int id, int priorita)
    {
        var allegato = await _repository.GetByIdAsync(id);
        if (allegato == null)
        {
            _logger.LogWarning("UpdatePrioritaAsync: Allegato Id={Id} not found", id);
            return false;
        }
        
        // Rinomina file su disco per riflettere la nuova priorità
        var ext = allegato.Estensione ?? Path.GetExtension(allegato.PathFile ?? "");
        var dir = Path.GetDirectoryName(allegato.PathFile ?? AllegatiBasePath) ?? AllegatiBasePath;
        // Sanitizza codice per nome file (/ e \ sono separatori di path)
        var safeCodeRename = (allegato.CodiceArticolo ?? "").Replace("/", "-").Replace("\\", "-");
        var newFileName = $"{safeCodeRename} {priorita}{ext}";
        var newPath = Path.Combine(dir, newFileName);
        if (!string.IsNullOrEmpty(allegato.PathFile) && allegato.PathFile != newPath && File.Exists(allegato.PathFile))
        {
            File.Move(allegato.PathFile, newPath, overwrite: true);
            allegato.PathFile = newPath;
            allegato.NomeFile = newFileName;
            _logger.LogInformation("File rinominato: {OldPath} -> {NewPath}", allegato.PathFile, newPath);
        }

        allegato.Priorita = priorita;
        await _repository.UpdateAsync(allegato);
        _logger.LogInformation("Allegato priorità updated: Id={Id}, Priorita={Priorita}", id, priorita);
        
        return true;
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetFileContentAsync(int id)
    {
        var allegato = await _repository.GetByIdAsync(id);
        if (allegato == null)
        {
            _logger.LogWarning("GetFileContentAsync: Allegato Id={Id} not found", id);
            return null;
        }
        
        // Converti path di rete P:\Documenti -> C:\Dati\Documenti
        var localPath = ConvertNetworkPath(allegato.PathFile);
        
        if (!File.Exists(localPath))
        {
            _logger.LogWarning("GetFileContentAsync: File not found: {Path} (converted: {LocalPath})", allegato.PathFile, localPath);
            return null;
        }
        
        var content = await File.ReadAllBytesAsync(localPath);
        var contentType = GetMimeType(allegato.Estensione ?? "");
        
        return (content, contentType, allegato.NomeFile ?? "file");
    }
    
    public async Task<ImportAllegatiResult> ImportFromGanttAsync(string codiceArticolo)
    {
        _logger.LogInformation("ImportFromGanttAsync: Importing for {CodiceArticolo}", codiceArticolo);
        return await ImportFromGanttInternalAsync(codiceArticolo);
    }

    public async Task<ImportAllegatiResult> ImportAllFromGanttAsync()
    {
        _logger.LogInformation("ImportAllFromGanttAsync: Starting full import");
        return await ImportFromGanttInternalAsync(null);
    }

    private async Task<ImportAllegatiResult> ImportFromGanttInternalAsync(string? codiceArticolo)
    {
        var result = new ImportAllegatiResult();
        
        try
        {
            await using var connection = new SqlConnection(_ganttConnectionString);
            await connection.OpenAsync();
            
            // Struttura Gantt.Allegati (verificata da query SSMS):
            // - Id (int), Archivio (nvarchar), IdArchivio (nvarchar=IdArticolo), 
            //   Allegato (nvarchar=path), DescrizioneAllegato (nvarchar), Priorita (int)
            // IdArchivio contiene l'ID numerico come stringa che corrisponde a tbArticoli.IdArticolo
            
            // Query con JOIN per ottenere CodiceArticolo da tbArticoli
            var query = @"
                SELECT 
                    a.Id,
                    ISNULL(a.Archivio, 'ARTICO') as Archivio,
                    a.IdArchivio,
                    art.CodiceArticolo,
                    a.Allegato as PathFile,
                    a.DescrizioneAllegato as Descrizione,
                    ISNULL(a.Priorita, 0) as Priorita
                FROM Allegati a
                INNER JOIN tbArticoli art ON a.IdArchivio = CAST(art.IdArticolo AS NVARCHAR(50))
                WHERE a.Archivio = 'ARTICO'";
            
            if (!string.IsNullOrEmpty(codiceArticolo))
            {
                query += " AND art.CodiceArticolo = @CodiceArticolo";
            }
            
            await using var command = new SqlCommand(query, connection);
            
            if (!string.IsNullOrEmpty(codiceArticolo))
            {
                command.Parameters.AddWithValue("@CodiceArticolo", codiceArticolo);
            }
            
            await using var reader = await command.ExecuteReaderAsync();
            
            var allegatiToAdd = new List<AllegatoArticolo>();
            
            while (await reader.ReadAsync())
            {
                try
                {
                    var idGantt = reader.GetInt32(reader.GetOrdinal("Id"));
                    
                    // Verifica se già importato
                    var existing = await _repository.GetByIdGanttOriginaleAsync(idGantt);
                    if (existing != null)
                    {
                        result.TotaleSaltati++;
                        continue;
                    }
                    
                    // In Gantt: Allegato = path completo del file (es: P:\Documenti\...\foto.jpg)
                    var pathFile = reader.GetString(reader.GetOrdinal("PathFile"));
                    var nomeFile = Path.GetFileName(pathFile); // Estrai nome file dal path
                    var extension = Path.GetExtension(nomeFile).ToLowerInvariant();
                    var isFoto = FileConstants.FotoExtensions.Contains(extension);
                    
                    // Determina dimensione file se accessibile
                    long? dimensione = null;
                    if (File.Exists(pathFile))
                    {
                        var fileInfo = new FileInfo(pathFile);
                        dimensione = fileInfo.Length;
                    }
                    
                    var idArchivioOrdinal = reader.GetOrdinal("IdArchivio");
                    // IdArchivio è nvarchar nel DB Gantt (contiene l'ID numerico come stringa)
                    int? idArchivioValue = null;
                    if (!reader.IsDBNull(idArchivioOrdinal))
                    {
                        var idArchivioStr = reader.GetString(idArchivioOrdinal);
                        if (int.TryParse(idArchivioStr, out var parsedId))
                        {
                            idArchivioValue = parsedId;
                        }
                    }
                    
                    // CodiceArticolo dalla JOIN con tbArticoli
                    var codiceArticoloOrdinal = reader.GetOrdinal("CodiceArticolo");
                    var codiceArticoloDb = reader.IsDBNull(codiceArticoloOrdinal) ? "" : reader.GetString(codiceArticoloOrdinal);
                    
                    // Salta se non ha CodiceArticolo
                    if (string.IsNullOrEmpty(codiceArticoloDb))
                    {
                        result.TotaleSaltati++;
                        continue;
                    }
                    
                    // Archivio
                    var archivioOrdinal = reader.GetOrdinal("Archivio");
                    var archivio = reader.IsDBNull(archivioOrdinal) ? "ARTICO" : reader.GetString(archivioOrdinal);
                    var descrizioneOrdinal = reader.GetOrdinal("Descrizione");
                    string? descrizione = reader.IsDBNull(descrizioneOrdinal) ? null : reader.GetString(descrizioneOrdinal);
                    
                    var allegato = new AllegatoArticolo
                    {
                        Archivio = archivio,
                        IdArchivio = idArchivioValue,
                        CodiceArticolo = codiceArticoloDb,
                        PathFile = pathFile,
                        NomeFile = nomeFile,
                        Descrizione = descrizione,
                        Priorita = reader.GetInt32(reader.GetOrdinal("Priorita")),
                        TipoFile = isFoto ? "Foto" : "Documento",
                        Estensione = extension,
                        DimensioneBytes = dimensione,
                        DataImportazione = DateTime.Now,
                        DataCreazione = DateTime.Now,
                        ImportatoDaGantt = true,
                        IdGanttOriginale = idGantt
                    };
                    
                    allegatiToAdd.Add(allegato);
                    result.TotaleImportati++;
                    
                    _logger.LogDebug("Found Gantt allegato: Id={Id}, CodiceArticolo={CodiceArticolo}, Path={Path}", 
                        idGantt, codiceArticoloDb, pathFile);
                }
                catch (Exception ex)
                {
                    result.TotaleErrori++;
                    result.Errori.Add($"Errore lettura record: {ex.Message}");
                    _logger.LogWarning(ex, "Error processing Gantt allegato record");
                }
            }
            
            // Salva in batch
            if (allegatiToAdd.Count > 0)
            {
                await _repository.AddRangeAsync(allegatiToAdd);
                _logger.LogInformation("Imported {Count} allegati from Gantt", allegatiToAdd.Count);
            }
        }
        catch (Exception ex)
        {
            result.TotaleErrori++;
            result.Errori.Add($"Errore connessione Gantt: {ex.Message}");
            _logger.LogError(ex, "Error importing from Gantt");
        }
        
        return result;
    }

    public async Task<int> CountAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Count();
    }

    public async Task<int> CountImportatiDaGanttAsync()
    {
        return await _repository.CountImportatiDaGanttAsync();
    }

    public async Task<Dictionary<string, (int Foto, int Documenti)>> GetConteggioPerArticoloAsync()
    {
        return await _repository.GetConteggioPerArticoloAsync();
    }

    private AllegatoArticoloDto MapToDto(AllegatoArticolo entity)
    {
        return new AllegatoArticoloDto
        {
            Id = entity.Id,
            Archivio = entity.Archivio ?? "",
            IdArchivio = entity.IdArchivio,
            CodiceArticolo = entity.CodiceArticolo ?? "",
            PathFile = entity.PathFile ?? "",
            NomeFile = entity.NomeFile ?? "",
            Descrizione = entity.Descrizione,
            Priorita = entity.Priorita,
            TipoFile = entity.TipoFile ?? "",
            Estensione = entity.Estensione ?? "",
            DimensioneBytes = entity.DimensioneBytes,
            DataCreazione = entity.DataCreazione,
            ImportatoDaGantt = entity.ImportatoDaGantt,
            FileUrl = $"/api/allegati-articoli/{entity.Id}/file"
        };
    }

}
