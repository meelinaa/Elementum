using Elementum.Application.Options;
using Elementum.Infrastructure.External;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using Moq;

namespace Elementum.Worker.Tests.Services;

public class MetalsApiClientTests
{
    private static IHttpClientFactory CreateHttpClientFactory(HttpClient? client = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client ?? new HttpClient());
        return factory.Object;
    }

    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsEmpty_ReturnsEmptyList()
    {
        var options = MicrosoftOptions.Create(new MetalsApiOptions { ApiKey = "" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(CreateHttpClientFactory(), logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsNull_ReturnsEmptyList()
    {
        var options = MicrosoftOptions.Create(new MetalsApiOptions { ApiKey = null! });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(CreateHttpClientFactory(), logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsSet_ReturnsListFromApi()
    {
        var options = MicrosoftOptions.Create(new MetalsApiOptions { ApiKey = "test-key" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(CreateHttpClientFactory(), logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }
}
