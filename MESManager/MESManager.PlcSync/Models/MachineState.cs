using Sharp7;

namespace MESManager.PlcSync.Models;

public class MachineState
{
    public S7Client Client { get; set; }
    public string LastStato { get; set; } = string.Empty;
    public int LastNumeroOperatore { get; set; } = -1;
    
    // Per tracciare eventi 0→1
    public bool PrevNuovaProduzione { get; set; }
    public bool PrevInizioSetup { get; set; }
    public bool PrevFineSetup { get; set; }
    public bool PrevInProduzione { get; set; }
    
    public MachineState()
    {
        Client = new S7Client();
    }
}
