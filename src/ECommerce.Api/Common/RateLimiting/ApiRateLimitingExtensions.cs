using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ECommerce.Api.Common.RateLimiting;

public static class ApiRateLimitingExtensions
{
    private const string GlobalPolicyName = "global";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            ApiRateLimitOptions.SectionName);

        services
            .AddOptions<ApiRateLimitOptions>()
            .Bind(section)
            .Validate(IsValid, "Rate limiting configuration is invalid.")
            .ValidateOnStart();

        services.AddRateLimiter();
        services
            .AddOptions<RateLimiterOptions>()
            .Configure<IOptions<ApiRateLimitOptions>>(
                (options, configuredSettings) => Configure(
                    options,
                    configuredSettings.Value));

        return services;
    }

    private static void Configure(
        RateLimiterOptions options,
        ApiRateLimitOptions settings)
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, cancellationToken) =>
            WriteRejectedResponseAsync(
                context,
                settings,
                cancellationToken);

        options.GlobalLimiter = PartitionedRateLimiter.Create<
            HttpContext,
            string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitionKey.Create(
                        context,
                        GlobalPolicyName),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = settings.Global.PermitLimit,
                        QueueLimit = 0,
                        QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst,
                        Window = settings.Global.Window
                    }));

        options.AddPolicy(
            ApiRateLimitPolicies.Authentication,
            context => RateLimitPartition.GetSlidingWindowLimiter(
                RateLimitPartitionKey.Create(
                    context,
                    ApiRateLimitPolicies.Authentication),
                _ => new SlidingWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit =
                        settings.Authentication.PermitLimit,
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    SegmentsPerWindow =
                        settings.Authentication.SegmentsPerWindow,
                    Window = settings.Authentication.Window
                }));

        options.AddPolicy(
            ApiRateLimitPolicies.Mutation,
            context => RateLimitPartition.GetTokenBucketLimiter(
                RateLimitPartitionKey.Create(
                    context,
                    ApiRateLimitPolicies.Mutation),
                _ => new TokenBucketRateLimiterOptions
                {
                    AutoReplenishment = true,
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    ReplenishmentPeriod =
                        settings.Mutation.ReplenishmentPeriod,
                    TokenLimit = settings.Mutation.TokenLimit,
                    TokensPerPeriod =
                        settings.Mutation.TokensPerPeriod
                }));
    }

    private static async ValueTask WriteRejectedResponseAsync(
        OnRejectedContext context,
        ApiRateLimitOptions settings,
        CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var problem = new ProblemDetails
        {
            Detail = "Too many requests were sent. Try again later.",
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4"
        };

        problem.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var retryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var estimatedRetryAfter)
                ? estimatedRetryAfter
                : GetConservativeRetryAfter(httpContext, settings);

        var retryAfterSeconds = Math.Max(
            1,
            (int)Math.Ceiling(retryAfter.TotalSeconds));

        httpContext.Response.Headers[HeaderNames.RetryAfter] =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;

        var problemDetailsService = httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>();

        await problemDetailsService.WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            });
    }

    private static TimeSpan GetConservativeRetryAfter(
        HttpContext context,
        ApiRateLimitOptions settings)
    {
        var endpointPolicy = context.GetEndpoint()?
            .Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?
            .PolicyName;

        var endpointRetryAfter = endpointPolicy switch
        {
            ApiRateLimitPolicies.Authentication =>
                settings.Authentication.Window,
            ApiRateLimitPolicies.Mutation =>
                settings.Mutation.ReplenishmentPeriod,
            _ => TimeSpan.Zero
        };

        return endpointRetryAfter > settings.Global.Window
            ? endpointRetryAfter
            : settings.Global.Window;
    }

    private static bool IsValid(ApiRateLimitOptions options)
    {
        return IsValidWindow(
                   options.Global.PermitLimit,
                   options.Global.Window) &&
               IsValidWindow(
                   options.Authentication.PermitLimit,
                   options.Authentication.Window) &&
               options.Authentication.SegmentsPerWindow is >= 1 and <= 100 &&
               options.Mutation.TokenLimit is >= 1 and <= 1_000_000 &&
               options.Mutation.TokensPerPeriod is >= 1 and <= 1_000_000 &&
               options.Mutation.TokensPerPeriod <=
                   options.Mutation.TokenLimit &&
               options.Mutation.ReplenishmentPeriod > TimeSpan.Zero &&
               options.Mutation.ReplenishmentPeriod <= TimeSpan.FromDays(1);
    }

    private static bool IsValidWindow(int permitLimit, TimeSpan window)
    {
        return permitLimit is >= 1 and <= 1_000_000 &&
               window > TimeSpan.Zero &&
               window <= TimeSpan.FromDays(1);
    }
}
