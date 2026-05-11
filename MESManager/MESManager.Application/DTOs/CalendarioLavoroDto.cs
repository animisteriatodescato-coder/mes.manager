using MESManager.Domain.Constants;

namespace MESManager.Application.DTOs;

public class CalendarioLavoroDto
{
    public Guid Id { get; set; }
    public bool Lunedi { get; set; } = CalendarioLavoroDefaults.Lunedi;
    public bool Martedi { get; set; } = CalendarioLavoroDefaults.Martedi;
    public bool Mercoledi { get; set; } = CalendarioLavoroDefaults.Mercoledi;
    public bool Giovedi { get; set; } = CalendarioLavoroDefaults.Giovedi;
    public bool Venerdi { get; set; } = CalendarioLavoroDefaults.Venerdi;
    public bool Sabato { get; set; } = CalendarioLavoroDefaults.Sabato;
    public bool Domenica { get; set; } = CalendarioLavoroDefaults.Domenica;
    
    public TimeOnly OraInizio { get; set; } = CalendarioLavoroDefaults.OraInizio;
    public TimeOnly OraFine { get; set; } = CalendarioLavoroDefaults.OraFine;
}
