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
    public int Rack { get; set; } = 0;
    public int Slot { get; set; } = 1;
    public int DbNumber { get; set; } = 55;
    public int DbStart { get; set; } = 0;
    public int DbLength { get; set; } = 200;
    public int TimeoutMs { get; set; } = 5000;
    public int ReconnectDelayMs { get; set; } = 3000;
}
