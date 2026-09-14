using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Notifications.Dtos;
using ECommerce.Api.Features.Orders.IntegrationEvents;
using ECommerce.Api.Features.Payments.Dtos;
using ECommerce.Api.Infrastructure.Outbox;
using ECommerce.Api.Models;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Api.Tests.Features.Notifications.Endpoints;

public sealed class NotificationEndpointTests
{
    public static TheoryData<string, string> ProtectedNotificationEndpoints =>
        new()
        {
            { "GET", "/api/notifications" },
            {
                "PATCH",
                $"/api/notifications/{Guid.NewGuid()}/read"
            }
        };

    [Theory]
    [MemberData(nameof(ProtectedNotificationEndpoints))]
    public async Task NotificationEndpoint_WithoutAuthentication_ReturnsUnauthorized(
        string method,
        string requestUri)
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.SendAsync(
            new HttpRequestMessage(
                new HttpMethod(method),
                requestUri));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OrderPaidEvent_WhenProcessed_CreatesOneReadableNotification()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        using var otherCustomerClient = factory.CreateCustomerClient();
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        using var paymentResponse = await SendPaymentAsync(
            client,
            orderId);

        paymentResponse.EnsureSuccessStatusCode();

        var processingResult = await ProcessOutboxAsync(factory);

        // Act
        using var listResponse = await client.GetAsync(
            "/api/notifications?unreadOnly=true&page=1&pageSize=10");

        // Assert
        Assert.Equal(1, processingResult.ProcessedCount);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var page = await listResponse.Content
            .ReadFromJsonAsync<PagedResult<NotificationResponse>>();

        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);

        var notification = Assert.Single(page.Items);
        Assert.Equal(orderId, notification.OrderId);
        Assert.Equal("OrderPaid", notification.Type);
        Assert.Equal("Payment received", notification.Title);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);

        using var forbiddenOwnershipResponse =
            await otherCustomerClient.PatchAsync(
                $"/api/notifications/{notification.Id}/read",
                content: null);

        await ProblemDetailsAssertions.AssertProblemAsync(
            forbiddenOwnershipResponse,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Notification with ID {notification.Id} was not found.");

        using var markReadResponse = await client.PatchAsync(
            $"/api/notifications/{notification.Id}/read",
            content: null);
        using var replayMarkReadResponse = await client.PatchAsync(
            $"/api/notifications/{notification.Id}/read",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            markReadResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            replayMarkReadResponse.StatusCode);

        using var unreadResponse = await client.GetAsync(
            "/api/notifications?unreadOnly=true");

        var unreadPage = await unreadResponse.Content
            .ReadFromJsonAsync<PagedResult<NotificationResponse>>();

        Assert.NotNull(unreadPage);
        Assert.Equal(0, unreadPage.TotalCount);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
            Assert.Equal(
                1,
                await dbContext.CustomerNotifications.CountAsync());

            var savedNotification = await dbContext
                .CustomerNotifications
                .AsNoTracking()
                .SingleAsync();

            Assert.True(savedNotification.IsRead);
            Assert.NotNull(savedNotification.ReadAtUtc);
        });
    }

    [Fact]
    public async Task Publisher_WhenSameMessageIsDispatchedAgain_DoesNotDuplicateSideEffects()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        using var paymentResponse = await SendPaymentAsync(
            client,
            orderId);

        paymentResponse.EnsureSuccessStatusCode();

        await ProcessOutboxAsync(factory);

        OutboxEnvelope? envelope = null;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var message = await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

            envelope = new OutboxEnvelope(
                message.Id,
                message.Type,
                message.Payload,
                message.OccurredAtUtc);
        });

        var publisher = factory.Services
            .GetRequiredService<IIntegrationEventPublisher>();

        // Act
        await publisher.PublishAsync(envelope!);
        await publisher.PublishAsync(envelope!);

        // Assert
        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
            Assert.Equal(
                1,
                await dbContext.CustomerNotifications.CountAsync());
        });
    }

    [Fact]
    public async Task OutboxProcessor_WhenEventDoesNotMatchPayment_SchedulesRetryWithoutSideEffects()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateClient();

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.OutboxMessages.Add(
                new OutboxMessage(
                    IntegrationEventTypes.OrderPaidV1,
                    "invalid-payment:succeeded",
                    JsonSerializer.Serialize(
                        new OrderPaidIntegrationEvent(
                            999,
                            Guid.NewGuid(),
                            100m,
                            "TRY",
                            DateTime.UtcNow),
                        JsonSerializerOptions.Web),
                    DateTime.UtcNow));

            await dbContext.SaveChangesAsync();
        });

        // Act
        var result = await ProcessOutboxAsync(factory);

        // Assert
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.ProcessedCount);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var message = await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(1, message.AttemptCount);
            Assert.NotNull(message.NextAttemptAtUtc);
            Assert.Contains(
                "does not match a succeeded customer payment",
                message.LastError);
            Assert.False(await dbContext.InboxMessages.AnyAsync());
            Assert.False(
                await dbContext.CustomerNotifications.AnyAsync());
        });
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidPagination_ReturnsValidationProblem()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.GetAsync(
            "/api/notifications?page=0&pageSize=101");

        // Assert
        await ProblemDetailsAssertions.AssertValidationErrorsAsync(
            response,
            new Dictionary<string, string>
            {
                ["page"] = "Page must be greater than zero.",
                ["pageSize"] =
                    "Page size must be between 1 and 100."
            });
    }

    private static async Task<int> SeedPendingOrderAsync(
        ECommerceApiFactory factory,
        Guid customerId)
    {
        var orderId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var email = $"{customerId:N}@example.com";

            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                email));

            var order = new Order
            {
                CustomerId = customerId,
                CustomerEmail = email,
                TotalAmount = 1000m,
                Currency = "TRY",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            orderId = order.Id;
        });

        return orderId;
    }

    private static async Task<HttpResponseMessage> SendPaymentAsync(
        HttpClient client,
        int orderId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/orders/{orderId}/payments");

        request.Headers.Add(
            "Idempotency-Key",
            $"notification-{Guid.NewGuid():N}");
        request.Content = JsonContent.Create(
            new ProcessPaymentRequest
            {
                PaymentMethodToken = "tok_success"
            });

        return await client.SendAsync(request);
    }

    private static async Task<OutboxProcessingResult> ProcessOutboxAsync(
        ECommerceApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IOutboxProcessor>();

        return await processor.ProcessPendingAsync();
    }
}
