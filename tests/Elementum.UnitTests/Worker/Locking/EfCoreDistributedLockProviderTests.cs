using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Locking;
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

    [Fact]
    public async Task TryAcquireLockAsync_WhenNoExistingLock_AcquiresLockSuccessfully()
    {
        await using var handle = await _lockProvider.TryAcquireLockAsync("resource_1", TimeSpan.FromMinutes(5));

        Assert.True(handle.IsAcquired);
    }

    [Fact]
    public async Task TryAcquireLockAsync_WhenLockAlreadyActive_ReturnsUnacquired()
    {
        await using var handle1 = await _lockProvider.TryAcquireLockAsync("resource_2", TimeSpan.FromMinutes(5));
        Assert.True(handle1.IsAcquired);

        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_2", TimeSpan.FromMinutes(5));
        Assert.False(handle2.IsAcquired);
    }

    [Fact]
    public async Task TryAcquireLockAsync_WhenLockExpired_TakesOverLock()
    {
        // Acquire lock with expired duration (0ms)
        await using (var handle1 = await _lockProvider.TryAcquireLockAsync("resource_3", TimeSpan.FromMilliseconds(-100)))
        {
            Assert.True(handle1.IsAcquired);
        }

        // Another instance attempts to acquire the expired lock -> must succeed
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_3", TimeSpan.FromMinutes(5));
        Assert.True(handle2.IsAcquired);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLockForOtherInstances()
    {
        var handle1 = await _lockProvider.TryAcquireLockAsync("resource_4", TimeSpan.FromMinutes(5));
        Assert.True(handle1.IsAcquired);

        // Release lock
        await handle1.DisposeAsync();

        // New acquisition should now succeed
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("resource_4", TimeSpan.FromMinutes(5));
        Assert.True(handle2.IsAcquired);
    }
}
