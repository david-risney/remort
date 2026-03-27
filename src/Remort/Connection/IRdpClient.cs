using Remort.Interop;

namespace Remort.Connection;

/// <summary>
/// Abstracts an RDP client connection for use by ViewModels.
/// Implemented by <see cref="RdpClientHost"/>.
/// </summary>
public interface IRdpClient
{
    /// <summary>Raised when the control begins connecting.</summary>
    public event EventHandler? Connecting;

    /// <summary>Raised when the connection is established.</summary>
    public event EventHandler? Connected;

    /// <summary>Raised when the session disconnects (normal, error, or remote drop).</summary>
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>Gets or sets the target hostname or IP address.</summary>
    public string Server { get; set; }

    /// <summary>Gets or sets the remote desktop width in pixels.</summary>
    public int DesktopWidth { get; set; }

    /// <summary>Gets or sets the remote desktop height in pixels.</summary>
    public int DesktopHeight { get; set; }

    /// <summary>Gets a value indicating whether a session is currently active.</summary>
    public bool IsConnected { get; }

    /// <summary>Gets the extended disconnect reason code (valid only immediately after disconnect).</summary>
    public int ExtendedDisconnectReason { get; }

    /// <summary>Initiates an RDP connection to <see cref="Server"/>.</summary>
    public void Connect();

    /// <summary>Disconnects the active session, if any.</summary>
    public void Disconnect();

    /// <summary>
    /// Applies default advanced settings (NLA, SmartSizing, SmartCards,
    /// AuthLevel, ColorDepth, resolution). Must be called before <see cref="Connect"/>.
    /// </summary>
    public void ApplyDefaultSettings();

    /// <summary>Gets a human-readable error description for a disconnect reason.</summary>
    /// <param name="disconnectReason">The disconnect reason code.</param>
    /// <param name="extendedReason">The extended disconnect reason code.</param>
    /// <returns>A human-readable description of the error.</returns>
    public string GetErrorDescription(int disconnectReason, int extendedReason);
}
