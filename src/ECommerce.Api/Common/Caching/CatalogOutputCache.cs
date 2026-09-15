using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.Api.Common.Caching;

public sealed class CatalogOutputCache
{
    public const string ProductPolicyName = "CatalogProducts";
    public const string CategoryPolicyName = "CatalogCategories";

    private const string ProductTag = "catalog-products";
    private const string CategoryTag = "catalog-categories";

    private readonly IOutputCacheStore _cacheStore;

    public CatalogOutputCache(IOutputCacheStore cacheStore)
    {
        _cacheStore = cacheStore;
    }

    public ValueTask EvictProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return _cacheStore.EvictByTagAsync(
            ProductTag,
            cancellationToken);
    }

    public ValueTask EvictCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return _cacheStore.EvictByTagAsync(
            CategoryTag,
            cancellationToken);
    }

    public async ValueTask EvictCategoriesAndProductsAsync(
        CancellationToken cancellationToken = default)
    {
        await EvictCategoriesAsync(cancellationToken);
        await EvictProductsAsync(cancellationToken);
    }

    public static void Configure(OutputCacheOptions options)
    {
        options.AddPolicy(
            ProductPolicyName,
            policy => policy
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("*")
                .Tag(ProductTag));

        options.AddPolicy(
            CategoryPolicyName,
            policy => policy
                .Expire(TimeSpan.FromSeconds(30))
                .Tag(CategoryTag));
    }
}
