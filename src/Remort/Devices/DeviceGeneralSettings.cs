namespace Remort.Devices;

/// <summary>
/// Per-device general preferences.
/// </summary>
public sealed record DeviceGeneralSettings
{
    /// <summary>Gets a value indicating whether the titlebar auto-hides when the cursor moves away.</summary>
    public bool AutohideTitleBar { get; init; }
}
