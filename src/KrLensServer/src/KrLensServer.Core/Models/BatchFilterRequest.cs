namespace KrLensServer.Core.Models;

public sealed class BatchFilterRequest
{
    public IReadOnlyList<FilterRequest> Filters { get; init; } = Array.Empty<FilterRequest>();
}
