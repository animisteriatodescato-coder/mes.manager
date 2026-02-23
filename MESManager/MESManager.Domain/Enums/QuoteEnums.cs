namespace MESManager.Domain.Enums;

/// <summary>
/// Stati possibili del preventivo
/// </summary>
public enum QuoteStatus
{
    Draft = 0,      // Bozza
    Sent = 1,       // Inviato al cliente
    Accepted = 2,   // Accettato
    Rejected = 3,   // Rifiutato
    Expired = 4     // Scaduto
}

/// <summary>
/// Tipi di riga preventivo
/// </summary>
public enum QuoteRowType
{
    Manual = 0,         // Riga inserita manualmente
    FromPriceList = 1,  // Riga da listino prezzi
    WorkProcessing = 2  // Riga lavorazione anime con calcolo automatico
}
