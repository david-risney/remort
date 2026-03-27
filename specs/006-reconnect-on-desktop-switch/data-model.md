# Data Model: Reconnect on Virtual Desktop Switch

**Feature**: 006-reconnect-on-desktop-switch
**Date**: 2026-03-23

## Entities

### AppSettings (Record — Settings/AppSettings.cs) — Modified

Add `ReconnectOnDesktopSwitchEnabled` to the existing settings record.

```csharp
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
}
```

| Field | Type | Default | JSON Key | Constraints |
|-------|------|---------|----------|-------------|
| `ReconnectOnDesktopSwitchEnabled` | `bool` | `false` | `reconnectOnDesktopSwitchEnabled` | None — boolean toggle |

### IDesktopSwitchDetector (Interface — VirtualDesktop/IDesktopSwitchDetector.cs) — New

Monitors whether the application window is on the currently visible virtual desktop, raising an event on `false → true` transitions.

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Detects when the application window transitions from a non-visible virtual
/// desktop to the currently active virtual desktop.
/// </summary>
public interface IDesktopSwitchDetector : IDisposable
{
    /// <summary>
    /// Raised when the monitored window becomes visible on the current virtual desktop
    /// after previously being on a different (non-visible) desktop.
    /// </summary>
    event EventHandler? SwitchedToDesktop;

    /// <summary>
    /// Begins monitoring the specified window for desktop switch transitions.
    /// </summary>
    /// <param name="hwnd">The top-level window handle to monitor.</param>
    void StartMonitoring(IntPtr hwnd);

    /// <summary>
    /// Stops monitoring. No further <see cref="SwitchedToDesktop"/> events will be raised.
    /// </summary>
    void StopMonitoring();
}
```

| Member | Type | Description |
|--------|------|-------------|
| `SwitchedToDesktop` | `event EventHandler?` | Fires on `false → true` transition of `IsOnCurrentDesktop` |
| `StartMonitoring(IntPtr)` | `void` | Begins polling with the given window handle |
| `StopMonitoring()` | `void` | Stops the polling timer |

### DesktopSwitchDetector (Class — VirtualDesktop/DesktopSwitchDetector.cs) — New

Implementation of `IDesktopSwitchDetector` using a `DispatcherTimer`.

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Polls <see cref="IVirtualDesktopService.IsOnCurrentDesktop"/> at a fixed interval
/// and raises <see cref="IDesktopSwitchDetector.SwitchedToDesktop"/> on false→true transitions.
/// </summary>
public sealed class DesktopSwitchDetector : IDesktopSwitchDetector
{
    private readonly IVirtualDesktopService _virtualDesktopService;
    private readonly DispatcherTimer _timer;
    private IntPtr _hwnd;
    private bool _wasOnCurrentDesktop = true;

    // ...
}
```

| Field | Type | Description |
|-------|------|-------------|
| `_virtualDesktopService` | `IVirtualDesktopService` | Service to query desktop visibility |
| `_timer` | `DispatcherTimer` | Polls at 500ms interval |
| `_hwnd` | `IntPtr` | The window handle being monitored |
| `_wasOnCurrentDesktop` | `bool` | Previous poll result; initialized to `true` to avoid false trigger on startup |

### IVirtualDesktopService (Interface — VirtualDesktop/IVirtualDesktopService.cs) — Modified

Add `IsOnCurrentDesktop` method.

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Manages pinning the application window to a specific virtual desktop.
/// </summary>
public interface IVirtualDesktopService
{
    /// <summary>
    /// Gets a value indicating whether the virtual desktop API is available
    /// on the current system.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Returns whether the specified window is on the currently active virtual desktop.
    /// Returns <c>true</c> if the API is not supported (graceful degradation).
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    /// <returns><c>true</c> if the window is on the current desktop or the API is unavailable.</returns>
    bool IsOnCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Pins the specified window to its current virtual desktop,
    /// ensuring it does not appear on other desktops.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    void PinToCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Releases the pin, returning the window to default OS virtual desktop behavior.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    void Unpin(IntPtr hwnd);
}
```

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `IsOnCurrentDesktop` | `IntPtr hwnd` | `bool` | Whether the window is on the active desktop |

### MainWindowViewModel (Class — Connection/MainWindowViewModel.cs) — Modified

New observable property and method additions.

| Member | Type | Description |
|--------|------|-------------|
| `ReconnectOnDesktopSwitchEnabled` | `bool` (`[ObservableProperty]`) | Toggle for the feature. Persisted via `ISettingsStore`. |
| `TryReconnectOnDesktopSwitch()` | `void` (public method) | Entry point called by code-behind on desktop switch detection. Checks preconditions and initiates reconnect. |

### State Transitions

```
Desktop Switch Detection (DesktopSwitchDetector):

  ┌─────────────────────┐      IsOnCurrentDesktop = true      ┌────────────────────┐
  │   Off Current        │ ──────────────────────────────────► │  On Current         │
  │   Desktop            │                                     │  Desktop            │
  │   (_wasOnCurrent =   │ ◄────────────────────────────────── │  (_wasOnCurrent =   │
  │    false)            │      IsOnCurrentDesktop = false      │   true)             │
  └─────────────────────┘                                     └────────────────────┘
                                    │
                          false → true transition
                                    │
                                    ▼
                          SwitchedToDesktop event
                                    │
                                    ▼
                       ViewModel.TryReconnectOnDesktopSwitch()
```

```
Reconnect Decision (MainWindowViewModel.TryReconnectOnDesktopSwitch):

  SwitchedToDesktop event received
          │
          ▼
  ReconnectOnDesktopSwitchEnabled == true? ──No──► return (no-op)
          │ Yes
          ▼
  PinToDesktopEnabled == true? ──No──► return (no-op)
          │ Yes
          ▼
  ConnectionState == Disconnected? ──No──► return (session active or connecting)
          │ Yes
          ▼
  LastConnectedHost is non-empty? ──No──► return (no host to reconnect to)
          │ Yes
          ▼
  Set _isAutoReconnect = true
  Set Hostname = lastHost
  Set ConnectionState = Connecting
  Call _connectionService.Connect()
```
