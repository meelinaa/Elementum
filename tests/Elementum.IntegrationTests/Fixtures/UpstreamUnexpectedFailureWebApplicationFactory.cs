using Elementum.Application.Ports.Outbound;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Elementum.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory that simulates an unexpected upstream failure mapped to HTTP 500.
/// Inherits from <see cref="CustomWebApplicationFactory"/> to reuse isolated configuration and database fixtures.
/// </summary>
public class UpstreamUnexpectedFailureWebApplicationFactory : CustomWebApplicationFactory
{
    /// <summary>
    /// Configures the web host by inheriting base test setup and substituting the external metals API client
    /// with a mock throwing an unhandled exception to test 500 Internal Server Error problem details mapping.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var mockMetalsApi = new Mock<IMetalsApiClient>();
            mockMetalsApi.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("unexpected upstream bug"));

            services.RemoveAll<IMetalsApiClient>();
            services.AddSingleton(mockMetalsApi.Object);
        });
    }
}
