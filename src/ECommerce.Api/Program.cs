using ECommerce.Api.Data;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Features.Orders.Services;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Identity;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ECommerceDbContext>();

builder.Services.AddScoped<IProductService, EfCoreProductService>();
builder.Services.AddScoped<ICategoryService, EfCoreCategoryService>();
builder.Services.AddScoped<IOrderService, EfCoreOrderService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGroup("/api/auth")
    .WithTags("Authentication")
    .MapIdentityApi<ApplicationUser>();

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

app.Run();

public partial class Program;
