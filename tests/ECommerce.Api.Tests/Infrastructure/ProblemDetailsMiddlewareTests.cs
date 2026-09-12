using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Tests.Infrastructure;

public sealed class ProblemDetailsMiddlewareTests
{
    [Fact]
    public async Task UnknownEndpoint_ReturnsProblemDetails()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response =
            await client.GetAsync("/api/not-found");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.False(string.IsNullOrWhiteSpace(problem.Type));
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }
}
