using Elementum.Application.UseCases.Ingestion;
using Elementum.Application.UseCases.Metals;
using Elementum.Application.UseCases.Prices;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application use cases, services, and FluentValidation validators in the DI container.
    /// </summary>
    public static IServiceCollection AddElementumApplication(this IServiceCollection services)
    {
        services.AddScoped<IIngestPricesUseCase, IngestPricesUseCase>();
        services.AddScoped<IGetPriceHistoryUseCase, GetPriceHistoryUseCase>();
        services.AddScoped<IGetMetalsUseCase, GetMetalsUseCase>();
        services.AddScoped<IGetDailyCandlesUseCase, GetDailyCandlesUseCase>();

        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }
}
