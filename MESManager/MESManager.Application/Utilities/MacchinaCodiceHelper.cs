namespace MESManager.Application.Utilities;

/// <summary>
/// Utility centralizzata per convertire codici macchina applicativi.
/// Formati supportati: "M001", "M01", "M1", "001", "1".
/// </summary>
public static class MacchinaCodiceHelper
{
    public static int? ExtractNumero(string? codice)
    {
        if (string.IsNullOrWhiteSpace(codice))
        {
            return null;
        }

        var normalized = codice.Trim();
        if (normalized.StartsWith('M') || normalized.StartsWith('m'))
        {
            normalized = normalized[1..];
        }

        return int.TryParse(normalized, out var numero) ? numero : null;
    }

    public static string FormatNumeroDueCifreOrCodice(string? codice)
    {
        var numero = ExtractNumero(codice);
        return numero.HasValue ? numero.Value.ToString("D2") : codice ?? string.Empty;
    }
}
