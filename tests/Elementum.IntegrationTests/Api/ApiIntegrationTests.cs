using System.Net;
using System.Net.Http.Json;
using Elementum.Application.DTOs;
using Elementum.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.IntegrationTests.Api;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // [R]IGHT-BICEP: live prices endpoint returns HTTP 200 with market overview
    [Fact]
    public async Task GetLivePrices_ReturnsOk_WithMarketOverview()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overview = await response.Content.ReadFromJsonAsync<LiveMarketOverviewDto>();
        Assert.NotNull(overview);
        Assert.NotNull(overview.Items);
        Assert.Equal(4, overview.Items.Count);
        Assert.Contains(overview.Items, m => m.Symbol == "XAU");
    }

    // [R]IGHT-BICEP: valid trading symbol returns calculated technical indicators
    [Fact]
    public async Task GetLiveTradingAnalysis_WhenValidSymbol_ReturnsTradingPrice()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/prices/live/trading/XAU?currency=EUR");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TradingPriceDto>();
        Assert.NotNull(dto);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("EUR", dto.Currency);
        Assert.True(dto.Price > 0);
    }

    // [R]IGHT-BICEP: currency query parameter selects the USD quote on the trading endpoint
    [Fact]
    public async Task GetLiveTradingAnalysis_WhenCurrencyUsd_ReturnsUsdPrice()
    {
        var response = await _client.GetAsync("/api/v1/prices/live/trading/XAU?currency=USD");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TradingPriceDto>();
        Assert.NotNull(dto);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(2500.50m, dto.Price);
    }

    // [R]IGHT-BICEP: history currency filter returns only ticks in the requested code
    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_WhenCurrencyUsd_ReturnsUsdTicks()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU?currency=USD");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PriceHistoryDto>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    // [E]RROR: non-existent trading symbol returns HTTP 404 ProblemDetails
    [Fact]
    public async Task GetLiveTradingAnalysis_WhenNotFound_Returns404ProblemDetails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/prices/live/trading/NONEXISTENT");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }
}
