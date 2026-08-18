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

    [Fact]
    public async Task GetLivePrices_ReturnsOk_WithMarketOverview()
    {
        var response = await _client.GetAsync("/api/v1/prices/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overview = await response.Content.ReadFromJsonAsync<LiveMarketOverviewDto>();
        Assert.NotNull(overview);
        Assert.NotNull(overview.Items);
        Assert.Equal(4, overview.Items.Count);
        Assert.Contains(overview.Items, m => m.Symbol == "XAU");
    }

    [Fact]
    public async Task GetLiveTradingAnalysis_WhenValidSymbol_ReturnsTradingPrice()
    {
        var response = await _client.GetAsync("/api/v1/prices/live/trading/XAU?currency=EUR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TradingPriceDto>();
        Assert.NotNull(dto);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("EUR", dto.Currency);
        Assert.True(dto.Price > 0);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_WhenExists_ReturnsHistoryList()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU?currency=USD");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PriceHistoryDto>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetLiveTradingAnalysis_WhenNotFound_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/prices/live/trading/NONEXISTENT");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }
}
