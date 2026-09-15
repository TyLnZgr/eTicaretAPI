using ECommerce.Application.Orders.IntegrationEvents;
using ECommerce.Infrastructure.Messaging.Outbox;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Tests.Infrastructure.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void MarkProcessed_AfterMessageWasProcessed_RejectsSecondChange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = CreateMessage(now);

        message.MarkProcessed(now);

        // Act
        var markAgainAction = () => message.MarkFailed(
            "broker_unavailable",
            now,
            TimeSpan.FromSeconds(2));

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(
            markAgainAction);

        Assert.Equal(
            "A finalized outbox message cannot be changed.",
            exception.Message);
        Assert.Equal(1, message.AttemptCount);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDuplicateDeduplicationKey_IsRejected()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = DateTime.UtcNow;

        database.DbContext.OutboxMessages.Add(
            CreateMessage(now, "payment:1:succeeded"));
        await database.DbContext.SaveChangesAsync();

        database.DbContext.OutboxMessages.Add(
            CreateMessage(now, "payment:1:succeeded"));

        // Act
        var saveAction = () => database.DbContext.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(saveAction);
    }

    [Fact]
    public void Retry_WhenMessageIsDeadLettered_ResetsDeliveryState()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var message = CreateMessage(now);

        message.MarkDeadLettered(
            "The broker is unavailable.",
            now);

        // Act
        message.Retry(now.AddMinutes(1));

        // Assert
        Assert.Equal(0, message.AttemptCount);
        Assert.Null(message.DeadLetteredAtUtc);
        Assert.Null(message.LastError);
        Assert.Equal(now.AddMinutes(1), message.NextAttemptAtUtc);
    }

    private static OutboxMessage CreateMessage(
        DateTime occurredAtUtc,
        string deduplicationKey = "payment:1:succeeded")
    {
        return new OutboxMessage(
            IntegrationEventTypes.OrderPaidV1,
            deduplicationKey,
            "{\"orderId\":1}",
            occurredAtUtc);
    }
}
