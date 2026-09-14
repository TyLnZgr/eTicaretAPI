using ECommerce.Api.Data;
using ECommerce.Api.Features.Addresses.Services;
using ECommerce.Api.Features.Carts.Services;
using ECommerce.Api.Features.Categories.Services;
using ECommerce.Api.Features.Orders.Services;
using ECommerce.Api.Features.Notifications.IntegrationEvents;
using ECommerce.Api.Features.Notifications.Services;
using ECommerce.Api.Features.Operations.Outbox.Services;
using ECommerce.Api.Features.Payments.Gateways;
using ECommerce.Api.Features.Payments.Services;
using ECommerce.Api.Features.Products.Services;
using ECommerce.Api.Identity;
using ECommerce.Api.Identity.Authorization;
using ECommerce.Api.Infrastructure.Messaging;
using ECommerce.Api.Infrastructure.Messaging.RabbitMq;
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
    .Validate(
        options => options.MaximumAttempts is >= 1 and <= 100,
        "Outbox maximum attempts must be between 1 and 100.")
    .Validate(
        options =>
            options.LeaseDuration >= TimeSpan.FromSeconds(10) &&
            options.LeaseDuration <= TimeSpan.FromMinutes(10),
        "Outbox lease duration must be between 10 seconds and 10 minutes.")
    .ValidateOnStart();
builder.Services
    .AddOptions<MessagingOptions>()
    .Bind(builder.Configuration.GetSection(MessagingOptions.SectionName))
    .Validate(
        options => MessagingProviders.IsSupported(options.Provider),
        "Messaging provider must be InProcess or RabbitMq.")
    .Validate(
        options =>
            !string.Equals(
                options.Provider,
                MessagingProviders.RabbitMq,
                StringComparison.OrdinalIgnoreCase) ||
            IsRabbitMqConfigurationValid(options.RabbitMq),
        "RabbitMQ configuration is invalid.")
    .ValidateOnStart();
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
builder.Services.AddScoped<INotificationService, EfCoreNotificationService>();
builder.Services.AddScoped<
    IOutboxAdministrationService,
    EfCoreOutboxAdministrationService>();
builder.Services.AddScoped<ICartService, EfCoreCartService>();
builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddScoped<
    IIntegrationEventHandler,
    OrderPaidNotificationHandler>();
builder.Services.AddSingleton<
    IIntegrationEventDispatcher,
    InProcessIntegrationEventDispatcher>();

var messagingProvider = builder.Configuration[
    $"{MessagingOptions.SectionName}:Provider"];

if (string.Equals(
        messagingProvider,
        MessagingProviders.RabbitMq,
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<
        IRabbitMqConnection,
        RabbitMqConnection>();
    builder.Services.AddSingleton<
        IIntegrationEventPublisher,
        RabbitMqIntegrationEventPublisher>();
    builder.Services.AddHostedService<
        RabbitMqConsumerBackgroundService>();
}
else
{
    builder.Services.AddSingleton<
        IIntegrationEventPublisher,
        InProcessIntegrationEventPublisher>();
}

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

static bool IsRabbitMqConfigurationValid(RabbitMqOptions options)
{
    return !string.IsNullOrWhiteSpace(options.HostName) &&
        options.Port is >= 1 and <= 65535 &&
        !string.IsNullOrWhiteSpace(options.UserName) &&
        !string.IsNullOrWhiteSpace(options.Password) &&
        !string.IsNullOrWhiteSpace(options.VirtualHost) &&
        !string.IsNullOrWhiteSpace(options.Exchange) &&
        !string.IsNullOrWhiteSpace(options.Queue) &&
        !string.IsNullOrWhiteSpace(options.DeadLetterExchange) &&
        !string.IsNullOrWhiteSpace(options.DeadLetterQueue) &&
        options.PrefetchCount > 0 &&
        options.InitialConnectionRetryDelay >= TimeSpan.FromSeconds(1);
}

public partial class Program;
