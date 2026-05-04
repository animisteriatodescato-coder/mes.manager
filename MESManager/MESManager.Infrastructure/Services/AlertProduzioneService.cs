using Microsoft.EntityFrameworkCore;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Implementazione S4: aggrega alert di produzione da più fonti.
/// Attualmente: Non Conformità aperte.
/// Estendibile in futuro con Manutenzioni, Note, ecc. senza toccare l'interfaccia.
/// </summary>
public class AlertProduzioneService : IAlertProduzioneService
{
    private readonly MesManagerDbContext _db;

    public AlertProduzioneService(MesManagerDbContext db) => _db = db;

    public async Task<Dictionary<string, List<AlertProduzioneDto>>> GetAlertPerArticoliBatchAsync(
        IEnumerable<string> codiciArticolo)
    {
        var codici = codiciArticolo
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!codici.Any())
            return new Dictionary<string, List<AlertProduzioneDto>>(StringComparer.OrdinalIgnoreCase);

        // Unica query batch — indice su (CodiceArticolo, Stato) consigliato per produzione
        var ncAperte = await _db.NonConformita
            .Where(nc => codici.Contains(nc.CodiceArticolo) && nc.Stato != "Chiusa")
            .OrderBy(nc => nc.CodiceArticolo)
            .ThenByDescending(nc => nc.DataSegnalazione)
            .ToListAsync();

        var result = new Dictionary<string, List<AlertProduzioneDto>>(StringComparer.OrdinalIgnoreCase);

        foreach (var nc in ncAperte)
        {
            if (!result.TryGetValue(nc.CodiceArticolo, out var lista))
            {
                lista = new List<AlertProduzioneDto>();
                result[nc.CodiceArticolo] = lista;
            }

            var cliente = string.IsNullOrWhiteSpace(nc.Cliente) ? "" : $"{nc.Cliente}: ";
            var maxLen = 80;
            var desc = nc.Descrizione.Length > maxLen
                ? nc.Descrizione[..maxLen] + "…"
                : nc.Descrizione;

            lista.Add(new AlertProduzioneDto
            {
                SourceId       = nc.Id,
                CodiceArticolo = nc.CodiceArticolo,
                Tipo           = nc.Tipo,
                Gravita        = nc.Gravita,
                Messaggio      = $"{cliente}{desc}",
                Data           = nc.DataSegnalazione,
                Fonte          = "NonConformita"
            });
        }

        return result;
    }
}
