using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elementum.UnitTests.Worker.Locking;

public class EfCoreDistributedLockProviderTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EfCoreDistributedLockProvider _lockProvider;

    public EfCoreDistributedLockProviderTests()
    {
        var services = new ServiceCollection();
        var dbRoot = new InMemoryDatabaseRoot();
        var dbName = "LockTestDb_" + Guid.NewGuid();

        services.AddDbContext<ElementumDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName, dbRoot);
        });

        _serviceProvider = services.BuildServiceProvider();
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _lockProvider = new EfCoreDistributedLockProvider(
            scopeFactory,
            NullLogger<EfCoreDistributedLockProvider>.Instance);
    }

    // [R]IGHT-BICEP: Verifies that an unheld resource lock is successfully acquired
    [Fact]
    public async Task TryAcquireLockAsync_WhenNoExistingLock_AcquiresLockSuccessfully()
    {
        // Arrange & Act
        await using var handle = await _lockProvider.TryAcquireLockAsync("resource_1", TimeSpan.FromMinutes(5));

        // Assert
        Assert.True(handle.IsAcquired);
    }

    // RIGHT-BIC[E]P: acquiring an active lock returns an unacquired handle
    [Fact]
    public async Task TryAcquireLockAsync_WhenLockAlreadyActive_ReturnsUnacquired()
    {
        // Arrange & Act
        await using var handle1 = await _lockProvider.TryAcquireLockAsync("resource_2", TimeSpan.FromMinutes(5));
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_2", TimeSpan.FromMinutes(5));

        // Assert
        Assert.True(handle1.IsAcquired);
        Assert.False(handle2.IsAcquired);
    }

    // RIGHT-BIC[E]P: expired locks are reclaimed by new callers
    [Fact]
    public async Task TryAcquireLockAsync_WhenLockExpired_TakesOverLock()
    {
        // Arrange
        await using (var handle1 = await _lockProvider.TryAcquireLockAsync("resource_3", TimeSpan.FromMilliseconds(-100)))
        {
            Assert.True(handle1.IsAcquired);
        }

        // Act - Another instance attempts to acquire the expired lock
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_3", TimeSpan.FromMinutes(5));

        // Assert
        Assert.True(handle2.IsAcquired);
    }

    // RIGHT-B[I]CEP: Verifies that disposing/releasing a lock makes the resource immediately available again
    [Fact]
    public async Task DisposeAsync_ReleasesLockForOtherInstances()
    {
        // Arrange
        var handle1 = await _lockProvider.TryAcquireLockAsync("resource_4", TimeSpan.FromMinutes(5));
        Assert.True(handle1.IsAcquired);

        // Act - Release lock
        await handle1.DisposeAsync();

        // Assert - New acquisition must now succeed
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_4", TimeSpan.FromMinutes(5));
        Assert.True(handle2.IsAcquired);
    }

    // RIGHT-BICE[P]: under concurrent parallel attempts, exactly one lock is granted
    [Fact]
    public async Task TryAcquireLockAsync_ParallelAttempts_OnlyOneAcquiresLock()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _lockProvider.TryAcquireLockAsync("resource_parallel", TimeSpan.FromMinutes(5)))
            .ToList();

        // Act
        var handles = await Task.WhenAll(tasks);
        var acquiredCount = handles.Count(h => h.IsAcquired);

        // Assert
        Assert.Equal(1, acquiredCount);

        foreach (var handle in handles)
        {
            await handle.DisposeAsync();
        }
    }

    // [R]IGHT-BICEP: Verifies that the internal heartbeat extends lock expiration before timeout
    [Fact]
    public async Task TryAcquireLockAsync_Heartbeat_ExtendsLockExpiration()
    {
        // Arrange - acquire with short TTL (1000ms -> heartbeat fires at 500ms)
        await using var handle = await _lockProvider.TryAcquireLockAsync("resource_heartbeat", TimeSpan.FromMilliseconds(1000));
        Assert.True(handle.IsAcquired);

        // Act - Wait 700ms (heartbeat should have fired at ~500ms and extended expiration)
        await Task.Delay(700);

        using var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
        var lockRecord = await db.DistributedLocks.FirstOrDefaultAsync(l => l.Resource == "resource_heartbeat");

        // Assert
        Assert.NotNull(lockRecord);
        Assert.True(lockRecord.ExpiresAtUtc > DateTime.UtcNow);
    }

    // RIGHT-BIC[E]P: concurrent takeover on an expired lock grants exactly one handle (DbUpdateConcurrency safety)
    [Fact]
    public async Task TryAcquireLockAsync_ParallelExpiredLockTakeover_OnlyOneAcquiresLock()
    {
        // Arrange
        await using (var expiredHandle = await _lockProvider.TryAcquireLockAsync("resource_expired_takeover", TimeSpan.FromMilliseconds(-100)))
        {
            Assert.True(expiredHandle.IsAcquired);
        }

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _lockProvider.TryAcquireLockAsync("resource_expired_takeover", TimeSpan.FromMinutes(5)))
            .ToList();

        // Act
        var handles = await Task.WhenAll(tasks);
        var acquiredCount = handles.Count(h => h.IsAcquired);

        // Assert
        Assert.Equal(1, acquiredCount);

        foreach (var handle in handles)
        {
            await handle.DisposeAsync();
        }
    }

    // RIGHT-BIC[E]P: database failures during lock acquisition return an unacquired handle instead of throwing
    [Fact]
    public async Task TryAcquireLockAsync_WhenDatabaseSaveFails_ReturnsUnacquired()
    {
        // Arrange
        var services = new ServiceCollection();
        var dbName = "LockFailureDb_" + Guid.NewGuid();

        services.AddDbContext<ElementumDbContext, ThrowingOnSaveDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        await using var serviceProvider = services.BuildServiceProvider();
        var lockProvider = new EfCoreDistributedLockProvider(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EfCoreDistributedLockProvider>.Instance);

        // Act
        await using var handle = await lockProvider.TryAcquireLockAsync("resource_db_failure", TimeSpan.FromMinutes(5));

        // Assert
        Assert.False(handle.IsAcquired);
    }

    // RIGHT-BI[C]EP: lock handles are IAsyncDisposable only — sync Dispose would block on EF (sync-over-async)
    [Fact]
    public void IDistributedLock_DoesNotImplementIDisposable()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(Elementum.Domain.Ports.Outbound.IDistributedLock)));

        var nestedLocks = typeof(EfCoreDistributedLockProvider)
            .GetNestedTypes(System.Reflection.BindingFlags.NonPublic)
            .Where(t => typeof(Elementum.Domain.Ports.Outbound.IDistributedLock).IsAssignableFrom(t));

        Assert.All(nestedLocks, t =>
            Assert.False(typeof(IDisposable).IsAssignableFrom(t), $"{t.Name} must not implement IDisposable"));
    }

    private sealed class ThrowingOnSaveDbContext(DbContextOptions<ElementumDbContext> options)
        : ElementumDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated database failure");
    }
}
