using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per la gestione avanzata della pianificazione con accodamento rigido.
/// Gestisce spostamenti, ricalcoli cascata e validazione anti-sovrapposizione.
/// </summary>
public class PianificazioneEngineService : IPianificazioneEngineService
{
    private readonly MesManagerDbContext _context;
    private readonly IPianificazioneService _pianificazioneService;
    private readonly ILogger<PianificazioneEngineService> _logger;

    public PianificazioneEngineService(
        MesManagerDbContext context,
        IPianificazioneService pianificazioneService,
        ILogger<PianificazioneEngineService> logger)
    {
        _context = context;
        _pianificazioneService = pianificazioneService;
        _logger = logger;
    }

    /// <summary>
    /// Sposta una commessa su una macchina con gestione accodamento rigido.
    /// </summary>
    public async Task<SpostaCommessaResponse> SpostaCommessaAsync(SpostaCommessaRequest request)
    {
        try
        {
            _logger.LogInformation("Inizio spostamento commessa {CommessaId} su macchina {TargetMacchina}", 
                request.CommessaId, request.TargetMacchina);

            // 1. Carica la commessa con l'articolo
            var commessa = await _context.Commesse
                .Include(c => c.Articolo)
                .FirstOrDefaultAsync(c => c.Id == request.CommessaId);

            if (commessa == null)
            {
                return new SpostaCommessaResponse
                {
                    Success = false,
                    ErrorMessage = $"Commessa con ID {request.CommessaId} non trovata"
                };
            }

            // 2. Verifica che la commessa non sia già in produzione
            if (commessa.DataInizioProduzione != null && commessa.DataFineProduzione == null)
            {
                return new SpostaCommessaResponse
                {
                    Success = false,
                    ErrorMessage = "Impossibile spostare una commessa già in produzione"
                };
            }

            // 3. Carica le impostazioni
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            // 4. Carica i festivi
            var festivi = await GetFestiviSetAsync();

            // 5. Salva la macchina di origine per eventuale ricalcolo
            var macchinaOrigine = commessa.NumeroMacchina;
            var targetMacchinaStr = request.TargetMacchina.ToString();

            // 6. Carica tutte le commesse della macchina di destinazione (esclusa quella da spostare)
            var commesseDestinazione = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == targetMacchinaStr && c.Id != request.CommessaId)
                .OrderBy(c => c.OrdineSequenza)
                .ThenBy(c => c.DataInizioPrevisione)
                .ToListAsync();

            // 7. Determina la posizione di inserimento
            int nuovoOrdine;
            DateTime dataInizio;

            if (request.InsertBeforeCommessaId.HasValue)
            {
                // Inserisci PRIMA di una specifica commessa
                var insertBefore = commesseDestinazione.FirstOrDefault(c => c.Id == request.InsertBeforeCommessaId);
                if (insertBefore == null)
                {
                    return new SpostaCommessaResponse
                    {
                        Success = false,
                        ErrorMessage = "Commessa di riferimento per inserimento non trovata sulla macchina di destinazione"
                    };
                }

                nuovoOrdine = insertBefore.OrdineSequenza;
                
                // Shifta tutte le commesse successive
                foreach (var c in commesseDestinazione.Where(c => c.OrdineSequenza >= nuovoOrdine))
                {
                    c.OrdineSequenza++;
                }

                // Data inizio = inizio della commessa prima cui inseriamo (verrà ricalcolata nella cascata)
                var commessaPrecedente = commesseDestinazione
                    .Where(c => c.OrdineSequenza < nuovoOrdine)
                    .OrderByDescending(c => c.OrdineSequenza)
                    .FirstOrDefault();

                dataInizio = commessaPrecedente?.DataFinePrevisione ?? request.TargetDataInizio ?? DateTime.Now;
            }
            else if (request.TargetDataInizio.HasValue)
            {
                // Data specifica richiesta - verifica sovrapposizioni
                dataInizio = request.TargetDataInizio.Value;
                
                // Trova la posizione corretta basata sulla data
                var commesseOrdinatePerData = commesseDestinazione
                    .OrderBy(c => c.DataInizioPrevisione ?? DateTime.MaxValue)
                    .ToList();

                // Trova la prima commessa che inizia dopo la data richiesta
                var commessaDopoDataRichiesta = commesseOrdinatePerData
                    .FirstOrDefault(c => c.DataInizioPrevisione >= dataInizio);

                if (commessaDopoDataRichiesta != null)
                {
                    nuovoOrdine = commessaDopoDataRichiesta.OrdineSequenza;
                    
                    // Shifta tutte le commesse dalla posizione in poi
                    foreach (var c in commesseDestinazione.Where(c => c.OrdineSequenza >= nuovoOrdine))
                    {
                        c.OrdineSequenza++;
                    }
                }
                else
                {
                    // Accoda in fondo
                    nuovoOrdine = (commesseDestinazione.Max(c => (int?)c.OrdineSequenza) ?? 0) + 1;
                    
                    // Ma la data inizio deve essere DOPO l'ultima commessa
                    var ultimaCommessa = commesseDestinazione
                        .OrderByDescending(c => c.DataFinePrevisione)
                        .FirstOrDefault();
                    
                    if (ultimaCommessa?.DataFinePrevisione > dataInizio)
                    {
                        dataInizio = ultimaCommessa.DataFinePrevisione.Value;
                    }
                }
            }
            else
            {
                // Accoda in fondo
                nuovoOrdine = (commesseDestinazione.Max(c => (int?)c.OrdineSequenza) ?? 0) + 1;
                
                // Data inizio = fine dell'ultima commessa
                var ultimaCommessa = commesseDestinazione
                    .OrderByDescending(c => c.DataFinePrevisione)
                    .FirstOrDefault();
                
                dataInizio = ultimaCommessa?.DataFinePrevisione ?? DateTime.Now;
            }

            // 8. Aggiorna la commessa spostata
            commessa.NumeroMacchina = targetMacchinaStr;
            commessa.OrdineSequenza = nuovoOrdine;
            commessa.DataInizioPrevisione = dataInizio;
            commessa.UltimaModifica = DateTime.UtcNow;

            // 9. Calcola la durata e data fine della commessa spostata
            var durataMinuti = CalcolaDurata(commessa, impostazioni);
            commessa.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                dataInizio,
                durataMinuti,
                impostazioni.OreLavorativeGiornaliere,
                impostazioni.GiorniLavorativiSettimanali,
                festivi
            );

            // 10. Ricalcola a cascata tutte le commesse della macchina di destinazione
            await RicalcolaAcqueMacchinaInternalAsync(targetMacchinaStr, impostazioni, festivi);

            // 11. Se la macchina di origine era diversa, ricalcola anche quella
            List<CommessaGanttDto>? commesseMacchinaOrigine = null;
            if (macchinaOrigine != null && macchinaOrigine != targetMacchinaStr)
            {
                await RicalcolaAcqueMacchinaInternalAsync(macchinaOrigine, impostazioni, festivi);
                
                // Carica le commesse aggiornate della macchina origine
                var commesseOrigine = await _context.Commesse
                    .Include(c => c.Articolo)
                    .Where(c => c.NumeroMacchina == macchinaOrigine)
                    .OrderBy(c => c.OrdineSequenza)
                    .ToListAsync();
                
                commesseMacchinaOrigine = commesseOrigine.Select(c => MapToGanttDto(c, impostazioni)).ToList();
            }

            // 12. Salva tutto
            await _context.SaveChangesAsync();

            // 13. Carica le commesse aggiornate della macchina destinazione
            var commesseAggiornate = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == targetMacchinaStr)
                .OrderBy(c => c.OrdineSequenza)
                .ToListAsync();

            _logger.LogInformation("Commessa {CommessaId} spostata su macchina {TargetMacchina} in posizione {Ordine}", 
                request.CommessaId, request.TargetMacchina, nuovoOrdine);

            return new SpostaCommessaResponse
            {
                Success = true,
                CommesseAggiornate = commesseAggiornate.Select(c => MapToGanttDto(c, impostazioni)).ToList(),
                CommesseMacchinaOrigine = commesseMacchinaOrigine
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante spostamento commessa {CommessaId}", request.CommessaId);
            return new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = $"Errore interno: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Ricalcola le date di tutte le commesse di una macchina (versione pubblica dell'interfaccia).
    /// </summary>
    public async Task RicalcolaAcqueMacchinaAsync(string numeroMacchina)
    {
        var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
            ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };
        var festivi = await GetFestiviSetAsync();
        
        await RicalcolaAcqueMacchinaInternalAsync(numeroMacchina, impostazioni, festivi);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Ricalcola le date di tutte le commesse di una macchina in modo sequenziale (accodamento rigido).
    /// </summary>
    private async Task RicalcolaAcqueMacchinaInternalAsync(string numeroMacchina, ImpostazioniProduzione impostazioni, HashSet<DateOnly> festivi)
    {
        var commesse = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == numeroMacchina)
            .OrderBy(c => c.OrdineSequenza)
            .ThenBy(c => c.DataInizioPrevisione)
            .ToListAsync();

        if (!commesse.Any()) return;

        DateTime? dataInizioCorrente = null;

        // Rinumera gli ordini sequenzialmente
        int ordine = 1;
        foreach (var commessa in commesse)
        {
            commessa.OrdineSequenza = ordine++;

            // La prima commessa mantiene la sua data inizio (o usa ora corrente se non impostata)
            if (dataInizioCorrente == null)
            {
                dataInizioCorrente = commessa.DataInizioPrevisione ?? DateTime.Now;
            }

            // Imposta data inizio
            commessa.DataInizioPrevisione = dataInizioCorrente;

            // Calcola durata e data fine
            var durataMinuti = CalcolaDurata(commessa, impostazioni);
            commessa.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                dataInizioCorrente.Value,
                durataMinuti,
                impostazioni.OreLavorativeGiornaliere,
                impostazioni.GiorniLavorativiSettimanali,
                festivi
            );

            // La prossima commessa inizia quando questa finisce (accodamento rigido)
            dataInizioCorrente = commessa.DataFinePrevisione;
            
            commessa.UltimaModifica = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Ricalcola tutte le commesse di tutte le macchine.
    /// </summary>
    public async Task<List<CommessaGanttDto>> RicalcolaTutteCommesseAsync()
    {
        var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
            ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

        var festivi = await GetFestiviSetAsync();

        // Ottieni tutte le macchine che hanno commesse
        var macchine = await _context.Commesse
            .Where(c => c.NumeroMacchina != null)
            .Select(c => c.NumeroMacchina!)
            .Distinct()
            .ToListAsync();

        foreach (var macchina in macchine)
        {
            await RicalcolaAcqueMacchinaInternalAsync(macchina, impostazioni, festivi);
        }

        await _context.SaveChangesAsync();

        // Ritorna tutte le commesse aggiornate
        var tutteCommesse = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina != null)
            .OrderBy(c => c.NumeroMacchina)
            .ThenBy(c => c.OrdineSequenza)
            .ToListAsync();

        return tutteCommesse.Select(c => MapToGanttDto(c, impostazioni)).ToList();
    }

    /// <summary>
    /// Ottiene il set di festivi per l'anno corrente e successivo.
    /// </summary>
    public async Task<HashSet<DateOnly>> GetFestiviSetAsync()
    {
        var annoCorrente = DateTime.Now.Year;
        var festivi = new HashSet<DateOnly>();

        var festiviDb = await _context.Festivi.ToListAsync();

        foreach (var festivo in festiviDb)
        {
            if (festivo.Ricorrente)
            {
                // Aggiungi per anno corrente e successivo
                festivi.Add(new DateOnly(annoCorrente, festivo.Data.Month, festivo.Data.Day));
                festivi.Add(new DateOnly(annoCorrente + 1, festivo.Data.Month, festivo.Data.Day));
            }
            else if (festivo.Anno.HasValue && (festivo.Anno == annoCorrente || festivo.Anno == annoCorrente + 1))
            {
                festivi.Add(festivo.Data);
            }
            else
            {
                festivi.Add(festivo.Data);
            }
        }

        return festivi;
    }

    /// <summary>
    /// Ottiene la lista dei festivi.
    /// </summary>
    public async Task<List<FestivoDto>> GetFestiviAsync()
    {
        var festivi = await _context.Festivi
            .OrderBy(f => f.Data)
            .ToListAsync();

        return festivi.Select(f => new FestivoDto
        {
            Id = f.Id,
            Data = f.Data,
            Descrizione = f.Descrizione,
            Ricorrente = f.Ricorrente,
            Anno = f.Anno
        }).ToList();
    }

    /// <summary>
    /// Aggiunge un festivo.
    /// </summary>
    public async Task<FestivoDto> AddFestivoAsync(CreateFestivoRequest request)
    {
        var festivo = new Festivo
        {
            Data = request.Data,
            Descrizione = request.Descrizione,
            Ricorrente = request.Ricorrente,
            Anno = request.Ricorrente ? null : request.Data.Year
        };

        _context.Festivi.Add(festivo);
        await _context.SaveChangesAsync();

        return new FestivoDto
        {
            Id = festivo.Id,
            Data = festivo.Data,
            Descrizione = festivo.Descrizione,
            Ricorrente = festivo.Ricorrente,
            Anno = festivo.Anno
        };
    }

    /// <summary>
    /// Rimuove un festivo.
    /// </summary>
    public async Task<bool> DeleteFestivoAsync(int id)
    {
        var festivo = await _context.Festivi.FindAsync(id);
        if (festivo == null) return false;

        _context.Festivi.Remove(festivo);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Inizializza i festivi standard italiani per un anno.
    /// </summary>
    public async Task<List<FestivoDto>> InizializzaFestiviStandardAsync(int anno)
    {
        // Festivi italiani ricorrenti
        var festiviStandard = new List<(int mese, int giorno, string descrizione)>
        {
            (1, 1, "Capodanno"),
            (1, 6, "Epifania"),
            (4, 25, "Festa della Liberazione"),
            (5, 1, "Festa dei Lavoratori"),
            (6, 2, "Festa della Repubblica"),
            (8, 15, "Ferragosto"),
            (11, 1, "Tutti i Santi"),
            (12, 8, "Immacolata Concezione"),
            (12, 25, "Natale"),
            (12, 26, "Santo Stefano")
        };

        var festiviAggiunti = new List<FestivoDto>();

        foreach (var (mese, giorno, descrizione) in festiviStandard)
        {
            var data = new DateOnly(anno, mese, giorno);
            
            // Verifica se esiste già
            var esistente = await _context.Festivi
                .FirstOrDefaultAsync(f => f.Data.Month == mese && f.Data.Day == giorno && f.Ricorrente);

            if (esistente == null)
            {
                var festivo = new Festivo
                {
                    Data = data,
                    Descrizione = descrizione,
                    Ricorrente = true,
                    Anno = null
                };
                _context.Festivi.Add(festivo);
                
                festiviAggiunti.Add(new FestivoDto
                {
                    Data = data,
                    Descrizione = descrizione,
                    Ricorrente = true
                });
            }
        }

        // Calcola Pasqua e Pasquetta per l'anno specifico
        var pasqua = CalcolaPasqua(anno);
        var pasquetta = pasqua.AddDays(1);

        // Aggiungi Pasquetta (non ricorrente, specifica per anno)
        var pasquettaData = DateOnly.FromDateTime(pasquetta);
        var pasquettaEsistente = await _context.Festivi
            .FirstOrDefaultAsync(f => f.Data == pasquettaData);

        if (pasquettaEsistente == null)
        {
            var festivoPasquetta = new Festivo
            {
                Data = pasquettaData,
                Descrizione = "Lunedì dell'Angelo",
                Ricorrente = false,
                Anno = anno
            };
            _context.Festivi.Add(festivoPasquetta);
            
            festiviAggiunti.Add(new FestivoDto
            {
                Data = pasquettaData,
                Descrizione = "Lunedì dell'Angelo",
                Ricorrente = false,
                Anno = anno
            });
        }

        await _context.SaveChangesAsync();
        
        return festiviAggiunti;
    }

    /// <summary>
    /// Calcola la data di Pasqua usando l'algoritmo di Computus (Gauss/Anonymous Gregorian).
    /// </summary>
    private DateTime CalcolaPasqua(int anno)
    {
        int a = anno % 19;
        int b = anno / 100;
        int c = anno % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mese = (h + l - 7 * m + 114) / 31;
        int giorno = ((h + l - 7 * m + 114) % 31) + 1;
        
        return new DateTime(anno, mese, giorno);
    }

    private int CalcolaDurata(Commessa commessa, ImpostazioniProduzione impostazioni)
    {
        var tempoCiclo = commessa.Articolo?.TempoCiclo ?? 0;
        var numeroFigure = commessa.Articolo?.NumeroFigure ?? 1;
        
        return _pianificazioneService.CalcolaDurataPrevistaMinuti(
            tempoCiclo,
            numeroFigure,
            commessa.QuantitaRichiesta,
            impostazioni.TempoSetupMinuti
        );
    }

    private CommessaGanttDto MapToGanttDto(Commessa commessa, ImpostazioniProduzione impostazioni)
    {
        var tempoCiclo = commessa.Articolo?.TempoCiclo ?? 0;
        var numeroFigure = commessa.Articolo?.NumeroFigure ?? 0;
        
        var durataMinuti = _pianificazioneService.CalcolaDurataPrevistaMinuti(
            tempoCiclo,
            numeroFigure,
            commessa.QuantitaRichiesta,
            impostazioni.TempoSetupMinuti
        );

        return new CommessaGanttDto
        {
            Id = commessa.Id,
            Codice = commessa.Codice,
            Description = commessa.Description ?? "",
            NumeroMacchina = commessa.NumeroMacchina != null ? int.TryParse(commessa.NumeroMacchina, out var num) ? num : (int?)null : null,
            NomeMacchina = !string.IsNullOrEmpty(commessa.NumeroMacchina) ? $"Macchina {commessa.NumeroMacchina}" : null,
            OrdineSequenza = commessa.OrdineSequenza,
            DataInizioPrevisione = commessa.DataInizioPrevisione,
            DataFinePrevisione = commessa.DataFinePrevisione,
            DataInizioProduzione = commessa.DataInizioProduzione,
            DataFineProduzione = commessa.DataFineProduzione,
            QuantitaRichiesta = commessa.QuantitaRichiesta,
            UoM = commessa.UoM,
            DataConsegna = commessa.DataConsegna,
            TempoCicloSecondi = tempoCiclo,
            NumeroFigure = numeroFigure,
            TempoSetupMinuti = impostazioni.TempoSetupMinuti,
            DurataPrevistaMinuti = durataMinuti,
            Stato = commessa.Stato.ToString(),
            ColoreStato = _pianificazioneService.GetColoreStato(commessa.Stato.ToString()),
            PercentualeCompletamento = CalcolaPercentualeCompletamento(commessa)
        };
    }

    private decimal CalcolaPercentualeCompletamento(Commessa commessa)
    {
        if (commessa.Stato == Domain.Enums.StatoCommessa.Completata)
            return 100;

        if (commessa.DataInizioProduzione == null || commessa.DataFinePrevisione == null)
            return 0;

        var now = DateTime.Now;
        if (now < commessa.DataInizioProduzione)
            return 0;

        if (now >= commessa.DataFinePrevisione)
            return 100;

        var totalDuration = (commessa.DataFinePrevisione.Value - commessa.DataInizioProduzione.Value).TotalMinutes;
        var elapsed = (now - commessa.DataInizioProduzione.Value).TotalMinutes;

        return (decimal)(elapsed / totalDuration * 100);
    }
}
