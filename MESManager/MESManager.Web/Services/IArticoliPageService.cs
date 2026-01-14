namespace MESManager.Web.Services;

public interface IPageToolbarService
{
    event Action? OnPageChanged;
    void SetActivePage(string pageKey, object? page);
    object? GetActivePage(string pageKey);
    string? GetCurrentPageKey();
    bool IsPageActive(string pageKey);
}
