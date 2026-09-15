using ECommerce.Application.Addresses.Services;
using ECommerce.Application.Carts.Services;
using ECommerce.Application.Categories.Services;
using ECommerce.Application.Notifications.Services;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Payments.Gateways;
using ECommerce.Application.Payments.Services;
using ECommerce.Application.Products.Services;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Messaging.Handlers;
using ECommerce.Infrastructure.Messaging.Outbox;
using ECommerce.Infrastructure.Messaging.RabbitMq;
using ECommerce.Infrastructure.Operations.Outbox.Services;
using ECommerce.Infrastructure.Payments.Gateways;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Services.Addresses;
using ECommerce.Infrastructure.Persistence.Services.Carts;
using ECommerce.Infrastructure.Persistence.Services.Categories;
using ECommerce.Infrastructure.Persistence.Services.Notifications;
using ECommerce.Infrastructure.Persistence.Services.Orders;
using ECommerce.Infrastructure.Persistence.Services.Payments;
using ECommerce.Infrastructure.Persistence.Services.Products;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
                "ECommerceDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'ECommerceDatabase' was not found.");

        services.AddDbContext<ECommerceDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton(TimeProvider.System);

        AddOutboxOptions(services, configuration);
        AddMessagingOptions(services, configuration);
        AddIdentity(services);
        AddApplicationAdapters(services);
        AddMessaging(services, configuration);

        services.AddScoped<IdentityDataSeeder>();

        return services;
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ECommerceDbContext>();
    }

    private static void AddApplicationAdapters(
        IServiceCollection services)
    {
        services.AddScoped<IProductService, EfCoreProductService>();
        services.AddScoped<
            ICustomerAddressService,
            EfCoreCustomerAddressService>();
        services.AddScoped<ICategoryService, EfCoreCategoryService>();
        services.AddScoped<IOrderService, EfCoreOrderService>();
        services.AddScoped<
            IOrderPlacementService,
            EfCoreOrderPlacementService>();
        services.AddScoped<IPaymentService, EfCorePaymentService>();
        services.AddScoped<
            INotificationService,
            EfCoreNotificationService>();
        services.AddScoped<
            IOutboxAdministrationService,
            EfCoreOutboxAdministrationService>();
        services.AddScoped<ICartService, EfCoreCartService>();
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
    }

    private static void AddMessaging(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<
            IIntegrationEventHandler,
            OrderPaidNotificationHandler>();
        services.AddSingleton<
            IIntegrationEventDispatcher,
            InProcessIntegrationEventDispatcher>();

        var messagingProvider = configuration[
            $"{MessagingOptions.SectionName}:Provider"];

        if (string.Equals(
                messagingProvider,
                MessagingProviders.RabbitMq,
                StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddSingleton<
                IIntegrationEventPublisher,
                RabbitMqIntegrationEventPublisher>();
            services.AddHostedService<RabbitMqConsumerBackgroundService>();
        }
        else
        {
            services.AddSingleton<
                IIntegrationEventPublisher,
                InProcessIntegrationEventPublisher>();
        }

        services.AddScoped<IOutboxProcessor, EfCoreOutboxProcessor>();
        services.AddHostedService<OutboxBackgroundService>();
    }

    private static void AddOutboxOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .Validate(
                options => options.BatchSize is >= 1 and <= 500,
                "Outbox batch size must be between 1 and 500.")
            .Validate(
                options =>
                    options.PollingInterval >= TimeSpan.FromSeconds(1),
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
    }

    private static void AddMessagingOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
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
    }

    private static bool IsRabbitMqConfigurationValid(
        RabbitMqOptions options)
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
}
