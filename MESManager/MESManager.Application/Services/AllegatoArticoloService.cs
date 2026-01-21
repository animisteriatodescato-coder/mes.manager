using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services;

/// <summary>
/// Service per la gestione degli allegati articolo con storage locale
/// </summary>
public class AllegatoArticoloService : IAllegatoArticoloService
{
    private readonly IAllegatoArticoloRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AllegatoArticoloService> _logger;
    private readonly string _allegatiBasePath;
    private readonly string _ganttConnectionString;

    // Estensioni considerate foto
    private static readonly HashSet<string> FotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    public AllegatoArticoloService(
        IAllegatoArticoloRepository repository,
        IConfiguration configuration,
        ILogger<AllegatoArticoloService> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
        
        // Path base per gli allegati - con fallback locale
        _allegatiBasePath = configuration["AllegatiBasePath"] 
            ?? @"P:\Documenti\AA SCHEDE PRODUZIONE\foto cel";
        
        // Se il path di rete non è accessibile, usa cartella locale
        if (!Directory.Exists(_allegatiBasePath))
        {
            _allegatiBasePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "allegati");
            Directory.CreateDirectory(_allegatiBasePath);
        }
        
        // Connection string per Gantt - usa SQL Authentication
        _ganttConnectionString = configuration.GetConnectionString("GanttConnection") 
            ?? "Server=192.168.1.230\\SQLEXPRESS;Database=Gantt;User Id=sa;Password=password.123;TrustServerCertificate=True;";
        
        _logger.LogInformation("AllegatoArticoloService initialized. BasePath={Path}", _allegatiBasePath);
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
        var isFoto = FotoExtensions.Contains(extension);
        
        // Genera nome file univoco
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..16];
        var safeFileName = $"{request.CodiceArticolo}_{timestamp}_{guid}{extension}";
        var fullPath = Path.Combine(_allegatiBasePath, safeFileName);
        
        // Salva file su disco
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
            NomeFile = fileName,
            Descrizione = request.Descrizione,
            Priorita = request.Priorita,
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

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetFileContentAsync(int id)
    {
        var allegato = await _repository.GetByIdAsync(id);
        if (allegato == null)
        {
            _logger.LogWarning("GetFileContentAsync: Allegato Id={Id} not found", id);
            return null;
        }
        
        if (!File.Exists(allegato.PathFile))
        {
            _logger.LogWarning("GetFileContentAsync: File not found: {Path}", allegato.PathFile);
            return null;
        }
        
        var content = await File.ReadAllBytesAsync(allegato.PathFile);
        var contentType = GetContentType(allegato.Estensione);
        
        return (content, contentType, allegato.NomeFile);
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
                    var isFoto = FotoExtensions.Contains(extension);
                    
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

    private AllegatoArticoloDto MapToDto(AllegatoArticolo entity)
    {
        return new AllegatoArticoloDto
        {
            Id = entity.Id,
            Archivio = entity.Archivio,
            IdArchivio = entity.IdArchivio,
            CodiceArticolo = entity.CodiceArticolo,
            PathFile = entity.PathFile,
            NomeFile = entity.NomeFile,
            Descrizione = entity.Descrizione,
            Priorita = entity.Priorita,
            TipoFile = entity.TipoFile,
            Estensione = entity.Estensione,
            DimensioneBytes = entity.DimensioneBytes,
            DataCreazione = entity.DataCreazione,
            ImportatoDaGantt = entity.ImportatoDaGantt,
            FileUrl = $"/api/allegati-articoli/{entity.Id}/file"
        };
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".tiff" or ".tif" => "image/tiff",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
