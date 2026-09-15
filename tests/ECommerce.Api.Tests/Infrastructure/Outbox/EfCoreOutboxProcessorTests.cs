using ECommerce.Application.Orders.IntegrationEvents;
using ECommerce.Infrastructure.Messaging.Outbox;
using ECommerce.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Tests.Infrastructure.Outbox;

public sealed class EfCoreOutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenPublishingSucceeds_MarksMessageProcessed()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);
        var message = CreateMessage(now.UtcDateTime);

        database.DbContext.OutboxMessages.Add(message);
        await database.DbContext.SaveChangesAsync();

        var publisher = new RecordingIntegrationEventPublisher();
        var processor = CreateProcessor(database, publisher, now);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(1, result.SelectedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.DeadLetteredCount);
        Assert.Equal(0, result.LeaseLostCount);

        var publishedMessage = Assert.Single(publisher.Messages);
        Assert.Equal(message.Id, publishedMessage.MessageId);
        Assert.Equal(message.Type, publishedMessage.Type);
        Assert.Equal(message.Payload, publishedMessage.Payload);

        database.DbContext.ChangeTracker.Clear();

        var savedMessage = await database.DbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(now.UtcDateTime, savedMessage.ProcessedAtUtc);
        Assert.Equal(1, savedMessage.AttemptCount);
        Assert.Null(savedMessage.NextAttemptAtUtc);
        Assert.Null(savedMessage.LastError);
        Assert.Null(savedMessage.LockId);
        Assert.Null(savedMessage.LockedBy);
        Assert.Null(savedMessage.LockedUntilUtc);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenPublishingFails_SchedulesRetry()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);

        database.DbContext.OutboxMessages.Add(
            CreateMessage(now.UtcDateTime));
        await database.DbContext.SaveChangesAsync();

        var publisher = new RecordingIntegrationEventPublisher(
            shouldFail: true);
        var processor = CreateProcessor(database, publisher, now);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(1, result.SelectedCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.DeadLetteredCount);
        Assert.Equal(0, result.LeaseLostCount);
        Assert.Empty(publisher.Messages);

        database.DbContext.ChangeTracker.Clear();

        var savedMessage = await database.DbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        Assert.Null(savedMessage.ProcessedAtUtc);
        Assert.Equal(1, savedMessage.AttemptCount);
        Assert.Equal(
            now.UtcDateTime.AddSeconds(2),
            savedMessage.NextAttemptAtUtc);
        Assert.Equal("The broker is unavailable.", savedMessage.LastError);
        Assert.Null(savedMessage.LockId);
        Assert.Null(savedMessage.LockedBy);
        Assert.Null(savedMessage.LockedUntilUtc);
    }

    [Fact]
    public async Task ProcessPendingAsync_BeforeRetryTime_DoesNotSelectMessage()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);
        var message = CreateMessage(now.UtcDateTime);

        message.MarkFailed(
            "The broker is unavailable.",
            now.UtcDateTime,
            TimeSpan.FromMinutes(1));

        database.DbContext.OutboxMessages.Add(message);
        await database.DbContext.SaveChangesAsync();

        var publisher = new RecordingIntegrationEventPublisher();
        var processor = CreateProcessor(database, publisher, now);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(0, result.SelectedCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.DeadLetteredCount);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task ProcessPendingAsync_WithActiveLease_DoesNotSelectMessage()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);

        database.DbContext.OutboxMessages.Add(
            CreateMessage(now.UtcDateTime));
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        await database.DbContext.OutboxMessages.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(message => message.LockId, Guid.NewGuid())
                .SetProperty(message => message.LockedBy, "worker-one")
                .SetProperty(
                    message => message.LockedUntilUtc,
                    now.UtcDateTime.AddMinutes(1)));

        var publisher = new RecordingIntegrationEventPublisher();
        var processor = CreateProcessor(database, publisher, now);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(0, result.SelectedCount);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task ProcessPendingAsync_WithExpiredLease_ReclaimsAndProcessesMessage()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);

        database.DbContext.OutboxMessages.Add(
            CreateMessage(now.UtcDateTime));
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        await database.DbContext.OutboxMessages.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(message => message.LockId, Guid.NewGuid())
                .SetProperty(message => message.LockedBy, "crashed-worker")
                .SetProperty(
                    message => message.LockedUntilUtc,
                    now.UtcDateTime.AddSeconds(-1)));

        var publisher = new RecordingIntegrationEventPublisher();
        var processor = CreateProcessor(database, publisher, now);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(1, result.SelectedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Single(publisher.Messages);

        database.DbContext.ChangeTracker.Clear();

        var savedMessage = await database.DbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        Assert.NotNull(savedMessage.ProcessedAtUtc);
        Assert.Null(savedMessage.LockId);
    }

    [Fact]
    public async Task ProcessPendingAsync_AtMaximumAttempts_MovesMessageToDeadLetter()
    {
        // Arrange
        await using var database =
            await SqliteTestDatabase.CreateAsync();

        var now = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);
        var message = CreateMessage(now.UtcDateTime);

        message.MarkFailed(
            "First failure",
            now.UtcDateTime.AddMinutes(-2),
            TimeSpan.FromSeconds(2));
        message.MarkFailed(
            "Second failure",
            now.UtcDateTime.AddMinutes(-1),
            TimeSpan.FromSeconds(4));

        database.DbContext.OutboxMessages.Add(message);
        await database.DbContext.SaveChangesAsync();

        var publisher = new RecordingIntegrationEventPublisher(
            shouldFail: true);
        var processor = CreateProcessor(
            database,
            publisher,
            now,
            maximumAttempts: 3);

        // Act
        var result = await processor.ProcessPendingAsync();

        // Assert
        Assert.Equal(1, result.SelectedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(1, result.DeadLetteredCount);

        database.DbContext.ChangeTracker.Clear();

        var savedMessage = await database.DbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(3, savedMessage.AttemptCount);
        Assert.Equal(now.UtcDateTime, savedMessage.DeadLetteredAtUtc);
        Assert.Null(savedMessage.NextAttemptAtUtc);
        Assert.Null(savedMessage.LockId);

        var replayResult = await processor.ProcessPendingAsync();

        Assert.Equal(0, replayResult.SelectedCount);
    }

    private static EfCoreOutboxProcessor CreateProcessor(
        SqliteTestDatabase database,
        IIntegrationEventPublisher publisher,
        DateTimeOffset now,
        int maximumAttempts = 5)
    {
        return new EfCoreOutboxProcessor(
            database.DbContext,
            publisher,
            new FixedTimeProvider(now),
            Options.Create(new OutboxOptions
            {
                BatchSize = 20,
                PollingInterval = TimeSpan.FromSeconds(5),
                MaximumAttempts = maximumAttempts,
                LeaseDuration = TimeSpan.FromMinutes(1)
            }),
            NullLogger<EfCoreOutboxProcessor>.Instance);
    }

    private static OutboxMessage CreateMessage(DateTime occurredAtUtc)
    {
        return new OutboxMessage(
            IntegrationEventTypes.OrderPaidV1,
            "payment:1:succeeded",
            "{\"orderId\":1}",
            occurredAtUtc);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class RecordingIntegrationEventPublisher
        : IIntegrationEventPublisher
    {
        private readonly bool _shouldFail;

        public RecordingIntegrationEventPublisher(
            bool shouldFail = false)
        {
            _shouldFail = shouldFail;
        }

        public List<OutboxEnvelope> Messages { get; } = [];

        public Task PublishAsync(
            OutboxEnvelope message,
            CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new InvalidOperationException(
                    "The broker is unavailable.");
            }

            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
