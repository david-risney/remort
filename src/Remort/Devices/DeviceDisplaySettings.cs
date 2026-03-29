namespace Remort.Devices;

/// <summary>
/// Per-device display preferences.
/// </summary>
public sealed record DeviceDisplaySettings
{
    /// <summary>Gets a value indicating whether the device window is pinned to the current virtual desktop.</summary>
    public bool PinToVirtualDesktop { get; init; }

    /// <summary>Gets a value indicating whether the remote session scales to fit the window.</summary>
    public bool FitSessionToWindow { get; init; } = true;

    /// <summary>Gets a value indicating whether the window spans all monitors when maximized.</summary>
    public bool UseAllMonitors { get; init; }
}
