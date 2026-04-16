using MESManager.Application.DTOs;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Services;

/// <summary>
/// Bridge tra AppSettingsService (Web) e AiAssistantService (Infrastructure).
/// Implementa IAiSettingsReader per consentire all'Infrastructure di leggere
/// la configurazione AI runtime senza dipendere dal layer Web.
/// </summary>
public class WebAiSettingsReader : IAiSettingsReader
{
    private readonly AppSettingsService _appSettings;

    public WebAiSettingsReader(AppSettingsService appSettings)
    {
        _appSettings = appSettings;
    }

    public AiProviderConfig GetConfig()
    {
        var s = _appSettings.GetSettings();
        return new AiProviderConfig
        {
            ProviderType  = s.AiProviderType,
            OllamaBaseUrl = s.OllamaBaseUrl,
            OllamaModel   = s.OllamaModel,
            OpenAiModel   = string.IsNullOrWhiteSpace(s.OpenAiModel) ? "gpt-4o-mini" : s.OpenAiModel
        };
    }
}
