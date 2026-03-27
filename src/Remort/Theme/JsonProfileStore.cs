using System.IO;
using System.Text.Json;

namespace Remort.Theme;

/// <summary>
/// Persists custom <see cref="ColorProfile"/> instances as JSON to the user's app-data folder.
/// Follows the same fallback pattern as <see cref="Settings.JsonSettingsStore"/>.
/// </summary>
public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProfileStore"/> class
    /// using the default path (<c>%APPDATA%/Remort/color-profiles.json</c>).
    /// </summary>
    public JsonProfileStore()
        : this(GetDefaultFilePath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProfileStore"/> class
    /// with a custom file path (for testing).
    /// </summary>
    /// <param name="filePath">The full path to the profiles JSON file.</param>
    public JsonProfileStore(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ColorProfile> LoadCustomProfiles()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            var profiles = JsonSerializer.Deserialize<List<ColorProfile>>(json, s_jsonOptions);
            if (profiles is null)
            {
                return [];
            }

            // Enforce IsPreset = false for all loaded profiles
            return profiles.Select(p => p with { IsPreset = false }).ToList();
        }
#pragma warning disable CA1031 // Do not catch general exception types — malformed JSON falls back to empty list
        catch
#pragma warning restore CA1031
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public void SaveCustomProfiles(IReadOnlyList<ColorProfile> profiles)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(profiles, s_jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    /// <inheritdoc/>
    public void DeleteAll()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private static string GetDefaultFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Remort", "color-profiles.json");
    }
}
