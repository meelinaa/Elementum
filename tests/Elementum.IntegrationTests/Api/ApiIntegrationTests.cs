using System.Net;
using System.Net.Http.Json;
using Elementum.Application.DTOs;
using Elementum.Application.Requests;
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

    // [R]IGHT-BICEP: live prices endpoint returns HTTP 200 with market overview, Cache-Control, and ETag
    [Fact]
    public async Task GetLivePrices_ReturnsOk_WithMarketOverviewAndCachingHeaders()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/prices/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
        Assert.True(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.CacheControl?.MaxAge);

        var overview = await response.Content.ReadFromJsonAsync<LiveMarketOverviewDto>();
        Assert.NotNull(overview);
        Assert.NotNull(overview.Items);
        Assert.Equal(4, overview.Items.Count);
        Assert.Contains(overview.Items, m => m.Symbol == "XAU");
    }

    // RIGHT-B[I]CEP: subsequent request with matching If-None-Match header returns HTTP 304 Not Modified
    [Fact]
    public async Task GetLivePrices_WhenIfNoneMatchMatchesCurrentEtag_Returns304NotModified()
    {
        // Arrange - Initial request to obtain current ETag
        var initialResponse = await _client.GetAsync("/api/v1/prices/live");
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        var etag = initialResponse.Headers.ETag?.ToString();
        Assert.NotNull(etag);

        // Act - Conditional GET with If-None-Match
        using var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/prices/live");
        conditionalRequest.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var conditionalResponse = await _client.SendAsync(conditionalRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotModified, conditionalResponse.StatusCode);
    }

    // [R]IGHT-BICEP: new primary trading route /api/v1/prices/trading/{symbol} returns calculated technical indicators
    [Fact]
    public async Task GetLiveTradingAnalysis_UsingDirectTradingRoute_ReturnsTradingPrice()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/prices/trading/XAU?currency=EUR");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TradingPriceDto>();
        Assert.NotNull(dto);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("EUR", dto.Currency);
        Assert.True(dto.Price > 0);
    }

    // [R]IGHT-BICEP: valid trading symbol returns calculated technical indicators via legacy route
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
        var page = await response.Content.ReadFromJsonAsync<PriceHistoryPageDto>();
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item => Assert.Equal("USD", item.Currency));
        Assert.False(page.HasMore);
        Assert.Equal(page.Items.Count, page.TotalCount);
    }

    // RIGHT-[B]ICEP: take above the hard cap is accepted and the response reports the clamped take
    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_WhenTakeExceedsCap_ReturnsClampedTake()
    {
        var response = await _client.GetAsync($"/api/v1/history/XAU?currency=USD&take={HistoryQueryLimits.MaxTake + 1}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PriceHistoryPageDto>();
        Assert.NotNull(page);
        Assert.Equal(HistoryQueryLimits.MaxTake, page.Take);
    }

    // RIGHT-BIC[E]P: invalid from date is rejected with 400 before the query runs
    [Fact]
    public async Task GetPriceHistoryByMetalSymbol_WhenFromInvalid_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/history/XAU?from=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // RIGHT-BIC[E]P: non-existent trading symbol returns HTTP 404 ProblemDetails
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
        Assert.Equal("Resource not found", problem.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", problem.Type);
    }

    // [R]IGHT-BICEP: X-Correlation-Id supplied by client is mirrored in HTTP response headers
    [Fact]
    public async Task GetLivePrices_WhenCorrelationIdHeaderSupplied_MirrorsInResponse()
    {
        // Arrange
        const string expectedCorrelationId = "test-correlation-trace-12345";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/prices/live");
        request.Headers.Add("X-Correlation-Id", expectedCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.Equal(expectedCorrelationId, response.Headers.GetValues("X-Correlation-Id").First());
    }

    // RIGHT-BIC[E]P: chronological inversion (from > to) fails validation with HTTP 400 ValidationProblemDetails
    [Fact]
    public async Task GetPriceHistory_WhenFromDateAfterToDate_Returns400ValidationProblemDetails()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/history/XAU?from=2026-08-20&to=2026-08-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", problem.Type);
        Assert.True(problem.Errors.ContainsKey(nameof(DateRangeRequest.From)) || problem.Errors.ContainsKey(""));
    }

    // RIGHT-[B]ICEP: sequential pagination requests traverse non-overlapping slices with valid HasMore flags
    [Fact]
    public async Task GetPriceHistory_WithPagination_ReturnsExpectedPagesAndHasMore()
    {
        // Arrange & Act - Fetch Page 1 (skip=0, take=2)
        var resPage1 = await _client.GetAsync("/api/v1/history/XAU?currency=USD&skip=0&take=2");
        Assert.Equal(HttpStatusCode.OK, resPage1.StatusCode);
        var page1 = await resPage1.Content.ReadFromJsonAsync<PriceHistoryPageDto>();
        Assert.NotNull(page1);

        // Arrange & Act - Fetch Page 2 (skip=2, take=2)
        var resPage2 = await _client.GetAsync("/api/v1/history/XAU?currency=USD&skip=2&take=2");
        Assert.Equal(HttpStatusCode.OK, resPage2.StatusCode);
        var page2 = await resPage2.Content.ReadFromJsonAsync<PriceHistoryPageDto>();
        Assert.NotNull(page2);

        // Assert
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.Equal(3, page1.TotalCount);
        Assert.True(page1.HasMore);
        Assert.False(page2.HasMore);

        // Pages must contain disjoint items
        var page1Ids = page1.Items.Select(x => x.Id).ToHashSet();
        var page2Ids = page2.Items.Select(x => x.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }
}
