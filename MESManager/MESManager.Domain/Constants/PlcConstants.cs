namespace MESManager.Domain.Constants;

/// <summary>
/// Centralizzazione assoluta di TUTTE le costanti PLC del sistema.
/// UNICO PUNTO DI RIFERIMENTO per DB numbers, offset ranges, dimensioni buffer.
/// 
/// REGOLA: Se devi modificare un numero DB, cambio SOLO qui.
/// </summary>
public static class PlcConstants
{
    // ============================================================================
    // DATABASE NUMBERS (fissi sul PLC - non cambiare leggermente con il tempo)
    // ============================================================================
    
    /// <summary>
    /// DB55: Database di LETTURA produzione
    /// Contiene:
    /// - Offsets 0-100: Parametri di lettura (READONLY) - stati produzione, cicli, scarti, etc.
    /// - Offsets 100+: Parametri di scrittura per la ricetta
    /// </summary>
    public const int PRODUCTION_DATABASE = 55;
    
    /// <summary>
    /// DB56: Database di ESECUZIONE macchina
    /// Contiene i tempi/valori reali di esecuzione da leggere in runtime
    /// </summary>
    public const int EXECUTION_DATABASE = 56;
    public const int RECIPE_DATABASE = EXECUTION_DATABASE;

    // ============================================================================
    // OFFSET RANGES (definiscono quali dati sono dove dentro i DB)
    // ============================================================================
    
    /// <summary>
    /// Offsets 0-99: Parametri di LETTURA (stato macchina, cicli realizzati)
    /// Questi sono READ-ONLY da MES → sono il "sensore" dello stato macchina
    /// </summary>
    public const int OFFSET_READONLY_START = 0;
    public const int OFFSET_READONLY_END = 99;
    
    /// <summary>
    /// Offsets 100+: Parametri di SCRITTURA (ricetta, tempi)
    /// Questi sono scritti da MES quando programma la ricetta
    /// Letti dal PLC durante i cicli
    /// </summary>
    public const int OFFSET_WRITABLE_START = 100;
    public const int OFFSET_RECIPE_PARAMETERS_START = OFFSET_WRITABLE_START;
    
    // ============================================================================
    // DIMENSIONI BUFFER (fissi - derivati dalla struttura PLC)
    // ============================================================================
    
    /// <summary>
    /// Lunghezza totale buffer DB55/DB56 in byte
    /// Usato da PlcSync quando legge (per allocare buffer[200])
    /// Usato da PlcRecipeWriterService quando scrive ricette
    /// </summary>
    public const int DATABASE_BUFFER_SIZE = 200;
    
    /// <summary>
    /// Dimensione minima per fallback lettura (se DB è più piccolo)
    /// Utilizzato quando il PLC non ha un DB della dimensione piena
    /// </summary>
    public const int DATABASE_MINIMUM_SIZE = 34;

    // ============================================================================
    // CONNESSIONE PLC (rack/slot fissi per tutti S7)
    // ============================================================================
    
    /// <summary>
    /// Rack Siemens S7 standard
    /// </summary>
    public const int PLC_RACK = 0;
    
    /// <summary>
    /// Slot Siemens S7 standard (quasi sempre 1)
    /// </summary>
    public const int PLC_SLOT = 1;
    
    /// <summary>
    /// Timeout connessione PLC in secondi
    /// </summary>
    public const int PLC_CONNECTION_TIMEOUT_SECONDS = 5;

    // ============================================================================
    // UTILITY METHODS (per accedere in modo type-safe)
    // ============================================================================
    
    public static class Database
    {
        public static int Production => PRODUCTION_DATABASE;
        public static int Execution => EXECUTION_DATABASE;
        public static int Recipe => RECIPE_DATABASE;
        public static int BufferSize => DATABASE_BUFFER_SIZE;
    }

    public static class Offsets
    {
        public static (int Start, int End) ReadOnlyRange => (OFFSET_READONLY_START, OFFSET_READONLY_END);
        public static (int Start, int End) WritableRange => (OFFSET_WRITABLE_START, int.MaxValue);
        public static int RecipeParametersStart => OFFSET_RECIPE_PARAMETERS_START;

        public static class Fields
        {
            // DB55 (0-99) - Lettura stato macchina
            public const int QuantitaRaggiunta = 16;
            public const int CicliFatti = 18;
            public const int CicliScarti = 20;
            public const int NumeroOperatore = 22;
            public const int TempoMedioRilevato = 24;
            public const int StatoEmergenza = 34;
            public const int StatoManuale = 36;
            public const int StatoAutomatico = 38;
            public const int StatoCiclo = 40;
            public const int StatoPezziRaggiunti = 42;
            public const int StatoAllarme = 44;
            public const int BarcodeLavorazione = 46;

            public const int InizioSetup = 8;
            public const int FineSetup = 10;
            public const int NuovaProduzione = 12;
            public const int FineProduzione = 14;

            // DB56 (>=100) - Lettura esecuzione
            public const int QuantitaDaProdurre = 162;
            public const int TempoMedio = 164;
            public const int Figure = 170;
        }
    }
}
