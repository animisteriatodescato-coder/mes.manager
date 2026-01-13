namespace MESManager.Application.DTOs;

public class OperatoreDto
{
    public Guid Id { get; set; }
    public int NumeroOperatore { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public bool Attivo { get; set; } = true;
    public DateTime? DataAssunzione { get; set; }
    public DateTime? DataLicenziamento { get; set; }
    public double? OreLavorate { get; set; }  // Calcolate dallo storico
}
