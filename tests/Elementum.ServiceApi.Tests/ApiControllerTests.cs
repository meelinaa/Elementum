using Elementum.Shared.DTOs;
using Elementum_ServiceApi.Controllers;
using Elementum_ServiceApi.RequestModels;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Elementum.ServiceApi.Tests;

public class ApiControllerTests
{
    private readonly Mock<IApiService> _apiServiceMock = new();
    private readonly ApiController _controller;

    public ApiControllerTests()
    {
        _controller = new ApiController(_apiServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetAllMetals_ReturnsServiceResult()
    {
        var metals = new List<MetalsDto> { new() { Id = 1, Symbol = "XPT", Name = "Platinum" } };
        _apiServiceMock.Setup(s => s.GetAllMetals(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(metals);

        var result = await _controller.GetAllMetals(CancellationToken.None);

        Assert.Same(metals, result);
        _apiServiceMock.Verify(s => s.GetAllMetals(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_ReturnsOkWithData()
    {
        // Arrange
        var request = new SymbolRequest { Symbol = "XPT" };
        var history = new List<PriceHistoryDto>
        {
            new() { Id = 1, MetalId = 3, Currency = "USD", EntryDate = DateOnly.FromDateTime(DateTime.UtcNow) }
        };
        _apiServiceMock.Setup(s => s.GetPriceHistoryByMetalSymbol("XPT", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(history);

        // Act
        var result = await _controller.GetPriceHistoryByMetalSymbol(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(history, ok.Value);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsNotFound_WhenNoData()
    {
        var request = new SymbolRequest { Symbol = "XAU" };
        _apiServiceMock.Setup(s => s.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PriceHistoryDto?)null);

        var actionResult = await _controller.GetPriceHistoryByMetalSymbolLatest(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, details.Status);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsOk_WhenFound()
    {
        var request = new SymbolRequest { Symbol = "XAG" };
        var dto = new PriceHistoryDto { Id = 1, MetalId = 2, Currency = "USD", EntryDate = DateOnly.FromDateTime(DateTime.UtcNow), Price = 24.50m };
        _apiServiceMock.Setup(s => s.GetPriceHistoryByMetalSymbolLatest("XAG", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(dto);

        var actionResult = await _controller.GetPriceHistoryByMetalSymbolLatest(request, CancellationToken.None);

        Assert.Equal(dto, actionResult.Value);
        _apiServiceMock.Verify(s => s.GetPriceHistoryByMetalSymbolLatest("XAG", It.IsAny<CancellationToken>()), Times.Once);
    }

}
