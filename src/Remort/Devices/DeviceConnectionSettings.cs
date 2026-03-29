namespace Remort.Devices;

/// <summary>
/// Per-device connection preferences.
/// </summary>
public sealed record DeviceConnectionSettings
{
    /// <summary>Gets a value indicating whether the device auto-connects when the app launches.</summary>
    public bool AutoconnectOnStart { get; init; }

    /// <summary>Gets a value indicating whether the device auto-connects when its virtual desktop becomes active.</summary>
    public bool AutoconnectWhenVisible { get; init; }

    /// <summary>Gets a value indicating whether the app should launch at Windows logon.</summary>
    public bool StartOnStartup { get; init; }

    /// <summary>Gets the maximum number of connection retry attempts.</summary>
    public int MaxRetryCount { get; init; } = 3;
}
