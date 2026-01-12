namespace MESManager.Domain.Entities;

public class Operatore
{
    public Guid Id { get; set; }
    public int? NumeroOperatore { get; set; }  // Numero dal PLC
    public string Matricola { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
}
