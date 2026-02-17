using MESManager.Domain.Constants;

namespace MESManager.PlcSync.Configuration;

public class PlcSyncSettings
{
    public int PollingIntervalSeconds { get; set; } = 4;
    public bool EnableRealtime { get; set; } = true;
    public bool EnableStorico { get; set; } = true;
    public bool EnableEvents { get; set; } = true;
    public string MachineConfigPath { get; set; } = "Configuration/machines";
    
    public PlcDefaultSettings PlcDefaults { get; set; } = new();
}

public class PlcDefaultSettings
{
    public int Rack { get; set; } = PlcConstants.PLC_RACK;
    public int Slot { get; set; } = PlcConstants.PLC_SLOT;
    public int DbNumber { get; set; } = PlcConstants.PRODUCTION_DATABASE;
    public int DbStart { get; set; } = 0;
    public int DbLength { get; set; } = PlcConstants.DATABASE_BUFFER_SIZE;
    public int TimeoutMs { get; set; } = PlcConstants.PLC_CONNECTION_TIMEOUT_SECONDS * 1000;
    public int ReconnectDelayMs { get; set; } = 3000;
}
