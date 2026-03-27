namespace Remort.Settings;

/// <summary>
/// Reads and writes application settings.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Loads settings from the backing store.</summary>
    /// <returns>The loaded settings, or defaults if not found.</returns>
    public AppSettings Load();

    /// <summary>Saves settings to the backing store.</summary>
    /// <param name="settings">The settings to persist.</param>
    public void Save(AppSettings settings);
}
