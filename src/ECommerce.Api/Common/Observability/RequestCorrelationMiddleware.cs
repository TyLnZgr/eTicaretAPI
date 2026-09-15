using System.Diagnostics;

namespace ECommerce.Api.Common.Observability;

public sealed partial class RequestCorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const int MaximumLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(
        RequestDelegate next,
        ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        var startedAt = Stopwatch.GetTimestamp();

        context.Request.Headers[HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(
                   new Dictionary<string, object?>
                   {
                       ["CorrelationId"] = correlationId
                   }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                if (!context.Request.Path.StartsWithSegments("/health"))
                {
                    LogRequestCompleted(
                        _logger,
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
                }
            }
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                HeaderName,
                out var headerValues) &&
            headerValues.Count == 1)
        {
            var candidate = headerValues[0]?.Trim();

            if (IsValid(candidate))
            {
                return candidate!;
            }
        }

        return Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;
    }

    private static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length <= MaximumLength &&
               value.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character is '-' or '_' or '.' or ':');
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:F2} ms")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string requestMethod,
        string requestPath,
        int statusCode,
        double elapsedMilliseconds);
}
