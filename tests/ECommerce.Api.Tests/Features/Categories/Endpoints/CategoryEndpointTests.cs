using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Categories.Dtos;
using ECommerce.Domain.Catalog;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;

namespace ECommerce.Api.Tests.Features.Categories.Endpoints;

public sealed class CategoryEndpointTests
{
    public static TheoryData<string, string> InvalidCategoryNames => new()
    {
        { string.Empty, "Category name is required." },
        { "   ", "Category name is required." },
        { new string('A', 101), "Category name cannot exceed 100 characters." }
    };

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ReturnsCategoriesOrderedByName()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Categories.AddRange(
                new Category
                {
                    Name = "Office",
                    IsActive = true
                },
                new Category
                {
                    Name = "Electronics",
                    IsActive = false
                });

            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response =
            await client.GetAsync("/api/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content
            .ReadFromJsonAsync<CategoryResponse[]>();

        Assert.NotNull(categories);

        Assert.Collection(
            categories,
            first =>
            {
                Assert.Equal("Electronics", first.Name);
                Assert.False(first.IsActive);
            },
            second =>
            {
                Assert.Equal("Office", second.Name);
                Assert.True(second.IsActive);
            });
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ReturnsCreatedCategory()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        var request = new CreateCategoryRequest
        {
            Name = "  Computer Accessories  ",
            IsActive = true
        };

        // Act
        using var response =
            await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var category = await response.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(category);
        Assert.True(category.Id > 0);
        Assert.Equal("Computer Accessories", category.Name);
        Assert.True(category.IsActive);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/categories/{category.Id}",
            response.Headers.Location.AbsolutePath);

        using var getResponse =
            await client.GetAsync($"/api/categories/{category.Id}");

        var savedCategory = await getResponse.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(savedCategory);
        Assert.Equal(category, savedCategory);
    }

    [Theory]
    [MemberData(nameof(InvalidCategoryNames))]
    public async Task CreateAsync_WhenNameIsInvalid_ReturnsBadRequest(
        string name,
        string expectedMessage)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var request = new CreateCategoryRequest
        {
            Name = name,
            IsActive = true
        };

        // Act
        using var response =
            await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        await ProblemDetailsAssertions.AssertValidationAsync(
            response,
            "name",
            expectedMessage);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryExists_ReturnsUpdatedCategory()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Old Category",
                IsActive = false
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            categoryId = category.Id;
        });

        var request = new UpdateCategoryRequest
        {
            Name = "  Updated Category  ",
            IsActive = true
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/categories/{categoryId}",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var category = await response.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(category);
        Assert.Equal(categoryId, category.Id);
        Assert.Equal("Updated Category", category.Name);
        Assert.True(category.IsActive);

        using var getResponse =
            await client.GetAsync($"/api/categories/{categoryId}");

        var savedCategory = await getResponse.Content
            .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(savedCategory);
        Assert.Equal(category, savedCategory);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasNoProducts_ReturnsNoContent()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Empty Category",
                IsActive = true
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            categoryId = category.Id;
        });

        // Act
        using var response =
            await client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using var getResponse =
            await client.GetAsync($"/api/categories/{categoryId}");

        await ProblemDetailsAssertions.AssertProblemAsync(
            getResponse,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Category with ID {categoryId} was not found.");
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasProducts_ReturnsConflict()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Category With Product",
                IsActive = true
            };

            dbContext.Products.Add(new Product
            {
                Name = "Test Product",
                Price = 100m,
                StockQuantity = 5,
                IsActive = true,
                Category = category
            });

            await dbContext.SaveChangesAsync();

            categoryId = category.Id;
        });

        // Act
        using var response =
            await client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            $"Category with ID {categoryId} cannot be deleted because it has products.");

        using var getResponse =
            await client.GetAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
