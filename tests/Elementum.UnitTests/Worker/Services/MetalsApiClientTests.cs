using System.Net;
using System.Text;
using System.Text.Json;
using Elementum.Application.Models;
using Elementum.Application.Options;
using Elementum.Infrastructure.Outbound.External;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using Moq;
using Moq.Protected;

namespace Elementum.Worker.Tests.Services;

public class MetalsApiClientTests
{
    private const string SampleJson = """
    {
      "gold_usd": 4410.6,
      "gold_eur": 3803.8,
      "silber_usd": 65.7105,
      "silber_eur": 56.675,
      "platin_usd": 1773.5,
      "platin_eur": 1529.59,
      "palladium_usd": 1334,
      "palladium_eur": 1150.53,
      "timestamp": 1786975085,
      "wechselkurs_usd_eur": 1.15952468584048
    }
    """;

    private static IHttpClientFactory CreateHttpClientFactory(string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factory.Object;
    }

    // [R]IGHT-BICEP: Verifies that valid JSON payload is correctly deserialized via HTTP streaming
    [Fact]
    public async Task GetEdelmetallePricesAsync_ParsesJsonCorrectly()
    {
        // Arrange
        var factory = CreateHttpClientFactory(SampleJson);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act
        var result = await client.GetEdelmetallePricesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4410.6m, result.GoldUsd);
        Assert.Equal(3803.8m, result.GoldEur);
        Assert.Equal(65.7105m, result.SilberUsd);
        Assert.Equal(56.675m, result.SilberEur);
        Assert.Equal(1773.5m, result.PlatinUsd);
        Assert.Equal(1529.59m, result.PlatinEur);
        Assert.Equal(1334m, result.PalladiumUsd);
        Assert.Equal(1150.53m, result.PalladiumEur);
        Assert.Equal(1786975085, result.Timestamp);
        Assert.Equal(1.15952468584048m, result.WechselkursUsdEur);
    }

    // [R]IGHT-BICEP: Verifies that GetPricesAsync maps the 4 metals in USD and EUR (8 entries total)
    [Fact]
    public async Task GetPricesAsync_MapsEdelmetalleResponseToDailyPricesList()
    {
        // Arrange
        var factory = CreateHttpClientFactory(SampleJson);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act
        var result = await client.GetPricesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Count); // 4 metals x 2 currencies (USD, EUR)
        Assert.Contains(result, p => p.Metal == "Gold" && p.Currency == "USD" && p.Price == 4410.6m);
        Assert.Contains(result, p => p.Metal == "Gold" && p.Currency == "EUR" && p.Price == 3803.8m);
        Assert.Contains(result, p => p.Metal == "Silver" && p.Currency == "USD" && p.Price == 65.7105m);
        Assert.Contains(result, p => p.Metal == "Silver" && p.Currency == "EUR" && p.Price == 56.675m);
        Assert.Contains(result, p => p.Metal == "Platinum" && p.Currency == "USD" && p.Price == 1773.5m);
        Assert.Contains(result, p => p.Metal == "Platinum" && p.Currency == "EUR" && p.Price == 1529.59m);
        Assert.Contains(result, p => p.Metal == "Palladium" && p.Currency == "USD" && p.Price == 1334m);
        Assert.Contains(result, p => p.Metal == "Palladium" && p.Currency == "EUR" && p.Price == 1150.53m);
    }

    // [B]OUNDARY: Verifies handling of minimal empty JSON object with default values
    [Fact]
    public async Task GetEdelmetallePricesAsync_EmptyJsonObject_DeserializesWithDefaultValues()
    {
        // Arrange
        var factory = CreateHttpClientFactory("{}");
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act
        var result = await client.GetEdelmetallePricesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.GoldUsd);
        Assert.Equal(0m, result.GoldEur);
    }

    // [E]RROR: Verifies that HTTP 500 server error propagates HttpRequestException
    [Fact]
    public async Task GetEdelmetallePricesAsync_WhenHttp500ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var factory = CreateHttpClientFactory("Internal Server Error", HttpStatusCode.InternalServerError);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetEdelmetallePricesAsync());
    }

    // [E]RROR: Verifies that HTTP 404 Not Found propagates HttpRequestException
    [Fact]
    public async Task GetEdelmetallePricesAsync_WhenHttp404NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        var factory = CreateHttpClientFactory("Not Found", HttpStatusCode.NotFound);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetEdelmetallePricesAsync());
    }

    // [E]RROR: Verifies that malformed non-JSON payload throws JsonException
    [Fact]
    public async Task GetEdelmetallePricesAsync_WhenMalformedJson_ThrowsJsonException()
    {
        // Arrange
        var factory = CreateHttpClientFactory("<html><body>Bad Gateway</body></html>");
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => client.GetEdelmetallePricesAsync());
    }

    // [E]RROR: Verifies that cancellation token abort throws OperationCanceledException
    [Fact]
    public async Task GetEdelmetallePricesAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var factory = CreateHttpClientFactory(SampleJson);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetEdelmetallePricesAsync(cts.Token));
    }
}
