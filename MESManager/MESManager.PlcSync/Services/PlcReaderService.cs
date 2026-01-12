using MESManager.PlcSync.Configuration;
using MESManager.PlcSync.Models;
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

            var buffer = new byte[config.PlcSettings.DbLength];
            var result = state.Client.DBRead(
                config.PlcSettings.DbNumber,
                config.PlcSettings.DbStart,
                config.PlcSettings.DbLength,
                buffer
            );

            if (result != 0)
            {
                _logger.LogError("Errore lettura DB PLC {MacchinaNumero}: Codice {ErrorCode}", 
                    config.Numero, result);
                return Task.FromResult<PlcSnapshot?>(null);
            }

            // Lettura dati produzione
            var snapshot = new PlcSnapshot
            {
                MacchinaId = config.MacchinaId,
                Timestamp = DateTime.Now,
                
                // Lettura dati produzione
                CicliFatti = ReadInt(buffer, config.Offsets.CicliFatti),
                QuantitaDaProdurre = ReadInt(buffer, config.Offsets.QuantitaDaProd),
                CicliScarti = ReadInt(buffer, config.Offsets.CicliScarti),
                BarcodeLavorazione = ReadInt(buffer, config.Offsets.BarcodeLavorazione),
                
                // Operatore
                NumeroOperatore = ReadInt(buffer, config.Offsets.NumeroOperatore),
                
                // Tempi
                TempoMedioRilevato = ReadInt(buffer, config.Offsets.TempoMedioRil),
                TempoMedio = ReadInt(buffer, config.Offsets.TempoMedio),
                Figure = ReadInt(buffer, config.Offsets.Figure),
                
                // Stati
                StatoMacchina = CalcolaStato(buffer, config.Offsets),
                QuantitaRaggiunta = ReadInt(buffer, config.Offsets.QuantitaRaggiunta) != 0
            };

            // Lettura eventi (flag bool)
            bool nuovaProd = ReadInt(buffer, config.Offsets.NuovaProduzione) != 0;
            bool inizioSetup = ReadInt(buffer, config.Offsets.InizioSetup) != 0;
            bool fineSetup = ReadInt(buffer, config.Offsets.FineSetup) != 0;
            bool inProd = ReadInt(buffer, config.Offsets.FineProduzione) != 0;

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

    private string CalcolaStato(byte[] buffer, PlcOffsetsConfig offsets)
    {
        bool emergenza = ReadInt(buffer, offsets.StatoEmergenza) != 0;
        bool allarme = ReadInt(buffer, offsets.StatoAllarme) != 0;
        bool manuale = ReadInt(buffer, offsets.StatoManuale) != 0;
        bool automatico = ReadInt(buffer, offsets.StatoAutomatico) != 0;
        bool ciclo = ReadInt(buffer, offsets.StatoCiclo) != 0;
        bool pezzi = ReadInt(buffer, offsets.StatoPezziRagg) != 0;

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
