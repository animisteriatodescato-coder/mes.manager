using System.Collections.Generic;
using System.Data.SqlClient;
using MESManager.Application.DTOs;

namespace MESManager.Application.Services
{
    public class ArticoloCatalogoService
    {
        private readonly string _connectionString = "Server=192.168.1.230\\SQLEXPRESS;Database=Gantt;User Id=sa;Password=password.123;";

        public List<ArticoloCatalogoDto> GetCatalogoArticoli()
        {
            var result = new List<ArticoloCatalogoDto>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"SELECT [IdArticolo], [CodiceArticolo], [DescrizioneArticolo], [DataModificaRecord], [UnitaMisura], [Larghezza], [Altezza], [Profondita], [Imballo] FROM dbo.tbArticoli", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ArticoloCatalogoDto
                        {
                            IdArticolo = reader.GetInt32(0),
                            CodiceArticolo = reader.IsDBNull(1) ? null : reader.GetString(1),
                            DescrizioneArticolo = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DataModificaRecord = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                            UnitaMisura = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Larghezza = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Altezza = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            Profondita = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                            Imballo = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                        });
                    }
                }
            }
            return result;
        }
    }
}
