namespace LocalhostTunnel.Application.Services;

public sealed class NavigationService
{
    private string _currentRoute = "overview";

    public string CurrentRoute => _currentRoute;

    public event EventHandler<string>? RouteChanged;

    public void Navigate(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        var normalized = route.Trim().ToLowerInvariant();
        if (string.Equals(_currentRoute, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _currentRoute = normalized;
        RouteChanged?.Invoke(this, _currentRoute);
    }
}
