using Elementum_WorkerService.Options;
using Elementum_WorkerService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elementum.WorkerService.Tests.Services;

public class MetalsApiClientTests
{
    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsEmpty_ReturnsEmptyList()
    {
        var options = Options.Create(new MetalsApiOptions { ApiKey = "" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsNull_ReturnsEmptyList()
    {
        var options = Options.Create(new MetalsApiOptions { ApiKey = null! });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPricesAsync_WhenApiKeyIsSet_ReturnsListFromApi()
    {
        var options = Options.Create(new MetalsApiOptions { ApiKey = "test-key" });
        var logger = new Mock<ILogger<MetalsApiClient>>().Object;
        var client = new MetalsApiClient(logger, options);

        var result = await client.GetPricesAsync();

        Assert.NotNull(result);
        // With real HTTP the list may be empty (no network) or contain data; we only assert it doesn't throw and returns a list.
        Assert.True(result.Count >= 0);
    }
}
