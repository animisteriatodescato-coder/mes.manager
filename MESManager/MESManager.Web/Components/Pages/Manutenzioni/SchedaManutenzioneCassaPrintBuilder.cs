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
    /// <param name="allegati">Lista allegati (opzionale)</param>
    /// <param name="azienda">Nome azienda</param>
    /// <param name="logoInlineHtml">HTML del logo da incorporare inline (es. tag img base64 o svg)</param>
    public static string Build(
        ManutenzioneCassaSchedaDto scheda,
        List<ManutenzioneCassaAllegatoDto>? allegati = null,
        string azienda = "Officina",
        string? logoInlineHtml = null)
    {
        const decimal fs = 10.5m;
        var fsBase  = fs.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var fsS     = (fs - 1.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);  // small
        var fsT     = (fs - 2.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);  // tiny
        var fsH     = (fs + 5.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);  // title
        var fsCond  = (fs - 3.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);  // condizioni
        // Font dati principali (codice, data, operatore) +2pt rispetto al base
        var fsData  = (fs + 2.0m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        var statoLabel = scheda.Stato switch
        {
            StatoSchedaManutenzione.Completata     => "COMPLETATA",
            StatoSchedaManutenzione.ConAnomalie    => "CON ANOMALIE",
            StatoSchedaManutenzione.InCompilazione => "IN COMPILAZIONE",
            _ => scheda.Stato.ToString().ToUpperInvariant()
        };
        var statoColor = scheda.Stato switch
        {
            StatoSchedaManutenzione.Completata     => "#2e7d32",
            StatoSchedaManutenzione.ConAnomalie    => "#b71c1c",
            StatoSchedaManutenzione.InCompilazione => "#e65100",
            _ => "#333"
        };

        var totOk          = scheda.Righe.Count(r => r.Esito == EsitoAttivitaManutenzione.OK);
        var totAnomalie    = scheda.Righe.Count(r => r.Esito == EsitoAttivitaManutenzione.Anomalia);

        // ── Tabella attività ────────────────────────────────────────────────
        var righeHtml = new System.Text.StringBuilder();
        foreach (var riga in scheda.Righe
            .Where(r => r.Esito != EsitoAttivitaManutenzione.NonEseguita)
            .OrderBy(r => r.OrdineAttivita))
        {
            var esitoLabel = riga.Esito switch
            {
                EsitoAttivitaManutenzione.OK       => "✔ OK",
                EsitoAttivitaManutenzione.Anomalia => "⚠ ANOMALIA",
                _ => "—"
            };
            var esitoColor = riga.Esito switch
            {
                EsitoAttivitaManutenzione.OK       => "#1b5e20",
                EsitoAttivitaManutenzione.Anomalia => "#b71c1c",
                _ => "#666"
            };
            righeHtml.Append(
                $"<tr>" +
                $"<td style=\"text-align:center;color:#888\">{riga.OrdineAttivita}</td>" +
                $"<td>{System.Web.HttpUtility.HtmlEncode(riga.NomeAttivita)}</td>" +
                $"<td style=\"color:{esitoColor};font-weight:600;white-space:nowrap\">{esitoLabel}</td>" +
                $"<td style=\"color:#555\">{System.Web.HttpUtility.HtmlEncode(riga.Commento ?? "")}</td>" +
                $"</tr>\n");
        }

        // ── Allegati ─────────────────────────────────────────────────────────
        var allegatiHtml = new System.Text.StringBuilder();
        if (allegati != null && allegati.Count > 0)
        {
            allegatiHtml.Append("<div class=\"section-title\">Documentazione fotografica e allegati</div>\n");
            var foto = allegati.Where(a => a.IsFoto).ToList();
            var docs = allegati.Where(a => !a.IsFoto).ToList();

            if (foto.Count > 0)
            {
                foreach (var f in foto)
                {
                    allegatiHtml.Append(
                        $"<figure style=\"margin:0 0 14px;text-align:center;page-break-inside:avoid\">" +
                        $"<img src=\"{f.UrlProxy}\" alt=\"{System.Web.HttpUtility.HtmlEncode(f.NomeFile)}\" " +
                        "style=\"width:100%;max-height:370px;border:1px solid #ccc;border-radius:3px;object-fit:contain;" +
                        "-webkit-print-color-adjust:exact;print-color-adjust:exact\" />" +
                        $"<figcaption style=\"font-size:8pt;color:#666;margin-top:4px\">{System.Web.HttpUtility.HtmlEncode(f.NomeFile)}</figcaption>" +
                        "</figure>\n");
                }
            }
            if (docs.Count > 0)
            {
                allegatiHtml.Append("<ul style=\"margin:0 0 10px;padding-left:18px\">\n");
                foreach (var a in docs)
                {
                    var dim = a.DimensioneBytes > 1024 * 1024
                        ? $"{a.DimensioneBytes / 1024.0 / 1024.0:N1} MB"
                        : $"{a.DimensioneBytes / 1024.0:N0} KB";
                    allegatiHtml.Append(
                        $"<li>{System.Web.HttpUtility.HtmlEncode(a.NomeFile)} <span style=\"color:#888\">({a.TipoFile}, {dim})</span></li>\n");
                }
                allegatiHtml.Append("</ul>\n");
            }
        }

        // ── Note ─────────────────────────────────────────────────────────────
        var noteHtml = string.IsNullOrWhiteSpace(scheda.Note) ? "" :
            "<div class=\"section-title\">Note operative</div>\n" +
            "<p style=\"margin:0 0 10px;padding:8px 10px;border-left:3px solid #1565c0;background:#e8f0fe;" +
            "white-space:pre-wrap;-webkit-print-color-adjust:exact;print-color-adjust:exact\">" +
            $"{System.Web.HttpUtility.HtmlEncode(scheda.Note)}</p>\n";

        // ── Dati cliente ──────────────────────────────────────────────────────
        var clienteHtml = new System.Text.StringBuilder();
        var hasCliente = !string.IsNullOrWhiteSpace(scheda.Cliente);
        var hasArticolo = !string.IsNullOrWhiteSpace(scheda.ArticoloDescrizione);
        if (hasCliente || hasArticolo || !string.IsNullOrWhiteSpace(scheda.CodiceArticolo))
        {
            clienteHtml.Append("<div class=\"section-title\">Dati Cliente / Articolo</div>\n<dl>\n");
            if (hasCliente)
                clienteHtml.Append($"  <div style=\"grid-column:1/-1\"><dt>Cliente</dt><dd style=\"font-size:{fsData}pt\">{System.Web.HttpUtility.HtmlEncode(scheda.Cliente!)}</dd></div>\n");
            if (!string.IsNullOrWhiteSpace(scheda.CodiceArticolo))
                clienteHtml.Append($"  <div><dt>Codice articolo</dt><dd style=\"font-size:{fsData}pt\">{System.Web.HttpUtility.HtmlEncode(scheda.CodiceArticolo)}</dd></div>\n");
            if (hasArticolo)
                clienteHtml.Append($"  <div style=\"grid-column:2/-1\"><dt>Descrizione articolo</dt><dd style=\"font-size:{fsData}pt\">{System.Web.HttpUtility.HtmlEncode(scheda.ArticoloDescrizione!)}</dd></div>\n");
            clienteHtml.Append("</dl>\n");
        }

        var dataChiusuraHtml = scheda.DataChiusura.HasValue
            ? $"  <div><dt>Data chiusura</dt><dd style=\"font-size:{fsData}pt\">{scheda.DataChiusura.Value:dd/MM/yyyy HH:mm}</dd></div>\n"
            : "";

        var logoBlock = logoInlineHtml != null
            ? $"  <div style=\"margin-bottom:6px\">{logoInlineHtml}</div>\n"
            : "";

        var titolo = $"ManutCassa_{scheda.CodiceCassa}_{scheda.DataEsecuzione:yyyyMMdd}";

        return "<!DOCTYPE html>\n" +
               "<html lang=\"it\">\n" +
               "<head>\n" +
               "  <meta charset=\"utf-8\" />\n" +
               $"  <title>{titolo}</title>\n" +
               "  <style>\n" +
               $"    * {{ box-sizing: border-box; margin: 0; padding: 0; }}\n" +
               $"    body {{ font-family: Arial, sans-serif; font-size: {fsBase}pt; color: #111; background: #fff;" +
               "            padding: 10mm 15mm; display: flex; flex-direction: column; min-height: calc(297mm - 30mm); }}\n" +
               "    @page { margin: 12mm 18mm 18mm 18mm; }\n" +
               $"    h1 {{ font-size: {fsH}pt; font-weight: 800; letter-spacing: 0.5px; margin-bottom: 3px; }}\n" +
               $"    .meta {{ font-size: {fsT}pt; color: #555; margin-bottom: 10px; }}\n" +
               $"    .section-title {{ font-size: {fsS}pt; font-weight: bold; text-transform: uppercase;\n" +
               "                       border-bottom: 1.5px solid #111; padding-bottom: 2px; margin: 14px 0 7px; }}\n" +
               "    dl { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px 12px; margin-bottom: 10px; }\n" +
               $"    dt {{ font-size: {fsT}pt; color: #666; margin-bottom: 1px; }}\n" +
               $"    dd {{ font-size: {fsData}pt; font-weight: 600; }}\n" +
               "    table.att { width: 100%; border-collapse: collapse; margin-top: 4px; }\n" +
               $"    table.att th {{ background: #f2f2f2; border: 1px solid #bbb; padding: 5px 8px; font-size: {fsT}pt; text-align: left;\n" +
               "                    -webkit-print-color-adjust: exact; print-color-adjust: exact; }}\n" +
               $"    table.att td {{ border: 1px solid #ddd; padding: 5px 8px; font-size: {fsBase}pt; vertical-align: top; }}\n" +
               "    .riepilogo { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 14px; }\n" +
               "    .rie-box { border: 1px solid #ccc; border-radius: 4px; padding: 10px; text-align: center;\n" +
               "               -webkit-print-color-adjust: exact; print-color-adjust: exact; }\n" +
               $"    .rie-num {{ font-size: {(fs + 6m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}pt; font-weight: 800; }}\n" +
               $"    .rie-label {{ font-size: {fsT}pt; color: #666; }}\n" +
               $"    .conditions {{ border: 1px solid #1565c0; border-radius: 4px; padding: 10px 14px;\n" +
               $"                   margin-top: auto; font-size: {fsCond}pt; line-height: 1.65;\n" +
               "                   -webkit-print-color-adjust: exact; print-color-adjust: exact; }}\n" +
               $"    .conditions .cond-title {{ font-weight: bold; color: #1565c0; font-size: {fsS}pt; margin-bottom: 5px; }}\n" +
               "    .conditions ul { padding-left: 16px; }\n" +
               "    .conditions li { margin-bottom: 3px; }\n" +
               "    @media print { body { padding: 0; } -webkit-print-color-adjust: exact; print-color-adjust: exact; }\n" +
               "  </style>\n" +
               "</head>\n" +
               "<body>\n" +
               // Logo + intestazione
               logoBlock +
               "  <hr style=\"border:none;border-top:2.5px solid #111;margin:7px 0 10px\" />\n" +
               "  <h1>Scheda Manutenzione Cassa d'Anima</h1>\n" +
               $"  <div class=\"meta\">{System.Web.HttpUtility.HtmlEncode(azienda)} &bull; Documento generato il {DateTime.Now:dd/MM/yyyy HH:mm}</div>\n\n" +
               // Dati Cliente / Articolo
               clienteHtml +
               // Dati Attrezzatura
               "  <div class=\"section-title\">Dati Attrezzatura</div>\n" +
               "  <dl>\n" +
               $"    <div><dt>Rif. #</dt><dd style=\"color:#1565c0;font-size:{(fs + 3m).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}pt;font-weight:800\">{scheda.CodiceRiferimento}</dd></div>\n" +
               $"    <div><dt>Codice cassa</dt><dd style=\"font-size:{fsData}pt\">{System.Web.HttpUtility.HtmlEncode(scheda.CodiceCassa)}</dd></div>\n" +
               $"    <div><dt>Data esecuzione</dt><dd style=\"font-size:{fsData}pt\">{scheda.DataEsecuzione:dd/MM/yyyy}</dd></div>\n" +
               $"    <div><dt>Operatore</dt><dd style=\"font-size:{fsData}pt\">{System.Web.HttpUtility.HtmlEncode(scheda.NomeOperatore ?? "—")}</dd></div>\n" +
               $"    <div><dt>Stato</dt><dd style=\"color:{statoColor};font-size:{fsData}pt\">{statoLabel}</dd></div>\n" +
               dataChiusuraHtml +
               "  </dl>\n\n" +
               noteHtml +
               // Tabella attività
               (righeHtml.Length > 0
                   ? "  <div class=\"section-title\">Attività Manutentive</div>\n" +
                     "  <table class=\"att\">\n" +
                     $"    <thead><tr><th style=\"width:36px\">#</th><th>Attività</th><th style=\"width:130px\">Esito</th><th>Commento / Anomalia</th></tr></thead>\n" +
                     $"    <tbody>\n{righeHtml}    </tbody>\n" +
                     "  </table>\n\n"
                   : "") +
               // Riepilogo
               (totOk + totAnomalie > 0
                   ? "  <div class=\"section-title\">Riepilogo</div>\n" +
                     "  <div class=\"riepilogo\">\n" +
                     $"    <div class=\"rie-box\"><div class=\"rie-num\" style=\"color:#1b5e20\">{totOk}</div><div class=\"rie-label\">OK</div></div>\n" +
                     $"    <div class=\"rie-box\"><div class=\"rie-num\" style=\"color:#b71c1c\">{totAnomalie}</div><div class=\"rie-label\">Anomalie</div></div>\n" +
                     "  </div>\n\n"
                   : "") +
               // Allegati
               allegatiHtml +
               // Condizioni generali
               "\n  <div class=\"conditions\" style=\"margin-top:20px\">\n" +
               "    <div class=\"cond-title\">CONDIZIONI GENERALI DI MANUTENZIONE</div>\n" +
               "    <ul>\n" +
               "      <li>Le attivit&agrave; di manutenzione vengono eseguite da personale qualificato nel rispetto delle normative vigenti in materia di sicurezza sul lavoro (D.Lgs. 81/2008).</li>\n" +
               "      <li>La presente scheda costituisce documento ufficiale di avvenuta manutenzione e deve essere conservata agli atti per almeno <strong>5 anni</strong>.</li>\n" +
               "      <li>Qualsiasi anomalia rilevata durante l&apos;intervento deve essere segnalata tempestivamente al responsabile di reparto e all&apos;ufficio tecnico prima di riprendere la produzione.</li>\n" +
               "      <li>La cassa deve essere rimessa in servizio esclusivamente a seguito di verifica con esito positivo da parte dell&apos;operatore responsabile. In caso di anomalie non risolte, la cassa non deve essere utilizzata.</li>\n" +
               "      <li>I materiali di consumo impiegati (lubrificanti, guarnizioni, prodotti chimici) devono essere conformi alle specifiche tecniche del fornitore della cassa e compatibili con il tipo di sabbia e resina utilizzati.</li>\n" +
               "      <li>Ogni sostituzione di componenti (inserti, piastre, perni) deve essere documentata indicando il codice del pezzo di ricambio e il fornitore, e registrata nel sistema gestionale aziendale.</li>\n" +
               "      <li>Le fotografie allegate hanno valore documentale e devono ritrarre fedelmente lo stato della cassa prima, durante e dopo l&apos;intervento; non &egrave; ammessa alterazione delle immagini.</li>\n" +
               "      <li>In caso di manutenzione straordinaria non pianificata, il responsabile di produzione deve approvare la scheda prima della rimessa in produzione della cassa.</li>\n" +
               "      <li>Il piano di manutenzione periodica pu&ograve; essere modificato solo dall&apos;ufficio tecnico sulla base dell&apos;andamento storico delle anomalie rilevate e delle indicazioni del costruttore dell&apos;attrezzatura.</li>\n" +
               "      <li>I dati contenuti in questa scheda sono riservati e ad uso interno; la diffusione a terzi &egrave; subordinata all&apos;autorizzazione della direzione aziendale.</li>\n" +
               "      <li>La firma di chiusura scheda attesta che l&apos;operatore ha eseguito tutte le attivit&agrave; previste con la diligenza richiesta e che le informazioni riportate sono veritiere e complete.</li>\n" +
               "      <li>Per le casse soggette a garanzia del costruttore, il mancato rispetto del piano di manutenzione programmata pu&ograve; invalidare la garanzia stessa; &egrave; responsabilit&agrave; del manutentore verificarne i termini.</li>\n" +
               "    </ul>\n" +
               "  </div>\n\n" +
               "</body>\n" +
               "</html>";
    }

}
