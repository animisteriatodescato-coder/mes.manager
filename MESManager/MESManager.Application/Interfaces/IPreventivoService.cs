using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

public interface IPreventivoService
{
    // ── Tipi Sabbia ────────────────────────────────────────────────────
    Task<List<PreventivoTipoSabbiaDto>> GetTipiSabbiaAsync();
    Task<PreventivoTipoSabbiaDto> CreateTipoSabbiaAsync(PreventivoTipoSabbiaDto dto);
    Task<PreventivoTipoSabbiaDto?> UpdateTipoSabbiaAsync(PreventivoTipoSabbiaDto dto);
    Task<bool> DeleteTipoSabbiaAsync(Guid id);

    // ── Tipi Vernice ───────────────────────────────────────────────────
    Task<List<PreventivoTipoVerniceDto>> GetTipiVerniceAsync();
    Task<PreventivoTipoVerniceDto> CreateTipoVerniceAsync(PreventivoTipoVerniceDto dto);
    Task<PreventivoTipoVerniceDto?> UpdateTipoVerniceAsync(PreventivoTipoVerniceDto dto);
    Task<bool> DeleteTipoVerniceAsync(Guid id);

    // ── Preventivi ─────────────────────────────────────────────────────
    Task<List<PreventivoDto>> GetAllAsync();
    Task<PreventivoDto?> GetByIdAsync(Guid id);
    Task<PreventivoDto> CreateAsync(PreventivoDto dto);
    Task<PreventivoDto?> UpdateAsync(PreventivoDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<PreventivoDto?> UpdateStatoAsync(Guid id, string stato);

    // ── Calcolo (logica pura, usata anche lato Blazor) ─────────────────
    PreventivoCalcoloResult Calcola(PreventivoDto dto);
    /// <summary>Calcola con un lotto specifico e margine % opzionale (per vista multi-lotto)</summary>
    PreventivoCalcoloResult CalcolaConLotto(PreventivoDto dto, int lotto, decimal margine = 0);
}
