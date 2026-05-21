using FluentAssertions;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using MESManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MESManager.IntegrationTests.Repositories;

/// <summary>
/// Test di integrazione per RicettaRepository — usa EF Core InMemory, nessun DB reale.
/// </summary>
public class RicettaRepositoryTests : IDisposable
{
    private readonly MesManagerDbContext _context;
    private readonly RicettaRepository _sut;

    public RicettaRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MesManagerDbContext>()
            .UseInMemoryDatabase(databaseName: $"RicettaRepo_{Guid.NewGuid()}")
            .Options;

        _context = new MesManagerDbContext(options);
        _sut = new RicettaRepository(_context, NullLogger<RicettaRepository>.Instance);
    }

    public void Dispose() => _context.Dispose();

    // ─────────────────────────────────────────────────────────────────────────
    // GetArticoliConRicettaAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoliConRicettaAsync_SenzaRicette_RestituisceListaVuota()
    {
        _context.Articoli.Add(new Articolo { Id = Guid.NewGuid(), Codice = "ART-001", Descrizione = "Test" });
        await _context.SaveChangesAsync();

        var risultato = await _sut.GetArticoliConRicettaAsync();

        risultato.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoliConRicettaAsync_ConRicetta_RestituisceArticolo()
    {
        var articolo = SeedArticoloConRicetta("ART-002", "Articolo con ricetta");
        await _context.SaveChangesAsync();

        var risultato = await _sut.GetArticoliConRicettaAsync();

        risultato.Should().HaveCount(1);
        risultato[0].Codice.Should().Be("ART-002");
        risultato[0].Ricetta.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoliConRicettaAsync_FiltroSearchTerm_FiltroPerCodice()
    {
        SeedArticoloConRicetta("ART-AAA", "Alpha");
        SeedArticoloConRicetta("ART-BBB", "Beta");
        await _context.SaveChangesAsync();

        var risultato = await _sut.GetArticoliConRicettaAsync(searchTerm: "AAA");

        risultato.Should().HaveCount(1);
        risultato[0].Codice.Should().Be("ART-AAA");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoliConRicettaAsync_MaxResults_LimitaRisultati()
    {
        for (var i = 1; i <= 5; i++)
            SeedArticoloConRicetta($"ART-{i:D3}", $"Articolo {i}");
        await _context.SaveChangesAsync();

        var risultato = await _sut.GetArticoliConRicettaAsync(maxResults: 3);

        risultato.Should().HaveCount(3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetArticoloConRicettaByCodeAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoloConRicettaByCodeAsync_CodiceEsistente_RestituisceArticolo()
    {
        SeedArticoloConRicetta("ART-XYZ", "Articolo XYZ");
        await _context.SaveChangesAsync();

        var risultato = await _sut.GetArticoloConRicettaByCodeAsync("ART-XYZ");

        risultato.Should().NotBeNull();
        risultato!.Codice.Should().Be("ART-XYZ");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task GetArticoloConRicettaByCodeAsync_CodiceInesistente_RestituisceNull()
    {
        var risultato = await _sut.GetArticoloConRicettaByCodeAsync("NIENTE");

        risultato.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CountArticoliConRicettaAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task CountArticoliConRicettaAsync_ConRicette_ContaCorrettamente()
    {
        SeedArticoloConRicetta("ART-C1", "C1");
        SeedArticoloConRicetta("ART-C2", "C2");
        _context.Articoli.Add(new Articolo { Id = Guid.NewGuid(), Codice = "ART-C3", Descrizione = "no ricetta" });
        await _context.SaveChangesAsync();

        var count = await _sut.CountArticoliConRicettaAsync();

        count.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateValoreParametroAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task UpdateValoreParametroAsync_ParametroEsistente_AggiornataERestituisceTrue()
    {
        var articolo = SeedArticoloConRicetta("ART-UPD", "Update test");
        var parametro = new ParametroRicetta
        {
            Id = Guid.NewGuid(),
            RicettaId = articolo.Ricetta!.Id,
            NomeParametro = "Temperatura",
            Valore = "100"
        };
        articolo.Ricetta.Parametri.Add(parametro);
        await _context.SaveChangesAsync();

        var successo = await _sut.UpdateValoreParametroAsync(parametro.Id, 150);

        successo.Should().BeTrue();
        var aggiornato = await _context.ParametriRicetta.FindAsync(parametro.Id);
        aggiornato!.Valore.Should().Be("150");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "Ricette")]
    public async Task UpdateValoreParametroAsync_ParametroInesistente_RestituisceFalse()
    {
        var successo = await _sut.UpdateValoreParametroAsync(Guid.NewGuid(), 99);

        successo.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper seed
    // ─────────────────────────────────────────────────────────────────────────

    private Articolo SeedArticoloConRicetta(string codice, string descrizione)
    {
        var articolo = new Articolo
        {
            Id = Guid.NewGuid(),
            Codice = codice,
            Descrizione = descrizione,
            UltimaModifica = DateTime.UtcNow
        };
        articolo.Ricetta = new Ricetta
        {
            Id = Guid.NewGuid(),
            ArticoloId = articolo.Id,
            Articolo = articolo
        };
        _context.Articoli.Add(articolo);
        return articolo;
    }
}
