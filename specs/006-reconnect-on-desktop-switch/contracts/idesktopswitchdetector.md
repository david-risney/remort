# Contract: IDesktopSwitchDetector Interface

**Feature**: 006-reconnect-on-desktop-switch
**Type**: Internal interface (ViewModel ← VirtualDesktop service boundary)
**File**: `src/Remort/VirtualDesktop/IDesktopSwitchDetector.cs`

## Purpose

`IDesktopSwitchDetector` is the abstraction boundary between `MainWindowViewModel` and the desktop-switch polling logic. It hides the `DispatcherTimer` and `IVirtualDesktopService.IsOnCurrentDesktop()` polling behind a simple event-driven interface. The ViewModel subscribes to `SwitchedToDesktop` and reacts with reconnect logic. Unit tests mock this interface to simulate desktop switches without timers, COM, or real virtual desktops.

## Interface Definition

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

## Event Contract: SwitchedToDesktop

- **Fires when**: The monitored window transitions from "not on current virtual desktop" (`false`) to "on current virtual desktop" (`true`), as reported by `IVirtualDesktopService.IsOnCurrentDesktop()`.
- **Does NOT fire when**: The window is already on the current desktop and remains there (steady state `true → true`). This prevents duplicate events.
- **Does NOT fire when**: The window leaves the current desktop (`true → false`). This is the user switching away — not relevant for reconnection.
- **Does NOT fire when**: Monitoring has not been started, or has been stopped.
- **Thread**: Fires on the UI (dispatcher) thread, since the underlying `DispatcherTimer` ticks on the dispatcher thread.
- **Debounce**: The polling interval (500ms) provides natural debounce. Rapid desktop switches (e.g., Ctrl+Win+Arrow held down) that resolve within a single poll interval produce at most one event for the final landing desktop.
- **Sender**: The `DesktopSwitchDetector` instance.
- **EventArgs**: `EventArgs.Empty` — the event signals the transition; the consumer determines context (which host, what state) from its own state.

## Method Contracts

### `StartMonitoring(IntPtr hwnd)`

- **Precondition**: `hwnd` is a valid, non-zero top-level window handle (obtained after `Window.SourceInitialized`).
- **Postcondition**: The internal polling timer starts. `_wasOnCurrentDesktop` is initialized to the current state of `IsOnCurrentDesktop(hwnd)` to prevent a false trigger on the first tick.
- **Idempotent**: Calling with a new hwnd stops any existing monitoring and restarts with the new handle.
- **Error handling**: If `hwnd` is `IntPtr.Zero`, the method is a no-op (does not start or raise events).

### `StopMonitoring()`

- **Postcondition**: The polling timer stops. No further `SwitchedToDesktop` events fire until `StartMonitoring` is called again.
- **Idempotent**: Calling when already stopped is a no-op.

### `Dispose()`

- Calls `StopMonitoring()` and releases the timer. The detector cannot be restarted after disposal.

## Consumer Responsibilities

- The **View** (`MainWindow.xaml.cs`) creates the `DesktopSwitchDetector` during window construction, calls `StartMonitoring(hwnd)` in `OnSourceInitialized`, and disposes it in `OnClosing`.
- The **ViewModel** (`MainWindowViewModel`) subscribes to `SwitchedToDesktop` and calls `TryReconnectOnDesktopSwitch()`.
- The **ViewModel** does NOT manage the detector lifecycle (start/stop). The View owns the lifecycle because the hwnd is a View-layer concern.

## Implementation Notes

- The `DesktopSwitchDetector` constructor takes `IVirtualDesktopService` as a dependency (injected). It does NOT create the timer in the constructor — the timer is created in `StartMonitoring` so the detector can be constructed before the hwnd is available.
- `_wasOnCurrentDesktop` is initialized to `true` in the field declaration. This means if the window starts on the current desktop (the common case), the first poll tick sees `true → true` and does nothing. If the window somehow starts off-desktop, the transition detection catches it on the next tick.
- The `DispatcherTimer.Interval` is set to `TimeSpan.FromMilliseconds(500)`.
- All methods are synchronous. No `async` needed — the COM call is in-process and completes in microseconds.
