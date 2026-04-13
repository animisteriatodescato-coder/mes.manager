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
    /// <summary>Calcola con un lotto specifico, margine % e sconto % opzionali.</summary>
    PreventivoCalcoloResult CalcolaConLotto(PreventivoDto dto, int lotto, decimal margine = 0);

    // ── Feature v1.65.7 ────────────────────────────────────────────────
    /// <summary>Duplica un preventivo esistente creando un nuovo record con nuovo numero.</summary>
    Task<PreventivoDto?> DuplicaAsync(Guid id);

    /// <summary>Ritorna lo storico revisioni di un preventivo.</summary>
    Task<List<PreventivoRevisioneDto>> GetRevisioniAsync(Guid preventivoId);

    /// <summary>Ripristina i dati di una revisione precedente (crea nuova revisione con i dati attuali).</summary>
    Task<PreventivoDto?> RipristinaRevisioneAsync(Guid revisioneId);

    /// <summary>Ritorna tutti i template salvati.</summary>
    Task<List<PreventivoTemplateDto>> GetTemplatesAsync();

    /// <summary>Salva il preventivo come template riutilizzabile.</summary>
    Task<PreventivoTemplateDto> SalvaTemplateAsync(string nome, string? descrizione, PreventivoDto dto);

    /// <summary>Carica i parametri di un template nel DTO (senza sovrascrivere cliente/stato).</summary>
    Task<PreventivoDto?> CaricaTemplateAsync(Guid templateId);

    /// <summary>Elimina un template.</summary>
    Task<bool> EliminaTemplateAsync(Guid id);

    /// <summary>Registra l'invio email sul preventivo.</summary>
    Task<PreventivoDto?> RegistraInvioEmailAsync(Guid id, string destinatario);
}
