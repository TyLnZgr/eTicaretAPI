using ECommerce.Api.Features.Categories.Outcomes;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Categories.Services;

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
