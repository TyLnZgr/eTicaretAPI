using ECommerce.Api.Infrastructure.Outbox;
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
            "A processed outbox message cannot be changed.",
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
