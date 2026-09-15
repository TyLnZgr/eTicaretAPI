namespace ECommerce.Api.Common.RateLimiting;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowRateLimitSettings Global { get; set; } = new();

    public SlidingWindowRateLimitSettings Authentication { get; set; } = new();

    public TokenBucketRateLimitSettings Mutation { get; set; } = new();
}

public sealed class FixedWindowRateLimitSettings
{
    public int PermitLimit { get; set; } = 300;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public sealed class SlidingWindowRateLimitSettings
{
    public int PermitLimit { get; set; } = 10;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    public int SegmentsPerWindow { get; set; } = 6;
}

public sealed class TokenBucketRateLimitSettings
{
    public int TokenLimit { get; set; } = 30;

    public int TokensPerPeriod { get; set; } = 5;

    public TimeSpan ReplenishmentPeriod { get; set; } =
        TimeSpan.FromSeconds(10);
}
