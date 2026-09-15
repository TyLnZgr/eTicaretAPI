using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Common.Caching;
using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Catalog;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class DistributedOutputCacheTests
{
    [Fact]
    public async Task RedisUnavailable_CatalogRequestFallsBackToDatabase()
    {
        // Arrange
        using var factory = new ECommerceApiFactory(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["OutputCache:Provider"] = "Redis",
                ["ConnectionStrings:Redis"] =
                    "127.0.0.1:1,abortConnect=false,connectRetry=0,connectTimeout=100,syncTimeout=100,asyncTimeout=100"
            });
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Products.Add(CreateProduct("Keyboard"));
            await dbContext.SaveChangesAsync();
        });

        // Act
        using var firstResponse = await client.GetAsync("/api/products");
        using var secondResponse = await client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(
            "Keyboard",
            Assert.Single((await ReadProductsAsync(firstResponse)).Items)
                .Name);
        Assert.IsType<ResilientOutputCacheStore>(
            factory.Services.GetRequiredService<IOutputCacheStore>());
    }

    [Fact]
    public async Task SharedStore_SharesEntriesAndTagEvictionAcrossInstances()
    {
        // Arrange
        var sharedStore = new SharedOutputCacheStore();

        using var firstFactory = CreateFactoryWithStore(sharedStore);
        using var secondFactory = CreateFactoryWithStore(sharedStore);
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();

        var firstProductId = 0;

        await firstFactory.SeedDatabaseAsync(async dbContext =>
        {
            var product = CreateProduct("First instance product");
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
            firstProductId = product.Id;
        });

        await secondFactory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Products.Add(CreateProduct(
                "Second instance product"));
            await dbContext.SaveChangesAsync();
        });

        using var initialResponse = await firstClient.GetAsync(
            "/api/products");
        Assert.Equal(
            "First instance product",
            Assert.Single((await ReadProductsAsync(initialResponse)).Items)
                .Name);

        await firstFactory.SeedDatabaseAsync(async dbContext =>
        {
            await dbContext.Products
                .Where(product => product.Id == firstProductId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    product => product.Name,
                    "Changed first instance product"));
        });

        // Act - the second app instance reads the entry cached by the first.
        using var sharedResponse = await secondClient.GetAsync(
            "/api/products");

        await using (var scope = secondFactory.Services
            .CreateAsyncScope())
        {
            var catalogCache = scope.ServiceProvider
                .GetRequiredService<CatalogOutputCache>();
            await catalogCache.EvictProductsAsync();
        }

        using var refreshedResponse = await firstClient.GetAsync(
            "/api/products");

        // Assert
        Assert.Equal(
            "First instance product",
            Assert.Single((await ReadProductsAsync(sharedResponse)).Items)
                .Name);
        Assert.Equal(
            "Changed first instance product",
            Assert.Single((await ReadProductsAsync(refreshedResponse)).Items)
                .Name);
        Assert.True(sharedStore.CacheHits >= 1);
        Assert.Equal(1, sharedStore.TagEvictions);
    }

    private static ECommerceApiFactory CreateFactoryWithStore(
        IOutputCacheStore store)
    {
        return new ECommerceApiFactory(
            serviceOverrides: services =>
            {
                services.RemoveAll<IOutputCacheStore>();
                services.AddSingleton(store);
            });
    }

    private static Product CreateProduct(string name)
    {
        return new Product
        {
            Name = name,
            Price = 1000m,
            StockQuantity = 10,
            IsActive = true,
            Category = new Category
            {
                Name = "Accessories",
                IsActive = true
            }
        };
    }

    private static async Task<PagedResult<ProductResponse>> ReadProductsAsync(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return (await response.Content
            .ReadFromJsonAsync<PagedResult<ProductResponse>>())!;
    }

    private sealed class SharedOutputCacheStore : IOutputCacheStore
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _entries =
            new(StringComparer.Ordinal);

        private int _cacheHits;
        private int _tagEvictions;

        public int CacheHits => Volatile.Read(ref _cacheHits);
        public int TagEvictions => Volatile.Read(ref _tagEvictions);

        public ValueTask<byte[]?> GetAsync(
            string key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_entries.TryGetValue(key, out var entry) &&
                entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
            {
                Interlocked.Increment(ref _cacheHits);
                return ValueTask.FromResult<byte[]?>(entry.Value);
            }

            _entries.TryRemove(key, out _);
            return ValueTask.FromResult<byte[]?>(null);
        }

        public ValueTask SetAsync(
            string key,
            byte[] value,
            string[]? tags,
            TimeSpan validFor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _entries[key] = new CacheEntry(
                value,
                tags ?? [],
                DateTimeOffset.UtcNow.Add(validFor));

            return ValueTask.CompletedTask;
        }

        public ValueTask EvictByTagAsync(
            string tag,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var entry in _entries)
            {
                if (entry.Value.Tags.Contains(
                        tag,
                        StringComparer.Ordinal))
                {
                    _entries.TryRemove(entry.Key, out _);
                }
            }

            Interlocked.Increment(ref _tagEvictions);
            return ValueTask.CompletedTask;
        }

        private sealed record CacheEntry(
            byte[] Value,
            string[] Tags,
            DateTimeOffset ExpiresAtUtc);
    }
}
