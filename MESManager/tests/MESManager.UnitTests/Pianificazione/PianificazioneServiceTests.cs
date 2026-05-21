using FluentAssertions;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Application.Services;
using MESManager.Domain.Constants;
using Moq;
using Xunit;

namespace MESManager.UnitTests.Pianificazione;

/// <summary>
/// Test unitari per PianificazioneService — logica pura, senza DB.
/// </summary>
public class PianificazioneServiceTests
{
    private readonly PianificazioneService _sut;

    public PianificazioneServiceTests()
    {
        var ricettaRepo = new Mock<IRicettaRepository>();
        _sut = new PianificazioneService(ricettaRepo.Object);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalcolaDurataPrevistaMinuti
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDurataPrevistaMinuti_DatiValidi_CalcolaCorretto()
    {
        // 60 s/ciclo × (100 pz / 2 fig) = 50 cicli × 60 s = 3000 s = 50 min + 10 setup = 60
        var risultato = _sut.CalcolaDurataPrevistaMinuti(
            tempoCicloSecondi: 60,
            numeroFigure: 2,
            quantitaRichiesta: 100,
            tempoSetupMinuti: 10);

        risultato.Should().Be(60);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDurataPrevistaMinuti_TempoCicloZero_ReturnDefaultPiuSetup()
    {
        var risultato = _sut.CalcolaDurataPrevistaMinuti(
            tempoCicloSecondi: 0,
            numeroFigure: 1,
            quantitaRichiesta: 50,
            tempoSetupMinuti: 30);

        // Default: 480 min (8h) + 30 setup
        risultato.Should().Be(510);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDurataPrevistaMinuti_NumeroFigureZero_ReturnDefaultPiuSetup()
    {
        var risultato = _sut.CalcolaDurataPrevistaMinuti(
            tempoCicloSecondi: 60,
            numeroFigure: 0,
            quantitaRichiesta: 50,
            tempoSetupMinuti: 0);

        risultato.Should().Be(480);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    [InlineData(120, 1, 1, 0, 2)]    // 120 s / 1 figura = 2 min
    [InlineData(30, 5, 10, 5, 6)]    // 30 s × (10/5) = 60 s = 1 min + 5 setup = 6
    [InlineData(60, 1, 30, 15, 45)]  // 60 s × 30 = 1800 s = 30 min + 15 setup = 45
    public void CalcolaDurataPrevistaMinuti_VariCasi_CalcolaCorretto(
        int tempoCiclo, int figure, int quantita, int setup, int atteso)
    {
        var risultato = _sut.CalcolaDurataPrevistaMinuti(tempoCiclo, figure, quantita, setup);
        risultato.Should().Be(atteso);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CalcolaDataFinePrevistaConFestivi
    // ─────────────────────────────────────────────────────────────────────────

    private static CalendarioLavoroDto CalendarioStandard() => new()
    {
        Lunedi = true, Martedi = true, Mercoledi = true,
        Giovedi = true, Venerdi = true, Sabato = false, Domenica = false,
        OraInizio = new TimeOnly(8, 0),
        OraFine = new TimeOnly(17, 0) // 9h = 540 min
    };

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDataFinePrevista_DurataZero_ReturnDataInizio()
    {
        var inizio = new DateTime(2026, 5, 19, 8, 0, 0); // lunedì
        var fine = _sut.CalcolaDataFinePrevistaConFestivi(inizio, 0, CalendarioStandard(), new HashSet<DateOnly>());

        fine.Should().Be(inizio);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDataFinePrevista_480Minuti_FinisceStessoGiorno()
    {
        // Lunedì 8:00, 480 min (8h) → lunedì 16:00 (dentro orario 8-17)
        var inizio = new DateTime(2026, 5, 18, 8, 0, 0); // lunedì
        var fine = _sut.CalcolaDataFinePrevistaConFestivi(inizio, 480, CalendarioStandard(), new HashSet<DateOnly>());

        fine.Should().Be(new DateTime(2026, 5, 18, 16, 0, 0));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDataFinePrevista_OltreLaGiornata_SaltoWeekend()
    {
        // Venerdì 8:00 + 10h (600 min): 540 min venerdì → 60 min rimanenti → lunedì 9:00
        var inizio = new DateTime(2026, 5, 22, 8, 0, 0); // venerdì
        var fine = _sut.CalcolaDataFinePrevistaConFestivi(inizio, 600, CalendarioStandard(), new HashSet<DateOnly>());

        fine.DayOfWeek.Should().Be(DayOfWeek.Monday);
        fine.Should().Be(new DateTime(2026, 5, 25, 9, 0, 0)); // lunedì 9:00
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    public void CalcolaDataFinePrevista_ConFestivo_SaltaIlGiorno()
    {
        // Lunedì 8:00, festivo martedì, 1 giorno di lavoro (540 min) + 1 min
        // Lunedì esaurisce 540 min, 1 min rimane → martedì è festivo → mercoledì 8:01
        var inizio = new DateTime(2026, 5, 18, 8, 0, 0); // lunedì
        var festivi = new HashSet<DateOnly> { new DateOnly(2026, 5, 19) }; // martedì festivo
        var fine = _sut.CalcolaDataFinePrevistaConFestivi(inizio, 541, CalendarioStandard(), festivi);

        fine.Should().Be(new DateTime(2026, 5, 20, 8, 1, 0)); // mercoledì 8:01
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetColoreStato
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Pianificazione")]
    [InlineData("incorso", "#4CAF50")]
    [InlineData("In Corso", "#4CAF50")]
    [InlineData("inritardo", "#F44336")]
    [InlineData("completata", "#2196F3")]
    [InlineData("pianificata", "#FF9800")]
    [InlineData("sospesa", "#9E9E9E")]
    [InlineData("sconosciuto", "#757575")]
    public void GetColoreStato_MappaCorrettamente(string stato, string coloreAtteso)
    {
        var colore = _sut.GetColoreStato(stato);
        colore.Should().Be(coloreAtteso);
    }
}
