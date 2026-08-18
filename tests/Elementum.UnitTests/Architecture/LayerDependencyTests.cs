using Elementum.Api.Inbound.Controllers;
using NetArchTest.Rules;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Enforces hexagonal architecture dependency boundaries across all solution layers.
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
        var result = Types.InAssembly(typeof(Elementum.Infrastructure.Data.ElementumDbContext).Assembly)
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
        var result = Types.InAssembly(typeof(ApiController).Assembly)
            .That()
            .ResideInNamespace("Elementum.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Elementum.Infrastructure.Data.ElementumDbContext")
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
            .HaveDependencyOn("Elementum.Infrastructure.Data.ElementumDbContext")
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker Jobs must not depend directly on ElementumDbContext (use Application Use Cases instead)");
    }

    [Fact]
    public void PresentationLayers_ShouldNot_DependOn_EachOther()
    {
        // Api and Worker are independent driving adapters and must never reference each other
        var apiResult = Types.InAssembly(typeof(ApiController).Assembly)
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
