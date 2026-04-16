using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Bridge tra Web (AppSettingsService) e Infrastructure (AiAssistantService).
/// Permette all'Infrastructure di leggere la config AI runtime senza dipendere da Web.
/// </summary>
public interface IAiSettingsReader
{
    AiProviderConfig GetConfig();
}
