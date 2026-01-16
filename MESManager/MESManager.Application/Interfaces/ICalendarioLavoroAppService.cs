using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface ICalendarioLavoroAppService
{
    Task<CalendarioLavoroDto?> GetAsync();
    Task<CalendarioLavoroDto> SalvaAsync(CalendarioLavoroDto dto);
}
