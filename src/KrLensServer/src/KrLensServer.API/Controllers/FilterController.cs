using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KrLensServer.API.Models;
using KrLensServer.Core.Filters;
using KrLensServer.Core.Logging;
using KrLensServer.Core.Models;
using KrLensServer.Core.Services;

namespace KrLensServer.API.Controllers;

[ApiController]
[Route("api/filter")]
public sealed class FilterController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly FilterRegistry _filterRegistry;
    private readonly FilterPipeline _filterPipeline;
    private readonly ImageService _imageService;
    private readonly FilterLogger _filterLogger;

    public FilterController(
        SessionStore sessionStore,
        FilterRegistry filterRegistry,
        FilterPipeline filterPipeline,
        ImageService imageService,
        FilterLogger filterLogger)
    {
        _sessionStore = sessionStore;
        _filterRegistry = filterRegistry;
        _filterPipeline = filterPipeline;
        _imageService = imageService;
        _filterLogger = filterLogger;
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyFilterApiRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.SessionId, request.Filter);

        var current = _sessionStore.GetRequired(request.SessionId);
        var filter = _filterRegistry.GetRequired(request.Filter);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = filter.Apply(current, request.Parameters);
            stopwatch.Stop();
            _sessionStore.Push(request.SessionId, result, request.Filter, request.Parameters);
            _filterLogger.LogFilter(
                request.SessionId,
                request.Filter,
                request.Parameters,
                stopwatch.ElapsedMilliseconds,
                current.Pixels.Length,
                result.Pixels.Length);

            var bytes = await _imageService.EncodeAsync(result, "png", cancellationToken);
            return File(bytes, "image/png");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _filterLogger.LogFilterFailure(request.SessionId, request.Filter, exception);
            throw;
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> Batch([FromBody] BatchFilterApiRequest request, CancellationToken cancellationToken)
    {
        if (request.Filters.Count == 0)
        {
            throw new InvalidDataException("Batch request must contain at least one filter.");
        }

        ValidateRequest(request.SessionId, request.Filters[0].Filter);

        var current = _sessionStore.GetRequired(request.SessionId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = _filterPipeline.Apply(current, request.Filters);
            stopwatch.Stop();
            _sessionStore.Push(request.SessionId, result, "Batch", null);
            _filterLogger.LogFilter(
                request.SessionId,
                "Batch",
                request.Filters
                    .Select((filter, index) => new KeyValuePair<string, double>($"{index}:{filter.Filter}", filter.Parameters.Count))
                    .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase),
                stopwatch.ElapsedMilliseconds,
                current.Pixels.Length,
                result.Pixels.Length);

            var bytes = await _imageService.EncodeAsync(result, "png", cancellationToken);
            return File(bytes, "image/png");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _filterLogger.LogFilterFailure(request.SessionId, "Batch", exception);
            throw;
        }
    }

    private static void ValidateRequest(string sessionId, string filter)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidDataException("SessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new InvalidDataException("Filter name is required.");
        }
    }
}
