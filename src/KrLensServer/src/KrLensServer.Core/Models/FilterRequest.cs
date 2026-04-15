namespace KrLensServer.Core.Models;

public sealed class FilterRequest
{
    public string Filter { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
}
