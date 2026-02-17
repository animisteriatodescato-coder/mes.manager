using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Models;
using MESManager.Domain.Constants;
using Sharp7;

namespace MESManager.PlcSync.Services;

public class PlcReaderService
{
    private readonly ILogger<PlcReaderService> _logger;
    private readonly PlcConnectionService _connectionService;

    public PlcReaderService(
        ILogger<PlcReaderService> logger,
        PlcConnectionService connectionService)
    {
        _logger = logger;
        _connectionService = connectionService;
    }

    public Task<PlcSnapshot?> ReadSnapshotAsync(PlcMachineConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = _connectionService.GetMachineState(config.MacchinaId);
            if (state == null || !state.Client.Connected)
            {
                _logger.LogWarning("PLC {MacchinaNumero} non connesso", config.Numero);
                return Task.FromResult<PlcSnapshot?>(null);
            }

            // DB55 = produzione/stati + area scrivibile ricetta
            var productionBuffer = new byte[config.PlcSettings.DbLength];
            var result = state.Client.DBRead(
                PlcConstants.PRODUCTION_DATABASE,
                config.PlcSettings.DbStart,
                config.PlcSettings.DbLength,
                productionBuffer
            );

            if (result != 0)
            {
                _logger.LogError("Errore lettura DB55 PLC {MacchinaNumero}: Codice {ErrorCode}", 
                    config.Numero, result);
                return Task.FromResult<PlcSnapshot?>(null);
            }

            // DB56 = tempi/valori reali di esecuzione macchina (soluzione 1)
            var executionBuffer = new byte[config.PlcSettings.DbLength];
            var executionResult = state.Client.DBRead(
                PlcConstants.EXECUTION_DATABASE,
                config.PlcSettings.DbStart,
                config.PlcSettings.DbLength,
                executionBuffer
            );

            var hasExecutionDb = executionResult == 0;
            if (!hasExecutionDb)
            {
                _logger.LogWarning("DB56 non disponibile su PLC {MacchinaNumero} (errore {Code}) - campi offset >=100 impostati a 0", config.Numero, executionResult);
            }

            // Lettura dati produzione
            var snapshot = new PlcSnapshot
            {
                MacchinaId = config.MacchinaId,
                Timestamp = DateTime.Now,
                
                // Lettura dati produzione
                CicliFatti = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.CicliFatti, hasExecutionDb),
                QuantitaDaProdurre = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.QuantitaDaProdurre, hasExecutionDb),
                CicliScarti = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.CicliScarti, hasExecutionDb),
                BarcodeLavorazione = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.BarcodeLavorazione, hasExecutionDb),
                
                // Operatore
                NumeroOperatore = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.NumeroOperatore, hasExecutionDb),
                
                // Tempi
                TempoMedioRilevato = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.TempoMedioRilevato, hasExecutionDb),
                TempoMedio = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.TempoMedio, hasExecutionDb),
                Figure = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.Figure, hasExecutionDb),
                
                // Stati
                StatoMacchina = CalcolaStato(productionBuffer),
                QuantitaRaggiunta = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.QuantitaRaggiunta, hasExecutionDb) != 0
            };

            // Lettura eventi (flag bool)
            bool nuovaProd = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.NuovaProduzione, hasExecutionDb) != 0;
            bool inizioSetup = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.InizioSetup, hasExecutionDb) != 0;
            bool fineSetup = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.FineSetup, hasExecutionDb) != 0;
            bool inProd = ReadMappedInt(productionBuffer, executionBuffer, PlcConstants.Offsets.Fields.FineProduzione, hasExecutionDb) != 0;

            snapshot.NuovaProduzione = nuovaProd;
            snapshot.InizioSetup = inizioSetup;
            snapshot.FineSetup = fineSetup;
            snapshot.InProduzione = inProd;

            // Gestione timestamp eventi 0→1
            string ts = snapshot.Timestamp.ToString("dd.MM.yy HH:mm:ss");
            
            if (!state.PrevNuovaProduzione && nuovaProd)
                snapshot.NuovaProduzioneTs = ts;
            if (!state.PrevInizioSetup && inizioSetup)
                snapshot.InizioSetupTs = ts;
            if (!state.PrevFineSetup && fineSetup)
                snapshot.FineSetupTs = ts;
            if (!state.PrevInProduzione && inProd)
                snapshot.InProduzioneTs = ts;

            // Aggiorna stati precedenti
            state.PrevNuovaProduzione = nuovaProd;
            state.PrevInizioSetup = inizioSetup;
            state.PrevFineSetup = fineSetup;
            state.PrevInProduzione = inProd;

            return Task.FromResult<PlcSnapshot?>(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lettura snapshot PLC {MacchinaNumero}", config.Numero);
            return Task.FromResult<PlcSnapshot?>(null);
        }
    }

    private int ReadInt(byte[] buffer, int offset)
    {
        return S7.GetIntAt(buffer, offset);
    }

    private int ReadMappedInt(byte[] productionBuffer, byte[] executionBuffer, int offset, bool hasExecutionDb)
    {
        if (offset <= PlcConstants.OFFSET_READONLY_END)
        {
            return ReadInt(productionBuffer, offset);
        }

        if (!hasExecutionDb)
        {
            return 0;
        }

        return ReadInt(executionBuffer, offset);
    }

    private string CalcolaStato(byte[] buffer)
    {
        bool emergenza = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoEmergenza) != 0;
        bool allarme = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoAllarme) != 0;
        bool manuale = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoManuale) != 0;
        bool automatico = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoAutomatico) != 0;
        bool ciclo = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoCiclo) != 0;
        bool pezzi = ReadInt(buffer, PlcConstants.Offsets.Fields.StatoPezziRaggiunti) != 0;

        if (emergenza) return "EMERGENZA";
        if (allarme) return "ALLARME";
        if (manuale) return "MANUALE";
        if (automatico && ciclo) return "AUTOMATICO - CICLO";
        if (automatico) return "AUTOMATICO";
        if (ciclo) return "CICLO IN CORSO";
        if (pezzi) return "NUMERO PEZZI RAGGIUNTI";

        return "Sconosciuto";
    }
}
