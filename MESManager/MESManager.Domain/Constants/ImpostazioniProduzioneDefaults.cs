namespace MESManager.Domain.Constants;

/// <summary>
/// Default applicativi per la pianificazione produzione.
/// Fonte unica per evitare divergenze tra Domain, DTO, Web e Infrastructure.
/// </summary>
public static class ImpostazioniProduzioneDefaults
{
    public const int TempoSetupMinuti = 90;
    public const int OreLavorativeGiornaliere = 8;
    public const int GiorniLavorativiSettimanali = 5;
}
