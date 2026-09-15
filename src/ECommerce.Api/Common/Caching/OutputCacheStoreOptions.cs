namespace ECommerce.Api.Common.Caching;

public sealed class OutputCacheStoreOptions
{
    public const string SectionName = "OutputCache";

    public string Provider { get; set; } = OutputCacheStoreProviders.Memory;

    public RedisOutputCacheSettings Redis { get; set; } = new();
}

public sealed class RedisOutputCacheSettings
{
    public string InstanceName { get; set; } =
        "ecommerce:output-cache:";

    public bool FailOpen { get; set; } = true;
}

public static class OutputCacheStoreProviders
{
    public const string Memory = "Memory";
    public const string Redis = "Redis";

    public static bool IsSupported(string? provider)
    {
        return string.Equals(
                   provider,
                   Memory,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   provider,
                   Redis,
                   StringComparison.OrdinalIgnoreCase);
    }
}
