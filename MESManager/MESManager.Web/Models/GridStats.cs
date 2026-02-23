namespace MESManager.Web.Models;

/// <summary>
/// Statistiche riga condivise tra tutti i catalog grid.
///映射 del ritorno JSON di agGridFactory.getStats()
/// </summary>
public class GridStats
{
    public int Total { get; set; }
    public int Filtered { get; set; }
    public int Selected { get; set; }
}
