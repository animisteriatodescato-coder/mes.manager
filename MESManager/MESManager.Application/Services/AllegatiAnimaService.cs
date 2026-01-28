using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MESManager.Application.Configuration;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Constants;

namespace MESManager.Application.Services
{
    public class AllegatiAnimaService : IAllegatiAnimaService
    {
        private readonly string _ganttConnectionString;
        private readonly ILogger<AllegatiAnimaService> _logger;
        private readonly string _allegatiBasePath;

        public AllegatiAnimaService(ILogger<AllegatiAnimaService> logger, IOptions<DatabaseConfiguration> dbConfig)
        {
            _logger = logger;
            _ganttConnectionString = dbConfig.Value.GanttDb;
            _allegatiBasePath = FileConstants.GetAllegatiBasePath();
            _logger.LogInformation("AllegatiAnimaService initialized. AllegatiBasePath={Path}", _allegatiBasePath);
        }

        public async Task<AllegatiAnimaResponse> GetAllegatiByIdArchivioAsync(int idArchivio)
        {
            return await GetAllegatiInternalAsync(idArchivio, null);
        }

        public async Task<AllegatiAnimaResponse> GetAllegatiByCodiceArticoloAsync(string codiceArticolo)
        {
            // Prova a parsare il codice articolo come intero per cercare per IdArchivio
            if (int.TryParse(codiceArticolo, out var idArchivio))
            {
                return await GetAllegatiInternalAsync(idArchivio, codiceArticolo);
            }
            return await GetAllegatiInternalAsync(null, codiceArticolo);
        }

        private async Task<AllegatiAnimaResponse> GetAllegatiInternalAsync(int? idArchivio, string? codiceArticolo)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("GetAllegatiInternalAsync START - IdArchivio={IdArchivio}, CodiceArticolo={CodiceArticolo}", 
                idArchivio, codiceArticolo);
            
            var response = new AllegatiAnimaResponse();
            var allegati = new List<AllegatoAnimaDto>();

            try
            {
                using var conn = new SqlConnection(_ganttConnectionString);
                await conn.OpenAsync();

                // Cerca per IdArchivio o per CodiceArticolo nel path
                var query = @"
                    SELECT Id, Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita
                    FROM [dbo].[Allegati]
                    WHERE Archivio = 'ARTICO' 
                      AND (IdArchivio = @IdArchivio OR Allegato LIKE @CodicePattern)
                    ORDER BY Priorita";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdArchivio", idArchivio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CodicePattern", $"%{codiceArticolo ?? "NOMATCH"}%");

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pathCompleto = reader.GetString(3);
                    var nomeFile = Path.GetFileName(pathCompleto);
                    var estensione = Path.GetExtension(nomeFile).ToLowerInvariant();
                    var isFoto = FileConstants.FotoExtensions.Contains(estensione);

                    var allegato = new AllegatoAnimaDto
                    {
                        Id = reader.GetInt32(0),
                        Archivio = reader.GetString(1),
                        IdArchivio = reader.GetInt32(2),
                        PathCompleto = pathCompleto,
                        NomeFile = nomeFile,
                        Descrizione = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Priorita = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        Estensione = estensione,
                        IsFoto = isFoto,
                        UrlProxy = $"/api/AllegatiAnima/file/{reader.GetInt32(0)}"
                    };

                    allegati.Add(allegato);
                }

                response.Foto = allegati.Where(a => a.IsFoto).OrderBy(a => a.Priorita).ToList();
                response.Documenti = allegati.Where(a => !a.IsFoto).OrderBy(a => a.Priorita).ToList();

                sw.Stop();
                _logger.LogInformation(
                    "GetAllegatiInternalAsync END - IdArchivio={IdArchivio}, CodiceArticolo={CodiceArticolo}, Foto={FotoCount}, Documenti={DocCount}, Duration={Duration}ms",
                    idArchivio, codiceArticolo, response.TotaleFoto, response.TotaleDocumenti, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllegatiInternalAsync ERROR - IdArchivio={IdArchivio}, CodiceArticolo={CodiceArticolo}", 
                    idArchivio, codiceArticolo);
                throw;
            }

            return response;
        }

        public async Task<AllegatoAnimaDto?> UploadAllegatoAsync(string codiceArticolo, string nomeFile, byte[] contenuto, string? descrizione, bool isFoto)
        {
            _logger.LogInformation("UploadAllegatoAsync START - CodiceArticolo={CodiceArticolo}, NomeFile={NomeFile}, Size={Size}bytes, IsFoto={IsFoto}",
                codiceArticolo, nomeFile, contenuto.Length, isFoto);

            try
            {
                // Genera nome file univoco
                var estensione = Path.GetExtension(nomeFile);
                var nomeFileUnivoco = $"{codiceArticolo}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{estensione}";
                var pathCompleto = Path.Combine(_allegatiBasePath, nomeFileUnivoco);

                // Salva il file su disco
                var directory = Path.GetDirectoryName(pathCompleto);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllBytesAsync(pathCompleto, contenuto);

                _logger.LogInformation("File saved to disk: {Path}", pathCompleto);

                // Inserisci record nel DB
                using var conn = new SqlConnection(_ganttConnectionString);
                await conn.OpenAsync();

                // Ottieni prossima priorità
                var maxPrioritaCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(Priorita), 0) FROM [dbo].[Allegati] WHERE Archivio = 'ARTICO' AND IdArchivio = @IdArchivio",
                    conn);
                
                int.TryParse(codiceArticolo, out var idArchivio);
                maxPrioritaCmd.Parameters.AddWithValue("@IdArchivio", idArchivio);
                var maxPriorita = (int)(await maxPrioritaCmd.ExecuteScalarAsync() ?? 0);

                var insertQuery = @"
                    INSERT INTO [dbo].[Allegati] (Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita)
                    OUTPUT INSERTED.Id
                    VALUES ('ARTICO', @IdArchivio, @Allegato, @Descrizione, @Priorita)";

                using var cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@IdArchivio", idArchivio);
                cmd.Parameters.AddWithValue("@Allegato", pathCompleto);
                cmd.Parameters.AddWithValue("@Descrizione", descrizione ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Priorita", maxPriorita + 1);

                var newId = (int)await cmd.ExecuteScalarAsync();

                _logger.LogInformation("UploadAllegatoAsync END - NewId={NewId}, Path={Path}", newId, pathCompleto);

                return new AllegatoAnimaDto
                {
                    Id = newId,
                    Archivio = "ARTICO",
                    IdArchivio = idArchivio,
                    PathCompleto = pathCompleto,
                    NomeFile = nomeFileUnivoco,
                    Descrizione = descrizione,
                    Priorita = maxPriorita + 1,
                    Estensione = estensione.ToLowerInvariant(),
                    IsFoto = isFoto,
                    UrlProxy = $"/api/AllegatiAnima/file/{newId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadAllegatoAsync ERROR - CodiceArticolo={CodiceArticolo}, NomeFile={NomeFile}", 
                    codiceArticolo, nomeFile);
                throw;
            }
        }

        public async Task<bool> DeleteAllegatoAsync(int id)
        {
            _logger.LogInformation("DeleteAllegatoAsync START - Id={Id}", id);

            try
            {
                // Prima ottieni il path per eliminare il file
                var allegato = await GetAllegatoByIdAsync(id);
                if (allegato == null)
                {
                    _logger.LogWarning("DeleteAllegatoAsync - Allegato not found: Id={Id}", id);
                    return false;
                }

                // Elimina dal DB
                using var conn = new SqlConnection(_ganttConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("DELETE FROM [dbo].[Allegati] WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    // Prova a eliminare il file fisico (non critico se fallisce)
                    try
                    {
                        if (File.Exists(allegato.PathCompleto))
                        {
                            File.Delete(allegato.PathCompleto);
                            _logger.LogInformation("File deleted: {Path}", allegato.PathCompleto);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete physical file: {Path}", allegato.PathCompleto);
                    }
                }

                _logger.LogInformation("DeleteAllegatoAsync END - Id={Id}, Deleted={Deleted}", id, rowsAffected > 0);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAllegatoAsync ERROR - Id={Id}", id);
                throw;
            }
        }

        public async Task<byte[]?> GetFileContentAsync(string path)
        {
            _logger.LogDebug("GetFileContentAsync - Path={Path}", path);
            
            try
            {
                // Converti path di rete se necessario
                var localPath = ConvertNetworkPath(path);
                
                if (!File.Exists(localPath))
                {
                    _logger.LogWarning("File not found: {Path} (converted: {LocalPath})", path, localPath);
                    return null;
                }

                return await File.ReadAllBytesAsync(localPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFileContentAsync ERROR - Path={Path}", path);
                return null;
            }
        }

        public Task<string?> GetFileMimeTypeAsync(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var mimeType = ext switch
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
                _ => "application/octet-stream"
            };
            return Task.FromResult<string?>(mimeType);
        }

        public async Task<AllegatoAnimaDto?> GetAllegatoByIdAsync(int id)
        {
            _logger.LogDebug("GetAllegatoByIdAsync - Id={Id}", id);
            
            try
            {
                using var conn = new SqlConnection(_ganttConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT Id, Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita
                    FROM [dbo].[Allegati]
                    WHERE Id = @Id";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var pathCompleto = reader.GetString(3);
                    var nomeFile = Path.GetFileName(pathCompleto);
                    var estensione = Path.GetExtension(nomeFile).ToLowerInvariant();

                    return new AllegatoAnimaDto
                    {
                        Id = reader.GetInt32(0),
                        Archivio = reader.GetString(1),
                        IdArchivio = reader.GetInt32(2),
                        PathCompleto = pathCompleto,
                        NomeFile = nomeFile,
                        Descrizione = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Priorita = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        Estensione = estensione,
                        IsFoto = FileConstants.FotoExtensions.Contains(estensione)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllegatoByIdAsync ERROR - Id={Id}", id);
                return null;
            }
        }

        private static string ConvertNetworkPath(string path)
        {
            // Il path nel DB è tipo: P:\Documenti\AA SCHEDE PRODUZIONE\foto cel\...
            // Potrebbe essere necessario mapparlo a un percorso UNC o locale
            // Per ora assumiamo che P: sia mappato correttamente sul server
            return path;
        }
    }
}
