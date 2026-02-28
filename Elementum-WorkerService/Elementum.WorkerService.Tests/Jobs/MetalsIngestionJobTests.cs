using Elementum.Shared.Objects;
using Elementum_WorkerService.Abstractions;
using Elementum_WorkerService.Jobs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.WorkerService.Tests.Jobs;

public class MetalsIngestionJobTests
{
    private readonly Mock<ILogger<MetalsIngestionJob>> _loggerMock;
    private readonly Mock<IMetalsApiClient> _apiClientMock;
    private readonly Mock<IDatabaseCheckService> _databaseCheckMock;
    private readonly Mock<IPriceHistoryRepository> _repositoryMock;

    public MetalsIngestionJobTests()
    {
        _loggerMock = new Mock<ILogger<MetalsIngestionJob>>();
        _apiClientMock = new Mock<IMetalsApiClient>();
        _databaseCheckMock = new Mock<IDatabaseCheckService>();
        _repositoryMock = new Mock<IPriceHistoryRepository>();
    }

    private static MetalsIngestionJob CreateJob(
        ILogger<MetalsIngestionJob>? logger = null,
        IMetalsApiClient? apiClient = null,
        IDatabaseCheckService? databaseCheck = null,
        IPriceHistoryRepository? repository = null)
    {
        return new MetalsIngestionJob(
            logger ?? new Mock<ILogger<MetalsIngestionJob>>().Object,
            apiClient ?? new Mock<IMetalsApiClient>().Object,
            databaseCheck ?? new Mock<IDatabaseCheckService>().Object,
            repository ?? new Mock<IPriceHistoryRepository>().Object);
    }

    [Fact]
    public async Task RunAsync_WhenDatabaseNotAvailable_DoesNotCallApiOrSave()
    {
        _databaseCheckMock.Setup(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var job = CreateJob(_loggerMock.Object, _apiClientMock.Object, _databaseCheckMock.Object, _repositoryMock.Object);
        await job.RunAsync();

        _databaseCheckMock.Verify(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _databaseCheckMock.Verify(x => x.DataExistsForTodayAsync(It.IsAny<CancellationToken>()), Times.Never);
        _apiClientMock.Verify(x => x.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SavePricesAsync(It.IsAny<IReadOnlyList<DailyPrices>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenDataExistsForToday_DoesNotCallApiOrSave()
    {
        _databaseCheckMock.Setup(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _databaseCheckMock.Setup(x => x.DataExistsForTodayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var job = CreateJob(_loggerMock.Object, _apiClientMock.Object, _databaseCheckMock.Object, _repositoryMock.Object);
        await job.RunAsync();

        _databaseCheckMock.Verify(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _databaseCheckMock.Verify(x => x.DataExistsForTodayAsync(It.IsAny<CancellationToken>()), Times.Once);
        _apiClientMock.Verify(x => x.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SavePricesAsync(It.IsAny<IReadOnlyList<DailyPrices>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenApiReturnsEmpty_DoesNotSave()
    {
        _databaseCheckMock.Setup(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _databaseCheckMock.Setup(x => x.DataExistsForTodayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _apiClientMock.Setup(x => x.GetPricesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DailyPrices>());

        var job = CreateJob(_loggerMock.Object, _apiClientMock.Object, _databaseCheckMock.Object, _repositoryMock.Object);
        await job.RunAsync();

        _apiClientMock.Verify(x => x.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.SavePricesAsync(It.IsAny<IReadOnlyList<DailyPrices>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenApiReturnsPrices_CallsSaveWithSamePrices()
    {
        var prices = new List<DailyPrices>
        {
            new() { Metal = "XAU", Currency = "USD", Price = 2650.50m }
        };
        _databaseCheckMock.Setup(x => x.IsDatabaseAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _databaseCheckMock.Setup(x => x.DataExistsForTodayAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _apiClientMock.Setup(x => x.GetPricesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(prices);

        var job = CreateJob(_loggerMock.Object, _apiClientMock.Object, _databaseCheckMock.Object, _repositoryMock.Object);
        await job.RunAsync();

        _apiClientMock.Verify(x => x.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(
            x => x.SavePricesAsync(It.Is<IReadOnlyList<DailyPrices>>(list => list.Count == 1 && list[0].Metal == "XAU"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
