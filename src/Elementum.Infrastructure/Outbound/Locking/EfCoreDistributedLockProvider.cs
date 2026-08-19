using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Entities;
using Elementum.Infrastructure.Locking.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Locking;

/// <summary>
/// Distributed lock implementation using Entity Framework Core and MySQL table <c>distributed_locks</c>.
/// Uses optimistic concurrency tokens to prevent race conditions during takeover and source-generated logging.
/// </summary>
public class EfCoreDistributedLockProvider : IDistributedLockProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EfCoreDistributedLockProvider> _logger;

    public EfCoreDistributedLockProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<EfCoreDistributedLockProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IDistributedLock> TryAcquireLockAsync(
        string resource,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var instanceId = $"{Environment.MachineName}_{Guid.NewGuid():N}";
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(timeout);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

            var existing = await db.DistributedLocks
                .FirstOrDefaultAsync(l => l.Resource == resource, cancellationToken);

            if (existing == null)
            {
                // No lock exists - create new
                var lockRecord = new DistributedLockEntity
                {
                    Resource = resource,
                    AcquiredBy = instanceId,
                    AcquiredAtUtc = now,
                    ExpiresAtUtc = expiresAt
                };
                db.DistributedLocks.Add(lockRecord);
            }
            else if (existing.ExpiresAtUtc < now)
            {
                // Lock expired - takeover
                DistributedLockLogMessages.StaleLockTakenOver(_logger, resource, instanceId, expiresAt);
                existing.AcquiredBy = instanceId;
                existing.AcquiredAtUtc = now;
                existing.ExpiresAtUtc = expiresAt;
            }
            else if (existing.AcquiredBy == instanceId)
            {
                // Re-entrant / extend own lock
                DistributedLockLogMessages.LockRenewedManually(_logger, resource, instanceId, expiresAt);
                existing.ExpiresAtUtc = expiresAt;
            }
            else
            {
                // Lock is actively held by another instance
                DistributedLockLogMessages.LockBusy(_logger, resource, existing.AcquiredBy, existing.ExpiresAtUtc);
                return new NoOpDistributedLock();
            }

            await db.SaveChangesAsync(cancellationToken);
            DistributedLockLogMessages.LockAcquired(_logger, resource, instanceId, expiresAt);

            return new EfCoreDistributedLock(_scopeFactory, resource, instanceId, timeout, _logger);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrency conflict: another instance won the race to take over the expired lock
            DistributedLockLogMessages.LockAcquisitionFailed(_logger, resource, null);
            return new NoOpDistributedLock();
        }
        catch (DbUpdateException)
        {
            // Concurrency conflict / primary key collision between instances creating a new lock
            DistributedLockLogMessages.LockAcquisitionFailed(_logger, resource, null);
            return new NoOpDistributedLock();
        }
        catch (Exception ex)
        {
            DistributedLockLogMessages.LockAcquisitionFailed(_logger, resource, ex);
            return new NoOpDistributedLock();
        }
    }

    private sealed class EfCoreDistributedLock : IDistributedLock
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _resource;
        private readonly string _instanceId;
        private readonly TimeSpan _timeout;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _heartbeatCts = new();
        private int _disposed;

        public bool IsAcquired => true;

        public EfCoreDistributedLock(
            IServiceScopeFactory scopeFactory,
            string resource,
            string instanceId,
            TimeSpan timeout,
            ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _resource = resource;
            _instanceId = instanceId;
            _timeout = timeout;
            _logger = logger;

            // Start background heartbeat to renew lock before it expires
            var renewalInterval = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 3);
            if (renewalInterval > TimeSpan.FromSeconds(1))
            {
                _ = RunHeartbeatAsync(renewalInterval, _heartbeatCts.Token);
            }
        }

        private async Task RunHeartbeatAsync(TimeSpan interval, CancellationToken ct)
        {
            try
            {
                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

                        var lockRecord = await db.DistributedLocks
                            .FirstOrDefaultAsync(l => l.Resource == _resource && l.AcquiredBy == _instanceId, ct);

                        if (lockRecord != null)
                        {
                            lockRecord.ExpiresAtUtc = DateTime.UtcNow.Add(_timeout);
                            await db.SaveChangesAsync(ct);
                            DistributedLockLogMessages.LockAutoRenewed(_logger, _resource, lockRecord.ExpiresAtUtc);
                        }
                        else
                        {
                            DistributedLockLogMessages.LockOwnershipLost(_logger, _resource);
                            break;
                        }
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        DistributedLockLogMessages.LockRenewalCollision(_logger, _resource);
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        DistributedLockLogMessages.LockRenewalFailed(_logger, _resource, ex);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await _heartbeatCts.CancelAsync();
            _heartbeatCts.Dispose();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

                var lockRecord = await db.DistributedLocks
                    .FirstOrDefaultAsync(l => l.Resource == _resource && l.AcquiredBy == _instanceId);

                if (lockRecord != null)
                {
                    db.DistributedLocks.Remove(lockRecord);
                    try
                    {
                        await db.SaveChangesAsync();
                        DistributedLockLogMessages.LockReleased(_logger, _resource);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Lock was already modified or removed
                    }
                }
            }
            catch (Exception ex)
            {
                DistributedLockLogMessages.LockReleaseError(_logger, _resource, ex);
            }
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private sealed class NoOpDistributedLock : IDistributedLock
    {
        public bool IsAcquired => false;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }
}
