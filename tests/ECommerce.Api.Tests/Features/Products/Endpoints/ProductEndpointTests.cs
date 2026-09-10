using System.Net;
using System.Net.Http.Json;
using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Infrastructure;

namespace ECommerce.Api.Tests.Features.Products.Endpoints;

public sealed class ProductEndpointTests
{
    [Theory]
    [InlineData(
        "/api/products?page=0",
        "Page must be greater than zero.")]
    [InlineData(
        "/api/products?pageSize=0",
        "Page size must be between 1 and 100.")]
    [InlineData(
        "/api/products?pageSize=101",
        "Page size must be between 1 and 100.")]
    [InlineData(
        "/api/products?categoryId=0",
        "Category ID must be greater than zero.")]
    [InlineData(
        "/api/products?minPrice=-1",
        "Minimum price cannot be negative.")]
    [InlineData(
        "/api/products?maxPrice=-1",
        "Maximum price cannot be negative.")]
    [InlineData(
        "/api/products?minPrice=200&maxPrice=100",
        "Minimum price cannot be greater than maximum price.")]
    [InlineData(
        "/api/products?sortBy=unknown",
        "Sort field must be id, name, price, or stockQuantity.")]
    [InlineData(
        "/api/products?sortDirection=sideways",
        "Sort direction must be asc or desc.")]
    public async Task GetAllAsync_WhenQueryIsInvalid_ReturnsBadRequest(
        string requestUri,
        string expectedMessage)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response =
            await client.GetAsync(requestUri);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(
            expectedMessage,
            error.Message);
    }

    [Fact]
    public async Task GetAllAsync_WhenRequestIsValid_ReturnsSortedAndPagedProducts()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Electronics",
                IsActive = true
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            dbContext.Products.AddRange(
                new Product
                {
                    Name = "Expensive Product",
                    Price = 3000m,
                    StockQuantity = 10,
                    IsActive = true,
                    CategoryId = category.Id
                },
                new Product
                {
                    Name = "Affordable Product",
                    Price = 1000m,
                    StockQuantity = 20,
                    IsActive = true,
                    CategoryId = category.Id
                },
                new Product
                {
                    Name = "Medium Product",
                    Price = 2000m,
                    StockQuantity = 15,
                    IsActive = true,
                    CategoryId = category.Id
                });

            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.GetAsync(
            "/api/products" +
            "?sortBy=price" +
            "&sortDirection=asc" +
            "&page=1" +
            "&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<ProductResponse>>();

        Assert.NotNull(result);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);

        Assert.Collection(
            result.Items,
            first =>
            {
                Assert.Equal("Affordable Product", first.Name);
                Assert.Equal(1000m, first.Price);
                Assert.Equal("Electronics", first.CategoryName);
            },
            second =>
            {
                Assert.Equal("Medium Product", second.Name);
                Assert.Equal(2000m, second.Price);
                Assert.Equal("Electronics", second.CategoryName);
            });
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        // Act
        using var response =
            await client.GetAsync("/api/products/999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(
            "Product with ID 999 was not found.",
            error.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ReturnsCreatedProduct()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Computer Accessories",
                IsActive = true
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();

            categoryId = category.Id;
        });

        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            Price = 2500m,
            StockQuantity = 12,
            CategoryId = categoryId,
            IsActive = true
        };

        // Act
        using var response =
            await client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.True(product.Id > 0);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(2500m, product.Price);
        Assert.Equal(12, product.StockQuantity);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal("Computer Accessories", product.CategoryName);
        Assert.True(product.IsActive);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/products/{product.Id}",
            response.Headers.Location.OriginalString);

        using var getResponse =
            await client.GetAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var savedProduct = await getResponse.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(savedProduct);
        Assert.Equal(product.Id, savedProduct.Id);
        Assert.Equal(product.Name, savedProduct.Name);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(
            _ => Task.CompletedTask);

        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            Price = 2500m,
            StockQuantity = 12,
            CategoryId = 999,
            IsActive = true
        };

        // Act
        using var response =
            await client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(
            "Category with ID 999 was not found.",
            error.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_ReturnsUpdatedProduct()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        var productId = 0;
        var categoryId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var category = new Category
            {
                Name = "Computer Accessories",
                IsActive = true
            };

            var product = new Product
            {
                Name = "Old Keyboard",
                Price = 1000m,
                StockQuantity = 4,
                IsActive = false,
                Category = category
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
            categoryId = category.Id;
        });

        var request = new UpdateProductRequest
        {
            Name = "Updated Keyboard",
            Price = 2750m,
            StockQuantity = 20,
            CategoryId = categoryId,
            IsActive = true
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            $"/api/products/{productId}",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(product);
        Assert.Equal(productId, product.Id);
        Assert.Equal("Updated Keyboard", product.Name);
        Assert.Equal(2750m, product.Price);
        Assert.Equal(20, product.StockQuantity);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal("Computer Accessories", product.CategoryName);
        Assert.True(product.IsActive);

        using var getResponse =
            await client.GetAsync($"/api/products/{productId}");

        var savedProduct = await getResponse.Content
            .ReadFromJsonAsync<ProductResponse>();

        Assert.NotNull(savedProduct);
        Assert.Equal("Updated Keyboard", savedProduct.Name);
        Assert.Equal(2750m, savedProduct.Price);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ReturnsNoContentAndRemovesProduct()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        var productId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var product = new Product
            {
                Name = "Product To Delete",
                Price = 500m,
                StockQuantity = 3,
                IsActive = true,
                Category = new Category
                {
                    Name = "Test Category",
                    IsActive = true
                }
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        });

        // Act
        using var response =
            await client.DeleteAsync($"/api/products/{productId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using var getResponse =
            await client.GetAsync($"/api/products/{productId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    private sealed record ErrorResponse(string Message);
}
