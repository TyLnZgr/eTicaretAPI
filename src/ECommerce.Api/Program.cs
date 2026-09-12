using ECommerce.Api.Data;
using ECommerce.Api.Features.Categories.Endpoints;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Features.Products.Endpoints;
using ECommerce.Api.Features.Products.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ECommerceDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'ECommerceDatabase' was not found.");

builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IProductService, EfCoreProductService>();
builder.Services.AddScoped<ICategoryService, EfCoreCategoryService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("ECommerce API")
            .ShowOperationId()
            .DisableAgent();
    });
}

app.MapGet("/", () => new
{
    message = "ECommerce API is running."
});

app.MapProductEndpoints();
app.MapCategoryEndpoints();

app.Run();

public partial class Program;
