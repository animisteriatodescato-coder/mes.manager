using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using PlcMagoSync.SYNC_MAGO.Config;
using PlcMagoSync.SYNC_MAGO.Models;

namespace PlcMagoSync.SYNC_MAGO.Services
{
    public class MagoDbService
    {
        private readonly string _connectionString;

        public MagoDbService(ConfigMago config)
        {
            _connectionString = config.MagoConnectionString 
                                ?? throw new ArgumentNullException(nameof(config.MagoConnectionString));
        }

        public async Task<List<T>> QueryAsync<T>(string sql, Func<IDataRecord, T> map)
        {
            var result = new List<T>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(map(reader));
                    }
                }
            }

            return result;
        }

        public async Task<List<ArticoloMago>> GetArticoliAsync()
        {
            var sql = @"
                SELECT 
                    i.Item AS Codice,
                    i.Description AS Descrizione,
                    COALESCE(i.BasePrice, 0) AS Prezzo,
                    CASE WHEN UPPER(CONVERT(VARCHAR(10), i.IsGood)) IN ('1','Y','TRUE') THEN 1 ELSE 0 END AS Attivo,
                    CONVERT(VARCHAR(19), i.TBModified, 120) AS UltimaModifica,
                    0 AS StatoCancellato,
                    CONVERT(VARCHAR(19), GETDATE(), 120) AS TimestampSync
                FROM MA_Items i
                ORDER BY i.Item
            ";

            return await QueryAsync(sql, reader => new ArticoloMago
            {
                Codice = reader["Codice"].ToString() ?? "",
                Descrizione = reader["Descrizione"].ToString() ?? "",
                Prezzo = Convert.ToDecimal(reader["Prezzo"] ?? 0),
                Attivo = Convert.ToInt32(reader["Attivo"]) == 1,
                UltimaModifica = reader["UltimaModifica"].ToString() ?? "",
                StatoCancellato = Convert.ToBoolean(reader["StatoCancellato"]),
                TimestampSync = reader["TimestampSync"].ToString() ?? ""
            });
        }
    }
}
