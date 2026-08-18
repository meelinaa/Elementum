using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Inbound.UseCases.Metals;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Services;
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
        services.AddMemoryCache();
        services.AddScoped<ILiveQuotesProvider, LiveQuotesProvider>();
        services.AddScoped<IIngestPricesUseCase, IngestPricesUseCase>();
        services.AddScoped<IGetPriceHistoryUseCase, GetPriceHistoryUseCase>();
        services.AddScoped<IGetMetalsUseCase, GetMetalsUseCase>();
        services.AddScoped<IGetDailyCandlesUseCase, GetDailyCandlesUseCase>();
        services.AddScoped<ILivePricesUseCase, LivePricesUseCase>();

        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }
}
