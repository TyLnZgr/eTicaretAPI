using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

namespace ECommerce.Api.Common.Caching;

public sealed partial class ResilientOutputCacheStore
    : IOutputCacheStore, IDisposable
{
    private static readonly long LogSuppressionPeriod =
        30 * Stopwatch.Frequency;

    private readonly IOutputCacheStore _innerStore;
    private readonly ILogger<ResilientOutputCacheStore> _logger;

    private long _nextLogTimestamp;
    private bool _disposed;

    public ResilientOutputCacheStore(
        IOutputCacheStore innerStore,
        ILogger<ResilientOutputCacheStore> logger)
    {
        ArgumentNullException.ThrowIfNull(innerStore);
        ArgumentNullException.ThrowIfNull(logger);

        _innerStore = innerStore;
        _logger = logger;
    }

    public async ValueTask<byte[]?> GetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _innerStore.GetAsync(key, cancellationToken);
        }
        catch (Exception exception) when (ShouldFailOpen(
            exception,
            cancellationToken))
        {
            LogFailure("read", exception);
            return null;
        }
    }

    public async ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        try
        {
            await _innerStore.SetAsync(
                key,
                value,
                tags,
                validFor,
                cancellationToken);
        }
        catch (Exception exception) when (ShouldFailOpen(
            exception,
            cancellationToken))
        {
            LogFailure("write", exception);
        }
    }

    public async ValueTask EvictByTagAsync(
        string tag,
        CancellationToken cancellationToken)
    {
        try
        {
            await _innerStore.EvictByTagAsync(tag, cancellationToken);
        }
        catch (Exception exception) when (ShouldFailOpen(
            exception,
            cancellationToken))
        {
            LogFailure("tag eviction", exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_innerStore is IDisposable disposableStore)
        {
            disposableStore.Dispose();
        }
    }

    private static bool ShouldFailOpen(
        Exception exception,
        CancellationToken cancellationToken)
    {
        return !cancellationToken.IsCancellationRequested &&
               exception is RedisException or SocketException or TimeoutException;
    }

    private void LogFailure(string operation, Exception exception)
    {
        var currentTimestamp = Stopwatch.GetTimestamp();
        var observedNextTimestamp = Volatile.Read(
            ref _nextLogTimestamp);

        if (currentTimestamp < observedNextTimestamp)
        {
            return;
        }

        var nextTimestamp = currentTimestamp + LogSuppressionPeriod;

        if (Interlocked.CompareExchange(
                ref _nextLogTimestamp,
                nextTimestamp,
                observedNextTimestamp) == observedNextTimestamp)
        {
            LogRedisUnavailable(_logger, operation, exception);
        }
    }

    [LoggerMessage(
        EventId = 3100,
        Level = LogLevel.Warning,
        Message = "Redis output cache {Operation} failed; the request will continue without the cache.")]
    private static partial void LogRedisUnavailable(
        ILogger logger,
        string operation,
        Exception exception);
}
