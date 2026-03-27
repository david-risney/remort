using System.IO;
using System.Text.Json;

namespace Remort.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON to the user's app-data folder.
/// Creates the file with defaults on first run. Falls back to defaults on malformed JSON.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsStore"/> class
    /// using the default settings path (<c>%APPDATA%/Remort/settings.json</c>).
    /// </summary>
    public JsonSettingsStore()
        : this(GetDefaultFilePath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsStore"/> class
    /// with a custom file path (for testing).
    /// </summary>
    /// <param name="filePath">The full path to the settings JSON file.</param>
    public JsonSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new AppSettings();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json, s_jsonOptions) ?? new AppSettings();
        }
#pragma warning disable CA1031 // Do not catch general exception types — malformed JSON falls back to defaults per research R4
        catch
#pragma warning restore CA1031
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc/>
    public void Save(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, s_jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string GetDefaultFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Remort", "settings.json");
    }
}
