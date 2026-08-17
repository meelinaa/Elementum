using Elementum.Application.UseCases.Ingestion;
using Elementum.Domain.Models;
using Elementum.Domain.Ports;
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
    public async Task ExecuteAsync_WhenAlreadyIngestedToday_DoesNotFetchOrSave()
    {
        _repositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _apiClientMock.Verify(c => c.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<DailyPrices>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotIngestedToday_FetchesAndSavesPrices()
    {
        _repositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var prices = new List<DailyPrices>
        {
            new() { Metal = "Gold", Price = 2500m, Symbol = "XAU" }
        };

        _apiClientMock.Setup(c => c.GetPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prices);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.SavePricesAsync(prices, It.IsAny<CancellationToken>()), Times.Once);
    }
}
