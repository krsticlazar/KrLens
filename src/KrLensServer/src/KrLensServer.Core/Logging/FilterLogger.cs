using Microsoft.Extensions.Logging;

namespace KrLensServer.Core.Logging;

public sealed class FilterLogger
{
    private readonly ILogger<FilterLogger> _logger;

    public FilterLogger(ILogger<FilterLogger> logger)
    {
        _logger = logger;
    }

    public void LogUpload(string sessionId, string fileName, long sizeBytes, string user, int width, int height)
    {
        _logger.LogInformation(
            "Upload: sessionId={SessionId}, file={FileName}, size={SizeBytes}, user={User}, width={Width}, height={Height}",
            sessionId,
            fileName,
            sizeBytes,
            user,
            width,
            height);
    }

    public void LogFilter(
        string sessionId,
        string filter,
        IReadOnlyDictionary<string, double> parameters,
        long durationMs,
        int inputBytes,
        int outputBytes)
    {
        _logger.LogInformation(
            "Filter: sessionId={SessionId}, filter={Filter}, params={@Parameters}, duration={DurationMs}ms, inputSize={InputBytes}, outputSize={OutputBytes}",
            sessionId,
            filter,
            parameters,
            durationMs,
            inputBytes,
            outputBytes);
    }

    public void LogFilterFailure(string sessionId, string filter, Exception exception)
    {
        _logger.LogError(exception, "Filter failed: sessionId={SessionId}, filter={Filter}", sessionId, filter);
    }
}
