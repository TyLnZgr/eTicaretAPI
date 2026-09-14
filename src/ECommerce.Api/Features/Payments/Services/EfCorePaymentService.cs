using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ECommerce.Api.Data;
using ECommerce.Api.Features.Orders.IntegrationEvents;
using ECommerce.Api.Features.Payments.Gateways;
using ECommerce.Api.Features.Payments.Outcomes;
using ECommerce.Api.Infrastructure.Outbox;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Payments.Services;

public sealed class EfCorePaymentService : IPaymentService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IPaymentGateway _paymentGateway;
    private readonly TimeProvider _timeProvider;

    public EfCorePaymentService(
        ECommerceDbContext dbContext,
        IPaymentGateway paymentGateway,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _paymentGateway = paymentGateway;
        _timeProvider = timeProvider;
    }

    public async Task<Payment?> GetByIdAsync(
        int orderId,
        Guid paymentId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Where(payment => payment.OrderId == orderId)
            .Where(payment => payment.Order.CustomerId == customerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentProcessingResult> ProcessAsync(
        int orderId,
        Guid customerId,
        string idempotencyKey,
        string paymentMethodToken,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0 ||
            customerId == Guid.Empty ||
            string.IsNullOrWhiteSpace(idempotencyKey) ||
            string.IsNullOrWhiteSpace(paymentMethodToken))
        {
            return new PaymentProcessingResult(
                PaymentProcessingStatus.InvalidRequest);
        }

        var normalizedIdempotencyKey = idempotencyKey.Trim();

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == orderId &&
                candidate.CustomerId == customerId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.TotalAmount,
                candidate.Currency,
                candidate.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return new PaymentProcessingResult(
                PaymentProcessingStatus.OrderNotFound);
        }

        var requestFingerprint = CreateRequestFingerprint(
            order.Id,
            order.TotalAmount,
            order.Currency,
            paymentMethodToken);

        var payment = await _dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrderId == orderId &&
                    candidate.IdempotencyKey == normalizedIdempotencyKey,
                cancellationToken);

        var wasReplay = payment is not null;

        if (payment is not null)
        {
            if (payment.RequestFingerprint != requestFingerprint)
            {
                return new PaymentProcessingResult(
                    PaymentProcessingStatus.IdempotencyConflict);
            }

            if (payment.Status != PaymentStatus.Processing)
            {
                return ToFinalResult(payment, wasReplay: true);
            }

            if (order.Status != OrderStatus.PaymentProcessing)
            {
                return new PaymentProcessingResult(
                    PaymentProcessingStatus.ConcurrencyConflict,
                    payment,
                    WasReplay: true);
            }
        }
        else
        {
            if (order.Status == OrderStatus.PaymentProcessing)
            {
                return new PaymentProcessingResult(
                    PaymentProcessingStatus.PaymentInProgress);
            }

            if (order.Status != OrderStatus.Pending)
            {
                return new PaymentProcessingResult(
                    PaymentProcessingStatus.OrderNotPayable);
            }

            payment = new Payment(
                order.Id,
                normalizedIdempotencyKey,
                requestFingerprint,
                order.TotalAmount,
                order.Currency,
                _paymentGateway.Name,
                _timeProvider.GetUtcNow().UtcDateTime);

            var acquireResult = await AcquirePaymentProcessingAsync(
                order.Id,
                customerId,
                payment,
                cancellationToken);

            if (acquireResult.HasValue)
            {
                return new PaymentProcessingResult(acquireResult.Value);
            }
        }

        var gatewayResult = await _paymentGateway.ChargeAsync(
            new PaymentGatewayRequest(
                payment.Id,
                payment.Amount,
                payment.Currency,
                paymentMethodToken,
                $"order:{order.Id}:{normalizedIdempotencyKey}"),
            cancellationToken);

        return await FinalizePaymentAsync(
            payment.Id,
            gatewayResult,
            wasReplay,
            cancellationToken);
    }

    private async Task<PaymentProcessingStatus?>
        AcquirePaymentProcessingAsync(
            int orderId,
            Guid customerId,
            Payment payment,
            CancellationToken cancellationToken)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var affectedOrders = await _dbContext.Orders
            .Where(order =>
                order.Id == orderId &&
                order.CustomerId == customerId &&
                order.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    order => order.Status,
                    OrderStatus.PaymentProcessing),
                cancellationToken);

        if (affectedOrders == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            var currentStatus = await _dbContext.Orders
                .AsNoTracking()
                .Where(order =>
                    order.Id == orderId &&
                    order.CustomerId == customerId)
                .Select(order => (OrderStatus?)order.Status)
                .SingleOrDefaultAsync(cancellationToken);

            return currentStatus == OrderStatus.PaymentProcessing
                ? PaymentProcessingStatus.PaymentInProgress
                : PaymentProcessingStatus.OrderNotPayable;
        }

        _dbContext.Payments.Add(payment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            return PaymentProcessingStatus.PaymentInProgress;
        }
    }

    private async Task<PaymentProcessingResult> FinalizePaymentAsync(
        Guid paymentId,
        PaymentGatewayResult gatewayResult,
        bool wasReplay,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var payment = await _dbContext.Payments
            .SingleAsync(
                candidate => candidate.Id == paymentId,
                cancellationToken);

        if (payment.Status != PaymentStatus.Processing)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ToFinalResult(payment, wasReplay: true);
        }

        var targetOrderStatus =
            gatewayResult.Status == PaymentGatewayStatus.Succeeded
                ? OrderStatus.Paid
                : OrderStatus.Pending;

        var affectedOrders = await _dbContext.Orders
            .Where(order =>
                order.Id == payment.OrderId &&
                order.Status == OrderStatus.PaymentProcessing)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    order => order.Status,
                    targetOrderStatus),
                cancellationToken);

        if (affectedOrders == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new PaymentProcessingResult(
                PaymentProcessingStatus.ConcurrencyConflict,
                payment,
                WasReplay: wasReplay);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (gatewayResult.Status == PaymentGatewayStatus.Succeeded)
        {
            payment.MarkSucceeded(
                gatewayResult.ProviderPaymentId
                    ?? throw new InvalidOperationException(
                        "The payment gateway did not return a payment ID."),
                now);

            var orderPaidEvent = new OrderPaidIntegrationEvent(
                payment.OrderId,
                payment.Id,
                payment.Amount,
                payment.Currency,
                now);

            _dbContext.OutboxMessages.Add(
                new OutboxMessage(
                    IntegrationEventTypes.OrderPaidV1,
                    $"payment:{payment.Id}:succeeded",
                    JsonSerializer.Serialize(
                        orderPaidEvent,
                        JsonSerializerOptions.Web),
                    now));
        }
        else
        {
            payment.MarkFailed(
                gatewayResult.FailureCode ?? "payment_declined",
                gatewayResult.ProviderPaymentId,
                now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToFinalResult(payment, wasReplay);
    }

    private static PaymentProcessingResult ToFinalResult(
        Payment payment,
        bool wasReplay)
    {
        return new PaymentProcessingResult(
            payment.Status == PaymentStatus.Succeeded
                ? PaymentProcessingStatus.Success
                : PaymentProcessingStatus.PaymentDeclined,
            payment,
            WasReplay: wasReplay);
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
