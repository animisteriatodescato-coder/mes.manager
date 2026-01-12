using Sharp7;

namespace MultiMachinePlcToSheets
{
    public class MachineState
    {
        public MachineConfig Config { get; }
        public S7Client Client { get; }

        public string LastStato = "";
        public int LastOperatore = -1;

        public EventState NuovaProduzione = new();
        public EventState InizioSetup = new();
        public EventState FineSetup = new();
        public EventState InProduzione = new();

        public MachineState(MachineConfig cfg)
        {
            Config = cfg;
            Client = new S7Client();
        }
    }
}
