using ECommerce.Api.Data;
using ECommerce.Api.Features.Categories.Endpoints;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Features.Products.Endpoints;
using ECommerce.Api.Features.Products.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ECommerceDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'ECommerceDatabase' was not found.");

builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IProductService, EfCoreProductService>();
builder.Services.AddScoped<ICategoryService, EfCoreCategoryService>();

var app = builder.Build();

app.MapGet("/", () => new
{
    message = "ECommerce API is running."
});

app.MapProductEndpoints();
app.MapCategoryEndpoints();

app.Run();
