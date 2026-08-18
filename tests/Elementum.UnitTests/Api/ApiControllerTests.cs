using Elementum.Api.Inbound.Controllers;
using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Elementum.Api.Tests;

public class ApiControllerTests
{
    private readonly Mock<IGetPriceHistoryUseCase> _priceHistoryUseCaseMock = new();
    private readonly Mock<ILivePricesUseCase> _livePricesUseCaseMock = new();

    private readonly LivePricesController _livePricesController;
    private readonly PriceHistoryController _priceHistoryController;

    public ApiControllerTests()
    {
        var httpContext = new DefaultHttpContext();

        _livePricesController = new LivePricesController(_livePricesUseCaseMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        _priceHistoryController = new PriceHistoryController(_priceHistoryUseCaseMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
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
    public async Task GetLiveTradingAnalysis_ReturnsOk_WhenAnalysisFound()
    {
        var request = new SymbolRequest { Symbol = "XAU" };
        var dto = new TradingPriceDto { Symbol = "XAU", Currency = "EUR", Price = 2300m };
        _livePricesUseCaseMock.Setup(s => s.GetLiveTradingAnalysisAsync("XAU", "EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _livePricesController.GetLiveTradingAnalysis(request, "EUR", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetLiveTradingAnalysis_ReturnsNotFound_WhenNull()
    {
        var request = new SymbolRequest { Symbol = "XYZ" };
        _livePricesUseCaseMock.Setup(s => s.GetLiveTradingAnalysisAsync("XYZ", "EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TradingPriceDto?)null);

        var result = await _livePricesController.GetLiveTradingAnalysis(request, "EUR", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, details.Status);
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
    public async Task GetPriceHistoryByMetalSymbol_WithExplicitCurrency_PassesCurrencyToUseCase()
    {
        var request = new SymbolRequest { Symbol = "XAU" };
        var history = new List<PriceHistoryDto>
        {
            new() { Id = 2, MetalId = 1, Currency = "EUR", EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) }
        };
        _priceHistoryUseCaseMock.Setup(s => s.GetBySymbolAsync("XAU", "EUR", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(history);

        var result = await _priceHistoryController.GetPriceHistoryByMetalSymbol(request, "EUR", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(history, ok.Value);
    }
}
