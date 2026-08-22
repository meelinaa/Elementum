using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
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

    /// <summary>
    /// Initializes static environment variables for test execution to guarantee valid default configuration
    /// when the application host bootstraps in isolated CI environments without an appsettings.json file.
    /// </summary>
    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=Elementum-Test;User=root;Password=root;");
        Environment.SetEnvironmentVariable("MetalsApi__BaseUrl", "https://api.edelmetalle.de/public.json");
        Environment.SetEnvironmentVariable("MetalsApi__Currency", "USD");
        Environment.SetEnvironmentVariable("RateLimiting__PermitLimit", "100");
        Environment.SetEnvironmentVariable("RateLimiting__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("RateLimiting__QueueLimit", "0");
    }

    /// <summary>
    /// Configures the web host for integration testing by providing in-memory configuration values,
    /// swapping MySQL with an EF Core in-memory database, and substituting external HTTP clients with test mocks.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Elementum-Test;User=root;Password=root;",
                ["MetalsApi:BaseUrl"] = "https://api.edelmetalle.de/public.json",
                ["MetalsApi:Currency"] = "USD",
                ["RateLimiting:PermitLimit"] = "100",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:QueueLimit"] = "0"
            });
        });

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

    /// <summary>
    /// Builds the host instance and initializes test data within the isolated in-memory database.
    /// </summary>
    /// <param name="builder">The host builder to instantiate.</param>
    /// <returns>The constructed host with seeded test data.</returns>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
        db.Database.EnsureCreated();
        SeedTestData(db);
        return host;
    }

    /// <summary>
    /// Seeds default metals and historical price ticks into the database for integration test assertions.
    /// </summary>
    /// <param name="db">The database context instance to populate.</param>
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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            db.PriceHistory.AddRange(
                PriceHistory.Create(
                    metalId: 1,
                    currency: "USD",
                    entryDate: today,
                    price: 2500.50m,
                    symbol: "FOREXCOM:XAUUSD",
                    openPrice: 2485.00m,
                    highPrice: 2510.00m,
                    lowPrice: 2475.00m,
                    prevClosePrice: 2480.00m,
                    ch: 20.50m,
                    chp: 0.82m,
                    referenceTimestamp: nowTimestamp),
                PriceHistory.Create(
                    metalId: 1,
                    currency: "USD",
                    entryDate: today.AddDays(-1),
                    price: 2480.00m,
                    symbol: "FOREXCOM:XAUUSD",
                    openPrice: 2460.00m,
                    highPrice: 2490.00m,
                    lowPrice: 2450.00m,
                    prevClosePrice: 2455.00m,
                    ch: 25.00m,
                    chp: 1.02m,
                    referenceTimestamp: nowTimestamp - 86400),
                PriceHistory.Create(
                    metalId: 1,
                    currency: "USD",
                    entryDate: today.AddDays(-2),
                    price: 2455.00m,
                    symbol: "FOREXCOM:XAUUSD",
                    openPrice: 2440.00m,
                    highPrice: 2465.00m,
                    lowPrice: 2435.00m,
                    prevClosePrice: 2430.00m,
                    ch: 25.00m,
                    chp: 1.03m,
                    referenceTimestamp: nowTimestamp - 172800));

            db.SaveChanges();
        }
    }
}
