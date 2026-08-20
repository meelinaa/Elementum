using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;
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
/// WebApplicationFactory that simulates upstream API timeout failures.
/// </summary>
public class UpstreamTimeoutWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();
    private readonly string _databaseName = "UpstreamTimeoutTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDatabase(services);

            var mockMetalsApi = new Mock<IMetalsApiClient>();
            mockMetalsApi.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("upstream request timed out"));

            services.RemoveAll<IMetalsApiClient>();
            services.AddSingleton(mockMetalsApi.Object);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedMetals(host);
        return host;
    }

    private void ReplaceDatabase(IServiceCollection services)
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
    }

    private static void SeedMetals(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
        db.Database.EnsureCreated();

        if (!db.Metals.Any())
        {
            db.Metals.AddRange(
                new Metals { Id = 1, Symbol = "XAU", Name = "Gold" },
                new Metals { Id = 2, Symbol = "XAG", Name = "Silver" },
                new Metals { Id = 3, Symbol = "XPT", Name = "Platinum" },
                new Metals { Id = 4, Symbol = "XPD", Name = "Palladium" });
            db.SaveChanges();
        }
    }
}
