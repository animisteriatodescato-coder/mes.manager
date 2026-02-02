using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace MESManager.Application.Services;

/// <summary>
/// Generatore PDF per preventivi usando QuestPDF
/// </summary>
public class QuotePdfGenerator : IQuotePdfGenerator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuotePdfGenerator> _logger;
    
    // Configurazione brand aziendale
    private readonly string _companyName;
    private readonly string _companyAddress;
    private readonly string _companyPhone;
    private readonly string _companyEmail;
    private readonly string _companyVat;
    
    // Cultura per formattazione valute
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");

    public QuotePdfGenerator(IConfiguration configuration, ILogger<QuotePdfGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Configura licenza QuestPDF (Community gratuita)
        QuestPDF.Settings.License = LicenseType.Community;
        
        // Leggi configurazione brand
        _companyName = configuration["Company:Name"] ?? "Todescato S.r.l.";
        _companyAddress = configuration["Company:Address"] ?? "Via Esempio 123, 36100 Vicenza (VI)";
        _companyPhone = configuration["Company:Phone"] ?? "+39 0444 123456";
        _companyEmail = configuration["Company:Email"] ?? "info@todescato.it";
        _companyVat = configuration["Company:VAT"] ?? "IT01234567890";
    }

    public Task<Stream> GenerateAsync(QuoteDto quote)
    {
        _logger.LogInformation("Generazione PDF per preventivo {Number}", quote.Number);
        
        var document = CreateDocument(quote);
        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;
        
        return Task.FromResult<Stream>(stream);
    }

    public async Task SaveAsync(QuoteDto quote, string outputPath)
    {
        _logger.LogInformation("Salvataggio PDF preventivo {Number} in {Path}", quote.Number, outputPath);
        
        var document = CreateDocument(quote);
        document.GeneratePdf(outputPath);
        
        await Task.CompletedTask;
    }

    private Document CreateDocument(QuoteDto quote)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComposeHeader(c, quote));
                page.Content().Element(c => ComposeContent(c, quote));
                page.Footer().Element(c => ComposeFooter(c, quote));
            });
        });
    }

    private void ComposeHeader(IContainer container, QuoteDto quote)
    {
        container.Column(column =>
        {
            // Intestazione azienda
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(_companyName)
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(_companyAddress).FontSize(8);
                    col.Item().Text($"Tel: {_companyPhone} | Email: {_companyEmail}").FontSize(8);
                    col.Item().Text($"P.IVA: {_companyVat}").FontSize(8);
                });
                
                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text("PREVENTIVO").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text(quote.Number).FontSize(14).Bold();
                });
            });
            
            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            
            // Info preventivo e cliente
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CLIENTE").FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(quote.ClienteName ?? "Cliente non specificato").Bold();
                    if (!string.IsNullOrEmpty(quote.ContactName))
                    {
                        col.Item().Text($"Att.ne: {quote.ContactName}").FontSize(9);
                    }
                });
                
                row.ConstantItem(180).AlignRight().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(80).Text("Data:").FontSize(9);
                        r.RelativeItem().AlignRight().Text(quote.Date.ToString("dd/MM/yyyy")).FontSize(9);
                    });
                    
                    if (quote.ValidUntil.HasValue)
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(80).Text("Validità:").FontSize(9);
                            r.RelativeItem().AlignRight().Text(quote.ValidUntil.Value.ToString("dd/MM/yyyy")).FontSize(9);
                        });
                    }
                    
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(80).Text("Stato:").FontSize(9);
                        r.RelativeItem().AlignRight().Text(quote.Status.ToString()).FontSize(9);
                    });
                });
            });
            
            column.Item().PaddingTop(10);
        });
    }

    private void ComposeContent(IContainer container, QuoteDto quote)
    {
        container.Column(column =>
        {
            // Tabella righe
            column.Item().Table(table =>
            {
                // Definizione colonne
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);  // Codice
                    columns.RelativeColumn(3);   // Descrizione
                    columns.ConstantColumn(40);  // Qta
                    columns.ConstantColumn(30);  // UM
                    columns.ConstantColumn(70);  // Prezzo Unit.
                    columns.ConstantColumn(40);  // Sconto
                    columns.ConstantColumn(70);  // Totale
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                        .Text("Codice").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                        .Text("Descrizione").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("Qtà").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter()
                        .Text("UM").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("€ Unit.").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("Sc.%").FontColor(Colors.White).FontSize(9).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("Totale").FontColor(Colors.White).FontSize(9).Bold();
                });
                
                // Righe
                bool alternate = false;
                foreach (var row in quote.Rows)
                {
                    var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                    alternate = !alternate;
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(row.Code ?? "").FontSize(8);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(row.Description).FontSize(9);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).AlignRight().Text(row.Quantity.ToString("N2", ItalianCulture)).FontSize(9);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).AlignCenter().Text(row.Unit ?? "").FontSize(8);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).AlignRight().Text(row.UnitPrice.ToString("N2", ItalianCulture)).FontSize(9);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).AlignRight().Text(row.DiscountPercent > 0 ? row.DiscountPercent.ToString("N1") : "-").FontSize(8);
                    
                    table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).AlignRight().Text(row.RowTotal.ToString("N2", ItalianCulture)).FontSize(9).Bold();
                }
            });
            
            // Spazio
            column.Item().PaddingTop(15);
            
            // Totali
            column.Item().AlignRight().Width(250).Table(totalsTable =>
            {
                totalsTable.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.ConstantColumn(100);
                });
                
                // Subtotale
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).Text("Subtotale:").FontSize(9);
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).AlignRight().Text(quote.TotalNet.ToString("C2", ItalianCulture)).FontSize(9);
                
                // Sconto totale (se presente)
                if (quote.DiscountTotal > 0)
                {
                    totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .Padding(3).Text("Sconto:").FontSize(9);
                    totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .Padding(3).AlignRight().Text($"-{quote.DiscountTotal.ToString("C2", ItalianCulture)}").FontSize(9).FontColor(Colors.Red.Darken1);
                }
                
                // Imponibile
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).Text("Imponibile:").FontSize(9);
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).AlignRight().Text(quote.TotalNet.ToString("C2", ItalianCulture)).FontSize(9);
                
                // IVA
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).Text("IVA:").FontSize(9);
                totalsTable.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                    .Padding(3).AlignRight().Text(quote.TotalVat.ToString("C2", ItalianCulture)).FontSize(9);
                
                // Totale
                totalsTable.Cell().Background(Colors.Blue.Darken2).Padding(5)
                    .Text("TOTALE:").FontSize(11).Bold().FontColor(Colors.White);
                totalsTable.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                    .Text(quote.TotalGross.ToString("C2", ItalianCulture)).FontSize(11).Bold().FontColor(Colors.White);
            });
            
            // Condizioni pagamento
            if (!string.IsNullOrEmpty(quote.PaymentTerms))
            {
                column.Item().PaddingTop(20).Column(c =>
                {
                    c.Item().Text("CONDIZIONI DI PAGAMENTO").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(3).Text(quote.PaymentTerms).FontSize(9);
                });
            }
            
            // Note
            if (!string.IsNullOrEmpty(quote.Notes))
            {
                column.Item().PaddingTop(15).Column(c =>
                {
                    c.Item().Text("NOTE").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(3).Text(quote.Notes).FontSize(9);
                });
            }
        });
    }

    private void ComposeFooter(IContainer container, QuoteDto quote)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Documento generato il ").FontSize(7).FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor(Colors.Grey.Medium);
                    
                    if (!string.IsNullOrEmpty(quote.PriceListVersionSnapshot))
                    {
                        text.Span($" - Listino versione: {quote.PriceListVersionSnapshot}").FontSize(7).FontColor(Colors.Grey.Medium);
                    }
                });
                
                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Pagina ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" di ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });
    }
}
