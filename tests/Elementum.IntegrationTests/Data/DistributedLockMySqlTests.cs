using Elementum.Infrastructure.Outbound.Data;
using Elementum.Infrastructure.Outbound.Locking;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elementum.IntegrationTests.Data;

[Collection(MySqlCollection.Name)]
public sealed class DistributedLockMySqlTests(MySqlContainerFixture fixture) : IAsyncLifetime
{
    private readonly string _connectionString = fixture.ConnectionString;
    private ServiceProvider _services = null!;
    private EfCoreDistributedLockProvider _lockProvider = null!;

    public async Task InitializeAsync()
    {
        await MySqlTestContext.ResetSchemaAsync(_connectionString);

        var services = new ServiceCollection();
        services.AddDbContext<ElementumDbContext>(options =>
            options.UseMySql(_connectionString, MySqlTestContext.ServerVersion));
        _services = services.BuildServiceProvider();
        _lockProvider = new EfCoreDistributedLockProvider(
            _services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EfCoreDistributedLockProvider>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
            await _services.DisposeAsync();
    }

    // [R]IGHT-BICEP: first acquirer wins on a real MySQL primary key
    [Fact]
    public async Task TryAcquireLockAsync_WhenNoExistingLock_AcquiresLockSuccessfully()
    {
        await using var handle = await _lockProvider.TryAcquireLockAsync("mysql_resource_1", TimeSpan.FromMinutes(5));

        Assert.True(handle.IsAcquired);
    }

    // RIGHT-BIC[E]P: a second caller against an active row is rejected
    [Fact]
    public async Task TryAcquireLockAsync_WhenLockAlreadyActive_ReturnsUnacquired()
    {
        await using var handle1 = await _lockProvider.TryAcquireLockAsync("mysql_resource_2", TimeSpan.FromMinutes(5));
        await using var handle2 = await _lockProvider.TryAcquireLockAsync("mysql_resource_2", TimeSpan.FromMinutes(5));

        Assert.True(handle1.IsAcquired);
        Assert.False(handle2.IsAcquired);
    }

    // RIGHT-B[I]CEP: dispose deletes the row so another instance can acquire
    [Fact]
    public async Task DisposeAsync_ReleasesLockForOtherInstances()
    {
        var handle1 = await _lockProvider.TryAcquireLockAsync("mysql_resource_3", TimeSpan.FromMinutes(5));
        Assert.True(handle1.IsAcquired);
        await handle1.DisposeAsync();

        await using var handle2 = await _lockProvider.TryAcquireLockAsync("mysql_resource_3", TimeSpan.FromMinutes(5));
        Assert.True(handle2.IsAcquired);
    }

    // RIGHT-BICE[P]: concurrent inserts collide on PK; exactly one handle is acquired
    [Fact]
    public async Task TryAcquireLockAsync_ParallelAttempts_OnlyOneAcquiresLock()
    {
        var handles = await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(_ => _lockProvider.TryAcquireLockAsync("mysql_resource_parallel", TimeSpan.FromMinutes(5))));

        Assert.Equal(1, handles.Count(h => h.IsAcquired));

        foreach (var handle in handles)
            await handle.DisposeAsync();
    }

    // RIGHT-BIC[E]P: concurrent takeover of an expired lock grants exactly one winner via concurrency token / PK
    [Fact]
    public async Task TryAcquireLockAsync_ParallelExpiredLockTakeover_OnlyOneAcquiresLock()
    {
        await using (var expired = await _lockProvider.TryAcquireLockAsync("mysql_resource_expired", TimeSpan.FromMilliseconds(-100)))
        {
            Assert.True(expired.IsAcquired);
        }

        var handles = await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(_ => _lockProvider.TryAcquireLockAsync("mysql_resource_expired", TimeSpan.FromMinutes(5))));

        Assert.Equal(1, handles.Count(h => h.IsAcquired));

        foreach (var handle in handles)
            await handle.DisposeAsync();
    }
}
