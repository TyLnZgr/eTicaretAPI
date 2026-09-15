using System.Net;
using System.Text.Json;
using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Orders.IntegrationEvents;
using ECommerce.Api.Features.Operations.Outbox.Dtos;
using ECommerce.Api.Infrastructure.Outbox;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Operations.Outbox.Endpoints;

public sealed class OutboxAdminEndpointTests
{
    [Fact]
    public async Task GetAllAsync_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/admin/outbox");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_AsCustomer_ReturnsForbidden()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.GetAsync("/api/admin/outbox");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAsync_AsAdministrator_FiltersMetadataWithoutPayload()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var deadLetteredMessage = new OutboxMessage(
            IntegrationEventTypes.OrderPaidV1,
            "payment:dead-letter:succeeded",
            "{\"secret\":\"must-not-be-returned\"}",
            DateTime.UtcNow);

        deadLetteredMessage.MarkDeadLettered(
            "The broker is unavailable.",
            DateTime.UtcNow);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.OutboxMessages.Add(deadLetteredMessage);
            dbContext.OutboxMessages.Add(
                new OutboxMessage(
                    IntegrationEventTypes.OrderPaidV1,
                    "payment:pending:succeeded",
                    "{\"orderId\":2}",
                    DateTime.UtcNow));

            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.GetAsync(
            "/api/admin/outbox?status=DeadLettered&page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<
            PagedResult<OutboxMessageResponse>>(
                json,
                JsonSerializerOptions.Web);

        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);

        var message = Assert.Single(page.Items);
        Assert.Equal(deadLetteredMessage.Id, message.Id);
        Assert.Equal("DeadLettered", message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(
            "The broker is unavailable.",
            message.LastError);

        Assert.DoesNotContain(
            "must-not-be-returned",
            json,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "payload",
            json,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RetryAsync_WithDeadLetteredMessage_MakesItPendingAgain()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var message = new OutboxMessage(
            IntegrationEventTypes.OrderPaidV1,
            "payment:manual-retry:succeeded",
            "{\"orderId\":1}",
            DateTime.UtcNow);

        message.MarkDeadLettered(
            "The broker is unavailable.",
            DateTime.UtcNow);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.PostAsync(
            $"/api/admin/outbox/{message.Id}/retry",
            content: null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var savedMessage = await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(0, savedMessage.AttemptCount);
            Assert.Null(savedMessage.DeadLetteredAtUtc);
            Assert.Null(savedMessage.LastError);
            Assert.NotNull(savedMessage.NextAttemptAtUtc);
        });
    }

    [Fact]
    public async Task RetryAsync_WithPendingMessage_ReturnsConflict()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        var message = new OutboxMessage(
            IntegrationEventTypes.OrderPaidV1,
            "payment:not-dead-letter:succeeded",
            "{\"orderId\":1}",
            DateTime.UtcNow);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync();
        });

        // Act
        using var response = await client.PostAsync(
            $"/api/admin/outbox/{message.Id}/retry",
            content: null);

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "Conflict",
            "Only a dead-lettered Outbox message can be retried manually.");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidQuery_ReturnsValidationProblem()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateAdministratorClient();

        // Act
        using var response = await client.GetAsync(
            "/api/admin/outbox?status=1&page=0&pageSize=101");

        // Assert
        await ProblemDetailsAssertions.AssertValidationErrorsAsync(
            response,
            new Dictionary<string, string>
            {
                ["status"] =
                    "Status must be Pending, Retrying, Processing, Processed, or DeadLettered.",
                ["page"] = "Page must be greater than zero.",
                ["pageSize"] =
                    "Page size must be between 1 and 100."
            });
    }
}
