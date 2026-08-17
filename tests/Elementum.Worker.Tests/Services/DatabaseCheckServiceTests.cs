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
    [Fact]
    public async Task CanConnectAsync_WhenUsingInMemoryDatabase_ReturnsTrue()
    {
        var scopeFactory = CreateScopeFactoryWithInMemoryDb();
        var logger = new Mock<ILogger<DatabaseCheckService>>().Object;
        var service = new DatabaseCheckService(logger, scopeFactory);

        var result = await service.CanConnectAsync();

        Assert.True(result);
    }

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
}
