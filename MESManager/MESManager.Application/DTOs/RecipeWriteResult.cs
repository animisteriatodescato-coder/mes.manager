namespace MESManager.Application.DTOs;

/// <summary>
/// Risultato operazione scrittura ricetta su DB55 (offset 100+)
/// </summary>
public class RecipeWriteResult
{
    /// <summary>Indica se la scrittura è stata completata con successo</summary>
    public bool Success { get; set; }
    
    /// <summary>Messaggio esplicativo (successo o errore)</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Timestamp scrittura (UTC)</summary>
    public DateTime WriteTimestamp { get; set; }
    
    /// <summary>Numero parametri scritti su DB55 (offset 100+)</summary>
    public int ParametersWritten { get; set; }
    
    /// <summary>Codice articolo della ricetta scritta</summary>
    public string? CodiceArticolo { get; set; }
    
    /// <summary>ID macchina target</summary>
    public Guid? MacchinaId { get; set; }
    
    /// <summary>Messaggio errore dettagliato (se Success = false)</summary>
    public string? ErrorMessage { get; set; }

    // ---- Esito invio PDF via FTP (v1.62.9) ----

    /// <summary>true se il PDF è stato inviato via FTP con successo</summary>
    public bool PdfInviato { get; set; }

    /// <summary>Nome file PDF caricato (es. "7687.pdf"), null se non inviato</summary>
    public string? PdfNomeFile { get; set; }

    /// <summary>SaleOrdId (numero ordine Mago) usato come nome PDF</summary>
    public int PdfSaleOrdId { get; set; }

    /// <summary>Messaggio errore FTP (null se successo o PDF non tentato)</summary>
    public string? PdfErrorMessage { get; set; }
}
