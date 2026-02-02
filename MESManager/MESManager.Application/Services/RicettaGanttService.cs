using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MESManager.Application.Configuration;
using MESManager.Application.DTOs;

namespace MESManager.Application.Services;

/// <summary>
/// Servizio per leggere le ricette articoli dal database MESManager
/// Legge dalla tabella [dbo].[ArticoliRicetta]
/// </summary>
public class RicettaGanttService
{
    private readonly string _connectionString;
    private readonly ILogger<RicettaGanttService> _logger;

    public RicettaGanttService(
        ILogger<RicettaGanttService> logger,
        IOptions<DatabaseConfiguration> dbConfig)
    {
        _logger = logger;
        _connectionString = dbConfig.Value.MESManagerDb;
    }

    /// <summary>
    /// Ottiene tutti i parametri della ricetta per un articolo specifico
    /// </summary>
    public async Task<RicettaArticoloDto?> GetRicettaByCodiceArticoloAsync(string codiceArticolo)
    {
        if (string.IsNullOrWhiteSpace(codiceArticolo))
            return null;

        _logger.LogInformation("GetRicettaByCodiceArticoloAsync: {CodiceArticolo}", codiceArticolo);

        try
        {
            var parametri = new List<ParametroRicettaArticoloDto>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT 
                    [IdRigaRicetta],
                    [CodiceArticolo],
                    [CodiceParametro],
                    [DescrizioneParametro],
                    [Indirizzo],
                    [Area],
                    [Tipo],
                    [UM],
                    [Valore]
                FROM [dbo].[ArticoliRicetta]
                WHERE [CodiceArticolo] = @CodiceArticolo
                ORDER BY [CodiceParametro]";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CodiceArticolo", codiceArticolo);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                parametri.Add(new ParametroRicettaArticoloDto
                {
                    IdRigaRicetta = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                    CodiceArticolo = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty,
                    CodiceParametro = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2)),
                    DescrizioneParametro = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3)?.ToString() ?? string.Empty,
                    Indirizzo = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetValue(4)),
                    Area = reader.IsDBNull(5) ? string.Empty : reader.GetValue(5)?.ToString() ?? string.Empty,
                    Tipo = reader.IsDBNull(6) ? string.Empty : reader.GetValue(6)?.ToString() ?? string.Empty,
                    UM = reader.IsDBNull(7) ? null : reader.GetValue(7)?.ToString(),
                    Valore = reader.IsDBNull(8) ? 0 : Convert.ToInt32(reader.GetValue(8))
                });
            }

            if (parametri.Count == 0)
            {
                _logger.LogWarning("Nessun parametro trovato per articolo {CodiceArticolo}", codiceArticolo);
                return null;
            }

            // Cerca descrizione articolo dalla tabella tbArticoli
            var descrizioneArticolo = await GetDescrizioneArticoloAsync(conn, codiceArticolo);

            return new RicettaArticoloDto
            {
                CodiceArticolo = codiceArticolo,
                DescrizioneArticolo = descrizioneArticolo,
                Parametri = parametri
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura ricetta per {CodiceArticolo}", codiceArticolo);
            throw;
        }
    }

    /// <summary>
    /// Cerca ricette per codice articolo (ricerca parziale)
    /// </summary>
    public async Task<RicetteSearchResponse> SearchRicetteAsync(string? searchTerm, int maxResults = 50)
    {
        _logger.LogInformation("SearchRicetteAsync: term={SearchTerm}, max={MaxResults}", searchTerm, maxResults);

        var response = new RicetteSearchResponse();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Prima ottieni la lista dei codici articolo distinti che hanno ricette
            var queryDistinct = @"
                SELECT DISTINCT TOP (@MaxResults) [CodiceArticolo]
                FROM [dbo].[ArticoliRicetta]
                WHERE @SearchTerm IS NULL OR @SearchTerm = '' OR [CodiceArticolo] LIKE @SearchPattern
                ORDER BY [CodiceArticolo]";

            var codiciArticolo = new List<string>();

            await using (var cmdDistinct = new SqlCommand(queryDistinct, conn))
            {
                cmdDistinct.Parameters.AddWithValue("@MaxResults", maxResults);
                cmdDistinct.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
                cmdDistinct.Parameters.AddWithValue("@SearchPattern", $"%{searchTerm ?? ""}%");

                await using var reader = await cmdDistinct.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        codiciArticolo.Add(reader.GetString(0));
                }
            }

            // Per ogni codice articolo, carica i parametri
            foreach (var codice in codiciArticolo)
            {
                var ricetta = await GetRicettaByCodiceArticoloAsync(codice);
                if (ricetta != null)
                {
                    response.Ricette.Add(ricetta);
                    response.TotaleParametri += ricetta.TotaleParametri;
                }
            }

            response.TotaleRicette = response.Ricette.Count;

            _logger.LogInformation("SearchRicetteAsync: trovate {Count} ricette con {Params} parametri totali",
                response.TotaleRicette, response.TotaleParametri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante ricerca ricette");
            throw;
        }

        return response;
    }

    /// <summary>
    /// Ottiene la lista di tutti i codici articolo che hanno ricette
    /// </summary>
    public async Task<List<string>> GetCodiciArticoloConRicetteAsync()
    {
        var result = new List<string>();

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT DISTINCT [CodiceArticolo]
                FROM [dbo].[ArticoliRicetta]
                ORDER BY [CodiceArticolo]";

            await using var cmd = new SqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetString(0));
            }

            _logger.LogInformation("GetCodiciArticoloConRicetteAsync: trovati {Count} articoli con ricette", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura codici articolo");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Conta il totale delle ricette (articoli distinti con parametri)
    /// </summary>
    public async Task<int> CountRicetteAsync()
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT COUNT(DISTINCT [CodiceArticolo]) FROM [dbo].[ArticoliRicetta]";
            await using var cmd = new SqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante conteggio ricette");
            return 0;
        }
    }

    private async Task<string?> GetDescrizioneArticoloAsync(SqlConnection conn, string codiceArticolo)
    {
        try
        {
            var query = @"
                SELECT [DescrizioneArticolo]
                FROM [dbo].[tbArticoliGantt]
                WHERE [CodiceArticolo] = @CodiceArticolo";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CodiceArticolo", codiceArticolo);

            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch
        {
            // Se fallisce, ritorna null silenziosamente
            return null;
        }
    }
}
