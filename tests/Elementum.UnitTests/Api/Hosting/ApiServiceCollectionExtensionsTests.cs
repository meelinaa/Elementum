using Elementum.Api.Hosting;
using Elementum.Application.Exceptions;
using Elementum.Application.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Elementum.UnitTests.Api.Hosting;

public class ApiServiceCollectionExtensionsTests
{
    // RIGHT-BIC[E]P: missing DefaultConnection connection string causes fail-fast ConfigurationException on startup
    [Fact]
    public void AddElementumApiServices_WhenMissingConnectionString_ThrowsConfigurationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => services.AddElementumApiServices(configuration));
    }

    // [R]IGHT-BICEP: valid configuration registers API controllers, ProblemDetails, and validated Options
    [Fact]
    public void AddElementumApiServices_WhenValidConfiguration_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var environmentMock = new Mock<IWebHostEnvironment>();
        services.AddSingleton(environmentMock.Object);

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Port=1;Database=test;User=root;Password=x;",
            ["MetalsApi:BaseUrl"] = "https://api.edelmetalle.de/public.json",
            ["MetalsApi:Currency"] = "USD",
            ["RateLimiting:PermitLimit"] = "100",
            ["RateLimiting:WindowSeconds"] = "60",
            ["RateLimiting:QueueLimit"] = "0"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        services.AddElementumApiServices(configuration);

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(IConfigureOptions<MetalsApiOptions>));
        Assert.Contains(services, d => d.ServiceType == typeof(IConfigureOptions<RateLimitingOptions>));
    }
}
