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
    public async Task GetAllMetals_ReturnsOk_WithSeededMetals()
    {
        var response = await _client.GetAsync("/api/v1/metals/all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metals = await response.Content.ReadFromJsonAsync<List<MetalsDto>>();
        Assert.NotNull(metals);
        Assert.True(metals.Count >= 4);
        Assert.Contains(metals, m => m.Symbol == "XAU" && m.Name == "Gold");
    }

    [Fact]
    public async Task GetPriceHistoryAllLatest_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/history/all/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PriceHistoryDto>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_WhenExists_ReturnsPriceHistory()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PriceHistoryDto>();
        Assert.NotNull(dto);
        Assert.Equal(2500.50m, dto.Price);
        Assert.Equal("USD", dto.Currency);
    }

    [Fact]
    public async Task GetPriceHistoryTradingLatest_WhenExists_ReturnsTradingPrice()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU/latest/trading");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TradingPriceDto>();
        Assert.NotNull(dto);
        Assert.Equal(2500.50m, dto.Price);
        Assert.Equal(2501.00m, dto.Ask);
        Assert.Equal(2500.00m, dto.Bid);
        Assert.Equal(2510.00m, dto.HighPrice);
    }

    [Fact]
    public async Task GetPriceHistoryKaratLatest_WhenExists_ReturnsKaratPrices()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU/latest/karat");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<KaratPricesDto>();
        Assert.NotNull(dto);
        Assert.Equal(80.40m, dto.PriceGram24k);
        Assert.Equal(60.30m, dto.PriceGram18k);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_WhenNotFound_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/history/NONEXISTENT/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task GetPriceHistoryByDateRange_WhenStartAfterEnd_Returns400ValidationProblem()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU/2024-01-20/2024-01-10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }
}
