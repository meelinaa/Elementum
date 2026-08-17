using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Locking;

/// <summary>
/// Secondary / Driven Adapter: Implements distributed locking using Entity Framework Core.
/// Fully database-agnostic, persists lock state in the <c>distributed_locks</c> table with automatic TTL expiration.
/// </summary>
public class EfCoreDistributedLockProvider : IDistributedLockProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EfCoreDistributedLockProvider> _logger;

    public EfCoreDistributedLockProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<EfCoreDistributedLockProvider> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
                db.DistributedLocks.Add(new DistributedLockEntity
                {
                    Resource = resource,
                    AcquiredBy = instanceId,
                    AcquiredAtUtc = now,
                    ExpiresAtUtc = expiresAt
                });
            }
            else if (existing.ExpiresAtUtc < now)
            {
                // Expired lock: take it over safely with optimistic concurrency check (ExpiresAtUtc is ConcurrencyCheck token)
                existing.AcquiredBy = instanceId;
                existing.AcquiredAtUtc = now;
                existing.ExpiresAtUtc = expiresAt;
            }
            else
            {
                // Lock is actively held by another instance
                _logger.LogInformation(
                    "Distributed lock for '{Resource}' is currently held by '{Holder}' until {ExpiresAtUtc} UTC.",
                    resource, existing.AcquiredBy, existing.ExpiresAtUtc);
                return new NoOpDistributedLock();
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogDebug(
                "Acquired EF Core distributed lock for '{Resource}' by '{InstanceId}' until {ExpiresAtUtc} UTC.",
                resource, instanceId, expiresAt);

            return new EfCoreDistributedLock(_scopeFactory, resource, instanceId, timeout, _logger);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Concurrency conflict: another instance won the race to take over the expired lock
            _logger.LogInformation(
                "Concurrency race acquiring EF Core distributed lock for '{Resource}'. Another instance took it over: {Message}",
                resource, ex.Message);
            return new NoOpDistributedLock();
        }
        catch (DbUpdateException ex)
        {
            // Concurrency conflict / primary key collision between instances creating a new lock
            _logger.LogInformation(
                "Conflict acquiring EF Core distributed lock for '{Resource}'. Another instance won the race: {Message}",
                resource, ex.Message);
            return new NoOpDistributedLock();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to acquire EF Core distributed lock for '{Resource}'.", resource);
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
        private readonly Task _heartbeatTask;
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
            _heartbeatTask = StartHeartbeatAsync(_heartbeatCts.Token);
        }

        private async Task StartHeartbeatAsync(CancellationToken ct)
        {
            var interval = TimeSpan.FromMilliseconds(Math.Max(500, _timeout.TotalMilliseconds / 2));
            using var timer = new PeriodicTimer(interval);
            try
            {
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
                            _logger.LogDebug("Auto-renewed distributed lock for '{Resource}' until {ExpiresAtUtc} UTC.", _resource, lockRecord.ExpiresAtUtc);
                        }
                        else
                        {
                            _logger.LogWarning("Lost ownership of distributed lock for '{Resource}' during heartbeat.", _resource);
                            break;
                        }
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _logger.LogWarning("Concurrency collision renewing distributed lock for '{Resource}'.", _resource);
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Failed to renew distributed lock for '{Resource}'.", _resource);
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
                        _logger.LogDebug("Released EF Core distributed lock for '{Resource}'", _resource);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Lock was already modified or removed
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error releasing EF Core distributed lock for '{Resource}'.", _resource);
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
