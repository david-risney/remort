namespace Remort.Theme;

/// <summary>
/// Applies color profiles to the application UI at runtime.
/// </summary>
public interface IThemeService
{
    /// <summary>Raised after a profile is applied.</summary>
    public event EventHandler? ProfileChanged;

    /// <summary>Gets the currently active color profile.</summary>
    public ColorProfile ActiveProfile { get; }

    /// <summary>
    /// Applies a color profile immediately to the UI.
    /// </summary>
    /// <param name="profile">The profile to apply.</param>
    public void ApplyProfile(ColorProfile profile);
}
