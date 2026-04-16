using LocalhostTunnel.Core.Configuration;

namespace LocalhostTunnel.Desktop.ViewModels;

public sealed class ProfileListItemViewModel
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required ProfileType Type { get; init; }

    public required bool Enabled { get; init; }

    public string TypeLabel => Type == ProfileType.Tavily ? "Tavily API" : "Standard";
}

