# Contract: IRdpClient Interface

**Feature**: 001-rdp-connect-disconnect
**Type**: Internal interface (Connection domain → Interop layer boundary)
**File**: `src/Remort/Connection/IRdpClient.cs`

## Purpose

`IRdpClient` is the abstraction boundary between the Connection Management domain and the COM Interop layer. It allows the `MainWindowViewModel` to drive RDP connections without any dependency on `AxHost`, `WindowsFormsHost`, COM interfaces, or `dynamic` dispatch.

This interface is the primary seam for unit testing — the ViewModel is tested with `NSubstitute` mocks of `IRdpClient`.

## Interface Definition

```csharp
namespace Remort.Connection;

/// <summary>
/// Abstracts an RDP client connection for use by ViewModels.
/// Implemented by <see cref="Interop.RdpClientHost"/>.
/// </summary>
public interface IRdpClient
{
    /// <summary>Gets or sets the target hostname or IP address.</summary>
    string Server { get; set; }

    /// <summary>Gets or sets the remote desktop width in pixels.</summary>
    int DesktopWidth { get; set; }

    /// <summary>Gets or sets the remote desktop height in pixels.</summary>
    int DesktopHeight { get; set; }

    /// <summary>Gets a value indicating whether a session is currently active.</summary>
    bool IsConnected { get; }

    /// <summary>Initiates an RDP connection to <see cref="Server"/>.</summary>
    void Connect();

    /// <summary>Disconnects the active session, if any.</summary>
    void Disconnect();

    /// <summary>
    /// Applies default advanced settings (NLA, SmartSizing, SmartCards,
    /// AuthLevel, ColorDepth, resolution). Must be called before <see cref="Connect"/>.
    /// </summary>
    void ApplyDefaultSettings();

    /// <summary>Gets the extended disconnect reason code (valid only immediately after disconnect).</summary>
    int ExtendedDisconnectReason { get; }

    /// <summary>Gets a human-readable error description for a disconnect reason.</summary>
    string GetErrorDescription(int disconnectReason, int extendedReason);

    /// <summary>Raised when the control begins connecting.</summary>
    event EventHandler? Connecting;

    /// <summary>Raised when the connection is established.</summary>
    event EventHandler? Connected;

    /// <summary>Raised when the session disconnects (normal, error, or remote drop).</summary>
    event EventHandler<DisconnectedEventArgs>? Disconnected;
}
```

## Invariants

1. `Connect()` MUST only be called when `IsConnected == false`.
2. `Server`, `DesktopWidth`, `DesktopHeight`, and `ApplyDefaultSettings()` MUST be set/called before `Connect()`.
3. `Disconnect()` is safe to call at any time — it is a no-op if not connected.
4. `ExtendedDisconnectReason` is only meaningful immediately after a `Disconnected` event fires.
5. Events fire on the UI thread (STA apartment).

## Consumers

| Consumer | Usage |
|----------|-------|
| `MainWindowViewModel` | Calls Connect/Disconnect, subscribes to events, reads state |
| `MainWindowViewModelTests` | NSubstitute mock for unit testing |

## Implementors

| Implementor | Location |
|-------------|----------|
| `RdpClientHost` | `src/Remort/Interop/RdpClientHost.cs` |
