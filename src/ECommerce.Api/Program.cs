using ECommerce.Api.Data;
using ECommerce.Api.Features.Addresses.Services;
using ECommerce.Api.Features.Carts.Services;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Features.Orders.Services;
using ECommerce.Api.Features.Payments.Gateways;
using ECommerce.Api.Features.Payments.Services;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Identity;
using ECommerce.Api.Identity.Authorization;
using ECommerce.Api.Infrastructure.Outbox;
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
builder.Services
    .AddOptions<OutboxOptions>()
    .Bind(builder.Configuration.GetSection(OutboxOptions.SectionName))
    .Validate(
        options => options.BatchSize is >= 1 and <= 500,
        "Outbox batch size must be between 1 and 500.")
    .Validate(
        options => options.PollingInterval >= TimeSpan.FromSeconds(1),
        "Outbox polling interval must be at least one second.")
    .ValidateOnStart();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AppPolicies.ManageOrders,
        policy => policy.RequireRole(AppRoles.Administrator));

    options.AddPolicy(
        AppPolicies.ManageCatalog,
        policy => policy.RequireRole(AppRoles.Administrator));
});

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
builder.Services.AddScoped<ICustomerAddressService, EfCoreCustomerAddressService>();
builder.Services.AddScoped<ICategoryService, EfCoreCategoryService>();
builder.Services.AddScoped<IOrderService, EfCoreOrderService>();
builder.Services.AddScoped<IOrderPlacementService, EfCoreOrderPlacementService>();
builder.Services.AddScoped<IPaymentService, EfCorePaymentService>();
builder.Services.AddScoped<ICartService, EfCoreCartService>();
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<
    IIntegrationEventPublisher,
    LoggingIntegrationEventPublisher>();
builder.Services.AddScoped<IOutboxProcessor, EfCoreOutboxProcessor>();
builder.Services.AddHostedService<OutboxBackgroundService>();
builder.Services.AddScoped<IdentityDataSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var identityDataSeeder = scope.ServiceProvider
        .GetRequiredService<IdentityDataSeeder>();

    await identityDataSeeder.SeedAsync();
}

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
