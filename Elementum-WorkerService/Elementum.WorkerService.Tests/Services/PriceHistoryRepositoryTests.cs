using Elementum.Infrastructure.Data;
using Elementum.Shared.Objects;
using Elementum_WorkerService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.WorkerService.Tests.Services;

public class PriceHistoryRepositoryTests
{
    [Fact]
    public async Task SavePricesAsync_WhenPricesEmpty_DoesNotThrow()
    {
        var scopeFactory = CreateScopeFactoryWithMetals();
        var logger = new Mock<ILogger<PriceHistoryRepository>>().Object;
        var repo = new PriceHistoryRepository(logger, scopeFactory);

        await repo.SavePricesAsync(Array.Empty<DailyPrices>());
    }

    [Fact]
    public async Task SavePricesAsync_WhenPricesContainValidMetal_InsertsRow()
    {
        var scopeFactory = CreateScopeFactoryWithMetals();
        var logger = new Mock<ILogger<PriceHistoryRepository>>().Object;
        var repo = new PriceHistoryRepository(logger, scopeFactory);
        var prices = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2650.50m, Symbol = "FOREXCOM:XAUUSD" }
        };

        await repo.SavePricesAsync(prices);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            var count = await db.PriceHistory.CountAsync();
            Assert.Equal(1, count);
            var row = await db.PriceHistory.FirstAsync();
            Assert.Equal(1, row.MetalId);
            Assert.Equal(2650.50m, row.Price);
            Assert.Equal("USD", row.Currency);
        }
    }

    [Fact]
    public async Task SavePricesAsync_WhenMetalUnknown_SkipsAndDoesNotInsert()
    {
        var scopeFactory = CreateScopeFactoryWithMetals();
        var logger = new Mock<ILogger<PriceHistoryRepository>>().Object;
        var repo = new PriceHistoryRepository(logger, scopeFactory);
        var prices = new List<DailyPrices>
        {
            new() { Metal = "UNKNOWN", Currency = "USD", Price = 1m }
        };

        await repo.SavePricesAsync(prices);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            var count = await db.PriceHistory.CountAsync();
            Assert.Equal(0, count);
        }
    }

    [Fact]
    public async Task SavePricesAsync_WhenMetalEmpty_SkipsEntry()
    {
        var scopeFactory = CreateScopeFactoryWithMetals();
        var logger = new Mock<ILogger<PriceHistoryRepository>>().Object;
        var repo = new PriceHistoryRepository(logger, scopeFactory);
        var prices = new List<DailyPrices>
        {
            new() { Metal = "", Currency = "USD", Price = 1m }
        };

        await repo.SavePricesAsync(prices);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            Assert.Equal(0, await db.PriceHistory.CountAsync());
        }
    }

    private static IServiceScopeFactory CreateScopeFactoryWithMetals()
    {
        var dbName = "Repo_" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<ElementumDbContext>(opts => opts.UseInMemoryDatabase(dbName));
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            db.Database.EnsureCreated();
            db.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
            db.SaveChanges();
        }
        return sp.GetRequiredService<IServiceScopeFactory>();
    }
}
