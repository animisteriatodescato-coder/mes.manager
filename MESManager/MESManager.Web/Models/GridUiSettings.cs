namespace MESManager.Web.Models;

public class GridUiSettings
{
    public int FontSize { get; set; } = 14;
    public int RowHeight { get; set; } = 35;
    public string Density { get; set; } = "Normal";
    public bool WrapDescrizione { get; set; } = false;
    public bool Zebra { get; set; } = true;
    public bool GridLines { get; set; } = true;
    public string? ColumnStateJson { get; set; }
    public string GlobalSearch { get; set; } = string.Empty;

    /// <summary>Restituisce il padding CSS orizzontale per la densità scelta.</summary>
    public string GetDensityPadding() => Density switch
    {
        "Compact"     => "4px",
        "Comfortable" => "12px",
        _             => "8px"
    };
}
