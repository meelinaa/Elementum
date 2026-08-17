using NetArchTest.Rules;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Verifies enterprise naming conventions across Domain, Application, and Api layers.
/// </summary>
public class NamingConventionTests
{
    [Fact]
    public void DomainInterfaces_Should_StartWithI()
    {
        var result = Types.InAssembly(typeof(Elementum.Domain.Ports.IPriceHistoryRepository).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain interfaces must start with 'I': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ApplicationInterfaces_Should_StartWithI()
    {
        var result = Types.InAssembly(typeof(Elementum.Application.UseCases.Ingestion.IIngestPricesUseCase).Assembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application interfaces must start with 'I': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ApiControllers_Should_EndWithController()
    {
        var result = Types.InAssembly(typeof(Elementum.Api.Controllers.ApiController).Assembly)
            .That()
            .ResideInNamespace("Elementum.Api.Controllers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful, $"API controllers must end with 'Controller': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ApplicationDtos_Should_EndWithDto()
    {
        var result = Types.InAssembly(typeof(Elementum.Application.DTOs.MetalsDto).Assembly)
            .That()
            .ResideInNamespace("Elementum.Application.DTOs")
            .Should()
            .HaveNameEndingWith("Dto")
            .GetResult();

        Assert.True(result.IsSuccessful, $"DTOs must end with 'Dto': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void UseCases_Should_EndWithUseCase()
    {
        var result = Types.InAssembly(typeof(Elementum.Application.UseCases.Ingestion.IngestPricesUseCase).Assembly)
            .That()
            .ResideInNamespaceStartingWith("Elementum.Application.UseCases")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("UseCase")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Use cases must end with 'UseCase': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
