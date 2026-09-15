using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Common.Caching;

public static class CatalogOutputCachingExtensions
{
    public static IServiceCollection AddCatalogOutputCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            OutputCacheStoreOptions.SectionName);

        services
            .AddOptions<OutputCacheStoreOptions>()
            .Bind(section)
            .Validate(
                options => OutputCacheStoreProviders.IsSupported(
                    options.Provider),
                "Output cache provider must be Memory or Redis.")
            .Validate(
                options =>
                    !IsRedis(options.Provider) ||
                    !string.IsNullOrWhiteSpace(
                        configuration.GetConnectionString("Redis")),
                "Redis connection string was not found.")
            .Validate(
                options =>
                    !IsRedis(options.Provider) ||
                    !string.IsNullOrWhiteSpace(
                        options.Redis.InstanceName),
                "Redis output cache instance name is required.")
            .ValidateOnStart();

        services.AddOutputCache(CatalogOutputCache.Configure);
        var memoryStoreRegistration = GetLastStoreRegistration(services);

        services.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration =
                configuration.GetConnectionString("Redis");
            options.InstanceName = section[
                $"{nameof(OutputCacheStoreOptions.Redis)}:{nameof(RedisOutputCacheSettings.InstanceName)}"];
        });
        var redisStoreRegistration = GetLastStoreRegistration(services);

        services.Remove(memoryStoreRegistration);
        services.Remove(redisStoreRegistration);

        services.AddSingleton<IOutputCacheStore>(serviceProvider =>
            CreateConfiguredStore(
                serviceProvider,
                memoryStoreRegistration,
                redisStoreRegistration));
        services.AddSingleton<CatalogOutputCache>();

        return services;
    }

    private static IOutputCacheStore CreateConfiguredStore(
        IServiceProvider serviceProvider,
        ServiceDescriptor memoryStoreRegistration,
        ServiceDescriptor redisStoreRegistration)
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<OutputCacheStoreOptions>>()
            .Value;

        if (!IsRedis(settings.Provider))
        {
            return CreateStore(
                serviceProvider,
                memoryStoreRegistration);
        }

        var redisStore = CreateStore(
            serviceProvider,
            redisStoreRegistration);

        if (!settings.Redis.FailOpen)
        {
            return redisStore;
        }

        return new ResilientOutputCacheStore(
            redisStore,
            serviceProvider.GetRequiredService<
                ILogger<ResilientOutputCacheStore>>());
    }

    private static IOutputCacheStore CreateStore(
        IServiceProvider serviceProvider,
        ServiceDescriptor registration)
    {
        if (registration.ImplementationInstance is IOutputCacheStore instance)
        {
            return instance;
        }

        if (registration.ImplementationFactory is not null)
        {
            return (IOutputCacheStore)registration
                .ImplementationFactory(serviceProvider);
        }

        if (registration.ImplementationType is not null)
        {
            return (IOutputCacheStore)ActivatorUtilities.CreateInstance(
                serviceProvider,
                registration.ImplementationType);
        }

        throw new InvalidOperationException(
            "The output cache store registration is invalid.");
    }

    private static ServiceDescriptor GetLastStoreRegistration(
        IServiceCollection services)
    {
        return services.LastOrDefault(descriptor =>
                   descriptor.ServiceType == typeof(IOutputCacheStore))
               ?? throw new InvalidOperationException(
                   "An output cache store was not registered.");
    }

    private static bool IsRedis(string? provider)
    {
        return string.Equals(
            provider,
            OutputCacheStoreProviders.Redis,
            StringComparison.OrdinalIgnoreCase);
    }
}
