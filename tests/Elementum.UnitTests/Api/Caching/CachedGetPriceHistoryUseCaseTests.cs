using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Infrastructure.Outbound.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elementum.Api.Tests.Caching;

public class CachedGetPriceHistoryUseCaseTests
{
    private readonly Mock<IGetPriceHistoryUseCase> _innerMock = new();
    private readonly HybridCache _cache;
    private readonly CachedGetPriceHistoryUseCase _cachedUseCase;

    public CachedGetPriceHistoryUseCaseTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();

        _cachedUseCase = new CachedGetPriceHistoryUseCase(_innerMock.Object, _cache);
    }

    // [R]IGHT-BICEP: Verifies cache-aside pattern (first call misses and fetches from inner, second hits cache directly)
    [Fact]
    public async Task GetLatestBySymbolAsync_FirstCallCallsInner_SecondCallUsesCache()
    {
        // Arrange
        var dto = new PriceHistoryDto
        {
            Id = 1,
            Symbol = "XAU",
            Price = 2500m,
            Currency = "USD"
        };

        _innerMock.Setup(x => x.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act - First call (Cache Miss)
        var result1 = await _cachedUseCase.GetLatestBySymbolAsync("XAU");

        // Act - Second call (Cache Hit)
        var result2 = await _cachedUseCase.GetLatestBySymbolAsync("XAU");

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(2500m, result1.Price);
        Assert.NotNull(result2);
        Assert.Equal(2500m, result2.Price);
        _innerMock.Verify(x => x.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Verifies that full list queries are cached across multiple callers
    [Fact]
    public async Task GetLatestAllAsync_CachesResultAcrossCalls()
    {
        // Arrange
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Symbol = "XAU", Price = 2500m },
            new() { Id = 2, Symbol = "XAG", Price = 30m }
        };

        _innerMock.Setup(x => x.GetLatestAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var call1 = await _cachedUseCase.GetLatestAllAsync();
        var call2 = await _cachedUseCase.GetLatestAllAsync();

        // Assert
        Assert.Equal(2, call1.Count());
        Assert.Equal(2, call2.Count());
        _innerMock.Verify(x => x.GetLatestAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
