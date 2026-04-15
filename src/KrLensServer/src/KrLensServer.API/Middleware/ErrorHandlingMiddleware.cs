using KrLensServer.Core.Exceptions;

namespace KrLensServer.API.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.StatusCode = exception switch
            {
                FilterParameterException => StatusCodes.Status400BadRequest,
                InvalidDataException => StatusCodes.Status400BadRequest,
                SessionNotFoundException => StatusCodes.Status404NotFound,
                UnsupportedImageFormatException => StatusCodes.Status415UnsupportedMediaType,
                MsiCorruptedException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            var message = context.Response.StatusCode == StatusCodes.Status500InternalServerError
                ? "Unexpected server error."
                : exception.Message;

            await context.Response.WriteAsync(message);
        }
    }
}
