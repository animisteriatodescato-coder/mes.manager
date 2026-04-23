using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Components.Dialogs.Preventivi;

/// <summary>
/// Costruisce l'HTML di stampa per il Modulo Cliente (PDF).
/// Classe separata per evitare problemi di escaping Razor con @page e @media.
/// </summary>
public static class ModuloClientePrintBuilder
{
    /// <param name="interna">true = versione interna (margine, sconto, note interne); false = versione cliente (solo prezzi)</param>
    /// <param name="fontSize">Dimensione base del testo in pt (default 10.5)</param>
    /// <param name="logoInlineHtml">HTML/SVG del logo da incorporare; se null non viene mostrato</param>
    public static string Build(PreventivoDto dto, string baseUri, IPreventivoService preventivoService,
        bool interna = false, decimal fontSize = 10.5m, string? logoInlineHtml = null,
        List<string>? condizioni = null)
    {
        var fs = fontSize.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsSmall  = (fontSize - 1.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsTiny   = (fontSize - 2.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsTitle  = (fontSize + 5.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsCond   = (fontSize - 3.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsSec    = (fontSize + 0.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        // Righe prezzi
        var righi = new System.Text.StringBuilder();
        foreach (var (lotto, margine, prezzo) in GetLottiPrezziConMargine(dto, preventivoService))
        {
            if (interna)
            {
                var mCell = margine != 0 ? (margine > 0 ? $"+{margine:N1}%" : $"{margine:N1}%") : "&mdash;";
                var sCell = dto.Sconto > 0 ? $"-{dto.Sconto:N1}%" : "&mdash;";
                righi.Append(
                    "<tr>" +
                    $"<td>{lotto:N0} pz</td>" +
                    $"<td style=\"text-align:center\">{mCell}</td>" +
                    $"<td style=\"text-align:center\">{sCell}</td>" +
                    $"<td class=\"price\">&euro;{prezzo:N2}</td>" +
                    "</tr>");
            }
            else
            {
                righi.Append(
                    "<tr>" +
                    $"<td>{lotto:N0} pz</td>" +
                    $"<td class=\"price\">&euro;{prezzo:N2}</td>" +
                    "</tr>");
            }
        }

        // Servizi aggiuntivi
        var servizi = new System.Text.StringBuilder();
        if (dto.VerniciaturaRichiesta && !string.IsNullOrWhiteSpace(dto.VerniceSnapshot))
            servizi.Append($"<div><dt>Verniciatura</dt><dd>{dto.VerniceSnapshot}</dd></div>");
        if (dto.IncollaggioRichiesto)
            servizi.Append("<div><dt>Servizi</dt><dd>Incollaggio" + (dto.ImballaggioRichiesto ? " + Imballaggio" : "") + "</dd></div>");
        else if (dto.ImballaggioRichiesto)
            servizi.Append("<div><dt>Servizi</dt><dd>Imballaggio</dd></div>");

        // Note cliente (sempre visibili); note interne solo nella versione interna
        var note = string.IsNullOrWhiteSpace(dto.NoteCliente) ? "" :
            "<div class=\"section-title\">Note</div>" +
            $"<div class=\"note-block\">{System.Web.HttpUtility.HtmlEncode(dto.NoteCliente)}</div>";

        var noteInterne = (interna && !string.IsNullOrWhiteSpace(dto.NoteInterne))
            ? "<div class=\"section-title\" style=\"color:#b71c1c\">Note Interne</div>" +
              "<div class=\"note-block\" style=\"border-color:#b71c1c;background:#fff8f8\">" +
              $"{System.Web.HttpUtility.HtmlEncode(dto.NoteInterne)}</div>"
            : "";

        var codiceArticolo = string.IsNullOrWhiteSpace(dto.CodiceArticolo) ? "" :
            $"<div><dt>Codice articolo</dt><dd>{System.Web.HttpUtility.HtmlEncode(dto.CodiceArticolo)}</dd></div>";

        var descrizione = string.IsNullOrWhiteSpace(dto.Descrizione) ? "" :
            "<div style=\"grid-column:1/-1\">" +
            $"<dt>Descrizione articolo</dt><dd>{System.Web.HttpUtility.HtmlEncode(dto.Descrizione)}</dd></div>";

        var sabbiaRow = string.IsNullOrWhiteSpace(dto.SabbiaSnapshot) ? "" :
            $"<div><dt>Tipo sabbia</dt><dd>{System.Web.HttpUtility.HtmlEncode(dto.SabbiaSnapshot)}</dd></div>";

        var numeroRef = dto.NumeroPreventivo > 0 ? $"N. {dto.NumeroPreventivo}" : "";
        var emissioneRow = string.IsNullOrWhiteSpace(numeroRef)
            ? $"Data emissione: {dto.DataCreazione:dd/MM/yyyy}"
            : $"Rif. {numeroRef} &nbsp;&bull;&nbsp; Data emissione: {dto.DataCreazione:dd/MM/yyyy}";

        var clienteEnc = System.Web.HttpUtility.HtmlEncode(dto.Cliente);
        var clienteSpan = $"<div style=\"grid-column:1/3\"><dt>Cliente</dt><dd>{clienteEnc}</dd></div>";

        var intestazioneInterna = interna
            ? "<div style=\"background:#fff3e0;border:1px solid #e65100;border-radius:3px;padding:3px 8px;" +
              "font-size:8pt;color:#e65100;font-weight:bold;margin-bottom:6px\">" +
              "&#128274; USO INTERNO &mdash; NON CONSEGNARE AL CLIENTE</div>"
            : "";

        var theadPrezzi = interna
            ? "<tr><th>Quantit&agrave; lotto (pz)</th>" +
              "<th style=\"text-align:center\">Margine</th>" +
              "<th style=\"text-align:center\">Sconto</th>" +
              "<th style=\"text-align:right\">Prezzo unitario (&euro;/pz)</th></tr>"
            : "<tr><th>Quantit&agrave; lotto (pz)</th>" +
              "<th style=\"text-align:right\">Prezzo unitario (&euro;/pz)</th></tr>";

        var logoBlock = logoInlineHtml != null
            ? $"  <div class=\"hdr-logo\">{logoInlineHtml}</div>\n"
            : "  <div class=\"hdr-logo\"></div>\n";

        return "<!DOCTYPE html>\n" +
               "<html lang=\"it\">\n" +
               "<head>\n" +
               "  <meta charset=\"utf-8\" />\n" +
               "  <title>" + TitoloDocumento(dto) + "</title>\n" +
               "  <style>\n" +
               $"    * {{ box-sizing: border-box; margin: 0; padding: 0; }}\n" +
               $"    body {{ font-family: Arial, sans-serif; font-size: {fs}pt; color: #111; background: #fff; padding: 10mm 15mm; display: flex; flex-direction: column; min-height: calc(297mm - 30mm); }}\n" +
               "    @page { margin: 12mm 18mm 18mm 18mm; }\n" +
               "    .hdr { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 6px; }\n" +
               "    .hdr-logo { display: flex; align-items: center; gap: 10px; }\n" +
               "    .hdr-logo img { max-height: 96px; width: auto; }\n" +
               "    .hdr-logo svg { height: 77px; width: auto; }\n" +
               "    .hdr-info { text-align: right; font-size: 7.5pt; color: #444; line-height: 1.55; }\n" +
               "    .hdr-info strong { display: block; font-size: 8.5pt; color: #111; margin-bottom: 2px; }\n" +
               "    hr.thick { border: none; border-top: 2.5px solid #111; margin: 7px 0 10px; }\n" +
               "    hr.thin  { border: none; border-top: 1px solid #ccc; margin: 6px 0; }\n" +
               $"    h1 {{ font-size: {fsTitle}pt; font-weight: 800; letter-spacing: 0.5px; margin-top: 1cm; margin-bottom: 3px; }}\n" +
               $"    .meta {{ font-size: {fsTiny}pt; color: #555; margin-bottom: 10px; }}\n" +
               $"    .section-title {{ font-size: {fsSec}pt; font-weight: bold; text-transform: uppercase;\n" +
               "                      border-bottom: 1.5px solid #111; padding-bottom: 2px; margin: 14px 0 7px; }}\n" +
               "    dl { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px 12px; margin-bottom: 10px; }\n" +
               $"    dt {{ font-size: {fsTiny}pt; color: #666; margin-bottom: 1px; }}\n" +
               $"    dd {{ font-size: {fsSmall}pt; font-weight: 600; }}\n" +
               "    table.prezzi { width: 100%; border-collapse: collapse; margin-top: 4px; }\n" +
               $"    table.prezzi th {{ background: #f2f2f2; border: 1px solid #bbb; padding: 5px 10px;\n" +
               $"                       font-size: {fsTiny}pt; text-align: left; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}\n" +
               $"    table.prezzi td {{ border: 1px solid #ccc; padding: 6px 10px; font-size: {fs}pt; }}\n" +
               "    td.price { text-align: right; font-weight: bold; color: #1a5c1a; }\n" +
               $"    .note-block {{ border-left: 3px solid #999; background: #f9f9f9; padding: 7px 12px;\n" +
               $"                   font-size: {fsTiny}pt; margin-bottom: 12px; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}\n" +
               $"    .conditions {{ border: 1px solid #2e7d32; border-radius: 4px; padding: 10px 14px;\n" +
               $"                   margin-top: auto; font-size: {fsCond}pt; line-height: 1.65;\n" +
               "                   -webkit-print-color-adjust: exact; print-color-adjust: exact; }\n" +
               $"    .conditions .cond-title {{ font-weight: bold; color: #1b5e20; font-size: {fsSmall}pt; margin-bottom: 5px; }}\n" +
               "    .conditions ul { padding-left: 16px; }\n" +
               "    .conditions li { margin-bottom: 3px; }\n" +
               "    .firma-section { margin-top: 28px; border-top: 1.5px solid #111; padding-top: 14px; }\n" +
               $"    .firma-title {{ font-size: {fsSmall}pt; font-weight: bold; text-transform: uppercase; margin-bottom: 12px; }}\n" +
               "    .firma-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }\n" +
               $"    .firma-label {{ font-size: {fsTiny}pt; color: #666; margin-bottom: 3px; }}\n" +
               "    .firma-line { border-bottom: 1px solid #333; height: 32px; }\n" +
               $"    .firma-note {{ font-size: {fsTiny}pt; color: #666; margin-top: 8px; }}\n" +
               "    @media print {\n" +
               "      body { padding: 0; }\n" +
               "      -webkit-print-color-adjust: exact; print-color-adjust: exact;\n" +
               "    }\n" +
               "  </style>\n" +
               "</head>\n" +
               "<body>\n" +
               $"  {intestazioneInterna}\n" +
               "  <div class=\"hdr\">\n" +
               logoBlock +
               "    <div class=\"hdr-info\">\n" +
               "      <strong>ANIMISTERIA TODESCATO</strong>\n" +
               "      Via Luigi Galvani 44/46 &bull; 36066 Sandrigo (VI)<br/>\n" +
               "      Tel: 0444 658208 &bull; info@animisteriatodescato.it<br/>\n" +
               "      PEC: animisteriatodescatosas@cgn.legalmail.it<br/>\n" +
               "      P.I.: 03200610248 &bull; SDI: SUBM70N\n" +
               "    </div>\n" +
               "  </div>\n" +
               "  <h1>PREVENTIVO FORNITURA ANIME</h1>\n" +
               $"  <div class=\"meta\">{emissioneRow}</div>\n\n" +
               "  <div class=\"section-title\">Dati Cliente</div>\n" +
               "  <dl>\n" +
               $"    {clienteSpan}\n" +
               $"    {codiceArticolo}\n" +
               $"    {descrizione}\n" +
               "  </dl>\n\n" +
               "  <div class=\"section-title\">Parametri Tecnici</div>\n" +
               "  <dl>\n" +
               $"    <div><dt>Figure per cassa</dt><dd>{dto.Figure}</dd></div>\n" +
               $"    <div><dt>Peso anima</dt><dd>{dto.PesoAnima:N3} kg</dd></div>\n" +
               $"    <div><dt>Lotto base</dt><dd>{dto.Lotto:N0} pz</dd></div>\n" +
               $"    {sabbiaRow}\n" +
               $"    {servizi}\n" +
               "  </dl>\n\n" +
               $"  {note}\n" +
               $"  {noteInterne}\n\n" +
               "  <div class=\"section-title\">Prezzi per Lotto</div>\n" +
               "  <table class=\"prezzi\">\n" +
               $"    <thead>{theadPrezzi}</thead>\n" +
               $"    <tbody>{righi}</tbody>\n" +
               "  </table>\n\n" +
               BuildCondizioniHtml(condizioni) +
               "</body>\n" +
               "</html>";
    }

    private static readonly List<string> _defaultCondizioni = new()
    {
        "La presente offerta ha validità <strong>90 giorni</strong> dalla data di emissione.",
        "I prezzi sono soggetti a revisione in funzione dell'andamento dei costi delle materie prime e dei vettori energetici.",
        "Prezzi IVA esclusa. L'imposta sarà applicata secondo l'aliquota vigente al momento della fatturazione.",
        "Pagamento: secondo accordi commerciali in essere."
    };

    private static string BuildCondizioniHtml(List<string>? condizioni)
    {
        if (condizioni == null) return "";
        var list = condizioni;
        var sb = new System.Text.StringBuilder();
        sb.Append("  <div class=\"conditions\">\n");
        sb.Append("    <div class=\"cond-title\">CONDIZIONI DI OFFERTA</div>\n");
        sb.Append("    <ul>\n");
        foreach (var c in list.Where(c => !string.IsNullOrWhiteSpace(c)))
            sb.Append($"      <li>{c}</li>\n");
        sb.Append("    </ul>\n");
        sb.Append("  </div>\n\n");
        return sb.ToString();
    }

    private static string TitoloDocumento(PreventivoDto dto)
    {
        var cliente = (dto.Cliente ?? "").Replace(" ", "_");
        var num = dto.NumeroPreventivo > 0 ? $"N{dto.NumeroPreventivo}_" : "";
        var data = dto.DataCreazione.ToString("yyyyMMdd");
        return $"Preventivo_{num}{cliente}_{data}";
    }

    private static IEnumerable<(int Lotto, decimal Margine, decimal Prezzo)> GetLottiPrezziConMargine(
        PreventivoDto dto, IPreventivoService svc)
    {
        if (dto.Lotto > 0)
        {
            var r = svc.CalcolaConLotto(dto, dto.Lotto, dto.Margine1);
            yield return (dto.Lotto, dto.Margine1, r.PrezzoVendita);
        }
        if (dto.Lotto2.HasValue && dto.Lotto2.Value > 0)
        {
            var r = svc.CalcolaConLotto(dto, dto.Lotto2.Value, dto.Margine2);
            yield return (dto.Lotto2.Value, dto.Margine2, r.PrezzoVendita);
        }
        if (dto.Lotto3.HasValue && dto.Lotto3.Value > 0)
        {
            var r = svc.CalcolaConLotto(dto, dto.Lotto3.Value, dto.Margine3);
            yield return (dto.Lotto3.Value, dto.Margine3, r.PrezzoVendita);
        }
        if (dto.Lotto4.HasValue && dto.Lotto4.Value > 0)
        {
            var r = svc.CalcolaConLotto(dto, dto.Lotto4.Value, dto.Margine4);
            yield return (dto.Lotto4.Value, dto.Margine4, r.PrezzoVendita);
        }
    }
}
