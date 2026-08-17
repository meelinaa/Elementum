using Elementum.Domain.Entities;
using Elementum.Domain.Ports;
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
                // Expired lock: take it over safely
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

            return new EfCoreDistributedLock(_scopeFactory, resource, instanceId, _logger);
        }
        catch (DbUpdateException ex)
        {
            // Concurrency conflict / race condition between instances
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
        private readonly ILogger _logger;
        private int _disposed;

        public bool IsAcquired => true;

        public EfCoreDistributedLock(
            IServiceScopeFactory scopeFactory,
            string resource,
            string instanceId,
            ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _resource = resource;
            _instanceId = instanceId;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();

                var lockRecord = await db.DistributedLocks
                    .FirstOrDefaultAsync(l => l.Resource == _resource && l.AcquiredBy == _instanceId);

                if (lockRecord != null)
                {
                    db.DistributedLocks.Remove(lockRecord);
                    await db.SaveChangesAsync();
                    _logger.LogDebug("Released EF Core distributed lock for '{Resource}'", _resource);
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
