namespace MESManager.PlcSync.Models;

/// <summary>
/// Snapshot minimo dei 4 flag evento DB55 (offset 8-14).
/// Usato dal fast event polling loop (ogni 500ms) per rilevare
/// rising edge senza leggere l'intero buffer da 200 byte.
/// </summary>
public class PlcEventFlagsSnapshot
{
    public bool NuovaProduzione { get; set; }   // offset 12
    public bool InizioSetup     { get; set; }   // offset  8
    public bool FineSetup       { get; set; }   // offset 10
    public bool FineProduzione  { get; set; }   // offset 14
    public DateTime Timestamp   { get; set; }
}
