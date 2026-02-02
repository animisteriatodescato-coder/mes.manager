using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Servizio per generazione PDF preventivo
/// </summary>
public interface IQuotePdfGenerator
{
    /// <summary>
    /// Genera PDF del preventivo
    /// </summary>
    /// <param name="quote">Dati completi del preventivo</param>
    /// <returns>Stream del PDF generato</returns>
    Task<Stream> GenerateAsync(QuoteDto quote);
    
    /// <summary>
    /// Genera PDF e salva su filesystem
    /// </summary>
    /// <param name="quote">Dati completi del preventivo</param>
    /// <param name="outputPath">Path di destinazione</param>
    Task SaveAsync(QuoteDto quote, string outputPath);
}
