using System.Net;
using ECommerce.Api.Common.Observability;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class RequestCorrelationMiddlewareTests
{
    [Fact]
    public async Task Request_WithValidCorrelationId_EchoesItInResponse()
    {
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/not-found");
        const string correlationId = "client-request-001";
        request.Headers.Add(
            RequestCorrelationMiddleware.HeaderName,
            correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            correlationId,
            Assert.Single(response.Headers.GetValues(
                RequestCorrelationMiddleware.HeaderName)));
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_GeneratesOne()
    {
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();

        var correlationId = Assert.Single(
            response.Headers.GetValues(
                RequestCorrelationMiddleware.HeaderName));

        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(
            correlationId.Length <=
            RequestCorrelationMiddleware.MaximumLength);
    }

    [Fact]
    public async Task Request_WithUnsafeCorrelationId_ReplacesIt()
    {
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        const string unsafeCorrelationId = "contains whitespace";
        request.Headers.Add(
            RequestCorrelationMiddleware.HeaderName,
            unsafeCorrelationId);

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var correlationId = Assert.Single(
            response.Headers.GetValues(
                RequestCorrelationMiddleware.HeaderName));

        Assert.NotEqual(unsafeCorrelationId, correlationId);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }
}
