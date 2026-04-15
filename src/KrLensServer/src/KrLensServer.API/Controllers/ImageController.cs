using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using KrLensServer.API.Models;
using KrLensServer.Core.Logging;
using KrLensServer.Core.Models;
using KrLensServer.Core.Services;

namespace KrLensServer.API.Controllers;

[ApiController]
[Route("api/image")]
public sealed class ImageController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedMimeTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = new[] { "image/png" },
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".bmp"] = new[] { "image/bmp", "image/x-ms-bmp", "image/x-bmp" },
        [".gif"] = new[] { "image/gif" },
        [".msi"] = new[] { "application/octet-stream", "application/msi" },
    };

    private readonly SessionStore _sessionStore;
    private readonly ImageService _imageService;
    private readonly FilterLogger _filterLogger;
    private readonly UploadOptions _options;

    public ImageController(
        SessionStore sessionStore,
        ImageService imageService,
        FilterLogger filterLogger,
        IOptions<UploadOptions> options)
    {
        _sessionStore = sessionStore;
        _imageService = imageService;
        _filterLogger = filterLogger;
        _options = options.Value;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<UploadImageResponse>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new InvalidDataException("Upload file is required.");
        }

        if (file.Length > _options.MaxUploadBytes)
        {
            throw new InvalidDataException($"File exceeds the {_options.MaxUploadBytes / (1024 * 1024)} MB upload limit.");
        }

        var safeName = Path.GetFileName(file.FileName);
        if (!string.Equals(safeName, file.FileName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid file name.");
        }

        var extension = Path.GetExtension(safeName);
        if (!AllowedMimeTypes.TryGetValue(extension, out var allowedMimeTypes))
        {
            throw new InvalidDataException($"File extension '{extension}' is not supported.");
        }

        if (!allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"MIME type '{file.ContentType}' is not valid for '{extension}'.");
        }

        await using var stream = file.OpenReadStream();
        var image = await _imageService.LoadAsync(stream, safeName, cancellationToken);
        var sessionId = _sessionStore.Create(image);
        var user = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _filterLogger.LogUpload(sessionId, safeName, file.Length, user, image.Width, image.Height);

        return Ok(new UploadImageResponse(sessionId, image.Width, image.Height, _sessionStore.GetState(sessionId)));
    }

    [HttpGet("download/{sessionId}")]
    public async Task<IActionResult> Download(string sessionId, [FromQuery] string format = "png", CancellationToken cancellationToken = default)
    {
        var image = _sessionStore.GetRequired(sessionId);
        var bytes = await _imageService.EncodeAsync(image, format, cancellationToken);
        var contentType = _imageService.GetContentType(format);
        var normalizedFormat = format.Trim().TrimStart('.').ToLowerInvariant();
        return File(bytes, contentType, $"{sessionId}.{normalizedFormat}");
    }

    [HttpGet("session/{sessionId}/state")]
    public ActionResult<SessionState> State(string sessionId)
    {
        return Ok(_sessionStore.GetState(sessionId));
    }

    [HttpPost("session/{sessionId}/undo")]
    public Task<IActionResult> Undo(string sessionId, CancellationToken cancellationToken)
    {
        var image = _sessionStore.Undo(sessionId);
        return CreatePreviewResponse(image, cancellationToken);
    }

    [HttpPost("session/{sessionId}/redo")]
    public Task<IActionResult> Redo(string sessionId, CancellationToken cancellationToken)
    {
        var image = _sessionStore.Redo(sessionId);
        return CreatePreviewResponse(image, cancellationToken);
    }

    [HttpPost("session/{sessionId}/revert")]
    public Task<IActionResult> Revert(string sessionId, CancellationToken cancellationToken)
    {
        var image = _sessionStore.Revert(sessionId);
        return CreatePreviewResponse(image, cancellationToken);
    }

    [HttpPost("session/{sessionId}/rotate-right")]
    public Task<IActionResult> RotateRight(string sessionId, CancellationToken cancellationToken)
    {
        var image = _sessionStore.RotateRight(sessionId);
        return CreatePreviewResponse(image, cancellationToken);
    }

    [HttpDelete("session/{sessionId}")]
    public IActionResult Delete(string sessionId)
    {
        _sessionStore.Delete(sessionId);
        return NoContent();
    }

    private async Task<IActionResult> CreatePreviewResponse(BitmapBuffer image, CancellationToken cancellationToken)
    {
        var bytes = await _imageService.EncodeAsync(image, "png", cancellationToken);
        return File(bytes, "image/png");
    }
}
