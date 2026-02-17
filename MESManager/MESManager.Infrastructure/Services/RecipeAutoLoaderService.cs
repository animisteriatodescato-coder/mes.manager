using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Application.Services;
using MESManager.Infrastructure.Data;
using System.Collections.Concurrent;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio event-driven per auto-caricamento ricette su DB55 (offset 100+)
/// TRIGGER: cambio commessa rilevato da PlcSync su DB55
/// </summary>
public class RecipeAutoLoaderService : IRecipeAutoLoaderService
{
    private readonly ILogger<RecipeAutoLoaderService> _logger;
    private readonly MesManagerDbContext _context;
    private readonly IRicettaGanttService _ricettaService;
    private readonly IPlcRecipeWriterService _recipeWriter;
    
    // Cache stato interno
    private readonly ConcurrentDictionary<Guid, string> _ultimoBarcodeMacchina = new();
    private readonly ConcurrentDictionary<Guid, bool> _plcOnlineStatus = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    
    public RecipeAutoLoaderService(
        ILogger<RecipeAutoLoaderService> logger,
        MesManagerDbContext context,
        IRicettaGanttService ricettaService,
        IPlcRecipeWriterService recipeWriter)
    {
        _logger = logger;
        _context = context;
        _ricettaService = ricettaService;
        _recipeWriter = recipeWriter;
    }
    
    public async Task OnCommessaCambiataAsync(Guid macchinaId, string nuovoBarcode, CancellationToken ct = default)
    {
        _logger.LogInformation("🔔 [AUTO-LOAD] Commessa cambiata su macchina {Id}: {Barcode}", 
            macchinaId, nuovoBarcode);
        
        // Verifica se è cambio reale
        if (_ultimoBarcodeMacchina.TryGetValue(macchinaId, out var vecchioBarcode) 
            && vecchioBarcode == nuovoBarcode)
        {
            return; // Nessun cambio
        }
        
        // Aggiorna cache
        _ultimoBarcodeMacchina[macchinaId] = nuovoBarcode;
        
        // Verifica PLC online
        if (!_plcOnlineStatus.GetValueOrDefault(macchinaId, true)) // Default true per primo avvio
        {
            _logger.LogWarning("⚠️ [AUTO-LOAD] PLC offline per macchina {Id} - BLOCCO caricamento", macchinaId);
            return; // BLOCCA se offline
        }
        
        // Lock per evitare race conditions
        await _writeLock.WaitAsync(ct);
        
        try
        {
            // 1. Identifica prossima commessa dal Gantt
            var prossimaCommessa = await GetProssimaCommessaDalGanttAsync(macchinaId, ct);
            
            if (prossimaCommessa == null)
            {
                _logger.LogInformation("ℹ️ [AUTO-LOAD] Nessuna commessa successiva programmata per macchina {Id}", 
                    macchinaId);
                return;
            }
            
            var codiceArticolo = prossimaCommessa.Articolo?.Codice;
            if (string.IsNullOrEmpty(codiceArticolo))
            {
                _logger.LogWarning("⚠️ [AUTO-LOAD] Commessa {Codice} non ha articolo valido", 
                    prossimaCommessa.Codice);
                return;
            }
            
            _logger.LogInformation("📌 [AUTO-LOAD] Prossima commessa identificata: {Codice} (Articolo: {Articolo})", 
                prossimaCommessa.Codice, codiceArticolo);
            
            // 2. Carica ricetta articolo
            var ricetta = await _ricettaService.GetRicettaByCodiceArticoloAsync(codiceArticolo);
            
            if (ricetta == null || ricetta.TotaleParametri == 0)
            {
                _logger.LogWarning("⚠️ [AUTO-LOAD] Nessuna ricetta trovata per articolo {Codice}", 
                    codiceArticolo);
                return;
            }
            
            _logger.LogInformation("📖 [AUTO-LOAD] Ricetta caricata: {Articolo} ({Parametri} parametri)", 
                ricetta.CodiceArticolo, ricetta.TotaleParametri);
            
            // 3. SCRIVE DB55(offset 100+) automaticamente
            var result = await _recipeWriter.WriteRecipeToDb56Async(macchinaId, ricetta, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ [AUTO-LOAD] Ricetta {Articolo} caricata in DB55(offset 100+) - Prossima lavorazione pronta", 
                    codiceArticolo);
                
                // 4. Aggiorna stato commessa (opzionale - per tracking)
                await AggiornaStatoCommessaAsync(prossimaCommessa.Id, "RicettaPrecaricata", ct);
            }
            else
            {
                _logger.LogError("❌ [AUTO-LOAD] Errore scrittura DB55(offset 100+): {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [AUTO-LOAD] Eccezione durante caricamento automatico ricetta");
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    public async Task<RecipeWriteResult> LoadNextRecipeManualAsync(Guid macchinaId, CancellationToken ct = default)
    {
        _logger.LogInformation("🔧 [MANUAL-LOAD] Caricamento manuale prossima ricetta per macchina {Id}", macchinaId);
        
        try
        {
            // Forza caricamento prossima ricetta (stessa logica auto-load ma senza check PLC offline)
            var prossimaCommessa = await GetProssimaCommessaDalGanttAsync(macchinaId, ct);
            
            if (prossimaCommessa == null)
            {
                return new RecipeWriteResult 
                { 
                    Success = false, 
                    ErrorMessage = "Nessuna commessa successiva programmata" 
                };
            }
            
            var codiceArticolo = prossimaCommessa.Articolo?.Codice;
            if (string.IsNullOrEmpty(codiceArticolo))
            {
                return new RecipeWriteResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Commessa {prossimaCommessa.Codice} non ha articolo valido" 
                };
            }
            
            var ricetta = await _ricettaService.GetRicettaByCodiceArticoloAsync(codiceArticolo);
            
            if (ricetta == null)
            {
                return new RecipeWriteResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Ricetta non trovata per articolo {codiceArticolo}" 
                };
            }
            
            var result = await _recipeWriter.WriteRecipeToDb56Async(macchinaId, ricetta, ct);
            
            if (result.Success)
            {
                _logger.LogInformation("✅ [MANUAL-LOAD] Ricetta caricata manualmente: {Articolo}", 
                    codiceArticolo);
                
                await AggiornaStatoCommessaAsync(prossimaCommessa.Id, "RicettaPrecaricata", ct);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MANUAL-LOAD] Eccezione");
            return new RecipeWriteResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }
    
    public void UpdatePlcStatus(Guid macchinaId, bool isOnline)
    {
        var wasOffline = !_plcOnlineStatus.GetValueOrDefault(macchinaId, false);
        _plcOnlineStatus[macchinaId] = isOnline;
        
        // AUTO-RECOVERY: se PLC torna online dopo essere stato offline
        if (isOnline && wasOffline)
        {
            _logger.LogInformation("🔄 [AUTO-RECOVERY] PLC macchina {Id} torna online - riprovo caricamento ricetta", 
                macchinaId);
            
            // Trigger caricamento in background
            _ = Task.Run(async () =>
            {
                var ultimoBarcode = _ultimoBarcodeMacchina.GetValueOrDefault(macchinaId, string.Empty);
                if (!string.IsNullOrEmpty(ultimoBarcode))
                {
                    await OnCommessaCambiataAsync(macchinaId, ultimoBarcode);
                }
            });
        }
        else if (!isOnline && !wasOffline)
        {
            _logger.LogWarning("⚠️ [STATUS-UPDATE] PLC macchina {Id} va OFFLINE - caricamenti bloccati", macchinaId);
        }
    }
    
    public async Task<(string? CodiceArticolo, bool IsLoaded)> GetNextRecipeStatusAsync(
        Guid macchinaId, 
        CancellationToken ct = default)
    {
        try
        {
            var prossima = await GetProssimaCommessaDalGanttAsync(macchinaId, ct);
            
            if (prossima == null)
            {
                return (null, false);
            }
            
            var codiceArticolo = prossima.Articolo?.Codice;
            
            // TODO: verificare se ricetta è già in DB55(offset 100+) confrontando i parametri ricetta
            // Per ora assumiamo loaded = true se prossima esiste
            return (codiceArticolo, true);
        }
        catch
        {
            return (null, false);
        }
    }
    
    /// <summary>
    /// Identifica la prossima commessa in coda dal Gantt
    /// Logica: ordineSequenza minimo tra commesse "Programmata" sulla stessa macchina
    /// </summary>
    private async Task<Domain.Entities.Commessa?> GetProssimaCommessaDalGanttAsync(
        Guid macchinaId, 
        CancellationToken ct)
    {
        var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
        if (macchina == null) return null;
        
        // Usa OrdineVisualizazione come numero macchina
        int numeroMacchina = macchina.OrdineVisualizazione;
        
        // Trova commessa con ordineSequenza minimo tra quelle programmate
        var prossima = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == numeroMacchina
                     && c.StatoProgramma == Domain.Enums.StatoProgramma.Programmata
                     && c.DataInizioPrevisione != null)
            .OrderBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataInizioPrevisione)
            .FirstOrDefaultAsync(ct);
        
        return prossima;
    }
    
    private async Task AggiornaStatoCommessaAsync(Guid commessaId, string nuovoStato, CancellationToken ct)
    {
        try
        {
            var commessa = await _context.Commesse.FindAsync(new object[] { commessaId }, ct);
            if (commessa != null)
            {
                // TODO: aggiungere campo RicettaStatus o usare Note
                // Per ora log only
                _logger.LogInformation("📝 Stato commessa {Id} aggiornato: {Stato}", commessaId, nuovoStato);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore aggiornamento stato commessa {Id}", commessaId);
        }
    }
}
