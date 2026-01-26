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
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private bool _userLoadAttempted = false;
    
    private readonly IJSRuntime _jsRuntime;
    private readonly CurrentUserService _currentUserService;
    private readonly IPreferenzeUtenteService _preferenzeUtenteService;
    private readonly IUtenteAppService _utenteAppService;

    public PreferencesService(
        IJSRuntime jsRuntime, 
        CurrentUserService currentUserService,
        IPreferenzeUtenteService preferenzeUtenteService,
        IUtenteAppService utenteAppService)
    {
        _jsRuntime = jsRuntime;
        _currentUserService = currentUserService;
        _preferenzeUtenteService = preferenzeUtenteService;
        _utenteAppService = utenteAppService;
    }

    /// <summary>
    /// Assicura che l'utente sia caricato dal localStorage se non già presente
    /// </summary>
    private async Task EnsureUserLoadedAsync()
    {
        if (_currentUserService.HasUser || _userLoadAttempted)
            return;

        await _loadSemaphore.WaitAsync();
        try
        {
            // Double-check dopo aver acquisito il lock
            if (_currentUserService.HasUser || _userLoadAttempted)
                return;
                
            _userLoadAttempted = true;
            
            var savedUserId = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "selectedUserId");
            if (!string.IsNullOrEmpty(savedUserId) && Guid.TryParse(savedUserId, out var userId))
            {
                var utente = await _utenteAppService.GetByIdAsync(userId);
                if (utente != null)
                {
                    _currentUserService.SetCurrentUser(utente);
                    Console.WriteLine($"[PreferencesService] Loaded user from localStorage: {utente.Nome} (ID: {userId})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PreferencesService] Error loading user from localStorage: {ex.Message}");
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    /// <summary>
    /// Recupera una preferenza. Se utente selezionato, usa database, altrimenti localStorage.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            // Assicura che l'utente sia caricato
            await EnsureUserLoadedAsync();
            
            // Se c'è un utente selezionato, usa il database
            if (_currentUserService.HasUser && _currentUserService.CurrentUserId.HasValue)
            {
                Console.WriteLine($"[PreferencesService] GetAsync: Loading '{key}' for user {_currentUserService.CurrentUserName} (ID: {_currentUserService.CurrentUserId})");
                
                var json = await _preferenzeUtenteService.GetAsync(_currentUserService.CurrentUserId.Value, key);
                if (string.IsNullOrEmpty(json))
                    return default;

                return JsonSerializer.Deserialize<T>(json);
            }

            Console.WriteLine($"[PreferencesService] GetAsync: No user, loading '{key}' from localStorage");
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
            // Assicura che l'utente sia caricato
            await EnsureUserLoadedAsync();
            
            var json = JsonSerializer.Serialize(value);

            // Se c'è un utente selezionato, salva nel database
            if (_currentUserService.HasUser && _currentUserService.CurrentUserId.HasValue)
            {
                Console.WriteLine($"[PreferencesService] SetAsync: Saving '{key}' for user {_currentUserService.CurrentUserName} (ID: {_currentUserService.CurrentUserId})");
                await _preferenzeUtenteService.SaveAsync(_currentUserService.CurrentUserId.Value, key, json);
            }
            else
            {
                Console.WriteLine($"[PreferencesService] SetAsync: No user selected, saving '{key}' to localStorage");
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
            // Assicura che l'utente sia caricato
            await EnsureUserLoadedAsync();
            
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
