using Elementum.Api.Inbound.Controllers;
using Elementum.Application.Inbound.UseCases.Ingestion;
using NetArchTest.Rules;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Verifies enterprise naming conventions across Domain, Application, and Api layers.
/// NetArchTest structural rules — not categorized under RIGHT-BICEP behavioral tests.
/// </summary>
public class NamingConventionTests
{
    [Fact]
    public void DomainInterfaces_Should_StartWithI()
    {
        var result = Types.InAssembly(typeof(Elementum.Domain.Ports.Outbound.IPriceHistoryRepository).Assembly)
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
        var result = Types.InAssembly(typeof(IIngestPricesUseCase).Assembly)
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
        var result = Types.InAssembly(typeof(LivePricesController).Assembly)
            .That()
            .ResideInNamespace("Elementum.Api.Inbound.Controllers")
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
        var result = Types.InAssembly(typeof(IngestPricesUseCase).Assembly)
            .That()
            .ResideInNamespaceStartingWith("Elementum.Application.Inbound.UseCases")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("UseCase")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Use cases must end with 'UseCase': {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    /// <summary>
    /// Production lock: every public *Controller in Elementum.Api must live in Inbound.Controllers.
    /// Types are selected by name so a controller in the legacy namespace cannot hide from the filter.
    /// </summary>
    [Fact]
    public void ApiControllers_MustResideIn_InboundControllers()
    {
        var api = typeof(LivePricesController).Assembly;
        var controllers = InboundNamespaceRules.Controllers(api);

        Assert.True(
            controllers.Count > 0,
            "Expected at least one public *Controller type in Elementum.Api; an empty set would make controller architecture rules vacuous.");

        var misplaced = InboundNamespaceRules.MisplacedControllers(api);
        Assert.True(
            misplaced.Count == 0,
            $"API controllers must reside in {InboundNamespaceRules.ApiInboundControllers}: {string.Join(", ", misplaced)}");
    }

    /// <summary>
    /// Negative lock: the same predicate must report a controller that lives in the legacy namespace.
    /// This test is green when the violation is detected — that failure is the intended outcome.
    /// </summary>
    [Fact]
    public void LegacyApiControllerNamespace_IsReportedAsMisplaced()
    {
        var misplaced = InboundNamespaceRules.MisplacedControllers(
            typeof(Elementum.Api.Controllers.LegacyNamespaceProbeController).Assembly);

        Assert.Contains(typeof(Elementum.Api.Controllers.LegacyNamespaceProbeController).FullName, misplaced);
    }

    /// <summary>
    /// Production lock: Application *UseCase types must live under Inbound.UseCases.
    /// </summary>
    [Fact]
    public void ApplicationUseCases_MustResideIn_InboundUseCases()
    {
        var application = typeof(IngestPricesUseCase).Assembly;
        var useCases = InboundNamespaceRules.UseCases(application);

        Assert.True(
            useCases.Count > 0,
            "Expected at least one public *UseCase type in Elementum.Application; an empty set would make use-case architecture rules vacuous.");

        var misplaced = InboundNamespaceRules.MisplacedUseCases(application);
        Assert.True(
            misplaced.Count == 0,
            $"Application use cases must reside under {InboundNamespaceRules.ApplicationInboundUseCases}: {string.Join(", ", misplaced)}");
    }

    /// <summary>
    /// Negative lock: the same predicate must report a use case that lives in the legacy namespace.
    /// This test is green when the violation is detected — that failure is the intended outcome.
    /// </summary>
    [Fact]
    public void LegacyApplicationUseCaseNamespace_IsReportedAsMisplaced()
    {
        var misplaced = InboundNamespaceRules.MisplacedUseCases(
            typeof(Elementum.Application.UseCases.LegacyNamespaceProbeUseCase).Assembly);

        Assert.Contains(typeof(Elementum.Application.UseCases.LegacyNamespaceProbeUseCase).FullName, misplaced);
    }
}
