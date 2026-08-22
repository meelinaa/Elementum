using Elementum.Application.Exceptions;
using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Elementum.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Elementum.UnitTests.Application.Services;

public class LiveQuotesProviderTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly LiveQuotesProvider _provider;

    public LiveQuotesProviderTests()
    {
        _provider = new LiveQuotesProvider(
            _apiClientMock.Object,
            NullLogger<LiveQuotesProvider>.Instance);
    }

    // [R]IGHT-BICEP: Verifies that fresh quotes are fetched directly from API and returned
    [Fact]
    public async Task GetLiveQuoteAsync_WhenApiReturnsData_ReturnsQuote()
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

    // RIGHT-BIC[E]P: null API response throws ExternalApiException for upstream empty-data path
    [Fact]
    public async Task GetLiveQuoteAsync_WhenApiReturnsNull_ThrowsExternalApiException()
    {
        // Arrange
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiException>(() => _provider.GetLiveQuoteAsync(CancellationToken.None));
    }

    // RIGHT-BIC[E]P: underlying network or API exceptions are propagated unchanged to the caller
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
