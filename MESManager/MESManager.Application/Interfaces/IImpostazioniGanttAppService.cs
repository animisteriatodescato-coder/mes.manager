using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IImpostazioniGanttAppService
{
    Task<ImpostazioniGanttDto?> GetAsync();
    Task<ImpostazioniGanttDto> SalvaAsync(ImpostazioniGanttDto dto);
}
