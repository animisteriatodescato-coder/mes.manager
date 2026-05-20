namespace MESManager.Domain.Entities;

/// <summary>
/// Record storico orario dell'impianto fotovoltaico.
/// Inserito ogni ora dal FotovoltaicoWorker con i valori aggregati dell'ora.
/// </summary>
public class FotovoltaicoStorico
{
    public long Id { get; set; }

    /// <summary>Ora di riferimento (troncata all'ora).</summary>
    public DateTime Timestamp { get; set; }

    public double PotenzaMedia_kW { get; set; }
    public double PotenzaMassima_kW { get; set; }
    public double EnergiaOra_kWh { get; set; }          // kWh prodotti nell'ora
    public double EnergiaAccumulata_kWh { get; set; }   // totale a fine ora
    public string StatoInverter { get; set; } = string.Empty;
}
