using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Application.Services
{
    public class AllegatiAnimaService : IAllegatiAnimaService
    {
        private readonly string _ganttConnectionString = "Server=192.168.1.230\\SQLEXPRESS;Database=Gantt;User Id=sa;Password=password.123;TrustServerCertificate=True;";
        private readonly ILogger<AllegatiAnimaService> _logger;
        
        private static readonly HashSet<string> FotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
        };

        public AllegatiAnimaService(ILogger<AllegatiAnimaService> logger)
        {
            _logger = logger;
        }

        public async Task<AllegatiAnimaResponse> GetAllegatiByIdArchivioAsync(int idArchivio)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("GetAllegatiByIdArchivioAsync START - IdArchivio={IdArchivio}", idArchivio);
            
            var response = new AllegatiAnimaResponse();
            var allegati = new List<AllegatoAnimaDto>();

            try
            {
                using var conn = new SqlConnection(_ganttConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT Id, Archivio, IdArchivio, Allegato, DescrizioneAllegato, Priorita
                    FROM [dbo].[Allegati]
                    WHERE Archivio = 'ARTICO' AND IdArchivio = @IdArchivio
                    ORDER BY Priorita";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdArchivio", idArchivio);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pathCompleto = reader.GetString(3);
                    var nomeFile = Path.GetFileName(pathCompleto);
                    var estensione = Path.GetExtension(nomeFile).ToLowerInvariant();
                    var isFoto = FotoExtensions.Contains(estensione);

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
                    "GetAllegatiByIdArchivioAsync END - IdArchivio={IdArchivio}, Foto={FotoCount}, Documenti={DocCount}, Duration={Duration}ms",
                    idArchivio, response.TotaleFoto, response.TotaleDocumenti, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllegatiByIdArchivioAsync ERROR - IdArchivio={IdArchivio}", idArchivio);
                throw;
            }

            return response;
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
                        IsFoto = FotoExtensions.Contains(estensione)
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
