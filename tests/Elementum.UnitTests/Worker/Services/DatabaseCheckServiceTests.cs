using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.Worker.Tests.Services;

public class DatabaseCheckServiceTests
{
    private static IServiceScopeFactory CreateScopeFactoryWithInMemoryDb()
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
        }
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    // [R]IGHT-BICEP: Verifies that CanConnectAsync returns true when the underlying database is reachable
    [Fact]
    public async Task CanConnectAsync_WhenUsingInMemoryDatabase_ReturnsTrue()
    {
        // Arrange
        var scopeFactory = CreateScopeFactoryWithInMemoryDb();
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, scopeFactory);

        // Act
        var result = await service.CanConnectAsync();

        // Assert
        Assert.True(result);
    }

    // [E]RROR: Verifies that CanConnectAsync returns false gracefully when database scope resolution fails
    [Fact]
    public async Task CanConnectAsync_WhenScopeFactoryThrows_ReturnsFalse()
    {
        // Arrange
        var brokenScopeFactoryMock = new Mock<IServiceScopeFactory>();
        brokenScopeFactoryMock.Setup(s => s.CreateScope()).Throws(new InvalidOperationException("DB connection failure"));
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, brokenScopeFactoryMock.Object);

        // Act
        var result = await service.CanConnectAsync();

        // Assert
        Assert.False(result);
    }
}
