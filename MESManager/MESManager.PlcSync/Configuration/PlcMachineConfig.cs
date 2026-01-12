namespace MESManager.PlcSync.Configuration;

public class PlcMachineConfig
{
    public Guid MacchinaId { get; set; }
    public int Numero { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string PlcIp { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    
    public PlcSettings PlcSettings { get; set; } = new();
    public PlcOffsetsConfig Offsets { get; set; } = new();
}

public class PlcSettings
{
    public int Rack { get; set; } = 0;
    public int Slot { get; set; } = 1;
    public int DbNumber { get; set; } = 55;
    public int DbStart { get; set; } = 0;
    public int DbLength { get; set; } = 200;
}
