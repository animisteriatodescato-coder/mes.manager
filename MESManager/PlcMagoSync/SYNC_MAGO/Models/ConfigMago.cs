using System;

namespace PlcMagoSync.SYNC_MAGO.Config
{
    public class ConfigMago
    {
        public string MagoConnectionString { get; set; } = "";
        public string ServiceAccountJsonPath { get; set; } = "";
        public string GoogleSheetId { get; set; } = "";
    }
}
