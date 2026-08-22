using Elementum.Application.Models;
using Elementum.Application.Services;
using Elementum.Infrastructure.Outbound.Caching;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Elementum.UnitTests.Infrastructure.Caching;

public class CachedLiveQuotesProviderTests
{
    private readonly Mock<ILiveQuotesProvider> _innerMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly CachedLiveQuotesProvider _decorator;

    public CachedLiveQuotesProviderTests()
    {
        _decorator = new CachedLiveQuotesProvider(_innerMock.Object, _cache);
    }

    // [R]IGHT-BICEP: Verifies that on cache miss, inner provider is invoked and value is cached
    [Fact]
    public async Task GetLiveQuoteAsync_WhenCacheMiss_DelegatesToInnerAndCachesResult()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _innerMock.Setup(i => i.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await _decorator.GetLiveQuoteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.GoldUsd);
        _innerMock.Verify(i => i.GetLiveQuoteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // [P]ERFORMANCE / CACHING: Verifies that repeated invocations return cached instance without invoking inner provider
    [Fact]
    public async Task GetLiveQuoteAsync_WhenCacheHit_ReturnsCachedQuoteWithoutInvokingInner()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _innerMock.Setup(i => i.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act - 1st call (populates cache)
        var first = await _decorator.GetLiveQuoteAsync(CancellationToken.None);

        // Act - 2nd call (cache hit)
        var second = await _decorator.GetLiveQuoteAsync(CancellationToken.None);

        // Assert
        Assert.Same(first, second);
        _innerMock.Verify(i => i.GetLiveQuoteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // RIGHT-BIC[E]P: Verifies that errors from inner provider propagate and do not corrupt cache
    [Fact]
    public async Task GetLiveQuoteAsync_WhenInnerThrows_PropagatesException()
    {
        // Arrange
        _innerMock.Setup(i => i.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API failure"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _decorator.GetLiveQuoteAsync(CancellationToken.None));
    }
}
