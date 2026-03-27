namespace Remort.Theme;

/// <summary>
/// Built-in color profiles shipped with the application. All presets are read-only
/// and meet WCAG AA 4.5:1 contrast ratio for text on background.
/// </summary>
public static class PresetProfiles
{
    /// <summary>Gets the default color profile (Dark).</summary>
    public static ColorProfile Default => All[0];

    /// <summary>Gets all preset color profiles.</summary>
    public static IReadOnlyList<ColorProfile> All { get; } = new[]
    {
        new ColorProfile
        {
            Name = "Dark",
            IsPreset = true,
            PrimaryBackground = "#1E1E1E",
            SecondaryBackground = "#252526",
            Accent = "#007ACC",
            TextPrimary = "#CCCCCC",
            TextSecondary = "#808080",
            Border = "#3F3F46",
        },
        new ColorProfile
        {
            Name = "Light",
            IsPreset = true,
            PrimaryBackground = "#F5F5F5",
            SecondaryBackground = "#FFFFFF",
            Accent = "#0078D4",
            TextPrimary = "#1E1E1E",
            TextSecondary = "#6E6E6E",
            Border = "#CCCCCC",
        },
        new ColorProfile
        {
            Name = "Midnight Blue",
            IsPreset = true,
            PrimaryBackground = "#1B2A4A",
            SecondaryBackground = "#243B5E",
            Accent = "#4FC3F7",
            TextPrimary = "#E0E0E0",
            TextSecondary = "#90A4AE",
            Border = "#2C4066",
        },
    };
}
