namespace Elementum_WorkerService.Hosting;

/// <summary>
/// Loads optional <c>.env</c> files into the process environment so <c>Configuration</c> can read METALS_API_KEY, connection strings, etc.
/// In production, prefer real environment variables or secrets instead of files.
/// </summary>
public static class WorkerDotNetEnvConfiguration
{
    /// <summary>
    /// Tries to load <c>.env</c> from the current directory, otherwise walks parent paths (DotNetEnv).
    /// Silently ignores missing files when variables are already set in the host environment.
    /// </summary>
    public static void LoadOptionalEnvFile()
    {
        try
        {
            if (File.Exists(".env"))
                DotNetEnv.Env.Load();
            else
                DotNetEnv.Env.TraversePath().Load();
        }
        catch (FileNotFoundException)
        {
            // .env is optional when using machine/environment configuration only.
        }
    }
}
