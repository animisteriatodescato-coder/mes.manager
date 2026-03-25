using Microsoft.JSInterop;
using System.Text.Json;
using MESManager.Application.Services;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio per la gestione delle preferenze utente.
/// Se l'utente e' autenticato (CurrentUserService.HasUser), salva/carica dal database.
/// Altrimenti usa localStorage come fallback.
/// </summary>
public class PreferencesService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly CurrentUserService _currentUserService;
    private readonly IPreferenzeUtenteService _preferenzeUtenteService;

    public PreferencesService(
        IJSRuntime jsRuntime,
        CurrentUserService currentUserService,
        IPreferenzeUtenteService preferenzeUtenteService)
    {
        _jsRuntime = jsRuntime;
        _currentUserService = currentUserService;
        _preferenzeUtenteService = preferenzeUtenteService;
    }

    /// <summary>
    /// Recupera una preferenza. Se utente autenticato, usa database, altrimenti localStorage.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_currentUserService.HasUser)
            {
                var json = await _preferenzeUtenteService.GetAsync(_currentUserService.UserId!, key);
                if (string.IsNullOrEmpty(json))
                    return default;
                return JsonSerializer.Deserialize<T>(json);
            }

            var localJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(localJson))
                return default;
            return JsonSerializer.Deserialize<T>(localJson);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Salva una preferenza. Se utente autenticato, usa database, altrimenti localStorage.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            if (_currentUserService.HasUser)
            {
                await _preferenzeUtenteService.SaveAsync(_currentUserService.UserId!, key, json);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Rimuove una preferenza.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            if (_currentUserService.HasUser)
            {
                await _preferenzeUtenteService.DeleteAsync(_currentUserService.UserId!, key);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
