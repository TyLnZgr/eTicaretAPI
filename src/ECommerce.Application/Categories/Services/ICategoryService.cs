using ECommerce.Application.Categories.Outcomes;
using ECommerce.Domain.Catalog;

namespace ECommerce.Application.Categories.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Category> CreateAsync(
        string name,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<Category?> UpdateAsync(
        int id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<CategoryDeleteStatus> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
