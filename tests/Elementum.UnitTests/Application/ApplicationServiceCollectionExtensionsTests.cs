using Elementum.Application;
using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Elementum.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.UnitTests.Application;

public class ApplicationServiceCollectionExtensionsTests
{
    // [R]IGHT-BICEP: AddElementumApplication registers all primary use cases and FluentValidation validators
    [Fact]
    public void AddElementumApplication_RegistersExpectedServicesAndValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElementumApplication();

        // Assert - verify registrations in service collection
        Assert.Contains(services, d => d.ServiceType == typeof(ILiveQuotesProvider) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(IIngestPricesUseCase) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(IGetPriceHistoryUseCase) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(ILivePricesUseCase) && d.Lifetime == ServiceLifetime.Scoped);

        // FluentValidation validators registered
        Assert.Contains(services, d => d.ServiceType == typeof(IValidator<SymbolRequest>));
        Assert.Contains(services, d => d.ServiceType == typeof(IValidator<DateRangeRequest>));
        Assert.Contains(services, d => d.ServiceType == typeof(IValidator<CurrencyRequest>));
    }
}
