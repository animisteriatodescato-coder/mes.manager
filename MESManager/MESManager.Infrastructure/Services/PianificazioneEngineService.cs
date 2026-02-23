using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Entities;
using MESManager.Domain.Enums;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio ROBUSTO per la gestione avanzata della pianificazione con scheduling industriale.
/// 
/// Funzionalità principali:
/// - Optimistic Concurrency Control (RowVersion)
/// - Segmenti bloccati con ricalcolo intelligente
/// - Priorità e vincoli temporali
/// - Setup dinamico e riduzione per classi consecutive
/// - Transazioni atomiche
/// - Update version per sincronizzazione client
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
    /// Sposta una commessa su una macchina con gestione robusta contro concorrenza, blocchi e vincoli.
    /// </summary>
    public async Task<SpostaCommessaResponse> SpostaCommessaAsync(SpostaCommessaRequest request)
    {
        try
        {
            var updateVersion = DateTime.UtcNow.Ticks;
            
            _logger.LogInformation("[v{UpdateVersion}] Inizio spostamento commessa {CommessaId} su macchina {TargetMacchina}", 
                updateVersion, request.CommessaId, request.TargetMacchina);

            // 0. Validazione input (defense-in-depth)
            if (request.TargetMacchina < 1 || request.TargetMacchina > 99)
            {
                _logger.LogWarning("Service: TargetMacchina non valida: {TargetMacchina}", request.TargetMacchina);
                return new SpostaCommessaResponse
                {
                    Success = false,
                    ErrorMessage = $"Numero macchina non valido: {request.TargetMacchina}. Deve essere tra 1 e 99.",
                    UpdateVersion = updateVersion
                };
            }

            // 1. Carica la commessa con tracking per concurrency
            var commessa = await _context.Commesse
                .Include(c => c.Articolo)
                .FirstOrDefaultAsync(c => c.Id == request.CommessaId);

            if (commessa == null)
            {
                return new SpostaCommessaResponse
                {
                    Success = false,
                    ErrorMessage = $"Commessa {request.CommessaId} non trovata",
                    UpdateVersion = updateVersion
                };
            }

            // 2. Commesse bloccate: permetti spostamento ma sblocca automaticamente
            // (l'utente ha già confermato nel dialog JavaScript)
            bool eraBloccata = commessa.Bloccata;
            if (commessa.Bloccata)
            {
                _logger.LogInformation("⚠️ Spostamento commessa bloccata {CommessaId} - sblocco automatico (utente ha confermato)", request.CommessaId);
                commessa.Bloccata = false; // Sblocca per permettere spostamento
            }
            
            // 3. Permetti spostamento anche se in produzione (con conferma utente già data)
            // Il controllo di sicurezza è già stato fatto nel dialog JavaScript
            if (commessa.StatoProgramma == StatoProgramma.InProduzione)
            {
                _logger.LogInformation("⚠️ Spostamento commessa InProduzione {Codice} - utente ha confermato override", 
                    commessa.Codice);
            }

            // 4. Carica impostazioni, calendario e festivi
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            var calendario = await GetCalendarioLavoroDtoAsync();
            var festivi = await GetFestiviSetAsync();

            // 5. Salva macchina origine
            var macchinaOrigine = commessa.NumeroMacchina;
            
            // 6. GANTT-FIRST: Rispetta posizione esatta dell'utente
            // Normalizza SOLO se cade in orario NON lavorativo (weekend/festivi/notte)
            DateTime dataInizioDesiderata = request.TargetDataInizio ?? DateTime.Now;
            
            // Verifica se la data desiderata è in orario lavorativo
            bool dentroOrarioLavorativo = IsInOrarioLavorativo(dataInizioDesiderata, calendario, festivi);
            
            if (!dentroOrarioLavorativo)
            {
                // Solo se FUORI orario, normalizza al prossimo slot valido
                var dataPrimaDiNormalizzare = dataInizioDesiderata;
                dataInizioDesiderata = NormalizzaSuOrarioLavorativo(dataInizioDesiderata, calendario, festivi);
                _logger.LogInformation("⏰ Data fuori orario lavorativo: normalizzata da {Prima} a {Dopo}",
                    dataPrimaDiNormalizzare, dataInizioDesiderata);
            }
            else
            {
                _logger.LogInformation("✅ Data dentro orario lavorativo: posizione esatta mantenuta {Data}",
                    dataInizioDesiderata);
            }
            
            // 7. Carica commesse macchina destinazione (esclusa quella da spostare)
            var commesseDestinazione = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == request.TargetMacchina && c.Id != request.CommessaId)
                .OrderBy(c => c.DataInizioPrevisione ?? DateTime.MaxValue)
                .ToListAsync();

            // 8. Calcola durata commessa da spostare
            var durataMinuti = CalcolaDurataConSetupDinamico(commessa, null, impostazioni);
            var dataFineCalcolata = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                dataInizioDesiderata,
                durataMinuti,
                calendario,
                festivi
            );

            // 9. CHECK SOVRAPPOSIZIONE con commesse esistenti
            bool hasOverlap = false;
            Commessa? commessaSovrapposta = null;
            
            foreach (var c in commesseDestinazione)
            {
                if (c.DataInizioPrevisione.HasValue && c.DataFinePrevisione.HasValue)
                {
                    // Overlap se: (nuovoInizio < vecchioFine) AND (nuovoFine > vecchioInizio)
                    if (dataInizioDesiderata < c.DataFinePrevisione && dataFineCalcolata > c.DataInizioPrevisione)
                    {
                        hasOverlap = true;
                        commessaSovrapposta = c;
                        break;
                    }
                }
            }

            DateTime dataInizioEffettiva;
            int nuovoOrdine;

            if (hasOverlap && commessaSovrapposta != null)
            {
                // ACCODAMENTO AUTOMATICO: Metti DOPO l'ultima commessa sovrapposta
                _logger.LogInformation("🔄 ACCODAMENTO: Sovrapposizione rilevata con {CodiceCommessa} ({DataInizio}-{DataFine}) - accodamento automatico", 
                    commessaSovrapposta.Codice, 
                    commessaSovrapposta.DataInizioPrevisione,
                    commessaSovrapposta.DataFinePrevisione);
                
                dataInizioEffettiva = commessaSovrapposta.DataFinePrevisione ?? dataInizioDesiderata;
                dataFineCalcolata = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                    dataInizioEffettiva,
                    durataMinuti,
                    calendario,
                    festivi
                );
                
                nuovoOrdine = commessaSovrapposta.OrdineSequenza + 1;
                
                // Shifta ordini successive
                foreach (var c in commesseDestinazione.Where(c => c.OrdineSequenza >= nuovoOrdine && !c.Bloccata))
                {
                    c.OrdineSequenza++;
                }
                
                _logger.LogInformation("✅ Nuova posizione accodata: {DataInizio} (dopo {CommessaPrecedente})", 
                    dataInizioEffettiva, commessaSovrapposta.Codice);
            }
            else
            {
                // POSIZIONE ESATTA: Usa la data desiderata senza modifiche
                _logger.LogInformation("✅ Nessuna sovrapposizione - posizione esatta mantenuta: {DataInizio}", dataInizioDesiderata);
                
                dataInizioEffettiva = dataInizioDesiderata;
                
                // Trova ordine sequenza per data
                var commessaPrecedente = commesseDestinazione
                    .Where(c => c.DataFinePrevisione <= dataInizioEffettiva)
                    .OrderByDescending(c => c.DataFinePrevisione)
                    .FirstOrDefault();
                
                if (commessaPrecedente != null)
                {
                    nuovoOrdine = commessaPrecedente.OrdineSequenza + 1;
                }
                else
                {
                    nuovoOrdine = 1;
                }
                
                // Shifta ordini successive solo se necessario
                foreach (var c in commesseDestinazione.Where(c => c.OrdineSequenza >= nuovoOrdine && !c.Bloccata))
                {
                    c.OrdineSequenza++;
                }
            }

            // 10. Aggiorna commessa spostata con posizione ESATTA
            commessa.NumeroMacchina = request.TargetMacchina;
            commessa.OrdineSequenza = nuovoOrdine;
            commessa.DataInizioPrevisione = dataInizioEffettiva;
            commessa.DataFinePrevisione = dataFineCalcolata;
            commessa.UltimaModifica = DateTime.UtcNow;

            // 11. Ricalcola SOLO commesse successive (non tutta macchina)
            await RicalcolaCommesseSuccessiveAsync(request.TargetMacchina, commessa, impostazioni, calendario, festivi);

            // 12. Ricalcola macchina origine se diversa
            List<CommessaGanttDto>? commesseMacchinaOrigine = null;
            if (macchinaOrigine != null && macchinaOrigine != request.TargetMacchina)
            {
                await RicalcolaMacchinaConBlocchiAsync(macchinaOrigine, impostazioni, calendario, festivi);
                
                var commesseOrigine = await _context.Commesse
                    .Include(c => c.Articolo)
                    .Where(c => c.NumeroMacchina == macchinaOrigine)
                    .OrderBy(c => c.OrdineSequenza)
                    .ToListAsync();
                
                commesseMacchinaOrigine = await MapToGanttDtoBatchAsync(commesseOrigine, impostazioni);
            }

            // 13. Salva con gestione concurrency exception
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict durante spostamento commessa {CommessaId}", request.CommessaId);
                return new SpostaCommessaResponse
                {
                    Success = false,
                    ErrorMessage = "I dati sono stati modificati da un altro utente. Aggiorna e riprova.",
                    UpdateVersion = updateVersion
                };
            }

            // 13. Carica commesse aggiornate destinazione
            var commesseAggiornate = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == request.TargetMacchina)
                .OrderBy(c => c.OrdineSequenza)
                .ToListAsync();

            var macchineCoinvolte = new List<string> { request.TargetMacchina.ToString() };
            if (macchinaOrigine != null && macchinaOrigine != request.TargetMacchina)
            {
                macchineCoinvolte.Add(macchinaOrigine.Value.ToString());
            }

            _logger.LogInformation("[v{UpdateVersion}] Spostamento completato: commessa {CommessaId} → macchina {TargetMacchina} posizione {Ordine}", 
                updateVersion, request.CommessaId, request.TargetMacchina, nuovoOrdine);

            return new SpostaCommessaResponse
            {
                Success = true,
                CommesseAggiornate = await MapToGanttDtoBatchAsync(commesseAggiornate, impostazioni),
                CommesseMacchinaOrigine = commesseMacchinaOrigine,
                UpdateVersion = updateVersion,
                MacchineCoinvolte = macchineCoinvolte
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante spostamento commessa {CommessaId}", request.CommessaId);
            return new SpostaCommessaResponse
            {
                Success = false,
                ErrorMessage = $"Errore interno: {ex.Message}",
                UpdateVersion = DateTime.UtcNow.Ticks
            };
        }
    }

    /// <summary>
    /// Overload pubblico per ricalcolare macchina con blocchi
    /// </summary>
    public async Task RicalcolaMacchinaConBlocchiAsync(int? numeroMacchina)
    {
        var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync() 
            ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };
        var calendario = await GetCalendarioLavoroDtoAsync();
        var festivi = await GetFestiviSetAsync();
        await RicalcolaMacchinaConBlocchiAsync(numeroMacchina, impostazioni, calendario, festivi);
    }

    /// <summary>
    /// Ricalcola una macchina rispettando i blocchi come segmenti fissi.
    /// Algoritmo: le commesse bloccate definiscono "segmenti" fissi, e le commesse non bloccate vengono 
    /// ricalcolate nei "vuoti" tra i blocchi o in coda.
    /// </summary>
    private async Task RicalcolaMacchinaConBlocchiAsync(int? numeroMacchina, ImpostazioniProduzione impostazioni, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
    {
        var commesse = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == numeroMacchina)
            .OrderBy(c => c.Priorita) // Priorità prima (più basse = più urgenti)
            .ThenBy(c => c.OrdineSequenza)
            .ToListAsync();

        if (!commesse.Any()) return;

        // Separa bloccate e non bloccate
        var commesseBloccate = commesse.Where(c => c.Bloccata).OrderBy(c => c.OrdineSequenza).ToList();
        var commesseNonBloccate = commesse.Where(c => !c.Bloccata).OrderBy(c => c.OrdineSequenza).ToList();

        // Le commesse bloccate NON vengono toccate: mantengono le loro date
        // Ricalcola solo le non bloccate "intorno" ai blocchi

        DateTime? cursore = null; // Cursore temporale per accodare

        // Rinumera tutti gli ordini alla fine per coerenza
        int ordineGlobale = 1;

        // Prima passa sulle commesse bloccate per rinumerarle e usarle come vincoli temporali
        foreach (var bloccata in commesseBloccate)
        {
            bloccata.OrdineSequenza = ordineGlobale++;
            // Le date non vengono toccate
        }

        // Ora ricalcola le non bloccate: le accoda sequenzialmente tra/dopo i blocchi
        foreach (var commessa in commesseNonBloccate)
        {
            commessa.OrdineSequenza = ordineGlobale++;

            // 🔒 PRESERVA DATE ESISTENTI se la commessa è già programmata con date valide
            // e non ha vincoli che forzano il ricalcolo
            bool preservaDateEsistenti = commessa.StatoProgramma == StatoProgramma.Programmata 
                && commessa.DataInizioPrevisione.HasValue 
                && commessa.DataFinePrevisione.HasValue
                && !commessa.VincoloDataInizio.HasValue // Nessun vincolo che forza ricalcolo
                && commessa.DataInizioPrevisione.Value > DateTime.Now; // Data ancora futura (non scaduta)

            if (preservaDateEsistenti)
            {
                // Mantieni date esistenti, aggiorna solo cursore per prossima commessa
                cursore = commessa.DataFinePrevisione;
                _logger.LogDebug("🔒 Preservate date per commessa {Codice} (già programmata: {DataInizio})", 
                    commessa.Codice, commessa.DataInizioPrevisione);
                continue;
            }

            // Determina data inizio rispettando vincoli
            DateTime dataInizio;

            // Vincolo DataInizio utente
            if (commessa.VincoloDataInizio.HasValue)
            {
                dataInizio = cursore.HasValue && cursore > commessa.VincoloDataInizio
                    ? cursore.Value
                    : commessa.VincoloDataInizio.Value;
            }
            else
            {
                dataInizio = cursore ?? DateTime.Now;
            }

            // Verifica se c'è una commessa bloccata che inizia prima della data inizio calcolata
            var prossimoBloccato = commesseBloccate
                .Where(b => b.DataInizioPrevisione.HasValue && b.DataInizioPrevisione > dataInizio)
                .OrderBy(b => b.DataInizioPrevisione)
                .FirstOrDefault();

            // Imposta data inizio
            commessa.DataInizioPrevisione = dataInizio;

            // Calcola durata con setup dinamico (considera classe lavorazione precedente)
            Commessa? commessaPrecedente = commesse
                .Where(c => c.OrdineSequenza < commessa.OrdineSequenza)
                .OrderByDescending(c => c.OrdineSequenza)
                .FirstOrDefault();
            
            var durataMinuti = CalcolaDurataConSetupDinamico(commessa, commessaPrecedente, impostazioni);
            
            commessa.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                dataInizio,
                durataMinuti,
                calendario,
                festivi
            );

            // Verifica vincolo DataFine utente (warning, non blocco)
            if (commessa.VincoloDataFine.HasValue && commessa.DataFinePrevisione > commessa.VincoloDataFine)
            {
                _logger.LogWarning("Commessa {CommessaCodice} supera vincolo data fine: previsto {DataFinePrevista}, limite {VincoloDataFine}",
                    commessa.Codice, commessa.DataFinePrevisione, commessa.VincoloDataFine);
                // Il flag warning verrà impostato nel DTO
            }

            // Aggiorna cursore per prossima commessa
            cursore = commessa.DataFinePrevisione;

            // Se c'è un bloccato che inizia prima della fine calcolata, il cursore deve saltare oltre il bloccato
            if (prossimoBloccato != null && prossimoBloccato.DataFinePrevisione.HasValue)
            {
                if (cursore < prossimoBloccato.DataInizioPrevisione)
                {
                    // C'è spazio: OK
                }
                else
                {
                    // Sovrapposizione: sposta cursore dopo il bloccato
                    cursore = prossimoBloccato.DataFinePrevisione.Value;
                }
            }

            commessa.UltimaModifica = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Calcola durata con setup dinamico:
    /// - Se SetupStimatoMinuti è valorizzato, usa quello
    /// - Altrimenti, se ClasseLavorazione è uguale alla precedente, riduce setup del 50%
    /// - Altrimenti, usa setup default da ImpostazioniProduzione
    /// </summary>
    private int CalcolaDurataConSetupDinamico(Commessa commessa, Commessa? commessaPrecedente, ImpostazioniProduzione impostazioni)
    {
        var tempoCiclo = commessa.Articolo?.TempoCiclo ?? 0;
        var numeroFigure = commessa.Articolo?.NumeroFigure ?? 1;

        int setupMinuti;

        if (commessa.SetupStimatoMinuti.HasValue)
        {
            // Override utente
            setupMinuti = commessa.SetupStimatoMinuti.Value;
        }
        else if (!string.IsNullOrEmpty(commessa.ClasseLavorazione) 
                 && !string.IsNullOrEmpty(commessaPrecedente?.ClasseLavorazione)
                 && commessa.ClasseLavorazione == commessaPrecedente.ClasseLavorazione)
        {
            // Stessa classe: riduzione setup 50%
            setupMinuti = impostazioni.TempoSetupMinuti / 2;
            _logger.LogDebug("Riduzione setup per classe lavorazione {Classe}: {SetupRidotto}min", 
                commessa.ClasseLavorazione, setupMinuti);
        }
        else
        {
            // Setup standard
            setupMinuti = impostazioni.TempoSetupMinuti;
        }

        return _pianificazioneService.CalcolaDurataPrevistaMinuti(
            tempoCiclo,
            numeroFigure,
            commessa.QuantitaRichiesta,
            setupMinuti
        );
    }

    /// <summary>
    /// Ricalcola una macchina (versione pubblica).
    /// </summary>
    public async Task RicalcolaAcqueMacchinaAsync(int? numeroMacchina)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };
            var calendario = await GetCalendarioLavoroDtoAsync();
            var festivi = await GetFestiviSetAsync();
            
            await RicalcolaMacchinaConBlocchiAsync(numeroMacchina, impostazioni, calendario, festivi);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Errore ricalcolo macchina {NumeroMacchina}", numeroMacchina);
            throw;
        }
    }

    /// <summary>
    /// Ricalcola tutte le macchine.
    /// </summary>
    public async Task<List<CommessaGanttDto>> RicalcolaTutteCommesseAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };

            var calendario = await GetCalendarioLavoroDtoAsync();
            var festivi = await GetFestiviSetAsync();

            var macchine = await _context.Commesse
                .Where(c => c.NumeroMacchina != null)
                .Select(c => c.NumeroMacchina!)
                .Distinct()
                .ToListAsync();

            foreach (var macchina in macchine)
            {
                await RicalcolaMacchinaConBlocchiAsync(macchina, impostazioni, calendario, festivi);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var tutteCommesse = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina != null)
                .OrderBy(c => c.NumeroMacchina)
                .ThenBy(c => c.OrdineSequenza)
                .ToListAsync();

            return await MapToGanttDtoBatchAsync(tutteCommesse, impostazioni);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Errore ricalcolo tutte commesse");
            throw;
        }
    }

    /// <summary>
    /// Suggerisce la macchina migliore per una commessa (earliest completion time).
    /// </summary>
    public async Task<SuggerisciMacchinaResponse> SuggerisciMacchinaMiglioreAsync(SuggerisciMacchinaRequest request)
    {
        try
        {
            var commessa = await _context.Commesse
                .Include(c => c.Articolo)
                .FirstOrDefaultAsync(c => c.Id == request.CommessaId);

            if (commessa == null)
            {
                return new SuggerisciMacchinaResponse
                {
                    Success = false,
                    ErrorMessage = "Commessa non trovata"
                };
            }

            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? new ImpostazioniProduzione { TempoSetupMinuti = 90, OreLavorativeGiornaliere = 8, GiorniLavorativiSettimanali = 5 };
            
            var calendario = await GetCalendarioLavoroDtoAsync();
            var festivi = await GetFestiviSetAsync();

            // Ottieni macchine candidate
            var macchineStringCandidate = request.NumeriMacchineCandidate != null && request.NumeriMacchineCandidate.Any()
                ? request.NumeriMacchineCandidate
                : await _context.Macchine
                    .Where(m => m.AttivaInGantt)
                    .Select(m => m.Codice.Replace("M0", "").Replace("M", "")) // Estrai numero
                    .ToListAsync();
            
            // Converti in int?
            var macchineCandidate = macchineStringCandidate
                .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
                .Where(m => m.HasValue)
                .Select(m => m!.Value)
                .ToList();

            var valutazioni = new List<ValutazioneMacchina>();

            foreach (var numeroMacchina in macchineCandidate)
            {
                // Carica ultima commessa NON bloccata
                var ultimaCommessa = await _context.Commesse
                    .Where(c => c.NumeroMacchina == numeroMacchina && !c.Bloccata)
                    .OrderByDescending(c => c.DataFinePrevisione)
                    .FirstOrDefaultAsync();

                var dataFineUltima = ultimaCommessa?.DataFinePrevisione ?? DateTime.Now;

                // Calcola durata della nuova commessa
                var durataMinuti = CalcolaDurataConSetupDinamico(commessa, ultimaCommessa, impostazioni);

                var dataInizioPrevista = dataFineUltima;
                var dataFinePrevista = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                    dataInizioPrevista,
                    durataMinuti,
                    calendario,
                    festivi
                );

                var numeroCommesseInCoda = await _context.Commesse
                    .CountAsync(c => c.NumeroMacchina == numeroMacchina && c.DataFineProduzione == null);

                // Calcola carico totale dalla differenza date (approssimazione)
                var commesseMacchina = await _context.Commesse
                    .Where(c => c.NumeroMacchina == numeroMacchina && c.DataFineProduzione == null && c.DataInizioPrevisione.HasValue && c.DataFinePrevisione.HasValue)
                    .ToListAsync();

                var caricoTotale = commesseMacchina.Sum(c => (int)(c.DataFinePrevisione!.Value - c.DataInizioPrevisione!.Value).TotalMinutes);

                valutazioni.Add(new ValutazioneMacchina
                {
                    NumeroMacchina = numeroMacchina.ToString(),
                    NomeMacchina = $"Macchina {numeroMacchina}",
                    DataFineUltimaCommessa = dataFineUltima,
                    DataInizioPrevista = dataInizioPrevista,
                    DataFinePrevista = dataFinePrevista,
                    NumeroCommesseInCoda = numeroCommesseInCoda,
                    CaricoPrevisto = caricoTotale
                });
            }

            // Ordina per earliest completion
            valutazioni = valutazioni.OrderBy(v => v.DataFinePrevista).ToList();
            var migliore = valutazioni.FirstOrDefault();

            if (migliore == null)
            {
                return new SuggerisciMacchinaResponse
                {
                    Success = false,
                    ErrorMessage = "Nessuna macchina disponibile"
                };
            }

            _logger.LogInformation("Suggerita macchina {NumeroMacchina} per commessa {CommessaId} (completion: {DataFinePrevista})",
                migliore.NumeroMacchina, request.CommessaId, migliore.DataFinePrevista);

            return new SuggerisciMacchinaResponse
            {
                Success = true,
                MacchinaSuggerita = migliore.NumeroMacchina,
                NomeMacchina = migliore.NomeMacchina,
                DataFineUltimaCommessa = migliore.DataFineUltimaCommessa,
                DataInizioPrevista = migliore.DataInizioPrevista,
                DataFinePrevista = migliore.DataFinePrevista,
                Valutazioni = valutazioni
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore suggerimento macchina per commessa {CommessaId}", request.CommessaId);
            return new SuggerisciMacchinaResponse
            {
                Success = false,
                ErrorMessage = $"Errore: {ex.Message}"
            };
        }
    }

    #region Festivi (uguale a prima)
    
    /// <summary>
    /// Carica CalendarioLavoro dal database e lo mappa a DTO
    /// </summary>
    private async Task<CalendarioLavoroDto> GetCalendarioLavoroDtoAsync()
    {
        var calendario = await _context.CalendarioLavoro.FirstOrDefaultAsync();
        
        if (calendario == null)
        {
            // Default: Lunedì-Venerdì 08:00-17:00
            return new CalendarioLavoroDto
            {
                Lunedi = true,
                Martedi = true,
                Mercoledi = true,
                Giovedi = true,
                Venerdi = true,
                Sabato = false,
                Domenica = false,
                OraInizio = new TimeOnly(8, 0),
                OraFine = new TimeOnly(17, 0)
            };
        }
        
        return new CalendarioLavoroDto
        {
            Id = calendario.Id,
            Lunedi = calendario.Lunedi,
            Martedi = calendario.Martedi,
            Mercoledi = calendario.Mercoledi,
            Giovedi = calendario.Giovedi,
            Venerdi = calendario.Venerdi,
            Sabato = calendario.Sabato,
            Domenica = calendario.Domenica,
            OraInizio = calendario.OraInizio,
            OraFine = calendario.OraFine
        };
    }
    
    public async Task<HashSet<DateOnly>> GetFestiviSetAsync()
    {
        var annoCorrente = DateTime.Now.Year;
        var festivi = new HashSet<DateOnly>();

        var festiviDb = await _context.Festivi.ToListAsync();

        foreach (var festivo in festiviDb)
        {
            if (festivo.Ricorrente)
            {
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

    public async Task<bool> DeleteFestivoAsync(int id)
    {
        var festivo = await _context.Festivi.FindAsync(id);
        if (festivo == null) return false;

        _context.Festivi.Remove(festivo);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<FestivoDto>> InizializzaFestiviStandardAsync(int anno)
    {
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

        var pasqua = CalcolaPasqua(anno);
        var pasquetta = pasqua.AddDays(1);

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

    #endregion

    #region Mapping

    private async Task<List<CommessaGanttDto>> MapToGanttDtoBatchAsync(List<Commessa> commesse, ImpostazioniProduzione impostazioni)
    {
        // Batch lookup Anime per performance
        var articoloCodes = commesse
            .Where(c => c.Articolo != null)
            .Select(c => c.Articolo!.Codice)
            .Distinct()
            .ToList();
            
        var animeData = await _context.Anime
            .Where(a => articoloCodes.Contains(a.CodiceArticolo))
            .ToListAsync();
            
        var animeLookup = animeData
            .GroupBy(a => a.CodiceArticolo)
            .ToDictionary(g => g.Key, g => g.First());
        
        // Usa il metodo centralizzato di PianificazioneService
        return await _pianificazioneService.MapToGanttDtoBatchAsync(commesse, impostazioni, animeLookup);
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

    /// <summary>
    /// Ricalcola SOLO le commesse successive a quella appena spostata SE ci sono sovrapposizioni
    /// (ottimizzazione GANTT-FIRST: preserva posizioni esatte se non sovrapposte)
    /// </summary>
    private async Task RicalcolaCommesseSuccessiveAsync(int? numeroMacchina, Commessa commessaSpostata, ImpostazioniProduzione impostazioni, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
    {
        var commesseSuccessive = await _context.Commesse
            .Include(c => c.Articolo)
            .Where(c => c.NumeroMacchina == numeroMacchina 
                && c.OrdineSequenza > commessaSpostata.OrdineSequenza
                && !c.Bloccata) // NON ricalcolare bloccate
            .OrderBy(c => c.OrdineSequenza)
            .ToListAsync();

        if (!commesseSuccessive.Any())
        {
            _logger.LogInformation("Nessuna commessa successiva da ricalcolare su macchina {Macchina}", numeroMacchina);
            return;
        }

        _logger.LogInformation("Verifica {Count} commesse successive su macchina {Macchina} per sovrapposizioni", commesseSuccessive.Count, numeroMacchina);

        DateTime? fineCommessaPrecedente = commessaSpostata.DataFinePrevisione;
        int commesseRicalcolate = 0;

        foreach (var c in commesseSuccessive)
        {
            // Verifica se c'è sovrapposizione con la commessa precedente
            bool hasSovrapposizione = false;
            
            if (fineCommessaPrecedente.HasValue && c.DataInizioPrevisione.HasValue)
            {
                // Sovrapposizione se inizio commessa corrente < fine precedente
                hasSovrapposizione = c.DataInizioPrevisione < fineCommessaPrecedente;
            }

            if (hasSovrapposizione || !c.DataInizioPrevisione.HasValue)
            {
                // Ricalcola solo se c'è sovrapposizione o date mancanti
                DateTime dataInizioCorrente = fineCommessaPrecedente ?? DateTime.Now;

                // Rispetta vincoli data inizio se presenti
                if (c.VincoloDataInizio.HasValue && c.VincoloDataInizio > dataInizioCorrente)
                {
                    dataInizioCorrente = c.VincoloDataInizio.Value;
                }

                c.DataInizioPrevisione = dataInizioCorrente;

                var durataMinuti = CalcolaDurataConSetupDinamico(c, null, impostazioni);
                c.DataFinePrevisione = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                    dataInizioCorrente,
                    durataMinuti,
                    calendario,
                    festivi
                );

                commesseRicalcolate++;
                _logger.LogDebug("🔄 Ricalcolata {Codice}: {Inizio} → {Fine} (sovrapposizione rilevata)",
                    c.Codice, c.DataInizioPrevisione, c.DataFinePrevisione);
            }
            else
            {
                _logger.LogDebug("✅ Preservata {Codice}: {Inizio} → {Fine} (nessuna sovrapposizione)",
                    c.Codice, c.DataInizioPrevisione, c.DataFinePrevisione);
            }

            // Aggiorna cursore per prossima iterazione
            fineCommessaPrecedente = c.DataFinePrevisione;
        }
        
        _logger.LogInformation("✅ Ricalcolate {Ricalcolate}/{Totale} commesse successive su macchina {Macchina}",
            commesseRicalcolate, commesseSuccessive.Count, numeroMacchina);
    }

    /// <summary>
    /// Normalizza una data agli orari lavorativi e salta giorni non lavorativi/festivi
    /// </summary>
    /// <summary>
    /// Verifica se una data è dentro orario lavorativo
    /// </summary>
    private bool IsInOrarioLavorativo(DateTime data, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
    {
        // 1. Verifica che non sia festivo
        if (festivi.Contains(DateOnly.FromDateTime(data)))
            return false;
        
        // 2. Verifica che sia giorno lavorativo
        if (!IsGiornoLavorativo(data, calendario))
            return false;
        
        // 3. Verifica che sia dentro orario (con tolleranza 1 ora dopo fine)
        var ora = TimeOnly.FromDateTime(data);
        return ora >= calendario.OraInizio && ora < calendario.OraFine.AddHours(1);
    }
    
    private DateTime NormalizzaSuOrarioLavorativo(DateTime data, CalendarioLavoroDto calendario, HashSet<DateOnly> festivi)
    {
        // 1. Normalizza ora all'inizio lavoro se prima, o giorno dopo se dopo fine lavoro
        var ora = TimeOnly.FromDateTime(data);
        if (ora < calendario.OraInizio)
        {
            data = data.Date + calendario.OraInizio.ToTimeSpan();
        }
        else if (ora >= calendario.OraFine)
        {
            // Se dopo fine lavoro, passa al giorno successivo all'inizio lavoro
            data = data.Date.AddDays(1) + calendario.OraInizio.ToTimeSpan();
        }
        
        // 2. Salta giorni non lavorativi e festivi
        while (!IsGiornoLavorativo(data, calendario) || festivi.Contains(DateOnly.FromDateTime(data)))
        {
            data = data.AddDays(1).Date + calendario.OraInizio.ToTimeSpan();
        }
        
        return data;
    }

    /// <summary>
    /// Verifica se un giorno è lavorativo secondo il calendario
    /// </summary>
    private bool IsGiornoLavorativo(DateTime data, CalendarioLavoroDto calendario)
    {
        return data.DayOfWeek switch
        {
            DayOfWeek.Monday => calendario.Lunedi,
            DayOfWeek.Tuesday => calendario.Martedi,
            DayOfWeek.Wednesday => calendario.Mercoledi,
            DayOfWeek.Thursday => calendario.Giovedi,
            DayOfWeek.Friday => calendario.Venerdi,
            DayOfWeek.Saturday => calendario.Sabato,
            DayOfWeek.Sunday => calendario.Domenica,
            _ => false
        };
    }

    #endregion

    #region Auto-Scheduler Intelligente v1.31

    /// <summary>
    /// 🚀 CARICA SUL GANTT - Auto-scheduling intelligente
    /// 
    /// Algoritmo:
    /// 1. Stima ore necessarie (se non già calcolate)
    /// 2. Trova macchine disponibili per l'articolo
    /// 3. Calcola carico attuale di ogni macchina
    /// 4. Seleziona macchina con minore carico E che rispetta data consegna
    /// 5. Accoda alla fine della coda della macchina selezionata
    /// </summary>
    /// <param name="commessaId">ID della commessa da caricare</param>
    /// <param name="numeroMacchinaManuale">Se specificato, forza il caricamento su questa macchina (bypass auto-scheduler)</param>
    public async Task<CaricaSuGanttResponse> CaricaSuGanttAsync(Guid commessaId, int? numeroMacchinaManuale = null)
    {
        var updateVersion = DateTime.UtcNow.Ticks;
        
        try
        {
            if (numeroMacchinaManuale.HasValue)
            {
                _logger.LogInformation("🚀 [AUTO-SCHEDULER] Caricamento commessa {CommessaId} su macchina manuale {Macchina}", commessaId, numeroMacchinaManuale);
            }
            else
            {
                _logger.LogInformation("🚀 [AUTO-SCHEDULER] Caricamento commessa {CommessaId} sul Gantt (auto-scheduler)", commessaId);
            }
            
            // 1. Carica commessa con articolo
            var commessa = await _context.Commesse
                .Include(c => c.Articolo)
                .FirstOrDefaultAsync(c => c.Id == commessaId);
                
            if (commessa == null)
            {
                return new CaricaSuGanttResponse
                {
                    Success = false,
                    ErrorMessage = "Commessa non trovata",
                    UpdateVersion = updateVersion
                };
            }
            
            // 2. Verifica se già assegnata a una macchina
            if (commessa.NumeroMacchina.HasValue)
            {
                return new CaricaSuGanttResponse
                {
                    Success = false,
                    ErrorMessage = $"Commessa già assegnata a macchina {commessa.NumeroMacchina}. Usare il Gantt per spostarla.",
                    UpdateVersion = updateVersion
                };
            }
            
            // 3. Carica impostazioni, calendario, festivi
            var impostazioni = await _context.ImpostazioniProduzione.FirstOrDefaultAsync()
                ?? throw new Exception("Impostazioni produzione mancanti");
            var impostazioniGantt = await _context.ImpostazioniGantt.FirstOrDefaultAsync();
            var bufferMinuti = impostazioniGantt?.BufferInizioProduzioneMinuti ?? 15;
            
            var calendarioEntity = await _context.CalendarioLavoro.FirstOrDefaultAsync()
                ?? throw new Exception("Calendario lavoro mancante");
            var calendario = new CalendarioLavoroDto
            {
                Lunedi = calendarioEntity.Lunedi,
                Martedi = calendarioEntity.Martedi,
                Mercoledi = calendarioEntity.Mercoledi,
                Giovedi = calendarioEntity.Giovedi,
                Venerdi = calendarioEntity.Venerdi,
                Sabato = calendarioEntity.Sabato,
                Domenica = calendarioEntity.Domenica,
                OraInizio = calendarioEntity.OraInizio,
                OraFine = calendarioEntity.OraFine
            };
            var festivi = await GetFestiviSetAsync();
            
            // 4. Stima ore necessarie (default 8h, TODO: stimare da catalogo anime)
            decimal oreNecessarie = 8m;
            
            // 5. Determina macchina target
            int macchinaSelezionata;
            string motivazione;
            
            if (numeroMacchinaManuale.HasValue)
            {
                // 👉 SELEZIONE MANUALE: usa la macchina specificata
                macchinaSelezionata = numeroMacchinaManuale.Value;
                motivazione = $"Macchina M{macchinaSelezionata:D3} selezionata manualmente dall'utente";
                _logger.LogInformation("👉 Macchina manuale M{Macchina} selezionata dall'utente", macchinaSelezionata);
            }
            else
            {
                // 🤖 AUTO-SCHEDULER: selezione automatica
                // Carica TUTTE le macchine attive nel Gantt (non solo quelle con commesse)
                var macchineAttive = await _context.Macchine
                    .Where(m => m.AttivaInGantt)
                    .OrderBy(m => m.OrdineVisualizazione)
                    .ToListAsync();
                
                // Estrai numeri macchina dai codici (es. "M001" → 1, "M005" → 5)
                var numeriMacchineAttive = macchineAttive
                    .Select(m => {
                        var numStr = m.Codice.Replace("M0", "").Replace("M", "");
                        return int.TryParse(numStr, out var n) ? n : (int?)null;
                    })
                    .Where(n => n.HasValue)
                    .Select(n => n!.Value)
                    .ToList();
                
                // Fallback: se non ci sono macchine configurate, usa macchina 1
                if (!numeriMacchineAttive.Any())
                {
                    numeriMacchineAttive = new List<int> { 1 };
                    _logger.LogWarning("⚠️ Nessuna macchina attiva in Gantt, uso macchina 1 di default");
                }
                
                // 6. Carica tutte le commesse assegnate per calcolare il carico
                var tutteCommesseAssegnate = await _context.Commesse
                    .Where(c => c.NumeroMacchina != null && c.DataInizioPrevisione != null)
                    .OrderBy(c => c.NumeroMacchina)
                    .ThenBy(c => c.OrdineSequenza)
                    .ThenBy(c => c.DataInizioPrevisione)
                    .ToListAsync();
                
                // 7. Calcola carico PER OGNI macchina attiva (anche quelle senza commesse = carico 0)
                var commessePerMacchina = tutteCommesseAssegnate
                    .GroupBy(c => c.NumeroMacchina!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                var caricoPerMacchina = numeriMacchineAttive
                    .Select(numMacchina => {
                        var commesseMacchina = commessePerMacchina.ContainsKey(numMacchina) 
                            ? commessePerMacchina[numMacchina] 
                            : new List<MESManager.Domain.Entities.Commessa>();
                        return new
                        {
                            NumeroMacchina = numMacchina,
                            UltimaDataFine = commesseMacchina.Any() 
                                ? commesseMacchina.Max(c => c.DataFinePrevisione ?? c.DataInizioPrevisione) 
                                : (DateTime?)null,
                            NumeroCommesse = commesseMacchina.Count,
                            OreTotali = commesseMacchina.Sum(c => (c.DataFinePrevisione.HasValue && c.DataInizioPrevisione.HasValue) 
                                ? (decimal)(c.DataFinePrevisione.Value - c.DataInizioPrevisione.Value).TotalHours 
                                : 8m)
                        };
                    })
                    .OrderBy(x => x.OreTotali) // Macchina con MENO carico prima (0h = vuota = priorità!)
                    .ToList();
                
                _logger.LogInformation("📊 Carico macchine (tutte {Count} attive): {Carico}", 
                    numeriMacchineAttive.Count,
                    string.Join(", ", caricoPerMacchina.Select(c => $"M{c.NumeroMacchina}={c.OreTotali:F1}h ({c.NumeroCommesse} comm.)")));
                
                // 8. Seleziona macchina migliore (con meno carico - le vuote hanno 0h!)
                var macchinaScelta = caricoPerMacchina.First();
                macchinaSelezionata = macchinaScelta.NumeroMacchina;
                motivazione = $"Macchina M{macchinaSelezionata:D3} selezionata (carico minimo: {macchinaScelta.OreTotali:F1}h, {macchinaScelta.NumeroCommesse} commesse)";
                
                _logger.LogInformation("✅ Macchina selezionata: M{Macchina} (carico={Ore}h, commesse={N})",
                    macchinaSelezionata, macchinaScelta.OreTotali, macchinaScelta.NumeroCommesse);
            }
            
            // 9. Calcola data inizio e fine
            DateTime dataInizioCalcolata;
            var now = DateTime.Now;
            
            // Carica ultima commessa della macchina selezionata per accodamento
            var ultimaCommessaMacchina = await _context.Commesse
                .Where(c => c.NumeroMacchina == macchinaSelezionata)
                .OrderByDescending(c => c.DataFinePrevisione)
                .FirstOrDefaultAsync();
            
            // Accoda DOPO l'ultima commessa di questa macchina (se esiste)
            if (ultimaCommessaMacchina?.DataFinePrevisione.HasValue == true)
            {
                dataInizioCalcolata = ultimaCommessaMacchina.DataFinePrevisione.Value;
                
                // Normalizza su orario lavorativo
                dataInizioCalcolata = NormalizzaSuOrarioLavorativo(dataInizioCalcolata, calendario, festivi);
            }
            else
            {
                // Macchina vuota: parte dall'inizio orario lavorativo di oggi + BUFFER
                var oraInizioTurno = now.Date.AddHours(calendario.OraInizio.ToTimeSpan().TotalHours);
                var baseStart = now > oraInizioTurno ? now : oraInizioTurno;
                
                // ⭐ BUFFER: Aggiungi tempo configurabile per riorganizzazione prima che vada InProduzione
                dataInizioCalcolata = baseStart.AddMinutes(bufferMinuti);
                
                _logger.LogInformation("🕒 Macchina vuota: buffer di {BufferMinuti} minuti applicato (da {BaseStart} a {DataInizio})",
                    bufferMinuti, baseStart, dataInizioCalcolata);
                
                // Normalizza su orario lavorativo (MA mantieni il buffer!)
                // Se cade fuori orario, sposta al prossimo orario lavorativo
                if (dataInizioCalcolata.TimeOfDay < calendario.OraInizio.ToTimeSpan() || 
                    dataInizioCalcolata.TimeOfDay > calendario.OraFine.ToTimeSpan())
                {
                    dataInizioCalcolata = NormalizzaSuOrarioLavorativo(dataInizioCalcolata, calendario, festivi);
                }
            }
            
            // 10. Calcola data fine con calendario (converti ore in minuti)
            int durataMinuti = (int)(oreNecessarie * 60);
            var dataFineCalcolata = _pianificazioneService.CalcolaDataFinePrevistaConFestivi(
                dataInizioCalcolata,
                durataMinuti,
                calendario,
                festivi
            );
            
            // 11. Assegna alla commessa
            commessa.NumeroMacchina = macchinaSelezionata;
            commessa.DataInizioPrevisione = dataInizioCalcolata;
            commessa.DataFinePrevisione = dataFineCalcolata;
            commessa.StatoProgramma = StatoProgramma.Programmata; // ⭐ Imposta subito come Programmata
            commessa.Bloccata = false; // ⭐ NON bloccata finché non va InProduzione
            commessa.DataCambioStatoProgramma = DateTime.Now;
            commessa.UltimaModifica = DateTime.UtcNow;
            
            // Calcola OrdineSequenza (massimo + 10)
            var maxOrdine = await _context.Commesse
                .Where(c => c.NumeroMacchina == macchinaSelezionata)
                .MaxAsync(c => (int?)c.OrdineSequenza) ?? 0;
            commessa.OrdineSequenza = maxOrdine + 10;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("✨ Commessa {Codice} caricata su M{Macchina}: {DataInizio} → {DataFine} ({Ore}h)",
                commessa.Codice, macchinaSelezionata, dataInizioCalcolata, dataFineCalcolata, oreNecessarie);
            
            // 12. Ricarica commesse aggiornate per response (materializza prima, poi mappa)
            var commesseDb = await _context.Commesse
                .Include(c => c.Articolo)
                .Where(c => c.NumeroMacchina == macchinaSelezionata)
                .OrderBy(c => c.OrdineSequenza)
                .ToListAsync();
            
            var commesseAggiornate = commesseDb.Select(c => new CommessaGanttDto
            {
                Id = c.Id,
                Codice = c.Codice,
                Description = c.Description ?? string.Empty,
                NumeroMacchina = c.NumeroMacchina,
                DataInizioPrevisione = c.DataInizioPrevisione,
                DataFinePrevisione = c.DataFinePrevisione,
                Stato = c.Stato.ToString(),
                ColoreStato = string.Empty, // Verrà calcolato lato client
                StatoProgramma = c.StatoProgramma.ToString(),
                QuantitaRichiesta = c.QuantitaRichiesta,
                UoM = c.UoM,
                Bloccata = c.Bloccata,
                DataConsegna = c.DataConsegna,
                OrdineSequenza = c.OrdineSequenza,
                Priorita = c.Priorita,
                VincoloDataInizio = c.VincoloDataInizio,
                VincoloDataFine = c.VincoloDataFine,
                ClasseLavorazione = c.ClasseLavorazione,
                PercentualeCompletamento = 0m // Default, non calcolato qui
            }).ToList();
            
            return new CaricaSuGanttResponse
            {
                Success = true,
                MacchinaAssegnata = macchinaSelezionata,
                DataInizioCalcolata = dataInizioCalcolata,
                DataFineCalcolata = dataFineCalcolata,
                OreNecessarie = oreNecessarie,
                Motivazione = motivazione,
                CommesseAggiornate = commesseAggiornate,
                UpdateVersion = updateVersion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Errore durante auto-scheduler per commessa {CommessaId}", commessaId);
            return new CaricaSuGanttResponse
            {
                Success = false,
                ErrorMessage = $"Errore: {ex.Message}",
                UpdateVersion = updateVersion
            };
        }
    }

    #endregion
}
