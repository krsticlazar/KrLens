using KrLensServer.Core.Models;

namespace KrLensServer.API.Models;

public sealed class BatchFilterApiRequest
{
    public string SessionId { get; init; } = string.Empty;

    public IReadOnlyList<FilterRequest> Filters { get; init; } = Array.Empty<FilterRequest>();
}
