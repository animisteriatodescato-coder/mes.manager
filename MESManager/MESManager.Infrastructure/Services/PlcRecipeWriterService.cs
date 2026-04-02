using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Domain.Constants;
using MESManager.Infrastructure.Data;
using Sharp7;
using System.Text;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio PLC con mapping reale:
/// - DB55 offset 0-99: sola lettura stati
/// - DB55 offset 100+: scrittura ricette
/// - DB56: lettura tempi/valori di esecuzione
/// Responsabilità: connessione PLC, validazione, scrittura parametri, lettura DB per viewer
/// </summary>
public class PlcRecipeWriterService : IPlcRecipeWriterService
{
    private readonly ILogger<PlcRecipeWriterService> _logger;
    private readonly MesManagerDbContext _context;
    private readonly IHostEnvironment _environment;
    
    // Configurazione DB offsets - CENTRALIZZATI in PlcConstants.cs
    // Modificare SOLO in PlcConstants.cs!
    private const int DB56_NUMBER = PlcConstants.EXECUTION_DATABASE;
    private const int DB55_NUMBER = PlcConstants.PRODUCTION_DATABASE;
    private const int DB_SIZE = PlcConstants.DATABASE_BUFFER_SIZE;
    private const int RECIPE_START_OFFSET = PlcConstants.OFFSET_RECIPE_PARAMETERS_START;
    
    public PlcRecipeWriterService(
        ILogger<PlcRecipeWriterService> logger,
        MesManagerDbContext context,
        IHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
    }
    
    public async Task<RecipeWriteResult> WriteRecipeToDb56Async(
        Guid macchinaId, 
        RicettaArticoloDto ricetta, 
        CancellationToken ct = default)
    {
        var result = new RecipeWriteResult
        {
            MacchinaId = macchinaId,
            CodiceArticolo = ricetta.CodiceArticolo,
            WriteTimestamp = DateTime.UtcNow
        };
        
        try
        {
            _logger.LogInformation("📡 [RECIPE-WRITE] Avvio scrittura ricetta su DB55(offset {Offset}+): Articolo {Codice} → Macchina {Id}",
                RECIPE_START_OFFSET,
                ricetta.CodiceArticolo, macchinaId);
            
            // 1. Carica configurazione macchina
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null)
            {
                result.ErrorMessage = "Macchina non trovata";
                return result;
            }
            
            if (string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                result.ErrorMessage = "IP PLC non configurato";
                return result;
            }
            
            // 2. Connessione PLC
            var plc= new S7Client();
            int connResult = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
            
            if (connResult != 0)
            {
                result.ErrorMessage = $"Connessione PLC fallita: {plc.ErrorText(connResult)}";
                _logger.LogWarning("⚠️ [RECIPE-WRITE] {Error}", result.ErrorMessage);
                return result;
            }
            
            _logger.LogInformation("✅ [RECIPE-WRITE] Connesso a PLC {Ip}", macchina.IndirizzoPLC);
            
            // 3. Legge DB55 corrente per aggiornare SOLO area scrivibile (offset 100+)
            byte[] buffer = new byte[DB_SIZE];
            int readCurrent = plc.DBRead(DB55_NUMBER, 0, DB_SIZE, buffer);
            if (readCurrent != 0)
            {
                result.ErrorMessage = $"Lettura DB55 fallita prima della scrittura ricetta: {plc.ErrorText(readCurrent)}";
                _logger.LogError("❌ [RECIPE-WRITE] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            // 4. Scrive parametri ricetta SOLO su area scrivibile DB55 (offset >=100)
            int parametriScritti = 0;
            
            foreach (var param in ricetta.Parametri.OrderBy(p => p.CodiceParametro))
            {
                if (param.Indirizzo < RECIPE_START_OFFSET || param.Indirizzo + 1 >= DB_SIZE)
                {
                    continue;
                }

                S7.SetIntAt(buffer, param.Indirizzo, (short)param.Valore);
                parametriScritti++;
            }

            if (parametriScritti == 0)
            {
                result.ErrorMessage = "Nessun parametro valido da scrivere in area DB55 offset 100+";
                plc.Disconnect();
                return result;
            }
            
            // 5. Scrive SOLO la sezione ricetta (100+) su DB55, preservando i campi read-only 0-99
            var writableLength = DB_SIZE - RECIPE_START_OFFSET;
            byte[] writableBuffer = new byte[writableLength];
            Array.Copy(buffer, RECIPE_START_OFFSET, writableBuffer, 0, writableLength);

            int writeResult = plc.DBWrite(DB55_NUMBER, RECIPE_START_OFFSET, writableLength, writableBuffer);
            
            if (writeResult != 0)
            {
                result.ErrorMessage = $"Scrittura ricetta su DB55 fallita: {plc.ErrorText(writeResult)}";
                _logger.LogError("❌ [RECIPE-WRITE] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }

            // 6. Scrive campi speciali (es. SaleOrdId a offset 46 BarcodeLavorazione) con write individuali.
            //    Questi offset stanno nella zona read-only della ricetta ma vengono scritti esplicitamente
            //    quando iniettati in ricetta.Parametri dal chiamante (es. PlcController/RecipeAutoLoaderService).
            var specialParams = ricetta.Parametri
                .Where(p => p.Indirizzo < RECIPE_START_OFFSET && p.Indirizzo + 1 < DB_SIZE)
                .ToList();
            foreach (var sp in specialParams)
            {
                byte[] spBuffer = new byte[2];
                S7.SetIntAt(spBuffer, 0, (short)sp.Valore);
                int spResult = plc.DBWrite(DB55_NUMBER, sp.Indirizzo, 2, spBuffer);
                if (spResult == 0)
                {
                    parametriScritti++;
                    _logger.LogInformation("🔑 [RECIPE-WRITE] Campo speciale scritto: offset={Offset} valore={Valore}", sp.Indirizzo, sp.Valore);
                }
                else
                {
                    _logger.LogWarning("⚠️ [RECIPE-WRITE] Campo speciale offset={Offset} fallito: {Err}", sp.Indirizzo, plc.ErrorText(spResult));
                }
            }
            
            // 10. Successo
            plc.Disconnect();
            
            result.Success = true;
            result.Message = $"Ricetta {ricetta.CodiceArticolo} scritta su DB55 (offset {RECIPE_START_OFFSET}+).";
            result.ParametersWritten = parametriScritti;
            
            _logger.LogInformation("✅ [RECIPE-WRITE] Completato: {Parametri} parametri scritti in DB55(offset {Offset}+)", 
                parametriScritti,
                RECIPE_START_OFFSET);
            
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [RECIPE-WRITE] Eccezione durante scrittura ricetta su DB55");
            return result;
        }
    }
    
    public async Task<RecipeWriteResult> CopyDb55ToDb56Async(Guid macchinaId, CancellationToken ct = default)
    {
        var result = new RecipeWriteResult
        {
            MacchinaId = macchinaId,
            WriteTimestamp = DateTime.UtcNow
        };
        
        try
        {
            _logger.LogInformation("📋 [COPY-DB] Sincronizzazione DB56(esecuzione) → DB55(offset {Offset}+) per macchina {Id}", RECIPE_START_OFFSET, macchinaId);
            
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                result.ErrorMessage = "Macchina o IP PLC non configurato";
                return result;
            }
            
            var plc = new S7Client();
            int connResult = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
            
            if (connResult != 0)
            {
                result.ErrorMessage = $"Connessione PLC fallita: {plc.ErrorText(connResult)}";
                return result;
            }
            
            var recipeSize = DB_SIZE - RECIPE_START_OFFSET;
            
            // STEP 1: Verifica accessibilità DB56 (fonte tempi di esecuzione)
            byte[] testBuffer = new byte[2];
            int testRead = plc.DBRead(DB56_NUMBER, 0, 2, testBuffer);
            
            if (testRead != 0)
            {
                result.ErrorMessage = "Il blocco DB56 non esiste su questo PLC. Verificare che il programma PLC includa il DB56 (esecuzione).";
                _logger.LogWarning("⚠️ [COPY-DB] DB56 non presente sul PLC ({Ip}): {Error}", macchina.IndirizzoPLC, plc.ErrorText(testRead));
                plc.Disconnect();
                return result;
            }
            
            // STEP 2: Leggi tempi/valori reali da DB56
            byte[] bufferRecipe = new byte[recipeSize];
            int readResult = plc.DBRead(DB56_NUMBER, RECIPE_START_OFFSET, recipeSize, bufferRecipe);
            
            if (readResult != 0)
            {
                result.ErrorMessage = $"Lettura DB56 fallita: {plc.ErrorText(readResult)}";
                _logger.LogWarning("⚠️ [COPY-DB] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            // STEP 3: Scrivi valori esecuzione in area ricetta DB55 (offset 100+)
            int writeResult = plc.DBWrite(DB55_NUMBER, RECIPE_START_OFFSET, recipeSize, bufferRecipe);
            
            if (writeResult != 0)
            {
                result.ErrorMessage = $"Scrittura parametri su DB55 fallita (offset {RECIPE_START_OFFSET}+): {plc.ErrorText(writeResult)}";
                _logger.LogWarning("⚠️ [COPY-DB] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            plc.Disconnect();
            
            result.Success = true;
            result.Message = $"Parametri sincronizzati DB56→DB55 (offset {RECIPE_START_OFFSET}+).";
            
            _logger.LogInformation("✅ [COPY-DB] Copiati {Size} byte di parametri ricetta", recipeSize);
            
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [COPY-DB] Eccezione");
            return result;
        }
    }
    
    public async Task<List<PlcDbEntryDto>> ReadDb56Async(Guid macchinaId, CancellationToken ct = default)
    {
        return await ReadDbAsync(macchinaId, DB56_NUMBER, ct);
    }
    
    public async Task<List<PlcDbEntryDto>> ReadDb55Async(Guid macchinaId, CancellationToken ct = default)
    {
        return await ReadDbAsync(macchinaId, DB55_NUMBER, ct);
    }
    
    private async Task<List<PlcDbEntryDto>> ReadDbAsync(Guid macchinaId, int dbNumber, CancellationToken ct)
    {
        var entries = new List<PlcDbEntryDto>();
        
        try
        {
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                entries.Add(new PlcDbEntryDto
                {
                    Offset = -1,
                    Nome = $"Errore lettura DB{dbNumber}",
                    Valore = "Macchina non trovata o IP PLC non configurato",
                    Tipo = "ERROR",
                    IsReadOnly = true
                });
                return entries;
            }
            
            var plc = new S7Client();
            int connResult = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
            
            if (connResult != 0)
            {
                var errorText = plc.ErrorText(connResult);
                _logger.LogWarning("Connessione PLC fallita per lettura DB{Num}: {Error}", dbNumber, errorText);
                entries.Add(new PlcDbEntryDto
                {
                    Offset = -1,
                    Nome = $"Errore lettura DB{dbNumber}",
                    Valore = errorText,
                    Tipo = "ERROR",
                    IsReadOnly = true
                });
                return entries;
            }
            
            byte[] buffer = new byte[DB_SIZE];
            int readResult = plc.DBRead(dbNumber, 0, DB_SIZE, buffer);
            
            if (readResult != 0 && dbNumber == DB56_NUMBER)
            {
                // Fallback progressivo per DB56 (ambiente sviluppo o DB piccoli)
                int[] fallbackSizes = { 140, 100, 50, 34 };
                bool fallbackSuccess = false;
                
                foreach (var size in fallbackSizes)
                {
                    _logger.LogInformation("Tentativo lettura DB56 con {Size} byte...", size);
                    buffer = new byte[size];
                    readResult = plc.DBRead(dbNumber, 0, size, buffer);
                    
                    if (readResult == 0)
                    {
                        _logger.LogInformation("✅ DB56 letto con fallback {Size} byte", size);
                        fallbackSuccess = true;
                        break;
                    }
                }
                
                if (!fallbackSuccess)
                {
                    _logger.LogWarning("⚠️ DB56 non presente sul PLC - il programma PLC non include il blocco DB56");
                    entries.Add(new PlcDbEntryDto
                    {
                        Offset = -1,
                        Nome = "DB56 non presente",
                        Valore = "Il blocco DB56 non esiste su questo PLC. Verificare che il programma PLC includa il DB56 (esecuzione).",
                        Tipo = "INFO",
                        IsReadOnly = true
                    });
                    plc.Disconnect();
                    return entries;
                }
            }
            
            if (readResult != 0)
            {
                var errorText = plc.ErrorText(readResult);
                _logger.LogWarning("Lettura DB{Num} fallita: errore {Code} - {Text}", dbNumber, readResult, errorText);
                entries.Add(new PlcDbEntryDto
                {
                    Offset = -1,
                    Nome = $"Errore lettura DB{dbNumber}",
                    Valore = errorText,
                    Tipo = "ERROR",
                    IsReadOnly = true
                });
                plc.Disconnect();
                return entries;
            }
            
            // Parse buffer con offsets PlcSync (formato dati produzione + ricette)
            // Usa TUTTI gli offset da PlcOffsetsConfig.cs
            // REGOLA: Offset 0-100 = SOLO LETTURA, Offset 102+ = SCRIVIBILE (ricette)
            
            // === CAMPI PRODUZIONE (uso corrente) - SOLO LETTURA ===
            entries.Add(new PlcDbEntryDto { Offset = 0, Nome = "NumeroMacchina", Valore = S7.GetIntAt(buffer, 0).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 2, Nome = "ComunicazioneAbilitata", Valore = S7.GetIntAt(buffer, 2).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 4, Nome = "ProntoRicevereNuoviDati", Valore = S7.GetIntAt(buffer, 4).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 6, Nome = "DatiRicevuti", Valore = S7.GetIntAt(buffer, 6).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 8, Nome = "InizioSetup", Valore = S7.GetIntAt(buffer, 8).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 10, Nome = "FineSetup", Valore = S7.GetIntAt(buffer, 10).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 12, Nome = "NuovaProduzione", Valore = S7.GetIntAt(buffer, 12).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 14, Nome = "FineProduzione", Valore = S7.GetIntAt(buffer, 14).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 16, Nome = "QuantitaRaggiunta", Valore = S7.GetIntAt(buffer, 16).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 18, Nome = "CicliFatti", Valore = S7.GetIntAt(buffer, 18).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 20, Nome = "CicliScarti", Valore = S7.GetIntAt(buffer, 20).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 22, Nome = "NumeroOperatore", Valore = S7.GetIntAt(buffer, 22).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 24, Nome = "TempoMedioRil", Valore = S7.GetIntAt(buffer, 24).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 26, Nome = "ProduzioneInRitardo", Valore = S7.GetIntAt(buffer, 26).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 28, Nome = "ProduzioneInAnticipo", Valore = S7.GetIntAt(buffer, 28).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 30, Nome = "ProduzioneInLineaConTempi", Valore = S7.GetIntAt(buffer, 30).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 32, Nome = "RegistroWatchDog", Valore = S7.GetIntAt(buffer, 32).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 34, Nome = "StatoEmergenza", Valore = S7.GetIntAt(buffer, 34).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 36, Nome = "StatoManuale", Valore = S7.GetIntAt(buffer, 36).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 38, Nome = "StatoAutomatico", Valore = S7.GetIntAt(buffer, 38).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 40, Nome = "StatoCiclo", Valore = S7.GetIntAt(buffer, 40).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 42, Nome = "StatoPezziRagg", Valore = S7.GetIntAt(buffer, 42).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 44, Nome = "StatoAllarme", Valore = S7.GetIntAt(buffer, 44).ToString(), Tipo = "INT", IsReadOnly = true });
            entries.Add(new PlcDbEntryDto { Offset = 46, Nome = "BarcodeLavorazione", Valore = S7.GetIntAt(buffer, 46).ToString(), Tipo = "INT", IsReadOnly = true });
            
            // === CAMPI RICETTE (uso futuro) - SCRIVIBILI ===
            entries.Add(new PlcDbEntryDto { Offset = 98, Nome = "StatoProduzione", Valore = S7.GetIntAt(buffer, 98).ToString(), Tipo = "INT", IsReadOnly = true }); // Ancora readonly
            entries.Add(new PlcDbEntryDto { Offset = 100, Nome = "NumeroRicetta", Valore = S7.GetIntAt(buffer, 100).ToString(), Tipo = "INT", IsReadOnly = true }); // Ancora readonly
            
            // DA QUI INIZIANO I PARAMETRI SCRIVIBILI (offset 102+)
            entries.Add(new PlcDbEntryDto { Offset = 102, Nome = "AbilitazionePrimaPulitura", Valore = S7.GetIntAt(buffer, 102).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 104, Nome = "AbilitazioneSecondaPulitura", Valore = S7.GetIntAt(buffer, 104).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 106, Nome = "TempoPulitoreAvanti", Valore = S7.GetIntAt(buffer, 106).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 108, Nome = "TempoRitardoSecondaPulitura", Valore = S7.GetIntAt(buffer, 108).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 110, Nome = "TempoSecondaPulitura", Valore = S7.GetIntAt(buffer, 110).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 112, Nome = "AbilitazioneNastroSalitaDiscesa", Valore = S7.GetIntAt(buffer, 112).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 114, Nome = "AbilitazioneNastroIndietro", Valore = S7.GetIntAt(buffer, 114).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 116, Nome = "TempoNastroAvanti", Valore = S7.GetIntAt(buffer, 116).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 118, Nome = "TempoRitardoNastroIndietro", Valore = S7.GetIntAt(buffer, 118).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 120, Nome = "TempoNastroIndietro", Valore = S7.GetIntAt(buffer, 120).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 122, Nome = "TempoRitardoSparo", Valore = S7.GetIntAt(buffer, 122).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 124, Nome = "TempoSparo", Valore = S7.GetIntAt(buffer, 124).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 126, Nome = "TempoInvestimento", Valore = S7.GetIntAt(buffer, 126).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 128, Nome = "TempoCottura", Valore = S7.GetIntAt(buffer, 128).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 130, Nome = "FrequenzaCariche", Valore = S7.GetIntAt(buffer, 130).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 132, Nome = "TempoMandata", Valore = S7.GetIntAt(buffer, 132).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 134, Nome = "TempoScaricoMandata", Valore = S7.GetIntAt(buffer, 134).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 136, Nome = "TempoSerbatoioChiudi", Valore = S7.GetIntAt(buffer, 136).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 138, Nome = "TempoRitardoDiscesaSerbatoio", Valore = S7.GetIntAt(buffer, 138).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 140, Nome = "RitardoEstrattoreLatoMobile", Valore = S7.GetIntAt(buffer, 140).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 142, Nome = "TempoEstrattoreLatoMobile", Valore = S7.GetIntAt(buffer, 142).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 144, Nome = "RitardoEstrattoreLatoFisso", Valore = S7.GetIntAt(buffer, 144).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 146, Nome = "TempoEstrattoreLatoFisso", Valore = S7.GetIntAt(buffer, 146).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 148, Nome = "TempoChiusuraPannello", Valore = S7.GetIntAt(buffer, 148).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 150, Nome = "AbilitazioneMaschio", Valore = S7.GetIntAt(buffer, 150).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 152, Nome = "TempoMaschio", Valore = S7.GetIntAt(buffer, 152).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 154, Nome = "RitardoChiusuraMaschio", Valore = S7.GetIntAt(buffer, 154).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 156, Nome = "RitardoAperturaMaschio", Valore = S7.GetIntAt(buffer, 156).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 158, Nome = "RitardoRestartCiclo", Valore = S7.GetIntAt(buffer, 158).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 160, Nome = "CodicePDF", Valore = S7.GetIntAt(buffer, 160).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 162, Nome = "QuantitaDaProd", Valore = S7.GetIntAt(buffer, 162).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 164, Nome = "TempoMedio", Valore = S7.GetIntAt(buffer, 164).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 166, Nome = "TempoRallentamentoChiusuraPannello", Valore = S7.GetIntAt(buffer, 166).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 168, Nome = "AbilitazioneSparoLaterale", Valore = S7.GetIntAt(buffer, 168).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 170, Nome = "Figure", Valore = S7.GetIntAt(buffer, 170).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 172, Nome = "RitardoCaricoSabbia", Valore = S7.GetIntAt(buffer, 172).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 174, Nome = "NastroAlto", Valore = S7.GetIntAt(buffer, 174).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 176, Nome = "NastroBasso", Valore = S7.GetIntAt(buffer, 176).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 178, Nome = "QuotaPannelloChiuso", Valore = S7.GetIntAt(buffer, 178).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 180, Nome = "QuotaRallentamentoChiusura", Valore = S7.GetIntAt(buffer, 180).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 182, Nome = "QuotaDisaccoppiamento", Valore = S7.GetIntAt(buffer, 182).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 184, Nome = "QuotaRallentamentoApertura", Valore = S7.GetIntAt(buffer, 184).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 186, Nome = "QuotaPannelloAperto", Valore = S7.GetIntAt(buffer, 186).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 188, Nome = "PressioneSparo", Valore = S7.GetIntAt(buffer, 188).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 190, Nome = "TempoCaricoSabbiaSuperiore", Valore = S7.GetIntAt(buffer, 190).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 192, Nome = "AbilitaPulitoreSuperiore", Valore = S7.GetIntAt(buffer, 192).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 194, Nome = "AbilitaSparoSuperiore", Valore = S7.GetIntAt(buffer, 194).ToString(), Tipo = "INT", IsReadOnly = false });
            entries.Add(new PlcDbEntryDto { Offset = 196, Nome = "TempoDiscesaTesta", Valore = S7.GetIntAt(buffer, 196).ToString(), Tipo = "INT", IsReadOnly = false });
            
            plc.Disconnect();
            
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore lettura DB{Num}", dbNumber);
            return entries;
        }
    }
    
    public async Task<bool> CheckPlcConnectionAsync(Guid macchinaId, CancellationToken ct = default)
    {
        try
        {
            var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
            if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
            {
                return false;
            }
            
            var plc = new S7Client();
            int result = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
            
            if (result == 0)
            {
                plc.Disconnect();
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    // Helper methods
    private void WriteStringAt(byte[] buffer, int offset, string value, int maxLength)
    {
        byte[] stringBytes = Encoding.ASCII.GetBytes(value.PadRight(maxLength).Substring(0, maxLength));
        Array.Copy(stringBytes, 0, buffer, offset, maxLength);
    }
    
    private string ReadStringAt(byte[] buffer, int offset, int length)
    {
        byte[] stringBytes = new byte[length];
        Array.Copy(buffer, offset, stringBytes, 0, length);
        return Encoding.ASCII.GetString(stringBytes).TrimEnd('\0', ' ');
    }
    
    /// <summary>
    /// Scansiona i DB disponibili su un PLC provando a leggere 2 byte da ciascuno
    /// </summary>
    public async Task<List<PlcDbScanResultDto>> ScanAvailableDbsAsync(Guid macchinaId, int maxDb = 100, CancellationToken ct = default)
    {
        var results = new List<PlcDbScanResultDto>();
        
        var macchina = await _context.Macchine.FindAsync(new object[] { macchinaId }, ct);
        if (macchina == null || string.IsNullOrEmpty(macchina.IndirizzoPLC))
        {
            _logger.LogWarning("Scan DB: macchina {Id} non trovata o IP non configurato", macchinaId);
            return results;
        }
        
        var plc = new S7Client();
        int connResult = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
        
        if (connResult != 0)
        {
            _logger.LogWarning("Scan DB: connessione PLC {Ip} fallita: {Error}", macchina.IndirizzoPLC, plc.ErrorText(connResult));
            return results;
        }
        
        _logger.LogInformation("🔍 Scansione DB1-DB{Max} su PLC {Ip} ({Codice})...", maxDb, macchina.IndirizzoPLC, macchina.Codice);
        
        byte[] testBuffer = new byte[2];
        
        for (int db = 1; db <= maxDb; db++)
        {
            ct.ThrowIfCancellationRequested();
            
            int readResult = plc.DBRead(db, 0, 2, testBuffer);
            
            if (readResult == 0)
            {
                // DB esiste - determina dimensione con binary search
                int size = DetectDbSize(plc, db);
                
                results.Add(new PlcDbScanResultDto
                {
                    DbNumber = db,
                    Available = true,
                    SizeBytes = size
                });
                
                _logger.LogInformation("  ✅ DB{Num} disponibile ({Size} byte)", db, size);
            }
        }
        
        plc.Disconnect();
        
        _logger.LogInformation("🔍 Scansione completata: {Count} DB trovati", results.Count);
        
        return results;
    }
    
    /// <summary>
    /// Determina dimensione DB con binary search (evita "Address out of range")
    /// </summary>
    private int DetectDbSize(S7Client plc, int dbNumber)
    {
        int low = 2;
        int high = 65536; // max teorico S7
        int lastGood = low;
        byte[] buf = new byte[2];
        
        while (low <= high)
        {
            int mid = (low + high) / 2;
            int result = plc.DBRead(dbNumber, mid - 2, 2, buf);
            
            if (result == 0)
            {
                lastGood = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        
        return lastGood;
    }
}
