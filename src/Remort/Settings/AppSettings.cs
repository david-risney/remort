namespace Remort.Settings;

/// <summary>
/// Application-wide settings, persisted to disk.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Gets the selected theme mode.</summary>
    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>Gets a value indicating whether DevBox automatic discovery is enabled.</summary>
    public bool DevBoxDiscoveryEnabled { get; init; } = true;

    /// <summary>Gets the Id of the last-selected device, used to restore selection on launch.</summary>
    public Guid? LastSelectedDeviceId { get; init; }
}
