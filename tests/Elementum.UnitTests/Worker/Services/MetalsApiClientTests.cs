using System.Net;
using System.Text;
using Elementum.Application.Options;
using Elementum.Infrastructure.External;
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

    [Fact]
    public async Task GetEdelmetallePricesAsync_ParsesJsonCorrectly()
    {
        var factory = CreateHttpClientFactory(SampleJson);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        var result = await client.GetEdelmetallePricesAsync();

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

    [Fact]
    public async Task GetPricesAsync_MapsEdelmetalleResponseToDailyPricesList()
    {
        var factory = CreateHttpClientFactory(SampleJson);
        var options = MicrosoftOptions.Create(new MetalsApiOptions { BaseUrl = "https://api.edelmetalle.de/public.json" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(factory, logger, options);

        var result = await client.GetPricesAsync();

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
}
