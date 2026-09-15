using ECommerce.Api.Common.Http;
using ECommerce.Application.Categories.Dtos;
using ECommerce.Application.Categories.Outcomes;
using ECommerce.Application.Categories.Services;
using ECommerce.Domain.Catalog;

namespace ECommerce.Api.Features.Categories.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("GetCategories")
            .WithSummary("List categories")
            .Produces<CategoryResponse[]>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetByIdAsync)
            .WithName("GetCategoryById")
            .WithSummary("Get a category by ID")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CreateCategory")
            .WithSummary("Create a category")
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", UpdateAsync)
            .WithName("UpdateCategory")
            .WithSummary("Update a category")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteAsync)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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
            return ApiProblemResults.NotFound(
                $"Category with ID {id} was not found.");
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
            return ApiProblemResults.NotFound(
                $"Category with ID {id} was not found.");
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
            return ApiProblemResults.NotFound(
                $"Category with ID {id} was not found.");
        }

        if (status == CategoryDeleteStatus.HasProducts)
        {
            return ApiProblemResults.Conflict(
                $"Category with ID {id} cannot be deleted because it has products.");
        }

        if (status == CategoryDeleteStatus.Success)
        {
            return Results.NoContent();
        }

        return ApiProblemResults.InternalServerError();
    }

    private static IResult? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ApiProblemResults.Validation(
                "name",
                "Category name is required.");
        }

        if (name.Trim().Length > 100)
        {
            return ApiProblemResults.Validation(
                "name",
                "Category name cannot exceed 100 characters.");
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
