using ECommerce.Api.Data;
using ECommerce.Api.Features.Categories.Outcomes;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Categories.Services;

public sealed class EfCoreCategoryService : ICategoryService
{
    private readonly ECommerceDbContext _dbContext;

    public EfCoreCategoryService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);
    }
    public async Task<Category> CreateAsync(
    string name,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            Name = name,
            IsActive = isActive
        };

        _dbContext.Categories.Add(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<Category?> UpdateAsync(
        int id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.Name = name;
        category.IsActive = isActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<CategoryDeleteStatus> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        if (category is null)
        {
            return CategoryDeleteStatus.NotFound;
        }

        var hasProducts = await _dbContext.Products
            .AnyAsync(
                product => product.CategoryId == id,
                cancellationToken);

        if (hasProducts)
        {
            return CategoryDeleteStatus.HasProducts;
        }

        _dbContext.Categories.Remove(category);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryDeleteStatus.Success;
    }
}
