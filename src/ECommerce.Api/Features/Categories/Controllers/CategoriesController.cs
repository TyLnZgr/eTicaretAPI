using ECommerce.Api.Common.Caching;
using ECommerce.Api.Common.RateLimiting;
using ECommerce.Application.Categories.Dtos;
using ECommerce.Application.Categories.Mappings;
using ECommerce.Application.Categories.Outcomes;
using ECommerce.Application.Categories.Services;
using ECommerce.Application.Categories.Validation;
using ECommerce.Api.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Features.Categories.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly CatalogOutputCache _catalogOutputCache;

    public CategoriesController(
        ICategoryService categoryService,
        CatalogOutputCache catalogOutputCache)
    {
        _categoryService = categoryService;
        _catalogOutputCache = catalogOutputCache;
    }

    [HttpGet(Name = "GetCategories")]
    [OutputCache(PolicyName = CatalogOutputCache.CategoryPolicyName)]
    [EndpointSummary("List categories")]
    [ProducesResponseType<CategoryResponse[]>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoryResponse[]>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(
            cancellationToken);

        var response = categories
            .Select(category => category.ToResponse())
            .ToArray();

        return Ok(response);
    }

    [HttpGet("{id:int}", Name = "GetCategoryById")]
    [EndpointSummary("Get a category by ID")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<CategoryResponse>> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            return Problem(
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        return Ok(category.ToResponse());
    }

    [HttpPost(Name = "CreateCategory")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Create a category")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryResponse>> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = CategoryRequestValidator.ValidateName(request.Name);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var category = await _categoryService.CreateAsync(
            request.Name.Trim(),
            request.IsActive,
            cancellationToken);

        var response = category.ToResponse();
        await _catalogOutputCache.EvictCategoriesAsync(
            cancellationToken);

        return CreatedAtRoute(
            "GetCategoryById",
            new { id = category.Id },
            response);
    }

    [HttpPut("{id:int}", Name = "UpdateCategory")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Update a category")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryResponse>> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = CategoryRequestValidator.ValidateName(request.Name);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var category = await _categoryService.UpdateAsync(
            id,
            request.Name.Trim(),
            request.IsActive,
            cancellationToken);

        if (category is null)
        {
            return Problem(
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");
        }

        await _catalogOutputCache.EvictCategoriesAndProductsAsync(
            cancellationToken);

        return Ok(category.ToResponse());
    }

    [HttpDelete("{id:int}", Name = "DeleteCategory")]
    [EnableRateLimiting(ApiRateLimitPolicies.Mutation)]
    [Authorize(Policy = AppPolicies.ManageCatalog)]
    [EndpointSummary("Delete a category")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var status = await _categoryService.DeleteAsync(
            id,
            cancellationToken);

        if (status == CategoryDeleteStatus.Success)
        {
            await _catalogOutputCache.EvictCategoriesAsync(
                cancellationToken);
        }

        return status switch
        {
            CategoryDeleteStatus.Success => NoContent(),

            CategoryDeleteStatus.NotFound => Problem(
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"),

            CategoryDeleteStatus.HasProducts => Problem(
                detail: $"Category with ID {id} cannot be deleted because it has products.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict"),

            _ => Problem(
                detail: "The server could not complete the request.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error")
        };
    }
}
