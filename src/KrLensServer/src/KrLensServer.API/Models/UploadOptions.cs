namespace KrLensServer.API.Models;

public sealed class UploadOptions
{
    public long MaxUploadBytes { get; init; } = 20 * 1024 * 1024;
}
