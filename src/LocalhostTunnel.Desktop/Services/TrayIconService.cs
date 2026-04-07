namespace LocalhostTunnel.Desktop.Services;

public sealed class TrayIconService
{
    public bool IsVisible { get; private set; }

    public void Show()
    {
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }
}
