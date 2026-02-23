using Microsoft.EntityFrameworkCore;
using MESManager.Infrastructure.Data;
using MESManager.Domain.Entities;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio gestione lavorazioni anime e calcolo automatico prezzi
/// </summary>
public class WorkProcessingService : IWorkProcessingService
{
    private readonly MesManagerDbContext _context;

    public WorkProcessingService(MesManagerDbContext context)
    {
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    // TIPI LAVORAZIONE
    // ═══════════════════════════════════════════════════════════

    public async Task<List<WorkProcessingTypeDto>> GetAllTypesAsync(bool onlyActive = true)
    {
        var query = _context.WorkProcessingTypes
            .Include(t => t.Parametri.Where(p => p.IsCurrent))
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(t => t.Attivo && !t.Archiviato);
        }

        var types = await query
            .OrderBy(t => t.Ordinamento)
            .ThenBy(t => t.Nome)
            .ToListAsync();

        return types.Select(MapToDto).ToList();
    }

    public async Task<WorkProcessingTypeDto?> GetTypeByIdAsync(Guid id)
    {
        var type = await _context.WorkProcessingTypes
            .Include(t => t.Parametri.Where(p => p.IsCurrent))
            .FirstOrDefaultAsync(t => t.Id == id);

        return type != null ? MapToDto(type) : null;
    }

    public async Task<WorkProcessingTypeDto?> GetTypeByCodeAsync(string codice)
    {
        var type = await _context.WorkProcessingTypes
            .Include(t => t.Parametri.Where(p => p.IsCurrent))
            .FirstOrDefaultAsync(t => t.Codice == codice);

        return type != null ? MapToDto(type) : null;
    }

    public async Task<WorkProcessingTypeDto> CreateTypeAsync(WorkProcessingTypeSaveDto dto, string? userId = null)
    {
        // Validazione codice univoco
        if (await _context.WorkProcessingTypes.AnyAsync(t => t.Codice == dto.Codice))
        {
            throw new InvalidOperationException($"Esiste già un tipo lavorazione con codice '{dto.Codice}'");
        }

        var entity = new WorkProcessingType
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Codice = dto.Codice,
            Descrizione = dto.Descrizione,
            Categoria = dto.Categoria,
            Ordinamento = dto.Ordinamento,
            Attivo = dto.Attivo,
            Archiviato = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.WorkProcessingTypes.Add(entity);
        await _context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<WorkProcessingTypeDto> UpdateTypeAsync(WorkProcessingTypeSaveDto dto, string? userId = null)
    {
        if (!dto.Id.HasValue)
        {
            throw new ArgumentException("Id obbligatorio per aggiornamento");
        }

        var entity = await _context.WorkProcessingTypes.FindAsync(dto.Id.Value);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Tipo lavorazione {dto.Id} non trovato");
        }

        // Validazione codice univoco (escludendo se stesso)
        if (await _context.WorkProcessingTypes.AnyAsync(t => t.Codice == dto.Codice && t.Id != dto.Id))
        {
            throw new InvalidOperationException($"Esiste già un tipo lavorazione con codice '{dto.Codice}'");
        }

        entity.Nome = dto.Nome;
        entity.Codice = dto.Codice;
        entity.Descrizione = dto.Descrizione;
        entity.Categoria = dto.Categoria;
        entity.Ordinamento = dto.Ordinamento;
        entity.Attivo = dto.Attivo;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<bool> ArchiveTypeAsync(Guid id, string? userId = null)
    {
        var entity = await _context.WorkProcessingTypes.FindAsync(id);
        if (entity == null) return false;

        entity.Archiviato = true;
        entity.Attivo = false;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await _context.SaveChangesAsync();
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // PARAMETRI ECONOMICI
    // ═══════════════════════════════════════════════════════════

    public async Task<WorkProcessingParameterDto?> GetCurrentParametersAsync(Guid workProcessingTypeId)
    {
        var param = await _context.WorkProcessingParameters
            .Where(p => p.WorkProcessingTypeId == workProcessingTypeId && p.IsCurrent)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync();

        return param != null ? MapParamToDto(param) : null;
    }

    public async Task<List<WorkProcessingParameterDto>> GetParametersHistoryAsync(Guid workProcessingTypeId)
    {
        var parameters = await _context.WorkProcessingParameters
            .Where(p => p.WorkProcessingTypeId == workProcessingTypeId)
            .OrderByDescending(p => p.ValidFrom)
            .ToListAsync();

        return parameters.Select(MapParamToDto).ToList();
    }

    public async Task<WorkProcessingParameterDto> SaveParametersAsync(WorkProcessingParameterSaveDto dto, string? userId = null)
    {
        // Verifica esistenza tipo lavorazione
        var typeExists = await _context.WorkProcessingTypes.AnyAsync(t => t.Id == dto.WorkProcessingTypeId);
        if (!typeExists)
        {
            throw new KeyNotFoundException($"Tipo lavorazione {dto.WorkProcessingTypeId} non trovato");
        }

        var now = DateTime.UtcNow;

        // Se richiesto versioning, invalida versione corrente
        if (dto.ImposeNewVersion)
        {
            var currentParams = await _context.WorkProcessingParameters
                .Where(p => p.WorkProcessingTypeId == dto.WorkProcessingTypeId && p.IsCurrent)
                .ToListAsync();

            foreach (var p in currentParams)
            {
                p.IsCurrent = false;
                p.ValidTo = now;
                p.UpdatedAt = now;
                p.UpdatedBy = userId;
            }
        }

        // Crea nuova versione parametri
        var entity = new WorkProcessingParameter
        {
            Id = Guid.NewGuid(),
            WorkProcessingTypeId = dto.WorkProcessingTypeId,
            EuroOra = dto.EuroOra,
            SabbiaCostoKg = dto.SabbiaCostoKg,
            CostoAttrezzatura = dto.CostoAttrezzatura,
            VerniceCostoPezzo = dto.VerniceCostoPezzo,
            VerniciaturaCostoOra = dto.VerniciaturaCostoOra,
            IncollaggioCostoOra = dto.IncollaggioCostoOra,
            ImballaggioOra = dto.ImballaggioOra,
            MargineDefaultPercent = dto.MargineDefaultPercent,
            ValidFrom = now,
            ValidTo = null,
            IsCurrent = true,
            VersionNotes = dto.VersionNotes,
            CreatedAt = now,
            CreatedBy = userId
        };

        _context.WorkProcessingParameters.Add(entity);
        await _context.SaveChangesAsync();

        return MapParamToDto(entity);
    }

    // ═══════════════════════════════════════════════════════════
    // PRICING ENGINE - CALCOLO AUTOMATICO
    // ═══════════════════════════════════════════════════════════

    public async Task<WorkProcessingCalculationResult> CalculatePriceAsync(WorkProcessingCalculationInput input)
    {
        // Validazione input
        var (isValid, errorMessage) = await ValidateCalculationInputAsync(input);
        if (!isValid)
        {
            return new WorkProcessingCalculationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        // Carica parametri economici correnti
        var parameters = await GetCurrentParametersAsync(input.WorkProcessingTypeId);
        if (parameters == null)
        {
            return new WorkProcessingCalculationResult
            {
                Success = false,
                ErrorMessage = $"Parametri economici non configurati per tipo lavorazione {input.WorkProcessingTypeId}"
            };
        }

        // ══════════════ CALCOLO COSTI ══════════════

        // 1. SABBIATURA (spari)
        var costoSabbiatura = parameters.EuroOra / input.SpariOrari;

        // 2. SABBIA (materiale)
        var costoSabbia = parameters.SabbiaCostoKg * input.PesoKg * input.Figure;

        // 3. ATTREZZATURA (ammortizzato su lotto)
        var costoAttrezzatura = parameters.CostoAttrezzatura / input.Lotto;

        // 4. COSTO ANIMA (totale lavorazione primaria)
        var costoAnima = costoSabbiatura + costoSabbia + costoAttrezzatura;

        // ══════════════ COSTI FUORI MACCHINA ══════════════

        // 5. VERNICE (materiale)
        var costoVernice = input.VerniciaturaPezziOra > 0
            ? parameters.VerniceCostoPezzo * input.VernicePesoKg
            : 0;

        // 6. VERNICIATURA (lavorazione)
        var costoVerniciatura = input.VerniciaturaPezziOra > 0
            ? parameters.VerniciaturaCostoOra / (input.Lotto / input.VerniciaturaPezziOra)
            : 0;

        // 7. INCOLLAGGIO
        var costoIncollaggio = input.IncollaggioOre > 0
            ? (parameters.IncollaggioCostoOra * input.IncollaggioOre) / input.Lotto
            : 0;

        // 8. IMBALLO
        var costoImballo = input.ImballaggioOre > 0
            ? (parameters.ImballaggioOra * input.ImballaggioOre) / input.Lotto
            : 0;

        //  9. TOTALE FUORI MACCHINA
        var costoFuoriMacchina = costoVernice + costoVerniciatura + costoIncollaggio + costoImballo;

        // ══════════════ PREZZO FINALE ══════════════

        // 10. COSTO TOTALE AL PEZZO
        var costoTotale = costoAnima + costoFuoriMacchina;

        // 11. MARGINE (usa custom se fornito, altrimenti default)
        var margine = input.MarginePercent ?? parameters.MargineDefaultPercent;

        // 12. PREZZO VENDITA (costo + margine%)
        var prezzoVendita = costoTotale * (1 + (margine / 100));

        // ══════════════ RISULTATO ══════════════

        return new WorkProcessingCalculationResult
        {
            Success = true,
            
            // Breakdown dettagliato
            CostoSabbiatura = Math.Round(costoSabbiatura, 4),
            CostoSabbia = Math.Round(costoSabbia, 4),
            CostoAttrezzatura = Math.Round(costoAttrezzatura, 4),
            CostoAnimaTotale = Math.Round(costoAnima, 4),
            
            CostoVernice = Math.Round(costoVernice, 4),
            CostoVerniciatura = Math.Round(costoVerniciatura, 4),
            CostoIncollaggio = Math.Round(costoIncollaggio, 4),
            CostoImballo = Math.Round(costoImballo, 4),
            CostoFuoriMacchina = Math.Round(costoFuoriMacchina, 4),
            
            CostoTotalePezzo = Math.Round(costoTotale, 4),
            MargineApplicato = margine,
            PrezzoVenditaPezzo = Math.Round(prezzoVendita, 4),
            
            // Tracciabilità parametri usati
            ParametriUsati = parameters
        };
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateCalculationInputAsync(WorkProcessingCalculationInput input)
    {
        // Validazioni business logic

        if (input.PesoKg <= 0)
            return (false, "Peso deve essere maggiore di zero");

        if (input.Figure < 1)
            return (false, "Figure deve essere almeno 1");

        if (input.Lotto < 1)
            return (false, "Lotto deve essere almeno 1");

        if (input.SpariOrari <= 0)
            return (false, "Spari orari deve essere maggiore di zero");

        if (input.VerniciaturaPezziOra < 0)
            return (false, "Pezzi verniciati per ora non può essere negativo");

        if (input.IncollaggioOre < 0)
            return (false, "Ore incollaggio non può essere negativo");

        if (input.ImballaggioOre < 0)
            return (false, "Ore imballaggio non può essere negativo");

        if (input.MarginePercent.HasValue && (input.MarginePercent.Value < 0 || input.MarginePercent.Value > 100))
            return (false, "Margine percentuale deve essere tra 0 e 100");

        // Verifica esistenza tipo lavorazione
        var typeExists = await _context.WorkProcessingTypes  .AnyAsync(t => t.Id == input.WorkProcessingTypeId && t.Attivo);
        
        if (!typeExists)
            return (false, $"Tipo lavorazione {input.WorkProcessingTypeId} non trovato o non attivo");

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    // MAPPING HELPERS
    // ═══════════════════════════════════════════════════════════

    private static WorkProcessingTypeDto MapToDto(WorkProcessingType entity)
    {
        return new WorkProcessingTypeDto
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Codice = entity.Codice,
            Descrizione = entity.Descrizione,
            Categoria = entity.Categoria,
            Ordinamento = entity.Ordinamento,
            Attivo = entity.Attivo,
            Archiviato = entity.Archiviato,
            ParametriCorrenti = entity.Parametri
                .Where(p => p.IsCurrent)
                .Select(MapParamToDto)
                .FirstOrDefault(),
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }

    private static WorkProcessingParameterDto MapParamToDto(WorkProcessingParameter entity)
    {
        return new WorkProcessingParameterDto
        {
            Id = entity.Id,
            WorkProcessingTypeId = entity.WorkProcessingTypeId,
            EuroOra = entity.EuroOra,
            SabbiaCostoKg = entity.SabbiaCostoKg,
            CostoAttrezzatura = entity.CostoAttrezzatura,
            VerniceCostoPezzo = entity.VerniceCostoPezzo,
            VerniciaturaCostoOra = entity.VerniciaturaCostoOra,
            IncollaggioCostoOra = entity.IncollaggioCostoOra,
            ImballaggioOra = entity.ImballaggioOra,
            MargineDefaultPercent = entity.MargineDefaultPercent,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            IsCurrent = entity.IsCurrent,
            VersionNotes = entity.VersionNotes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
    }
}
