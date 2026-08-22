namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Represents an acquired or unacquired distributed lock handle.
/// Release the lock with <see cref="IAsyncDisposable.DisposeAsync"/> (<c>await using</c>).
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the distributed lock was successfully acquired.
    /// </summary>
    bool IsAcquired { get; }
}

/// <summary>
/// Secondary / Driven Outbound Port: Provides distributed locking to prevent duplicate concurrent execution across instances.
/// </summary>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Attempts to acquire a distributed lock on the specified <paramref name="resource"/>.
    /// </summary>
    /// <param name="resource">Unique resource lock name (e.g. "lock:job:metals_ingestion").</param>
    /// <param name="timeout">Maximum time to wait to acquire the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lock handle indicating whether the lock was acquired.</returns>
    Task<IDistributedLock> TryAcquireLockAsync(string resource, TimeSpan timeout, CancellationToken cancellationToken = default);
}
