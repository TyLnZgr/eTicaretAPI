using ECommerce.Api.Features.Categories.Dtos;
using ECommerce.Api.Features.Categories.Outcomes;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Categories.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("GetCategories");

        group.MapGet("/{id:int}", GetByIdAsync)
            .WithName("GetCategoryById");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CreateCategory");

        group.MapPut("/{id:int}", UpdateAsync)
            .WithName("UpdateCategory");

        group.MapDelete("/{id:int}", DeleteAsync)
            .WithName("DeleteCategory");

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        var response = categories
            .Select(ToResponse)
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            return Results.NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        return Results.Ok(ToResponse(category));
    }

    private static async Task<IResult> CreateAsync(
        CreateCategoryRequest request,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateName(request.Name);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var category = await categoryService.CreateAsync(
            request.Name.Trim(),
            request.IsActive,
            cancellationToken);

        return Results.Created(
            $"/api/categories/{category.Id}",
            ToResponse(category));
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateName(request.Name);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var category = await categoryService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.IsActive,
            cancellationToken);

        if (category is null)
        {
            return Results.NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        return Results.Ok(ToResponse(category));
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var status = await categoryService.DeleteAsync(
            id,
            cancellationToken);

        if (status == CategoryDeleteStatus.NotFound)
        {
            return Results.NotFound(new
            {
                message = $"Category with ID {id} was not found."
            });
        }

        if (status == CategoryDeleteStatus.HasProducts)
        {
            return Results.Conflict(new
            {
                message = $"Category with ID {id} cannot be deleted because it has products."
            });
        }

        if (status == CategoryDeleteStatus.Success)
        {
            return Results.NoContent();
        }

        return Results.Problem(
            "Category deletion returned an unexpected result.");
    }

    private static IResult? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new
            {
                message = "Category name is required."
            });
        }

        if (name.Trim().Length > 100)
        {
            return Results.BadRequest(new
            {
                message = "Category name cannot exceed 100 characters."
            });
        }

        return null;
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.IsActive);
    }
}
