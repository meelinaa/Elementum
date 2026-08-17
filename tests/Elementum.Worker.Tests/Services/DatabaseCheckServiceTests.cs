using Elementum.Infrastructure.Data;
using Elementum.Shared.Objects;
using Elementum.WorkerService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.WorkerService.Tests.Services;

public class DatabaseCheckServiceTests
{
    [Fact]
    public async Task IsDatabaseAvailableAsync_WhenUsingInMemoryDatabase_ReturnsTrue()
    {
        var scopeFactory = CreateScopeFactoryWithInMemoryDb();
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, scopeFactory);

        var result = await service.IsDatabaseAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task DataExistsForTodayAsync_WhenPriceHistoryEmpty_ReturnsFalse()
    {
        var scopeFactory = CreateScopeFactoryWithInMemoryDb(seedPriceHistory: false);
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, scopeFactory);

        var result = await service.DataExistsForTodayAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task DataExistsForTodayAsync_WhenPriceHistoryHasRowForToday_ReturnsTrue()
    {
        var scopeFactory = CreateScopeFactoryWithInMemoryDb(seedPriceHistory: true);
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, scopeFactory);

        var result = await service.DataExistsForTodayAsync();

        Assert.True(result);
    }

    private static IServiceScopeFactory CreateScopeFactoryWithInMemoryDb(bool seedPriceHistory = false)
    {
        var dbName = "DbCheck_" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<ElementumDbContext>(opts => opts.UseInMemoryDatabase(dbName));
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            db.Database.EnsureCreated();
            db.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
            db.SaveChanges();
            if (seedPriceHistory)
            {
                db.PriceHistory.Add(new PriceHistory
                {
                    MetalId = 1,
                    Currency = "USD",
                    EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Price = 1m
                });
                db.SaveChanges();
            }
        }
        return sp.GetRequiredService<IServiceScopeFactory>();
    }
}
