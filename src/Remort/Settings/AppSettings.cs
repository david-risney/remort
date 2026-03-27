namespace Remort.Settings;

/// <summary>
/// Application-wide settings, persisted to disk.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Gets the maximum number of connection retry attempts.</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Gets a value indicating whether auto-reconnect on Windows login is enabled.</summary>
    public bool AutoReconnectEnabled { get; init; }

    /// <summary>Gets the hostname of the last connected (or last attempted) host.</summary>
    public string LastConnectedHost { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the window is pinned to the current virtual desktop.</summary>
    public bool PinToDesktopEnabled { get; init; }

    /// <summary>Gets a value indicating whether the session auto-reconnects when the pinned desktop becomes visible.</summary>
    public bool ReconnectOnDesktopSwitchEnabled { get; init; }

    /// <summary>Gets the name of the active color profile. Empty means use default dark theme.</summary>
    public string ActiveProfileName { get; init; } = string.Empty;
}
