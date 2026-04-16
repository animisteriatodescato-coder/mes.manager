using MESManager.Application.DTOs;
using MESManager.Domain.Enums;

namespace MESManager.Web.Components.Pages.Manutenzioni;

/// <summary>
/// Costruisce l'HTML di stampa per la scheda manutenzione cassa d'anima.
/// Struttura analoga a ModuloClientePrintBuilder (Preventivi).
/// </summary>
public static class SchedaManutenzioneCassaPrintBuilder
{
    /// <param name="scheda">DTO scheda (con righe già caricate)</param>
    /// <param name="allegati">Lista allegati (opzionale, per elencarli nel PDF)</param>
    /// <param name="azienda">Nome azienda da intestazione</param>
    public static string Build(
        ManutenzioneCassaSchedaDto scheda,
        List<ManutenzioneCassaAllegatoDto>? allegati = null,
        string azienda = "Officina")
    {
        const decimal fs = 10.5m;
        var fsS  = (fs - 1.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsT  = (fs - 2.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsH  = (fs + 4.5m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsBase = fs.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        var statoLabel = scheda.Stato switch
        {
            StatoSchedaManutenzione.Completata    => "COMPLETATA",
            StatoSchedaManutenzione.ConAnomalie   => "CON ANOMALIE",
            StatoSchedaManutenzione.InCompilazione => "IN COMPILAZIONE",
            _ => scheda.Stato.ToString().ToUpperInvariant()
        };
        var statoColor = scheda.Stato switch
        {
            StatoSchedaManutenzione.Completata    => "#2e7d32",
            StatoSchedaManutenzione.ConAnomalie   => "#b71c1c",
            StatoSchedaManutenzione.InCompilazione => "#e65100",
            _ => "#333"
        };

        var totOk          = scheda.Righe.Count(r => r.Esito == EsitoAttivitaManutenzione.OK);
        var totAnomalie    = scheda.Righe.Count(r => r.Esito == EsitoAttivitaManutenzione.Anomalia);
        var totNonEseguite = scheda.Righe.Count(r => r.Esito == EsitoAttivitaManutenzione.NonEseguita);

        // ── Tabella attività ────────────────────────────────────────────────
        var righeHtml = new System.Text.StringBuilder();
        foreach (var riga in scheda.Righe.Where(r => r.Esito != EsitoAttivitaManutenzione.NonEseguita).OrderBy(r => r.OrdineAttivita))
        {
            var esitoLabel = riga.Esito switch
            {
                EsitoAttivitaManutenzione.OK => "✔ OK",
                EsitoAttivitaManutenzione.Anomalia => "⚠ ANOMALIA",
                _ => "— Non eseguita"
            };
            var esitoColor = riga.Esito switch
            {
                EsitoAttivitaManutenzione.OK => "#1b5e20",
                EsitoAttivitaManutenzione.Anomalia => "#b71c1c",
                _ => "#666"
            };
            var commento = System.Web.HttpUtility.HtmlEncode(riga.Commento ?? "");

            righeHtml.Append(
                $"<tr>" +
                $"<td style=\"text-align:center;color:#888\">{riga.OrdineAttivita}</td>" +
                $"<td>{System.Web.HttpUtility.HtmlEncode(riga.NomeAttivita)}</td>" +
                $"<td style=\"color:{esitoColor};font-weight:600;white-space:nowrap\">{esitoLabel}</td>" +
                $"<td style=\"color:#555\">{commento}</td>" +
                $"</tr>\n");
        }

        // ── Elenco allegati ─────────────────────────────────────────────────
        var allegatiHtml = "";
        if (allegati != null && allegati.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<div class=\"section-title\">Allegati</div>\n");

            // Foto: mostrate come immagini
            var foto = allegati.Where(a => a.IsFoto).ToList();
            var docs = allegati.Where(a => !a.IsFoto).ToList();

            if (foto.Count > 0)
            {
                sb.Append("<div style=\"display:flex;flex-wrap:wrap;gap:10px;margin-bottom:10px\">\n");
                foreach (var f in foto)
                {
                    sb.Append($"<figure style=\"margin:0;text-align:center;max-width:280px\">");
                    sb.Append($"<img src=\"{f.UrlProxy}\" alt=\"{System.Web.HttpUtility.HtmlEncode(f.NomeFile)}\" "
                             + "style=\"max-width:280px;max-height:200px;border:1px solid #ccc;border-radius:3px;object-fit:contain\" />");
                    sb.Append($"<figcaption style=\"font-size:8pt;color:#666;margin-top:2px\">{System.Web.HttpUtility.HtmlEncode(f.NomeFile)}</figcaption>");
                    sb.Append("</figure>\n");
                }
                sb.Append("</div>\n");
            }

            // Documenti: lista testuale
            if (docs.Count > 0)
            {
                sb.Append("<ul style=\"margin:0 0 10px;padding-left:18px\">\n");
                foreach (var a in docs)
                {
                    var dim = a.DimensioneBytes > 1024 * 1024
                        ? $"{a.DimensioneBytes / 1024.0 / 1024.0:N1} MB"
                        : $"{a.DimensioneBytes / 1024.0:N0} KB";
                    sb.Append($"<li>{System.Web.HttpUtility.HtmlEncode(a.NomeFile)} <span style=\"color:#888\">({a.TipoFile}, {dim})</span></li>\n");
                }
                sb.Append("</ul>\n");
            }

            allegatiHtml = sb.ToString();
        }

        // ── Note ────────────────────────────────────────────────────────────
        var noteHtml = string.IsNullOrWhiteSpace(scheda.Note) ? "" :
            $"<div class=\"section-title\">Note</div>\n" +
            $"<p style=\"margin:0 0 10px;padding:8px 10px;border-left:3px solid #1565c0;background:#e8f0fe;white-space:pre-wrap\">" +
            $"{System.Web.HttpUtility.HtmlEncode(scheda.Note)}</p>\n";

        var dataChiusuraHtml = scheda.DataChiusura.HasValue
            ? $"<div><dt>Data chiusura</dt><dd>{scheda.DataChiusura.Value:dd/MM/yyyy HH:mm}</dd></div>"
            : "";

        var titolo = $"ManutCassa_{scheda.CodiceCassa}_{scheda.DataEsecuzione:yyyyMMdd}";

        return "<!DOCTYPE html>\n" +
               "<html lang=\"it\">\n" +
               "<head>\n" +
               "  <meta charset=\"utf-8\" />\n" +
               $"  <title>{titolo}</title>\n" +
               "  <style>\n" +
               $"    * {{ box-sizing: border-box; margin: 0; padding: 0; }}\n" +
               $"    body {{ font-family: Arial, sans-serif; font-size: {fsBase}pt; color: #111; background: #fff; padding: 10mm 15mm; }}\n" +
               "    @page { margin: 12mm 15mm 15mm 15mm; }\n" +
               $"    h1 {{ font-size: {fsH}pt; font-weight: 800; }}\n" +
               $"    .meta {{ font-size: {fsT}pt; color: #555; margin: 4px 0 10px; }}\n" +
               $"    .section-title {{ font-size: {fsS}pt; font-weight: bold; text-transform: uppercase;\n" +
               "                       border-bottom: 1.5px solid #111; padding-bottom: 2px; margin: 14px 0 7px; }}\n" +
               "    dl { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px 12px; margin-bottom: 10px; }\n" +
               $"    dt {{ font-size: {fsT}pt; color: #666; margin-bottom: 1px; }}\n" +
               $"    dd {{ font-size: {fsS}pt; font-weight: 600; }}\n" +
               "    table.att { width: 100%; border-collapse: collapse; margin-top: 4px; }\n" +
               $"    table.att th {{ background: #f2f2f2; border: 1px solid #bbb; padding: 5px 8px; font-size: {fsT}pt; text-align: left; }}\n" +
               $"    table.att td {{ border: 1px solid #ddd; padding: 5px 8px; font-size: {fsBase}pt; vertical-align: top; }}\n" +
               "    .riepilogo { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin-top: 14px; }\n" +
               "    .rie-box { border: 1px solid #ccc; border-radius: 4px; padding: 10px; text-align: center; }\n" +
               $"    .rie-num {{ font-size: {(fs + 6m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}pt; font-weight: 800; }}\n" +
               $"    .rie-label {{ font-size: {fsT}pt; color: #666; }}\n" +
               "    .firma-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 28px; border-top: 1.5px solid #111; padding-top: 14px; }\n" +
               $"    .firma-label {{ font-size: {fsT}pt; color: #666; margin-bottom: 3px; }}\n" +
               "    .firma-line { border-bottom: 1px solid #333; height: 32px; }\n" +
               "    @media print { body { padding: 0; } -webkit-print-color-adjust: exact; print-color-adjust: exact; }\n" +
               "  </style>\n" +
               "</head>\n" +
               "<body>\n" +
               // Header
               $"  <h1>Scheda Manutenzione Cassa d'Anima</h1>\n" +
               $"  <p class=\"meta\">{System.Web.HttpUtility.HtmlEncode(azienda)} · Documento generato il {DateTime.Now:dd/MM/yyyy HH:mm}</p>\n" +
               "  <hr style=\"border:none;border-top:2.5px solid #111;margin:7px 0 10px\">\n\n" +
               // Dati cassa
               "  <div class=\"section-title\">Dati Attrezzatura</div>\n" +
               "  <dl>\n" +
               $"    <div><dt>Rif. #</dt><dd style=\"color:#1565c0;font-weight:800\">{scheda.CodiceRiferimento}</dd></div>\n" +
               $"    <div><dt>Codice cassa</dt><dd>{System.Web.HttpUtility.HtmlEncode(scheda.CodiceCassa)}</dd></div>\n" +
               $"    <div><dt>Data esecuzione</dt><dd>{scheda.DataEsecuzione:dd/MM/yyyy}</dd></div>\n" +
               $"    <div><dt>Operatore</dt><dd>{System.Web.HttpUtility.HtmlEncode(scheda.NomeOperatore ?? "—")}</dd></div>\n" +
               $"    <div><dt>Stato</dt><dd style=\"color:{statoColor}\">{statoLabel}</dd></div>\n" +
               $"    {dataChiusuraHtml}\n" +
               "  </dl>\n\n" +
               noteHtml +
               // Tabella attività (solo se ci sono righe compilate)
               (righeHtml.Length > 0
                   ? "  <div class=\"section-title\">Attività Manutentive</div>\n" +
                     "  <table class=\"att\">\n" +
                     $"    <thead><tr><th style=\"width:36px\">#</th><th>Attività</th><th style=\"width:130px\">Esito</th><th>Commento / Anomalia</th></tr></thead>\n" +
                     $"    <tbody>\n{righeHtml}    </tbody>\n" +
                     "  </table>\n\n"
                   : "") +
               // Riepilogo (solo se ci sono attività compilate)
               (totOk + totAnomalie > 0
                   ? "  <div class=\"section-title\">Riepilogo</div>\n" +
                     "  <div class=\"riepilogo\" style=\"grid-template-columns:1fr 1fr\">\n" +
                     $"    <div class=\"rie-box\"><div class=\"rie-num\" style=\"color:#1b5e20\">{totOk}</div><div class=\"rie-label\">OK</div></div>\n" +
                     $"    <div class=\"rie-box\"><div class=\"rie-num\" style=\"color:#b71c1c\">{totAnomalie}</div><div class=\"rie-label\">Anomalie</div></div>\n" +
                     "  </div>\n\n"
                   : "") +
               allegatiHtml +
               "</body>\n" +
               "</html>";
    }
}
