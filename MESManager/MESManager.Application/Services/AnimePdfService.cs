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
/// Pattern: segue QuotePdfGenerator.cs (v1.60.40)
/// </summary>
public class AnimePdfService : IAnimePdfService
{
    private readonly IAnimeService _animeService;
    private readonly IAllegatoArticoloService _allegatoService;
    private readonly ILogger<AnimePdfService> _logger;

    // Palette professionale — corporate navy
    private const string PrimaryColor  = "#1C3F6E"; // Navy corporate
    private const string AccentLine    = "#2E6DA4"; // Blue accent per bordi
    private const string HeaderBg      = "#F0F4F9"; // Sfondo sezione — grigio azzurrino tenue
    private const string RowAlt        = "#F7F9FB"; // Riga alternata tenue
    private const string BorderColor   = "#D0D8E4"; // Bordo sottile

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

        var allegati = await _allegatoService.GetAllegatiByArticoloAsync(anime.CodiceArticolo);

        // Carica TUTTE le foto in ordine di priorità
        var fotoOrdinata = allegati.Foto.OrderBy(f => f.Priorita).ToList();
        var fotoCaricate = new List<(AllegatoArticoloDto Dto, byte[] Bytes)>();
        foreach (var f in fotoOrdinata)
        {
            var content = await _allegatoService.GetFileContentAsync(f.Id);
            if (content?.Content is { Length: > 0 } bytes)
                fotoCaricate.Add((f, bytes));
        }

        _logger.LogInformation("PDF Scheda Anima: generazione per {Codice} ({NrFoto} foto caricate su {NrTot})",
            anime.CodiceArticolo, fotoCaricate.Count, allegati.Foto.Count);

        var document = BuildDocument(anime, fotoCaricate);
        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;

        _logger.LogInformation("PDF Scheda Anima: PDF generato per {Codice}, {Bytes} bytes", anime.CodiceArticolo, stream.Length);
        return stream;
    }

    private Document BuildDocument(AnimeDto a, List<(AllegatoArticoloDto Dto, byte[] Bytes)> foto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, a));
                page.Content().Element(c => ComposeContent(c, a, foto));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private static void ComposeHeader(IContainer container, AnimeDto a)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("SCHEDA ANIMA")
                        .Bold().FontSize(17).FontColor(PrimaryColor).LetterSpacing(0.05f);
                    inner.Item().PaddingTop(2)
                        .Text($"{a.CodiceArticolo}  ·  {a.DescrizioneArticolo}")
                        .FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
                    if (!string.IsNullOrEmpty(a.Cliente))
                        inner.Item().PaddingTop(1)
                            .Text($"Cliente: {a.Cliente}")
                            .FontSize(10.5f).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(5);
                row.ConstantItem(80).AlignRight().Column(inner =>
                {
                    inner.Item().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                    inner.Item().Text($"ID: {a.Id}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                });
            });
            col.Item().PaddingTop(5).LineHorizontal(2).LineColor(PrimaryColor);
            col.Item().PaddingBottom(5);
        });
    }

    private static void ComposeContent(IContainer container, AnimeDto a, List<(AllegatoArticoloDto Dto, byte[] Bytes)> foto)
    {
        container.Row(row =>
        {
            // ── Colonna DATI (56%) ──────────────────────────────────
            row.RelativeItem(56).Column(dati =>
            {
                dati.Item().Element(c => SectionTitle(c, "IDENTIFICAZIONE"));
                dati.Item().Element(c => InfoGrid(c, [
                    ("Codice",       a.CodiceArticolo),
                    ("Codice Anime", a.CodiceAnime),
                    ("Codice Cassa", a.CodiceCassa),
                    ("Unità Misura", a.UnitaMisura),
                    ("Ubicazione",   a.Ubicazione),
                ]));

                dati.Item().PaddingTop(7);

                dati.Item().Element(c => SectionTitle(c, "IMBALLO"));
                int totImballo = (a.QuantitaPiano ?? 0) * (a.NumeroPiani ?? 0);
                dati.Item().Element(c => InfoGrid(c, [
                    ("Imballo",   a.ImballoDescrizione ?? a.Imballo?.ToString()),
                    ("Qta/Piano", a.QuantitaPiano?.ToString()),
                    ("N.Piani",   a.NumeroPiani?.ToString()),
                    ("Totale",    totImballo > 0 ? totImballo.ToString() : null),
                ]));

                dati.Item().PaddingTop(7);

                dati.Item().Element(c => SectionTitle(c, "MATERIALI"));
                dati.Item().Element(c => InfoGrid(c, [
                    ("Colla",   a.CollaDescrizione   ?? a.Colla),
                    ("Sabbia",  a.SabbiaDescrizione  ?? a.Sabbia),
                    ("Vernice", a.VerniceDescrizione ?? a.Vernice),
                ]));

                dati.Item().PaddingTop(7);

                dati.Item().Element(c => SectionTitle(c, "PRODUZIONE"));
                dati.Item().Element(c => InfoGrid(c, [
                    ("Ciclo",          a.Ciclo),
                    ("Peso",           a.Peso),
                    ("Figure",         a.Figure),
                    ("Maschere",       a.Maschere),
                    ("Togl. Sparo",    a.TogliereSparo == "1" ? "Sì" : a.TogliereSparo == "0" ? "No" : a.TogliereSparo),
                    ("Armata L",       a.ArmataL),
                    ("Assemblata",     a.Assemblata),
                    ("Larghezza",      a.Larghezza?.ToString()),
                    ("Altezza",        a.Altezza?.ToString()),
                    ("Profondità",     a.Profondita?.ToString()),
                ]));

                if (!string.IsNullOrEmpty(a.MacchineSuDisponibiliDescrizione))
                {
                    dati.Item().PaddingTop(7);
                    dati.Item().Element(c => SectionTitle(c, "MACCHINE"));
                    dati.Item().PaddingHorizontal(3).PaddingVertical(4)
                        .Text(a.MacchineSuDisponibiliDescrizione).FontSize(10.5f);
                }

                if (!string.IsNullOrEmpty(a.Note))
                {
                    dati.Item().PaddingTop(7);
                    dati.Item().Element(c => SectionTitle(c, "NOTE"));
                    dati.Item().Background(RowAlt).Border(0.5f).BorderColor(BorderColor)
                        .Padding(5).Text(a.Note).FontSize(10.5f);
                }
            });

            row.ConstantItem(12); // spaziatura centrale

            // ── Colonna FOTO (44%) ──────────────────────────────────
            row.RelativeItem(44).Column(fotoCol =>
            {
                string titoloFoto = foto.Count > 0 ? $"FOTO ({foto.Count})" : "FOTO";
                fotoCol.Item().Element(c => SectionTitle(c, titoloFoto));

                if (foto.Count == 0)
                {
                    fotoCol.Item().Background(RowAlt).Border(0.5f).BorderColor(BorderColor)
                        .Height(90).AlignCenter().AlignMiddle()
                        .Text("Nessuna foto disponibile").FontSize(10).FontColor(Colors.Grey.Darken1).Italic();
                }
                else
                {
                    for (int i = 0; i < foto.Count; i++)
                    {
                        var (dto, bytes) = foto[i];
                        if (i > 0) fotoCol.Item().PaddingTop(6);

                        fotoCol.Item().Border(0.5f).BorderColor(BorderColor)
                            .MaxHeight(175).Image(bytes).FitArea();

                        // Didascalia: priorità + nome/descrizione
                        string caption = string.IsNullOrWhiteSpace(dto.Descrizione)
                            ? dto.NomeFile
                            : dto.Descrizione;
                        fotoCol.Item().PaddingTop(2).PaddingHorizontal(1)
                            .Text($"[{i + 1}] {caption}")
                            .FontSize(9).FontColor(Colors.Grey.Darken2).Italic();
                    }
                }
            });
        });
    }

    private static void SectionTitle(IContainer container, string title)
    {
        container
            .BorderLeft(3).BorderColor(AccentLine)
            .Background(HeaderBg)
            .PaddingLeft(6).PaddingVertical(3)
            .Text(title).Bold().FontSize(10).FontColor(PrimaryColor);
    }

    private static void InfoGrid(IContainer container, (string Label, string? Value)[] items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(76);
                cols.RelativeColumn();
            });

            bool alt = false;
            foreach (var (label, value) in items)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                string bg = alt ? Colors.White : RowAlt;
                alt = !alt;

                table.Cell().Background(bg).PaddingVertical(2.5f).PaddingHorizontal(4)
                    .Text(label + ":").FontSize(10).FontColor(Colors.Grey.Darken2).Bold();
                table.Cell().Background(bg).PaddingVertical(2.5f).PaddingHorizontal(4)
                    .Text(value).FontSize(10).FontColor(Colors.Grey.Darken3);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(BorderColor);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .Text("MESManager — Scheda Anima")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight()
                    .Text(ctx =>
                    {
                        ctx.Span("Pag. ").FontSize(9).FontColor(Colors.Grey.Medium);
                        ctx.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        ctx.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                        ctx.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
            });
        });
    }
}
