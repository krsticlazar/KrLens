namespace KrLensServer.API.Models;

public sealed class ApplyFilterApiRequest
{
    public string SessionId { get; init; } = string.Empty;

    public string Filter { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
}
