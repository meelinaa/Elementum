using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for E2E integration testing.
/// Configures an isolated in-memory database and deterministic offline test fixtures.
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

            // Mock external HTTP client for deterministic offline testing
            var mockMetalsApi = new Mock<IMetalsApiClient>();
            mockMetalsApi.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EdelmetalleApiResponse
                {
                    GoldUsd = 2500.50m,
                    GoldEur = 2280.80m,
                    SilberUsd = 30.70m,
                    SilberEur = 28.60m,
                    PlatinUsd = 1000m,
                    PlatinEur = 910m,
                    PalladiumUsd = 1050m,
                    PalladiumEur = 960m,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    WechselkursUsdEur = 1.095m
                });

            services.RemoveAll<IMetalsApiClient>();
            services.AddSingleton(mockMetalsApi.Object);
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
        }

        if (!db.PriceHistory.Any())
        {
            db.PriceHistory.Add(new PriceHistory
            {
                Id = 1,
                MetalId = 1,
                Currency = "USD",
                Symbol = "FOREXCOM:XAUUSD",
                ReferenceTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Price = 2500.50m,
                PrevClosePrice = 2480.00m,
                OpenPrice = 2485.00m,
                LowPrice = 2475.00m,
                HighPrice = 2510.00m,
                Ch = 20.50m,
                Chp = 0.82m
            });

            db.SaveChanges();
        }
    }
}
