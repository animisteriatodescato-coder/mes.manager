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
    
    // Configurazione DB offsets (TODO: caricare da config/JSON)
    private const int DB52_NUMBER = 52;
    private const int DB55_NUMBER = 55;
    private const int DB_SIZE = 512; // byte
    
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
            _logger.LogInformation("📋 [COPY-DB] Copia DB55 → DB52 per macchina {Id}", macchinaId);
            
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
            
            // Leggi DB55
            byte[] bufferDb55 = new byte[DB_SIZE];
            int readResult = plc.DBRead(DB55_NUMBER, 0, DB_SIZE, bufferDb55);
            
            if (readResult != 0)
            {
                result.ErrorMessage = $"Lettura DB55 fallita: {plc.ErrorText(readResult)}";
                plc.Disconnect();
                return result;
            }
            
            // Scrivi su DB52 (copia diretta)
            int writeResult = plc.DBWrite(DB52_NUMBER, 0, DB_SIZE, bufferDb55);
            
            if (writeResult != 0)
            {
                result.ErrorMessage = $"Scrittura DB52 fallita: {plc.ErrorText(writeResult)}";
                plc.Disconnect();
                return result;
            }
            
            plc.Disconnect();
            
            result.Success = true;
            result.Message = "DB55 copiato su DB52 con successo";
            
            _logger.LogInformation("✅ [COPY-DB] Completato");
            
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
                plc.Disconnect();
                return entries;
            }
            
            // Parse buffer e crea entries (esempio semplificato)
            entries.Add(new PlcDbEntryDto 
            { 
                Offset = 0, 
                Nome = "Codice Articolo", 
                Valore = ReadStringAt(buffer, 0, 20),
                Tipo = "STRING[20]"
            });
            
            entries.Add(new PlcDbEntryDto 
            { 
                Offset = 20, 
                Nome = "Numero Parametri", 
                Valore = S7.GetIntAt(buffer, 20).ToString(),
                Tipo = "INT"
            });
            
            // Parametri dinamici
            int numParams = S7.GetIntAt(buffer, 20);
            int offsetParam = 22;
            
            for (int i = 0; i < numParams && i < 50; i++)
            {
                if (offsetParam >= DB_SIZE - 4) break;
                
                var indirizzo = S7.GetIntAt(buffer, offsetParam);
                var valore = S7.GetIntAt(buffer, offsetParam + 2);
                
                entries.Add(new PlcDbEntryDto
                {
                    Offset = offsetParam,
                    Nome = $"Param_{i+1}_Indirizzo",
                    Valore = indirizzo.ToString(),
                    Tipo = "INT"
                });
                
                entries.Add(new PlcDbEntryDto
                {
                    Offset = offsetParam + 2,
                    Nome = $"Param_{i+1}_Valore",
                    Valore = valore.ToString(),
                    Tipo = "INT"
                });
                
                offsetParam += 4;
            }
            
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
