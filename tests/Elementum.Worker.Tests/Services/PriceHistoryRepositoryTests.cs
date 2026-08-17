using Elementum.Domain.Entities;
using Elementum.Domain.Models;
using Elementum.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Worker.Tests.Services;

public class PriceHistoryRepositoryTests
{
    private static ElementumDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ElementumDbContext(options);
        context.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        context.Metals.Add(new Metals { Id = 2, Symbol = "XAG", Name = "Silver" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task SavePricesAsync_WhenPricesEmpty_DoesNotThrow()
    {
        await using var db = CreateDbContext();
        await db.SavePricesAsync(Array.Empty<DailyPrices>());
        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SavePricesAsync_WhenPricesContainValidMetal_InsertsRow()
    {
        await using var db = CreateDbContext();
        var prices = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2650.50m, Symbol = "FOREXCOM:XAUUSD" }
        };

        await db.SavePricesAsync(prices);

        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(1, count);
        var row = await db.PriceHistory.FirstAsync();
        Assert.Equal(1, row.MetalId);
        Assert.Equal(2650.50m, row.Price);
        Assert.Equal("USD", row.Currency);
    }

    [Fact]
    public async Task SavePricesAsync_WhenMetalUnknown_SkipsAndDoesNotInsert()
    {
        await using var db = CreateDbContext();
        var prices = new List<DailyPrices>
        {
            new() { Metal = "UNKNOWN", Currency = "USD", Price = 1m }
        };

        await db.SavePricesAsync(prices);

        var count = await db.PriceHistory.CountAsync();
        Assert.Equal(0, count);
    }
}
