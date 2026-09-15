using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.IntegrationEvents;
using ECommerce.Application.Payments.Dtos;
using ECommerce.Infrastructure.Messaging.Outbox;
using ECommerce.Api.Tests.Common.Http;
using ECommerce.Api.Tests.Infrastructure;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Features.Payments.Endpoints;

public sealed class PaymentEndpointTests
{
    public static TheoryData<string, string> ProtectedPaymentEndpoints =>
        new()
        {
            { "POST", "/api/orders/1/payments" },
            {
                "GET",
                $"/api/orders/1/payments/{Guid.NewGuid()}"
            }
        };

    [Theory]
    [MemberData(nameof(ProtectedPaymentEndpoints))]
    public async Task PaymentEndpoint_WithoutAuthentication_ReturnsUnauthorized(
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
    public async Task ProcessAsync_WithSuccessfulToken_CreatesPaymentAndMarksOrderPaid()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(
            factory,
            customerId,
            totalAmount: 2500m);

        // Act
        using var response = await SendPaymentAsync(
            client,
            orderId,
            "payment-success-001",
            "tok_success");

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payment = await response.Content
            .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(payment);
        Assert.Equal(orderId, payment.OrderId);
        Assert.Equal(2500m, payment.Amount);
        Assert.Equal("TRY", payment.Currency);
        Assert.Equal("Succeeded", payment.Status);
        Assert.Equal("FakePay", payment.Provider);
        Assert.NotNull(payment.ProviderPaymentId);
        Assert.Null(payment.FailureCode);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal(
            $"/api/orders/{orderId}/payments/{payment.Id}",
            response.Headers.Location.AbsolutePath);

        using var getResponse = await client.GetAsync(
            response.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var responseJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "paymentMethodToken",
            responseJson,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "requestFingerprint",
            responseJson,
            StringComparison.OrdinalIgnoreCase);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var orderStatus = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.Id == orderId)
                .Select(order => order.Status)
                .SingleAsync();

            Assert.Equal(OrderStatus.Paid, orderStatus);

            var savedPayment = await dbContext.Payments
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(PaymentStatus.Succeeded, savedPayment.Status);
            Assert.Equal(64, savedPayment.RequestFingerprint.Length);
            Assert.DoesNotContain(
                "tok_success",
                savedPayment.RequestFingerprint,
                StringComparison.Ordinal);

            var outboxMessage = await dbContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(
                IntegrationEventTypes.OrderPaidV1,
                outboxMessage.Type);
            Assert.Equal(
                $"payment:{savedPayment.Id}:succeeded",
                outboxMessage.DeduplicationKey);
            Assert.Null(outboxMessage.ProcessedAtUtc);
            Assert.Equal(0, outboxMessage.AttemptCount);

            var orderPaidEvent = JsonSerializer
                .Deserialize<OrderPaidIntegrationEvent>(
                    outboxMessage.Payload,
                    JsonSerializerOptions.Web);

            Assert.NotNull(orderPaidEvent);
            Assert.Equal(orderId, orderPaidEvent.OrderId);
            Assert.Equal(savedPayment.Id, orderPaidEvent.PaymentId);
            Assert.Equal(2500m, orderPaidEvent.Amount);
            Assert.Equal("TRY", orderPaidEvent.Currency);
            Assert.Contains("\"orderId\"", outboxMessage.Payload);
            Assert.DoesNotContain(
                "tok_success",
                outboxMessage.Payload,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ProcessAsync_WithDeclinedToken_ReturnsPaymentRequiredAndRestoresPendingStatus()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        // Act
        using var response = await SendPaymentAsync(
            client,
            orderId,
            "payment-declined-001",
            "tok_declined");

        // Assert
        Assert.Equal(
            HttpStatusCode.PaymentRequired,
            response.StatusCode);

        var payment = await response.Content
            .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(payment);
        Assert.Equal("Failed", payment.Status);
        Assert.Equal(
            "payment_method_declined",
            payment.FailureCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var orderStatus = await dbContext.Orders
                .AsNoTracking()
                .Where(order => order.Id == orderId)
                .Select(order => order.Status)
                .SingleAsync();

            Assert.Equal(OrderStatus.Pending, orderStatus);
            Assert.Equal(
                PaymentStatus.Failed,
                await dbContext.Payments
                    .Select(candidate => candidate.Status)
                    .SingleAsync());
            Assert.False(
                await dbContext.OutboxMessages.AnyAsync());
        });
    }

    [Fact]
    public async Task ProcessAsync_WhenSameRequestIsRepeated_ReturnsSamePaymentOnlyOnce()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        // Act
        using var firstResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-replay-001",
            "tok_success");
        using var secondResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-replay-001",
            "tok_success");

        // Assert
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPayment = await firstResponse.Content
            .ReadFromJsonAsync<PaymentResponse>();
        var secondPayment = await secondResponse.Content
            .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(firstPayment);
        Assert.NotNull(secondPayment);
        Assert.Equal(firstPayment.Id, secondPayment.Id);
        Assert.Equal(
            firstPayment.ProviderPaymentId,
            secondPayment.ProviderPaymentId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.Payments.CountAsync());
            Assert.Equal(1, await dbContext.OutboxMessages.CountAsync());
            Assert.Equal(
                OrderStatus.Paid,
                await dbContext.Orders
                    .Select(order => order.Status)
                    .SingleAsync());
        });
    }

    [Fact]
    public async Task ProcessAsync_WhenKeyIsReusedWithDifferentToken_ReturnsConflict()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        using var firstResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-conflict-001",
            "tok_success");

        // Act
        using var secondResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-conflict-001",
            "tok_declined");

        // Assert
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        await ProblemDetailsAssertions.AssertProblemAsync(
            secondResponse,
            HttpStatusCode.Conflict,
            "Conflict",
            "The Idempotency-Key was already used with a different payment request.");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.Payments.CountAsync());
        });
    }

    [Fact]
    public async Task ProcessAsync_AfterDeclineWithNewKey_CanSucceed()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        using var declinedResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-retry-failed-001",
            "tok_declined");

        // Act
        using var successfulResponse = await SendPaymentAsync(
            client,
            orderId,
            "payment-retry-success-001",
            "tok_success");

        // Assert
        Assert.Equal(
            HttpStatusCode.PaymentRequired,
            declinedResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            successfulResponse.StatusCode);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var payments = await dbContext.Payments
                .AsNoTracking()
                .OrderBy(payment => payment.CreatedAtUtc)
                .ToArrayAsync();

            Assert.Equal(2, payments.Length);
            Assert.Equal(PaymentStatus.Failed, payments[0].Status);
            Assert.Equal(PaymentStatus.Succeeded, payments[1].Status);

            Assert.Equal(
                OrderStatus.Paid,
                await dbContext.Orders
                    .Select(order => order.Status)
                    .SingleAsync());
        });
    }

    [Fact]
    public async Task ProcessAsync_ForAnotherCustomersOrder_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(intruderId);
        var orderId = await SeedPendingOrderAsync(factory, ownerId);

        // Act
        using var response = await SendPaymentAsync(
            client,
            orderId,
            "payment-intruder-001",
            "tok_success");

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Order with ID {orderId} was not found.");

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.False(await dbContext.Payments.AnyAsync());
            Assert.Equal(
                OrderStatus.Pending,
                await dbContext.Orders
                    .Select(order => order.Status)
                    .SingleAsync());
        });
    }

    [Fact]
    public async Task GetByIdAsync_AsDifferentCustomer_ReturnsNotFound()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        using var ownerClient = factory.CreateCustomerClient(ownerId);
        using var intruderClient = factory.CreateCustomerClient(intruderId);
        var orderId = await SeedPendingOrderAsync(factory, ownerId);

        using var paymentResponse = await SendPaymentAsync(
            ownerClient,
            orderId,
            "payment-owned-001",
            "tok_success");

        var payment = await paymentResponse.Content
            .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(payment);

        // Act
        using var response = await intruderClient.GetAsync(
            $"/api/orders/{orderId}/payments/{payment.Id}");

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "Not Found",
            $"Payment with ID {payment.Id} was not found for order {orderId}.");
    }

    [Fact]
    public async Task ProcessAsync_WithoutIdempotencyKey_ReturnsValidationProblem()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();
        using var client = factory.CreateCustomerClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/orders/1/payments",
            new ProcessPaymentRequest
            {
                PaymentMethodToken = "tok_success"
            });

        // Assert
        await ProblemDetailsAssertions.AssertValidationAsync(
            response,
            "Idempotency-Key",
            "Idempotency-Key must be 8 to 100 characters and contain only letters, digits, '-', '_', '.', or ':'.");
    }

    [Fact]
    public async Task PaymentProcessingOrder_BlocksAnotherPaymentAndCancellation()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Payments.Add(new Payment(
                orderId,
                "existing-payment-001",
                new string('A', 64),
                1000m,
                "TRY",
                "FakePay",
                DateTime.UtcNow));

            await dbContext.SaveChangesAsync();

            await dbContext.Orders
                .Where(order => order.Id == orderId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        order => order.Status,
                        OrderStatus.PaymentProcessing));
        });

        // Act
        using var paymentResponse = await SendPaymentAsync(
            client,
            orderId,
            "another-payment-001",
            "tok_success");
        using var cancellationResponse = await client.PatchAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new UpdateOrderStatusRequest
            {
                Status = "Cancelled"
            });

        // Assert
        await ProblemDetailsAssertions.AssertProblemAsync(
            paymentResponse,
            HttpStatusCode.Conflict,
            "Conflict",
            "A payment is already being processed for this order.");

        await ProblemDetailsAssertions.AssertProblemAsync(
            cancellationResponse,
            HttpStatusCode.Conflict,
            "Conflict",
            "Order status cannot transition from PaymentProcessing to Cancelled.");
    }

    [Fact]
    public async Task ProcessAsync_WhenPreviousAttemptIsStillProcessing_RecoversWithSameKey()
    {
        // Arrange
        using var factory = new ECommerceApiFactory();

        var customerId = Guid.NewGuid();
        using var client = factory.CreateCustomerClient(customerId);
        var orderId = await SeedPendingOrderAsync(factory, customerId);

        const string idempotencyKey = "payment-recovery-001";
        const string paymentMethodToken = "tok_success";
        var paymentId = Guid.Empty;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            var payment = new Payment(
                orderId,
                idempotencyKey,
                CreateRequestFingerprint(
                    orderId,
                    1000m,
                    "TRY",
                    paymentMethodToken),
                1000m,
                "TRY",
                "FakePay",
                DateTime.UtcNow);

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            await dbContext.Orders
                .Where(order => order.Id == orderId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        order => order.Status,
                        OrderStatus.PaymentProcessing));

            paymentId = payment.Id;
        });

        // Act
        using var response = await SendPaymentAsync(
            client,
            orderId,
            idempotencyKey,
            paymentMethodToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paymentResponse = await response.Content
            .ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(paymentResponse);
        Assert.Equal(paymentId, paymentResponse.Id);
        Assert.Equal("Succeeded", paymentResponse.Status);

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            Assert.Equal(1, await dbContext.Payments.CountAsync());
            Assert.Equal(
                PaymentStatus.Succeeded,
                await dbContext.Payments
                    .Select(payment => payment.Status)
                    .SingleAsync());
            Assert.Equal(
                OrderStatus.Paid,
                await dbContext.Orders
                    .Select(order => order.Status)
                    .SingleAsync());
        });
    }

    private static async Task<int> SeedPendingOrderAsync(
        ECommerceApiFactory factory,
        Guid customerId,
        decimal totalAmount = 1000m)
    {
        var orderId = 0;

        await factory.SeedDatabaseAsync(async dbContext =>
        {
            dbContext.Users.Add(TestEntityFactory.CreateUser(
                customerId,
                $"{customerId:N}@example.com"));

            var order = new Order
            {
                CustomerId = customerId,
                CustomerEmail = $"{customerId:N}@example.com",
                TotalAmount = totalAmount,
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
        int orderId,
        string idempotencyKey,
        string paymentMethodToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/orders/{orderId}/payments");

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(
            new ProcessPaymentRequest
            {
                PaymentMethodToken = paymentMethodToken
            });

        return await client.SendAsync(request);
    }

    private static string CreateRequestFingerprint(
        int orderId,
        decimal amount,
        string currency,
        string paymentMethodToken)
    {
        var value = string.Join(
            '\n',
            orderId.ToString(CultureInfo.InvariantCulture),
            amount.ToString("G29", CultureInfo.InvariantCulture),
            currency,
            paymentMethodToken);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
