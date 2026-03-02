using Elementum.Shared.Objects;
using Elementum_ServiceApi.Controllers;
using Elementum_ServiceApi.Models;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Elementum.ServiceApi.Tests;


public class FrontendServiceControllerTests
{
    private readonly Mock<IApiService> _apiServiceMock = new();
    private readonly FrontendService _controller;

    public FrontendServiceControllerTests()
    {
        _controller = new FrontendService(_apiServiceMock.Object)
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
        // Arrange
        var metals = new List<Metals> { new() { Id = 1, Symbol = "XPT", Name = "Platinum" } };
        _apiServiceMock.Setup(s => s.GetAllMetals(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(metals);

        // Act
        var result = await _controller.GetAllMetals(CancellationToken.None);

        // Assert
        Assert.Same(metals, result);
        _apiServiceMock.Verify(s => s.GetAllMetals(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMetalBySymbol_ReturnsNotFound_WhenMetalIsNull()
    {
        // Arrange
        var request = new SymbolRequest { Symbol = "XAU" };
        _apiServiceMock.Setup(s => s.GetMetalBySymbol("XAU", It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Metals?)null);

        // Act
        var actionResult = await _controller.GetMetalBySymbol(request, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, details.Status);
    }

    [Fact]
    public async Task GetMetalBySymbol_ReturnsMetal_WhenFound()
    {
        // Arrange
        var request = new SymbolRequest { Symbol = "XAG" };
        var metal = new Metals { Id = 2, Symbol = "XAG", Name = "Silver" };
        _apiServiceMock.Setup(s => s.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(metal);

        // Act
        var actionResult = await _controller.GetMetalBySymbol(request, CancellationToken.None);

        // Assert
        Assert.Equal(metal, actionResult.Value);
        _apiServiceMock.Verify(s => s.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_ReturnsOkWithData()
    {
        // Arrange
        var request = new SymbolRequest { Symbol = "XPT" };
        var history = new List<PriceHistory>
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
    public async Task GetPriceHistoryAllByDateRange_ParsesDatesAndDelegatesToService()
    {
        // Arrange
        var range = new DateRangeRequest { FirstDate = "2025-01-01", LastDate = "2025-01-31" };
        var history = new List<PriceHistory>();

        _apiServiceMock.Setup(s => s.GetPriceHistoryAllByDateRange(
                               It.IsAny<DateOnly>(),
                               It.IsAny<DateOnly>(),
                               It.IsAny<CancellationToken>()))
                       .ReturnsAsync(history);

        // Act
        var result = await _controller.GetPriceHistoryAllByDateRange(range, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(history, ok.Value);
        _apiServiceMock.Verify(s => s.GetPriceHistoryAllByDateRange(
                                   It.Is<DateOnly>(d => d.Year == 2025 && d.Month == 1 && d.Day == 1),
                                   It.Is<DateOnly>(d => d.Year == 2025 && d.Month == 1 && d.Day == 31),
                                   It.IsAny<CancellationToken>()),
                               Times.Once);
    }
}
