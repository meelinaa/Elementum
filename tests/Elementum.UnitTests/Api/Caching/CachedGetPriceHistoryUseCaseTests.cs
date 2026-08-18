using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Infrastructure.Caching;
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
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var sp = services.BuildServiceProvider();
        _cache = sp.GetRequiredService<HybridCache>();

        _cachedUseCase = new CachedGetPriceHistoryUseCase(_innerMock.Object, _cache);
    }

    [Fact]
    public async Task GetLatestBySymbolAsync_FirstCallCallsInner_SecondCallUsesCache()
    {
        var dto = new PriceHistoryDto
        {
            Id = 1,
            Symbol = "XAU",
            Price = 2500m,
            Currency = "USD"
        };

        _innerMock.Setup(x => x.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // First call - Cache Miss -> Calls inner
        var result1 = await _cachedUseCase.GetLatestBySymbolAsync("XAU");
        Assert.NotNull(result1);
        Assert.Equal(2500m, result1.Price);
        _innerMock.Verify(x => x.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()), Times.Once);

        // Second call - Cache Hit -> Does not call inner again
        var result2 = await _cachedUseCase.GetLatestBySymbolAsync("XAU");
        Assert.NotNull(result2);
        Assert.Equal(2500m, result2.Price);
        _innerMock.Verify(x => x.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestAllAsync_CachesResultAcrossCalls()
    {
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Symbol = "XAU", Price = 2500m },
            new() { Id = 2, Symbol = "XAG", Price = 30m }
        };

        _innerMock.Setup(x => x.GetLatestAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var call1 = await _cachedUseCase.GetLatestAllAsync();
        var call2 = await _cachedUseCase.GetLatestAllAsync();

        Assert.Equal(2, call1.Count());
        Assert.Equal(2, call2.Count());
        _innerMock.Verify(x => x.GetLatestAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
