using System.Net;
using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class HealthCheckEndpointTests
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthCheck_WhenApplicationIsHealthy_ReturnsHealthy(
        string requestUri)
    {
        using var factory = new ECommerceApiFactory(
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "text/plain",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "Healthy",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HealthChecks_WhenDatabaseIsUnavailable_OnlyReadinessFails()
    {
        using var baseFactory = new ECommerceApiFactory(
            useTestAuthentication: false);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbConnection>();
                services.AddSingleton<DbConnection>(_ =>
                    new SqliteConnection(
                        $"Data Source={Path.Combine(
                            Path.GetTempPath(),
                            $"missing-{Guid.NewGuid():N}.db")};" +
                        "Mode=ReadOnly"));
            }));
        using var client = factory.CreateClient();

        using var livenessResponse = await client.GetAsync(
            "/health/live");
        using var readinessResponse = await client.GetAsync(
            "/health/ready");

        Assert.Equal(HttpStatusCode.OK, livenessResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            readinessResponse.StatusCode);
        Assert.Equal(
            "Healthy",
            await livenessResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "Unhealthy",
            await readinessResponse.Content.ReadAsStringAsync());
    }
}
