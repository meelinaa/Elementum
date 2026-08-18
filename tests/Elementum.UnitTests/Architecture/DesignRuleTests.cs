using Elementum.Api.Inbound.Controllers;
using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Inbound.UseCases.Metals;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Verifies object-oriented design and encapsulation rules.
/// </summary>
public class DesignRuleTests
{
    [Fact]
    public void ApiControllers_Should_InheritFromControllerBase()
    {
        var result = Types.InAssembly(typeof(MetalsController).Assembly)
            .That()
            .ResideInNamespace("Elementum.Api.Controllers")
            .And()
            .AreClasses()
            .Should()
            .Inherit(typeof(ControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful, $"API controllers must inherit from ControllerBase: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void DomainEntities_Should_BePublicClasses()
    {
        var result = Types.InAssembly(typeof(Elementum.Domain.Entities.Metals).Assembly)
            .That()
            .ResideInNamespace("Elementum.Domain.Entities")
            .Should()
            .BePublic()
            .And()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain entities must be public classes: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void DomainPorts_Should_BePublicInterfaces()
    {
        var result = Types.InAssembly(typeof(Elementum.Domain.Ports.Outbound.IPriceHistoryRepository).Assembly)
            .That()
            .ResideInNamespaceStartingWith("Elementum.Domain.Ports")
            .Should()
            .BePublic()
            .And()
            .BeInterfaces()
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain ports must be public interfaces: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void IngestPricesUseCase_Should_Implement_IIngestPricesUseCase()
    {
        var result = Types.InAssembly(typeof(IngestPricesUseCase).Assembly)
            .That()
            .HaveName("IngestPricesUseCase")
            .Should()
            .ImplementInterface(typeof(IIngestPricesUseCase))
            .GetResult();

        Assert.True(result.IsSuccessful, "IngestPricesUseCase must implement IIngestPricesUseCase");
    }

    [Fact]
    public void GetPriceHistoryUseCase_Should_Implement_IGetPriceHistoryUseCase()
    {
        var result = Types.InAssembly(typeof(GetPriceHistoryUseCase).Assembly)
            .That()
            .HaveName("GetPriceHistoryUseCase")
            .Should()
            .ImplementInterface(typeof(IGetPriceHistoryUseCase))
            .GetResult();

        Assert.True(result.IsSuccessful, "GetPriceHistoryUseCase must implement IGetPriceHistoryUseCase");
    }

    [Fact]
    public void GetMetalsUseCase_Should_Implement_IGetMetalsUseCase()
    {
        var result = Types.InAssembly(typeof(GetMetalsUseCase).Assembly)
            .That()
            .HaveName("GetMetalsUseCase")
            .Should()
            .ImplementInterface(typeof(IGetMetalsUseCase))
            .GetResult();

        Assert.True(result.IsSuccessful, "GetMetalsUseCase must implement IGetMetalsUseCase");
    }

    [Fact]
    public void CachedGetPriceHistoryUseCase_Should_Implement_IGetPriceHistoryUseCase()
    {
        var result = Types.InAssembly(typeof(Elementum.Infrastructure.Caching.CachedGetPriceHistoryUseCase).Assembly)
            .That()
            .HaveName("CachedGetPriceHistoryUseCase")
            .Should()
            .ImplementInterface(typeof(IGetPriceHistoryUseCase))
            .GetResult();

        Assert.True(result.IsSuccessful, "CachedGetPriceHistoryUseCase must implement IGetPriceHistoryUseCase");
    }
}
