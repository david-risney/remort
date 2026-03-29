namespace Remort.Devices;

/// <summary>
/// Per-device RDP local resource redirection toggles.
/// </summary>
public sealed record DeviceRedirectionSettings
{
    /// <summary>Gets a value indicating whether clipboard redirection is enabled.</summary>
    public bool Clipboard { get; init; } = true;

    /// <summary>Gets a value indicating whether printer redirection is enabled.</summary>
    public bool Printers { get; init; }

    /// <summary>Gets a value indicating whether local drive redirection is enabled.</summary>
    public bool Drives { get; init; }

    /// <summary>Gets a value indicating whether remote audio is played locally.</summary>
    public bool AudioPlayback { get; init; } = true;

    /// <summary>Gets a value indicating whether microphone redirection is enabled.</summary>
    public bool AudioRecording { get; init; }

    /// <summary>Gets a value indicating whether smart card redirection is enabled.</summary>
    public bool SmartCards { get; init; }

    /// <summary>Gets a value indicating whether serial port redirection is enabled.</summary>
    public bool SerialPorts { get; init; }

    /// <summary>Gets a value indicating whether USB device redirection is enabled.</summary>
    public bool UsbDevices { get; init; }
}
