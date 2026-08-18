using Elementum.Application.UseCases.Ingestion;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.Application.Tests.UseCases;

public class IngestPricesUseCaseTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly Mock<IPriceHistoryRepository> _repositoryMock = new();
    private readonly Mock<ILogger<IngestPricesUseCase>> _loggerMock = new();
    private readonly IngestPricesUseCase _useCase;

    public IngestPricesUseCaseTests()
    {
        _useCase = new IngestPricesUseCase(_apiClientMock.Object, _repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleReturnsData_SavesEdelmetallePrices()
    {
        var response = new EdelmetalleApiResponse
        {
            GoldUsd = 4410.6m,
            GoldEur = 3803.8m,
            SilberUsd = 65.7m,
            SilberEur = 56.6m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveEdelmetallePricesAsync(response, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleNull_FallsBackToGetPricesAsync()
    {
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);

        var fallbackPrices = new List<DailyPrices>
        {
            new() { Metal = "Gold", Price = 2500m, Symbol = "XAU" }
        };

        _apiClientMock.Setup(c => c.GetPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackPrices);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.SavePricesAsync(fallbackPrices, It.IsAny<CancellationToken>()), Times.Once);
    }
}
