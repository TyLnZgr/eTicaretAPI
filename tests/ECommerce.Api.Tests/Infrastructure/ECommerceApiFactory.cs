using System.Data.Common;
using ECommerce.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class ECommerceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public ECommerceApiFactory(string environment = "Testing")
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

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
        });

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
