using Microsoft.AspNetCore.Components;

namespace MESManager.Web.Services;

/// <summary>
/// Servizio scoped per gestire contenuto dinamico nella AppBar del Layout.
/// Permette alle pagine di iniettare controlli custom nella barra superiore.
/// </summary>
public class AppBarContentService
{
    private RenderFragment? _content;
    private string _pageTitle = "MES Manager";
    
    public event Action? OnChange;

    /// <summary>
    /// Contenuto custom da renderizzare nell'AppBar
    /// </summary>
    public RenderFragment? Content
    {
        get => _content;
        set
        {
            _content = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Titolo della pagina corrente
    /// </summary>
    public string PageTitle
    {
        get => _pageTitle;
        set
        {
            _pageTitle = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Imposta sia titolo che contenuto in un'unica chiamata
    /// </summary>
    public void SetAppBarContent(string title, RenderFragment? content)
    {
        _pageTitle = title;
        _content = content;
        NotifyStateChanged();
    }

    /// <summary>
    /// Resetta l'AppBar al contenuto di default
    /// </summary>
    public void Clear()
    {
        _pageTitle = "MES Manager";
        _content = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
