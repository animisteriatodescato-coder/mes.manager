using FluentAssertions;
using MESManager.Application.Utilities;
using Xunit;

namespace MESManager.UnitTests.Utilities;

/// <summary>
/// Test unitari per MacchinaCodiceHelper — parsing e formattazione codici macchina.
/// </summary>
public class MacchinaCodiceHelperTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // ExtractNumero
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData("M001", 1)]
    [InlineData("M01", 1)]
    [InlineData("M1", 1)]
    [InlineData("001", 1)]
    [InlineData("1", 1)]
    [InlineData("m005", 5)]
    [InlineData("M010", 10)]
    [InlineData("10", 10)]
    public void ExtractNumero_CodiciValidi_RestituisceNumero(string codice, int atteso)
    {
        var risultato = MacchinaCodiceHelper.ExtractNumero(codice);
        risultato.Should().Be(atteso);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractNumero_CodiceNullOVuoto_RestituisceNull(string? codice)
    {
        var risultato = MacchinaCodiceHelper.ExtractNumero(codice);
        risultato.Should().BeNull();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData("ABC")]
    [InlineData("Mxyz")]
    [InlineData("M")]
    public void ExtractNumero_CodiceNonNumerico_RestituisceNull(string codice)
    {
        var risultato = MacchinaCodiceHelper.ExtractNumero(codice);
        risultato.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FormatNumeroDueCifreOrCodice
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData("M001", "01")]
    [InlineData("M1", "01")]
    [InlineData("M010", "10")]
    [InlineData("5", "05")]
    [InlineData("M005", "05")]
    public void FormatNumeroDueCifreOrCodice_CodiciNumerici_FormattatoDueCifre(string codice, string atteso)
    {
        var risultato = MacchinaCodiceHelper.FormatNumeroDueCifreOrCodice(codice);
        risultato.Should().Be(atteso);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData("ABC", "ABC")]
    [InlineData("Mxyz", "Mxyz")]
    public void FormatNumeroDueCifreOrCodice_CodiceNonNumerico_RestituisceCodiceOriginale(string codice, string atteso)
    {
        var risultato = MacchinaCodiceHelper.FormatNumeroDueCifreOrCodice(codice);
        risultato.Should().Be(atteso);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Utilities")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void FormatNumeroDueCifreOrCodice_NullOVuoto_RestituisceStringaVuota(string? codice, string atteso)
    {
        var risultato = MacchinaCodiceHelper.FormatNumeroDueCifreOrCodice(codice);
        risultato.Should().Be(atteso);
    }
}
