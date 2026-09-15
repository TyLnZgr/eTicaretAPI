using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class RateLimitingEndpointTests
{
    private static readonly HashSet<string> SafeHttpMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethod.Get.Method,
            HttpMethod.Head.Method,
            HttpMethod.Options.Method
        };

    [Fact]
    public async Task AuthenticationPolicy_WhenLimitIsExceeded_ReturnsProblemDetails()
    {
        // Arrange
        using var factory = CreateFactory(
            authenticationPermitLimit: 2);
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(_ => Task.CompletedTask);

        // Act
        using var firstResponse = await SendInvalidLoginAsync(client);
        using var secondResponse = await SendInvalidLoginAsync(client);
        using var rejectedResponse = await SendInvalidLoginAsync(client);

        // Assert
        Assert.NotEqual(
            HttpStatusCode.TooManyRequests,
            firstResponse.StatusCode);
        Assert.NotEqual(
            HttpStatusCode.TooManyRequests,
            secondResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejectedResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejectedResponse.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(rejectedResponse.Headers.RetryAfter?.Delta);
        Assert.True(
            rejectedResponse.Headers.RetryAfter.Delta > TimeSpan.Zero);

        var problem = await rejectedResponse.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.TooManyRequests, problem.Status);
        Assert.Equal("Too Many Requests", problem.Title);
        Assert.Equal(
            "Too many requests were sent. Try again later.",
            problem.Detail);
        Assert.Equal(
            "https://www.rfc-editor.org/rfc/rfc6585#section-4",
            problem.Type);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.True(problem.Extensions.ContainsKey("retryAfterSeconds"));
    }

    [Fact]
    public async Task MutationPolicy_UsesSeparateBucketsForAuthenticatedUsers()
    {
        // Arrange
        using var factory = CreateFactory(mutationTokenLimit: 2);

        await factory.SeedDatabaseAsync(_ => Task.CompletedTask);

        using var firstUserClient = factory.CreateCustomerClient();
        using var secondUserClient = factory.CreateCustomerClient();
        var notificationId = Guid.NewGuid();

        // Act
        using var firstResponse = await MarkNotificationAsReadAsync(
            firstUserClient,
            notificationId);
        using var secondResponse = await MarkNotificationAsReadAsync(
            firstUserClient,
            notificationId);
        using var rejectedResponse = await MarkNotificationAsReadAsync(
            firstUserClient,
            notificationId);
        using var otherUserResponse = await MarkNotificationAsReadAsync(
            secondUserClient,
            notificationId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherUserResponse.StatusCode);
    }

    [Fact]
    public async Task GlobalPolicy_UsesSeparateBucketsForAuthenticatedUsers()
    {
        // Arrange
        using var factory = CreateFactory(globalPermitLimit: 1);
        using var firstUserClient = factory.CreateCustomerClient();
        using var secondUserClient = factory.CreateCustomerClient();

        // Act
        using var firstResponse = await firstUserClient.GetAsync("/");
        using var rejectedResponse = await firstUserClient.GetAsync("/");
        using var otherUserResponse = await secondUserClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherUserResponse.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_BypassTheGlobalLimiter()
    {
        // Arrange
        using var factory = CreateFactory(globalPermitLimit: 1);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync("/health/live");
        using var secondResponse = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public void ApiMutationEndpoints_HaveAnEndpointSpecificPolicy()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        var endpointDataSource = factory.Services
            .GetRequiredService<EndpointDataSource>();

        // Act
        var unprotectedRoutes = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?
                .StartsWith("api/", StringComparison.OrdinalIgnoreCase) ==
                true)
            .Where(endpoint => endpoint.Metadata
                .GetMetadata<HttpMethodMetadata>()?
                .HttpMethods
                .Any(method => !SafeHttpMethods.Contains(method)) == true)
            .Where(endpoint => endpoint.Metadata
                .GetMetadata<EnableRateLimitingAttribute>() is null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.True(
            unprotectedRoutes.Length == 0,
            $"Mutation endpoints without a named rate limit policy: {string.Join(", ", unprotectedRoutes)}");
    }

    [Fact]
    public void InvalidConfiguration_PreventsApplicationStartup()
    {
        // Arrange
        using var factory = new ECommerceApiFactory(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "0"
            });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(
            factory.CreateClient);

        // Assert
        Assert.Contains(
            "Rate limiting configuration is invalid.",
            exception.Failures);
    }

    private static ECommerceApiFactory CreateFactory(
        int globalPermitLimit = 100,
        int authenticationPermitLimit = 100,
        int mutationTokenLimit = 100)
    {
        return new ECommerceApiFactory(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] =
                    globalPermitLimit.ToString(),
                ["RateLimiting:Global:Window"] = "1.00:00:00",
                ["RateLimiting:Authentication:PermitLimit"] =
                    authenticationPermitLimit.ToString(),
                ["RateLimiting:Authentication:Window"] = "1.00:00:00",
                ["RateLimiting:Authentication:SegmentsPerWindow"] = "2",
                ["RateLimiting:Mutation:TokenLimit"] =
                    mutationTokenLimit.ToString(),
                ["RateLimiting:Mutation:TokensPerPeriod"] = "1",
                ["RateLimiting:Mutation:ReplenishmentPeriod"] =
                    "1.00:00:00"
            });
    }

    private static Task<HttpResponseMessage> SendInvalidLoginAsync(
        HttpClient client)
    {
        return client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "missing-user@example.com",
                password = "StrongPass1!"
            });
    }

    private static Task<HttpResponseMessage> MarkNotificationAsReadAsync(
        HttpClient client,
        Guid notificationId)
    {
        return client.PatchAsync(
            $"/api/notifications/{notificationId}/read",
            content: null);
    }
}
