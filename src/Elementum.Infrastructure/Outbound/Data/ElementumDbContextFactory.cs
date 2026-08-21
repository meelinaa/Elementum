using Elementum.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Elementum.Infrastructure.Outbound.Data;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>.
/// Uses the same <c>ConnectionStrings:DefaultConnection</c> as the API (appsettings / env).
/// </summary>
public sealed class ElementumDbContextFactory : IDesignTimeDbContextFactory<ElementumDbContext>
{
    public ElementumDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveContentRoot())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw ConfigurationException.MissingConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new ElementumDbContext(options);
    }

    private static string ResolveContentRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (HasAppSettings(current.FullName))
                return current.FullName;

            var apiPath = Path.Combine(current.FullName, "src", "Elementum.Api");
            if (HasAppSettings(apiPath))
                return apiPath;

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool HasAppSettings(string directory) =>
        File.Exists(Path.Combine(directory, "appsettings.json"))
        || File.Exists(Path.Combine(directory, "appsettings.Development.json"));
}
