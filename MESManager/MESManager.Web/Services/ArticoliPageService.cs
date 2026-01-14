namespace MESManager.Web.Services;

public class PageToolbarService : IPageToolbarService
{
    private string? _currentPageKey;
    private object? _activePage;
    public event Action? OnPageChanged;

    public void SetActivePage(string pageKey, object? page)
    {
        Console.WriteLine($"[PageToolbarService] SetActivePage called: pageKey='{pageKey}', page={(page != null ? "SET" : "NULL")}");
        _currentPageKey = pageKey;
        _activePage = page;
        Console.WriteLine($"[PageToolbarService] Current page is now: '{_currentPageKey}'");
        OnPageChanged?.Invoke();
    }

    public object? GetActivePage(string pageKey)
    {
        var result = _currentPageKey == pageKey ? _activePage : null;
        Console.WriteLine($"[PageToolbarService] GetActivePage('{pageKey}'): currentKey='{_currentPageKey}', returning={(result != null ? "OBJECT" : "NULL")}");
        return result;
    }

    public string? GetCurrentPageKey() 
    {
        Console.WriteLine($"[PageToolbarService] GetCurrentPageKey called, returning: '{_currentPageKey}'");
        return _currentPageKey;
    }

    public bool IsPageActive(string pageKey) => _currentPageKey == pageKey;
}
