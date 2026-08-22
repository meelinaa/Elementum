using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory that simulates upstream API returning empty live quote data (null response).
/// Inherits from <see cref="CustomWebApplicationFactory"/> to reuse isolated configuration and database fixtures.
/// </summary>
public class UpstreamFailureWebApplicationFactory : CustomWebApplicationFactory
{
    /// <summary>
    /// Configures the web host by inheriting base test setup and substituting the external metals API client
    /// with a mock returning null to test upstream failure exception mapping (HTTP 502).
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var mockMetalsApi = new Mock<IMetalsApiClient>();
            mockMetalsApi.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((EdelmetalleApiResponse?)null);

            services.RemoveAll<IMetalsApiClient>();
            services.AddSingleton(mockMetalsApi.Object);
        });
    }
}
