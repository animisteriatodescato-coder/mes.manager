using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MESManager.Application.Services;

/// <summary>
/// Genera il PDF "Scheda Anima" usando QuestPDF.
/// Riusa IAnimeService e IAllegatoArticoloService — zero duplicazione.
/// Pattern: segue QuotePdfGenerator.cs (v1.60.38)
/// </summary>
public class AnimePdfService : IAnimePdfService
{
    private readonly IAnimeService _animeService;
    private readonly IAllegatoArticoloService _allegatoService;
    private readonly ILogger<AnimePdfService> _logger;

    // Colori brand
    private const string PrimaryColor = "#1565C0"; // Blue Darken3
    private const string LightGray = "#F5F5F5";

    public AnimePdfService(
        IAnimeService animeService,
        IAllegatoArticoloService allegatoService,
        ILogger<AnimePdfService> logger)
    {
        _animeService = animeService;
        _allegatoService = allegatoService;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Stream?> GenerateSchedaAsync(int animeId)
    {
        _logger.LogInformation("PDF Scheda Anima: avvio generazione per ID {Id}", animeId);

        var anime = await _animeService.GetByIdAsync(animeId);
        if (anime is null)
        {
            _logger.LogWarning("PDF Scheda Anima: ID {Id} non trovato", animeId);
            return null;
        }

        // Recupera allegati (foto + documenti)
        var allegati = await _allegatoService.GetAllegatiByArticoloAsync(anime.CodiceArticolo);

        // Recupera il contenuto della foto con priorità più alta (priorità 1), se presente
        byte[]? fotoBytes = null;
        var fotoOrdinata = allegati.Foto.OrderBy(f => f.Priorita).FirstOrDefault();
        if (fotoOrdinata is not null)
        {
            var content = await _allegatoService.GetFileContentAsync(fotoOrdinata.Id);
            fotoBytes = content?.Content;
        }

        _logger.LogInformation("PDF Scheda Anima: generazione per {Codice} ({NrFoto} foto)", anime.CodiceArticolo, allegati.Foto.Count);

        var document = BuildDocument(anime, allegati, fotoBytes);
        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;

        _logger.LogInformation("PDF Scheda Anima: PDF generato per {Codice}, {Bytes} bytes", anime.CodiceArticolo, stream.Length);
        return stream;
    }

    private Document BuildDocument(AnimeDto a, AllegatiArticoloResponse allegati, byte[]? fotoBytes)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, a));
                page.Content().Element(c => ComposeContent(c, a, allegati, fotoBytes));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private void ComposeHeader(IContainer container, AnimeDto a)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("SCHEDA ANIMA").Bold().FontSize(14).FontColor(PrimaryColor);
                    inner.Item().Text($"{a.CodiceArticolo} — {a.DescrizioneArticolo}").FontSize(10).Bold();
                    if (!string.IsNullOrEmpty(a.Cliente))
                        inner.Item().Text($"Cliente: {a.Cliente}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(90).AlignRight().Column(inner =>
                {
                    inner.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    inner.Item().Text($"ID: {a.Id}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(PrimaryColor);
            col.Item().PaddingBottom(4);
        });
    }

    private void ComposeContent(IContainer container, AnimeDto a, AllegatiArticoloResponse allegati, byte[]? fotoBytes)
    {
        container.Column(col =>
        {
            // Layout a due colonne: dati (65%) | foto (35%)
            col.Item().Row(row =>
            {
                // Colonna dati
                row.RelativeItem(65).Column(dati =>
                {
                    // IDENTIFICAZIONE
                    dati.Item().Element(c => SectionTitle(c, "IDENTIFICAZIONE"));
                    dati.Item().Element(c => InfoGrid(c, new List<(string Label, string? Value)>
                    {
                        ("Codice", a.CodiceArticolo),
                        ("Codice Anime", a.CodiceAnime),
                        ("Codice Cassa", a.CodiceCassa),
                        ("Unità Misura", a.UnitaMisura),
                        ("Ubicazione", a.Ubicazione),
                    }));

                    dati.Item().PaddingTop(6);

                    // IMBALLO
                    dati.Item().Element(c => SectionTitle(c, "IMBALLO"));
                    var totImballo = (a.QuantitaPiano ?? 0) * (a.NumeroPiani ?? 0);
                    dati.Item().Element(c => InfoGrid(c, new List<(string Label, string? Value)>
                    {
                        ("Imballo", a.ImballoDescrizione ?? a.Imballo?.ToString()),
                        ("Qta/Piano", a.QuantitaPiano?.ToString()),
                        ("N.Piani", a.NumeroPiani?.ToString()),
                        ("Totale", totImballo > 0 ? totImballo.ToString() : null),
                    }));

                    dati.Item().PaddingTop(6);

                    // MATERIALI
                    dati.Item().Element(c => SectionTitle(c, "MATERIALI"));
                    dati.Item().Element(c => InfoGrid(c, new List<(string Label, string? Value)>
                    {
                        ("Colla", a.CollaDescrizione ?? a.Colla),
                        ("Sabbia", a.SabbiaDescrizione ?? a.Sabbia),
                        ("Vernice", a.VerniceDescrizione ?? a.Vernice),
                    }));

                    dati.Item().PaddingTop(6);

                    // PRODUZIONE
                    dati.Item().Element(c => SectionTitle(c, "PRODUZIONE"));
                    dati.Item().Element(c => InfoGrid(c, new List<(string Label, string? Value)>
                    {
                        ("Ciclo", a.Ciclo),
                        ("Peso", a.Peso),
                        ("Figure", a.Figure),
                        ("Maschere", a.Maschere),
                        ("Togliere Sparo", a.TogliereSparo == "1" ? "Sì" : a.TogliereSparo == "0" ? "No" : a.TogliereSparo),
                        ("Armata L", a.ArmataL),
                        ("Assemblata", a.Assemblata),
                        ("Larghezza", a.Larghezza?.ToString()),
                        ("Altezza", a.Altezza?.ToString()),
                        ("Profondità", a.Profondita?.ToString()),
                    }));

                    // MACCHINE
                    if (!string.IsNullOrEmpty(a.MacchineSuDisponibiliDescrizione))
                    {
                        dati.Item().PaddingTop(6);
                        dati.Item().Element(c => SectionTitle(c, "MACCHINE"));
                        dati.Item().Text(a.MacchineSuDisponibiliDescrizione).FontSize(9);
                    }

                    // NOTE
                    if (!string.IsNullOrEmpty(a.Note))
                    {
                        dati.Item().PaddingTop(6);
                        dati.Item().Element(c => SectionTitle(c, "NOTE"));
                        dati.Item().Background(LightGray).Padding(4).Text(a.Note).FontSize(9);
                    }
                });

                row.ConstantItem(10); // spaziatura

                // Colonna foto
                row.RelativeItem(35).Column(foto =>
                {
                    foto.Item().Element(c => SectionTitle(c, $"FOTO ({allegati.Foto.Count})"));

                    if (fotoBytes is not null && fotoBytes.Length > 0)
                    {
                        foto.Item().MaxHeight(220).Image(fotoBytes).FitArea();
                        var primaFoto = allegati.Foto.OrderBy(f => f.Priorita).First();
                        foto.Item().PaddingTop(2).AlignCenter()
                            .Text(primaFoto.Descrizione ?? primaFoto.NomeFile)
                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        foto.Item().Background(LightGray).Height(100)
                            .AlignCenter().AlignMiddle()
                            .Text("Nessuna foto").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    if (allegati.Foto.Count > 1)
                    {
                        foto.Item().PaddingTop(4)
                            .Text($"+ altre {allegati.Foto.Count - 1} foto")
                            .FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });
        });
    }

    private static void SectionTitle(IContainer container, string title)
    {
        container.Background("#1565C0").Padding(3)
            .Text(title).Bold().FontSize(8).FontColor(Colors.White);
    }

    private static void InfoGrid(IContainer container, List<(string Label, string? Value)> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(80);
                cols.RelativeColumn();
            });

            bool alt = false;
            foreach (var (label, value) in items)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                string bg = alt ? Colors.White : LightGray;
                alt = !alt;

                table.Cell().Background(bg).Padding(2)
                    .Text(label + ":").FontSize(8).FontColor(Colors.Grey.Darken2).Bold();
                table.Cell().Background(bg).Padding(2)
                    .Text(value).FontSize(8);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor("#BDBDBD");
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text("MESManager — Scheda Anima").FontSize(7).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight()
                    .Text(ctx => { ctx.Span("Pag. ").FontSize(7); ctx.CurrentPageNumber().FontSize(7); ctx.Span(" / ").FontSize(7); ctx.TotalPages().FontSize(7); });
            });
        });
    }
}
