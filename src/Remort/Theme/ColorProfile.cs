namespace Remort.Theme;

/// <summary>
/// A named collection of color values defining the visual appearance of the application UI.
/// </summary>
public sealed record ColorProfile
{
    /// <summary>Gets the display name of the profile.</summary>
    public required string Name { get; init; }

    /// <summary>Gets a value indicating whether this is a built-in preset profile (read-only).</summary>
    public bool IsPreset { get; init; }

    /// <summary>Gets the primary background color as a hex string (e.g. "#1E1E1E").</summary>
    public required string PrimaryBackground { get; init; }

    /// <summary>Gets the secondary background color as a hex string.</summary>
    public required string SecondaryBackground { get; init; }

    /// <summary>Gets the accent color as a hex string.</summary>
    public required string Accent { get; init; }

    /// <summary>Gets the primary text color as a hex string.</summary>
    public required string TextPrimary { get; init; }

    /// <summary>Gets the secondary text color as a hex string.</summary>
    public required string TextSecondary { get; init; }

    /// <summary>Gets the border color as a hex string.</summary>
    public required string Border { get; init; }
}
