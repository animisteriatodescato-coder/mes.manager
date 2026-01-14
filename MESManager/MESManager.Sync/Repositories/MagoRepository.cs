using Microsoft.Data.SqlClient;
using MESManager.Sync.Configuration;
using MESManager.Sync.DTO;

namespace MESManager.Sync.Repositories;

public class MagoRepository
{
    private readonly MagoOptions _options;

    public MagoRepository(MagoOptions options)
    {
        _options = options;
    }

    public async Task<List<ClienteMago>> GetClientiAsync(DateTime? lastSync)
    {
        var result = new List<ClienteMago>();

        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
    CAST(CustSupp AS VARCHAR(50)) AS Codice,
    CompanyName AS Nome,
    EMail AS Email,
    Notes AS Note,
    CONVERT(VARCHAR(19), TBModified, 120) AS UltimaModifica
FROM MA_CustSupp
ORDER BY CAST(CustSupp AS VARCHAR(50));
";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var cliente = new ClienteMago
            {
                Codice = reader["Codice"]?.ToString()?.Trim() ?? string.Empty,
                Nome = reader["Nome"]?.ToString()?.Trim() ?? string.Empty,
                Email = reader["Email"]?.ToString()?.Trim() ?? string.Empty,
                Note = reader["Note"]?.ToString()?.Trim() ?? string.Empty,
                UltimaModifica = reader["UltimaModifica"]?.ToString() ?? string.Empty
            };
            result.Add(cliente);
        }

        return result;
    }

    public async Task<List<ArticoloMago>> GetArticoliAsync(DateTime? lastSync)
    {
        var result = new List<ArticoloMago>();

        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
    i.Item AS Codice,
    i.Description AS Descrizione,
    COALESCE(i.BasePrice, 0) AS Prezzo,
    CASE WHEN UPPER(CONVERT(VARCHAR(10), i.IsGood)) IN ('1','Y','TRUE') THEN 1 ELSE 0 END AS Attivo,
    CONVERT(VARCHAR(19), i.TBModified, 120) AS UltimaModifica
FROM MA_Items i
ORDER BY i.Item
";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var articolo = new ArticoloMago
            {
                Codice = reader["Codice"]?.ToString()?.Trim() ?? string.Empty,
                Descrizione = reader["Descrizione"]?.ToString()?.Trim() ?? string.Empty,
                Prezzo = decimal.TryParse(reader["Prezzo"]?.ToString(), out var p) ? p : 0,
                Attivo = reader["Attivo"]?.ToString() == "1",
                UltimaModifica = reader["UltimaModifica"]?.ToString() ?? string.Empty
            };
            result.Add(articolo);
        }

        return result;
    }

    public async Task<List<CommessaMago>> GetCommesseAsync(DateTime? lastSync)
    {
        var result = new List<CommessaMago>();

        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT 
    SO.Delivered,
    SO.Invoiced,
    SO.Customer, 
    SO.SaleOrdId, 
    SOD.Line, 
    SOD.Description, 
    SOD.Item, 
    SOD.Qty, 
    SOD.UoM, 
    SO.InternalOrdNo, 
    SO.ExternalOrdNo, 
    SO.OurReference, 
    SO.YourReference,
    SO.ExpectedDeliveryDate,
    C.CompanyName,
    CONVERT(VARCHAR(19), SO.TBModified, 120) AS TBModified
FROM MA_SaleOrd SO
INNER JOIN MA_SaleOrdDetails SOD ON SO.SaleOrdId = SOD.SaleOrdId
LEFT JOIN MA_CustSupp C ON C.CustSupp = SO.Customer AND C.CustSuppType = 3211264
ORDER BY SO.InternalOrdNo, SOD.Line;
";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var commessa = new CommessaMago
            {
                SaleOrdId = reader["SaleOrdId"]?.ToString()?.Trim() ?? string.Empty,
                InternalOrdNo = reader["InternalOrdNo"]?.ToString()?.Trim() ?? string.Empty,
                ExternalOrdNo = reader["ExternalOrdNo"]?.ToString()?.Trim() ?? string.Empty,
                Customer = reader["Customer"]?.ToString()?.Trim() ?? string.Empty,
                CompanyName = reader["CompanyName"]?.ToString()?.Trim() ?? string.Empty,
                Delivered = reader["Delivered"]?.ToString()?.Trim() ?? string.Empty,
                Invoiced = reader["Invoiced"]?.ToString()?.Trim() ?? string.Empty,
                ExpectedDeliveryDate = reader["ExpectedDeliveryDate"]?.ToString()?.Trim() ?? string.Empty,
                OurReference = reader["OurReference"]?.ToString()?.Trim() ?? string.Empty,
                YourReference = reader["YourReference"]?.ToString()?.Trim() ?? string.Empty,
                Item = reader["Item"]?.ToString()?.Trim() ?? string.Empty,
                Line = reader["Line"]?.ToString()?.Trim() ?? string.Empty,
                Description = reader["Description"]?.ToString()?.Trim() ?? string.Empty,
                Qty = reader["Qty"]?.ToString()?.Trim() ?? string.Empty,
                UoM = reader["UoM"]?.ToString()?.Trim() ?? string.Empty,
                TBModified = reader["TBModified"]?.ToString()?.Trim() ?? string.Empty
            };
            result.Add(commessa);
        }

        return result;
    }
}
