using Elementum.Application.Exceptions;
using Elementum.Application.Services;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Elementum.UnitTests.Application.Services;

public class LiveQuotesProviderTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly LiveQuotesProvider _provider;

    public LiveQuotesProviderTests()
    {
        _provider = new LiveQuotesProvider(
            _apiClientMock.Object,
            _cache,
            NullLogger<LiveQuotesProvider>.Instance);
    }

    // [R]IGHT-BICEP: Verifies that when cache is empty, fresh quotes are fetched from API and returned
    [Fact]
    public async Task GetLiveQuoteAsync_WhenCacheMiss_FetchesFromApiAndPopulatesCache()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act
        var result = await _provider.GetLiveQuoteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.GoldUsd);
        _apiClientMock.Verify(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // [P]ERFORMANCE / CACHING: Verifies that subsequent calls hit in-memory cache without repeating HTTP requests
    [Fact]
    public async Task GetLiveQuoteAsync_WhenCacheHit_ReturnsCachedQuoteWithoutCallingApi()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        // Act - 1st call (Populate cache)
        var call1 = await _provider.GetLiveQuoteAsync(CancellationToken.None);

        // Act - 2nd call (Cache hit)
        var call2 = await _provider.GetLiveQuoteAsync(CancellationToken.None);

        // Assert
        Assert.Same(call1, call2);
        _apiClientMock.Verify(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // [E]RROR: Verifies that null API response throws ExternalApiException
    [Fact]
    public async Task GetLiveQuoteAsync_WhenApiReturnsNull_ThrowsExternalApiException()
    {
        // Arrange
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiException>(() => _provider.GetLiveQuoteAsync(CancellationToken.None));
    }

    // [E]RROR: Verifies that underlying network or API exceptions are propagated to caller
    [Fact]
    public async Task GetLiveQuoteAsync_WhenApiThrowsException_RethrowsException()
    {
        // Arrange
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Remote API unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _provider.GetLiveQuoteAsync(CancellationToken.None));
    }
}
