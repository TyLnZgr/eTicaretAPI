using System.Data.Common;
using ECommerce.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class ECommerceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly bool _useTestAuthentication;

    public ECommerceApiFactory(
        string environment = "Testing",
        bool useTestAuthentication = true)
    {
        _environment = environment;
        _useTestAuthentication = useTestAuthentication;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Identity:BootstrapAdminEmail"] = string.Empty
                });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType ==
                    typeof(IDbContextOptionsConfiguration<ECommerceDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();

                return connection;
            });

            services.AddDbContext<ECommerceDbContext>(
                (serviceProvider, options) =>
                {
                    var connection = serviceProvider
                        .GetRequiredService<DbConnection>();

                    options.UseSqlite(connection);
                });

            if (_useTestAuthentication)
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme =
                            TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme =
                            TestAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme =
                            TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions,
                        TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
            }
        });

    }

    public HttpClient CreateCustomerClient(
        Guid? userId = null,
        string email = "customer@example.com")
    {
        return CreateAuthenticatedClient(
            userId ?? Guid.NewGuid(),
            email,
            isAdministrator: false);
    }

    public HttpClient CreateAdministratorClient(
        Guid? userId = null,
        string email = "admin@example.com")
    {
        return CreateAuthenticatedClient(
            userId ?? Guid.NewGuid(),
            email,
            isAdministrator: true);
    }

    private HttpClient CreateAuthenticatedClient(
        Guid userId,
        string email,
        bool isAdministrator)
    {
        var client = CreateClient();

        TestAuthenticationHandler.Authenticate(
            client,
            userId,
            email,
            isAdministrator);

        return client;
    }

    public async Task SeedDatabaseAsync(
        Func<ECommerceDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ECommerceDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        await seedAction(dbContext);
    }
}
