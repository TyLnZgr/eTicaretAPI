using ECommerce.Api.Contracts.Products;
using ECommerce.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProductService, InMemoryProductService>();

var app = builder.Build();

app.MapGet("/", () => new
{
    message = "ECommerce API is running."
});

app.MapGet("/api/products", (IProductService productService) =>
{
    return productService.GetAll();
});

app.MapGet("/api/products/{id:int}", (int id, IProductService productService) =>
{
    var product = productService.GetById(id);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Product with ID {id} was not found."
        });
    }

    return Results.Ok(product);
});

app.MapPost("/api/products", (
    CreateProductRequest request,
    IProductService productService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new
        {
            message = "Product name is required."
        });
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Product price must be greater than zero."
        });
    }

    if (request.StockQuantity < 0)
    {
        return Results.BadRequest(new
        {
            message = "Product stock quantity cannot be negative."
        });
    }

    var product = productService.Create(
        request.Name.Trim(),
        request.Price,
        request.StockQuantity,
        request.IsActive);

    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id:int}", (
    int id,
    UpdateProductRequest request,
    IProductService productService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new
        {
            message = "Product name is required."
        });
    }

    if (request.Price <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Product price must be greater than zero."
        });
    }

    if (request.StockQuantity < 0)
    {
        return Results.BadRequest(new
        {
            message = "Product stock quantity cannot be negative."
        });
    }

    var product = productService.Update(
        id,
        request.Name.Trim(),
        request.Price,
        request.StockQuantity,
        request.IsActive);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Product with ID {id} was not found."
        });
    }

    return Results.Ok(product);
});

app.MapDelete("/api/products/{id:int}", (
    int id,
    IProductService productService) =>
{
    var wasDeleted = productService.Delete(id);

    if (!wasDeleted)
    {
        return Results.NotFound(new
        {
            message = $"Product with ID {id} was not found."
        });
    }

    return Results.NoContent();
});

app.Run();
