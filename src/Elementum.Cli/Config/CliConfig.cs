using Elementum.Cli.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Config;

/// <summary>CLI configuration. Base URL: env ELEMENTUM_API_BASEURL overrides appsettings.json.</summary>
public static class CliConfig
{
    private const string DefaultBaseUrl = "http://localhost:5093/api/v1/";
    private const string EnvApiBaseUrl = "ELEMENTUM_API_BASEURL";

    private static readonly Lazy<string> _baseUrl = new(LoadBaseUrl);

    /// <summary>API base URL for HTTP calls. Set ELEMENTUM_API_BASEURL to override.</summary>
    public static string ApiBaseUrl => _baseUrl.Value;

    /// <summary>Trims the URL and ensures it ends with a single trailing slash.</summary>
    public static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.Trim().TrimEnd('/') + "/";

    /// <summary>Returns the API base URL: env overrides file, file overrides default.</summary>
    public static string ResolveApiBaseUrl(string? fromEnv, string? fromFile)
    {
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return NormalizeBaseUrl(fromEnv);
        if (!string.IsNullOrWhiteSpace(fromFile))
            return NormalizeBaseUrl(fromFile);
        return DefaultBaseUrl;
    }

    private static string LoadBaseUrl()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return NormalizeBaseUrl(fromEnv);
        }

        string? fromFile = null;
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var config = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            fromFile = config["ApiBaseUrl"];
        }
        catch (Exception ex)
        {
            CliLogging.GetLogger(nameof(CliConfig)).LogWarning(ex, "Failed to load ApiBaseUrl from appsettings.json, using default");
        }

        return ResolveApiBaseUrl(fromEnv, fromFile);
    }
}
