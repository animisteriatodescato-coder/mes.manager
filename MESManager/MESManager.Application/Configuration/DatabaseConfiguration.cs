using System.Collections.Generic;

namespace MESManager.Application.Configuration
{
    /// <summary>
    /// Configurazione centralizzata delle connection string per tutti i database.
    /// Le credenziali vengono lette da appsettings.Secrets.json (non in git) o variabili d'ambiente.
    /// </summary>
    public class DatabaseConfiguration
    {
        /// <summary>
        /// Connection string per il database principale MESManager
        /// </summary>
        public string MESManagerDb { get; set; } = string.Empty;

        /// <summary>
        /// Connection string per il database Mago (ERP esterno)
        /// </summary>
        public string MagoDb { get; set; } = string.Empty;

        /// <summary>
        /// Connection string per il database Gantt (legacy)
        /// </summary>
        public string GanttDb { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurazione per la connessione a Mago ERP
    /// </summary>
    public class MagoConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurazione di sicurezza dell'applicazione
    /// </summary>
    public class SecurityConfiguration
    {
        /// <summary>
        /// Host consentiti separati da punto e virgola
        /// </summary>
        public string AllowedHosts { get; set; } = "localhost";

        /// <summary>
        /// Chiave API per autenticazione esterna (opzionale)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurazione per i percorsi dei file allegati
    /// </summary>
    public class FileConfiguration
    {
        /// <summary>
        /// Percorso base per gli allegati (produzione)
        /// </summary>
        public string AllegatiBasePath { get; set; } = @"C:\Dati\Documenti\AA SCHEDE PRODUZIONE\foto cel";

        /// <summary>
        /// Mappature di percorsi di rete (formato: "P:\Documenti->C:\Dati\Documenti")
        /// </summary>
        public List<string> PathMappings { get; set; } = new()
        {
            @"P:\Documenti->C:\Dati\Documenti"
        };
    }
}
