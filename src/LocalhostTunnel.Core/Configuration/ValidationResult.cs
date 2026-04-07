using System.Collections.ObjectModel;

namespace LocalhostTunnel.Core.Configuration;

public sealed class ValidationResult
{
    public ValidationResult(IReadOnlyDictionary<string, string> errors)
    {
        Errors = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(errors));
    }

    public IReadOnlyDictionary<string, string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
