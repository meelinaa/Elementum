using Elementum.Domain.Entities;
using Elementum.Domain.Ports;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for E2E integration testing.
/// Configures an isolated in-memory database and seeds standard test fixtures.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();
    private readonly string _databaseName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ElementumDbContext>));
            if (dbOptionsDescriptor != null)
            {
                services.Remove(dbOptionsDescriptor);
            }

            var internalSp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ElementumDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName, _dbRoot);
                options.UseInternalServiceProvider(internalSp);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);
        return host;
    }

    private static void SeedTestData(ElementumDbContext db)
    {
        if (!db.Metals.Any())
        {
            var gold = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };
            var silver = new Metals { Id = 2, Symbol = "XAG", Name = "Silver" };
            var platinum = new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" };
            var palladium = new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" };

            db.Metals.AddRange(gold, silver, platinum, palladium);
            db.SaveChanges();

            db.PriceHistory.Add(new PriceHistory
            {
                Id = 1,
                MetalId = 1,
                Currency = "USD",
                Exchange = "FOREX",
                Symbol = "FOREXCOM:XAUUSD",
                EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Price = 2500.50m,
                PrevClosePrice = 2480.00m,
                OpenPrice = 2485.00m,
                LowPrice = 2475.00m,
                HighPrice = 2510.00m,
                Ch = 20.50m,
                Chp = 0.82m,
                Ask = 2501.00m,
                Bid = 2500.00m,
                PriceGram24k = 80.40m,
                PriceGram22k = 73.70m,
                PriceGram21k = 70.35m,
                PriceGram20k = 67.00m,
                PriceGram18k = 60.30m,
                PriceGram16k = 53.60m,
                PriceGram14k = 46.90m,
                PriceGram10k = 33.50m
            });

            db.SaveChanges();
        }
    }
}
