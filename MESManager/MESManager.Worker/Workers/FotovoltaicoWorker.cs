using FluentModbus;
using MESManager.Domain.Entities;
using MESManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MESManager.Worker.Workers;

/// <summary>
/// Worker che legge i dati Modbus TCP dal'inverter Huawei SUN2000 ogni ~30s
/// e aggiorna FotovoltaicoRealtime. Ogni ora salva un record FotovoltaicoStorico.
/// </summary>
public class FotovoltaicoWorker : BackgroundService
{
    private readonly ILogger<FotovoltaicoWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly FotovoltaicoOptions _options;

    // Accumulo dati per storico orario
    private readonly List<double> _potenzeSample = new();
    private DateTime _ultimaScritturaStorico = DateTime.MinValue;
    private double _energiaAccumulataInizioPeriodo = 0;

    public FotovoltaicoWorker(
        ILogger<FotovoltaicoWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = configuration.GetSection("Fotovoltaico").Get<FotovoltaicoOptions>()
                   ?? new FotovoltaicoOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("[FOTOVOLTAICO] Worker disabilitato (Fotovoltaico.Enabled=false). Per abilitarlo impostare IP e Enabled=true in appsettings.json");
            return;
        }

        _logger.LogInformation("[FOTOVOLTAICO] Worker avviato - IP: {Ip}:{Port}, UnitId: {Unit}, Poll: {Poll}s",
            _options.ModbusIp, _options.ModbusPort, _options.UnitId, _options.PollIntervalSeconds);

        // Attesa iniziale per lasciar avviare il resto del sistema
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollInverterAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PollInverterAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MesManagerDbContext>();

        FotovoltaicoRealtime? snapshot = null;

        try
        {
            snapshot = await LeggiDatiModbusAsync(ct);
            snapshot.ConnessioneOk = true;
            snapshot.ErroreConnessione = null;
            _logger.LogDebug("[FOTOVOLTAICO] Lettura OK - Potenza: {P:F2} kW, Oggi: {E:F2} kWh",
                snapshot.PotenzaAttuale_kW, snapshot.EnergiaOggi_kWh);
        }
        catch (Exception ex)
        {
            snapshot ??= new FotovoltaicoRealtime();
            snapshot.ConnessioneOk = false;
            snapshot.ErroreConnessione = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;
            snapshot.UltimoAggiornamento = DateTime.Now;
            _logger.LogWarning("[FOTOVOLTAICO] Errore lettura Modbus: {Msg}", ex.Message);
        }

        await SalvaRealtimeAsync(db, snapshot, ct);

        if (snapshot.ConnessioneOk)
        {
            _potenzeSample.Add(snapshot.PotenzaAttuale_kW);
            await SalvaStoricoSeNecessarioAsync(db, snapshot, ct);
        }
    }

    private async Task<FotovoltaicoRealtime> LeggiDatiModbusAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(_options.ModbusIp), _options.ModbusPort);

        // FluentModbus usa Connect sincrono + lettura Span<byte> che va copiata subito
        // per evitare "ref in async" — tutto in Task.Run per non bloccare il thread async
        return await Task.Run(() =>
        {
            using var client = new ModbusTcpClient();
            client.Connect(endpoint);

            try
            {
                byte unitId = (byte)_options.UnitId;

                // Blocco 1: registri 32016..32090 → 75 registri = 150 byte
                byte[] block1 = client.ReadHoldingRegisters<byte>(unitId, 32016, 75).ToArray();

                // Blocco 2: registri 32106..32116 → 11 registri = 22 byte
                byte[] block2 = client.ReadHoldingRegisters<byte>(unitId, 32106, 11).ToArray();

                return new FotovoltaicoRealtime
                {
                    Id = 1,
                    UltimoAggiornamento = DateTime.Now,

                    TensioneStringa_V    = ReadUInt16(block1, 32016 - 32016) * 0.1,
                    CorrenteStringa_A    = ReadInt16(block1,  32017 - 32016) * 0.01,
                    TensioneRete_V       = ReadUInt16(block1, 32069 - 32016) * 0.1,
                    PotenzaAttuale_kW    = ReadInt32(block1,  32080 - 32016) / 1000.0,
                    TemperaturaInterna_C = ReadInt16(block1,  32087 - 32016) * 0.1,
                    StatoCodice          = ReadUInt16(block1, 32089 - 32016),
                    StatoInverter        = DecodeStatus(ReadUInt16(block1, 32089 - 32016)),

                    EnergiaAccumulata_kWh = ReadUInt32(block2, 32106 - 32106) * 0.01,
                    EnergiaOggi_kWh       = ReadUInt32(block2, 32114 - 32106) * 0.01,
                };
            }
            finally
            {
                client.Disconnect();
            }
        }, ct);
    }

    private async Task SalvaRealtimeAsync(MesManagerDbContext db, FotovoltaicoRealtime snap, CancellationToken ct)
    {
        var existing = await db.FotovoltaicoRealtime.FirstOrDefaultAsync(r => r.Id == 1, ct);
        if (existing == null)
        {
            snap.Id = 1;
            db.FotovoltaicoRealtime.Add(snap);
        }
        else
        {
            existing.UltimoAggiornamento    = snap.UltimoAggiornamento;
            existing.ConnessioneOk          = snap.ConnessioneOk;
            existing.ErroreConnessione      = snap.ErroreConnessione;
            existing.PotenzaAttuale_kW      = snap.PotenzaAttuale_kW;
            existing.EnergiaOggi_kWh        = snap.EnergiaOggi_kWh;
            existing.EnergiaAccumulata_kWh  = snap.EnergiaAccumulata_kWh;
            existing.TensioneStringa_V      = snap.TensioneStringa_V;
            existing.CorrenteStringa_A      = snap.CorrenteStringa_A;
            existing.TensioneRete_V         = snap.TensioneRete_V;
            existing.TemperaturaInterna_C   = snap.TemperaturaInterna_C;
            existing.StatoCodice            = snap.StatoCodice;
            existing.StatoInverter          = snap.StatoInverter;
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SalvaStoricoSeNecessarioAsync(MesManagerDbContext db, FotovoltaicoRealtime snap, CancellationToken ct)
    {
        var ora = DateTime.Now;
        var intervalloStorico = TimeSpan.FromMinutes(_options.StoricaIntervalMinutes);

        if (_ultimaScritturaStorico == DateTime.MinValue)
        {
            _ultimaScritturaStorico = ora;
            _energiaAccumulataInizioPeriodo = snap.EnergiaAccumulata_kWh;
            return;
        }

        if (ora - _ultimaScritturaStorico < intervalloStorico) return;

        var storico = new FotovoltaicoStorico
        {
            Timestamp             = new DateTime(ora.Year, ora.Month, ora.Day, ora.Hour, 0, 0),
            PotenzaMedia_kW       = _potenzeSample.Count > 0 ? _potenzeSample.Average() : 0,
            PotenzaMassima_kW     = _potenzeSample.Count > 0 ? _potenzeSample.Max()     : 0,
            EnergiaOra_kWh        = snap.EnergiaAccumulata_kWh - _energiaAccumulataInizioPeriodo,
            EnergiaAccumulata_kWh = snap.EnergiaAccumulata_kWh,
            StatoInverter         = snap.StatoInverter,
        };

        db.FotovoltaicoStorico.Add(storico);
        await db.SaveChangesAsync(ct);

        _potenzeSample.Clear();
        _ultimaScritturaStorico = ora;
        _energiaAccumulataInizioPeriodo = snap.EnergiaAccumulata_kWh;

        _logger.LogInformation("[FOTOVOLTAICO] Storico salvato: {T} | Media {Pm:F2} kW | Ora {Eh:F3} kWh",
            storico.Timestamp, storico.PotenzaMedia_kW, storico.EnergiaOra_kWh);
    }

    // ─── Helpers Modbus parsing (big-endian, 0-indexed offset in register count) ───

    private static int ReadInt32(byte[] data, int registerOffset)
    {
        int o = registerOffset * 2;
        return (data[o] << 24) | (data[o + 1] << 16) | (data[o + 2] << 8) | data[o + 3];
    }

    private static uint ReadUInt32(byte[] data, int registerOffset)
    {
        int o = registerOffset * 2;
        return (uint)((data[o] << 24) | (data[o + 1] << 16) | (data[o + 2] << 8) | data[o + 3]);
    }

    private static ushort ReadUInt16(byte[] data, int registerOffset)
    {
        int o = registerOffset * 2;
        return (ushort)((data[o] << 8) | data[o + 1]);
    }

    private static short ReadInt16(byte[] data, int registerOffset) =>
        (short)ReadUInt16(data, registerOffset);

    private static string DecodeStatus(int code) => code switch
    {
        0x0000 => "Standby: inizializzazione",
        0x0200 => "In rete",
        0x0201 => "In rete: potenza normale",
        0x0202 => "In rete: derating",
        0x0203 => "In rete: schedulazione",
        0x0204 => "In rete: limitato",
        0x0205 => "In rete: self-derating",
        0x0300 => "Spento: fermo normale",
        0x0301 => "Spento: fermo emergenza",
        0x0302 => "Spento: attesa collegamento",
        0x0303 => "Spento: attesa iniezione",
        0x0400 => "Standby: rilevamento irraggiamento",
        0x0401 => "Standby: rete non connessa",
        0x0500 => "Spento: fault",
        0x0600 => "Precauzione",
        0x0700 => "OK: derating avanzato",
        0x0800 => "Avvio",
        _      => $"Sconosciuto (0x{code:X4})"
    };
}

public class FotovoltaicoOptions
{
    public bool Enabled { get; set; } = false;
    public string ModbusIp { get; set; } = string.Empty;
    public int ModbusPort { get; set; } = 502;
    public int UnitId { get; set; } = 1;
    public int PollIntervalSeconds { get; set; } = 30;
    public int StoricaIntervalMinutes { get; set; } = 60;
}
