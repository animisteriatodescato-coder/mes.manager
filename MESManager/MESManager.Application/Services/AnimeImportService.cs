using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;

namespace MESManager.Application.Services
{
    public class AnimeImportService
    {
        private readonly string _ganttConnectionString = "Server=192.168.1.230\\SQLEXPRESS;Database=Gantt;User Id=sa;Password=password.123;TrustServerCertificate=True;";
        private readonly IAnimeRepository _animeRepository;

        public AnimeImportService(IAnimeRepository animeRepository)
        {
            _animeRepository = animeRepository;
        }

        public async Task<int> ImportFromGanttAsync()
        {
            var articoli = await ReadArticoliFromGanttAsync();
            var importedCount = 0;

            foreach (var articolo in articoli)
            {
                articolo.DataImportazione = DateTime.Now;
                await _animeRepository.AddAsync(articolo);
                importedCount++;
            }

            return importedCount;
        }

        private async Task<List<Anime>> ReadArticoliFromGanttAsync()
        {
            var result = new List<Anime>();
            
            using (var conn = new SqlConnection(_ganttConnectionString))
            {
                await conn.OpenAsync();
                
                var query = @"
                    SELECT TOP (1000) 
                        [IdArticolo],
                        [CodiceArticolo],
                        [DescrizioneArticolo],
                        [DataModificaRecord],
                        [UtenteModificaRecord],
                        [Allegato],
                        [Larghezza],
                        [Profondita],
                        [Imballo],
                        [Note],
                        [MacchineDisponibili],
                        [Ubicazione],
                        [Ciclo],
                        [CodiceCassa],
                        [CodiceArticolo] as [CodiceAnime],
                        [Altezza],
                        [Peso],
                        [UnitaMisura],
                        0 as [TrasmettiTutto]
                    FROM [dbo].[tbArticoli]
                    ORDER BY [IdArticolo]";
                
                var cmd = new SqlCommand(query, conn);
                
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var anime = new Anime
                        {
                            IdArticolo = reader.IsDBNull(0) ? null : ParseInt32(reader.GetValue(0)),
                            CodiceArticolo = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1).ToString() ?? string.Empty,
                            DescrizioneArticolo = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2).ToString() ?? string.Empty,
                            DataModificaRecord = reader.IsDBNull(3) ? null : ParseDateTime(reader.GetValue(3)),
                            UtenteModificaRecord = reader.IsDBNull(4) ? null : ParseDateTime(reader.GetValue(4)),
                            Allegato = reader.IsDBNull(5) ? null : reader.GetValue(5).ToString(),
                            Larghezza = reader.IsDBNull(6) ? null : ParseInt32(reader.GetValue(6)),
                            Profondita = reader.IsDBNull(7) ? null : ParseInt32(reader.GetValue(7)),
                            Imballo = reader.IsDBNull(8) ? null : ParseInt32(reader.GetValue(8)),
                            Note = reader.IsDBNull(9) ? null : reader.GetValue(9).ToString(),
                            MacchineSuDisponibili = reader.IsDBNull(10) ? null : reader.GetValue(10).ToString(),
                            Ubicazione = reader.IsDBNull(11) ? null : reader.GetValue(11).ToString(),
                            Ciclo = reader.IsDBNull(12) ? null : reader.GetValue(12).ToString(),
                            CodiceCassa = reader.IsDBNull(13) ? null : reader.GetValue(13).ToString(),
                            CodiceAnime = reader.IsDBNull(14) ? null : reader.GetValue(14).ToString(),
                            Altezza = reader.IsDBNull(15) ? null : ParseInt32(reader.GetValue(15)),
                            Peso = reader.IsDBNull(16) ? null : reader.GetValue(16).ToString(),
                            UnitaMisura = reader.IsDBNull(17) ? null : reader.GetValue(17).ToString(),
                            TrasmettiTutto = reader.IsDBNull(18) ? false : ParseBoolean(reader.GetValue(18))
                        };
                        
                        result.Add(anime);
                    }
                }
            }
            
            return result;
        }

        private static int? ParseInt32(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
                
            if (int.TryParse(value.ToString(), out int result))
                return result;
                
            return null;
        }

        private static DateTime? ParseDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
                
            if (DateTime.TryParse(value.ToString(), out DateTime result))
                return result;
                
            return null;
        }

        private static bool ParseBoolean(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;
                
            if (bool.TryParse(value.ToString(), out bool result))
                return result;
                
            // Prova anche con conversioni numeriche (0/1)
            if (int.TryParse(value.ToString(), out int intResult))
                return intResult != 0;
                
            return false;
        }
    }
}
