using KrLensServer.Core.Models;

namespace KrLensServer.API.Models;

public sealed record UploadImageResponse(string SessionId, int Width, int Height, SessionState State);
