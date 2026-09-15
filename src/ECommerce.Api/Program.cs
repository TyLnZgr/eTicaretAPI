using ECommerce.Api.Common.Caching;
using ECommerce.Api.Common.Observability;
using ECommerce.Api.Identity.Authorization;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddOutputCache(CatalogOutputCache.Configure);
builder.Services.AddSingleton<CatalogOutputCache>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AppPolicies.ManageOrders,
        policy => policy.RequireRole(AppRoles.Administrator));

    options.AddPolicy(
        AppPolicies.ManageOperations,
        policy => policy.RequireRole(AppRoles.Administrator));

    options.AddPolicy(
        AppPolicies.ManageCatalog,
        policy => policy.RequireRole(AppRoles.Administrator));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var identityDataSeeder = scope.ServiceProvider
        .GetRequiredService<IdentityDataSeeder>();

    await identityDataSeeder.SeedAsync();
}

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = _ => false
        })
    .AllowAnonymous();

app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = healthCheck =>
                healthCheck.Tags.Contains("ready")
        })
    .AllowAnonymous();

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
