using System.Net;
using System.Text;
using Elementum.Cli.Api;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elementum.Cli.Tests;

public class HttpCallTests
{
    private const string LiveOverviewJson = """
        {
          "items": [
            {
              "symbol": "XAU",
              "name": "Gold",
              "priceUsd": 2400.5,
              "priceEur": 2200.1
            }
          ],
          "exchangeRateUsdEur": 1.09,
          "timestamp": 1700000000,
          "lastUpdatedAtLocal": "2026-08-20T12:00:00"
        }
        """;

    // [R]IGHT-BICEP: live overview JSON is deserialized from the injected HttpClient
    [Fact]
    public async Task GetLiveMarketOverviewAsync_DeserializesPayload()
    {
        var handler = new StubHttpMessageHandler(LiveOverviewJson);
        using var sut = CreateSut(handler);

        var result = await sut.Client.GetLiveMarketOverviewAsync();

        Assert.NotNull(result);
        Assert.Equal(1.09m, result.ExchangeRateUsdEur);
        Assert.Equal(1700000000, result.Timestamp);
        var item = Assert.Single(result.Items);
        Assert.Equal("XAU", item.Symbol);
        Assert.Equal("Gold", item.Name);
        Assert.Equal(2400.5m, item.PriceUsd);
        Assert.Equal(2200.1m, item.PriceEur);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("/prices/live", handler.LastRequestUri?.AbsolutePath);
    }

    // [R]IGHT-BICEP: second identical call is served from memory cache and does not hit the handler
    [Fact]
    public async Task GetLiveMarketOverviewAsync_SecondCall_UsesCache()
    {
        var handler = new StubHttpMessageHandler(LiveOverviewJson);
        using var sut = CreateSut(handler);

        var first = await sut.Client.GetLiveMarketOverviewAsync();
        var second = await sut.Client.GetLiveMarketOverviewAsync();

        Assert.Same(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    // [I]NVERSE RIGHT-BICEP: ClearCache drops entries so the next call hits the handler again
    [Fact]
    public async Task ClearCache_ForcesSubsequentRequest()
    {
        var handler = new StubHttpMessageHandler(LiveOverviewJson);
        using var sut = CreateSut(handler);

        await sut.Client.GetLiveMarketOverviewAsync();
        sut.Client.ClearCache();
        await sut.Client.GetLiveMarketOverviewAsync();

        Assert.Equal(2, handler.CallCount);
    }

    // [E]RROR RIGHT-BICEP: non-success status is swallowed by the public API and returns null
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenNotSuccess_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler("{}", HttpStatusCode.InternalServerError);
        using var sut = CreateSut(handler);

        var result = await sut.Client.GetLiveMarketOverviewAsync();

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }

    // [E]RROR RIGHT-BICEP: HttpClient.Timeout cancels a slow handler; the public method returns null
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenHttpClientTimesOut_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(LiveOverviewJson)
        {
            Delay = TimeSpan.FromSeconds(30)
        };
        using var sut = CreateSut(handler, timeout: TimeSpan.FromMilliseconds(100));

        var result = await sut.Client.GetLiveMarketOverviewAsync();

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }

    // [C]ROSS-CHECK RIGHT-BICEP: caller CancellationToken is forwarded to SendAsync (cancel returns null quickly)
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenCallerCancels_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(LiveOverviewJson)
        {
            Delay = TimeSpan.FromSeconds(30)
        };
        using var sut = CreateSut(handler, timeout: TimeSpan.FromMinutes(1));
        using var cts = new CancellationTokenSource();

        var request = sut.Client.GetLiveMarketOverviewAsync(cts.Token);
        SpinWait.SpinUntil(() => handler.CallCount > 0, TimeSpan.FromSeconds(2));
        cts.Cancel();

        var result = await request;

        Assert.Null(result);
        Assert.True(handler.SawCancellation);
    }

    private static Sut CreateSut(StubHttpMessageHandler handler, TimeSpan? timeout = null)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/"),
            Timeout = timeout ?? HttpCall.DefaultTimeout
        };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new HttpCall(http, cache, NullLogger<HttpCall>.Instance);
        return new Sut(client, http, cache);
    }

    private sealed class Sut : IDisposable
    {
        public Sut(HttpCall client, HttpClient http, MemoryCache cache)
        {
            Client = client;
            _http = http;
            _cache = cache;
        }

        public HttpCall Client { get; }

        public void Dispose()
        {
            _http.Dispose();
            _cache.Dispose();
        }

        private readonly HttpClient _http;
        private readonly MemoryCache _cache;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _statusCode;

        public StubHttpMessageHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _json = json;
            _statusCode = statusCode;
        }

        public TimeSpan Delay { get; init; }
        public int CallCount { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public bool SawCancellation { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestUri = request.RequestUri;

            if (Delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(Delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    SawCancellation = true;
                    throw;
                }
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
        }
    }
}
