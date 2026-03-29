namespace Remort.Devices;

/// <summary>
/// Represents a remote machine the user can connect to.
/// </summary>
public sealed record Device
{
    /// <summary>Gets the stable identifier that survives renames.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the display name shown on the card and titlebar.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the DNS hostname or IP address used by the RDP client.</summary>
    public required string Hostname { get; init; }

    /// <summary>Gets a value indicating whether this device appears on the Favorites page.</summary>
    public bool IsFavorite { get; init; }

    /// <summary>Gets a value indicating whether this device was discovered via DevBox resolution.</summary>
    public bool IsDiscovered { get; init; }

    /// <summary>Gets the relative path to the last-captured session screenshot PNG, or <see langword="null"/>.</summary>
    public string? LastScreenshotPath { get; init; }

    /// <summary>Gets the per-device connection preferences.</summary>
    public DeviceConnectionSettings ConnectionSettings { get; init; } = new();

    /// <summary>Gets the per-device general preferences.</summary>
    public DeviceGeneralSettings GeneralSettings { get; init; } = new();

    /// <summary>Gets the per-device display preferences.</summary>
    public DeviceDisplaySettings DisplaySettings { get; init; } = new();

    /// <summary>Gets the per-device redirection toggles.</summary>
    public DeviceRedirectionSettings RedirectionSettings { get; init; } = new();
}
