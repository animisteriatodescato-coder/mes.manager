namespace MESManager.Domain.Constants;

/// <summary>
/// Default applicativi del calendario lavoro.
/// Fonte unica per evitare divergenze tra Domain, DTO, Web, test e Infrastructure.
/// </summary>
public static class CalendarioLavoroDefaults
{
    public const bool Lunedi = true;
    public const bool Martedi = true;
    public const bool Mercoledi = true;
    public const bool Giovedi = true;
    public const bool Venerdi = true;
    public const bool Sabato = false;
    public const bool Domenica = false;

    public static readonly TimeOnly OraInizio = new(8, 0);
    public static readonly TimeOnly OraFine = new(17, 0);
}
