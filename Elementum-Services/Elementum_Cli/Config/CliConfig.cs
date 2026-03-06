using Microsoft.Extensions.Configuration;

namespace Elementum_Cli.Config;

/// <summary>CLI configuration. Base URL: env ELEMENTUM_API_BASEURL overrides appsettings.json.</summary>
public static class CliConfig
{
    private const string DefaultBaseUrl = "http://localhost:5000/api/v1/";
    private const string EnvApiBaseUrl = "ELEMENTUM_API_BASEURL";

    private static readonly Lazy<string> _baseUrl = new Lazy<string>(LoadBaseUrl);

    /// <summary>API base URL for HTTP calls. Set ELEMENTUM_API_BASEURL to override.</summary>
    public static string ApiBaseUrl => _baseUrl.Value;

    private static string LoadBaseUrl()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.TrimEnd('/') + "/";
        }

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var config = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            var fromFile = config["ApiBaseUrl"];
            if (!string.IsNullOrWhiteSpace(fromFile))
                return fromFile.TrimEnd('/') + "/";
        }
        catch
        {
            // Fall back to default
        }

        return DefaultBaseUrl;
    }
}
