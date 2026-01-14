namespace MESManager.Web.Services;

public class PageToolbarService : IPageToolbarService
{
    private string? _currentPageKey;
    private object? _activePage;
    public event Action? OnPageChanged;

    public void SetActivePage(string pageKey, object? page)
    {
        _currentPageKey = pageKey;
        _activePage = page;
        OnPageChanged?.Invoke();
    }

    public object? GetActivePage(string pageKey)
    {
        return _currentPageKey == pageKey ? _activePage : null;
    }

    public string? GetCurrentPageKey() => _currentPageKey;

    public bool IsPageActive(string pageKey) => _currentPageKey == pageKey;
}
