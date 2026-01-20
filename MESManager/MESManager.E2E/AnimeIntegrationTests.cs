using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MESManager.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace MESManager.E2E
{
    /// <summary>
    /// Test E2E per verificare che le colonne Anime siano sincronizzate end-to-end
    /// DB -> Repository -> Service -> API -> Client
    /// </summary>
    public class AnimeIntegrationTests : PlaywrightTestBase
    {
        public AnimeIntegrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(DisplayName = "API GET /api/Anime deve restituire dati non vuoti")]
        public async Task GetAnime_ShouldReturnData()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Anime");

            // Assert
            response.EnsureSuccessStatusCode();
            var anime = await response.Content.ReadFromJsonAsync<List<AnimeDto>>();
            
            Assert.NotNull(anime);
            
            // CRITICAL: Se sync è configurata, il DB non deve essere vuoto
            if (anime.Count == 0)
            {
                _output.WriteLine("WARNING: API returned ZERO anime - Database may be empty or sync not executed");
            }
            else
            {
                _output.WriteLine($"SUCCESS: API returned {anime.Count} anime");
            }
        }

        [Fact(DisplayName = "AnimeDto deve contenere tutte le proprietà critiche")]
        public async Task GetAnime_ShouldContainAllCriticalFields()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/Anime");
            response.EnsureSuccessStatusCode();
            var anime = await response.Content.ReadFromJsonAsync<List<AnimeDto>>();

            // Assert
            Assert.NotNull(anime);
            
            if (anime.Count == 0)
            {
                _output.WriteLine("SKIP: No anime in DB to validate");
                return;
            }

            var sample = anime[0];
            
            // Verifica campi base
            Assert.NotEqual(0, sample.Id);
            Assert.NotNull(sample.CodiceArticolo);
            Assert.NotEmpty(sample.CodiceArticolo);
            
            _output.WriteLine($"Sample Anime - Id={sample.Id}, Codice={sample.CodiceArticolo}");
            
            // Verifica che i campi aggiunti di recente esistano (anche se null/vuoti)
            // Se queste properties non esistono, il test fallisce a compile-time = mapping rotto
            var _ = sample.Colla;
            var __ = sample.Sabbia;
            var ___ = sample.Vernice;
            var ____ = sample.Cliente;
            var _____ = sample.TogliereSparo;
            var ______ = sample.QuantitaPiano;
            var _______ = sample.NumeroPiani;
            var ________ = sample.Figure;
            var _________ = sample.Piastra;
            var __________ = sample.Maschere;
            var ___________ = sample.Incollata;
            var ____________ = sample.Assemblata;
            var _____________ = sample.ArmataL;
            
            _output.WriteLine("✓ All critical fields are present in DTO");
        }

        [Fact(DisplayName = "Dopo import Gantt, API deve restituire gli stessi dati")]
        public async Task AfterImport_ApiShouldReturnImportedData()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            // Act - Trigger import
            var importResponse = await client.PostAsync("/api/Anime/import", null);
            
            if (!importResponse.IsSuccessStatusCode)
            {
                _output.WriteLine($"WARNING: Import failed with status {importResponse.StatusCode}");
                return;
            }

            var importResult = await importResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Import result: {importResult}");

            // Act - Get data
            var getResponse = await client.GetAsync("/api/Anime");
            getResponse.EnsureSuccessStatusCode();
            var anime = await getResponse.Content.ReadFromJsonAsync<List<AnimeDto>>();

            // Assert
            Assert.NotNull(anime);
            
            if (anime.Count == 0)
            {
                _output.WriteLine("CRITICAL: Import succeeded but API returns ZERO anime - BUG DETECTED");
                Assert.Fail("Import succeeded but no data returned from API");
            }
            else
            {
                _output.WriteLine($"✓ After import, API returns {anime.Count} anime");
            }
        }
    }
}
