using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;
using MESManager.Infrastructure.Data;
using Sharp7;
using System.Text;

namespace MESManager.Infrastructure.Services;

/// <summary>
/// Servizio per scrittura ricette su DB52 PLC tramite Sharp7
/// Responsabilità: connessione PLC, validazione, scrittura parametri, lettura DB per viewer
/// </summary>
public class PlcRecipeWriterService : IPlcRecipeWriterService
{
    private readonly ILogger<PlcRecipeWriterService> _logger;
    private readonly MesManagerDbContext _context;
    
    // Configurazione DB offsets (allineato con PlcSync)
    private const int DB52_NUMBER = 52;
    private const int DB55_NUMBER = 55;
    private const int DB_SIZE = 200; // byte - allineato con PlcSync DbLength
    
    public PlcRecipeWriterService(
        ILogger<PlcRecipeWriterService> logger,
        MesManagerDbContext context)
    {
        _logger = logger;
        _context = context;
    }
    
    public async Task<RecipeWriteResult> WriteRecipeToDb52Async(
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
            _logger.LogInformation("📡 [RECIPE-WRITE] Avvio scrittura DB52: Articolo {Codice} → Macchina {Id}", 
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
            
            // 3. Prepara buffer DB52
            byte[] buffer = new byte[DB_SIZE];
            
            // 4. Scrive codice articolo (offset 0, STRING[20])
            WriteStringAt(buffer, 0, ricetta.CodiceArticolo, 20);
            
            // 5. Scrive numero parametri (offset 20, INT)
            S7.SetIntAt(buffer, 20, (short)ricetta.TotaleParametri);
            
            // 6. Scrive parametri (dinamico based on ArticoliRicetta)
            int parametriScritti = 0;
            int offsetParametri = 22; // Inizio parametri
            
            foreach (var param in ricetta.Parametri.OrderBy(p => p.CodiceParametro))
            {
                // Ogni parametro: Indirizzo (INT) + Valore (INT) = 4 byte
                S7.SetIntAt(buffer, offsetParametri, (short)param.Indirizzo);
                S7.SetIntAt(buffer, offsetParametri + 2, (short)param.Valore);
                
                offsetParametri += 4;
                parametriScritti++;
                
                if (offsetParametri >= DB_SIZE - 10) break; // Safety check
            }
            
            // 7. Timestamp scrittura (offset 500, LONG - unix timestamp)
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            S7.SetLIntAt(buffer, 500, unixTimestamp);
            
            // 8. Status (offset 508, INT - 0=OK)
            S7.SetIntAt(buffer, 508, 0);
            
            // 9. Scrittura DB52
            int writeResult = plc.DBWrite(DB52_NUMBER, 0, DB_SIZE, buffer);
            
            if (writeResult != 0)
            {
                result.ErrorMessage = $"Scrittura DB52 fallita: {plc.ErrorText(writeResult)}";
                _logger.LogError("❌ [RECIPE-WRITE] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            // 10. Successo
            plc.Disconnect();
            
            result.Success = true;
            result.Message = $"Ricetta {ricetta.CodiceArticolo} scritta su DB52";
            result.ParametersWritten = parametriScritti;
            
            _logger.LogInformation("✅ [RECIPE-WRITE] Completato: {Parametri} parametri scritti in DB52", 
                parametriScritti);
            
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [RECIPE-WRITE] Eccezione durante scrittura DB52");
            return result;
        }
    }
    
    public async Task<RecipeWriteResult> CopyDb55ToDb52Async(Guid macchinaId, CancellationToken ct = default)
    {
        var result = new RecipeWriteResult
        {
            MacchinaId = macchinaId,
            WriteTimestamp = DateTime.UtcNow
        };
        
        try
        {
            _logger.LogInformation("📋 [COPY-DB] Copia parametri ricetta DB55 → DB52 per macchina {Id}", macchinaId);
            
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
            
            // IMPORTANTE: Copia SOLO parametri ricetta (offset 102-170) da DB55
            // Offset 0-100 sono SOLO LETTURA (stati/produzione)
            // RIDUCIAMO LA DIMENSIONE PER EVITARE "AUTO-FRAME" ERROR
            // DB52 potrebbe essere più piccolo di DB55 su alcune macchine
            const int RECIPE_START_OFFSET = 102;
            const int RECIPE_END_OFFSET = 172; // Fino a Figure (170) + 2 byte - sicuro per tutte le macchine
            const int RECIPE_SIZE = RECIPE_END_OFFSET - RECIPE_START_OFFSET;
            
            // Leggi solo parametri ricetta da DB55
            byte[] bufferRecipe = new byte[RECIPE_SIZE];
            int readResult = plc.DBRead(DB55_NUMBER, RECIPE_START_OFFSET, RECIPE_SIZE, bufferRecipe);
            
            if (readResult != 0)
            {
                result.ErrorMessage = $"Lettura parametri ricetta da DB55 fallita: {plc.ErrorText(readResult)}";
                _logger.LogWarning("⚠️ [COPY-DB] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            // Scrivi parametri ricetta su DB52 (stesso offset 102-198)
            int writeResult = plc.DBWrite(DB52_NUMBER, RECIPE_START_OFFSET, RECIPE_SIZE, bufferRecipe);
            
            if (writeResult != 0)
            {
                result.ErrorMessage = $"Scrittura parametri ricetta su DB52 fallita: {plc.ErrorText(writeResult)}";
                _logger.LogWarning("⚠️ [COPY-DB] {Error}", result.ErrorMessage);
                plc.Disconnect();
                return result;
            }
            
            plc.Disconnect();
            
            result.Success = true;
            result.Message = $"Parametri ricetta copiati (offset {RECIPE_START_OFFSET}-{RECIPE_END_OFFSET})";
            
            _logger.LogInformation("✅ [COPY-DB] Copiati {Size} byte di parametri ricetta", RECIPE_SIZE);
            
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "❌ [COPY-DB] Eccezione");
            return result;
        }
    }
    
    public async Task<List<PlcDbEntryDto>> ReadDb52Async(Guid macchinaId, CancellationToken ct = default)
    {
        return await ReadDbAsync(macchinaId, DB52_NUMBER, ct);
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
                return entries;
            }
            
            var plc = new S7Client();
            int connResult = plc.ConnectTo(macchina.IndirizzoPLC, 0, 1);
            
            if (connResult != 0)
            {
                _logger.LogWarning("Connessione PLC fallita per lettura DB{Num}", dbNumber);
                return entries;
            }
            
            byte[] buffer = new byte[DB_SIZE];
            int readResult = plc.DBRead(dbNumber, 0, DB_SIZE, buffer);
            
            if (readResult != 0)
            {
                _logger.LogWarning("Lettura DB{Num} fallita: errore {Code}", dbNumber, readResult);
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
}
