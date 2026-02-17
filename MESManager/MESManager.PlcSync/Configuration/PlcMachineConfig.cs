using System.Text.Json.Serialization;
using MESManager.Domain.Constants;

namespace MESManager.PlcSync.Configuration;

public class PlcMachineConfig
{
    [JsonPropertyName("MachineId")]
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
    public int Rack { get; set; } = PlcConstants.PLC_RACK;
    public int Slot { get; set; } = PlcConstants.PLC_SLOT;
    public int DbNumber { get; set; } = PlcConstants.PRODUCTION_DATABASE;
    public int DbStart { get; set; } = 0;
    public int DbLength { get; set; } = PlcConstants.DATABASE_BUFFER_SIZE;
}
