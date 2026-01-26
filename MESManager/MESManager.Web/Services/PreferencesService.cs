using Microsoft.JSInterop;
using System.Text.Json;
using MESManager.Application.Services;
using MESManager.Application.Interfaces;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio per la gestione delle preferenze utente.
/// Se un utente è selezionato (CurrentUserService), salva/carica dal database.
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
    /// Recupera una preferenza. Se utente selezionato, usa database, altrimenti localStorage.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // Se c'è un utente selezionato, usa il database
            if (_currentUserService.HasUser && _currentUserService.CurrentUserId.HasValue)
            {
                var json = await _preferenzeUtenteService.GetAsync(_currentUserService.CurrentUserId.Value, key);
                if (string.IsNullOrEmpty(json))
                    return default;

                return JsonSerializer.Deserialize<T>(json);
            }

            // Altrimenti fallback a localStorage
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
    /// Salva una preferenza. Se utente selezionato, usa database, altrimenti localStorage.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);

            // Se c'è un utente selezionato, salva nel database
            if (_currentUserService.HasUser && _currentUserService.CurrentUserId.HasValue)
            {
                await _preferenzeUtenteService.SaveAsync(_currentUserService.CurrentUserId.Value, key, json);
            }
            else
            {
                // Altrimenti fallback a localStorage
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
            // Se c'è un utente selezionato, rimuovi dal database
            if (_currentUserService.HasUser && _currentUserService.CurrentUserId.HasValue)
            {
                await _preferenzeUtenteService.DeleteAsync(_currentUserService.CurrentUserId.Value, key);
            }
            else
            {
                // Altrimenti rimuovi da localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
        }
        catch
        {
            // Ignore errors
        }
    }
}
