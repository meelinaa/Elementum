using Elementum.Api.Inbound.Controllers;
using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Metals;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Elementum.Api.Tests;

public class ApiControllerTests
{
    private readonly Mock<IGetPriceHistoryUseCase> _priceHistoryUseCaseMock = new();
    private readonly Mock<IGetMetalsUseCase> _metalsUseCaseMock = new();
    private readonly Mock<IGetDailyCandlesUseCase> _dailyCandlesUseCaseMock = new();
    private readonly Mock<ILivePricesUseCase> _livePricesUseCaseMock = new();

    private readonly MetalsController _metalsController;
    private readonly LivePricesController _livePricesController;
    private readonly PriceHistoryController _priceHistoryController;

    public ApiControllerTests()
    {
        var httpContext = new DefaultHttpContext();

        _metalsController = new MetalsController(_metalsUseCaseMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        _livePricesController = new LivePricesController(_livePricesUseCaseMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        _priceHistoryController = new PriceHistoryController(
            _priceHistoryUseCaseMock.Object,
            _dailyCandlesUseCaseMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task GetAllMetals_ReturnsServiceResult()
    {
        var metals = new List<MetalsDto> { new() { Id = 1, Symbol = "XPT", Name = "Platinum" } };
        _metalsUseCaseMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(metals);

        var result = await _metalsController.GetAllMetals(CancellationToken.None);

        Assert.Same(metals, result);
        _metalsUseCaseMock.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLivePrices_ReturnsOkWithOverview()
    {
        var overview = new LiveMarketOverviewDto();
        _livePricesUseCaseMock.Setup(s => s.GetLiveMarketOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        var result = await _livePricesController.GetLivePrices(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(overview, ok.Value);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_ReturnsOkWithData()
    {
        var request = new SymbolRequest { Symbol = "XPT" };
        var history = new List<PriceHistoryDto>
        {
            new() { Id = 1, MetalId = 3, Currency = "USD", EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) }
        };
        _priceHistoryUseCaseMock.Setup(s => s.GetBySymbolAsync("XPT", null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(history);

        var result = await _priceHistoryController.GetPriceHistoryByMetalSymbol(request, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(history, ok.Value);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsNotFound_WhenNoData()
    {
        var request = new SymbolRequest { Symbol = "XAU" };
        _priceHistoryUseCaseMock.Setup(s => s.GetLatestBySymbolAsync("XAU", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PriceHistoryDto?)null);

        var actionResult = await _priceHistoryController.GetPriceHistoryByMetalSymbolLatest(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, details.Status);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsOk_WhenFound()
    {
        var request = new SymbolRequest { Symbol = "XAG" };
        var dto = new PriceHistoryDto { Id = 1, MetalId = 2, Currency = "USD", EntryDate = DateOnly.FromDateTime(DateTime.UtcNow), Price = 24.50m };
        _priceHistoryUseCaseMock.Setup(s => s.GetLatestBySymbolAsync("XAG", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(dto);

        var actionResult = await _priceHistoryController.GetPriceHistoryByMetalSymbolLatest(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(dto, ok.Value);
        _priceHistoryUseCaseMock.Verify(s => s.GetLatestBySymbolAsync("XAG", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDailyCandles_ReturnsOkWithSummaries()
    {
        var request = new SymbolRequest { Symbol = "XAU" };
        var list = new List<DailyPriceSummaryDto>
        {
            new()
            {
                Id = 1,
                MetalId = 1,
                Symbol = "XAU",
                MetalName = "Gold",
                Currency = "USD",
                EntryDate = new DateOnly(2026, 8, 17),
                OpenPrice = 4400m,
                HighPrice = 4450m,
                LowPrice = 4390m,
                ClosePrice = 4410m,
                ExchangeRateUsdEur = 1.15m
            }
        };

        _dailyCandlesUseCaseMock
            .Setup(u => u.ExecuteAsync("XAU", "USD", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Elementum.Domain.Common.Result.Success<IReadOnlyList<DailyPriceSummaryDto>>(list));

        var result = await _priceHistoryController.GetDailyCandles(request, "USD", null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(list, ok.Value);
    }
}
