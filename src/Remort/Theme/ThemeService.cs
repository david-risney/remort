using System.Windows;
using System.Windows.Media;

namespace Remort.Theme;

/// <summary>
/// Applies <see cref="ColorProfile"/> values to the application's merged resource dictionaries
/// so that controls using <c>DynamicResource</c> update immediately.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly ResourceDictionary _appResources;
    private ResourceDictionary? _currentThemeDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class and applies the default profile.
    /// </summary>
    /// <param name="appResources">The application-level <see cref="ResourceDictionary"/>.</param>
    public ThemeService(ResourceDictionary appResources)
    {
        _appResources = appResources;
        ApplyProfile(PresetProfiles.Default);
    }

    /// <inheritdoc/>
    public event EventHandler? ProfileChanged;

    /// <inheritdoc/>
    public ColorProfile ActiveProfile { get; private set; } = PresetProfiles.Default;

    /// <inheritdoc/>
    public void ApplyProfile(ColorProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var dictionary = new ResourceDictionary
        {
            [ThemeResourceKeys.PrimaryBackground] = BrushFromHex(profile.PrimaryBackground),
            [ThemeResourceKeys.SecondaryBackground] = BrushFromHex(profile.SecondaryBackground),
            [ThemeResourceKeys.Accent] = BrushFromHex(profile.Accent),
            [ThemeResourceKeys.TextPrimary] = BrushFromHex(profile.TextPrimary),
            [ThemeResourceKeys.TextSecondary] = BrushFromHex(profile.TextSecondary),
            [ThemeResourceKeys.Border] = BrushFromHex(profile.Border),
        };

        if (_currentThemeDictionary is not null)
        {
            _appResources.MergedDictionaries.Remove(_currentThemeDictionary);
        }

        _appResources.MergedDictionaries.Add(dictionary);
        _currentThemeDictionary = dictionary;

        ActiveProfile = profile;
        ProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
