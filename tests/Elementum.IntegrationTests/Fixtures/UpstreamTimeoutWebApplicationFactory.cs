using Elementum.Application.Ports.Outbound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory that simulates upstream API timeout failures.
/// Inherits from <see cref="CustomWebApplicationFactory"/> to reuse isolated configuration and database fixtures.
/// </summary>
public class UpstreamTimeoutWebApplicationFactory : CustomWebApplicationFactory
{
    /// <summary>
    /// Configures the web host by inheriting base test setup and substituting the external metals API client
    /// with a mock throwing a <see cref="TimeoutException"/> to test timeout exception mapping (HTTP 504).
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var mockMetalsApi = new Mock<IMetalsApiClient>();
            mockMetalsApi.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("upstream request timed out"));

            services.RemoveAll<IMetalsApiClient>();
            services.AddSingleton(mockMetalsApi.Object);
        });
    }
}
