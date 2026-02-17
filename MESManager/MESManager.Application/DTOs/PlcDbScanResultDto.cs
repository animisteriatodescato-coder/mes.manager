namespace MESManager.Application.DTOs;

/// <summary>
/// Risultato scansione di un singolo DB su un PLC
/// </summary>
public class PlcDbScanResultDto
{
    /// <summary>
    /// Numero del Data Block (es: 1, 52, 55, 100)
    /// </summary>
    public int DbNumber { get; set; }
    
    /// <summary>
    /// Se il DB è accessibile/esistente
    /// </summary>
    public bool Available { get; set; }
    
    /// <summary>
    /// Dimensione in byte del DB (se disponibile)
    /// </summary>
    public int SizeBytes { get; set; }
    
    /// <summary>
    /// Eventuale errore di lettura
    /// </summary>
    public string? Error { get; set; }
}
