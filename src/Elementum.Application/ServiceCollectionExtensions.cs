using Elementum.Application.UseCases.Ingestion;
using Elementum.Application.UseCases.Metals;
using Elementum.Application.UseCases.Prices;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application use cases and services in the DI container.
    /// </summary>
    public static IServiceCollection AddElementumApplication(this IServiceCollection services)
    {
        services.AddScoped<IIngestPricesUseCase, IngestPricesUseCase>();
        services.AddScoped<IGetPriceHistoryUseCase, GetPriceHistoryUseCase>();
        services.AddScoped<IGetMetalsUseCase, GetMetalsUseCase>();

        return services;
    }
}
