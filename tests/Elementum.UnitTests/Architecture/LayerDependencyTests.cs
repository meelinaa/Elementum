using Elementum.Api.Inbound.Controllers;
using NetArchTest.Rules;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Enforces hexagonal architecture dependency boundaries across all solution layers.
/// NetArchTest structural rules — not categorized under RIGHT-BICEP behavioral tests.
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "Elementum.Domain";
    private const string ApplicationNamespace = "Elementum.Application";
    private const string InfrastructureNamespace = "Elementum.Infrastructure";
    private const string ApiNamespace = "Elementum.Api";
    private const string WorkerNamespace = "Elementum.Worker";
    private const string CliNamespace = "Elementum.Cli";

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOn_OtherProjects()
    {
        // Domain is the pure core and must never reference any outer layer
        var result = Types.InAssembly(typeof(Elementum.Domain.Entities.Metals).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace,
                CliNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Domain layer has illegal outer dependencies: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_ShouldNot_HaveDependencyOn_EntityFrameworkCore()
    {
        // Domain core must stay pure and free from ORM frameworks
        var result = Types.InAssembly(typeof(Elementum.Domain.Entities.Metals).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must not depend on Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void DomainPorts_ShouldNot_ExposeIQueryable()
    {
        // IQueryable on a Domain port leaks LINQ/EF composition into Application.
        var queryableMethods = typeof(Elementum.Domain.Ports.Outbound.IPriceHistoryReadRepository).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.Namespace is not null && t.Namespace.StartsWith("Elementum.Domain.Ports", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods())
            .Where(m => typeof(IQueryable).IsAssignableFrom(m.ReturnType))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.True(
            queryableMethods.Count == 0,
            $"Domain ports must not return IQueryable: {string.Join(", ", queryableMethods)}");
    }

    [Fact]
    public void Application_ShouldNot_HaveDependencyOn_InfrastructureOrPresentation()
    {
        // Application layer only depends on Domain, never on Infrastructure or Presentation (Api/Worker/Cli)
        var result = Types.InAssembly(typeof(Elementum.Application.DTOs.MetalsDto).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace,
                WorkerNamespace,
                CliNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application layer has illegal outer dependencies: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_ShouldNot_HaveDependencyOn_EntityFrameworkCore()
    {
        // Application layer operates on Domain abstractions/Ports, not EF Core directly
        var result = Types.InAssembly(typeof(Elementum.Application.DTOs.MetalsDto).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer must not depend on Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Infrastructure_ShouldNot_HaveDependencyOn_PresentationLayers()
    {
        // Infrastructure implements Driven Ports for Domain/Application and must not depend on Api, Worker, or Cli
        var result = Types.InAssembly(typeof(Elementum.Infrastructure.Outbound.Data.ElementumDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApiNamespace,
                WorkerNamespace,
                CliNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Infrastructure layer has illegal dependencies on Presentation: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ApiControllers_ShouldNot_HaveDirectDependencyOn_DbContext()
    {
        // Controllers in Elementum.Api must consume Application Use Cases, not DbContext directly
        var result = Types.InAssembly(typeof(LivePricesController).Assembly)
            .That()
            .ResideInNamespace("Elementum.Api.Inbound.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Elementum.Infrastructure.Outbound.Data.ElementumDbContext")
            .GetResult();

        Assert.True(result.IsSuccessful, "API Controllers must not depend directly on ElementumDbContext (use Application Use Cases instead)");
    }

    [Fact]
    public void WorkerJobs_ShouldNot_HaveDirectDependencyOn_DbContext()
    {
        // Jobs in Elementum.Worker must consume Application Use Cases, not DbContext directly
        var result = Types.InAssembly(typeof(Elementum.Worker.Jobs.MetalsIngestionJob).Assembly)
            .That()
            .ResideInNamespace("Elementum.Worker.Jobs")
            .ShouldNot()
            .HaveDependencyOn("Elementum.Infrastructure.Outbound.Data.ElementumDbContext")
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker Jobs must not depend directly on ElementumDbContext (use Application Use Cases instead)");
    }

    [Fact]
    public void PresentationLayers_ShouldNot_DependOn_EachOther()
    {
        // Api and Worker are independent driving adapters and must never reference each other
        var apiResult = Types.InAssembly(typeof(LivePricesController).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(WorkerNamespace, CliNamespace)
            .GetResult();

        Assert.True(apiResult.IsSuccessful, "API project must not depend on Worker or Cli");

        var workerResult = Types.InAssembly(typeof(Elementum.Worker.Jobs.MetalsIngestionJob).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, CliNamespace)
            .GetResult();

        Assert.True(workerResult.IsSuccessful, "Worker project must not depend on Api or Cli");
    }
}
