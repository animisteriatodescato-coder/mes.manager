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
            var updatedCount = 0;

            foreach (var articolo in articoli)
            {
                articolo.DataImportazione = DateTime.Now;
                
                // Cerca se esiste già un record con lo stesso CodiceArticolo
                var existingAnime = await _animeRepository.GetByCodiceArticoloAsync(articolo.CodiceArticolo);

                if (existingAnime != null)
                {
                    // Aggiorna il record esistente con tutti i nuovi dati
                    existingAnime.DescrizioneArticolo = articolo.DescrizioneArticolo;
                    existingAnime.DataModificaRecord = articolo.DataModificaRecord;
                    existingAnime.UtenteModificaRecord = articolo.UtenteModificaRecord;
                    existingAnime.Allegato = articolo.Allegato;
                    existingAnime.Larghezza = articolo.Larghezza;
                    existingAnime.Profondita = articolo.Profondita;
                    existingAnime.Imballo = articolo.Imballo;
                    existingAnime.Note = articolo.Note;
                    existingAnime.MacchineSuDisponibili = articolo.MacchineSuDisponibili;
                    existingAnime.Ubicazione = articolo.Ubicazione;
                    existingAnime.Ciclo = articolo.Ciclo;
                    existingAnime.CodiceCassa = articolo.CodiceCassa;
                    existingAnime.CodiceAnime = articolo.CodiceAnime;
                    existingAnime.Altezza = articolo.Altezza;
                    existingAnime.Peso = articolo.Peso;
                    existingAnime.UnitaMisura = articolo.UnitaMisura;
                    existingAnime.TrasmettiTutto = articolo.TrasmettiTutto;
                    
                    // *** AGGIORNA LE NUOVE COLONNE ***
                    existingAnime.Colla = articolo.Colla;
                    existingAnime.Sabbia = articolo.Sabbia;
                    existingAnime.Vernice = articolo.Vernice;
                    existingAnime.Cliente = articolo.Cliente;
                    existingAnime.TogliereSparo = articolo.TogliereSparo;
                    existingAnime.QuantitaPiano = articolo.QuantitaPiano;
                    existingAnime.NumeroPiani = articolo.NumeroPiani;
                    existingAnime.Figure = articolo.Figure;
                    existingAnime.Piastra = articolo.Piastra;
                    existingAnime.Maschere = articolo.Maschere;
                    existingAnime.Incollata = articolo.Incollata;
                    existingAnime.Assemblata = articolo.Assemblata;
                    existingAnime.ArmataL = articolo.ArmataL;
                    
                    existingAnime.IdArticolo = articolo.IdArticolo;
                    existingAnime.DataImportazione = DateTime.Now;
                    
                    await _animeRepository.UpdateAsync(existingAnime);
                    updatedCount++;
                }
                else
                {
                    // Inserisci nuovo record
                    await _animeRepository.AddAsync(articolo);
                    importedCount++;
                }
            }

            return importedCount + updatedCount;
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
                        0 as [TrasmettiTutto],
                        ISNULL([Colla], '') as [Colla],
                        ISNULL([Sabbia], '') as [Sabbia],
                        ISNULL([Vernice], '') as [Vernice],
                        ISNULL([Cliente], '') as [Cliente],
                        ISNULL([TogliereSparo], '') as [TogliereSparo],
                        [QuantitaPiano],
                        [NumeroPiani],
                        ISNULL([Figure], '') as [Figure],
                        ISNULL([Piastra], '') as [Piastra],
                        ISNULL([Maschere], '') as [Maschere],
                        ISNULL([Incollata], '') as [Incollata],
                        ISNULL([Assemblata], '') as [Assemblata],
                        ISNULL([ArmataL], '') as [ArmataL]
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
                            UtenteModificaRecord = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString(),
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
                            TrasmettiTutto = reader.IsDBNull(18) ? false : ParseBoolean(reader.GetValue(18)),
                            Colla = reader.IsDBNull(19) ? null : reader.GetValue(19).ToString(),
                            Sabbia = reader.IsDBNull(20) ? null : reader.GetValue(20).ToString(),
                            Vernice = reader.IsDBNull(21) ? null : reader.GetValue(21).ToString(),
                            Cliente = reader.IsDBNull(22) ? null : reader.GetValue(22).ToString(),
                            TogliereSparo = reader.IsDBNull(23) ? null : reader.GetValue(23).ToString(),
                            QuantitaPiano = reader.IsDBNull(24) ? null : ParseInt32(reader.GetValue(24)),
                            NumeroPiani = reader.IsDBNull(25) ? null : ParseInt32(reader.GetValue(25)),
                            Figure = reader.IsDBNull(26) ? null : reader.GetValue(26).ToString(),
                            Piastra = reader.IsDBNull(27) ? null : reader.GetValue(27).ToString(),
                            Maschere = reader.IsDBNull(28) ? null : reader.GetValue(28).ToString(),
                            Incollata = reader.IsDBNull(29) ? null : reader.GetValue(29).ToString(),
                            Assemblata = reader.IsDBNull(30) ? null : reader.GetValue(30).ToString(),
                            ArmataL = reader.IsDBNull(31) ? null : reader.GetValue(31).ToString()
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
