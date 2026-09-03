using ECommerce.Api.Contracts.Products;
using ECommerce.Api.Data;
using ECommerce.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ECommerceDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'ECommerceDatabase' was not found.");

builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IProductService, EfCoreProductService>();

var app = builder.Build();

app.MapGet("/", () => new
{
    message = "ECommerce API is running."
});

app.MapGet("/api/products", async (
    IProductService productService,
    CancellationToken cancellationToken) =>
{
    var products = await productService.GetAllAsync(cancellationToken);

    return Results.Ok(products);
});

app.MapGet("/api/products/{id:int}", async (
    int id,
    IProductService productService,
    CancellationToken cancellationToken) =>
{
    var product = await productService.GetByIdAsync(id, cancellationToken);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Product with ID {id} was not found."
        });
    }

    return Results.Ok(product);
});

app.MapPost("/api/products", async (
    CreateProductRequest request,
    IProductService productService,
    CancellationToken cancellationToken) =>
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

    var product = await productService.CreateAsync(
        request.Name.Trim(),
        request.Price,
        request.StockQuantity,
        request.IsActive,
        cancellationToken);

    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id:int}", async (
    int id,
    UpdateProductRequest request,
    IProductService productService,
    CancellationToken cancellationToken) =>
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

    var product = await productService.UpdateAsync(
        id,
        request.Name.Trim(),
        request.Price,
        request.StockQuantity,
        request.IsActive,
        cancellationToken);

    if (product is null)
    {
        return Results.NotFound(new
        {
            message = $"Product with ID {id} was not found."
        });
    }

    return Results.Ok(product);
});

app.MapDelete("/api/products/{id:int}", async (
    int id,
    IProductService productService,
    CancellationToken cancellationToken) =>
{
    var wasDeleted = await productService.DeleteAsync(id, cancellationToken);

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
