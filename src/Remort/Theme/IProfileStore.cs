namespace Remort.Theme;

/// <summary>
/// Persists custom color profiles.
/// </summary>
public interface IProfileStore
{
    /// <summary>Loads all saved custom profiles.</summary>
    /// <returns>A list of custom profiles, or empty if none exist.</returns>
    public IReadOnlyList<ColorProfile> LoadCustomProfiles();

    /// <summary>Saves the complete list of custom profiles, replacing any previous data.</summary>
    /// <param name="profiles">The custom profiles to persist.</param>
    public void SaveCustomProfiles(IReadOnlyList<ColorProfile> profiles);

    /// <summary>Deletes all custom profiles.</summary>
    public void DeleteAll();
}
