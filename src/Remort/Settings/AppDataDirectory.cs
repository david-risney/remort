namespace Remort.Settings;

/// <summary>
/// Provides the base directory for all application data files.
/// Defaults to <c>%APPDATA%/Remort/</c> but can be overridden via
/// the <c>--data-dir</c> command line argument or the <c>REMORT_DATA_DIR</c> environment variable.
/// </summary>
public static class AppDataDirectory
{
    private static string? s_override;

    /// <summary>
    /// Gets the base directory for application data.
    /// </summary>
    public static string Path => s_override ?? GetDefaultPath();

    /// <summary>
    /// Sets a custom data directory. Call before any store is created.
    /// </summary>
    /// <param name="path">The custom directory path.</param>
    public static void SetOverride(string path)
    {
        s_override = path;
    }

    /// <summary>
    /// Resets to the default data directory.
    /// </summary>
    public static void ResetOverride()
    {
        s_override = null;
    }

    private static string GetDefaultPath()
    {
        string? envOverride = Environment.GetEnvironmentVariable("REMORT_DATA_DIR");
        if (!string.IsNullOrEmpty(envOverride))
        {
            return envOverride;
        }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "Remort");
    }
}
